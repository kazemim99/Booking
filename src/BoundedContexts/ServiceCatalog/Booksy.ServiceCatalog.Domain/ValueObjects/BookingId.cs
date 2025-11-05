// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/BookingId.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Strongly-typed identifier for Booking aggregate
    /// </summary>
    public sealed class BookingId : ValueObject
    {
        public BookingId() { }
        public Guid Value { get; }

        private BookingId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("BookingId cannot be empty", nameof(value));

            Value = value;
        }

        public static BookingId New() => new(Guid.NewGuid());
        public static BookingId From(Guid value) => new(value);
        public static BookingId From(string value) => new(new Guid(value));

        public override string ToString() => Value.ToString();

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        // Implicit conversions
        public static implicit operator Guid(BookingId bookingId) => bookingId.Value;
        public static implicit operator BookingId(Guid value) => From(value);
    }
}
