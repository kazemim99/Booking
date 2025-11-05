// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/TimeSlot.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    /// <summary>
    /// Represents a time slot for a booking with start and end times
    /// </summary>
    public sealed class TimeSlot : ValueObject
    {
        public TimeSlot() { }

        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public Duration Duration => Duration.FromMinutes((int)(EndTime - StartTime).TotalMinutes);

        private TimeSlot(DateTime startTime, DateTime endTime)
        {
            if (startTime >= endTime)
                throw new ArgumentException("Start time must be before end time", nameof(startTime));

            if (startTime < DateTime.UtcNow.AddMinutes(-5)) // Allow 5 minute grace period
                throw new ArgumentException("Start time cannot be in the past", nameof(startTime));

            StartTime = startTime;
            EndTime = endTime;
        }

        public static TimeSlot Create(DateTime startTime, DateTime endTime) => new(startTime, endTime);

        public static TimeSlot Create(DateTime startTime, Duration duration)
            => new(startTime, startTime.AddMinutes(duration.Value));

        /// <summary>
        /// Checks if this time slot overlaps with another time slot
        /// </summary>
        public bool OverlapsWith(TimeSlot other)
        {
            return StartTime < other.EndTime && EndTime > other.StartTime;
        }

        /// <summary>
        /// Checks if this time slot contains a specific date/time
        /// </summary>
        public bool Contains(DateTime dateTime)
        {
            return dateTime >= StartTime && dateTime < EndTime;
        }

        /// <summary>
        /// Checks if this time slot is adjacent to another (no gap between them)
        /// </summary>
        public bool IsAdjacentTo(TimeSlot other)
        {
            return EndTime == other.StartTime || StartTime == other.EndTime;
        }

        /// <summary>
        /// Adds a buffer time after the end of this slot
        /// </summary>
        public TimeSlot WithBuffer(Duration bufferDuration)
        {
            return new TimeSlot(StartTime, EndTime.AddMinutes(bufferDuration.Value));
        }

        public override string ToString()
        {
            return $"{StartTime:yyyy-MM-dd HH:mm} - {EndTime:HH:mm} ({Duration})";
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return StartTime;
            yield return EndTime;
        }
    }
}
