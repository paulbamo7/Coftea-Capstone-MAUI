using System.Collections.Concurrent;
using CofteaPayMongoBridge.Models;

namespace CofteaPayMongoBridge.Services;

public class PaymentStatusStore
{
    private readonly ConcurrentDictionary<string, PaymentStatusSnapshot> _statuses = new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(string sourceId, string status, string? email, decimal? amount)
    {
        var snapshot = new PaymentStatusSnapshot(
            sourceId,
            string.IsNullOrWhiteSpace(status) ? "unknown" : status,
            DateTimeOffset.UtcNow,
            email,
            amount);

        _statuses.AddOrUpdate(sourceId, snapshot, (_, _) => snapshot);
    }

    public bool TryGet(string sourceId, out PaymentStatusSnapshot? snapshot)
    {
        return _statuses.TryGetValue(sourceId, out snapshot);
    }
}
