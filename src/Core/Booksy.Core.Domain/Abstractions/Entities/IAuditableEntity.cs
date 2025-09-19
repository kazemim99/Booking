// ========================================
// Booksy.Core.Domain/Abstractions/Entities/IAuditableEntity.cs
// ========================================
namespace Booksy.Core.Domain.Abstractions.Entities
{
    /// <summary>
    /// Represents an entity that tracks creation and modification metadata
    /// </summary>

    public interface IAuditableEntity
    {
        /// <summary>
        /// Gets the date and time when the entity was created
        /// </summary>
        DateTime CreatedAt { get; protected set; }

        /// <summary>
        /// Gets the identifier of the user who created the entity
        /// </summary>
        string? CreatedBy { get; protected set; }

        /// <summary>
        /// Gets the date and time when the entity was last modified
        /// </summary>
        DateTime? LastModifiedAt { get; protected set; }

        /// <summary>
        /// Gets the identifier of the user who last modified the entity
        /// </summary>
        string? LastModifiedBy { get; protected set; }

        void SetCreatedAt(DateTime utcNow);
        void SetCreatedBy(string v);
        void SetLastModifiedAt(DateTime utcNow);
        void SetLastModifiedBy(string v);
    }

    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}