using Booksy.ServiceCatalog.Domain.Aggregates.ProviderAvailabilityAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Read repository implementation for ProviderAvailability aggregate
/// Provides optimized queries for availability calendar and booking functionality
/// </summary>
public sealed class ProviderAvailabilityReadRepository : IProviderAvailabilityReadRepository
{
    private readonly ServiceCatalogDbContext _context;
    private readonly ILogger<ProviderAvailabilityReadRepository> _logger;

    public ProviderAvailabilityReadRepository(
        ServiceCatalogDbContext context,
        ILogger<ProviderAvailabilityReadRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<ProviderAvailability>> GetAvailabilityByDateRangeAsync(
        ProviderId providerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Fetching availability for provider {ProviderId} from {StartDate} to {EndDate}",
            providerId.Value,
            startDate,
            endDate);

        return await _context.ProviderAvailability
            .AsNoTracking()
            .Where(a => a.ProviderId == providerId.Value &&
                       a.Date >= startDate &&
                       a.Date <= endDate)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderAvailability>> GetAvailabilityByDateAsync(
        ProviderId providerId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var dateOnly = date.Date;

        return await _context.ProviderAvailability
            .AsNoTracking()
            .Where(a => a.ProviderId == providerId.Value &&
                       a.Date == dateOnly)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderAvailability>> GetAvailableSlotsAsync(
        ProviderId providerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProviderAvailability
            .AsNoTracking()
            .Where(a => a.ProviderId == providerId.Value &&
                       a.Date >= startDate &&
                       a.Date <= endDate &&
                       a.Status == AvailabilityStatus.Available)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderAvailability>> GetSlotsByStatusAsync(
        ProviderId providerId,
        AvailabilityStatus status,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProviderAvailability
            .AsNoTracking()
            .Where(a => a.ProviderId == providerId.Value &&
                       a.Date >= startDate &&
                       a.Date <= endDate &&
                       a.Status == status)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasAvailableSlotsAsync(
        ProviderId providerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProviderAvailability
            .AsNoTracking()
            .AnyAsync(a => a.ProviderId == providerId.Value &&
                          a.Date >= startDate &&
                          a.Date <= endDate &&
                          a.Status == AvailabilityStatus.Available,
                     cancellationToken);
    }

    public async Task<AvailabilityStatistics> GetAvailabilityStatisticsAsync(
        ProviderId providerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var slots = await _context.ProviderAvailability
            .AsNoTracking()
            .Where(a => a.ProviderId == providerId.Value &&
                       a.Date >= startDate &&
                       a.Date <= endDate)
            .ToListAsync(cancellationToken);

        var totalSlots = slots.Count;
        var availableSlots = slots.Count(s => s.Status == AvailabilityStatus.Available);
        var bookedSlots = slots.Count(s => s.Status == AvailabilityStatus.Booked);
        var blockedSlots = slots.Count(s => s.Status == AvailabilityStatus.Blocked);
        var breakSlots = slots.Count(s => s.Status == AvailabilityStatus.Break);

        return new AvailabilityStatistics(
            TotalSlots: totalSlots,
            AvailableSlots: availableSlots,
            BookedSlots: bookedSlots,
            BlockedSlots: blockedSlots,
            BreakSlots: breakSlots);
    }
}
