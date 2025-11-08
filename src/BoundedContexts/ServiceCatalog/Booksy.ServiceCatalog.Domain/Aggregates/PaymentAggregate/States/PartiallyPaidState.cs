// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/PaymentAggregate/States/PartiallyPaidState.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Events;

namespace Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.States
{
    /// <summary>
    /// State: Partial payment has been made
    /// Valid transitions: ProcessPartialPayment (more payments), MarkAsFailed, Refund
    /// </summary>
    public sealed class PartiallyPaidState : PaymentStateBase
    {
        public override PaymentStatus Status => PaymentStatus.PartiallyPaid;

        public override void ProcessPartialPayment(Payment payment, Money amount, string transactionId)
        {
            if (amount.Amount <= 0)
                throw new ArgumentException("Payment amount must be positive", nameof(amount));

            if (amount.Currency != payment.Amount.Currency)
                throw new ArgumentException("Payment currency mismatch");

            var newPaidAmount = payment.PaidAmount.Add(amount);
            if (newPaidAmount.Amount > payment.Amount.Amount)
                throw new InvalidOperationException("Paid amount cannot exceed total amount");

            payment.SetPaidAmount(newPaidAmount);

            var transaction = Transaction.CreateCharge(
                amount,
                transactionId,
                $"Partial payment for Payment {payment.Id}",
                payment.Metadata);

            payment.AddTransaction(transaction);

            // Transition based on amount paid
            if (newPaidAmount.Amount >= payment.Amount.Amount)
            {
                payment.TransitionToState(new PaidState());
                payment.SetCapturedAt(DateTime.UtcNow);
            }
            // Else remain in PartiallyPaid state

            payment.RaiseDomainEvent(new PaymentProcessedEvent(
                payment.Id,
                payment.BookingId,
                payment.CustomerId,
                payment.ProviderId,
                amount,
                DateTime.UtcNow));
        }

        public override void MarkAsFailed(Payment payment, string reason)
        {
            payment.SetFailureReason(reason);
            payment.SetFailedAt(DateTime.UtcNow);

            // Transition to Failed state
            payment.TransitionToState(new FailedState());

            payment.RaiseDomainEvent(new PaymentFailedEvent(
                payment.Id,
                payment.BookingId,
                payment.CustomerId,
                reason,
                payment.FailedAt!.Value));
        }

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
