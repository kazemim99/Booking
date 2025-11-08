// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/PaymentAggregate/States/FailedState.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Events;

namespace Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.States
{
    /// <summary>
    /// State: Payment has failed
    /// Valid transitions: ProcessPayment (retry)
    /// </summary>
    public sealed class FailedState : PaymentStateBase
    {
        public override PaymentStatus Status => PaymentStatus.Failed;

        public override void ProcessPayment(Payment payment, string paymentIntentId, string? paymentMethodId = null)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId))
                throw new ArgumentException("Payment intent ID is required", nameof(paymentIntentId));

            // Retry payment after failure
            payment.SetPaymentIntentId(paymentIntentId);
            payment.SetPaymentMethodId(paymentMethodId);
            payment.SetPaidAmount(payment.Amount);
            payment.SetCapturedAt(DateTime.UtcNow);
            payment.SetFailureReason(null); // Clear previous failure reason

            var transaction = Transaction.CreateCharge(
                payment.Amount,
                paymentIntentId,
                $"Retry charge for Payment {payment.Id}",
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
            payment.SetFailureReason(null); // Clear previous failure reason

            var transaction = Transaction.CreateCharge(
                amount,
                transactionId,
                $"Retry partial payment for Payment {payment.Id}",
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
    }
}
