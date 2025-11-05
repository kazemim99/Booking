// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/NotificationId.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Strongly-typed identifier for Notification aggregate
    /// </summary>
    public sealed class NotificationId : ValueObject
    {
        public Guid Value { get; }

        private NotificationId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("NotificationId cannot be empty", nameof(value));

            Value = value;
        }

        public static NotificationId From(Guid value) => new(value);

        public static NotificationId Create() => new(Guid.NewGuid());

        public override string ToString() => Value.ToString();

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        // Implicit conversion to Guid for convenience
        public static implicit operator Guid(NotificationId notificationId) => notificationId.Value;
    }
}
