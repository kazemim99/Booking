namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Detailed response model for booking with full information
/// </summary>
public class BookingDetailsResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid StaffProviderId { get; set; }

    // Service information
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;

    // Provider information
    public string ProviderBusinessName { get; set; } = string.Empty;
    public string ProviderCity { get; set; } = string.Empty;

    // Staff information
    public string StaffName { get; set; } = string.Empty;

    // Time slot
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }

    // Status
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;

    // Payment information
    public PaymentInfoResponse PaymentInfo { get; set; } = new();

    // Notes
    public string? CustomerNotes { get; set; }
    public string? StaffNotes { get; set; }

    // Booking policy
    public BookingPolicyResponse Policy { get; set; } = new();

    // History
    public List<BookingHistoryEntryResponse> History { get; set; } = new();

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
