// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetCustomerBookings/CustomerBookingDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetCustomerBookings
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
        string? StaffName = null);
}
