// Booksy.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
// Booksy.API.Infrastructure/Responses/ApiResponse.cs

// Booksy.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
// Booksy.API.Infrastructure/Responses/ApiResponse.cs
namespace Booksy.Core.Domain.Infrastructure.Middleware.Responses
{
    /// <summary>
    /// Standard API response wrapper with generic data support
    /// </summary>
    public class ApiResponse<T> where T : class
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public ErrorResponse? Error { get; set; }
        public ResponseMetadata? Metadata { get; set; } = new();
    }
}
