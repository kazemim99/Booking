// 📁 AsanRezerve.UserManagement.Application/Abstractions/Queries/IUserQueryRepository.cs
using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.Core.Domain.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AsanRezerve.Core.Application.Abstractions.Persistence
{
    public interface IQueryRepositoryBase<TEntity, TId> where TEntity : class, IEntity<TId>
   where TId : notnull
    {
        IQueryable<TEntity> ApplySpecification(IQueryable<TEntity> query, ISpecification<TEntity> specification);
        IQueryable<TEntity> ApplySpecificationForCount(IQueryable<TEntity> query, ISpecification<TEntity> specification);
        Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TResult>> GetListAsync<TResult>(ISpecification<TEntity> specification, Expression<Func<TEntity, TResult>> selector, CancellationToken cancellationToken = default);
        Task<PagedResult<TResult>> GetPagedAsync<TResult>(IAdvancedSpecification<TEntity> specification, Expression<Func<TEntity, TResult>> selector, CancellationToken cancellationToken = default);
        Task<TResult?> GetSingleAsync<TResult>(ISpecification<TEntity> specification, Expression<Func<TEntity, TResult>> selector, CancellationToken cancellationToken = default);
    }
}