// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CompleteBooking/CompleteBookingResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Booking.CompleteBooking
{
    public sealed record CompleteBookingResult(
        Guid BookingId,
        string Status,
        DateTime CompletedAt);
}
