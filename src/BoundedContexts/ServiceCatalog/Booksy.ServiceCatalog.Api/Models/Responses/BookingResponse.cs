namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Response model for booking summary
/// </summary>
public class BookingResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid? StaffProviderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Alias for StaffProviderId (backward compatibility)
    /// </summary>
    public Guid? StaffId { get => StaffProviderId; set => StaffProviderId = value; }
}
