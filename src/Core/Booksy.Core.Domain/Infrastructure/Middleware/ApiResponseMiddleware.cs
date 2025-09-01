// Booksy.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Booksy.Core.Domain.Infrastructure.Middleware.Responses;
using Booksy.Core.Domain.Infrastructure.Configuration;

namespace Booksy.Core.Domain.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware for standardizing API responses and handling exceptions globally
    /// Implements Chain of Responsibility pattern for exception handling
    /// </summary>
    public sealed class ApiResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiResponseMiddleware> _logger;
        private readonly ApiResponseOptions _options;
        private readonly IExceptionHandlerStrategy _exceptionHandler;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiResponseMiddleware(
            RequestDelegate next,
            ILogger<ApiResponseMiddleware> logger,
            IOptions<ApiResponseOptions> options,
            IExceptionHandlerStrategy exceptionHandler)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _options.WriteIndented,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
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

                await _next(context);

                // Don't wrap NoContent responses
                if (context.Response.StatusCode != (int)HttpStatusCode.NoContent)
                {
                    await FormatSuccessResponse(context, stopwatch.ElapsedMilliseconds);
                }

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                context.Response.Body = originalBodyStream;
                await HandleExceptionAsync(context, ex, stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task FormatSuccessResponse(HttpContext context, long elapsedMs)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            var response = CreateSuccessResponse(context, responseContent, elapsedMs);

            context.Response.Headers.Clear();
            await context.Response.Body.FlushAsync(); // ensure anything written is flushed
            context.Response.Body.SetLength(0);       // reset the body

            await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
            context.Response.ContentType = "application/json";
        }

        private ApiResponse<object> CreateSuccessResponse(HttpContext context, string responseContent, long elapsedMs)
        {
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

            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = context.Response.StatusCode,
                Message = GetSuccessMessage(context.Response.StatusCode),
                Data = data,
                Metadata = CreateMetadata(context, elapsedMs)
            };
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, long elapsedMs)
        {
            var (statusCode, errorResponse) = _exceptionHandler.Handle(exception);

            _logger.LogError(exception,
                "Exception handled in API middleware. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}",
                statusCode, errorResponse.Code);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Success = false,
                StatusCode = statusCode,
                Message = errorResponse.Message,
                Error = errorResponse,
                Metadata = CreateMetadata(context, elapsedMs)
            };

            await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
        }

        private ResponseMetadata CreateMetadata(HttpContext context, long elapsedMs)
        {
            return new ResponseMetadata
            {
                RequestId = context.TraceIdentifier,
                Timestamp = DateTimeOffset.UtcNow,
                Duration = elapsedMs,
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                Version = _options.ApiVersion
            };
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
                204 => "Request completed with no content",
                _ => "Request processed"
            };
        }
    }
}
