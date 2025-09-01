// ========================================
// Booksy.Core.Application/Abstractions/CQRS/IQuery.cs
// ========================================
using MediatR;

namespace Booksy.Core.Application.Abstractions.CQRS
{
    /// <summary>
    /// Marker interface for queries
    /// </summary>
    /// <typeparam name="TResponse">The type of the response</typeparam>
    public interface IQuery<out TResponse> : IRequest<TResponse>
    {
        /// <summary>
        /// Indicates whether the query result can be cached
        /// </summary>
        bool IsCacheable => false;

        /// <summary>
        /// Gets the cache key for this query
        /// </summary>
        string? CacheKey => null;

        /// <summary>
        /// Gets the cache expiration in seconds
        /// </summary>
        int? CacheExpirationSeconds => null;
    }
}

