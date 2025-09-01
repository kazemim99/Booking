// ========================================
// Booksy.Core.Application/Behaviors/CachingBehavior.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.Core.Application.Behaviors
{
    /// <summary>
    /// Pipeline behavior for caching query results
    /// </summary>
    public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public CachingBehavior(
            IDistributedCache cache,
            ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Only cache queries that opt-in to caching
            if (request is not IQuery<TResponse> query || !query.IsCacheable)
            {
                return await next();
            }

            var cacheKey = GetCacheKey(query);

            try
            {
                // Try to get from cache
                var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

                if (!string.IsNullOrEmpty(cachedValue))
                {
                    _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);

                    var deserializedValue = JsonSerializer.Deserialize<TResponse>(cachedValue, _jsonOptions);
                    if (deserializedValue != null)
                    {
                        return deserializedValue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving from cache for key: {CacheKey}", cacheKey);
                // Continue to execute the request if cache fails
            }

            // Execute the request
            var response = await next();

            // Cache the result
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromSeconds(query.CacheExpirationSeconds ?? 300)
                };

                var serializedValue = JsonSerializer.Serialize(response, _jsonOptions);
                await _cache.SetStringAsync(cacheKey, serializedValue, cacheOptions, cancellationToken);

                _logger.LogDebug("Cached result for key: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching result for key: {CacheKey}", cacheKey);
                // Don't throw, just log the error
            }

            return response;
        }

        private static string GetCacheKey(IQuery<TResponse> query)
        {
            if (!string.IsNullOrEmpty(query.CacheKey))
            {
                return query.CacheKey;
            }

            // Generate cache key based on query type and properties
            var queryType = query.GetType().Name;
            var queryJson = JsonSerializer.Serialize(query);
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(queryJson));
            var hashString = Convert.ToBase64String(hash);

            return $"{queryType}:{hashString}";
        }
    }
}