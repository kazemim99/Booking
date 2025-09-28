// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/ServiceId.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    public sealed class ServiceId : ValueObject
    {
        public ServiceId() { }
        public Guid Value { get; }

        private ServiceId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("ServiceId cannot be empty", nameof(value));

            Value = value;
        }

        public static ServiceId New() => new(Guid.NewGuid());
        public static ServiceId Create(Guid value) => new(value);
        public static ServiceId Create(string value) => new(new Guid(value));

        public override string ToString() => Value.ToString();

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        // Implicit conversions
        public static implicit operator Guid(ServiceId serviceId) => serviceId.Value;
        public static implicit operator ServiceId(Guid value) => Create(value);
    }
}