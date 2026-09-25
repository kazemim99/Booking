using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using AsanRezerve.Core.Application.Abstractions.Caching;

namespace AsanRezerve.Infrastructure.Core.Caching;

/// <summary>Hit/miss counts of one cache region since the process started.</summary>
public sealed record CacheRegionStats(string Region, long Requests, long Hits, long Misses)
{
    /// <summary>Hits / requests, 0 when there were none.</summary>
    public double HitRatio => Requests == 0 ? 0 : (double)Hits / Requests;
}

/// <summary>
/// Process-wide cache counters: an in-memory snapshot for the admin cache page, and the
/// <c>asanrezerve.cache.requests</c> counter (meter <see cref="MeterName"/>) for any metrics exporter.
/// </summary>
public sealed class CacheMetrics : ICacheMetrics
{
    public const string MeterName = "AsanRezerve.Caching";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        "asanrezerve.cache.requests", description: "Cached reads, tagged by region and result (hit|miss).");

    private readonly ConcurrentDictionary<string, Counters> _regions = new(StringComparer.Ordinal);

    public void Record(string region, bool hit)
    {
        var counters = _regions.GetOrAdd(region, _ => new Counters());
        Interlocked.Increment(ref counters.Requests);
        if (!hit) Interlocked.Increment(ref counters.Misses);

        Requests.Add(1,
            new KeyValuePair<string, object?>("region", region),
            new KeyValuePair<string, object?>("result", hit ? "hit" : "miss"));
    }

    /// <summary>Every region, busiest first.</summary>
    public IReadOnlyList<CacheRegionStats> Snapshot() =>
        _regions
            .Select(kv =>
            {
                var requests = Interlocked.Read(ref kv.Value.Requests);
                var misses = Interlocked.Read(ref kv.Value.Misses);
                return new CacheRegionStats(kv.Key, requests, requests - misses, misses);
            })
            .OrderByDescending(r => r.Requests)
            .ThenBy(r => r.Region, StringComparer.Ordinal)
            .ToList();

    private sealed class Counters
    {
        public long Requests;
        public long Misses;
    }
}
