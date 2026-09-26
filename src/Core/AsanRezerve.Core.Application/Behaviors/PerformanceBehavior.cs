// ========================================
// AsanRezerve.Core.Application/Behaviors/PerformanceBehavior.cs
// ========================================
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AsanRezerve.Core.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior for monitoring request performance
    /// </summary>
    public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
        private readonly int _warningThresholdMs;

        public PerformanceBehavior(
            ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
            int warningThresholdMs = 500)
        {
            _logger = logger;
            _warningThresholdMs = warningThresholdMs;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            var response = await next();

            stopwatch.Stop();

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            if (elapsedMilliseconds > _warningThresholdMs)
            {
                var requestName = typeof(TRequest).Name;

                // One event; the request is destructured so the logging pipeline masks its secrets.
                _logger.LogWarning(
                    "Slow request {RequestName} took {ElapsedMs} ms (threshold {ThresholdMs} ms) {@Request}",
                    requestName,
                    elapsedMilliseconds,
                    _warningThresholdMs,
                    request);
            }

            return response;
        }
    }
}