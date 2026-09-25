namespace AsanRezerve.ServiceCatalog.Api.Models.Responses;

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

    /// <summary>
    /// Who does it: the assigned member's name — real name, else the salon's name for them, else the salon's own
    /// name; never a placeholder or a phone. Null (left out of the JSON) when the salon itself holds the booking.
    /// </summary>
    public string? StaffName { get; set; }

    /// <summary>Why the customer cannot move this booking now (Persian); null when they can.</summary>
    public string? RescheduleBlockedReason { get; set; }

    /// <summary>The person this booking is for may review it now. False for anyone else who can see it.</summary>
    public bool CanReview { get; set; }

    /// <summary>Why they cannot review it yet (Persian) — the salon has not marked the visit done; else null.</summary>
    public string? ReviewBlockedReason { get; set; }

    /// <summary>The review they wrote for it, if any.</summary>
    public Guid? ReviewId { get; set; }

    /// <summary>That review's moderation state: Pending | Published | Rejected | Hidden.</summary>
    public string? ReviewStatus { get; set; }

    // Service information
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;

    // Provider information
    public string ProviderBusinessName { get; set; } = string.Empty;
    public string ProviderCity { get; set; } = string.Empty;



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
