// ========================================
// RateLimiting/SlidingWindowRateLimiter.cs
// ========================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.Infrastructure.Security.RateLimiting;

/// <summary>
/// Sliding window rate limiter implementation
/// </summary>
public class SlidingWindowRateLimiter : IRateLimiter
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<SlidingWindowRateLimiter> _logger;

    public SlidingWindowRateLimiter(
        IDistributedCache cache,
        ILogger<SlidingWindowRateLimiter> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<RateLimitResult> AllowRequestAsync(string key, int limit, TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now - window;
        var cacheKey = $"rate_limit:{key}";

        try
        {
            // Get existing timestamps
            var json = await _cache.GetStringAsync(cacheKey);
            var timestamps = string.IsNullOrEmpty(json)
                ? new List<DateTimeOffset>()
                : JsonSerializer.Deserialize<List<DateTimeOffset>>(json) ?? new List<DateTimeOffset>();

            // Remove old timestamps outside the window
            timestamps.RemoveAll(t => t < windowStart);

            // Check if limit exceeded
            if (timestamps.Count >= limit)
            {
                var oldestTimestamp = timestamps.Min();
                var retryAfter = (oldestTimestamp + window) - now;

                return new RateLimitResult
                {
                    IsAllowed = false,
                    CurrentCount = timestamps.Count,
                    Limit = limit,
                    RetryAfter = retryAfter
                };
            }

            // Add current timestamp
            timestamps.Add(now);

            // Save back to cache
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(timestamps),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = window
                });

            return new RateLimitResult
            {
                IsAllowed = true,
                CurrentCount = timestamps.Count,
                Limit = limit,
                RetryAfter = TimeSpan.Zero
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for key {Key}", key);

            // On error, allow the request but log it
            return new RateLimitResult
            {
                IsAllowed = true,
                CurrentCount = 0,
                Limit = limit,
                RetryAfter = TimeSpan.Zero
            };
        }
    }

    public async Task<int> GetRequestCountAsync(string key)
    {
        var cacheKey = $"rate_limit:{key}";
        var json = await _cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(json))
        {
            return 0;
        }

        var timestamps = JsonSerializer.Deserialize<List<DateTimeOffset>>(json);
        return timestamps?.Count ?? 0;
    }

    public async Task ResetAsync(string key)
    {
        var cacheKey = $"rate_limit:{key}";
        await _cache.RemoveAsync(cacheKey);
    }
}
