// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/CancelBooking/CancelBookingCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Authorization;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.CancelBooking
{
    /// <summary>
    /// Cancels a booking. <see cref="ActingUserId"/> is populated server-side from the
    /// JWT and enforced by <c>AuthorizationBehavior</c> (only the owning customer,
    /// owning provider, or an admin may cancel). <see cref="ByProvider"/> is a legacy
    /// flag defaulted false and no longer set by the API — provider-initiation is being
    /// moved to a server-side derivation (see C1 follow-up).
    /// </summary>
    /// <remarks>
    /// Implements <see cref="Core.Application.Abstractions.CQRS.INonTransactionalCommand"/> because
    /// cancelling past the free window issues a <b>gateway refund</b>
    /// (<c>CancelBookingCommandHandler</c> → <c>IPaymentGateway.RefundPaymentAsync</c>). Inside the
    /// ambient retried transaction, a transient DB fault would re-run the handler and refund the
    /// customer twice. The handler already persists its own single unit via
    /// <c>CommitAndPublishEventsAsync</c>, which is what makes opting out safe here.
    /// Enforced by <c>MoneyCommandTransactionTests</c>.
    /// </remarks>
    public sealed record CancelBookingCommand(
        Guid BookingId,
        string Reason,
        Guid ActingUserId,
        bool ByProvider = false,
        Guid? IdempotencyKey = null)
        : ICommand<CancelBookingResult>,
          IRequireBookingOwnership,
          Core.Application.Abstractions.CQRS.INonTransactionalCommand;
}
