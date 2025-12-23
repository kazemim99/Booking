using Booksy.Core.Application.DTOs;
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
    public sealed class ServiceReadRepository : EfReadRepositoryBase<Service, ServiceId, ServiceCatalogDbContext>, IServiceReadRepository
    {
        public ServiceReadRepository(
            ServiceCatalogDbContext context,
            ILogger<ServiceReadRepository> logger)
            : base(context)
        {
        }

        public async Task<IReadOnlyList<Service>> GetByProviderIdAsync(ProviderId providerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .Where(s => s.ProviderId == providerId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Service>> GetByProviderIdAndStatusAsync(ProviderId providerId, ServiceStatus status, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .Where(s => s.ProviderId == providerId && s.Status == status)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Service>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .Where(s => s.Category.ToString() == category)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Service>> GetByStatusAsync(ServiceStatus status, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .Where(s => s.Status == status)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Service>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var normalizedSearchTerm = searchTerm.ToLower().Trim();

            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .Where(s =>
                    s.Name.ToLower().Contains(normalizedSearchTerm) ||
                    s.Description.ToLower().Contains(normalizedSearchTerm))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Service>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, string currency, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .Where(s => s.BasePrice.Currency == currency &&
                           s.BasePrice.Amount >= minPrice &&
                           s.BasePrice.Amount <= maxPrice)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Service>> GetByDurationRangeAsync(int minMinutes, int maxMinutes, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .Where(s => s.Duration.Value >= minMinutes && s.Duration.Value <= maxMinutes)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Service>> GetMobileServicesAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .Where(s => s.AvailableAsMobile)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Service>> GetServicesRequiringDepositAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .Where(s => s.RequiresDeposit)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Service>> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            ServiceStatus? status = null,
            ServiceCategory? category = null,
            ProviderId? providerId = null,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(s => s.Status == status.Value);

            if (category != null)
                query = query.Where(s => s.Category == category);

            if (providerId != null)
                query = query.Where(s => s.ProviderId == providerId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Service>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<bool> ExistsWithNameForProviderAsync(ProviderId providerId, string serviceName, ServiceId? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = DbSet.Where(s => s.ProviderId == providerId && s.Name == serviceName);

            if (excludeId != null)
                query = query.Where(s => s.Id != excludeId.Value);

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<int> CountByProviderAsync(ProviderId providerId, ServiceStatus? status = null, CancellationToken cancellationToken = default)
        {
            var query = DbSet.Where(s => s.ProviderId == providerId);

            if (status.HasValue)
                query = query.Where(s => s.Status == status.Value);

            return await query.CountAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Service>> GetPopularServicesAsync(int count, CancellationToken cancellationToken = default)
        {
            // Note: This is a simplified implementation. In a real scenario, 
            // you might want to join with booking data to determine popularity
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .Where(s => s.Status == ServiceStatus.Active)
                .OrderBy(s => s.Name) // Placeholder ordering - replace with actual popularity metric
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Service>> GetServicesByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(s => s.Options)
                .Include(s => s.PriceTiers)
                .ToListAsync(cancellationToken);
        }

        public async Task<decimal> GetAveragePriceByCategoryAsync(string category, string currency, CancellationToken cancellationToken = default)
        {
            var services = await DbSet
                .Where(s => s.Category.ToString() == category && s.BasePrice.Currency == currency)
                .Select(s => s.BasePrice.Amount)
                .ToListAsync(cancellationToken);

            return services.Any() ? services.Average() : 0m;
        }
    }
}
