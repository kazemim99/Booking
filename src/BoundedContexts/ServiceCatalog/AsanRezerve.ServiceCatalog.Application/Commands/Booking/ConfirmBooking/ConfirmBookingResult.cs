// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/ConfirmBooking/ConfirmBookingResult.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.ConfirmBooking
{
    public sealed record ConfirmBookingResult(
        Guid BookingId,
        string Status,
        DateTime ConfirmedAt);
}
