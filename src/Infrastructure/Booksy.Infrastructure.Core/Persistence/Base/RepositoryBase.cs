//using Booksy.Core.Application.Abstractions.Persistence;
//using Booksy.Core.Domain.Abstractions.Entities;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using System.Linq.Expressions;
///// <summary>
///// Base repository implementation with common CRUD operations
///// </summary>
//public abstract class RepositoryBase<TEntity, TId> : IRepository<TEntity, TId>
//    where TEntity : class, IAggregateRoot, IEntity<TId>
//    where TId : notnull
//{
//    protected readonly DbContext Context;
//    protected readonly DbSet<TEntity> DbSet;
//    protected readonly ILogger Logger;

//    protected RepositoryBase(DbContext context, ILogger logger)
//    {
//        Context = context;
//        DbSet = context.Set<TEntity>();
//        Logger = logger;
//    }

//    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
//    {
//        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
//    }

//    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
//    {
//        return await DbSet.ToListAsync(cancellationToken);
//    }

//    public virtual async Task<IReadOnlyList<TEntity>> GetAsync(
//        Expression<Func<TEntity, bool>> predicate,
//        CancellationToken cancellationToken = default)
//    {
//        return await DbSet.Where(predicate).ToListAsync(cancellationToken);
//    }

//    public virtual async Task<TEntity?> GetSingleAsync(
//        Expression<Func<TEntity, bool>> predicate,
//        CancellationToken cancellationToken = default)
//    {
//        return await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);
//    }

//    public virtual async Task<bool> ExistsAsync(
//        Expression<Func<TEntity, bool>> predicate,
//        CancellationToken cancellationToken = default)
//    {
//        return await DbSet.AnyAsync(predicate, cancellationToken);
//    }

//    public virtual async Task<int> CountAsync(
//        Expression<Func<TEntity, bool>>? predicate = null,
//        CancellationToken cancellationToken = default)
//    {
//        return predicate == null
//            ? await DbSet.CountAsync(cancellationToken)
//            : await DbSet.CountAsync(predicate, cancellationToken);
//    }

//    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
//    {
//        await DbSet.AddAsync(entity, cancellationToken);
//    }

//    public virtual async Task AddRangeAsync(
//        IEnumerable<TEntity> entities,
//        CancellationToken cancellationToken = default)
//    {
//        await DbSet.AddRangeAsync(entities, cancellationToken);
//    }

//    public virtual void Update(TEntity entity)
//    {
//        DbSet.Update(entity);
//    }

//    public virtual void UpdateRange(IEnumerable<TEntity> entities)
//    {
//        DbSet.UpdateRange(entities);
//    }

//    public virtual void Remove(TEntity entity)
//    {
//        DbSet.Remove(entity);
//    }

//    public virtual void RemoveRange(IEnumerable<TEntity> entities)
//    {
//        DbSet.RemoveRange(entities);
//    }

//    public IQueryable<TEntity> GetQueryable()
//    {
//        return DbSet.AsQueryable();
//    }

//    public Task SaveAsync(TEntity aggregate, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task RemoveAsync(TEntity aggregate, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }
//}

