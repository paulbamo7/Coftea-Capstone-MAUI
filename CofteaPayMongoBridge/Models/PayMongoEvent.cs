using System.Text.Json.Serialization;

namespace CofteaPayMongoBridge.Models;

public class PayMongoEvent
{
    [JsonPropertyName("data")]
    public PayMongoEventData? Data { get; set; }
}

public class PayMongoEventData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("attributes")]
    public PayMongoEventAttributes? Attributes { get; set; }
}

public class PayMongoEventAttributes
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("amount")]
    public int? AmountInCents { get; set; }

    [JsonPropertyName("billing")]
    public PayMongoBilling? Billing { get; set; }

    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("resource_id")]
    public string? ResourceId { get; set; }

    [JsonPropertyName("data")]
    public PayMongoResourceWrapper? Resource { get; set; }

    [JsonIgnore]
    public decimal? AttributesAmount => AmountInCents.HasValue ? AmountInCents.Value / 100m : null;
}

public class PayMongoResourceWrapper
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("attributes")]
    public PayMongoResourceAttributes? Attributes { get; set; }
}

public class PayMongoResourceAttributes
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("amount")]
    public int? AmountInCents { get; set; }

    [JsonPropertyName("billing")]
    public PayMongoBilling? Billing { get; set; }

    [JsonIgnore]
    public decimal? AttributesAmount => AmountInCents.HasValue ? AmountInCents.Value / 100m : null;
}

public class PayMongoBilling
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
