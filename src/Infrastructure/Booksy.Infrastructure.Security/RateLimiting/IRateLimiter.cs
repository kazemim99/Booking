// ========================================
// RateLimiting/IRateLimiter.cs
// ========================================
namespace Booksy.Infrastructure.Security.RateLimiting;

/// <summary>
/// Rate limiter interface
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Checks if a request should be allowed
    /// </summary>
    Task<RateLimitResult> AllowRequestAsync(string key, int limit, TimeSpan window);

    /// <summary>
    /// Gets the current request count for a key
    /// </summary>
    Task<int> GetRequestCountAsync(string key);

    /// <summary>
    /// Resets the counter for a key
    /// </summary>
    Task ResetAsync(string key);
}


/// <summary>
/// Rate limit result
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int CurrentCount { get; set; }
    public int Limit { get; set; }
    public TimeSpan RetryAfter { get; set; }
}