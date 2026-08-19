// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/Price.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    public sealed class Price : ValueObject
    {
        public Price() { }
        public decimal Amount { get; }
        public string Currency { get; }

        private Price(decimal amount, string currency)
        {
            if (amount < 0)
                throw new ArgumentException("Price amount cannot be negative", nameof(amount));

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency cannot be empty", nameof(currency));

            Amount = Math.Round(amount, 2); // Round to 2 decimal places
            Currency = currency.ToUpperInvariant();
        }

        public static Price Create(decimal amount, string currency) => new(amount, currency);
        public static Price Zero(string currency) => new(0, currency);

        // Common currencies
        public static Price USD(decimal amount) => new(amount, "USD");
        public static Price EUR(decimal amount) => new(amount, "EUR");
        public static Price GBP(decimal amount) => new(amount, "GBP");

        public bool IsZero => Amount == 0;
        public bool IsPositive => Amount > 0;

        /// <summary>
        /// Returns an equal <see cref="Price"/> that is a distinct CLR instance.
        /// </summary>
        /// <remarks>
        /// <see cref="Price"/> is persisted as an EF Core <b>owned</b> entity, so the same CLR instance must
        /// never occupy two owned slots (e.g. an original booking's <c>TotalPrice</c> and its rescheduled
        /// successor's). Sharing one corrupts owned-entity tracking — EF treats the second slot as a re-parent
        /// of the first and refuses it ("part of a key and so cannot be modified"). Same rule as
        /// <see cref="Core.Domain.ValueObjects.Money.Clone"/>; see ADR-005.
        /// </remarks>
        public new Price Clone() => new(Amount, Currency);

        public override string ToString() => $"{Amount:F2} {Currency}";

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Amount;
            yield return Currency;
        }

        // Operators (same currency only)
        public static Price operator +(Price left, Price right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Cannot add prices with different currencies");

            return Create(left.Amount + right.Amount, left.Currency);
        }

        public static Price operator -(Price left, Price right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Cannot subtract prices with different currencies");

            return Create(left.Amount - right.Amount, left.Currency);
        }

        public static Price operator *(Price price, decimal multiplier)
            => Create(price.Amount * multiplier, price.Currency);

        public static Price operator /(Price price, decimal divisor)
        {
            if (divisor == 0)
                throw new DivideByZeroException("Cannot divide by zero");

            return Create(price.Amount / divisor, price.Currency);
        }

        public static bool operator >(Price left, Price right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Cannot compare prices with different currencies");

            return left.Amount > right.Amount;
        }

        public static bool operator <(Price left, Price right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Cannot compare prices with different currencies");

            return left.Amount < right.Amount;
        }

        public static bool operator >=(Price left, Price right) => !(left < right);
        public static bool operator <=(Price left, Price right) => !(left > right);
    }
}