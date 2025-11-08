// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/SearchBookings/SearchBookingsResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Queries.Booking.SearchBookings
{
    public sealed record SearchBookingsResult(
        IReadOnlyList<BookingSearchDto> Bookings,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages);

    public sealed record BookingSearchDto(
        Guid BookingId,
        Guid CustomerId,
        Guid ProviderId,
        Guid ServiceId,
        Guid StaffId,
        DateTime StartTime,
        DateTime EndTime,
        string Status,
        decimal TotalPrice,
        string Currency,
        DateTime RequestedAt,
        DateTime? ConfirmedAt,
        string? CustomerNotes);
}
