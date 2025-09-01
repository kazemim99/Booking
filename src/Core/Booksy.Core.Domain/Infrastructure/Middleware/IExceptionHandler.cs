// Booksy.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
using Booksy.Core.Domain.Infrastructure.Configuration;
using Booksy.Core.Domain.Infrastructure.Middleware.Responses;

namespace Booksy.Core.Domain.Infrastructure.Middleware
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
