// 📁 Booksy.Core.Application/Abstractions/Persistence/IReadRepository.cs
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Abstractions.Entities;
using System.Linq.Expressions;

namespace Booksy.Core.Application.Abstractions.Persistence
{
    /// <summary>
    /// Pure read repository interface - NO write methods
    /// </summary>
    public interface IReadRepository<TEntity, TId>
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        // ✅ ONLY read methods
        Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<TEntity?> GetSingleAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TEntity>> GetAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
        Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

        Task<PagedResult<TEntity>> GetPaginatedAsync(
                    ISpecification<TEntity> specification,
                    PaginationRequest pagination,
                    CancellationToken cancellationToken = default);

        Task<PagedResult<TProjection>> GetPaginatedAsync<TProjection>(
            ISpecification<TEntity> specification,
            PaginationRequest pagination,
            Expression<Func<TEntity, TProjection>> projection,
            CancellationToken cancellationToken = default);

    }
}