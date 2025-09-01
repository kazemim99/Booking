// Booksy.API.Infrastructure/Middleware/ApiResponseMiddleware.cs

// Booksy.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
using Booksy.Core.Domain.Infrastructure.Middleware.Responses;


// Booksy.API.Infrastructure/ExceptionHandling/IExceptionHandlerStrategy.cs
namespace Booksy.Core.Domain.Infrastructure.Middleware
{
    /// <summary>
    /// Strategy pattern for exception handling
    /// </summary>
    public interface IExceptionHandlerStrategy
    {
        (int StatusCode, ErrorResponse ErrorResponse) Handle(Exception exception);
    }
}
