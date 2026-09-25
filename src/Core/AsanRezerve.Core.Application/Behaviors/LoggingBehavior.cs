// ========================================
// AsanRezerve.Core.Application/Behaviors/LoggingBehavior.cs
// ========================================
using System.Diagnostics;
using AsanRezerve.Core.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.Core.Application.Behaviors
{
    /// <summary>
    /// Logs MediatR requests at Debug — destructured (<c>{@Request}</c>), so the logging pipeline's masking step
    /// can redact passwords, codes and tokens inside them — and a failure once.
    /// <para>The HTTP request already gets one completion event, so a request handled normally writes nothing
    /// above Debug here. A failure during an HTTP request is left to the exception handler, which logs it with the
    /// status it became. Outside a request (background jobs, CAP subscribers) nobody else would log it: an
    /// unexpected failure is an Error with its stack trace, an expected rejection a Warning without one.</para>
    /// </summary>
    public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        private static readonly string RequestName = typeof(TRequest).Name;

        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public LoggingBehavior(
            ILogger<LoggingBehavior<TRequest, TResponse>> logger,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var debug = _logger.IsEnabled(LogLevel.Debug);
            if (debug)
            {
                _logger.LogDebug("Handling {RequestName} {@Request}", RequestName, request);
            }

            var started = Stopwatch.GetTimestamp();
            try
            {
                var response = await next(cancellationToken);

                if (debug)
                {
                    _logger.LogDebug("Handled {RequestName} in {ElapsedMs} ms",
                        RequestName, Math.Round(Stopwatch.GetElapsedTime(started).TotalMilliseconds, 1));
                }

                return response;
            }
            catch (Exception ex)
            {
                var elapsedMs = Math.Round(Stopwatch.GetElapsedTime(started).TotalMilliseconds, 1);

                if (_httpContextAccessor?.HttpContext is not null)
                {
                    _logger.LogDebug("{RequestName} failed after {ElapsedMs} ms: {ExceptionType}", RequestName, elapsedMs, ex.GetType().Name);
                }
                else if (IsExpected(ex))
                {
                    _logger.LogWarning("{RequestName} was rejected after {ElapsedMs} ms: {ExceptionType} {ExceptionMessage}",
                        RequestName, elapsedMs, ex.GetType().Name, ex.Message);
                }
                else
                {
                    _logger.LogError(ex, "{RequestName} failed after {ElapsedMs} ms", RequestName, elapsedMs);
                }

                throw;
            }
        }

        /// <summary>A rejection the caller caused (the 4xx family), as opposed to a fault of ours.</summary>
        internal static bool IsExpected(Exception exception) => exception switch
        {
            Exceptions.ExternalServiceException => false,
            AsanRezerve.Core.Application.Exceptions.ApplicationException or DomainException => true,
            FluentValidation.ValidationException => true,
            Exceptions.IdempotencyConflictException or Exceptions.TooManyRequestsException => true,
            UnauthorizedAccessException { InnerException: not IOException } => true,
            ArgumentException or KeyNotFoundException or OperationCanceledException => true,
            _ => exception.GetType().Name == "ValidationException",
        };
    }
}
