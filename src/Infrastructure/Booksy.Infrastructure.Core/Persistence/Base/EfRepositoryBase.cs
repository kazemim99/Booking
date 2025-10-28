// 📁 Booksy.Core.Infrastructure/Persistence/Base/EfReadEfRepositoryBase.cs - NEW
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Booksy.Infrastructure.Core.Persistence.Base;

/// <summary>
/// Combined read/write repository base for domain repositories
/// </summary>
public abstract class EfRepositoryBase<TEntity, TId, TContext> : IReadRepository<TEntity, TId>, IWriteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
    where TContext : DbContext
{
    protected readonly TContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected EfRepositoryBase(TContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<TEntity>();
    }

    // ✅ Read methods
    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id! }, cancellationToken);
    }

    public virtual async Task<TEntity?> GetSingleAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();
        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);

        return await query.AnyAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();
        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);

        return await query.CountAsync(cancellationToken);
    }

    // ✅ Write methods
    public virtual async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entry = Context.Entry(entity);

        if (entry.State == EntityState.Detached)
        {
            await DbSet.AddAsync(entity, cancellationToken);
        }
        else if (entry.State == EntityState.Modified)
        {
            DbSet.Update(entity);
        }
    }

    public virtual Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task RemoveByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await RemoveAsync(entity, cancellationToken);
        }
    }

    protected virtual IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> specification)
    {
        var query = DbSet.AsQueryable(); 

        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        return query;
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        DbSet.Update(entity);
    }

    /// <summary>
    /// Get paginated results using specification and pagination request
    /// </summary>
    public virtual async Task<PagedResult<TEntity>> GetPaginatedAsync(
        ISpecification<TEntity> specification,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        // Get total count without pagination
        var totalCount = await CountAsync(specification, cancellationToken);

        // Build query with specification
        var query = ApplySpecification(specification);

        // Apply pagination-specific sorting if no ordering in specification
        query = ApplyPaginationSorting(query, specification, pagination);

        // Apply pagination
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <summary>
    /// Get paginated results with projection using specification and pagination request
    /// </summary>
    public virtual async Task<PagedResult<TProjection>> GetPaginatedAsync<TProjection>(
        ISpecification<TEntity> specification,
        PaginationRequest pagination,
        Expression<Func<TEntity, TProjection>> projection,
        CancellationToken cancellationToken = default)
    {
        // Get total count without pagination and projection
        var totalCount = await CountAsync(specification, cancellationToken);

        // Build query with specification
        var query = ApplySpecification(specification);

        // Apply pagination-specific sorting if no ordering in specification
        query = ApplyPaginationSorting(query, specification, pagination);

        // Apply projection and pagination
        var items = await query.AsNoTracking()
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .Select(projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<TProjection>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }


    /// <summary>
    /// Apply sorting from pagination request if specification doesn't have ordering
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyPaginationSorting(
        IQueryable<TEntity> query,
        ISpecification<TEntity> specification,
        PaginationRequest pagination)
    {
        // If specification is orderable and has ordering, don't override
        if (specification is IOrderableSpecification<TEntity> orderableSpec && orderableSpec.OrderBy.Any())
        {
            return query; // Specification handles ordering
        }

        // Apply pagination sorting
        return ApplySortingDescriptors(query, pagination.SortBy);
    }

    /// <summary>
    /// Apply sorting descriptors to query
    /// </summary>
    protected virtual IQueryable<TEntity> ApplySortingDescriptors(
        IQueryable<TEntity> query,
        List<SortingDescriptor> sortingDescriptors)
    {
        if (!sortingDescriptors.Any())
        {
            return ApplyDefaultSorting(query);
        }

        IOrderedQueryable<TEntity>? orderedQuery = null;

        foreach (var (descriptor, index) in sortingDescriptors.Select((d, i) => (d, i)))
        {
            var sortExpression = GetSortExpression(descriptor.FieldName);
            if (sortExpression == null) continue;

            if (index == 0)
            {
                orderedQuery = descriptor.Direction == SortDirection.Ascending
                    ? query.OrderBy(sortExpression)
                    : query.OrderByDescending(sortExpression);
            }
            else
            {
                orderedQuery = descriptor.Direction == SortDirection.Ascending
                    ? orderedQuery!.ThenBy(sortExpression)
                    : orderedQuery!.ThenByDescending(sortExpression);
            }
        }

        return orderedQuery ?? ApplyDefaultSorting(query);
    }

    /// <summary>
    /// Get sort expression for a field name - Override in derived classes for entity-specific sorting
    /// </summary>
    protected virtual Expression<Func<TEntity, object>>? GetSortExpression(string fieldName)
    {
        // Default implementation - tries to find property by name
        var parameter = Expression.Parameter(typeof(TEntity), "x");

        try
        {
            var property = typeof(TEntity).GetProperty(fieldName);
            if (property != null)
            {
                var propertyAccess = Expression.Property(parameter, property);
                var objectConversion = Expression.Convert(propertyAccess, typeof(object));
                return Expression.Lambda<Func<TEntity, object>>(objectConversion, parameter);
            }
        }
        catch
        {
            // Property not found or not accessible
        }

        return null;
    }

    /// <summary>
    /// Apply default sorting when no sort is specified - Override in derived classes
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyDefaultSorting(IQueryable<TEntity> query)
    {
        // Default: try to sort by Id if it exists
        try
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var idProperty = Expression.Property(parameter, "Id");
            var lambda = Expression.Lambda<Func<TEntity, object>>(
                Expression.Convert(idProperty, typeof(object)), parameter);
            return query.OrderBy(lambda);
        }
        catch
        {
            // If Id property doesn't exist, return unsorted
            return query;
        }
    }
}