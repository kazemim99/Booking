// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/OperatingHours.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    public sealed class OperatingHours : ValueObject
    {
        public OperatingHours() { }
        public TimeOnly StartTime { get; }
        public TimeOnly EndTime { get; }

        private OperatingHours(TimeOnly startTime, TimeOnly endTime)
        {
            if (startTime >= endTime)
                throw new ArgumentException("Start time must be before end time");

            StartTime = startTime;
            EndTime = endTime;
        }

        public static OperatingHours Create(TimeOnly startTime, TimeOnly endTime) => new(startTime, endTime);

        // Common operating hours
        public static OperatingHours StandardBusiness => Create(new TimeOnly(9, 0), new TimeOnly(17, 0));
        public static OperatingHours Extended => Create(new TimeOnly(8, 0), new TimeOnly(20, 0));
        public static OperatingHours EarlyMorning => Create(new TimeOnly(6, 0), new TimeOnly(14, 0));

        public Duration GetDuration()
        {
            var totalMinutes = (int)(EndTime - StartTime).TotalMinutes;
            return Duration.FromMinutes(totalMinutes);
        }

        public bool Contains(TimeOnly time)
        {
            return time >= StartTime && time <= EndTime;
        }

        public bool OverlapsWith(OperatingHours other)
        {
            return StartTime < other.EndTime && EndTime > other.StartTime;
        }

        public OperatingHours? GetOverlap(OperatingHours other)
        {
            if (!OverlapsWith(other))
                return null;

            var overlapStart = StartTime > other.StartTime ? StartTime : other.StartTime;
            var overlapEnd = EndTime < other.EndTime ? EndTime : other.EndTime;

            return Create(overlapStart, overlapEnd);
        }

        public override string ToString() => $"{StartTime:HH:mm} - {EndTime:HH:mm}";

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return StartTime;
            yield return EndTime;
        }
    }
}