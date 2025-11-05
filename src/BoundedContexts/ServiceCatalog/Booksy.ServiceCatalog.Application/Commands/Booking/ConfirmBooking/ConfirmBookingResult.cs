// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/ConfirmBooking/ConfirmBookingResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Booking.ConfirmBooking
{
    public sealed record ConfirmBookingResult(
        Guid BookingId,
        string Status,
        DateTime ConfirmedAt);
}
