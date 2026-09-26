using System.Security.Claims;
using AsanRezerve.Core.Application.Abstractions.Caching;
using AsanRezerve.Infrastructure.Observability.Diagnostics;
using AsanRezerve.Infrastructure.Observability.Logging.Levels;
using AsanRezerve.Infrastructure.Observability.LogStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace AsanRezerve.Infrastructure.Observability.Api;

/// <summary>
/// Logs, log levels, the system overview, the AI digest and the cache — for the admin panel's Logs page and the
/// MCP server (openspec/changes/add-observability-and-caching, D10). AdminOnly (decision 2026-09-25).
/// </summary>
[ApiController]
[Route(RoutePrefix)]
[Produces("application/json")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminObservabilityController(
    LogQueryService logs,
    SystemOverviewService overview,
    LogLevelService levels,
    CacheDiagnostics cache,
    ICacheInvalidator invalidator) : ControllerBase
{
    public const string RoutePrefix = "api/v1/admin/observability";

    /// <summary>The export streams NDJSON; the response envelope must not buffer and wrap it.</summary>
    public const string ExportPath = "/" + RoutePrefix + "/logs/export";

    public sealed record LogSearchQuery(
        DateTimeOffset? From,
        DateTimeOffset? To,
        string? MinLevel,
        string? Search,
        string? Source,
        string? TraceId,
        string? RequestPath,
        int? StatusCode,
        int Page = 1,
        int PageSize = 50);

    public sealed record SetLogLevelRequest(string Category, string Level, int? DurationMinutes);

    public sealed record InvalidateCacheRequest(string? Tag, bool All = false);

    public sealed record LogLevelOverrideDto(string Level, DateTimeOffset? ExpiresAt, string UpdatedBy, DateTimeOffset UpdatedAt);

    public sealed record LogLevelDto(string Category, string? ConfiguredLevel, string EffectiveLevel, LogLevelOverrideDto? Override);

    /// <summary>Stored events, newest first. Default window: the last 24 hours; at most 14 days.</summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(LogPage<LogEventListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] LogSearchQuery query, CancellationToken cancellationToken)
    {
        if (!TryParseLevel(query.MinLevel, out var minLevel))
            return BadRequest(new { message = "minLevel must be Verbose, Debug, Information, Warning, Error or Fatal" });

        return Ok(await logs.SearchAsync(ToSearch(query, minLevel), cancellationToken));
    }

    [HttpGet("logs/{id:long}")]
    [ProducesResponseType(typeof(LogEventDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(long id, CancellationToken cancellationToken) =>
        await logs.GetAsync(id, cancellationToken) is { } detail ? Ok(detail) : NotFound();

    /// <summary>Every stored event of one request (the <c>X-Trace-Id</c> a client or error body reported).</summary>
    [HttpGet("logs/trace/{traceId}")]
    [ProducesResponseType(typeof(IReadOnlyList<LogEventDetail>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Trace(string traceId, CancellationToken cancellationToken)
    {
        if (traceId.Length is 0 or > 64) return BadRequest(new { message = "traceId is 32 hex characters" });
        return Ok(await logs.TraceAsync(traceId, cancellationToken));
    }

    /// <summary>Matching events as newline-delimited JSON, oldest first, at most 50 000.</summary>
    [HttpGet("logs/export")]
    public async Task Export([FromQuery] LogSearchQuery query, CancellationToken cancellationToken)
    {
        if (!TryParseLevel(query.MinLevel, out var minLevel))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Response.ContentType = "application/x-ndjson";
        Response.Headers.ContentDisposition = $"attachment; filename=\"asanrezerve-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.ndjson\"";
        await logs.ExportAsync(ToSearch(query, minLevel), Response.Body, cancellationToken);
    }

    /// <summary>
    /// A window summarised for a person or an AI assistant: level counts, grouped warnings/errors with sample trace
    /// ids, slowest routes, cache hit ratios. Default window: the last hour. <c>format=markdown</c> returns
    /// <c>{ markdown }</c> ready to paste into a model.
    /// </summary>
    [HttpGet("digest")]
    public async Task<IActionResult> Digest(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var (start, end) = logs.Window(from, to, TimeSpan.FromHours(1));
        var digest = await overview.DigestAsync(start, end, cancellationToken);

        return string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase)
            ? Ok(new { markdown = SystemDigestMarkdown.Render(digest) })
            : Ok(digest);
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(SystemOverview), StatusCodes.Status200OK)]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken) =>
        Ok(await overview.OverviewAsync(cancellationToken));

    [HttpGet("log-levels")]
    [ProducesResponseType(typeof(IReadOnlyList<LogLevelDto>), StatusCodes.Status200OK)]
    public IActionResult LogLevels() => Ok(levels.GetLevels().Select(ToDto).ToList());

    /// <summary>Overrides a category's level, optionally for <c>durationMinutes</c> (then it reverts by itself).</summary>
    [HttpPut("log-levels")]
    [ProducesResponseType(typeof(LogLevelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetLogLevel([FromBody] SetLogLevelRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<LogLevel>(request.Level, ignoreCase: true, out var level) || !Enum.IsDefined(level))
            return BadRequest(new { message = "level must be Trace, Debug, Information, Warning, Error, Critical or None" });

        var duration = request.DurationMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : (TimeSpan?)null;
        var row = await levels.SetAsync(request.Category ?? string.Empty, level, duration, Actor(), cancellationToken);
        return Ok(ToDto(row));
    }

    [HttpDelete("log-levels/{category}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetLogLevel(string category, CancellationToken cancellationToken)
    {
        await levels.ResetAsync(category, Actor(), cancellationToken);
        return NoContent();
    }

    [HttpGet("cache")]
    [ProducesResponseType(typeof(CacheOverview), StatusCodes.Status200OK)]
    public IActionResult Cache() => Ok(cache.Snapshot());

    /// <summary>Evicts one tag (for example <c>provider:{id}</c>, <c>provider-directory</c>, <c>locations</c>) or everything.</summary>
    [HttpPost("cache/invalidate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InvalidateCache([FromBody] InvalidateCacheRequest request, CancellationToken cancellationToken)
    {
        if (request.All)
        {
            await invalidator.InvalidateAllAsync(cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Tag) && request.Tag.Length <= 200 && request.Tag.Trim() != "*")
        {
            await invalidator.InvalidateAsync([request.Tag.Trim()], cancellationToken);
        }
        else
        {
            return BadRequest(new { message = "Give a tag, or all: true" });
        }

        return NoContent();
    }

    private string Actor()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var name = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
        return string.Join(" ", new[] { name, id }.Where(v => !string.IsNullOrWhiteSpace(v))) is { Length: > 0 } actor ? actor : "admin";
    }

    private static bool TryParseLevel(string? value, out LogEventLevel? level)
    {
        level = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed)) return false;
        level = parsed;
        return true;
    }

    private static LogSearch ToSearch(LogSearchQuery query, LogEventLevel? minLevel) =>
        new(query.From, query.To, minLevel, query.Search, query.Source, query.TraceId, query.RequestPath, query.StatusCode,
            query.Page, query.PageSize);

    private static LogLevelDto ToDto(LogLevelInfo info) =>
        new(info.Category,
            info.ConfiguredLevel?.ToString(),
            info.EffectiveLevel.ToString(),
            info.Override is { } o ? new LogLevelOverrideDto(o.Level.ToString(), o.ExpiresAt, o.UpdatedBy, o.UpdatedAt) : null);
}
