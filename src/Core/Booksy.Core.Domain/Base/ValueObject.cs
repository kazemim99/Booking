
using Booksy.Core.Domain.Abstractions.ValueObjects;

// ========================================
// Booksy.Core.Domain/Base/ValueObject.cs
// ========================================

namespace Booksy.Core.Domain.Base
{
    /// <summary>
    /// Base class for value objects in Domain-Driven Design
    /// Value objects are immutable and compared by their values
    /// </summary>
    public abstract class ValueObject : IValueObject, IEquatable<ValueObject>
    {
        /// <summary>
        /// Gets the atomic values that compose this value object for equality comparison
        /// </summary>
        protected abstract IEnumerable<object?> GetAtomicValues();

        IEnumerable<object?> IValueObject.GetAtomicValues() => GetAtomicValues();

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            var other = (ValueObject)obj;
            return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
        }

        public bool Equals(ValueObject? other)
        {
            return Equals((object?)other);
        }

        public override int GetHashCode()
        {
            return GetAtomicValues()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate((x, y) => x ^ y);
        }

        public static bool operator ==(ValueObject? left, ValueObject? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(ValueObject? left, ValueObject? right)
        {
            return !(left == right);
        }

        public ValueObject Clone()
        {
            return (ValueObject)MemberwiseClone();
        }
    }
}

