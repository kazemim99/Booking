//// ========================================
//// Booksy.ServiceCatalog.Domain/ValueObjects/HolidaySchedule.cs
//// ========================================
//using Booksy.Core.Domain.Base;
//using Booksy.Core.Domain.Exceptions;

//namespace Booksy.ServiceCatalog.Domain.ValueObjects
//{
//    /// <summary>
//    /// Recurrence pattern for holidays
//    /// </summary>


//    /// <summary>
//    /// Value Object representing a holiday or closure date
//    /// </summary>
//    public sealed class HolidaySchedule : ValueObject
//    {
//        public Guid Id { get; private init; }
//        public DateOnly Date { get; private init; }
//        public string Reason { get; private init; }
//        public bool IsRecurring { get; private init; }
//        public RecurrencePattern? Pattern { get; private init; }
//        public DateTime CreatedAt { get; private init; }

//        // Private constructor for EF Core
//        private HolidaySchedule()
//        {
//            Reason = string.Empty;
//        }

//        private HolidaySchedule(DateOnly date, string reason, bool isRecurring, RecurrencePattern? pattern)
//        {
//            if (string.IsNullOrWhiteSpace(reason))
//                throw new DomainValidationException("Holiday reason is required");

//            if (isRecurring && pattern == null)
//                throw new DomainValidationException("Recurring holidays must have a pattern");

//            if (!isRecurring && pattern != null && pattern != RecurrencePattern.None)
//                throw new DomainValidationException("Non-recurring holidays cannot have a pattern");

//            Id = Guid.NewGuid();
//            Date = date;
//            Reason = reason.Trim();
//            IsRecurring = isRecurring;
//            Pattern = pattern;
//            CreatedAt = DateTime.UtcNow;
//        }

//        /// <summary>
//        /// Creates a single-date (non-recurring) holiday
//        /// </summary>
//        public static HolidaySchedule CreateSingle(DateOnly date, string reason)
//        {
//            return new HolidaySchedule(date, reason, isRecurring: false, pattern: null);
//        }

//        /// <summary>
//        /// Creates a recurring holiday
//        /// </summary>
//        public static HolidaySchedule CreateRecurring(DateOnly date, string reason, RecurrencePattern pattern)
//        {
//            if (pattern == RecurrencePattern.None)
//                throw new DomainValidationException("Recurring pattern cannot be None");

//            return new HolidaySchedule(date, reason, isRecurring: true, pattern: pattern);
//        }

//        /// <summary>
//        /// Checks if this holiday occurs on a specific date
//        /// </summary>
//        public bool OccursOn(DateOnly checkDate)
//        {
//            // Exact match
//            if (Date == checkDate)
//                return true;

//            // Non-recurring holidays only match exact date
//            if (!IsRecurring)
//                return false;

//            // Check recurrence pattern
//            return Pattern switch
//            {
//                RecurrencePattern.Daily => true, // Occurs every day
//                RecurrencePattern.Weekly => Date.DayOfWeek == checkDate.DayOfWeek,
//                RecurrencePattern.Monthly => Date.Day == checkDate.Day,
//                RecurrencePattern.Yearly => Date.Month == checkDate.Month && Date.Day == checkDate.Day,
//                _ => false
//            };
//        }

//        /// <summary>
//        /// Gets all occurrences within a date range
//        /// </summary>
//        public IEnumerable<DateOnly> GetOccurrences(DateOnly startDate, DateOnly endDate)
//        {
//            if (startDate > endDate)
//                throw new ArgumentException("Start date must be before or equal to end date");

//            var occurrences = new List<DateOnly>();

//            // Add the base date if it falls in range
//            if (Date >= startDate && Date <= endDate)
//            {
//                occurrences.Add(Date);
//            }

//            // For non-recurring, return just the base date if in range
//            if (!IsRecurring)
//                return occurrences;

//            // Calculate recurring occurrences
//            var current = startDate;
//            var maxIterations = 10000; // Safety limit
//            var iterations = 0;

//            while (current <= endDate && iterations < maxIterations)
//            {
//                if (OccursOn(current) && !occurrences.Contains(current))
//                {
//                    occurrences.Add(current);
//                }

//                // Increment based on pattern for efficiency
//                current = Pattern switch
//                {
//                    RecurrencePattern.Daily => current.AddDays(1),
//                    RecurrencePattern.Weekly => current.AddDays(1),
//                    RecurrencePattern.Monthly => current.AddDays(1),
//                    RecurrencePattern.Yearly => current.AddMonths(1),
//                    _ => current.AddDays(1)
//                };

//                iterations++;
//            }

//            return occurrences.OrderBy(d => d);
//        }

//        /// <summary>
//        /// Gets the next occurrence after a given date
//        /// </summary>
//        public DateOnly? GetNextOccurrence(DateOnly afterDate)
//        {
//            // If not recurring and date is in future, return it
//            if (!IsRecurring)
//            {
//                return Date > afterDate ? Date : null;
//            }

//            // Search up to 2 years ahead
//            var searchEnd = afterDate.AddYears(2);
//            var occurrences = GetOccurrences(afterDate.AddDays(1), searchEnd);
//            return occurrences.FirstOrDefault();
//        }

//        public override string ToString()
//        {
//            var recurring = IsRecurring ? $" (Recurring: {Pattern})" : "";
//            return $"{Date:yyyy-MM-dd}: {Reason}{recurring}";
//        }

//        protected override IEnumerable<object> GetAtomicValues()
//        {
//            yield return Id;
//            yield return Date;
//            yield return Reason;
//            yield return IsRecurring;
//            yield return Pattern ?? RecurrencePattern.None;
//        }
//    }
//}
