using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Infrastructure.Core.Persistence.Base;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProviderReadRepository : EfReadRepositoryBase<Provider, ProviderId, ServiceCatalogDbContext>, IProviderReadRepository
    {
        public ProviderReadRepository(
            ServiceCatalogDbContext context,
            ILogger<ProviderReadRepository> logger)
            : base(context)
        {
        }

        public override async Task<Provider?> GetByIdAsync(ProviderId id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.BusinessHours)
                .Include(p => p.Services)
                .Include(p => p.Profile)
                // Without these two, a provider loaded through this method always reports zero
                // holidays and zero exceptions, regardless of what is in the database:
                // AvailabilityService.IsHoliday/GetExceptionSchedule read the in-memory collection,
                // not the table. Both features were completely inert in production — a provider
                // could mark a holiday and every slot on that day would still show as available.
                // Found porting the Reqnroll "No availability on holidays" / "Exception hours
                // override regular business hours" scenarios to xUnit (retire-reqnroll).
                .Include(p => p.Holidays)
                .Include(p => p.Exceptions)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Provider?> GetByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.BusinessHours)
                .FirstOrDefaultAsync(p => p.OwnerId == ownerId, cancellationToken);
        }

        public async Task<Provider?> GetByBusinessNameAsync(string businessName, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.BusinessHours)
                .FirstOrDefaultAsync(p => p.Profile.BusinessName == businessName, cancellationToken);
        }

        public async Task<IReadOnlyList<Provider>> GetByStatusAsync(ProviderStatus status, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.BusinessHours)
                .Where(p => p.Status == status)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Provider>> GetByCategoryAsync(ServiceCategory category, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.BusinessHours)
                .Where(p => p.PrimaryCategory == category)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Provider>> GetByCityAsync(string city, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.BusinessHours)
                .Where(p => p.Address.City == city)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Provider>> GetByLocationAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default)
        {
            // Using Haversine formula for distance calculation
            return await DbSet
                .Include(p => p.BusinessHours)
                .Where(p => p.Address.Latitude != null && p.Address.Longitude != null)
                .Where(p =>
                    6371 * Math.Acos(
                        Math.Cos(Math.PI * latitude / 180) *
                        Math.Cos(Math.PI * p.Address.Latitude!.Value / 180) *
                        Math.Cos(Math.PI * p.Address.Longitude!.Value / 180 - Math.PI * longitude / 180) +
                        Math.Sin(Math.PI * latitude / 180) *
                        Math.Sin(Math.PI * p.Address.Latitude!.Value / 180)
                    ) <= radiusKm)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Provider>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var normalizedSearchTerm = searchTerm.ToLower().Trim();

            return await DbSet
                .Include(p => p.BusinessHours)
                .Where(p =>
                    p.Profile.BusinessName.ToLower().Contains(normalizedSearchTerm) ||
                    p.Profile.BusinessDescription.ToLower().Contains(normalizedSearchTerm) ||
                    p.Address.City.ToLower().Contains(normalizedSearchTerm))
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Provider>> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            ProviderStatus? status = null,
            ServiceCategory? type = null,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet
                .Include(p => p.BusinessHours)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (type.HasValue)
                query = query.Where(p => p.PrimaryCategory == type.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Provider>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<bool> ExistsByBusinessNameAsync(string businessName, ProviderId? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = DbSet.Where(p => p.Profile.BusinessName == businessName);

            if (excludeId != null)
                query = query.Where(p => p.Id != excludeId.Value);

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<bool> ExistsByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default)
        {
            return await DbSet.AnyAsync(p => p.OwnerId == ownerId, cancellationToken);
        }

        public async Task<int> CountByStatusAsync(ProviderStatus status, CancellationToken cancellationToken = default)
        {
            return await DbSet.CountAsync(p => p.Status == status, cancellationToken);
        }

        public async Task<IReadOnlyDictionary<ServiceCategory, int>> CountByCategoryAsync(
            ProviderStatus status,
            CancellationToken cancellationToken = default)
        {
            // GROUP BY runs in the database — the category browse page must not scale with the
            // number of providers. Served by IX_Providers_PrimaryCategory.
            var counts = await DbSet
                .Where(p => p.Status == status)
                .GroupBy(p => p.PrimaryCategory)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return counts.ToDictionary(x => x.Category, x => x.Count);
        }

        public async Task<IReadOnlyList<Provider>> GetRecentlyActiveAsync(int count, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.BusinessHours)
                .Where(p => p.LastActiveAt.HasValue)
                .OrderByDescending(p => p.LastActiveAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }


    }
}
