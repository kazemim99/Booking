// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/ProviderId.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    public sealed class ProviderId : ValueObject
    {
        public ProviderId() { }
        public Guid Value { get; }

        private ProviderId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("ProviderId cannot be empty", nameof(value));

            Value = value;
        }

        public static ProviderId New() => new(Guid.NewGuid());
        public static ProviderId From(Guid value) => new(value);

        public override string ToString() => Value.ToString();

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        // Implicit conversions
        public static implicit operator Guid(ProviderId providerId) => providerId.Value;
        public static implicit operator ProviderId(Guid value) => From(value);
    }
}