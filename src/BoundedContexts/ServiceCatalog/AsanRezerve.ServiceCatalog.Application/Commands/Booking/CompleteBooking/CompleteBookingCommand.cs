// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/CompleteBooking/CompleteBookingCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.CompleteBooking
{
    public sealed record CompleteBookingCommand(
        Guid BookingId,
        string? StaffNotes = null,
        Guid? IdempotencyKey = null) : ICommand<CompleteBookingResult>;
}
