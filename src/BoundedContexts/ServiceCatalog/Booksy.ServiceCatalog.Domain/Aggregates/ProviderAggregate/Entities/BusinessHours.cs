// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/BusinessHours.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Entities
{
    /// <summary>
    /// Entity representing business operating hours for a specific day of the week
    /// Converted from ValueObject to support mutability, audit trail, and lifecycle management
    /// </summary>
    public sealed class BusinessHours : Entity<Guid>
    {
        public DayOfWeek DayOfWeek { get; private set; }
        public TimeOnly? OpenTime { get; private set; }
        public TimeOnly? CloseTime { get; private set; }

        private readonly List<BreakPeriod> _breaks = new();

        /// <summary>
        /// Break periods within operating hours
        /// </summary>
        public IReadOnlyCollection<BreakPeriod> Breaks => _breaks.AsReadOnly();

        /// <summary>
        /// Indicates whether the business is open on this day
        /// </summary>
        public bool IsOpen => OpenTime.HasValue && CloseTime.HasValue;

        public ProviderId ProviderId { get;private set; }


        // Private constructor for EF Core
        private BusinessHours() : base() { }

        /// <summary>
        /// Creates business hours for an open day
        /// </summary>
        internal static BusinessHours CreateOpen(ProviderId providerId,DayOfWeek day, TimeOnly openTime, TimeOnly closeTime, string? createdBy = null)
        {
            if (openTime >= closeTime)
                throw new DomainValidationException("Open time must be before close time");

            var businessHours = new BusinessHours
            {
                Id = Guid.NewGuid(),
                DayOfWeek = day,
                OpenTime = openTime,
                CloseTime = closeTime,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
            businessHours.ProviderId = providerId;
            return businessHours;
        }

        /// <summary>
        /// Creates business hours for a closed day
        /// </summary>
        internal static BusinessHours CreateClosed(ProviderId providerId,DayOfWeek day, string? createdBy = null)
        {
            var businessHours = new BusinessHours
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                DayOfWeek = day,
                OpenTime = null,
                CloseTime = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            return businessHours;
        }

        /// <summary>
        /// Creates business hours for an open day with break periods
        /// </summary>
        internal static BusinessHours CreateWithBreaks(
            ProviderId providerId,
            DayOfWeek day,
            TimeOnly openTime,
            TimeOnly closeTime,
            IEnumerable<BreakPeriod> breaks,
            string? createdBy = null)
        {
            if (openTime >= closeTime)
                throw new DomainValidationException("Open time must be before close time");

            var breakList = breaks.ToList();

            // Validate all breaks fall within operating hours
            foreach (var breakPeriod in breakList)
            {
                if (!breakPeriod.FallsWithin(openTime, closeTime))
                    throw new DomainValidationException(
                        $"Break period {breakPeriod} falls outside operating hours {openTime:HH:mm} - {closeTime:HH:mm}");
            }

            // Validate no overlapping breaks
            BreakPeriod.ValidateNoOverlaps(breakList);

            var businessHours = new BusinessHours
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                DayOfWeek = day,
                OpenTime = openTime,
                CloseTime = closeTime,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            businessHours._breaks.AddRange(breakList);

            return businessHours;
        }

        /// <summary>
        /// Updates operating hours
        /// </summary>
        public void UpdateHours(ProviderId providerId,TimeOnly openTime, TimeOnly closeTime, string? modifiedBy = null)
        {
            if (openTime >= closeTime)
                throw new DomainValidationException("Open time must be before close time");

            // Validate existing breaks still fall within new hours
            foreach (var breakPeriod in _breaks)
            {
                if (!breakPeriod.FallsWithin(openTime, closeTime))
                    throw new DomainValidationException(
                        $"Break period {breakPeriod} would fall outside new operating hours {openTime:HH:mm} - {closeTime:HH:mm}");
            }

            OpenTime = openTime;
            CloseTime = closeTime;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
            ProviderId = providerId;
        }

        /// <summary>
        /// Sets the day as closed
        /// </summary>
        public void SetClosed(ProviderId providerId,string? modifiedBy = null)
        {
            OpenTime = null;
            CloseTime = null;
            _breaks.Clear();
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
            ProviderId = providerId;
        }

        /// <summary>
        /// Adds a break period
        /// </summary>
        public void AddBreak(ProviderId providerId, BreakPeriod breakPeriod, string? modifiedBy = null)
        {
            if (!IsOpen)
                throw new DomainValidationException("Cannot add breaks to a closed day");

            if (!breakPeriod.FallsWithin(OpenTime!.Value, CloseTime!.Value))
                throw new DomainValidationException(
                    $"Break period {breakPeriod} falls outside operating hours {OpenTime:HH:mm} - {CloseTime:HH:mm}");

            // Check for overlaps with existing breaks
            var allBreaks = _breaks.Concat(new[] { breakPeriod }).ToList();
            BreakPeriod.ValidateNoOverlaps(allBreaks);

            _breaks.Add(breakPeriod);
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
            ProviderId = providerId;
        }

        /// <summary>
        /// Removes all breaks
        /// </summary>
        public void ClearBreaks(ProviderId providerId, string? modifiedBy = null)
        {
            _breaks.Clear();
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
            ProviderId = providerId;
        }

        /// <summary>
        /// Sets break periods, replacing existing ones
        /// </summary>
        public void SetBreaks(ProviderId providerId, IEnumerable<BreakPeriod> breaks, string? modifiedBy = null)
        {
            if (!IsOpen)
                throw new DomainValidationException("Cannot set breaks on a closed day");

            var breakList = breaks.ToList();

            // Validate all breaks fall within operating hours
            foreach (var breakPeriod in breakList)
            {
                if (!breakPeriod.FallsWithin(OpenTime!.Value, CloseTime!.Value))
                    throw new DomainValidationException(
                        $"Break period {breakPeriod} falls outside operating hours {OpenTime:HH:mm} - {CloseTime:HH:mm}");
            }

            // Validate no overlapping breaks
            BreakPeriod.ValidateNoOverlaps(breakList);

            _breaks.Clear();
            _breaks.AddRange(breakList);
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
            ProviderId = providerId;
        }

        /// <summary>
        /// Gets the duration of operating hours
        /// </summary>
        public Duration? GetDuration()
        {
            if (!IsOpen) return null;

            var totalMinutes = (int)(CloseTime!.Value - OpenTime!.Value).TotalMinutes;
            return Duration.FromMinutes(totalMinutes);
        }

        /// <summary>
        /// Checks if a specific time falls within operating hours
        /// </summary>
        public bool Contains(TimeOnly time)
        {
            if (!IsOpen) return false;
            return time >= OpenTime && time <= CloseTime;
        }

        /// <summary>
        /// Gets available time slots by subtracting breaks from operating hours
        /// </summary>
        public IEnumerable<(TimeOnly Start, TimeOnly End)> GetAvailableSlots()
        {
            if (!IsOpen)
                return Enumerable.Empty<(TimeOnly, TimeOnly)>();

            if (!_breaks.Any())
                return new[] { (OpenTime!.Value, CloseTime!.Value) };

            var slots = new List<(TimeOnly, TimeOnly)>();
            var currentStart = OpenTime!.Value;

            // Sort breaks by start time
            var sortedBreaks = _breaks.OrderBy(b => b.StartTime).ToList();

            foreach (var breakPeriod in sortedBreaks)
            {
                // Add slot before break if there's time
                if (currentStart < breakPeriod.StartTime)
                {
                    slots.Add((currentStart, breakPeriod.StartTime));
                }
                currentStart = breakPeriod.EndTime;
            }

            // Add final slot after last break
            if (currentStart < CloseTime!.Value)
            {
                slots.Add((currentStart, CloseTime.Value));
            }

            return slots;
        }

        /// <summary>
        /// Gets the total available duration after subtracting breaks
        /// </summary>
        public Duration? GetNetAvailableDuration()
        {
            if (!IsOpen) return null;

            var grossMinutes = (int)(CloseTime!.Value - OpenTime!.Value).TotalMinutes;
            var breakMinutes = _breaks.Sum(b => b.GetDurationInMinutes());
            var netMinutes = grossMinutes - breakMinutes;

            return netMinutes > 0 ? Duration.FromMinutes(netMinutes) : null;
        }

        public override string ToString()
        {
            if (!IsOpen) return $"{DayOfWeek}: Closed";
            return $"{DayOfWeek}: {OpenTime:HH:mm} - {CloseTime:HH:mm}";
        }
    }
}
