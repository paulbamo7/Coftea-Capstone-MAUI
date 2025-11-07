using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CofteaPayMongoBridge.Models;
using CofteaPayMongoBridge.Options;
using CofteaPayMongoBridge.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PayMongoOptions>(builder.Configuration.GetSection("PayMongo"));
builder.Services.AddSingleton<PayMongoOptions>(sp =>
{
    var options = sp.GetRequiredService<IOptions<PayMongoOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.WebhookSecret))
    {
        options.WebhookSecret = Environment.GetEnvironmentVariable("PAYMONGO_WEBHOOK_SECRET") ?? string.Empty;
    }
    return options;
});

builder.Services.AddSingleton<PaymentStatusStore>();
builder.Services.AddSingleton<WebhookVerifier>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/paymongo/webhook", async (HttpRequest request, WebhookVerifier verifier, PaymentStatusStore store, PayMongoOptions options, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("PayMongoWebhook");
    var payload = await new StreamReader(request.Body).ReadToEndAsync();
    var signatureHeader = request.Headers["Paymongo-Signature"].ToString();

    if (!string.IsNullOrWhiteSpace(options.WebhookSecret))
    {
        if (!verifier.Verify(signatureHeader, payload, options.WebhookSecret))
        {
            logger.LogWarning("Invalid PayMongo signature. Header: {SignatureHeader}", signatureHeader);
            return Results.Unauthorized();
        }
    }
    else
    {
        logger.LogWarning("Skipping signature verification because no signing secret is configured.");
    }

    PayMongoEvent? envelope;
    try
    {
        envelope = JsonSerializer.Deserialize<PayMongoEvent>(payload);
    }
    catch (JsonException ex)
    {
        logger.LogError(ex, "Unable to parse webhook payload");
        return Results.BadRequest(new { message = "Invalid payload" });
    }

    if (envelope?.Data?.Attributes == null)
    {
        logger.LogWarning("Webhook payload missing data.attributes");
        return Results.BadRequest(new { message = "Missing data" });
    }

    var attrs = envelope.Data.Attributes;
    var resource = attrs.Resource;
    var resourceType = attrs.ResourceType ?? resource?.Type;
    var isSourceEvent = !string.IsNullOrWhiteSpace(resourceType) && resourceType.StartsWith("source", StringComparison.OrdinalIgnoreCase);
    if (!isSourceEvent)
    {
        logger.LogInformation("Ignoring non-source event type: {Type}", resourceType ?? attrs.Type);
        return Results.Ok();
    }

    var sourceId = attrs.ResourceId ?? resource?.Id;
    if (string.IsNullOrWhiteSpace(sourceId))
    {
        logger.LogWarning("Webhook payload missing source id");
        return Results.BadRequest(new { message = "Missing source id" });
    }

    var status = attrs.Status ?? resource?.Attributes?.Status ?? "unknown";
    var email = attrs.Billing?.Email ?? resource?.Attributes?.Billing?.Email;
    var amount = attrs.AttributesAmount ?? resource?.Attributes?.AttributesAmount;

    store.Upsert(sourceId, status, email, amount);
    logger.LogInformation("Stored status {Status} for source {SourceId}", status, sourceId);

    return Results.Ok();
})
.WithName("PayMongoWebhook")
.WithOpenApi(operation =>
{
    operation.Summary = "PayMongo webhook endpoint";
    operation.Description = "Receives PayMongo source status updates.";
    return operation;
});

app.MapGet("/payment-status/{sourceId}", (string sourceId, PaymentStatusStore store) =>
{
    if (store.TryGet(sourceId, out var snapshot) && snapshot is not null)
    {
        return Results.Ok(snapshot);
    }

    return Results.NotFound();
})
.WithName("GetPaymentStatus");

app.MapPost("/payment-status/back-to-pos", (BackToPosRequest request, PaymentStatusStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.SourceId))
    {
        return Results.BadRequest(new { message = "sourceId is required" });
    }

    store.Upsert(request.SourceId, request.Status ?? "chargeable", request.CustomerEmail, request.Amount);
    return Results.Ok(new { message = "Status updated" });
})
.WithName("BackToPos");

app.Run();
