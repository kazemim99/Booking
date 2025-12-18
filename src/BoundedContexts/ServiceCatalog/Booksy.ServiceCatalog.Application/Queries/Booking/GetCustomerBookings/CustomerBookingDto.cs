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
        string? StaffName,
        DateTime StartTime,
        DateTime EndTime,
        int DurationMinutes,
        string Status,
        decimal TotalPrice,
        string Currency,
        string PaymentStatus,
        DateTime RequestedAt,
        DateTime? ConfirmedAt,
        string? CustomerNotes);
}
