// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/AssignStaff/AssignStaffToBookingResult.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.AssignStaff
{
    public sealed record AssignStaffToBookingResult(
        Guid BookingId,
        Guid PreviousStaffId,
        Guid NewStaffId,
        DateTime AssignedAt);
}
