// ========================================
// AsanRezerve.Core.Application/Abstractions/CQRS/IQuery.cs
// ========================================
using MediatR;

namespace AsanRezerve.Core.Application.Abstractions.CQRS
{
    /// <summary>
    /// Marker interface for queries
    /// </summary>
    /// <typeparam name="TResponse">The type of the response</typeparam>
    public interface IQuery<out TResponse> : IRequest<TResponse>
    {
        /// <summary>
        /// Opts the query into the query cache (<c>CachingBehavior</c>). Only for results that are the same for
        /// every caller allowed to run the query, and whose changes evict <see cref="CacheTags"/> — otherwise a
        /// cached result is served stale for its whole lifetime.
        /// </summary>
        /// <remarks>
        /// Implement these members with exactly the interface's types (<c>int?</c>, <c>string?</c>, ...): a
        /// property of another type (<c>int CacheExpirationSeconds</c>) does not implement the interface member and
        /// is silently ignored.
        /// </remarks>
        bool IsCacheable => false;

        /// <summary>
        /// Optional key, namespaced by the query type. Leave null to key on every property of the query, which is
        /// what keeps two queries that differ in one filter apart.
        /// </summary>
        string? CacheKey => null;

        /// <summary>Absolute lifetime in seconds; null for the configured default (<c>Cache:DefaultExpirationMinutes</c>).</summary>
        int? CacheExpirationSeconds => null;

        /// <summary>Tags the cached result is evicted by (for example <c>provider:{id}</c>).</summary>
        IReadOnlyCollection<string>? CacheTags => null;
    }
}

