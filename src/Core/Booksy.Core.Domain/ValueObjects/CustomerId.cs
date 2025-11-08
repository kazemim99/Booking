using Booksy.Core.Domain.Base;

namespace Booksy.Core.Domain.ValueObjects
{
    /// <summary>
    /// Strongly-typed identifier for Customer aggregate
    /// </summary>
    public sealed class CustomerId : ValueObject
    {
        public Guid Value { get; }

        private CustomerId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("CustomerId cannot be empty", nameof(value));

            Value = value;
        }

        public static CustomerId CreateNew() => new(Guid.NewGuid());

        public static CustomerId From(Guid value) => new(value);

        public static CustomerId From(string value)
        {
            if (!Guid.TryParse(value, out var guid))
                throw new ArgumentException($"Invalid CustomerId format: {value}", nameof(value));

            return new CustomerId(guid);
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();

        public static implicit operator Guid(CustomerId id) => id.Value;

        public static implicit operator string(CustomerId id) => id.Value.ToString();
    }
}
