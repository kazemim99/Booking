//using Booksy.Core.Application.Abstractions.Persistence;
//using Booksy.Core.Application.DTOs;
//using Booksy.Core.Domain.Abstractions.Entities;
//using Microsoft.EntityFrameworkCore;
//using System.Linq.Expressions;

//namespace Booksy.Infrastructure.Core.Persistence.Base;

///// <summary>
///// Base read-only repository for query operations
///// </summary>
//public  class ReadRepositoryBase<TEntity, TId> : IReadRepository<TEntity, TId>
//    where TEntity : class, IEntity<TId>
//    where TId : notnull
//{
//    protected readonly DbContext Context;
//    protected readonly IQueryable<TEntity> Query;

//    protected ReadRepositoryBase(DbContext context)
//    {
//        Context = context;
//        Query = context.Set<TEntity>().AsNoTracking();
//    }

//    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
//    {
//        return await Query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
//    }

//    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
//    {
//        return await Query.ToListAsync(cancellationToken);
//    }

//    public virtual async Task<IReadOnlyList<TEntity>> GetAsync(
//        Expression<Func<TEntity, bool>> predicate,
//        CancellationToken cancellationToken = default)
//    {
//        return await Query.Where(predicate).ToListAsync(cancellationToken);
//    }


//    public virtual async Task<bool> ExistsAsync(
//        Expression<Func<TEntity, bool>> predicate,
//        CancellationToken cancellationToken = default)
//    {
//        return await Query.AnyAsync(predicate, cancellationToken);
//    }

//    public virtual async Task<int> CountAsync(
//        Expression<Func<TEntity, bool>>? predicate = null,
//        CancellationToken cancellationToken = default)
//    {
//        return predicate == null
//            ? await Query.CountAsync(cancellationToken)
//            : await Query.Where(predicate).CountAsync(cancellationToken);
//    }

//    public IQueryable<TEntity> GetQueryable()
//    {
//        return Query;
//    }

//    public Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
//    {

//        throw new NotImplementedException();
//    }

//    public Task<TEntity?> FindSingleAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }


//    public Task<IReadOnlyList<TEntity>> GetWithIncludesAsync(Expression<Func<TEntity, bool>>? predicate = null, params Expression<Func<TEntity, object>>[] includes)
//    {
//        throw new NotImplementedException();
//    }
//}
