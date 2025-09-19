using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class CachedServiceReadRepository : IServiceReadRepository
    {
        private readonly IServiceReadRepository _inner;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedServiceReadRepository> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public CachedServiceReadRepository(
            IServiceReadRepository inner,
            IMemoryCache cache,
            ILogger<CachedServiceReadRepository> logger)
        {
            _inner = inner;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"service:id:{id.Value}";

            if (_cache.TryGetValue(cacheKey, out Service? cachedService))
            {
                _logger.LogDebug("Cache hit for service {ServiceId}", id);
                return cachedService;
            }

            var service = await _inner.GetByIdAsync(id, cancellationToken);
            if (service != null)
            {
                _cache.Set(cacheKey, service, _cacheDuration);
                _logger.LogDebug("Cached service {ServiceId}", id);
            }

            return service;
        }

        // Other methods delegate directly to inner repository
        public Task<IReadOnlyList<Service>> GetByProviderIdAsync(ProviderId providerId, CancellationToken cancellationToken = default) =>
            _inner.GetByProviderIdAsync(providerId, cancellationToken);

        public Task<IReadOnlyList<Service>> GetByProviderIdAndStatusAsync(ProviderId providerId, ServiceStatus status, CancellationToken cancellationToken = default) =>
            _inner.GetByProviderIdAndStatusAsync(providerId, status, cancellationToken);

        public Task<IReadOnlyList<Service>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default) =>
            _inner.GetByCategoryAsync(category, cancellationToken);

        public Task<IReadOnlyList<Service>> GetByStatusAsync(ServiceStatus status, CancellationToken cancellationToken = default) =>
            _inner.GetByStatusAsync(status, cancellationToken);

        public Task<IReadOnlyList<Service>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default) =>
            _inner.SearchAsync(searchTerm, cancellationToken);

        public Task<IReadOnlyList<Service>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, string currency, CancellationToken cancellationToken = default) =>
            _inner.GetByPriceRangeAsync(minPrice, maxPrice, currency, cancellationToken);

        public Task<IReadOnlyList<Service>> GetByDurationRangeAsync(int minMinutes, int maxMinutes, CancellationToken cancellationToken = default) =>
            _inner.GetByDurationRangeAsync(minMinutes, maxMinutes, cancellationToken);

        public Task<IReadOnlyList<Service>> GetMobileServicesAsync(CancellationToken cancellationToken = default) =>
            _inner.GetMobileServicesAsync(cancellationToken);

        public Task<IReadOnlyList<Service>> GetServicesRequiringDepositAsync(CancellationToken cancellationToken = default) =>
            _inner.GetServicesRequiringDepositAsync(cancellationToken);

        public Task<PagedResult<Service>> GetPaginatedAsync(int pageNumber, int pageSize, ServiceStatus? status = null, ServiceCategory? category = null, ProviderId? providerId = null, CancellationToken cancellationToken = default) =>
            _inner.GetPaginatedAsync(pageNumber, pageSize, status, category, providerId, cancellationToken);

        public Task<bool> ExistsWithNameForProviderAsync(ProviderId providerId, string serviceName, ServiceId? excludeId = null, CancellationToken cancellationToken = default) =>
            _inner.ExistsWithNameForProviderAsync(providerId, serviceName, excludeId, cancellationToken);

        public Task<int> CountByProviderAsync(ProviderId providerId, ServiceStatus? status = null, CancellationToken cancellationToken = default) =>
            _inner.CountByProviderAsync(providerId, status, cancellationToken);

        public Task<IReadOnlyList<Service>> GetPopularServicesAsync(int count, CancellationToken cancellationToken = default) =>
            _inner.GetPopularServicesAsync(count, cancellationToken);

        public Task<IReadOnlyList<Service>> GetServicesByTagAsync(string tag, CancellationToken cancellationToken = default) =>
            _inner.GetServicesByTagAsync(tag, cancellationToken);

        public Task<decimal> GetAveragePriceByCategoryAsync(string category, string currency, CancellationToken cancellationToken = default) =>
            _inner.GetAveragePriceByCategoryAsync(category, currency, cancellationToken);

        // IReadRepository implementation
        public Task<Service?> GetSingleAsync(ISpecification<Service> specification, CancellationToken cancellationToken = default) =>
            _inner.GetSingleAsync(specification, cancellationToken);

        public Task<IReadOnlyList<Service>> GetAsync(ISpecification<Service> specification, CancellationToken cancellationToken = default) =>
            _inner.GetAsync(specification, cancellationToken);

        public Task<bool> ExistsAsync(ISpecification<Service> specification, CancellationToken cancellationToken = default) =>
            _inner.ExistsAsync(specification, cancellationToken);

        public Task<int> CountAsync(ISpecification<Service> specification, CancellationToken cancellationToken = default) =>
            _inner.CountAsync(specification, cancellationToken);

        public Task<PagedResult<Service>> GetPaginatedAsync(ISpecification<Service> specification, PaginationRequest pagination, CancellationToken cancellationToken = default) =>
            _inner.GetPaginatedAsync(specification, pagination, cancellationToken);

        public Task<PagedResult<TProjection>> GetPaginatedAsync<TProjection>(ISpecification<Service> specification, PaginationRequest pagination, System.Linq.Expressions.Expression<Func<Service, TProjection>> projection, CancellationToken cancellationToken = default) =>
            _inner.GetPaginatedAsync(specification, pagination, projection, cancellationToken);
    }
}
