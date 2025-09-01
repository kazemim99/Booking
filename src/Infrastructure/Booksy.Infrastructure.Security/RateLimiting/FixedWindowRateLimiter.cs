// ========================================
// RateLimiting/FixedWindowRateLimiter.cs
// ========================================
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Security.RateLimiting;

/// <summary>
/// Fixed window rate limiter implementation
/// </summary>
public class FixedWindowRateLimiter : IRateLimiter
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<FixedWindowRateLimiter> _logger;

    public FixedWindowRateLimiter(
        IMemoryCache cache,
        ILogger<FixedWindowRateLimiter> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<RateLimitResult> AllowRequestAsync(string key, int limit, TimeSpan window)
    {
        var cacheKey = $"rate_limit:{key}:{GetWindowKey(window)}";

        var count = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = window;
            return 0;
        });

        if (count >= limit)
        {
            return Task.FromResult(new RateLimitResult
            {
                IsAllowed = false,
                CurrentCount = count,
                Limit = limit,
                RetryAfter = GetTimeUntilNextWindow(window)
            });
        }

        _cache.Set(cacheKey, count + 1, window);

        return Task.FromResult(new RateLimitResult
        {
            IsAllowed = true,
            CurrentCount = count + 1,
            Limit = limit,
            RetryAfter = TimeSpan.Zero
        });
    }

    public Task<int> GetRequestCountAsync(string key)
    {
        var cacheKey = $"rate_limit:{key}:{GetWindowKey(TimeSpan.FromMinutes(1))}";
        var count = _cache.Get<int>(cacheKey);
        return Task.FromResult(count);
    }

    public Task ResetAsync(string key)
    {
        // In fixed window, we can't easily reset without knowing all windows
        // This would require tracking all window keys
        return Task.CompletedTask;
    }

    private string GetWindowKey(TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var windowStart = new DateTime(
            now.Year, now.Month, now.Day,
            now.Hour, now.Minute / (int)window.TotalMinutes * (int)window.TotalMinutes, 0);
        return windowStart.ToString("yyyyMMddHHmm");
    }

    private TimeSpan GetTimeUntilNextWindow(TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var windowMinutes = (int)window.TotalMinutes;
        var currentWindowMinute = now.Minute / windowMinutes * windowMinutes;
        var nextWindowStart = new DateTime(
            now.Year, now.Month, now.Day,
            now.Hour, currentWindowMinute, 0).AddMinutes(windowMinutes);

        if (nextWindowStart <= now)
        {
            nextWindowStart = nextWindowStart.AddMinutes(windowMinutes);
        }

        return nextWindowStart - now;
    }
}
