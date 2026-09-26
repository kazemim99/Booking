using AsanRezerve.Infrastructure.Core.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace AsanRezerve.Infrastructure.Observability.Diagnostics;

public sealed record L1Stats(long Entries, long Hits, long Misses, long? EstimatedSize);

public sealed record L2Stats(
    string Store,
    string Circuit,
    int ConsecutiveFailures,
    DateTimeOffset? OpenedAt,
    string? LastError,
    string? RedisEndpoints,
    bool? RedisConnected);

/// <summary>What the admin cache page shows.</summary>
public sealed record CacheOverview(IReadOnlyList<CacheRegionStats> Regions, L1Stats? L1, L2Stats L2);

/// <summary>Reads the cache's counters and the state of both tiers (read-caching: "Admins observe and purge the cache").</summary>
public sealed class CacheDiagnostics(IServiceProvider services, CacheMetrics metrics)
{
    public CacheOverview Snapshot()
    {
        L1Stats? l1 = null;
        if (services.GetService<IMemoryCache>() is MemoryCache memory && memory.GetCurrentStatistics() is { } stats)
            l1 = new L1Stats(stats.CurrentEntryCount, stats.TotalHits, stats.TotalMisses, stats.CurrentEstimatedSize);

        var breaker = services.GetService<ResilientDistributedCache>();
        var redis = services.GetService<RedisConnection>();
        var store = services.GetService<IDistributedCache>()?.GetType().Name ?? "none";

        var l2 = new L2Stats(
            store,
            breaker?.State.ToString() ?? "n/a",
            breaker?.ConsecutiveFailures ?? 0,
            breaker?.OpenedAt,
            breaker?.LastError,
            redis?.Endpoints,
            redis?.IsConnected);

        return new CacheOverview(metrics.Snapshot(), l1, l2);
    }
}
