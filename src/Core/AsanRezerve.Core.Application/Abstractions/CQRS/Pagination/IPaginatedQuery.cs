// ========================================
// AsanRezerve.Core.Application/Abstractions/CQRS/IPaginatedQuery.cs
// ========================================
using AsanRezerve.Core.Application.DTOs;

namespace AsanRezerve.Core.Application.Abstractions.CQRS
{
    /// <summary>
    /// Marker interface for paginated queries
    /// </summary>
    /// <typeparam name="TResponse">The type of items in the paginated result</typeparam>
    public interface IPaginatedQuery<TResponse> : IQuery<PagedResult<TResponse>>
    {
        /// <summary>
        /// Gets the pagination parameters
        /// </summary>
        PaginationRequest Pagination { get; }
    }
}


