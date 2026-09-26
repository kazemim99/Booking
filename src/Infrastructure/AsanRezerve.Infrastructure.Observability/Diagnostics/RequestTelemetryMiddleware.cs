using System.Diagnostics;
using System.Security.Claims;
using AsanRezerve.Infrastructure.Observability.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AsanRezerve.Infrastructure.Observability.Diagnostics;

/// <summary>
/// One event per HTTP request, and the request's trace id on the response (design D4).
/// <para>First in the pipeline, so it times the whole request and sees the final status code (the exception
/// handler and the response envelope run inside it). Level: 5xx Error, slower than the threshold Warning,
/// otherwise Information; health probes at Debug. The event carries method, path, route template, status,
/// elapsed milliseconds, user id and client IP; Serilog stamps it — and every other event of the request — with
/// the W3C trace id this middleware returns in <c>X-Trace-Id</c>, so a user-reported error can be found.</para>
/// </summary>
public sealed class RequestTelemetryMiddleware
{
    public const string TraceIdHeader = "X-Trace-Id";

    private const string Template = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMs} ms";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTelemetryMiddleware> _logger;
    private readonly IOptionsMonitor<ObservabilityLoggingOptions> _options;
    private readonly TimeProvider _time;

    public RequestTelemetryMiddleware(
        RequestDelegate next,
        ILogger<RequestTelemetryMiddleware> logger,
        IOptionsMonitor<ObservabilityLoggingOptions> options,
        TimeProvider time)
    {
        _next = next;
        _logger = logger;
        _options = options;
        _time = time;
    }

    /// <summary>The id a client quotes and an admin searches by: the W3C trace id of the request's activity.</summary>
    public static string TraceIdOf(HttpContext context) =>
        Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;

    public async Task InvokeAsync(HttpContext context)
    {
        var started = _time.GetTimestamp();
        var traceId = TraceIdOf(context);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[TraceIdHeader] = traceId;
            return Task.CompletedTask;
        });

        var failed = false;
        try
        {
            await _next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            var elapsed = Math.Round(_time.GetElapsedTime(started).TotalMilliseconds, 1);
            var status = failed && !context.Response.HasStarted ? StatusCodes.Status500InternalServerError : context.Response.StatusCode;
            Write(context, status, elapsed);
        }
    }

    private void Write(HttpContext context, int status, double elapsedMs)
    {
        var level = LevelOf(context.Request.Path, status, elapsedMs, _options.CurrentValue.SlowRequestThresholdMs);
        if (!_logger.IsEnabled(level)) return;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["RouteTemplate"] = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText,
            ["UserId"] = UserIdOf(context.User),
            ["ClientIp"] = ClientIpOf(context),
        }))
        {
            _logger.Log(level, Template, context.Request.Method, context.Request.Path.Value, status, elapsedMs);
        }
    }

    internal static LogLevel LevelOf(PathString path, int status, double elapsedMs, int slowThresholdMs)
    {
        if (path.StartsWithSegments("/health")) return LogLevel.Debug;
        if (status >= 500) return LogLevel.Error;
        if (elapsedMs > slowThresholdMs) return LogLevel.Warning;
        return LogLevel.Information;
    }

    private static string? UserIdOf(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
            ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value
            : null;

    /// <summary>
    /// The client behind nginx: the first <c>X-Forwarded-For</c> hop, else the socket. For the log only — never
    /// for a security decision (the header is client-controlled where no trusted proxy strips it).
    /// </summary>
    private static string? ClientIpOf(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
