// ========================================
// Booksy.Core.Application/Abstractions/Persistence/IReadRepository.cs
// ========================================
using System.Linq.Expressions;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Abstractions.Entities;

namespace Booksy.Core.Application.Abstractions.Persistence
{
    /// <summary>
    /// Defines a read-only repository for querying entities
    /// </summary>
    /// <typeparam name="TEntity">The type of entity</typeparam>
    /// <typeparam name="TId">The type of the entity's identifier</typeparam>
    public interface IReadRepository<TEntity, TId>
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        /// <summary>
        /// Gets an entity by its identifier
        /// </summary>
        Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities
        /// </summary>
        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds entities matching a predicate
        /// </summary>
        Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds a single entity matching a predicate
        /// </summary>
        Task<TEntity?> FindSingleAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if any entity exists matching a predicate
        /// </summary>
        Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts entities matching a predicate
        /// </summary>
        Task<int> CountAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable for complex queries
        /// </summary>
        IQueryable<TEntity> GetQueryable();

        ///// <summary>
        ///// Gets paged results
        ///// </summary>
        //Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
        //    int pageNumber,
        //    int pageSize,
        //    Expression<Func<TEntity, bool>>? predicate = null,
        //    Expression<Func<TEntity, object>>? orderBy = null,
        //    bool descending = false,
        //    CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets entities with included navigation properties
        /// </summary>
        Task<IReadOnlyList<TEntity>> GetWithIncludesAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            params Expression<Func<TEntity, object>>[] includes);
    }

    /// <summary>
    /// Defines a read-only repository with default Guid identifier
    /// </summary>
    public interface IReadRepository<TEntity> : IReadRepository<TEntity, Guid>
        where TEntity : class, IEntity<Guid>
    {
    }
}
