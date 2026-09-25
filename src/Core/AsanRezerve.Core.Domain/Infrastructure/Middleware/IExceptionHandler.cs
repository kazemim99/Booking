// AsanRezerve.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
using AsanRezerve.Core.Domain.Infrastructure.Configuration;
using AsanRezerve.Core.Domain.Infrastructure.Middleware.Responses;

namespace AsanRezerve.Core.Domain.Infrastructure.Middleware
{
    /// <summary>
    /// Base interface for exception handlers
    /// </summary>
    public interface IExceptionHandler
    {
        int Order { get; }
        bool CanHandle(Exception exception);
        (int StatusCode, ErrorResponse ErrorResponse) Handle(Exception exception, ApiResponseOptions options);
    }
}
