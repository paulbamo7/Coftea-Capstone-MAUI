using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CofteaPayMongoBridge.Services;

public class WebhookVerifier
{
    public bool Verify(string signatureHeader, string payload, string secret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        string? timestamp = null;
        var signatures = new List<string>();

        foreach (var part in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length != 2)
            {
                continue;
            }

            if (kv[0].Trim() == "t")
            {
                timestamp = kv[1].Trim();
            }
            else if (kv[0].Trim().Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                signatures.Add(kv[1].Trim());
            }
        }

        if (timestamp is null || signatures.Count == 0)
        {
            return false;
        }

        var signedPayload = $"t={timestamp}.{payload}";
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(signedPayload);

        using var hmac = new HMACSHA256(key);
        var hashBytes = hmac.ComputeHash(data);
        var expected = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return signatures.Any(sig => string.Equals(sig, expected, StringComparison.OrdinalIgnoreCase));
    }
}
