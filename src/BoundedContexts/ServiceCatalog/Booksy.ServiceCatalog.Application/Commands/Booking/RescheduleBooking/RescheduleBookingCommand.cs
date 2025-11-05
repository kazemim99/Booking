// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/RescheduleBooking/RescheduleBookingCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.RescheduleBooking
{
    public sealed record RescheduleBookingCommand(
        Guid BookingId,
        DateTime NewStartTime,
        Guid? NewStaffId = null,
        string? Reason = null,
        Guid? IdempotencyKey = null) : ICommand<RescheduleBookingResult>;
}
