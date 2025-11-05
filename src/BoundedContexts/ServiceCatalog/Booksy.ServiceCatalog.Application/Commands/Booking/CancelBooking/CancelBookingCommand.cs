// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CancelBooking/CancelBookingCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.CancelBooking
{
    public sealed record CancelBookingCommand(
        Guid BookingId,
        string Reason,
        bool ByProvider = false,
        Guid? IdempotencyKey = null) : ICommand<CancelBookingResult>;
}
