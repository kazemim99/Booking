// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/AddNotes/AddBookingNotesResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Booking.AddNotes
{
    public sealed record AddBookingNotesResult(
        Guid BookingId,
        string Notes,
        bool IsStaffNote,
        DateTime AddedAt);
}
