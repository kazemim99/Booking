// ========================================
// Caching/InMemoryCacheService.cs
// ========================================
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booksy.Infrastructure.Core.Caching;

/// <summary>
/// In-memory implementation of caching service
/// </summary>
public sealed class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheSettings _settings;
    private readonly ILogger<InMemoryCacheService> _logger;

    public InMemoryCacheService(
        IMemoryCache cache,
        IOptions<CacheSettings> settings,
        ILogger<InMemoryCacheService> logger)
    {
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue<T>(GetKey(key), out var value))
        {
            _logger.LogDebug("Cache hit for key {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("Cache miss for key {Key}", key);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new MemoryCacheEntryOptions();

        if (_settings.SlidingExpiration)
        {
            options.SetSlidingExpiration(expiration ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes));
        }
        else
        {
            options.SetAbsoluteExpiration(expiration ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes));
        }

        _cache.Set(GetKey(key), value, options);

        _logger.LogDebug("Cached value for key {Key}", key);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(GetKey(key));
        _logger.LogDebug("Removed cache key {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // In-memory cache doesn't support pattern removal directly
        // This would require maintaining a separate index of keys
        _logger.LogWarning("Pattern removal not supported in in-memory cache");
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cache.TryGetValue(GetKey(key), out _));
    }

    public async Task<T> GetOrAddAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        return await _cache.GetOrCreateAsync(
            GetKey(key),
            async entry =>
            {
                if (_settings.SlidingExpiration)
                {
                    entry.SetSlidingExpiration(expiration ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes));
                }
                else
                {
                    entry.SetAbsoluteExpiration(expiration ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes));
                }

                return await factory();
            });
    }

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        // Trigger sliding expiration by accessing the item
        _cache.TryGetValue(GetKey(key), out _);
        return Task.CompletedTask;
    }

    private string GetKey(string key) => $"{_settings.KeyPrefix}:{key}";
}
