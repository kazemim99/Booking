// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/TemplateId.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Strongly-typed identifier for NotificationTemplate aggregate
    /// </summary>
    public sealed class TemplateId : ValueObject
    {
        public Guid Value { get; }

        private TemplateId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("TemplateId cannot be empty", nameof(value));

            Value = value;
        }

        public static TemplateId From(Guid value) => new(value);

        public static TemplateId Create() => new(Guid.NewGuid());

        public override string ToString() => Value.ToString();

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        // Implicit conversion to Guid for convenience
        public static implicit operator Guid(TemplateId templateId) => templateId.Value;
    }
}
