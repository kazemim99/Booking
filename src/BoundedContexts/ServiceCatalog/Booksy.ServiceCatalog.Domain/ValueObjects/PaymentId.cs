// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/PaymentId.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Strongly-typed identifier for Payment aggregate
    /// </summary>
    public sealed class PaymentId : ValueObject
    {
        public Guid Value { get; }

        private PaymentId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("PaymentId cannot be empty", nameof(value));

            Value = value;
        }

        public static PaymentId New() => new(Guid.NewGuid());

        public static PaymentId From(Guid value) => new(value);

        public static PaymentId From(string value)
        {
            if (!Guid.TryParse(value, out var guid))
                throw new ArgumentException($"Invalid PaymentId format: {value}", nameof(value));

            return new PaymentId(guid);
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();

        public static implicit operator Guid(PaymentId paymentId) => paymentId.Value;
    }
}
