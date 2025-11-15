using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories;

/// <summary>
/// Write repository for managing ProviderAvailability aggregate
/// Used for marking slots as booked, blocked, or available
/// </summary>
public interface IProviderAvailabilityWriteRepository : IWriteRepository<ProviderAvailability, Guid>
{
    /// <summary>
    /// Get availability slot by ID for updating
    /// </summary>
    Task<ProviderAvailability?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find a specific availability slot by provider, date, and time
    /// Used for marking slots as booked during booking creation
    /// </summary>
    Task<ProviderAvailability?> FindSlotAsync(
        ProviderId providerId,
        DateTime date,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find overlapping availability slots for a time range
    /// Used for conflict detection
    /// </summary>
    Task<IReadOnlyList<ProviderAvailability>> FindOverlappingSlotsAsync(
        ProviderId providerId,
        DateTime date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeSlotId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a new availability slot
    /// </summary>
    Task SaveAsync(ProviderAvailability availability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing availability slot
    /// </summary>
    Task UpdateAsync(ProviderAvailability availability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an availability slot
    /// </summary>
    Task DeleteAsync(ProviderAvailability availability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Release expired tentative holds
    /// Background job will call this periodically
    /// </summary>
    Task<int> ReleaseExpiredHoldsAsync(CancellationToken cancellationToken = default);
}
