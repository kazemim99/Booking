

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
}


