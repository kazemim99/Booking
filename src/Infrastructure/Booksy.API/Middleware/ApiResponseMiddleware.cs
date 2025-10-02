// Booksy.Core.Domain/Infrastructure/Middleware/ApiResponseMiddleware.cs
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Booksy.Core.Domain.Infrastructure.Configuration;

namespace Booksy.Core.Domain.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware for wrapping successful API responses in a standard format
    /// Works with ExceptionHandlingMiddleware for error responses
    /// </summary>
    public sealed class ApiResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiResponseMiddleware> _logger;
        private readonly ApiResponseOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiResponseMiddleware(
            RequestDelegate next,
            ILogger<ApiResponseMiddleware> logger,
            IOptions<ApiResponseOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _options.WriteIndented,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip non-API requests
            if (!IsApiRequest(context.Request))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            try
            {
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                // Call next middleware (exceptions will bubble up to ExceptionHandlingMiddleware)
                await _next(context);

                stopwatch.Stop();

                // Only wrap successful responses (2xx status codes)
                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    // Don't wrap NoContent responses
                    if (context.Response.StatusCode != (int)HttpStatusCode.NoContent)
                    {
                        await WrapSuccessResponse(context, responseBody, stopwatch.ElapsedMilliseconds);
                    }
                }

                // Copy the wrapped response back to original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
                stopwatch.Stop();
            }
        }

        private async Task WrapSuccessResponse(HttpContext context, MemoryStream responseBody, long elapsedMs)
        {
            // Read the original response
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();

            // Parse the response data
            object? data = null;
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                try
                {
                    data = JsonSerializer.Deserialize<object>(responseContent, _jsonOptions);
                }
                catch
                {
                    data = responseContent;
                }
            }

            // Create wrapped response
            var wrappedResponse = new
            {
                success = true,
                statusCode = context.Response.StatusCode,
                message = GetSuccessMessage(context.Response.StatusCode),
                data = data,
                metadata = _options.IncludeMetadata ? new
                {
                    requestId = context.TraceIdentifier,
                    timestamp = DateTimeOffset.UtcNow,
                    duration = elapsedMs,
                    path = context.Request.Path.Value,
                    method = context.Request.Method,
                    version = _options.ApiVersion
                } : null
            };

            // Write wrapped response
            responseBody.SetLength(0);
            responseBody.Seek(0, SeekOrigin.Begin);
            await JsonSerializer.SerializeAsync(responseBody, wrappedResponse, _jsonOptions);
        }

        private bool IsApiRequest(HttpRequest request)
        {
            return request.Path.StartsWithSegments(_options.ApiPathPrefix);
        }

        private string GetSuccessMessage(int statusCode)
        {
            return statusCode switch
            {
                200 => "Request completed successfully",
                201 => "Resource created successfully",
                202 => "Request accepted for processing",
                _ => "Request processed"
            };
        }
    }
}