// 📁 AsanRezerve.Core.Infrastructure/Persistence/Base/EfReadRepositoryBase.cs - NEW
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.Core.Domain.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;

namespace AsanRezerve.Infrastructure.Core.Persistence.Base;

/// <summary>
/// Base read-only repository with specification support
/// </summary>
public abstract class EfReadRepositoryBase<TEntity, TId, TContext> : EfRepositoryBase<TEntity, TId, TContext>,IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
    where TContext : DbContext
{
    protected readonly TContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected EfReadRepositoryBase(TContext context):base(context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<TEntity>();
    }

  
}

