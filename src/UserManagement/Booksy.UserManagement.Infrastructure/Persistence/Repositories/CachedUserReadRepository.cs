// 📁 Booksy.UserManagement.Infrastructure/Persistence/Repositories/CachedUserRepository.cs - FIXED
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace Booksy.UserManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Cached decorator for UserRepository - only caches read operations
/// </summary>
public class CachedUserRepository : IUserRepository
{
    private readonly IUserRepository _innerRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedUserRepository> _logger;
    private readonly TimeSpan _defaultCacheDuration = TimeSpan.FromMinutes(5);

    public CachedUserRepository(
        IUserRepository innerRepository,
        IDistributedCache cache,
        ILogger<CachedUserRepository> logger)
    {
        _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ✅ Cached read methods
    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user:id:{id.Value}";

        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Cache hit for user ID: {UserId}", id.Value);
                return JsonSerializer.Deserialize<User>(cachedData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from cache for user ID: {UserId}", id.Value);
        }

        var user = await _innerRepository.GetByIdAsync(id, cancellationToken);

        if (user != null)
        {
            await SetCacheAsync(cacheKey, user, cancellationToken);
        }

        return user;
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user:email:{email.Value}";

        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Cache hit for email: {Email}", email.Value);
                return JsonSerializer.Deserialize<User>(cachedData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from cache for email: {Email}", email.Value);
        }

        var user = await _innerRepository.GetByEmailAsync(email, cancellationToken);

        if (user != null)
        {
            await SetCacheAsync(cacheKey, user, cancellationToken);
            // Also cache by ID
            await SetCacheAsync($"user:id:{user.Id.Value}", user, cancellationToken);
        }

        return user;
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user:exists:email:{email.Value}";

        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Cache hit for email existence: {Email}", email.Value);
                return bool.Parse(cachedData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from cache for email existence: {Email}", email.Value);
        }

        var exists = await _innerRepository.ExistsByEmailAsync(email, cancellationToken);

        await SetCacheAsync(cacheKey, exists.ToString(), TimeSpan.FromMinutes(1), cancellationToken);

        return exists;
    }

    // ✅ Write operations - invalidate cache and delegate
    public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        await _innerRepository.SaveAsync(user, cancellationToken);

        // Invalidate cache
        await InvalidateUserCacheAsync(user);
    }

    public async Task RemoveAsync(User user, CancellationToken cancellationToken = default)
    {
        await _innerRepository.RemoveAsync(user, cancellationToken);

        // Invalidate cache
        await InvalidateUserCacheAsync(user);
    }

    public async Task RemoveByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        // Get user first to invalidate all cache keys
        var user = await _innerRepository.GetByIdAsync(id, cancellationToken);

        await _innerRepository.RemoveByIdAsync(id, cancellationToken);

        if (user != null)
        {
            await InvalidateUserCacheAsync(user);
        }
    }

    // ✅ Specification methods - delegate without caching (too complex to cache)
    public Task<User?> GetSingleAsync(ISpecification<User> specification, CancellationToken cancellationToken = default)
        => _innerRepository.GetSingleAsync(specification, cancellationToken);

    public Task<IReadOnlyList<User>> GetAsync(ISpecification<User> specification, CancellationToken cancellationToken = default)
        => _innerRepository.GetAsync(specification, cancellationToken);

    public Task<bool> ExistsAsync(ISpecification<User> specification, CancellationToken cancellationToken = default)
        => _innerRepository.ExistsAsync(specification, cancellationToken);

    public Task<int> CountAsync(ISpecification<User> specification, CancellationToken cancellationToken = default)
        => _innerRepository.CountAsync(specification, cancellationToken);

    private async Task SetCacheAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        await SetCacheAsync(key, value, _defaultCacheDuration, cancellationToken);
    }

    private async Task SetCacheAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = duration
            };

            var serialized = typeof(T) == typeof(string)
                ? value as string
                : JsonSerializer.Serialize(value);

            await _cache.SetStringAsync(key, serialized!, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error writing to cache for key: {Key}", key);
        }
    }

    private async Task InvalidateUserCacheAsync(User user)
    {
        try
        {
            await _cache.RemoveAsync($"user:id:{user.Id.Value}");
            await _cache.RemoveAsync($"user:email:{user.Email.Value}");
            await _cache.RemoveAsync($"user:exists:email:{user.Email.Value}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating cache for user: {UserId}", user.Id.Value);
        }
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
      await Task.FromResult(_innerRepository.UpdateAsync(user, cancellationToken));   
    }

    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken)
    {
      return  await _innerRepository.GetAllAsync(cancellationToken);
    }

    public async Task<PagedResult<User>> GetPaginatedAsync(ISpecification<User> specification, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
       return await _innerRepository.GetPaginatedAsync(specification, pagination, cancellationToken);   
    }

    public async Task<PagedResult<TProjection>> GetPaginatedAsync<TProjection>(ISpecification<User> specification, PaginationRequest pagination, System.Linq.Expressions.Expression<Func<User, TProjection>> projection, CancellationToken cancellationToken = default)
    {
       return await _innerRepository.GetPaginatedAsync(specification, pagination, projection, cancellationToken);
    }
}