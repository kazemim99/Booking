// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/RescheduleBooking/RescheduleBookingResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Booking.RescheduleBooking
{
    public sealed record RescheduleBookingResult(
        Guid OldBookingId,
        Guid NewBookingId,
        DateTime NewStartTime,
        DateTime NewEndTime,
        string Status);
}
