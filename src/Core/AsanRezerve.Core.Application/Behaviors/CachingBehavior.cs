// ========================================
// AsanRezerve.Core.Application/Behaviors/CachingBehavior.cs
// ========================================
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using AsanRezerve.Core.Application.Abstractions.Caching;
using AsanRezerve.Core.Application.Abstractions.CQRS;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.Core.Application.Behaviors
{
    /// <summary>
    /// Serves queries that opt in (<see cref="IQuery{TResponse}.IsCacheable"/>) from <see cref="HybridCache"/>:
    /// in-process L1 over Redis L2, absolute lifetime, eviction by tag, and one handler run per key when many
    /// requests miss it at once.
    /// <para>Innermost in the pipeline, after validation and authorization, so a cached result is only ever
    /// returned to a caller who passed both.</para>
    /// <para>What is not cached: a null result (a salon that does not exist yet must be findable the moment it
    /// does) and a handler exception. A cache failure never fails the query — it runs uncached.</para>
    /// </summary>
    public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        /// <summary>L1 never outlives this, so a missed invalidation (e.g. on a future second node) converges fast.</summary>
        private static readonly TimeSpan MaxLocalLifetime = TimeSpan.FromSeconds(60);

        private static readonly string Region = typeof(TRequest).Name;
        private static readonly string RegionTag = "query:" + Region;
        private static readonly string KeyPrefix = "q:" + (typeof(TRequest).FullName ?? Region) + ":";

        private readonly HybridCache _cache;
        private readonly ICacheMetrics _metrics;
        private readonly ICacheKeyContributor[] _keyContributors;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(
            HybridCache cache,
            ICacheMetrics metrics,
            IEnumerable<ICacheKeyContributor> keyContributors,
            ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _metrics = metrics;
            _keyContributors = keyContributors.ToArray();
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is not IQuery<TResponse> query || !query.IsCacheable)
            {
                return await next(cancellationToken);
            }

            // HybridCache runs the factory on a pool thread without the caller's ExecutionContext; the handler needs
            // it (IHttpContextAccessor, the request's Activity for trace ids in its logs).
            var invocation = new Invocation(next, ExecutionContext.Capture());

            try
            {
                var key = KeyPrefix + (query.CacheKey ?? HashOf(request)) + Variant();

                var response = await _cache.GetOrCreateAsync(
                    key,
                    invocation,
                    static async (call, ct) =>
                    {
                        call.Ran = true;
                        TResponse result;
                        try
                        {
                            result = await call.RunHandler(ct).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            throw new HandlerFailedException(ex);
                        }

                        return result is null ? throw new UncacheableResultException() : result;
                    },
                    EntryOptions(query),
                    Tags(query),
                    cancellationToken).ConfigureAwait(false);

                _metrics.Record(Region, hit: !invocation.Ran);
                return response;
            }
            catch (HandlerFailedException failed)
            {
                ExceptionDispatchInfo.Throw(failed.InnerException!);
                throw; // unreachable
            }
            catch (UncacheableResultException)
            {
                _metrics.Record(Region, hit: false);
                return default!;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // The cache itself failed (key, serialisation, store). Queries are reads, so running the handler
                // again is safe; failing the request over an optimisation is not.
                _logger.LogWarning(ex, "Query cache failed for {QueryType}; serving it uncached", Region);
                return await next(cancellationToken);
            }
        }

        private static HybridCacheEntryOptions? EntryOptions(IQuery<TResponse> query)
        {
            if (query.CacheExpirationSeconds is not int seconds || seconds <= 0)
            {
                return null; // the configured defaults
            }

            var lifetime = TimeSpan.FromSeconds(seconds);
            return new HybridCacheEntryOptions
            {
                Expiration = lifetime,
                LocalCacheExpiration = lifetime < MaxLocalLifetime ? lifetime : MaxLocalLifetime,
            };
        }

        private string Variant()
        {
            if (_keyContributors.Length == 0) return string.Empty;

            var parts = _keyContributors.Select(c => c.Contribute()).Where(v => !string.IsNullOrEmpty(v)).ToArray();
            return parts.Length == 0 ? string.Empty : "|" + string.Join("|", parts);
        }

        private static string[] Tags(IQuery<TResponse> query) =>
            query.CacheTags is { Count: > 0 } tags ? [RegionTag, .. tags] : [RegionTag];

        /// <summary>SHA-256 of the whole query, so every parameter is part of the key.</summary>
        private static string HashOf(TRequest request) =>
            Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(request, request.GetType())));

        private sealed class Invocation(RequestHandlerDelegate<TResponse> next, ExecutionContext? context)
        {
            public RequestHandlerDelegate<TResponse> Next { get; } = next;

            /// <summary>Runs the handler under the caller's ExecutionContext; its awaits keep that context.</summary>
            public Task<TResponse> RunHandler(CancellationToken cancellationToken)
            {
                if (context is null) return Next(cancellationToken);

                Task<TResponse>? task = null;
                ExecutionContext.Run(context, _ => task = Next(cancellationToken), null);
                return task!;
            }

            /// <summary>
            /// Set when this caller's own factory ran (a miss). A caller that joined another's in-flight run, or
            /// found the entry, is a hit.
            /// </summary>
            public volatile bool Ran;
        }

        /// <summary>Carries a handler exception through HybridCache, which must not cache it, back to the caller.</summary>
        private sealed class HandlerFailedException(Exception inner) : Exception(inner.Message, inner);

        /// <summary>Returns a null result without caching it.</summary>
        private sealed class UncacheableResultException : Exception;
    }
}
