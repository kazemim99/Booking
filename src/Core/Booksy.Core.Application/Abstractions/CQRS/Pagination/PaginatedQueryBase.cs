// ========================================
// Booksy.Core.Application/Abstractions/CQRS/IPaginatedQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.DTOs;

namespace Booksy.Core.Application.CQRS
{
    /// <summary>
    /// Base class for paginated queries to reduce boilerplate
    /// </summary>
    /// <typeparam name="TResponse">The type of items in the paginated result</typeparam>
    public abstract record PaginatedQueryBase<TResponse> : IPaginatedQuery<TResponse>
    {
        /// <summary>
        /// Pagination parameters
        /// </summary>
        public PaginationRequest Pagination { get; init; } = PaginationRequest.Default;

        /// <summary>
        /// Indicates if the query result can be cached
        /// </summary>
        public virtual bool IsCacheable => false;

        /// <summary>
        /// Cache key for the query (override if cacheable)
        /// </summary>
        public virtual string? CacheKey => null;

        /// <summary>
        /// Cache expiration in seconds (override if cacheable)
        /// </summary>
        public virtual int? CacheExpirationSeconds => null;

        protected PaginatedQueryBase() { }

        protected PaginatedQueryBase(PaginationRequest pagination)
        {
            Pagination = pagination;
        }
    }
}

