// Booksy.Core.Application/Extensions/QueryableExtensions.cs
using Booksy.Core.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Booksy.Core.Application.Extensions;

/// <summary>
/// Extension methods for generic pagination support
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Apply pagination to any IQueryable
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        PaginationRequest pagination)
    {
        return query
            .ApplySorting(pagination.SortBy)
            .Skip(pagination.Skip)
            .Take(pagination.Take);
    }

    /// <summary>
    /// Create paginated result from IQueryable with automatic count
    /// </summary>
    public static async Task<PagedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplyPagination(pagination)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <summary>
    /// Create paginated result with projection
    /// </summary>
    public static async Task<PagedResult<TResult>> ToPaginatedResultAsync<T, TResult>(
        this IQueryable<T> query,
        PaginationRequest pagination,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplyPagination(pagination)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResult>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <summary>
    /// Apply sorting to IQueryable
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        List<SortingDescriptor> sortingDescriptors)
    {
        if (!sortingDescriptors.Any())
            return query;

        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var (descriptor, index) in sortingDescriptors.Select((d, i) => (d, i)))
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, descriptor.FieldName);
            var lambda = Expression.Lambda<Func<T, object>>(
                Expression.Convert(property, typeof(object)), parameter);

            if (index == 0)
            {
                orderedQuery = descriptor.Direction == SortDirection.Ascending
                    ? query.OrderBy(lambda)
                    : query.OrderByDescending(lambda);
            }
            else
            {
                orderedQuery = descriptor.Direction == SortDirection.Ascending
                    ? orderedQuery!.ThenBy(lambda)
                    : orderedQuery!.ThenByDescending(lambda);
            }
        }

        return orderedQuery ?? query;
    }
}


