using System.Net;
using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace Coftea_Capstone.Services;

public class PayMongoBridgeService
{
    private const string PreferenceKey = "PayMongoBridgeBaseUrl";
    private const string DefaultBaseUrl = ""; // set a value here or via ConfigureBaseUrl/Preferences

    private readonly HttpClient _httpClient;
    private string _baseUrl;

    public PayMongoBridgeService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        _baseUrl = Preferences.Get(PreferenceKey, DefaultBaseUrl) ?? string.Empty;
    }

    public void ConfigureBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        _baseUrl = baseUrl.Trim().TrimEnd('/');
        Preferences.Set(PreferenceKey, _baseUrl);
    }

    public string GetConfiguredBaseUrl() => _baseUrl;

    public async Task<(bool Success, string? Status, string? Error)> GetStatusAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            return (false, null, "Bridge base URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return (false, null, "Source ID is required.");
        }

        try
        {
            var requestUri = $"{_baseUrl.TrimEnd('/')}/payment-status/{Uri.EscapeDataString(sourceId)}";
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (false, "notfound", null);
            }

            response.EnsureSuccessStatusCode();

            var snapshot = await response.Content.ReadFromJsonAsync<BridgeStatusResponse>(cancellationToken: cancellationToken);
            return (true, snapshot?.Status, null);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return (false, null, $"Bridge request timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> NotifyBackToPosAsync(string sourceId, string status = "chargeable", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            return (false, "Bridge base URL is not configured.");
        }

        try
        {
            var requestUri = $"{_baseUrl.TrimEnd('/')}/payment-status/back-to-pos";
            var payload = new BackToPosBridgeRequest
            {
                SourceId = sourceId,
                Status = status
            };

            var response = await _httpClient.PostAsJsonAsync(requestUri, payload, cancellationToken);
            response.EnsureSuccessStatusCode();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private sealed class BridgeStatusResponse
    {
        public string? SourceId { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string? CustomerEmail { get; set; }
        public decimal? Amount { get; set; }
    }

    private sealed class BackToPosBridgeRequest
    {
        public string? SourceId { get; set; }
        public string? Status { get; set; }
    }
}
