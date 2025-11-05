// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/RefundPolicy.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Represents refund policy rules for a service or booking
    /// </summary>
    public sealed class RefundPolicy : ValueObject
    {
        public bool AllowRefunds { get; }
        public int FullRefundWindowHours { get; }
        public int PartialRefundWindowHours { get; }
        public decimal PartialRefundPercentage { get; }
        public decimal CancellationFeePercentage { get; }
        public bool RefundProcessingFees { get; }

        private RefundPolicy(
            bool allowRefunds,
            int fullRefundWindowHours,
            int partialRefundWindowHours,
            decimal partialRefundPercentage,
            decimal cancellationFeePercentage,
            bool refundProcessingFees)
        {
            if (fullRefundWindowHours < 0)
                throw new ArgumentException("Full refund window cannot be negative", nameof(fullRefundWindowHours));

            if (partialRefundWindowHours < 0)
                throw new ArgumentException("Partial refund window cannot be negative", nameof(partialRefundWindowHours));

            if (partialRefundPercentage < 0 || partialRefundPercentage > 100)
                throw new ArgumentException("Partial refund percentage must be between 0 and 100", nameof(partialRefundPercentage));

            if (cancellationFeePercentage < 0 || cancellationFeePercentage > 100)
                throw new ArgumentException("Cancellation fee percentage must be between 0 and 100", nameof(cancellationFeePercentage));

            AllowRefunds = allowRefunds;
            FullRefundWindowHours = fullRefundWindowHours;
            PartialRefundWindowHours = partialRefundWindowHours;
            PartialRefundPercentage = partialRefundPercentage;
            CancellationFeePercentage = cancellationFeePercentage;
            RefundProcessingFees = refundProcessingFees;
        }

        public static RefundPolicy Create(
            bool allowRefunds,
            int fullRefundWindowHours,
            int partialRefundWindowHours,
            decimal partialRefundPercentage,
            decimal cancellationFeePercentage,
            bool refundProcessingFees)
        {
            return new RefundPolicy(
                allowRefunds,
                fullRefundWindowHours,
                partialRefundWindowHours,
                partialRefundPercentage,
                cancellationFeePercentage,
                refundProcessingFees);
        }

        /// <summary>
        /// Flexible refund policy - full refund up to 24 hours before
        /// </summary>
        public static RefundPolicy Flexible =>
            new(allowRefunds: true,
                fullRefundWindowHours: 24,
                partialRefundWindowHours: 48,
                partialRefundPercentage: 50,
                cancellationFeePercentage: 10,
                refundProcessingFees: true);

        /// <summary>
        /// Moderate refund policy - full refund up to 48 hours before
        /// </summary>
        public static RefundPolicy Moderate =>
            new(allowRefunds: true,
                fullRefundWindowHours: 48,
                partialRefundWindowHours: 72,
                partialRefundPercentage: 50,
                cancellationFeePercentage: 20,
                refundProcessingFees: false);

        /// <summary>
        /// Strict refund policy - full refund up to 7 days before
        /// </summary>
        public static RefundPolicy Strict =>
            new(allowRefunds: true,
                fullRefundWindowHours: 168, // 7 days
                partialRefundWindowHours: 336, // 14 days
                partialRefundPercentage: 30,
                cancellationFeePercentage: 30,
                refundProcessingFees: false);

        /// <summary>
        /// No refunds policy
        /// </summary>
        public static RefundPolicy NoRefunds =>
            new(allowRefunds: false,
                fullRefundWindowHours: 0,
                partialRefundWindowHours: 0,
                partialRefundPercentage: 0,
                cancellationFeePercentage: 100,
                refundProcessingFees: false);

        /// <summary>
        /// Calculates refund amount based on policy and time until booking
        /// </summary>
        public Money CalculateRefundAmount(Money paidAmount, DateTime bookingTime, DateTime currentTime)
        {
            if (!AllowRefunds)
                return Money.Zero(paidAmount.Currency);

            var hoursUntilBooking = (bookingTime - currentTime).TotalHours;

            // Full refund window
            if (hoursUntilBooking >= FullRefundWindowHours)
            {
                return paidAmount;
            }

            // Partial refund window
            if (hoursUntilBooking >= PartialRefundWindowHours)
            {
                return paidAmount.Multiply(PartialRefundPercentage / 100m);
            }

            // Outside refund window - apply cancellation fee
            var cancellationFee = paidAmount.Multiply(CancellationFeePercentage / 100m);
            return paidAmount.Subtract(cancellationFee);
        }

        /// <summary>
        /// Checks if full refund is available
        /// </summary>
        public bool CanGetFullRefund(DateTime bookingTime, DateTime currentTime)
        {
            if (!AllowRefunds)
                return false;

            var hoursUntilBooking = (bookingTime - currentTime).TotalHours;
            return hoursUntilBooking >= FullRefundWindowHours;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return AllowRefunds;
            yield return FullRefundWindowHours;
            yield return PartialRefundWindowHours;
            yield return PartialRefundPercentage;
            yield return CancellationFeePercentage;
            yield return RefundProcessingFees;
        }
    }
}
