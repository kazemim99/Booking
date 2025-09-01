// ========================================
// Booksy.Core.Application/Abstractions/CQRS/IPaginatedQuery.cs
// ========================================
using Booksy.Core.Application.DTOs;

namespace Booksy.Core.Application.Abstractions.CQRS;

/// <summary>
/// Base interface for paginated queries
/// </summary>
/// <typeparam name="TResult">Result type</typeparam>
public interface IPaginatedQuery<TResult> : IQuery<PagedResult<TResult>>
{
    int PageNumber { get; }
    int PageSize { get; }
    string? OrderBy { get; }
    bool Descending { get; }
}
