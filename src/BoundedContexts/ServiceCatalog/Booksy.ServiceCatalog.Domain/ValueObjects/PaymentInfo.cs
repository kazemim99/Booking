// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/PaymentInfo.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Represents payment information for a booking
    /// </summary>
    public sealed class PaymentInfo : ValueObject
    {
        public PaymentInfo() { }

        public Money TotalAmount { get; }
        public Money DepositAmount { get; }
        public Money PaidAmount { get; }
        public Money RefundedAmount { get; }
        public PaymentStatus Status { get; private set; }
        public string? PaymentIntentId { get; private set; }
        public string? DepositPaymentIntentId { get; private set; }
        public string? RefundId { get; private set; }
        public DateTime? PaidAt { get; private set; }
        public DateTime? RefundedAt { get; private set; }

        private PaymentInfo(
            Money totalAmount,
            Money depositAmount,
            Money paidAmount,
            Money refundedAmount,
            PaymentStatus status,
            string? paymentIntentId = null,
            string? depositPaymentIntentId = null,
            string? refundId = null,
            DateTime? paidAt = null,
            DateTime? refundedAt = null)
        {
            if (totalAmount.Amount <= 0)
                throw new ArgumentException("Total amount must be positive", nameof(totalAmount));

            if (depositAmount.Amount < 0)
                throw new ArgumentException("Deposit amount cannot be negative", nameof(depositAmount));

            if (paidAmount.Amount < 0)
                throw new ArgumentException("Paid amount cannot be negative", nameof(paidAmount));

            if (refundedAmount.Amount < 0)
                throw new ArgumentException("Refunded amount cannot be negative", nameof(refundedAmount));

            if (depositAmount.Amount > totalAmount.Amount)
                throw new ArgumentException("Deposit amount cannot exceed total amount", nameof(depositAmount));

            if (totalAmount.Currency != depositAmount.Currency ||
                totalAmount.Currency != paidAmount.Currency ||
                totalAmount.Currency != refundedAmount.Currency)
                throw new ArgumentException("All amounts must have the same currency");

            TotalAmount = totalAmount;
            DepositAmount = depositAmount;
            PaidAmount = paidAmount;
            RefundedAmount = refundedAmount;
            Status = status;
            PaymentIntentId = paymentIntentId;
            DepositPaymentIntentId = depositPaymentIntentId;
            RefundId = refundId;
            PaidAt = paidAt;
            RefundedAt = refundedAt;
        }

        public static PaymentInfo Create(Money totalAmount, Money depositAmount)
        {
            return new PaymentInfo(
                totalAmount,
                depositAmount,
                Money.Create(0, totalAmount.Currency),
                Money.Create(0, totalAmount.Currency),
                PaymentStatus.Pending);
        }

        public static PaymentInfo CreateWithNoDeposit(Money totalAmount)
        {
            return new PaymentInfo(
                totalAmount,
                Money.Create(0, totalAmount.Currency),
                Money.Create(0, totalAmount.Currency),
                Money.Create(0, totalAmount.Currency),
                PaymentStatus.Pending);
        }

        /// <summary>
        /// Records a deposit payment
        /// </summary>
        public PaymentInfo WithDepositPaid(string paymentIntentId)
        {
            if (DepositAmount.Amount == 0)
                throw new InvalidOperationException("No deposit required for this booking");

            var newPaidAmount = PaidAmount.Add(DepositAmount);
            var newStatus = newPaidAmount.Amount >= TotalAmount.Amount
                ? PaymentStatus.Paid
                : PaymentStatus.PartiallyPaid;

            return new PaymentInfo(
                TotalAmount,
                DepositAmount,
                newPaidAmount,
                RefundedAmount,
                newStatus,
                PaymentIntentId,
                paymentIntentId,
                RefundId,
                DateTime.UtcNow,
                RefundedAt);
        }

        /// <summary>
        /// Records a full payment
        /// </summary>
        public PaymentInfo WithFullPayment(string paymentIntentId)
        {
            return new PaymentInfo(
                TotalAmount,
                DepositAmount,
                TotalAmount,
                RefundedAmount,
                PaymentStatus.Paid,
                paymentIntentId,
                DepositPaymentIntentId,
                RefundId,
                DateTime.UtcNow,
                RefundedAt);
        }

        /// <summary>
        /// Records a partial payment
        /// </summary>
        public PaymentInfo WithPartialPayment(Money amount, string paymentIntentId)
        {
            var newPaidAmount = PaidAmount.Add(amount);
            if (newPaidAmount.Amount > TotalAmount.Amount)
                throw new InvalidOperationException("Paid amount cannot exceed total amount");

            var newStatus = newPaidAmount.Amount >= TotalAmount.Amount
                ? PaymentStatus.Paid
                : PaymentStatus.PartiallyPaid;

            return new PaymentInfo(
                TotalAmount,
                DepositAmount,
                newPaidAmount,
                RefundedAmount,
                newStatus,
                paymentIntentId,
                DepositPaymentIntentId,
                RefundId,
                DateTime.UtcNow,
                RefundedAt);
        }

        /// <summary>
        /// Records a refund
        /// </summary>
        public PaymentInfo WithRefund(Money refundAmount, string refundId)
        {
            var newRefundedAmount = RefundedAmount.Add(refundAmount);
            if (newRefundedAmount.Amount > PaidAmount.Amount)
                throw new InvalidOperationException("Refunded amount cannot exceed paid amount");

            var newStatus = newRefundedAmount.Amount >= PaidAmount.Amount
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;

            return new PaymentInfo(
                TotalAmount,
                DepositAmount,
                PaidAmount,
                newRefundedAmount,
                newStatus,
                PaymentIntentId,
                DepositPaymentIntentId,
                refundId,
                PaidAt,
                DateTime.UtcNow);
        }

        /// <summary>
        /// Marks payment as failed
        /// </summary>
        public PaymentInfo MarkAsFailed()
        {
            return new PaymentInfo(
                TotalAmount,
                DepositAmount,
                PaidAmount,
                RefundedAmount,
                PaymentStatus.Failed,
                PaymentIntentId,
                DepositPaymentIntentId,
                RefundId,
                PaidAt,
                RefundedAt);
        }

        /// <summary>
        /// Calculates the remaining amount to be paid
        /// </summary>
        public Money GetRemainingAmount()
        {
            return TotalAmount.Subtract(PaidAmount);
        }

        /// <summary>
        /// Checks if deposit has been paid
        /// </summary>
        public bool IsDepositPaid()
        {
            return PaidAmount.Amount >= DepositAmount.Amount;
        }

        /// <summary>
        /// Checks if full payment has been made
        /// </summary>
        public bool IsFullyPaid()
        {
            return PaidAmount.Amount >= TotalAmount.Amount;
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return TotalAmount;
            yield return DepositAmount;
            yield return PaidAmount;
            yield return RefundedAmount;
            yield return Status;
            yield return PaymentIntentId;
            yield return DepositPaymentIntentId;
            yield return RefundId;
            yield return PaidAt;
            yield return RefundedAt;
        }
    }
}
