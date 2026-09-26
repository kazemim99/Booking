// AsanRezerve.Core.Domain/Infrastructure/Middleware/ApiResponseMiddleware.cs
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsanRezerve.Core.Domain.Infrastructure.Configuration;

namespace AsanRezerve.Core.Domain.Infrastructure.Middleware
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
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                // Persian as UTF-8, not \uXXXX; HTML-sensitive characters are still escaped.
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
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
                context.Response.Body = originalBodyStream;

                // Only wrap successful responses (2xx status codes), and never NoContent
                var status = context.Response.StatusCode;
                if (status is >= 200 and < 300 && status != (int)HttpStatusCode.NoContent)
                {
                    var wrapped = Wrap(context, responseBody, stopwatch.ElapsedMilliseconds);
                    context.Response.ContentLength = wrapped.WrittenCount;
                    await originalBodyStream.WriteAsync(wrapped.WrittenMemory, context.RequestAborted);
                    return;
                }

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream, context.RequestAborted);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Writes the envelope around the controller's bytes without parsing them into objects: a JSON body is
        /// embedded as it is (validated by a forward-only read), anything else becomes a string. The previous version
        /// deserialised every body and serialised it again — twice the work per response, and every Persian
        /// character escaped as \uXXXX. Null members are omitted, as before.
        /// </summary>
        private ArrayBufferWriter<byte> Wrap(HttpContext context, MemoryStream responseBody, long elapsedMs)
        {
            ReadOnlySpan<byte> content = responseBody.TryGetBuffer(out var segment)
                ? segment.AsSpan(0, (int)responseBody.Length)
                : responseBody.ToArray();

            var output = new ArrayBufferWriter<byte>(content.Length + 512);
            using var writer = new Utf8JsonWriter(output, new JsonWriterOptions
            {
                Encoder = _jsonOptions.Encoder,
                Indented = _options.WriteIndented,
            });

            writer.WriteStartObject();
            writer.WriteBoolean("success", true);
            writer.WriteNumber("statusCode", context.Response.StatusCode);
            writer.WriteString("message", GetSuccessMessage(context.Response.StatusCode));

            if (!IsBlank(content))
            {
                writer.WritePropertyName("data");
                if (IsJson(content))
                    writer.WriteRawValue(content, skipInputValidation: true);
                else
                    writer.WriteStringValue(Encoding.UTF8.GetString(content));
            }

            if (_options.IncludeMetadata)
            {
                writer.WriteStartObject("metadata");
                writer.WriteString("requestId", context.TraceIdentifier);
                // The id every log event of this request carries (and X-Trace-Id); what an admin searches by.
                writer.WriteString("traceId", Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier);
                writer.WriteString("timestamp", DateTimeOffset.UtcNow);
                writer.WriteNumber("duration", elapsedMs);
                writer.WriteString("path", context.Request.Path.Value);
                writer.WriteString("method", context.Request.Method);
                writer.WriteString("version", _options.ApiVersion);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.Flush();
            return output;
        }

        private static bool IsBlank(ReadOnlySpan<byte> content)
        {
            foreach (var b in content)
            {
                if (b is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')) return false;
            }
            return true;
        }

        /// <summary>A single, complete JSON value — checked with a forward-only reader, no objects built.</summary>
        private static bool IsJson(ReadOnlySpan<byte> content)
        {
            try
            {
                var reader = new Utf8JsonReader(content, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
                if (!reader.Read()) return false;
                reader.Skip();
                return !reader.Read(); // nothing after the value
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private bool IsApiRequest(HttpRequest request)
        {
            return request.Path.StartsWithSegments(_options.ApiPathPrefix)
                && !_options.ExcludedPathPrefixes.Any(prefix => request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
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