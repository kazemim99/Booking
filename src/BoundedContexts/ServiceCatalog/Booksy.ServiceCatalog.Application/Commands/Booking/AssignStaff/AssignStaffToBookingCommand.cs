// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/AssignStaff/AssignStaffToBookingCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.AssignStaff
{
    public sealed record AssignStaffToBookingCommand(
        Guid BookingId,
        Guid StaffId,
        Guid? IdempotencyKey = null) : ICommand<AssignStaffToBookingResult>;
}
