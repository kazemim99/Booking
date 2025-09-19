// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/Duration.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    public sealed class Duration : ValueObject
    {

        public Duration() { }
        public int Value { get; } // Duration in minutes

        private Duration(int value)
        {
            if (value <= 0)
                throw new ArgumentException("Duration must be positive", nameof(value));

            if (value > 480) // 8 hours max
                throw new ArgumentException("Duration cannot exceed 8 hours", nameof(value));

            Value = value;
        }

        public static Duration FromMinutes(int minutes) => new(minutes);
        public static Duration FromHours(double hours) => new((int)(hours * 60));

        // Common durations
        public static Duration FifteenMinutes => FromMinutes(15);
        public static Duration ThirtyMinutes => FromMinutes(30);
        public static Duration OneHour => FromMinutes(60);
        public static Duration TwoHours => FromMinutes(120);

        public TimeSpan ToTimeSpan() => TimeSpan.FromMinutes(Value);

        public override string ToString()
        {
            if (Value < 60)
                return $"{Value}m";

            var hours = Value / 60;
            var minutes = Value % 60;

            return minutes == 0 ? $"{hours}h" : $"{hours}h {minutes}m";
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        // Operators
        public static Duration operator +(Duration left, Duration right)
            => FromMinutes(left.Value + right.Value);

        public static Duration operator -(Duration left, Duration right)
        {
            var result = left.Value - right.Value;
            return result > 0 ? FromMinutes(result) : FromMinutes(1); // Minimum 1 minute
        }

        public static bool operator >(Duration left, Duration right) => left.Value > right.Value;
        public static bool operator <(Duration left, Duration right) => left.Value < right.Value;
        public static bool operator >=(Duration left, Duration right) => left.Value >= right.Value;
        public static bool operator <=(Duration left, Duration right) => left.Value <= right.Value;

        // Implicit conversion
        public static implicit operator int(Duration duration) => duration.Value;
    }
}