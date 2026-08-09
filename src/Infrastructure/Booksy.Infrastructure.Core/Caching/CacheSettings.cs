

// ========================================
// Caching/CacheSettings.cs
// ========================================
namespace Booksy.Infrastructure.Core.Caching;

public class CacheSettings
{
    public string Provider { get; set; } = "InMemory";
    public string KeyPrefix { get; set; } = "booksy";
    public int DefaultExpirationMinutes { get; set; } = 5;
    public bool SlidingExpiration { get; set; } = true;
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Consecutive cache failures before the circuit opens. The cache is an
    /// optimization, never a dependency: once Redis is clearly unreachable we
    /// stop paying its connect timeout on every request and read through to
    /// the database instead.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 3;

    /// <summary>
    /// How long the circuit stays open before a single trial request is let
    /// through to see whether Redis has recovered.
    /// </summary>
    public int CircuitBreakerResetSeconds { get; set; } = 30;
}


