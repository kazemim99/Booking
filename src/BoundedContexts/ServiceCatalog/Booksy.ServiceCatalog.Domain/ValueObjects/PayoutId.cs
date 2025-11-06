// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/PayoutId.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Strongly-typed identifier for Payout aggregate
    /// </summary>
    public sealed class PayoutId : ValueObject
    {
        public Guid Value { get; }

        private PayoutId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("PayoutId cannot be empty", nameof(value));

            Value = value;
        }

        public static PayoutId New() => new(Guid.NewGuid());

        public static PayoutId From(Guid value) => new(value);

        public static PayoutId From(string value)
        {
            if (!Guid.TryParse(value, out var guid))
                throw new ArgumentException($"Invalid PayoutId format: {value}", nameof(value));

            return new PayoutId(guid);
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();

        public static implicit operator Guid(PayoutId payoutId) => payoutId.Value;
    }
}
