

///// <summary>
///// Evaluates specifications against EF Core queryables
///// </summary>
using Booksy.Core.Domain.Abstractions.Entities.Specifications;
using Microsoft.EntityFrameworkCore;

public class SpecificationEvaluator<TEntity> where TEntity : class
{
    public static IQueryable<TEntity> GetQuery(
        IQueryable<TEntity> inputQuery,
        IAdvancedSpecification<TEntity> specification)
    {
        var query = inputQuery;

        // Apply criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        query = specification.Includes.Aggregate(
            query,
            (current, include) => current.Include(include));

        // Apply string includes
        query = specification.IncludeStrings.Aggregate(
            query,
            (current, includeString) => current.Include(includeString));

        // Apply ordering.
        //
        // This was commented out, so EVERY specification-based query in the system came back unordered
        // regardless of what it asked for: provider search returned the same sequence for SortBy=name,
        // SortBy=rating and SortBy=distance alike. It failed silently — a plausible list arrived, simply not in
        // the requested order — which is why it survived so long.
        //
        // The dead code referenced a single `specification.OrderBy` expression, but the API is now a LIST of
        // OrderExpression<T> (primary + subsequent ThenBy). That signature change is presumably why it was
        // disabled and never restored; this walks the list instead.
        //
        // Ordering must precede the Skip/Take below, or paging would slice an unordered sequence and pages
        // could repeat or drop rows.
        if (specification.OrderBy.Count > 0)
        {
            IOrderedQueryable<TEntity>? ordered = null;

            foreach (var order in specification.OrderBy)
            {
                // The first expression establishes the ordering; IsSubsequentOrdering marks the ThenBy chain.
                // A ThenBy arriving first (no primary) is still treated as the primary, so a mis-built
                // specification degrades to a sensible order rather than throwing at runtime.
                if (ordered is null)
                {
                    ordered = order.Direction == OrderDirection.Descending
                        ? query.OrderByDescending(order.KeySelector)
                        : query.OrderBy(order.KeySelector);
                }
                else
                {
                    ordered = order.Direction == OrderDirection.Descending
                        ? ordered.ThenByDescending(order.KeySelector)
                        : ordered.ThenBy(order.KeySelector);
                }
            }

            query = ordered!;
        }

        // Apply grouping
        if (specification.GroupBy != null)
        {
            query = query.GroupBy(specification.GroupBy).SelectMany(x => x);
        }

        // Apply distinct
        if (specification.IsDistinct)
        {
            query = query.Distinct();
        }

        // Apply paging
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        // Apply tracking
        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        // Apply query filters
        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }



        return query;
    }

    public static async Task<List<TEntity>> ToListAsync(
        IQueryable<TEntity> inputQuery,
        IAdvancedSpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = GetQuery(inputQuery, specification);
        return await query.ToListAsync(cancellationToken);
    }

    public static async Task<int> CountAsync(
        IQueryable<TEntity> inputQuery,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = inputQuery;
        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);

        return await query.CountAsync(cancellationToken);
    }
}
