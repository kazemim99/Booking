using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProviderReadRepository : EfReadRepositoryBase<Provider, ProviderId, ServiceCatalogDbContext>, IProviderReadRepository
    {
        public ProviderReadRepository(
            ServiceCatalogDbContext context,
            ILogger<ProviderReadRepository> logger)
            : base(context)
        {
        }

        public async Task<Provider?> GetByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .FirstOrDefaultAsync(p => p.OwnerId == ownerId, cancellationToken);
        }

        public async Task<Provider?> GetByBusinessNameAsync(string businessName, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .FirstOrDefaultAsync(p => p.Profile.BusinessName == businessName, cancellationToken);
        }

        public async Task<IReadOnlyList<Provider>> GetByStatusAsync(ProviderStatus status, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .Where(p => p.Status == status)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Provider>> GetByTypeAsync(ProviderType type, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .Where(p => p.ProviderType == type)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Provider>> GetByCityAsync(string city, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .Where(p => p.Address.City == city)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Provider>> GetByLocationAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default)
        {
            // Using Haversine formula for distance calculation
            return await DbSet
                .Include(p => p.Staff)
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
                .Include(p => p.Staff)
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
            ProviderType? type = null,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (type.HasValue)
                query = query.Where(p => p.ProviderType == type.Value);

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

        public async Task<IReadOnlyList<Provider>> GetRecentlyActiveAsync(int count, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Staff)
                .Include(p => p.BusinessHours)
                .Where(p => p.LastActiveAt.HasValue)
                .OrderByDescending(p => p.LastActiveAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }
    }
}
