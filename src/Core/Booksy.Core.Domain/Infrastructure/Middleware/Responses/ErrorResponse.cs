// Booksy.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
// Booksy.API.Infrastructure/Responses/ApiResponse.cs
using System.ComponentModel;

namespace Booksy.Core.Domain.Infrastructure.Middleware.Responses
{
    /// <summary>
    /// Error response details with validation support
    /// </summary>
    public class ErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Target { get; set; }
        public Dictionary<string, string[]>? ValidationErrors { get; set; }
        public List<ErrorDetail>? Details { get; set; }
        public string? StackTrace { get; set; }
        public Dictionary<string, object>? Extensions { get; set; }
    }


}
