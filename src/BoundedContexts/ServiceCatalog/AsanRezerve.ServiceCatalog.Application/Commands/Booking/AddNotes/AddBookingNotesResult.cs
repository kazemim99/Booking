// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/AddNotes/AddBookingNotesResult.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.AddNotes
{
    public sealed record AddBookingNotesResult(
        Guid BookingId,
        string Notes,
        bool IsStaffNote,
        DateTime AddedAt);
}
