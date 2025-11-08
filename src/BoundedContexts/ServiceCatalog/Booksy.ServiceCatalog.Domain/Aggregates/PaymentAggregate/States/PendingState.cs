// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/PaymentAggregate/States/PendingState.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Events;

namespace Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.States
{
    /// <summary>
    /// State: Payment is pending (not yet processed)
    /// Valid transitions: ProcessPayment, ProcessPartialPayment, MarkAsFailed, VerifyPayment
    /// </summary>
    public sealed class PendingState : PaymentStateBase
    {
        public override PaymentStatus Status => PaymentStatus.Pending;

        public override void ProcessPayment(Payment payment, string paymentIntentId, string? paymentMethodId = null)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId))
                throw new ArgumentException("Payment intent ID is required", nameof(paymentIntentId));

            payment.SetPaymentIntentId(paymentIntentId);
            payment.SetPaymentMethodId(paymentMethodId);
            payment.SetPaidAmount(payment.Amount);
            payment.SetCapturedAt(DateTime.UtcNow);

            var transaction = Transaction.CreateCharge(
                payment.Amount,
                paymentIntentId,
                $"Charge for Payment {payment.Id}",
                payment.Metadata);

            payment.AddTransaction(transaction);

            // Transition to Paid state
            payment.TransitionToState(new PaidState());

            payment.RaiseDomainEvent(new PaymentProcessedEvent(
                payment.Id,
                payment.BookingId,
                payment.CustomerId,
                payment.ProviderId,
                payment.Amount,
                payment.CapturedAt!.Value));
        }

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
            else
            {
                payment.TransitionToState(new PartiallyPaidState());
            }

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

        public override void VerifyPayment(Payment payment, string refNumber, string? cardPan = null, decimal? fee = null)
        {
            if (string.IsNullOrWhiteSpace(refNumber))
                throw new ArgumentException("Reference number is required", nameof(refNumber));

            payment.SetRefNumber(refNumber);
            payment.SetCardPan(cardPan);
            if (fee.HasValue)
            {
                payment.SetFee(Money.Create(fee.Value, payment.Amount.Currency));
            }

            payment.SetPaidAmount(payment.Amount);
            payment.SetCapturedAt(DateTime.UtcNow);

            var metadata = new Dictionary<string, object>
            {
                ["RefNumber"] = refNumber
            };

            if (cardPan != null)
                metadata["CardPan"] = cardPan;
            if (fee.HasValue)
                metadata["Fee"] = fee.Value;

            var transaction = Transaction.CreateVerification(
                payment.Amount,
                refNumber,
                $"Payment verified for Payment {payment.Id}",
                metadata);

            payment.AddTransaction(transaction);

            // Transition to Paid state
            payment.TransitionToState(new PaidState());

            payment.RaiseDomainEvent(new PaymentVerifiedEvent(
                payment.Id,
                payment.BookingId,
                payment.CustomerId,
                payment.ProviderId,
                payment.Amount,
                refNumber,
                cardPan,
                payment.CapturedAt!.Value));
        }
    }
}
