// AsanRezerve.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
// AsanRezerve.API.Infrastructure/Responses/ApiResponse.cs
namespace AsanRezerve.Core.Domain.Infrastructure.Middleware.Responses
{
    /// <summary>
    /// Response metadata for debugging and monitoring
    /// </summary>
    public class ResponseMetadata
    {
        public string RequestId { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public long Duration { get; set; }
        public string? Path { get; set; }
        public string? Method { get; set; }
        public string? Version { get; set; }
    }
}
