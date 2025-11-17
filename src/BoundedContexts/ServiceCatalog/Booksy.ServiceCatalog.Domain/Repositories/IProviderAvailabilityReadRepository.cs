using Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories;

/// <summary>
/// Read repository for querying provider availability data
/// Used for booking calendar and availability heatmap queries
/// </summary>
public interface IProviderAvailabilityReadRepository
{
    /// <summary>
    /// Get all availability slots for a provider within a date range
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of availability slots ordered by date and time</returns>
    Task<IReadOnlyList<ProviderAvailability>> GetAvailabilityByDateRangeAsync(
        ProviderId providerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get availability slots for a specific date
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="date">Specific date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of availability slots for the date</returns>
    Task<IReadOnlyList<ProviderAvailability>> GetAvailabilityByDateAsync(
        ProviderId providerId,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available (bookable) slots for a provider within a date range
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available slots only</returns>
    Task<IReadOnlyList<ProviderAvailability>> GetAvailableSlotsAsync(
        ProviderId providerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get availability slots by status
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="status">Availability status</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of slots with specified status</returns>
    Task<IReadOnlyList<ProviderAvailability>> GetSlotsByStatusAsync(
        ProviderId providerId,
        AvailabilityStatus status,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if provider has any available slots in the date range
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any available slots exist</returns>
    Task<bool> HasAvailableSlotsAsync(
        ProviderId providerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get availability statistics for a provider in a date range
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Availability statistics (total, available, booked, blocked counts)</returns>
    Task<AvailabilityStatistics> GetAvailabilityStatisticsAsync(
        ProviderId providerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Availability statistics for a date range
/// </summary>
public record AvailabilityStatistics(
    int TotalSlots,
    int AvailableSlots,
    int BookedSlots,
    int BlockedSlots,
    int BreakSlots);
