// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/AssignStaff/AssignStaffToBookingCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.AssignStaff
{
    public sealed record AssignStaffToBookingCommand(
        Guid BookingId,
        Guid StaffId,
        Guid? IdempotencyKey = null) : ICommand<AssignStaffToBookingResult>;
}
