// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CompleteBooking/CompleteBookingCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.CompleteBooking
{
    public sealed record CompleteBookingCommand(
        Guid BookingId,
        string? StaffNotes = null,
        Guid? IdempotencyKey = null) : ICommand<CompleteBookingResult>;
}
