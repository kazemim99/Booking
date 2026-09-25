// ========================================
// Caching/CacheSettings.cs
// ========================================
namespace AsanRezerve.Infrastructure.Core.Caching;

/// <summary>The <c>Cache</c> configuration section.</summary>
public class CacheSettings
{
    /// <summary>
    /// <c>Redis</c> (default) or <c>InMemory</c>. With Redis, the connection is <see cref="RedisConnectionString"/>
    /// when set, otherwise <c>ConnectionStrings:Redis</c> — the one the rest of the host (rate limiting, OTP state,
    /// health checks) uses. See <see cref="CachingRegistration.ResolveRedisConnectionString"/>.
    /// </summary>
    public string Provider { get; set; } = "Redis";

    /// <summary>Prefix of every key this application writes to Redis (<c>{KeyPrefix}:</c>).</summary>
    public string KeyPrefix { get; set; } = "asanrezerve";

    /// <summary>Default absolute lifetime of a cached read when the caller does not say.</summary>
    public int DefaultExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Longest an entry may live in the in-process (L1) tier. Shorter than the Redis (L2) lifetime so that a
    /// process which missed an invalidation (a future second node) converges quickly.
    /// </summary>
    public int LocalExpirationSeconds { get; set; } = 60;

    /// <summary>Largest serialised value the cache stores; bigger ones are served but not cached.</summary>
    public int MaximumPayloadBytes { get; set; } = 1024 * 1024;

    /// <summary>
    /// Optional dedicated Redis for the cache. Leave empty to share <c>ConnectionStrings:Redis</c>.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Consecutive cache failures before the circuit opens. The cache is an optimization, never a dependency:
    /// once Redis is clearly unreachable we stop paying its timeout on every request.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 3;

    /// <summary>
    /// How long the circuit stays open before a single trial request is let through to see whether Redis has
    /// recovered.
    /// </summary>
    public int CircuitBreakerResetSeconds { get; set; } = 30;
}
