// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/AddNotes/AddBookingNotesCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.AddNotes
{
    public sealed record AddBookingNotesCommand(
        Guid BookingId,
        string Notes,
        bool IsStaffNote,
        string AddedBy,
        Guid? IdempotencyKey = null) : ICommand<AddBookingNotesResult>;
}
