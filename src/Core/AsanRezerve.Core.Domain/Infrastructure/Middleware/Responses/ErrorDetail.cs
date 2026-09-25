// AsanRezerve.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
// AsanRezerve.API.Infrastructure/Responses/ApiResponse.cs
namespace AsanRezerve.Core.Domain.Infrastructure.Middleware.Responses
{
    /// <summary>
    /// Detailed error information for complex scenarios
    /// </summary>
    public class ErrorDetail
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Target { get; set; }
    }
}
