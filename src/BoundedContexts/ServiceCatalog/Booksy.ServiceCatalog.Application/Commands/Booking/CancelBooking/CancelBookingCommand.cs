// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CancelBooking/CancelBookingCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Authorization;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.CancelBooking
{
    /// <summary>
    /// Cancels a booking. <see cref="ActingUserId"/> is populated server-side from the
    /// JWT and enforced by <c>AuthorizationBehavior</c> (only the owning customer,
    /// owning provider, or an admin may cancel). <see cref="ByProvider"/> is a legacy
    /// flag defaulted false and no longer set by the API — provider-initiation is being
    /// moved to a server-side derivation (see C1 follow-up).
    /// </summary>
    public sealed record CancelBookingCommand(
        Guid BookingId,
        string Reason,
        Guid ActingUserId,
        bool ByProvider = false,
        Guid? IdempotencyKey = null) : ICommand<CancelBookingResult>, IRequireBookingOwnership;
}
