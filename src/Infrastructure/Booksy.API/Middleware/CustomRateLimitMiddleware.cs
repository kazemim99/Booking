// ========================================
// Middleware/CustomRateLimitMiddleware.cs
// Custom middleware for handling rate limit responses with RFC 7807 Problem Details format
// ========================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.API.Middleware;

/// <summary>
/// Custom middleware that intercepts rate limit responses and returns RFC 7807 Problem Details format
/// </summary>
public class CustomRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomRateLimitMiddleware> _logger;

    public CustomRateLimitMiddleware(
        RequestDelegate next,
        ILogger<CustomRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store the original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Create a new memory stream to capture the response
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next middleware in the pipeline
            await _next(context);

            // Check if the response is a 429 (Too Many Requests)
            if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
            {
                await HandleRateLimitResponse(context, responseBody, originalBodyStream);
            }
            else
            {
                // Copy the response back to the original stream
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task HandleRateLimitResponse(
        HttpContext context,
        MemoryStream responseBody,
        Stream originalBodyStream)
    {
        // Get retry-after header if present
        var retryAfter = context.Response.Headers["Retry-After"].FirstOrDefault() ?? "60";

        // Log rate limit exceeded
        _logger.LogWarning(
            "Rate limit exceeded for {Path} by {ClientId}. Retry after: {RetryAfter} seconds",
            context.Request.Path,
            context.User?.Identity?.Name ?? "Anonymous",
            retryAfter);

        // Create RFC 7807 Problem Details response
        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too Many Requests",
            status = StatusCodes.Status429TooManyRequests,
            detail = $"API rate limit exceeded. Please retry after {retryAfter} seconds.",
            instance = context.Request.Path.Value,
            traceId = context.TraceIdentifier,
            retryAfter = retryAfter
        };

        // Set the content type
        context.Response.ContentType = "application/problem+json";

        // Add retry-after header if not already present
        if (!context.Response.Headers.ContainsKey("Retry-After"))
        {
            context.Response.Headers["Retry-After"] = retryAfter;
        }

        // Add rate limit headers for better client understanding
        context.Response.Headers["X-RateLimit-Limit"] = "See documentation for limits by user type";
        context.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.Response.Headers["X-RateLimit-Reset"] = retryAfter;

        // Serialize the problem details to JSON
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var problemDetailsJson = JsonSerializer.Serialize(problemDetails, jsonOptions);

        // Reset the response body and write the problem details
        context.Response.Body = originalBodyStream;
        await context.Response.WriteAsync(problemDetailsJson);
    }
}
