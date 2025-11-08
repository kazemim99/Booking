// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/PaymentAggregate/States/PaymentStateBase.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Exceptions;

namespace Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.States
{
    /// <summary>
    /// Base class for payment states - provides default implementations that throw exceptions
    /// </summary>
    public abstract class PaymentStateBase : IPaymentState
    {
        public abstract PaymentStatus Status { get; }

        public virtual void ProcessPayment(Payment payment, string paymentIntentId, string? paymentMethodId = null)
        {
            throw new InvalidPaymentStateTransitionException(Status, "ProcessPayment");
        }

        public virtual void ProcessPartialPayment(Payment payment, Money amount, string transactionId)
        {
            throw new InvalidPaymentStateTransitionException(Status, "ProcessPartialPayment");
        }

        public virtual void MarkAsFailed(Payment payment, string reason)
        {
            throw new InvalidPaymentStateTransitionException(Status, "MarkAsFailed");
        }

        public virtual void Refund(Payment payment, Money refundAmount, string refundId, RefundReason reason, string? notes = null)
        {
            throw new InvalidPaymentStateTransitionException(Status, "Refund");
        }

        public virtual void VerifyPayment(Payment payment, string refNumber, string? cardPan = null, decimal? fee = null)
        {
            throw new InvalidPaymentStateTransitionException(Status, "VerifyPayment");
        }
    }
}
