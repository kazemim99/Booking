// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/CompleteBooking/CompleteBookingResult.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.CompleteBooking
{
    public sealed record CompleteBookingResult(
        Guid BookingId,
        string Status,
        DateTime CompletedAt);
}
