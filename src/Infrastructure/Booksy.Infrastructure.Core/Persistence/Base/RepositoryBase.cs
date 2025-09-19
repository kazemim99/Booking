// 📁 Booksy.Core.Infrastructure/Persistence/Base/EfReadRepositoryBase.cs - NEW
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;

namespace Booksy.Infrastructure.Core.Persistence.Base;

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

