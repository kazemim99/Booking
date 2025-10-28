//// ========================================
//// Booksy.ServiceCatalog.Domain/ValueObjects/ExceptionSchedule.cs
//// ========================================
//using Booksy.Core.Domain.Base;
//using Booksy.Core.Domain.Exceptions;

//namespace Booksy.ServiceCatalog.Domain.ValueObjects
//{
//    /// <summary>
//    /// Value Object representing a temporary schedule override for a specific date
//    /// </summary>
//    public sealed class ExceptionSchedule : ValueObject
//    {
//        public Guid Id { get; private init; }
//        public DateOnly Date { get; private init; }
//        public TimeOnly? OpenTime { get; private init; }
//        public TimeOnly? CloseTime { get; private init; }
//        public string Reason { get; private init; }
//        public DateTime CreatedAt { get; private init; }

//        /// <summary>
//        /// Indicates whether the business is closed on this exception date
//        /// </summary>
//        public bool IsClosed => !OpenTime.HasValue || !CloseTime.HasValue;

//        // Private constructor for EF Core
//        private ExceptionSchedule()
//        {
//            Reason = string.Empty;
//        }

//        private ExceptionSchedule(DateOnly date, TimeOnly? openTime, TimeOnly? closeTime, string reason)
//        {
//            if (string.IsNullOrWhiteSpace(reason))
//                throw new DomainValidationException("Exception reason is required");

//            // If times are provided, validate them
//            if (openTime.HasValue && closeTime.HasValue)
//            {
//                if (openTime >= closeTime)
//                    throw new DomainValidationException("Open time must be before close time");
//            }

//            // Both must be null or both must have values
//            if (openTime.HasValue != closeTime.HasValue)
//                throw new DomainValidationException("Both open and close times must be provided, or both must be null for closure");

//            Id = Guid.NewGuid();
//            Date = date;
//            OpenTime = openTime;
//            CloseTime = closeTime;
//            Reason = reason.Trim();
//            CreatedAt = DateTime.UtcNow;
//        }

//        /// <summary>
//        /// Creates an exception with modified hours for a specific date
//        /// </summary>
//        public static ExceptionSchedule CreateWithModifiedHours(DateOnly date, TimeOnly openTime, TimeOnly closeTime, string reason)
//        {
//            return new ExceptionSchedule(date, openTime, closeTime, reason);
//        }

//        /// <summary>
//        /// Creates an exception marking a date as closed
//        /// </summary>
//        public static ExceptionSchedule CreateClosed(DateOnly date, string reason)
//        {
//            return new ExceptionSchedule(date, openTime: null, closeTime: null, reason);
//        }

//        /// <summary>
//        /// Gets the duration of operating hours for this exception
//        /// Returns null if closed
//        /// </summary>
//        public Duration? GetDuration()
//        {
//            if (IsClosed) return null;

//            var totalMinutes = (int)(CloseTime!.Value - OpenTime!.Value).TotalMinutes;
//            return Duration.FromMinutes(totalMinutes);
//        }

//        /// <summary>
//        /// Checks if a specific time falls within exception hours
//        /// Returns false if closed
//        /// </summary>
//        public bool Contains(TimeOnly time)
//        {
//            if (IsClosed) return false;
//            return time >= OpenTime && time <= CloseTime;
//        }

//        /// <summary>
//        /// Checks if this exception conflicts with a holiday on the same date
//        /// </summary>
//        public bool ConflictsWith(HolidaySchedule holiday)
//        {
//            return holiday.OccursOn(Date);
//        }

//        public override string ToString()
//        {
//            if (IsClosed)
//                return $"{Date:yyyy-MM-dd}: Closed - {Reason}";

//            return $"{Date:yyyy-MM-dd}: {OpenTime:HH:mm} - {CloseTime:HH:mm} - {Reason}";
//        }

//        protected override IEnumerable<object> GetAtomicValues()
//        {
//            yield return Id;
//            yield return Date;
//            yield return OpenTime ?? TimeOnly.MinValue;
//            yield return CloseTime ?? TimeOnly.MinValue;
//            yield return Reason;
//        }
//    }
//}
