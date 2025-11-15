// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAvailabilityAggregate/ProviderAvailability.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate
{
    /// <summary>
    /// Represents availability for a specific time slot for a provider
    /// Used for booking calendar, availability heatmaps, and slot reservation
    /// </summary>
    public sealed class ProviderAvailability : AggregateRoot<Guid>, IAuditableEntity
    {
        // Core Identity
        public ProviderId ProviderId { get; private set; }
        
        // Optional: Staff member providing the service (null = any staff/owner)
        public Guid? StaffId { get; private set; }
        
        // Time Slot Definition
        public DateTime Date { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }
        
        // Availability Status
        public AvailabilityStatus Status { get; private set; }
        
        // Optional: Associated booking if status is Booked
        public Guid? BookingId { get; private set; }
        
        // Optional: Block reason if status is Blocked
        public string? BlockReason { get; private set; }
        
        // Optional: Hold expiration for TentativeHold status
        public DateTime? HoldExpiresAt { get; private set; }
        
        // Audit Properties
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        
        // Private constructor for EF Core
        private ProviderAvailability() : base() { }
        
        /// <summary>
        /// Creates an available time slot
        /// </summary>
        public static ProviderAvailability CreateAvailable(
            ProviderId providerId,
            DateTime date,
            TimeOnly startTime,
            TimeOnly endTime,
            Guid? staffId = null,
            string? createdBy = null)
        {
            ValidateTimeSlot(date, startTime, endTime);
            
            return new ProviderAvailability
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                StaffId = staffId,
                Date = date.Date, // Normalize to date only
                StartTime = startTime,
                EndTime = endTime,
                Status = AvailabilityStatus.Available,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
        }
        
        /// <summary>
        /// Creates a blocked time slot
        /// </summary>
        public static ProviderAvailability CreateBlocked(
            ProviderId providerId,
            DateTime date,
            TimeOnly startTime,
            TimeOnly endTime,
            string? blockReason = null,
            Guid? staffId = null,
            string? createdBy = null)
        {
            ValidateTimeSlot(date, startTime, endTime);
            
            return new ProviderAvailability
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                StaffId = staffId,
                Date = date.Date,
                StartTime = startTime,
                EndTime = endTime,
                Status = AvailabilityStatus.Blocked,
                BlockReason = blockReason,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
        }
        
        /// <summary>
        /// Creates a break period time slot
        /// </summary>
        public static ProviderAvailability CreateBreak(
            ProviderId providerId,
            DateTime date,
            TimeOnly startTime,
            TimeOnly endTime,
            Guid? staffId = null,
            string? createdBy = null)
        {
            ValidateTimeSlot(date, startTime, endTime);
            
            return new ProviderAvailability
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                StaffId = staffId,
                Date = date.Date,
                StartTime = startTime,
                EndTime = endTime,
                Status = AvailabilityStatus.Break,
                BlockReason = "Break period",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
        }
        
        /// <summary>
        /// Marks slot as booked with a booking ID
        /// </summary>
        public void MarkAsBooked(Guid bookingId, string? modifiedBy = null)
        {
            if (Status != AvailabilityStatus.Available && Status != AvailabilityStatus.TentativeHold)
                throw new DomainValidationException(
                    $"Cannot book slot with status {Status}. Only Available or TentativeHold slots can be booked.");
            
            Status = AvailabilityStatus.Booked;
            BookingId = bookingId;
            HoldExpiresAt = null; // Clear hold expiration
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Releases a booked slot back to available
        /// </summary>
        public void Release(string? modifiedBy = null)
        {
            if (Status != AvailabilityStatus.Booked && Status != AvailabilityStatus.TentativeHold)
                throw new DomainValidationException(
                    $"Cannot release slot with status {Status}. Only Booked or TentativeHold slots can be released.");
            
            Status = AvailabilityStatus.Available;
            BookingId = null;
            HoldExpiresAt = null;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Places a tentative hold on the slot (for booking in progress)
        /// </summary>
        public void PlaceTentativeHold(int holdDurationMinutes = 10, string? modifiedBy = null)
        {
            if (Status != AvailabilityStatus.Available)
                throw new DomainValidationException(
                    $"Cannot place hold on slot with status {Status}. Only Available slots can be held.");
            
            Status = AvailabilityStatus.TentativeHold;
            HoldExpiresAt = DateTime.UtcNow.AddMinutes(holdDurationMinutes);
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Blocks the slot with optional reason
        /// </summary>
        public void Block(string? reason = null, string? modifiedBy = null)
        {
            if (Status == AvailabilityStatus.Booked)
                throw new DomainValidationException(
                    "Cannot block a booked slot. Cancel the booking first.");
            
            Status = AvailabilityStatus.Blocked;
            BlockReason = reason;
            HoldExpiresAt = null;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Unblocks the slot
        /// </summary>
        public void Unblock(string? modifiedBy = null)
        {
            if (Status != AvailabilityStatus.Blocked)
                throw new DomainValidationException(
                    $"Cannot unblock slot with status {Status}. Only Blocked slots can be unblocked.");
            
            Status = AvailabilityStatus.Available;
            BlockReason = null;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Checks if the tentative hold has expired
        /// </summary>
        public bool IsHoldExpired()
        {
            return Status == AvailabilityStatus.TentativeHold 
                   && HoldExpiresAt.HasValue 
                   && HoldExpiresAt.Value < DateTime.UtcNow;
        }
        
        /// <summary>
        /// Releases expired holds (to be called by background job)
        /// </summary>
        public void ReleaseExpiredHold(string? modifiedBy = null)
        {
            if (IsHoldExpired())
            {
                Release(modifiedBy);
            }
        }
        
        /// <summary>
        /// Gets duration of the time slot in minutes
        /// </summary>
        public int GetDurationMinutes()
        {
            return (int)(EndTime - StartTime).TotalMinutes;
        }
        
        /// <summary>
        /// Checks if this slot conflicts with another slot
        /// </summary>
        public bool ConflictsWith(ProviderAvailability other)
        {
            if (Date.Date != other.Date.Date) return false;
            if (ProviderId != other.ProviderId) return false;
            if (StaffId.HasValue && other.StaffId.HasValue && StaffId != other.StaffId) return false;
            
            // Check time overlap
            return StartTime < other.EndTime && EndTime > other.StartTime;
        }
        
        /// <summary>
        /// Validates time slot parameters
        /// </summary>
        private static void ValidateTimeSlot(DateTime date, TimeOnly startTime, TimeOnly endTime)
        {
            if (startTime >= endTime)
                throw new DomainValidationException("Start time must be before end time");
            
            if (date.Date < DateTime.UtcNow.Date)
                throw new DomainValidationException("Cannot create availability for past dates");
            
            var duration = (endTime - startTime).TotalMinutes;
            if (duration < 15)
                throw new DomainValidationException("Time slot must be at least 15 minutes");
            
            if (duration > 480) // 8 hours
                throw new DomainValidationException("Time slot cannot exceed 8 hours");
        }
        
        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd} {StartTime:HH:mm}-{EndTime:HH:mm} [{Status}]";
        }
    }
}
