// ========================================
// Booksy.Core.Application/Abstractions/Persistence/IWriteRepository.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;

namespace Booksy.Core.Application.Abstractions.Persistence
{
    /// <summary>
    /// Defines a write repository for modifying entities
    /// </summary>
    /// <typeparam name="TEntity">The type of entity</typeparam>
    /// <typeparam name="TId">The type of the entity's identifier</typeparam>
    public interface IWriteRepository<TEntity, TId>
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        /// <summary>
        /// Adds an entity to the repository
        /// </summary>
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple entities to the repository
        /// </summary>
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an entity in the repository
        /// </summary>
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates multiple entities in the repository
        /// </summary>
        Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an entity from the repository
        /// </summary>
        Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an entity by its identifier
        /// </summary>
        Task RemoveByIdAsync(TId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes multiple entities from the repository
        /// </summary>
        Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attaches an entity to the context
        /// </summary>
        void Attach(TEntity entity);

        /// <summary>
        /// Detaches an entity from the context
        /// </summary>
        void Detach(TEntity entity);

        /// <summary>
        /// Gets the unit of work associated with this repository
        /// </summary>
        //IUnitOfWork UnitOfWork { get; }
    }

    /// <summary>
    /// Defines a write repository with default Guid identifier
    /// </summary>
    public interface IWriteRepository<TEntity> : IWriteRepository<TEntity, Guid>
        where TEntity : class, IEntity<Guid>
    {
    }
}