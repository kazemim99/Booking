// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/TaxRate.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Represents tax rate information for financial calculations
    /// </summary>
    public sealed class TaxRate : ValueObject
    {
        public decimal Percentage { get; }
        public string TaxName { get; }
        public string TaxCode { get; }
        public bool IsInclusive { get; }

        private TaxRate(decimal percentage, string taxName, string taxCode, bool isInclusive)
        {
            if (percentage < 0 || percentage > 100)
                throw new ArgumentException("Tax percentage must be between 0 and 100", nameof(percentage));

            if (string.IsNullOrWhiteSpace(taxName))
                throw new ArgumentException("Tax name is required", nameof(taxName));

            if (string.IsNullOrWhiteSpace(taxCode))
                throw new ArgumentException("Tax code is required", nameof(taxCode));

            Percentage = percentage;
            TaxName = taxName;
            TaxCode = taxCode;
            IsInclusive = isInclusive;
        }

        public static TaxRate Create(decimal percentage, string taxName, string taxCode, bool isInclusive = false)
        {
            return new TaxRate(percentage, taxName, taxCode, isInclusive);
        }

        /// <summary>
        /// No tax
        /// </summary>
        public static TaxRate Zero => new(0, "No Tax", "ZERO", false);

        /// <summary>
        /// US Sales Tax (example - varies by state)
        /// </summary>
        public static TaxRate UsSalesTax(decimal percentage) =>
            new(percentage, "Sales Tax", "US-SALES", false);

        /// <summary>
        /// EU VAT (standard rate)
        /// </summary>
        public static TaxRate EuVat(decimal percentage) =>
            new(percentage, "VAT", "EU-VAT", true);

        /// <summary>
        /// UK VAT
        /// </summary>
        public static TaxRate UkVat => new(20m, "VAT", "UK-VAT", true);

        /// <summary>
        /// Calculates tax amount from base amount
        /// </summary>
        public Money CalculateTaxAmount(Money baseAmount)
        {
            if (IsInclusive)
            {
                // Tax is already included in the base amount
                // Extract tax: taxAmount = baseAmount * (taxRate / (100 + taxRate))
                var taxAmount = baseAmount.Amount * (Percentage / (100 + Percentage));
                return Money.Create(taxAmount, baseAmount.Currency);
            }
            else
            {
                // Tax is added on top
                return baseAmount.Multiply(Percentage / 100m);
            }
        }

        /// <summary>
        /// Calculates total amount including tax
        /// </summary>
        public Money CalculateTotalWithTax(Money baseAmount)
        {
            if (IsInclusive)
            {
                // Tax already included
                return baseAmount;
            }
            else
            {
                var taxAmount = CalculateTaxAmount(baseAmount);
                return baseAmount.Add(taxAmount);
            }
        }

        /// <summary>
        /// Calculates base amount excluding tax (if inclusive)
        /// </summary>
        public Money CalculateBaseAmount(Money totalAmount)
        {
            if (IsInclusive)
            {
                // Remove tax: baseAmount = totalAmount / (1 + taxRate/100)
                var baseAmount = totalAmount.Amount / (1 + Percentage / 100m);
                return Money.Create(baseAmount, totalAmount.Currency);
            }
            else
            {
                return totalAmount;
            }
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Percentage;
            yield return TaxName;
            yield return TaxCode;
            yield return IsInclusive;
        }
    }
}
