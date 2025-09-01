// Booksy.API.Infrastructure/Middleware/ApiResponseMiddleware.cs
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Booksy.Core.Domain.Infrastructure.Middleware.Responses;
using Booksy.Core.Domain.Infrastructure.Configuration;
using Booksy.Core.Domain.Infrastructure.Middleware;

namespace Booksy.Core.Domain.Infrastructure.Middleware.Handlers
{
    /// <summary>
    /// Chain of Responsibility implementation for exception handling
    /// </summary>

    public class ChainedExceptionHandler : IExceptionHandlerStrategy
    {
        private readonly List<IExceptionHandler> _handlers;
        private readonly ApiResponseOptions _options;
        private readonly ILogger<ChainedExceptionHandler> _logger;

        public ChainedExceptionHandler(
            IEnumerable<IExceptionHandler> handlers,
            IOptions<ApiResponseOptions> options,
            ILogger<ChainedExceptionHandler> logger)
        {
            _handlers = handlers.OrderBy(h => h.Order).ToList();
            _options = options.Value;
            _logger = logger;
        }

        public (int StatusCode, ErrorResponse ErrorResponse) Handle(Exception exception)
        {
            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(exception))
                {
                    return handler.Handle(exception, _options);
                }
            }

            // Default handler for unhandled exceptions
            return HandleUnknownException(exception);
        }

        private (int StatusCode, ErrorResponse ErrorResponse) HandleUnknownException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception in API pipeline");

            var error = new ErrorResponse
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = exception.InnerException != null ? exception.InnerException.Message : "An unexpected error occurred while processing your request",
               
                Extensions = new Dictionary<string, object>
                {
                    ["timestamp"] = DateTimeOffset.UtcNow,
                    ["type"] = exception.GetType().Name
                }
            };

            if (_options.IncludeStackTrace)
            {
                error.StackTrace = exception.StackTrace;
            }

            return ((int)HttpStatusCode.InternalServerError, error);
        }
    }
}
