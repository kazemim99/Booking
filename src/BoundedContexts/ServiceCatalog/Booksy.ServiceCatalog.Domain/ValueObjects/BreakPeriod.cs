// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/BreakPeriod.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Value Object representing a break period within business operating hours
    /// </summary>
    public sealed class BreakPeriod : ValueObject
    {
        public TimeOnly StartTime { get; private init; }
        public TimeOnly EndTime { get; private init; }
        public string? Label { get; private init; }

        // Private constructor for EF Core
        private BreakPeriod() { }

        private BreakPeriod(TimeOnly startTime, TimeOnly endTime, string? label = null)
        {
            if (startTime >= endTime)
                throw new DomainValidationException("Break start time must be before end time");

            StartTime = startTime;
            EndTime = endTime;
            Label = label;
        }

        /// <summary>
        /// Creates a break period
        /// </summary>
        public static BreakPeriod Create(TimeOnly startTime, TimeOnly endTime, string? label = null)
        {
            return new BreakPeriod(startTime, endTime, label);
        }

        /// <summary>
        /// Gets the duration of the break in minutes
        /// </summary>
        public int GetDurationInMinutes()
        {
            return (int)(EndTime - StartTime).TotalMinutes;
        }

        /// <summary>
        /// Gets the duration of the break
        /// </summary>
        public Duration GetDuration()
        {
            return Duration.FromMinutes(GetDurationInMinutes());
        }

        /// <summary>
        /// Checks if this break overlaps with another break
        /// </summary>
        public bool OverlapsWith(BreakPeriod other)
        {
            return StartTime < other.EndTime && EndTime > other.StartTime;
        }

        /// <summary>
        /// Checks if this break falls within the given operating hours
        /// </summary>
        public bool FallsWithin(TimeOnly openTime, TimeOnly closeTime)
        {
            return StartTime >= openTime && EndTime <= closeTime;
        }

        /// <summary>
        /// Validates that this break doesn't overlap with any in the collection
        /// </summary>
        public static void ValidateNoOverlaps(IEnumerable<BreakPeriod> breaks)
        {
            var breakList = breaks.ToList();
            for (int i = 0; i < breakList.Count; i++)
            {
                for (int j = i + 1; j < breakList.Count; j++)
                {
                    if (breakList[i].OverlapsWith(breakList[j]))
                    {
                        throw new DomainValidationException(
                            $"Break periods overlap: {breakList[i]} and {breakList[j]}");
                    }
                }
            }
        }

        public override string ToString()
        {
            var timeRange = $"{StartTime:HH:mm} - {EndTime:HH:mm}";
            return string.IsNullOrWhiteSpace(Label)
                ? timeRange
                : $"{Label} ({timeRange})";
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return StartTime;
            yield return EndTime;
            yield return Label ?? string.Empty;
        }
    }
}
