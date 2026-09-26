using System.Text.Json;
using AsanRezerve.Infrastructure.Observability.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog.Events;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

public sealed record LogSearch(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    LogEventLevel? MinLevel = null,
    string? Search = null,
    string? Source = null,
    string? TraceId = null,
    string? RequestPath = null,
    int? StatusCode = null,
    int Page = 1,
    int PageSize = 50);

public sealed record LogEventListItem(
    long Id,
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? SourceContext,
    string? TraceId,
    string? RequestPath,
    int? StatusCode,
    double? ElapsedMs,
    bool HasException);

public sealed record LogEventDetail(
    long Id,
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? MessageTemplate,
    string? Exception,
    string? SourceContext,
    string? TraceId,
    string? SpanId,
    string? RequestPath,
    string? RouteTemplate,
    int? StatusCode,
    double? ElapsedMs,
    string? UserId,
    JsonElement? Properties);

public sealed record LogPage<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize);

/// <summary>Reads the log store for the admin API, the digest and the MCP server (log-explorer).</summary>
public sealed class LogQueryService(ObservabilityDbContext db, LogStoreDataSource dataSource, TimeProvider time)
{
    /// <summary>The widest window any query covers: the retention period.</summary>
    public static readonly TimeSpan MaxWindow = TimeSpan.FromDays(14);

    public const int MaxPageSize = 200;
    public const int MaxExport = 50_000;

    public (DateTimeOffset From, DateTimeOffset To) Window(DateTimeOffset? from, DateTimeOffset? to, TimeSpan defaultSpan)
    {
        var end = (to ?? time.GetUtcNow()).ToUniversalTime();
        var start = (from ?? end - defaultSpan).ToUniversalTime();
        if (start > end) (start, end) = (end, start);
        if (end - start > MaxWindow) start = end - MaxWindow;
        return (start, end);
    }

    public async Task<LogPage<LogEventListItem>> SearchAsync(LogSearch search, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, search.Page);
        var size = Math.Clamp(search.PageSize, 1, MaxPageSize);

        var query = Filter(search);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.Timestamp).ThenByDescending(e => e.Id)
            .Skip((page - 1) * size).Take(size)
            .Select(e => new LogEventListItem(e.Id, e.Timestamp, ((LogEventLevel)e.Level).ToString(), e.Message,
                e.SourceContext, e.TraceId, e.RequestPath, e.StatusCode, e.ElapsedMs, e.Exception != null))
            .ToListAsync(cancellationToken);

        return new LogPage<LogEventListItem>(items, total, page, size);
    }

    public async Task<LogEventDetail?> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var e = await db.LogEvents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return e is null ? null : Detail(e);
    }

    /// <summary>Every stored event of one request, in order.</summary>
    public async Task<IReadOnlyList<LogEventDetail>> TraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        var id = traceId.Trim().ToLowerInvariant();
        var rows = await db.LogEvents.AsNoTracking()
            .Where(e => e.TraceId == id)
            .OrderBy(e => e.Timestamp).ThenBy(e => e.Id)
            .Take(1_000)
            .ToListAsync(cancellationToken);
        return rows.Select(Detail).ToList();
    }

    /// <summary>Matching events as newline-delimited JSON (one event per line, oldest first), at most <see cref="MaxExport"/>.</summary>
    public async Task ExportAsync(LogSearch search, Stream output, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var newline = "\n"u8.ToArray();
        await foreach (var e in Filter(search).OrderBy(x => x.Timestamp).ThenBy(x => x.Id).Take(MaxExport)
                           .AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            await JsonSerializer.SerializeAsync(output, Detail(e), options, cancellationToken);
            await output.WriteAsync(newline, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<LevelCount>> LevelCountsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.Value.CreateCommand(
            "SELECT level, count(*) FROM observability.log_events WHERE timestamp >= $1 AND timestamp < $2 " +
            "GROUP BY level ORDER BY level DESC");
        Window(command, from, to);

        var result = new List<LevelCount>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new LevelCount(((LogEventLevel)reader.GetInt16(0)).ToString(), reader.GetInt64(1)));
        return result;
    }

    /// <summary>Warnings and errors grouped by level, source, message template and exception type.</summary>
    public async Task<IReadOnlyList<ErrorGroup>> ErrorGroupsAsync(
        DateTimeOffset from, DateTimeOffset to, int limit = 20, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.Value.CreateCommand(
            "SELECT level, source_context, message_template, NULLIF(split_part(exception, E'\\n', 1), '') AS exception_head, " +
            "       count(*) AS n, min(timestamp), max(timestamp), " +
            "       (array_remove(array_agg(DISTINCT trace_id), NULL))[1:3] " +
            "FROM observability.log_events " +
            "WHERE timestamp >= $1 AND timestamp < $2 AND level >= 3 " +
            "GROUP BY 1, 2, 3, 4 ORDER BY n DESC, max(timestamp) DESC LIMIT $3");
        Window(command, from, to);
        command.Parameters.Add(new NpgsqlParameter { Value = Math.Clamp(limit, 1, 100) });

        var result = new List<ErrorGroup>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ErrorGroup(
                ((LogEventLevel)reader.GetInt16(0)).ToString(),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetInt64(4),
                reader.GetFieldValue<DateTimeOffset>(5),
                reader.GetFieldValue<DateTimeOffset>(6),
                reader.IsDBNull(7) ? [] : reader.GetFieldValue<string[]>(7)));
        }
        return result;
    }

    /// <summary>Latency per route from the request completion events, slowest p95 first.</summary>
    public async Task<IReadOnlyList<RouteLatency>> SlowestRoutesAsync(
        DateTimeOffset from, DateTimeOffset to, int limit = 20, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.Value.CreateCommand(
            "SELECT coalesce(route_template, request_path, '?') AS route, count(*), " +
            "       count(*) FILTER (WHERE status_code >= 500), " +
            "       percentile_cont(0.5) WITHIN GROUP (ORDER BY elapsed_ms), " +
            "       percentile_cont(0.95) WITHIN GROUP (ORDER BY elapsed_ms), max(elapsed_ms) " +
            "FROM observability.log_events " +
            "WHERE timestamp >= $1 AND timestamp < $2 AND elapsed_ms IS NOT NULL AND message_template LIKE 'HTTP %' " +
            "GROUP BY 1 ORDER BY 5 DESC LIMIT $3");
        Window(command, from, to);
        command.Parameters.Add(new NpgsqlParameter { Value = Math.Clamp(limit, 1, 100) });

        var result = new List<RouteLatency>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new RouteLatency(reader.GetString(0), reader.GetInt64(1), reader.GetInt64(2),
                Math.Round(reader.GetDouble(3), 1), Math.Round(reader.GetDouble(4), 1), Math.Round(reader.GetDouble(5), 1)));
        }
        return result;
    }

    /// <summary>Hourly counts per level, for the overview chart.</summary>
    public async Task<IReadOnlyList<TimelineBucket>> TimelineAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.Value.CreateCommand(
            "SELECT date_trunc('hour', timestamp) AS bucket, " +
            "       count(*) FILTER (WHERE level <= 2), count(*) FILTER (WHERE level = 3), count(*) FILTER (WHERE level >= 4) " +
            "FROM observability.log_events WHERE timestamp >= $1 AND timestamp < $2 GROUP BY 1 ORDER BY 1");
        Window(command, from, to);

        var result = new List<TimelineBucket>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TimelineBucket(reader.GetFieldValue<DateTimeOffset>(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3)));
        }
        return result;
    }

    private IQueryable<LogEventRecord> Filter(LogSearch search)
    {
        var (from, to) = Window(search.From, search.To, TimeSpan.FromHours(24));
        var query = db.LogEvents.AsNoTracking().Where(e => e.Timestamp >= from && e.Timestamp <= to);

        if (search.MinLevel is { } level)
        {
            var min = (short)level;
            query = query.Where(e => e.Level >= min);
        }

        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            var pattern = "%" + Escape(search.Search.Trim()) + "%";
            query = query.Where(e => EF.Functions.ILike(e.Message, pattern, "\\")
                                     || (e.Exception != null && EF.Functions.ILike(e.Exception, pattern, "\\")));
        }

        if (!string.IsNullOrWhiteSpace(search.Source))
        {
            var prefix = Escape(search.Source.Trim()) + "%";
            query = query.Where(e => e.SourceContext != null && EF.Functions.ILike(e.SourceContext, prefix, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(search.TraceId))
        {
            var id = search.TraceId.Trim().ToLowerInvariant();
            query = query.Where(e => e.TraceId == id);
        }

        if (!string.IsNullOrWhiteSpace(search.RequestPath))
        {
            var prefix = Escape(search.RequestPath.Trim()) + "%";
            query = query.Where(e => e.RequestPath != null && EF.Functions.ILike(e.RequestPath, prefix, "\\"));
        }

        if (search.StatusCode is { } status)
            query = query.Where(e => e.StatusCode == status);

        return query;
    }

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static void Window(NpgsqlCommand command, DateTimeOffset from, DateTimeOffset to)
    {
        command.Parameters.Add(new NpgsqlParameter { Value = from.ToUniversalTime() });
        command.Parameters.Add(new NpgsqlParameter { Value = to.ToUniversalTime() });
    }

    private static LogEventDetail Detail(LogEventRecord e)
    {
        JsonElement? properties = null;
        if (!string.IsNullOrEmpty(e.Properties))
        {
            using var document = JsonDocument.Parse(e.Properties);
            properties = document.RootElement.Clone();
        }

        return new LogEventDetail(e.Id, e.Timestamp, ((LogEventLevel)e.Level).ToString(), e.Message, e.MessageTemplate,
            e.Exception, e.SourceContext, e.TraceId, e.SpanId, e.RequestPath, e.RouteTemplate, e.StatusCode, e.ElapsedMs,
            e.UserId, properties);
    }
}

/// <summary>Events in one hour: information and below, warnings, errors and above.</summary>
public sealed record TimelineBucket(DateTimeOffset Hour, long Information, long Warnings, long Errors);
