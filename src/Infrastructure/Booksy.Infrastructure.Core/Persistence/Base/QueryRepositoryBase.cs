using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Booksy.Infrastructure.Core.Persistence.Base
{
    public class QueryRepositoryBase<TEntity, TId> : IQueryRepositoryBase<TEntity, TId>
        where TEntity : class, IEntity<TId>
       where TId : notnull
    {
        private readonly DbContext _context;
        public QueryRepositoryBase(DbContext context) 
        {
            _context = context;
        }

        public async Task<PagedResult<TResult>> GetPagedAsync<TResult>(
            IAdvancedSpecification<TEntity> specification,
            Expression<Func<TEntity, TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Set<TEntity>().AsNoTracking();

            // Get total count without paging
            var countQuery = ApplySpecificationForCount(query, specification);
            var totalCount = await countQuery.CountAsync(cancellationToken);

            // Apply full specification with paging for items
            var itemsQuery = ApplySpecification(query, specification).Select(selector);
            var items = await itemsQuery.ToListAsync(cancellationToken);

            var pageNumber = specification.IsPagingEnabled ? specification.Skip / specification.Take + 1 : 1;
            var pageSize = specification.IsPagingEnabled ? specification.Take : totalCount;

            return new PagedResult<TResult>(items, totalCount, pageNumber, pageSize);
        }

        public IQueryable<TEntity> ApplySpecificationForCount(IQueryable<TEntity> query, ISpecification<TEntity> specification)
        {
            // Apply only criteria and includes for count, not sorting or paging
            if (specification.Criteria != null)
                query = query.Where(specification.Criteria);

            return query;
        }

        public IQueryable<TEntity> ApplySpecification(IQueryable<TEntity> query, ISpecification<TEntity> specification)
        {
            // Apply criteria
            if (specification.Criteria != null)
                query = query.Where(specification.Criteria);

            // Apply includes
            query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
            query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

            if (specification is IAdvancedSpecification<TEntity> advancedSpec)
            {
                // Apply ordering
                //if (advancedSpec.OrderBy != null)
                //    query = query.OrderBy(advancedSpec.OrderBy);

                //else 

                //if (advancedSpec.OrderByDescending != null)
                //    query = query.OrderByDescending(advancedSpec.OrderByDescending);

                // Apply secondary ordering
                //if (advancedSpec.ThenBy != null && query is IOrderedQueryable<TEntity> orderedQuery)
                //    query = orderedQuery.ThenBy(advancedSpec.ThenBy);
                //else if (advancedSpec.ThenByDescending != null && query is IOrderedQueryable<TEntity> orderedQueryDesc)
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
            ISpecification<TEntity> specification,
            Expression<Func<TEntity, TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            var query = ApplySpecification(_context.Set<TEntity>().AsNoTracking(), specification);
            return await query.Select(selector).ToListAsync(cancellationToken);
        }

        public async Task<TResult?> GetSingleAsync<TResult>(
            ISpecification<TEntity> specification,
            Expression<Func<TEntity, TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            var query = ApplySpecification(_context.Set<TEntity>().AsNoTracking(), specification);
            return await query.Select(selector).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> CountAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default)
        {
            return await SpecificationEvaluator<TEntity>.CountAsync(_context.Set<TEntity>(), specification, cancellationToken);
        }



        private ISpecification<TEntity> CloneSpecificationWithoutPaging
            (IAdvancedSpecification<TEntity> specification)
        {
            specification.SetCriteria(specification.Criteria);
            return specification;

        }

    
    }
}