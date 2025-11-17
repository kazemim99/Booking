using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Write repository implementation for ProviderAvailability aggregate
/// Handles slot updates, booking assignments, and conflict resolution
/// </summary>
public sealed class ProviderAvailabilityWriteRepository
    : EfWriteRepositoryBase<ProviderAvailability, Guid, ServiceCatalogDbContext>,
      IProviderAvailabilityWriteRepository
{
    private readonly ILogger<ProviderAvailabilityWriteRepository> _logger;

    public ProviderAvailabilityWriteRepository(
        ServiceCatalogDbContext context,
        ILogger<ProviderAvailabilityWriteRepository> logger)
        : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProviderAvailability?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<ProviderAvailability?> FindSlotAsync(
        ProviderId providerId,
        DateTime date,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken cancellationToken = default)
    {
        var dateOnly = date.Date;

        return await DbSet
            .FirstOrDefaultAsync(a =>
                a.ProviderId == providerId.Value &&
                a.Date == dateOnly &&
                a.StartTime == startTime &&
                a.EndTime == endTime,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderAvailability>> FindOverlappingSlotsAsync(
        ProviderId providerId,
        DateTime date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeSlotId = null,
        CancellationToken cancellationToken = default)
    {
        var dateOnly = date.Date;

        var query = DbSet
            .Where(a => a.ProviderId == providerId.Value &&
                       a.Date == dateOnly &&
                       a.StartTime < endTime &&
                       a.EndTime > startTime);

        if (excludeSlotId.HasValue)
        {
            query = query.Where(a => a.Id != excludeSlotId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(
        ProviderAvailability availability,
        CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(availability, cancellationToken);
    }

    public async Task UpdateAsync(
        ProviderAvailability availability,
        CancellationToken cancellationToken = default)
    {
        Context.Update(availability);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(
        ProviderAvailability availability,
        CancellationToken cancellationToken = default)
    {
        DbSet.Remove(availability);
        await Task.CompletedTask;
    }

    public async Task<int> ReleaseExpiredHoldsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var expiredHolds = await DbSet
            .Where(a => a.Status == AvailabilityStatus.TentativeHold &&
                       a.HoldExpiresAt.HasValue &&
                       a.HoldExpiresAt.Value <= now)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Releasing {Count} expired tentative holds", expiredHolds.Count);

        foreach (var hold in expiredHolds)
        {
            hold.ReleaseExpiredHold("ProviderAvailabilityWriteRepository.ReleaseExpiredHoldsAsync");
        }

        return expiredHolds.Count;
    }
}
