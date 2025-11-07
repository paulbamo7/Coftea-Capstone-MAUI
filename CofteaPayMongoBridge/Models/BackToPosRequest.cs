namespace CofteaPayMongoBridge.Models;

public class BackToPosRequest
{
    public string? SourceId { get; set; }
    public string? Status { get; set; }
    public string? CustomerEmail { get; set; }
    public decimal? Amount { get; set; }
}
