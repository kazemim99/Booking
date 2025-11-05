// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CancelBooking/CancelBookingResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Booking.CancelBooking
{
    public sealed record CancelBookingResult(
        Guid BookingId,
        string Status,
        bool RefundIssued,
        decimal RefundAmount,
        DateTime CancelledAt);
}
