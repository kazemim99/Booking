// AsanRezerve.API.Infrastructure/Middleware/ApiResponseMiddleware.cs

// AsanRezerve.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
using AsanRezerve.Core.Domain.Infrastructure.Middleware.Responses;


// AsanRezerve.API.Infrastructure/ExceptionHandling/IExceptionHandlerStrategy.cs
namespace AsanRezerve.Core.Domain.Infrastructure.Middleware
{
    /// <summary>
    /// Strategy pattern for exception handling
    /// </summary>
    public interface IExceptionHandlerStrategy
    {
        (int StatusCode, ErrorResponse ErrorResponse) Handle(Exception exception);
    }
}
