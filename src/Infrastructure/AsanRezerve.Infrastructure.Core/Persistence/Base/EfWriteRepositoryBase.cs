// 📁 AsanRezerve.Core.Infrastructure/Persistence/Base/EfReadEfWriteRepositoryBase.cs - NEW
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Domain.Abstractions.Entities;
using AsanRezerve.Core.Domain.Base;
using Microsoft.EntityFrameworkCore;

namespace AsanRezerve.Infrastructure.Core.Persistence.Base;

/// <summary>
/// Base write-only repository for command operations
/// </summary>
public abstract class EfWriteRepositoryBase<TEntity, TId, TContext> : EfRepositoryBase<TEntity, TId, TContext>, IWriteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
    where TContext : DbContext
{
    protected readonly TContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected EfWriteRepositoryBase(TContext context):base(context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<TEntity>();
    }

  
}