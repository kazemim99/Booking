
using Booksy.Core.Domain.Base;

namespace Booksy.Core.Domain.ValueObjects
{
    /// <summary>
    /// Strongly-typed identifier for User aggregate
    /// </summary>
    public sealed class UserId : ValueObject
    {
        public Guid Value { get; }

        private UserId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty", nameof(value));

            Value = value;
        }

        public static UserId CreateNew() => new(Guid.NewGuid());

        public static UserId From(Guid value) => new(value);

        public static UserId From(string value)
        {
            if (!Guid.TryParse(value, out var guid))
                throw new ArgumentException($"Invalid UserId format: {value}", nameof(value));

            return new UserId(guid);
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();

        public static implicit operator Guid(UserId id) => id.Value;

        public static implicit operator string(UserId id) => id.Value.ToString();
    }
}


