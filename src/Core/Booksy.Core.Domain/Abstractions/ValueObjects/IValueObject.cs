// ========================================
// Booksy.Core.Domain/Abstractions/ValueObjects/IValueObject.cs
// ========================================
namespace Booksy.Core.Domain.Abstractions.ValueObjects
{
    /// <summary>
    /// Marker interface for value objects in Domain-Driven Design
    /// Value objects are immutable and compared by their values
    /// </summary>
    public interface IValueObject
    {
        /// <summary>
        /// Gets the atomic values that compose this value object for equality comparison
        /// </summary>
        IEnumerable<object?> GetAtomicValues();
    }
}