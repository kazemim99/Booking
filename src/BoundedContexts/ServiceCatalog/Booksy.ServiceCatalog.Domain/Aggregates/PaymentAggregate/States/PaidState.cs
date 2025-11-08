// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/PaymentAggregate/States/PaidState.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Events;

namespace Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.States
{
    /// <summary>
    /// State: Payment has been completed (fully paid)
    /// Valid transitions: Refund
    /// </summary>
    public sealed class PaidState : PaymentStateBase
    {
        public override PaymentStatus Status => PaymentStatus.Paid;

        public override void Refund(Payment payment, Money refundAmount, string refundId, RefundReason reason, string? notes = null)
        {
            if (refundAmount.Amount <= 0)
                throw new ArgumentException("Refund amount must be positive", nameof(refundAmount));

            if (refundAmount.Currency != payment.Amount.Currency)
                throw new ArgumentException("Refund currency mismatch");

            var newRefundedAmount = payment.RefundedAmount.Add(refundAmount);
            if (newRefundedAmount.Amount > payment.PaidAmount.Amount)
                throw new InvalidOperationException("Refunded amount cannot exceed paid amount");

            payment.SetRefundedAmount(newRefundedAmount);
            payment.SetRefundedAt(DateTime.UtcNow);

            var metadata = new Dictionary<string, object>(payment.Metadata)
            {
                ["RefundReason"] = reason.ToString(),
                ["RefundNotes"] = notes ?? string.Empty
            };

            var transaction = Transaction.CreateRefund(
                refundAmount,
                refundId,
                $"Refund for Payment {payment.Id}",
                metadata);

            payment.AddTransaction(transaction);

            // Transition based on refunded amount
            if (newRefundedAmount.Amount >= payment.PaidAmount.Amount)
            {
                payment.TransitionToState(new RefundedState());
            }
            else
            {
                payment.TransitionToState(new PartiallyRefundedState());
            }

            payment.RaiseDomainEvent(new PaymentRefundedEvent(
                payment.Id,
                payment.BookingId,
                payment.CustomerId,
                payment.ProviderId,
                refundAmount,
                reason,
                payment.RefundedAt!.Value));
        }
    }
}
