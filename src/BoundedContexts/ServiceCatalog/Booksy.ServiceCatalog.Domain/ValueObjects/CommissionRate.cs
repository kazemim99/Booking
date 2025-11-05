// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/CommissionRate.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Represents the platform commission rate for provider earnings
    /// </summary>
    public sealed class CommissionRate : ValueObject
    {
        public decimal Percentage { get; }
        public Money? FixedAmount { get; }
        public CommissionType Type { get; }

        private CommissionRate(decimal percentage, Money? fixedAmount, CommissionType type)
        {
            if (percentage < 0 || percentage > 100)
                throw new ArgumentException("Commission percentage must be between 0 and 100", nameof(percentage));

            Percentage = percentage;
            FixedAmount = fixedAmount;
            Type = type;
        }

        public static CommissionRate CreatePercentage(decimal percentage)
        {
            return new CommissionRate(percentage, null, CommissionType.Percentage);
        }

        public static CommissionRate CreateFixed(Money amount)
        {
            if (amount.Amount <= 0)
                throw new ArgumentException("Fixed commission must be positive", nameof(amount));

            return new CommissionRate(0, amount, CommissionType.Fixed);
        }

        public static CommissionRate CreateMixed(decimal percentage, Money fixedAmount)
        {
            if (fixedAmount.Amount <= 0)
                throw new ArgumentException("Fixed commission must be positive", nameof(fixedAmount));

            return new CommissionRate(percentage, fixedAmount, CommissionType.Mixed);
        }

        /// <summary>
        /// Default platform commission rate (15%)
        /// </summary>
        public static CommissionRate Default => CreatePercentage(15m);

        /// <summary>
        /// Premium tier commission rate (10%)
        /// </summary>
        public static CommissionRate Premium => CreatePercentage(10m);

        /// <summary>
        /// Enterprise tier commission rate (5%)
        /// </summary>
        public static CommissionRate Enterprise => CreatePercentage(5m);

        /// <summary>
        /// Calculates commission amount from gross amount
        /// </summary>
        public Money CalculateCommission(Money grossAmount)
        {
            Money commission = Money.Zero(grossAmount.Currency);

            if (Type == CommissionType.Percentage || Type == CommissionType.Mixed)
            {
                commission = commission.Add(grossAmount.Multiply(Percentage / 100m));
            }

            if (Type == CommissionType.Fixed || Type == CommissionType.Mixed)
            {
                if (FixedAmount == null)
                    throw new InvalidOperationException("Fixed amount not set");

                if (FixedAmount.Currency != grossAmount.Currency)
                    throw new InvalidOperationException("Currency mismatch");

                commission = commission.Add(FixedAmount);
            }

            return commission;
        }

        /// <summary>
        /// Calculates net amount after commission
        /// </summary>
        public Money CalculateNetAmount(Money grossAmount)
        {
            var commission = CalculateCommission(grossAmount);
            return grossAmount.Subtract(commission);
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return Percentage;
            yield return FixedAmount;
            yield return Type;
        }
    }

    public enum CommissionType
    {
        Percentage,
        Fixed,
        Mixed
    }
}
