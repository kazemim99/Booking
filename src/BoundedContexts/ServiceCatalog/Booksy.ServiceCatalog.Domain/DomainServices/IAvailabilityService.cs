// ========================================
// Booksy.ServiceCatalog.Domain/DomainServices/IAvailabilityService.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.DomainServices
{
    /// <summary>
    /// Domain service for checking availability and generating time slots
    /// </summary>
    public interface IAvailabilityService
    {
        /// <summary>
        /// Get available time slots for a service at a provider on a specific date
        /// </summary>
        /// <param name="provider">The provider offering the service</param>
        /// <param name="service">The service to be booked</param>
        /// <param name="date">The date to check availability</param>
        /// <param name="staff">Optional specific staff member</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available time slots</returns>
        Task<IReadOnlyList<AvailableTimeSlot>> GetAvailableTimeSlotsAsync(
            Provider provider,
            Service service,
            DateTime date,
            Staff? staff = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a specific time slot is available for booking
        /// </summary>
        /// <param name="provider">The provider</param>
        /// <param name="service">The service</param>
        /// <param name="staff">The staff member</param>
        /// <param name="startTime">Requested start time</param>
        /// <param name="duration">Service duration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the time slot is available</returns>
        Task<bool> IsTimeSlotAvailableAsync(
            Provider provider,
            Service service,
            Staff staff,
            DateTime startTime,
            Duration duration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get available staff for a service at a specific time
        /// </summary>
        /// <param name="provider">The provider</param>
        /// <param name="service">The service</param>
        /// <param name="startTime">Desired start time</param>
        /// <param name="duration">Service duration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available qualified staff</returns>
        Task<IReadOnlyList<Staff>> GetAvailableStaffAsync(
            Provider provider,
            Service service,
            DateTime startTime,
            Duration duration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate booking constraints (business hours, holidays, minimum/maximum advance booking)
        /// </summary>
        /// <param name="provider">The provider</param>
        /// <param name="service">The service</param>
        /// <param name="startTime">Desired start time</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with any errors</returns>
        Task<AvailabilityValidationResult> ValidateBookingConstraintsAsync(
            Provider provider,
            Service service,
            DateTime startTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate date-level constraints (holidays, day of week, max advance days) without time checks
        /// Used when checking if a date has any availability before generating time slots
        /// </summary>
        /// <param name="provider">The provider</param>
        /// <param name="service">The service</param>
        /// <param name="date">The date to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with any errors</returns>
        Task<AvailabilityValidationResult> ValidateDateConstraintsAsync(
            Provider provider,
            Service service,
            DateTime date,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents an available time slot with staff information
    /// </summary>
    public sealed record AvailableTimeSlot(
        DateTime StartTime,
        DateTime EndTime,
        Duration Duration,
        Guid StaffId,
        string StaffName);

    /// <summary>
    /// Result of availability validation
    /// </summary>
    public sealed record AvailabilityValidationResult(
        bool IsValid,
        List<string> Errors)
    {
        public static AvailabilityValidationResult Success() => new(true, new List<string>());
        public static AvailabilityValidationResult Failure(params string[] errors) => new(false, errors.ToList());
    }
}
