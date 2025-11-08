// ========================================
// Caching/CachedRepositoryDecorator.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Abstractions.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace Booksy.Infrastructure.Core.Caching;

/// <summary>
/// Generic caching decorator for read repositories using Redis distributed cache
/// Implements the Decorator pattern to add caching capabilities to any IReadRepository
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TId">The entity identifier type</typeparam>
public sealed class CachedRepositoryDecorator<TEntity, TId> : IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly IReadRepository<TEntity, TId> _inner;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedRepositoryDecorator<TEntity, TId>> _logger;
    private readonly TimeSpan _cacheExpiration;
    private readonly string _entityName;

    public CachedRepositoryDecorator(
        IReadRepository<TEntity, TId> inner,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings,
        ILogger<CachedRepositoryDecorator<TEntity, TId>> logger)
    {
        _inner = inner;
        _cacheService = cacheService;
        _logger = logger;
        _entityName = typeof(TEntity).Name;

        // Use configured expiration or default to 15 minutes
        var expirationMinutes = cacheSettings.Value.DefaultExpirationMinutes > 0
            ? cacheSettings.Value.DefaultExpirationMinutes
            : 15;
        _cacheExpiration = TimeSpan.FromMinutes(expirationMinutes);
    }

    /// <summary>
    /// Gets an entity by ID with caching support
    /// Cache key format: "{EntityName}:{Id}"
    /// </summary>
    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(id);

        return await _cacheService.GetOrAddAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Cache miss for {EntityName} with ID {Id}", _entityName, id);
                var entity = await _inner.GetByIdAsync(id, cancellationToken);

                if (entity != null)
                {
                    _logger.LogDebug("Cached {EntityName} with ID {Id}", _entityName, id);
                }

                return entity;
            },
            _cacheExpiration,
            cancellationToken);
    }

    /// <summary>
    /// Gets a single entity by specification
    /// Note: Complex queries are not cached to avoid cache bloat
    /// </summary>
    public Task<TEntity?> GetSingleAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return _inner.GetSingleAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Gets entities by specification
    /// Note: List queries are not cached due to complexity and size
    /// </summary>
    public Task<IReadOnlyList<TEntity>> GetAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return _inner.GetAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Checks if entity exists by specification
    /// </summary>
    public Task<bool> ExistsAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return _inner.ExistsAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Counts entities by specification
    /// </summary>
    public Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return _inner.CountAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Gets paginated entities by specification
    /// Note: Paginated queries are not cached due to complexity
    /// </summary>
    public Task<PagedResult<TEntity>> GetPaginatedAsync(
        ISpecification<TEntity> specification,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        return _inner.GetPaginatedAsync(specification, pagination, cancellationToken);
    }

    /// <summary>
    /// Gets paginated entities with projection
    /// Note: Projected queries are not cached due to complexity
    /// </summary>
    public Task<PagedResult<TProjection>> GetPaginatedAsync<TProjection>(
        ISpecification<TEntity> specification,
        PaginationRequest pagination,
        Expression<Func<TEntity, TProjection>> projection,
        CancellationToken cancellationToken = default)
    {
        return _inner.GetPaginatedAsync(specification, pagination, projection, cancellationToken);
    }

    /// <summary>
    /// Generates cache key in format: "{EntityName}:{Id}"
    /// Example: "Provider:123e4567-e89b-12d3-a456-426614174000"
    /// </summary>
    private string GetCacheKey(TId id) => $"{_entityName}:{id}";
}
