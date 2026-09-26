using System.Diagnostics;
using System.Reflection;
using AsanRezerve.Infrastructure.Core.Caching;
using AsanRezerve.Infrastructure.Observability.LogStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AsanRezerve.Infrastructure.Observability.Diagnostics;

public sealed record ProcessStats(
    double WorkingSetMb,
    double GcHeapMb,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    int ThreadPoolThreads,
    long PendingWorkItems,
    double CpuSeconds);

public sealed record RequestSummary(long Requests, long ServerErrors, double ErrorRate, double P95Ms);

public sealed record SystemOverview(
    DateTimeOffset GeneratedAt,
    string Environment,
    string Version,
    DateTimeOffset StartedAt,
    ProcessStats Process,
    RequestSummary LastHour,
    IReadOnlyList<LevelCount> LevelsLastHour,
    IReadOnlyList<TimelineBucket> Last24Hours,
    IReadOnlyList<RouteLatency> SlowestRoutesLastHour,
    IReadOnlyList<ErrorGroup> TopErrorsLastHour,
    CacheOverview Cache,
    LogStoreStats? LogStore,
    IReadOnlyList<CounterTotal> Counters);

/// <summary>Assembles the admin overview and the AI digest.</summary>
public sealed class SystemOverviewService(
    IServiceProvider services,
    IHostEnvironment environment,
    CacheDiagnostics cache,
    CacheMetrics cacheMetrics,
    ApplicationCounters counters,
    TimeProvider time)
{
    private static readonly DateTimeOffset StartedAt = new(Process.GetCurrentProcess().StartTime.ToUniversalTime());

    public async Task<SystemOverview> OverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var hourAgo = now.AddHours(-1);
        var logs = services.GetService<LogQueryService>();
        var store = services.GetService<LogStoreWriter>()?.Stats;

        IReadOnlyList<LevelCount> levels = [];
        IReadOnlyList<TimelineBucket> timeline = [];
        IReadOnlyList<RouteLatency> routes = [];
        IReadOnlyList<ErrorGroup> errors = [];
        if (logs is not null && store is { Ready: true })
        {
            levels = await logs.LevelCountsAsync(hourAgo, now, null, cancellationToken);
            timeline = await logs.TimelineAsync(now.AddHours(-24), now, cancellationToken);
            routes = await logs.SlowestRoutesAsync(hourAgo, now, 10, cancellationToken);
            errors = await logs.ErrorGroupsAsync(hourAgo, now, 10, null, cancellationToken);
        }

        var requests = routes.Sum(r => r.Requests);
        var serverErrors = routes.Sum(r => r.ServerErrors);
        var summary = new RequestSummary(
            requests,
            serverErrors,
            requests == 0 ? 0 : Math.Round((double)serverErrors / requests, 4),
            routes.Count == 0 ? 0 : routes.Max(r => r.P95Ms));

        return new SystemOverview(now, environment.EnvironmentName, Version(), StartedAt, ProcessNow(), summary,
            levels, timeline, routes, errors, cache.Snapshot(), store, counters.Snapshot());
    }

    /// <param name="source">Optional source-context prefix (e.g. <c>AsanRezerve.ServiceCatalog</c>) for levels and errors.</param>
    public async Task<SystemDigest> DigestAsync(
        DateTimeOffset from, DateTimeOffset to, string? source = null, CancellationToken cancellationToken = default)
    {
        var logs = services.GetRequiredService<LogQueryService>();
        return new SystemDigest(
            from, to, time.GetUtcNow(),
            await logs.LevelCountsAsync(from, to, source, cancellationToken),
            await logs.ErrorGroupsAsync(from, to, 25, source, cancellationToken),
            await logs.SlowestRoutesAsync(from, to, 15, cancellationToken),
            cacheMetrics.Snapshot(),
            services.GetService<LogStoreWriter>()?.Stats);
    }

    private static ProcessStats ProcessNow()
    {
        using var process = Process.GetCurrentProcess();
        ThreadPool.GetAvailableThreads(out _, out _);
        return new ProcessStats(
            Math.Round(process.WorkingSet64 / 1024d / 1024d, 1),
            Math.Round(GC.GetTotalMemory(false) / 1024d / 1024d, 1),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            ThreadPool.ThreadCount,
            ThreadPool.PendingWorkItemCount,
            Math.Round(process.TotalProcessorTime.TotalSeconds, 1));
    }

    private static string Version() =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        ?? "unknown";
}
