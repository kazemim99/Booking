// ========================================
// Middleware/ExceptionHandlingMiddleware.cs
// ========================================
using System.Diagnostics;

namespace Booksy.API.Middleware;
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        context.Items["RequestId"] = requestId;

        try
        {
            _logger.LogInformation(
                "Request {RequestId} started: {Method} {Path} from {RemoteIp}",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            await _next(context);

            stopwatch.Stop();

            _logger.LogInformation(
                "Request {RequestId} completed: {StatusCode} in {ElapsedMs}ms",
                requestId,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Request {RequestId} failed after {ElapsedMs}ms",
                requestId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}

