//using Booksy.Core.Application.Abstractions.Persistence;
//using Booksy.Core.Domain.Abstractions.Entities;
//using Booksy.Core.Domain.Base;
//using Microsoft.EntityFrameworkCore;

//namespace Booksy.Infrastructure.Core.Persistence.Base;


///// <summary>
///// Base write-only repository for command operations
///// </summary>
//public  class WriteRepositoryBase<TEntity, TId> : IWriteRepository<TEntity, TId>
//    where TEntity : AggregateRoot<TId>, IEntity<TId>
//    where TId : notnull
//{
//    protected readonly DbContext Context;
//    protected readonly DbSet<TEntity> DbSet;

//    public IUnitOfWork UnitOfWork => throw new NotImplementedException();

//    protected WriteRepositoryBase(DbContext context)
//    {
//        Context = context;
//        DbSet = context.Set<TEntity>();
//    }

//    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
//    {
//        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
//    }

//    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
//    {
//        await DbSet.AddAsync(entity, cancellationToken);
//    }

//    public virtual void Update(TEntity entity)
//    {
//        DbSet.Update(entity);
//    }

//    public virtual void Remove(TEntity entity)
//    {
//        DbSet.Remove(entity);
//    }

//    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
//    {
//        var entity = await DbSet.FindAsync(new object[] { id }, cancellationToken);
//        return entity != null;
//    }

//    Task<TEntity> IWriteRepository<TEntity, TId>.AddAsync(TEntity entity, CancellationToken cancellationToken)
//    {
//        throw new NotImplementedException();
//    }

//    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task RemoveByIdAsync(TId id, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public void Attach(TEntity entity)
//    {
//        throw new NotImplementedException();
//    }

//    public void Detach(TEntity entity)
//    {
//        throw new NotImplementedException();
//    }
//}

