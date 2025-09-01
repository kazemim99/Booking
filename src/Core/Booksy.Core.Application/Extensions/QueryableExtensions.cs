// Booksy.Core.Application/Extensions/QueryableExtensions.cs
using Booksy.Core.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Booksy.Core.Application.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> query,
        PaginationRequest request)
    {
        return query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);
    }

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

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationRequest request,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplySorting(request.SortBy)
            .ApplyPaging(request)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, request.PageNumber, request.PageSize);
    }
}