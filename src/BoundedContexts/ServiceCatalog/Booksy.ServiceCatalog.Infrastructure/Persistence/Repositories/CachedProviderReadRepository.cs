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
    public sealed class CachedProviderReadRepository : IProviderReadRepository
    {
        private readonly IProviderReadRepository _inner;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedProviderReadRepository> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

        public CachedProviderReadRepository(
            IProviderReadRepository inner,
            IMemoryCache cache,
            ILogger<CachedProviderReadRepository> logger)
        {
            _inner = inner;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Provider?> GetByIdAsync(ProviderId id, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"provider:id:{id.Value}";

            if (_cache.TryGetValue(cacheKey, out Provider? cachedProvider))
            {
                _logger.LogDebug("Cache hit for provider {ProviderId}", id);
                return cachedProvider;
            }

            var provider = await _inner.GetByIdAsync(id, cancellationToken);
            if (provider != null)
            {
                _cache.Set(cacheKey, provider, _cacheDuration);
                _logger.LogDebug("Cached provider {ProviderId}", id);
            }

            return provider;
        }

        public async Task<Provider?> GetByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"provider:owner:{ownerId.Value}";

            if (_cache.TryGetValue(cacheKey, out Provider? cachedProvider))
            {
                _logger.LogDebug("Cache hit for provider by owner {OwnerId}", ownerId);
                return cachedProvider;
            }

            var provider = await _inner.GetByOwnerIdAsync(ownerId, cancellationToken);
            if (provider != null)
            {
                _cache.Set(cacheKey, provider, _cacheDuration);
                _logger.LogDebug("Cached provider by owner {OwnerId}", ownerId);
            }

            return provider;
        }

        // Other methods delegate directly to inner repository (not cached for complexity)
        public Task<Provider?> GetByBusinessNameAsync(string businessName, CancellationToken cancellationToken = default) =>
            _inner.GetByBusinessNameAsync(businessName, cancellationToken);

        public Task<IReadOnlyList<Provider>> GetByStatusAsync(ProviderStatus status, CancellationToken cancellationToken = default) =>
            _inner.GetByStatusAsync(status, cancellationToken);

        public Task<IReadOnlyList<Provider>> GetByTypeAsync(ProviderType type, CancellationToken cancellationToken = default) =>
            _inner.GetByTypeAsync(type, cancellationToken);

        public Task<IReadOnlyList<Provider>> GetByCityAsync(string city, CancellationToken cancellationToken = default) =>
            _inner.GetByCityAsync(city, cancellationToken);

        public Task<IReadOnlyList<Provider>> GetByLocationAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default) =>
            _inner.GetByLocationAsync(latitude, longitude, radiusKm, cancellationToken);

        public Task<IReadOnlyList<Provider>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default) =>
            _inner.SearchAsync(searchTerm, cancellationToken);

        public Task<PagedResult<Provider>> GetPaginatedAsync(int pageNumber, int pageSize, ProviderStatus? status = null, ProviderType? type = null, CancellationToken cancellationToken = default) =>
            _inner.GetPaginatedAsync(pageNumber, pageSize, status, type, cancellationToken);

        public Task<bool> ExistsByBusinessNameAsync(string businessName, ProviderId? excludeId = null, CancellationToken cancellationToken = default) =>
            _inner.ExistsByBusinessNameAsync(businessName, excludeId, cancellationToken);

        public Task<bool> ExistsByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default) =>
            _inner.ExistsByOwnerIdAsync(ownerId, cancellationToken);

        public Task<int> CountByStatusAsync(ProviderStatus status, CancellationToken cancellationToken = default) =>
            _inner.CountByStatusAsync(status, cancellationToken);

        public Task<IReadOnlyList<Provider>> GetRecentlyActiveAsync(int count, CancellationToken cancellationToken = default) =>
            _inner.GetRecentlyActiveAsync(count, cancellationToken);

        // IReadRepository implementation
        public Task<Provider?> GetSingleAsync(ISpecification<Provider> specification, CancellationToken cancellationToken = default) =>
            _inner.GetSingleAsync(specification, cancellationToken);

        public Task<IReadOnlyList<Provider>> GetAsync(ISpecification<Provider> specification, CancellationToken cancellationToken = default) =>
            _inner.GetAsync(specification, cancellationToken);

        public Task<bool> ExistsAsync(ISpecification<Provider> specification, CancellationToken cancellationToken = default) =>
            _inner.ExistsAsync(specification, cancellationToken);

        public Task<int> CountAsync(ISpecification<Provider> specification, CancellationToken cancellationToken = default) =>
            _inner.CountAsync(specification, cancellationToken);

        public Task<PagedResult<Provider>> GetPaginatedAsync(ISpecification<Provider> specification, PaginationRequest pagination, CancellationToken cancellationToken = default) =>
            _inner.GetPaginatedAsync(specification, pagination, cancellationToken);

        public Task<PagedResult<TProjection>> GetPaginatedAsync<TProjection>(ISpecification<Provider> specification, PaginationRequest pagination, System.Linq.Expressions.Expression<Func<Provider, TProjection>> projection, CancellationToken cancellationToken = default) =>
            _inner.GetPaginatedAsync(specification, pagination, projection, cancellationToken);
    }
}
