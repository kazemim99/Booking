// ========================================
// Booksy.Core.Application/Behaviors/LoggingBehavior.cs
// ========================================
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Booksy.Core.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior for request/response logging
    /// </summary>
    public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestGuid = Guid.NewGuid();

            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            _logger.LogInformation(
                "Handling request {RequestName} ({RequestGuid}): {Request}",
                requestName,
                requestGuid,
                requestJson);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await next();

                stopwatch.Stop();

                _logger.LogInformation(
                    "Request {RequestName} ({RequestGuid}) handled successfully in {ElapsedMilliseconds}ms",
                    requestName,
                    requestGuid,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Request {RequestName} ({RequestGuid}) failed after {ElapsedMilliseconds}ms",
                    requestName,
                    requestGuid,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}
