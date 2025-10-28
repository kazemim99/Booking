// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/ExceptionSchedule.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Entities
{
    /// <summary>
    /// Entity representing a temporary schedule override for a specific date
    /// Converted from ValueObject to support mutability, audit trail, and lifecycle management
    /// </summary>
    public sealed class ExceptionSchedule : Entity<Guid>
    {
        public DateOnly Date { get; private set; }
        public TimeOnly? OpenTime { get; private set; }
        public TimeOnly? CloseTime { get; private set; }
        public string Reason { get; private set; }
        public ProviderId ProviderId { get; private set; }

        /// <summary>
        /// Indicates whether the business is closed on this exception date
        /// </summary>
        public bool IsClosed => !OpenTime.HasValue || !CloseTime.HasValue;

        // Foreign key to Provider (set by EF Core)

        // Private constructor for EF Core
        private ExceptionSchedule() : base()
        {
            Reason = string.Empty;
        }

        private ExceptionSchedule(ProviderId providerId,DateOnly date, TimeOnly? openTime, TimeOnly? closeTime, string reason, string? createdBy)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainValidationException("Exception reason is required");

            // If times are provided, validate them
            if (openTime.HasValue && closeTime.HasValue)
            {
                if (openTime >= closeTime)
                    throw new DomainValidationException("Open time must be before close time");
            }

            // Both must be null or both must have values
            if (openTime.HasValue != closeTime.HasValue)
                throw new DomainValidationException("Both open and close times must be provided, or both must be null for closure");

            Id = Guid.NewGuid();
            Date = date;
            OpenTime = openTime;
            CloseTime = closeTime;
            Reason = reason.Trim();
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy;
            ProviderId = providerId;
        }

        /// <summary>
        /// Creates an exception with modified hours for a specific date
        /// </summary>
        internal static ExceptionSchedule CreateWithModifiedHours(ProviderId providerId,DateOnly date, TimeOnly openTime, TimeOnly closeTime, string reason, string? createdBy = null)
        {
            return new ExceptionSchedule(providerId,date, openTime, closeTime, reason, createdBy);
        }

        /// <summary>
        /// Creates an exception marking a date as closed
        /// </summary>
        internal static ExceptionSchedule CreateClosed(ProviderId providerId, DateOnly date, string reason, string? createdBy = null)
        {
            return new ExceptionSchedule(providerId,date, openTime: null, closeTime: null, reason, createdBy);
        }

        /// <summary>
        /// Updates the exception details
        /// </summary>
        public void Update(DateOnly date, TimeOnly? openTime, TimeOnly? closeTime, string reason, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainValidationException("Exception reason is required");

            // If times are provided, validate them
            if (openTime.HasValue && closeTime.HasValue)
            {
                if (openTime >= closeTime)
                    throw new DomainValidationException("Open time must be before close time");
            }

            // Both must be null or both must have values
            if (openTime.HasValue != closeTime.HasValue)
                throw new DomainValidationException("Both open and close times must be provided, or both must be null for closure");

            Date = date;
            OpenTime = openTime;
            CloseTime = closeTime;
            Reason = reason.Trim();
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Updates only the hours while keeping the same date and reason
        /// </summary>
        public void UpdateHours(TimeOnly openTime, TimeOnly closeTime, string? modifiedBy = null)
        {
            if (openTime >= closeTime)
                throw new DomainValidationException("Open time must be before close time");

            OpenTime = openTime;
            CloseTime = closeTime;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Marks the exception date as closed
        /// </summary>
        public void SetClosed(string? modifiedBy = null)
        {
            OpenTime = null;
            CloseTime = null;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Marks the exception as deleted (soft delete)
        /// </summary>
        public void Delete(string? deletedBy = null)
        {
            IsDeleted = true;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = deletedBy;
        }

        /// <summary>
        /// Gets the duration of operating hours for this exception
        /// Returns null if closed
        /// </summary>
        public Duration? GetDuration()
        {
            if (IsClosed) return null;

            var totalMinutes = (int)(CloseTime!.Value - OpenTime!.Value).TotalMinutes;
            return Duration.FromMinutes(totalMinutes);
        }

        /// <summary>
        /// Checks if a specific time falls within exception hours
        /// Returns false if closed
        /// </summary>
        public bool Contains(TimeOnly time)
        {
            if (IsClosed) return false;
            return time >= OpenTime && time <= CloseTime;
        }

        /// <summary>
        /// Checks if this exception conflicts with a holiday on the same date
        /// </summary>
        public bool ConflictsWith(HolidaySchedule holiday)
        {
            return holiday.OccursOn(Date);
        }

        public override string ToString()
        {
            if (IsClosed)
                return $"{Date:yyyy-MM-dd}: Closed - {Reason}";

            return $"{Date:yyyy-MM-dd}: {OpenTime:HH:mm} - {CloseTime:HH:mm} - {Reason}";
        }
    }
}
