// 📁 Booksy.Core.Application/Abstractions/Persistence/IWriteRepository.cs - FIXED
using Booksy.Core.Domain.Abstractions.Entities;

namespace Booksy.Core.Application.Abstractions.Persistence
{
    /// <summary>
    /// Pure write repository interface - NO read methods except GetByIdAsync for updates
    /// </summary>
    public interface IWriteRepository<TEntity, TId>
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        // ✅ ONLY write methods
        Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken);

        Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task RemoveByIdAsync(TId id, CancellationToken cancellationToken = default);
    }
}