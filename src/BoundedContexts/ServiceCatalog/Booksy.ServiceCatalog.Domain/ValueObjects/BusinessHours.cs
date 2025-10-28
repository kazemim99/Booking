//// ========================================
//// Booksy.ServiceCatalog.Domain/ValueObjects/BusinessHours.cs
//// ========================================
//using Booksy.Core.Domain.Base;
//using Booksy.Core.Domain.Exceptions;
//using Booksy.ServiceCatalog.Domain.Enums;

//namespace Booksy.ServiceCatalog.Domain.ValueObjects
//{
//    /// <summary>
//    /// Value Object representing business operating hours for a specific day of the week
//    /// </summary>
//    public sealed class BusinessHours : ValueObject
//    {
//        /// <summary>
//        /// EF Core requires an Id for owned collections to track changes
//        /// This is a technical requirement, not part of the domain model
//        /// </summary>
//        public int Id { get; private set; }

//        public DayOfWeek DayOfWeek { get; private init; }
//        public TimeOnly? OpenTime { get; private init; }
//        public TimeOnly? CloseTime { get; private init; }

//        private readonly List<BreakPeriod> _breaks = new();

//        /// <summary>
//        /// Break periods within operating hours
//        /// </summary>
//        public IReadOnlyCollection<BreakPeriod> Breaks => _breaks.AsReadOnly();

//        /// <summary>
//        /// Indicates whether the business is open on this day
//        /// </summary>
//        public bool IsOpen => OpenTime.HasValue && CloseTime.HasValue;

//        // Private constructor for EF Core
//        private BusinessHours() { }

//        /// <summary>
//        /// Creates business hours for an open day
//        /// </summary>
//        public static BusinessHours CreateOpen(DayOfWeek day, TimeOnly openTime, TimeOnly closeTime)
//        {
//            if (openTime >= closeTime)
//                throw new DomainValidationException("Open time must be before close time");

//            return new BusinessHours
//            {
//                DayOfWeek = day,
//                OpenTime = openTime,
//                CloseTime = closeTime
//            };
//        }

//        /// <summary>
//        /// Creates business hours for a closed day
//        /// </summary>
//        public static BusinessHours CreateClosed(DayOfWeek day)
//        {
//            return new BusinessHours
//            {
//                DayOfWeek = day,
//                OpenTime = null,
//                CloseTime = null
//            };
//        }

//        /// <summary>
//        /// Creates business hours for an open day with break periods
//        /// </summary>
//        public static BusinessHours CreateWithBreaks(DayOfWeek day, TimeOnly openTime, TimeOnly closeTime, IEnumerable<BreakPeriod> breaks)
//        {
//            if (openTime >= closeTime)
//                throw new DomainValidationException("Open time must be before close time");

//            var breakList = breaks.ToList();

//            // Validate all breaks fall within operating hours
//            foreach (var breakPeriod in breakList)
//            {
//                if (!breakPeriod.FallsWithin(openTime, closeTime))
//                    throw new DomainValidationException($"Break period {breakPeriod} falls outside operating hours {openTime:HH:mm} - {closeTime:HH:mm}");
//            }

//            // Validate no overlapping breaks
//            BreakPeriod.ValidateNoOverlaps(breakList);

//            var businessHours = new BusinessHours
//            {
//                DayOfWeek = day,
//                OpenTime = openTime,
//                CloseTime = closeTime
//            };

//            businessHours._breaks.AddRange(breakList);

//            return businessHours;
//        }

//        /// <summary>
//        /// Gets the duration of operating hours
//        /// </summary>
//        public Duration? GetDuration()
//        {
//            if (!IsOpen) return null;

//            var totalMinutes = (int)(CloseTime!.Value - OpenTime!.Value).TotalMinutes;
//            return Duration.FromMinutes(totalMinutes);
//        }

//        /// <summary>
//        /// Checks if a specific time falls within operating hours
//        /// </summary>
//        public bool Contains(TimeOnly time)
//        {
//            if (!IsOpen) return false;
//            return time >= OpenTime && time <= CloseTime;
//        }

//        /// <summary>
//        /// Gets available time slots by subtracting breaks from operating hours
//        /// </summary>
//        public IEnumerable<(TimeOnly Start, TimeOnly End)> GetAvailableSlots()
//        {
//            if (!IsOpen)
//                return Enumerable.Empty<(TimeOnly, TimeOnly)>();

//            if (!_breaks.Any())
//                return new[] { (OpenTime!.Value, CloseTime!.Value) };

//            var slots = new List<(TimeOnly, TimeOnly)>();
//            var currentStart = OpenTime!.Value;

//            // Sort breaks by start time
//            var sortedBreaks = _breaks.OrderBy(b => b.StartTime).ToList();

//            foreach (var breakPeriod in sortedBreaks)
//            {
//                // Add slot before break if there's time
//                if (currentStart < breakPeriod.StartTime)
//                {
//                    slots.Add((currentStart, breakPeriod.StartTime));
//                }
//                currentStart = breakPeriod.EndTime;
//            }

//            // Add final slot after last break
//            if (currentStart < CloseTime!.Value)
//            {
//                slots.Add((currentStart, CloseTime.Value));
//            }

//            return slots;
//        }

//        /// <summary>
//        /// Gets the total available duration after subtracting breaks
//        /// </summary>
//        public Duration? GetNetAvailableDuration()
//        {
//            if (!IsOpen) return null;

//            var grossMinutes = (int)(CloseTime!.Value - OpenTime!.Value).TotalMinutes;
//            var breakMinutes = _breaks.Sum(b => b.GetDurationInMinutes());
//            var netMinutes = grossMinutes - breakMinutes;

//            return netMinutes > 0 ? Duration.FromMinutes(netMinutes) : null;
//        }

//        public override string ToString()
//        {
//            if (!IsOpen) return $"{DayOfWeek}: Closed";
//            return $"{DayOfWeek}: {OpenTime:HH:mm} - {CloseTime:HH:mm}";
//        }

//        protected override IEnumerable<object> GetAtomicValues()
//        {
//            yield return DayOfWeek;
//            yield return OpenTime ?? TimeOnly.MinValue;
//            yield return CloseTime ?? TimeOnly.MinValue;

//            foreach (var breakPeriod in _breaks.OrderBy(b => b.StartTime))
//            {
//                yield return breakPeriod;
//            }
//        }
//    }
//}
