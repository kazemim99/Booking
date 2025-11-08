// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/PaymentAggregate/States/IPaymentState.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.States
{
    /// <summary>
    /// Interface for Payment State pattern - defines state-specific behavior
    /// </summary>
    public interface IPaymentState
    {
        /// <summary>
        /// Gets the payment status this state represents
        /// </summary>
        PaymentStatus Status { get; }

        /// <summary>
        /// Process payment (charge or capture)
        /// </summary>
        void ProcessPayment(Payment payment, string paymentIntentId, string? paymentMethodId = null);

        /// <summary>
        /// Process partial payment
        /// </summary>
        void ProcessPartialPayment(Payment payment, Money amount, string transactionId);

        /// <summary>
        /// Mark payment as failed
        /// </summary>
        void MarkAsFailed(Payment payment, string reason);

        /// <summary>
        /// Refund the payment (full or partial)
        /// </summary>
        void Refund(Payment payment, Money refundAmount, string refundId, RefundReason reason, string? notes = null);

        /// <summary>
        /// Verify payment (for ZarinPal and similar gateways)
        /// </summary>
        void VerifyPayment(Payment payment, string refNumber, string? cardPan = null, decimal? fee = null);
    }
}
