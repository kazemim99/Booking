

///// <summary>
///// Evaluates specifications against EF Core queryables
///// </summary>
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

        // Apply ordering
        //if (specification.OrderBy != null)
        //{
        //    query = query.Order(specification.OrderBy);
        //}
        //else 
        //if (specification.OrderByDescending != null)
        //{
        //    query = query.OrderByDescending(specification.OrderByDescending);
        //}

        //// Apply secondary ordering
        //if (specification.ThenBy != null)
        //{
        //    query = ((IOrderedQueryable<TEntity>)query).ThenBy(specification.ThenBy);
        //}
        //else if (specification.ThenByDescending != null)
        //{
        //    query = ((IOrderedQueryable<TEntity>)query).ThenByDescending(specification.ThenByDescending);
        //}

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
