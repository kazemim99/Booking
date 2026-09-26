using AsanRezerve.Infrastructure.Core.Caching;
using AsanRezerve.Infrastructure.Observability.LogStore;

namespace AsanRezerve.Infrastructure.Observability.Diagnostics;

/// <summary>Stored events of one level in a window.</summary>
public sealed record LevelCount(string Level, long Count);

/// <summary>
/// Repeated warnings/errors collapsed into one line: same level, source, message template and exception type.
/// </summary>
public sealed record ErrorGroup(
    string Level,
    string? SourceContext,
    string? MessageTemplate,
    string? ExceptionType,
    long Count,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    IReadOnlyList<string> SampleTraceIds);

/// <summary>Latency of one route from its request events.</summary>
public sealed record RouteLatency(
    string Route,
    long Requests,
    long ServerErrors,
    double P50Ms,
    double P95Ms,
    double MaxMs);

/// <summary>
/// A compact picture of a time window for a person or an AI model: what went wrong, how often, where it is slow,
/// and how the cache is doing (log-explorer: "System overview and AI digest").
/// </summary>
public sealed record SystemDigest(
    DateTimeOffset From,
    DateTimeOffset To,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<LevelCount> Levels,
    IReadOnlyList<ErrorGroup> ErrorGroups,
    IReadOnlyList<RouteLatency> SlowestRoutes,
    IReadOnlyList<CacheRegionStats> Cache,
    LogStoreStats? LogStore);
