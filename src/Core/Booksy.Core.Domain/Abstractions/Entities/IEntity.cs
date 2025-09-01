// ========================================
// Booksy.Core.Domain/Abstractions/Entities/IEntity.cs
// ========================================
namespace Booksy.Core.Domain.Abstractions.Entities
{
    /// <summary>
    /// Represents a domain entity with a unique identifier
    /// </summary>
    /// <typeparam name="TId">The type of the entity's identifier</typeparam>
    public interface IEntity<TId> where TId : notnull
    {
        /// <summary>
        /// Gets the unique identifier of the entity
        /// </summary>
        TId Id { get; }

        /// <summary>
        /// Checks if this entity is transient (not persisted)
        /// </summary>
        bool IsTransient();
    }

    /// <summary>
    /// Represents a domain entity with a default identifier type
    /// </summary>
    public interface IEntity : IEntity<Guid>
    {
    }
}