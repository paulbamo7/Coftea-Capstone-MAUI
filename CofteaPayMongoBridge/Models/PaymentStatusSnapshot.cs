namespace CofteaPayMongoBridge.Models;

public record PaymentStatusSnapshot(
    string SourceId,
    string Status,
    DateTimeOffset UpdatedAt,
    string? CustomerEmail,
    decimal? Amount);
