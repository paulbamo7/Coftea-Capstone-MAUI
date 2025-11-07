using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Coftea_Capstone.Services
{
    public class PayMongoService
    {
        private const string BaseUrl = "https://api.paymongo.com/v1";
        private const string SecretKey = "sk_test_K3bzPFjFBPJ5LGhnLcws5Tij";
        private readonly HttpClient _httpClient;

        public PayMongoService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(BaseUrl);
            }

            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{SecretKey}:"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<(bool Success, string? CheckoutUrl, string? SourceId, string? ErrorMessage)> CreateGCashSourceAsync(decimal amount, string description, string? customerName = null, string? customerEmail = null, string? customerPhone = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var amountInCents = (int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
                var payload = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            amount = amountInCents,
                            currency = "PHP",
                            type = "gcash",
                            description,
                            statement_descriptor = "Coftea POS",
                            redirect = new
                            {
                                success = "https://coftea.app/payments/success",
                                failed = "https://coftea.app/payments/failed"
                            },
                            billing = new
                            {
                                name = string.IsNullOrWhiteSpace(customerName) ? "Coftea POS Customer" : customerName,
                                email = string.IsNullOrWhiteSpace(customerEmail) ? "support@coftea.app" : customerEmail,
                                phone = string.IsNullOrWhiteSpace(customerPhone) ? "00000000000" : customerPhone
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, "/sources")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var error = TryParseError(content);
                    return (false, null, null, error ?? $"PayMongo error ({response.StatusCode}): {content}");
                }

                try
                {
                    using var doc = JsonDocument.Parse(content);
                    var data = doc.RootElement.GetProperty("data");
                    var attributes = data.GetProperty("attributes");
                    var redirect = attributes.GetProperty("redirect");
                    var checkoutUrl = redirect.GetProperty("checkout_url").GetString();
                    var sourceId = data.GetProperty("id").GetString();
                    return (true, checkoutUrl, sourceId, null);
                }
                catch (Exception parseEx)
                {
                    return (false, null, null, $"Unable to parse PayMongo response: {parseEx.Message}");
                }
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }

        public async Task<(bool Success, string Status, string? ErrorMessage)> RetrieveSourceStatusAsync(string sourceId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync($"/sources/{sourceId}", cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var error = TryParseError(content);
                    return (false, "unknown", error ?? $"PayMongo error ({response.StatusCode}): {content}");
                }

                using var doc = JsonDocument.Parse(content);
                var attributes = doc.RootElement.GetProperty("data").GetProperty("attributes");
                var status = attributes.GetProperty("status").GetString() ?? "unknown";
                return (true, status, null);
            }
            catch (Exception ex)
            {
                return (false, "unknown", ex.Message);
            }
        }

        private static string? TryParseError(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
                {
                    var first = errors.EnumerateArray().FirstOrDefault();
                    if (first.ValueKind != JsonValueKind.Undefined && first.TryGetProperty("detail", out var detail))
                    {
                        return detail.GetString();
                    }
                }
            }
            catch
            {
                // ignore parsing errors
            }

            return null;
        }
    }
}
