namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Booking history entry response model
/// </summary>
public class BookingHistoryEntryResponse
{
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid PerformedBy { get; set; }
    public string? Notes { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
