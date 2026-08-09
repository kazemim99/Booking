// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/RefundPayment/RefundPaymentCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions;
using Booksy.Core.Application.Authorization;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.RefundPayment
{
    /// <summary>
    /// Refunds a payment. <see cref="ActingUserId"/> is populated server-side from the
    /// JWT and enforced by <c>AuthorizationBehavior</c> (only the paying customer, the
    /// receiving provider, or an admin may refund) — closing the refund IDOR.
    /// </summary>
    public sealed record RefundPaymentCommand(
        Guid PaymentId,
        decimal RefundAmount,
        RefundReason Reason,
        Guid ActingUserId,
        string? Notes = null,
        Guid? IdempotencyKey = null)
        : ICommand<RefundPaymentResult>, IRequirePaymentOwnership,
          Core.Application.Abstractions.CQRS.INonTransactionalCommand, // gateway refund must not run in a retried tx
          Core.Application.Abstractions.CQRS.IRequireIdempotency;      // at-most-once per idempotency key
}
