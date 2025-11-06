// ========================================
// Booksy.Core.Application/Behaviors/IdempotencyBehavior.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.Core.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior for command idempotency using distributed cache
    /// Prevents duplicate command execution by caching command results
    /// </summary>
    public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommand<TResponse>
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

        public IdempotencyBehavior(
            IDistributedCache cache,
            ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Skip idempotency if no key provided
            if (request.IdempotencyKey == null)
            {
                return await next();
            }

            var cacheKey = $"idempotency:{typeof(TRequest).Name}:{request.IdempotencyKey}";

            try
            {
                // Check if this command was already processed
                var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

                if (cached != null)
                {
                    _logger.LogInformation(
                        "Duplicate command detected. Returning cached result for {CommandType} with IdempotencyKey: {Key}",
                        typeof(TRequest).Name,
                        request.IdempotencyKey);

                    // Deserialize and return cached response
                    var cachedResponse = JsonSerializer.Deserialize<TResponse>(cached);
                    return cachedResponse!;
                }

                // Process the command
                _logger.LogDebug(
                    "Processing command {CommandType} with IdempotencyKey: {Key}",
                    typeof(TRequest).Name,
                    request.IdempotencyKey);

                var response = await next();

                // Cache the response for 24 hours
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                };

                var serialized = JsonSerializer.Serialize(response);
                await _cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);

                _logger.LogDebug(
                    "Cached response for command {CommandType} with IdempotencyKey: {Key}",
                    typeof(TRequest).Name,
                    request.IdempotencyKey);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in idempotency behavior for command {CommandType} with IdempotencyKey: {Key}",
                    typeof(TRequest).Name,
                    request.IdempotencyKey);

                // On error, don't cache and let the exception propagate
                throw;
            }
        }
    }
}
