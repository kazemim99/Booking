// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/RescheduleBooking/RescheduleBookingCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Authorization;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.RescheduleBooking
{
    /// <summary>
    /// Reschedules a booking. <see cref="ActingUserId"/> is populated server-side from
    /// the JWT and enforced by <c>AuthorizationBehavior</c> (owner/provider/admin only).
    /// </summary>
    public sealed record RescheduleBookingCommand(
        Guid BookingId,
        DateTime NewStartTime,
        Guid ActingUserId,
        Guid? NewStaffId = null,
        string? Reason = null,
        Guid? IdempotencyKey = null) : ICommand<RescheduleBookingResult>, IRequireBookingOwnership;
}
