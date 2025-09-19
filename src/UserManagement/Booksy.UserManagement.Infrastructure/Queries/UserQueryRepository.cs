using Booksy.Core.Application.DTOs;
using Booksy.UserManagement.Application.Abstractions.Queries;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Booksy.UserManagement.Infrastructure.Queries;

public class UserQueryRepository : IUserQueryRepository
{
    private readonly UserManagementDbContext _context;

    public UserQueryRepository(UserManagementDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TResult>> GetPagedAsync<TResult>(
        IAdvancedSpecification<User> specification,
        Expression<Func<User, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking();

        // Get total count without paging
        var countQuery = ApplySpecificationForCount(query, specification);
        var totalCount = await countQuery.CountAsync(cancellationToken);

        // Apply full specification with paging for items
        var itemsQuery = ApplySpecification(query, specification).Select(selector);
        var items = await itemsQuery.ToListAsync(cancellationToken);

        var pageNumber = specification.IsPagingEnabled ? (specification.Skip / specification.Take) + 1 : 1;
        var pageSize = specification.IsPagingEnabled ? specification.Take : totalCount;

        return new PagedResult<TResult>(items, totalCount, pageNumber, pageSize);
    }

    private IQueryable<User> ApplySpecificationForCount(IQueryable<User> query, ISpecification<User> specification)
    {
        // Apply only criteria and includes for count, not sorting or paging
        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);

        return query;
    }

    private IQueryable<User> ApplySpecification(IQueryable<User> query, ISpecification<User> specification)
    {
        // Apply criteria
        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);

        // Apply includes
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (specification is IAdvancedSpecification<User> advancedSpec)
        {
            // Apply ordering
            //if (advancedSpec.OrderBy != null)
            //    query = query.OrderBy(advancedSpec.OrderBy);

            //else 
            
            //if (advancedSpec.OrderByDescending != null)
            //    query = query.OrderByDescending(advancedSpec.OrderByDescending);

            // Apply secondary ordering
            //if (advancedSpec.ThenBy != null && query is IOrderedQueryable<User> orderedQuery)
            //    query = orderedQuery.ThenBy(advancedSpec.ThenBy);
            //else if (advancedSpec.ThenByDescending != null && query is IOrderedQueryable<User> orderedQueryDesc)
            //    query = orderedQueryDesc.ThenByDescending(advancedSpec.ThenByDescending);

            // Apply distinct
            if (advancedSpec.IsDistinct)
                query = query.Distinct();

            // Apply paging
            if (advancedSpec.IsPagingEnabled)
                query = query.Skip(advancedSpec.Skip).Take(advancedSpec.Take);
        }

        return query;
    }

    public async Task<IReadOnlyList<TResult>> GetListAsync<TResult>(
        ISpecification<User> specification,
        Expression<Func<User, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(_context.Users.AsNoTracking(), specification);
        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    public async Task<TResult?> GetSingleAsync<TResult>(
        ISpecification<User> specification,
        Expression<Func<User, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(_context.Users.AsNoTracking(), specification);
        return await query.Select(selector).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        ISpecification<User> specification,
        CancellationToken cancellationToken = default)
    {
        return await SpecificationEvaluator<User>.CountAsync(_context.Users, specification, cancellationToken);
    }

 

    private ISpecification<User> CloneSpecificationWithoutPaging
        (IAdvancedSpecification<User> specification)
    {
        specification.SetCriteria(specification.Criteria);
        return specification;

    }
}

