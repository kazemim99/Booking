// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Booking/GetCustomerBookings/CustomerBookingDto.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Queries.Booking.GetCustomerBookings
{
    /// <summary>
    /// DTO for customer booking list items with enriched data
    /// </summary>
    public sealed record CustomerBookingDto(
        Guid BookingId,
        Guid CustomerId,
        Guid ProviderId,
        Guid ServiceId,
        Guid? StaffId,
        string ServiceName,
        string ProviderName,
        DateTime StartTime,
        DateTime EndTime,
        int DurationMinutes,
        string Status,
        decimal TotalPrice,
        string Currency,
        string PaymentStatus,
        DateTime RequestedAt,
        DateTime? ConfirmedAt,
        string? CustomerNotes,
        // Who does it: the assigned member's name (real name, else the salon's name for them, else the salon's
        // own name — never a placeholder or a phone); null when the booking is held by the salon itself.
        string? StaffName = null,
        // Why the customer cannot move this booking right now, in Persian; null when they can. Said up front so the
        // apps disable «تغییر زمان» with the reason instead of failing after a slot is chosen.
        string? RescheduleBlockedReason = null,
        // Where the booking's review stands (openspec/changes/_inline/customer-reviews-and-nahal-seed): whether the
        // customer may write one now; why not yet, in Persian (the salon has not marked the visit done); or the one
        // they wrote and its moderation state (Pending | Published | Rejected | Hidden).
        bool CanReview = false,
        string? ReviewBlockedReason = null,
        Guid? ReviewId = null,
        string? ReviewStatus = null,
        // The discount the booking received (openspec/changes/add-discounts-and-campaigns). TotalPrice is already net of
        // it; Subtotal is the services' list prices.
        decimal Subtotal = 0m,
        decimal DiscountAmount = 0m,
        string? DiscountTitle = null,
        string? DiscountCode = null);
}
