using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.DTOs;
using Booksy.Infrastructure.Core.Caching;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Cached decorator for ProviderReadRepository using Redis distributed cache
    /// Caches GetByIdAsync and GetByOwnerIdAsync for improved performance
    /// </summary>
    public sealed class CachedProviderReadRepository : IProviderReadRepository
    {
        private readonly IProviderReadRepository _inner;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachedProviderReadRepository> _logger;
        private readonly TimeSpan _cacheDuration;

        public CachedProviderReadRepository(
            IProviderReadRepository inner,
            ICacheService cacheService,
            IOptions<CacheSettings> cacheSettings,
            ILogger<CachedProviderReadRepository> logger)
        {
            _inner = inner;
            _cacheService = cacheService;
            _logger = logger;

            // Use configured expiration or default to 15 minutes
            var expirationMinutes = cacheSettings.Value.DefaultExpirationMinutes > 0
                ? cacheSettings.Value.DefaultExpirationMinutes
                : 15;
            _cacheDuration = TimeSpan.FromMinutes(expirationMinutes);
        }

        public async Task<Provider?> GetByIdAsync(ProviderId id, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"Provider:{id.Value}";

            return await _cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogDebug("Cache miss for provider {ProviderId}", id);
                    var provider = await _inner.GetByIdAsync(id, cancellationToken);

                    if (provider != null)
                    {
                        _logger.LogDebug("Cached provider {ProviderId}", id);
                    }

                    return provider;
                },
                _cacheDuration,
                cancellationToken);
        }

        public async Task<Provider?> GetByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"Provider:owner:{ownerId.Value}";

            return await _cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogDebug("Cache miss for provider by owner {OwnerId}", ownerId);
                    var provider = await _inner.GetByOwnerIdAsync(ownerId, cancellationToken);

                    if (provider != null)
                    {
                        _logger.LogDebug("Cached provider by owner {OwnerId}", ownerId);
                    }

                    return provider;
                },
                _cacheDuration,
                cancellationToken);
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

        // Hierarchy-related methods (delegating to inner repository)
        public Task<IReadOnlyList<Provider>> GetStaffByOrganizationIdAsync(ProviderId organizationId, CancellationToken cancellationToken = default) =>
            _inner.GetStaffByOrganizationIdAsync(organizationId, cancellationToken);

        public Task<Provider?> GetOrganizationByStaffIdAsync(ProviderId staffProviderId, CancellationToken cancellationToken = default) =>
            _inner.GetOrganizationByStaffIdAsync(staffProviderId, cancellationToken);

        public Task<IReadOnlyList<Provider>> GetByHierarchyTypeAsync(ProviderHierarchyType hierarchyType, CancellationToken cancellationToken = default) =>
            _inner.GetByHierarchyTypeAsync(hierarchyType, cancellationToken);

        public Task<IReadOnlyList<Provider>> GetIndependentIndividualsAsync(CancellationToken cancellationToken = default) =>
            _inner.GetIndependentIndividualsAsync(cancellationToken);

        public Task<int> CountStaffByOrganizationAsync(ProviderId organizationId, CancellationToken cancellationToken = default) =>
            _inner.CountStaffByOrganizationAsync(organizationId, cancellationToken);
    }
}
