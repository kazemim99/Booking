// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/HolidaySchedule.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Entities
{
  
    /// <summary>
    /// Entity representing a holiday or closure date
    /// Converted from ValueObject to support mutability, audit trail, and lifecycle management
    /// </summary>
    public sealed class HolidaySchedule : Entity<Guid>
    {
        public DateOnly Date { get; private set; }
        public string Reason { get; private set; }
        public bool IsRecurring { get; private set; }
        public RecurrencePattern? Pattern { get; private set; }

        public ProviderId ProviderId { get; private set; }

        // Private constructor for EF Core
        private HolidaySchedule() : base()
        {
            Reason = string.Empty;
        }

        private HolidaySchedule(ProviderId providerId,DateOnly date, string reason, bool isRecurring, RecurrencePattern? pattern, string? createdBy)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainValidationException("Holiday reason is required");

            if (isRecurring && pattern == null)
                throw new DomainValidationException("Recurring holidays must have a pattern");

            if (!isRecurring && pattern != null && pattern != RecurrencePattern.None)
                throw new DomainValidationException("Non-recurring holidays cannot have a pattern");

            Id = Guid.NewGuid();
            Date = date;
            Reason = reason.Trim();
            IsRecurring = isRecurring;
            Pattern = pattern;
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy;
            ProviderId = providerId;
        }

        /// <summary>
        /// Creates a single-date (non-recurring) holiday
        /// </summary>
        internal static HolidaySchedule CreateSingle(ProviderId providerId,DateOnly date, string reason, string? createdBy = null)
        {
            return new HolidaySchedule(providerId,date, reason, isRecurring: false, pattern: null, createdBy);
        }

        /// <summary>
        /// Creates a recurring holiday
        /// </summary>
        internal static HolidaySchedule CreateRecurring(ProviderId providerId, DateOnly date, string reason, RecurrencePattern pattern, string? createdBy = null)
        {
            if (pattern == RecurrencePattern.None)
                throw new DomainValidationException("Recurring pattern cannot be None");

            return new HolidaySchedule(providerId, date, reason, isRecurring: true, pattern: pattern, createdBy);
        }

        /// <summary>
        /// Updates the holiday details
        /// </summary>
        public void Update(DateOnly date, string reason, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainValidationException("Holiday reason is required");

            Date = date;
            Reason = reason.Trim();
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Updates the recurrence pattern
        /// </summary>
        public void UpdateRecurrence(RecurrencePattern pattern, string? modifiedBy = null)
        {
            if (pattern == RecurrencePattern.None)
                throw new DomainValidationException("Recurring pattern cannot be None");

            IsRecurring = true;
            Pattern = pattern;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Removes the recurrence pattern, making it a single-date holiday
        /// </summary>
        public void RemoveRecurrence(string? modifiedBy = null)
        {
            IsRecurring = false;
            Pattern = null;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Marks the holiday as deleted (soft delete)
        /// </summary>
        public void Delete(string? deletedBy = null)
        {
            IsDeleted = true;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = deletedBy;
        }

        /// <summary>
        /// Checks if this holiday occurs on a specific date
        /// </summary>
        public bool OccursOn(DateOnly checkDate)
        {
            // Exact match
            if (Date == checkDate)
                return true;

            // Non-recurring holidays only match exact date
            if (!IsRecurring)
                return false;

            // Check recurrence pattern
            return Pattern switch
            {
                RecurrencePattern.Daily => true, // Occurs every day
                RecurrencePattern.Weekly => Date.DayOfWeek == checkDate.DayOfWeek,
                RecurrencePattern.Monthly => Date.Day == checkDate.Day,
                RecurrencePattern.Yearly => Date.Month == checkDate.Month && Date.Day == checkDate.Day,
                _ => false
            };
        }

        /// <summary>
        /// Gets all occurrences within a date range
        /// </summary>
        public IEnumerable<DateOnly> GetOccurrences(DateOnly startDate, DateOnly endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("Start date must be before or equal to end date");

            var occurrences = new List<DateOnly>();

            // Add the base date if it falls in range
            if (Date >= startDate && Date <= endDate)
            {
                occurrences.Add(Date);
            }

            // For non-recurring, return just the base date if in range
            if (!IsRecurring)
                return occurrences;

            // Calculate recurring occurrences
            var current = startDate;
            var maxIterations = 10000; // Safety limit
            var iterations = 0;

            while (current <= endDate && iterations < maxIterations)
            {
                if (OccursOn(current) && !occurrences.Contains(current))
                {
                    occurrences.Add(current);
                }

                // Increment based on pattern for efficiency
                current = Pattern switch
                {
                    RecurrencePattern.Daily => current.AddDays(1),
                    RecurrencePattern.Weekly => current.AddDays(1),
                    RecurrencePattern.Monthly => current.AddDays(1),
                    RecurrencePattern.Yearly => current.AddMonths(1),
                    _ => current.AddDays(1)
                };

                iterations++;
            }

            return occurrences.OrderBy(d => d);
        }

        /// <summary>
        /// Gets the next occurrence after a given date
        /// </summary>
        public DateOnly? GetNextOccurrence(DateOnly afterDate)
        {
            // If not recurring and date is in future, return it
            if (!IsRecurring)
            {
                return Date > afterDate ? Date : null;
            }

            // Search up to 2 years ahead
            var searchEnd = afterDate.AddYears(2);
            var occurrences = GetOccurrences(afterDate.AddDays(1), searchEnd);
            return occurrences.FirstOrDefault();
        }

        public override string ToString()
        {
            var recurring = IsRecurring ? $" (Recurring: {Pattern})" : "";
            return $"{Date:yyyy-MM-dd}: {Reason}{recurring}";
        }
    }
}
