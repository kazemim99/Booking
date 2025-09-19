// ========================================
// Booksy.Core.Domain/Base/Entity.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;

namespace Booksy.Core.Domain.Base
{
    /// <summary>
    /// Base class for domain entities
    /// </summary>
    /// <typeparam name="TId">The type of the entity's identifier</typeparam>
    public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>, IAuditableEntity,ISoftDelete
        where TId : notnull
    {
        private int? _requestedHashCode;
        public bool IsDeleted { get; set; }
        public TId Id { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        protected Entity() { }

        protected Entity(TId id)
        {
            Id = id;
        }

        public bool IsTransient()
        {
            return Id == null || Id.Equals(default(TId));
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Entity<TId> entity)
                return false;

            if (ReferenceEquals(this, entity))
                return true;

            if (GetType() != entity.GetType())
                return false;

            if (entity.IsTransient() || IsTransient())
                return false;

            return Id.Equals(entity.Id);
        }

        public bool Equals(Entity<TId>? other)
        {
            return Equals((object?)other);
        }

        public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            if (!IsTransient())
            {
                _requestedHashCode ??= Id.GetHashCode() ^ 31;
                return _requestedHashCode.Value;
            }

            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"{GetType().Name} [Id={Id}]";
        }

        public void SetCreatedAt(DateTime utcNow)
        {
            this.CreatedAt = utcNow;
        }

        public void SetCreatedBy(string v)
        {
            CreatedBy = v;
        }

        public void SetLastModifiedAt(DateTime utcNow)
        {
            LastModifiedAt = utcNow;
        }

        public void SetLastModifiedBy(string v)
        {
            LastModifiedBy = v;
        }
    }



    /// <summary>
    /// Base class for domain entities with default Guid identifier
    /// </summary>
    public abstract class Entity : Entity<Guid>, IEntity
    {
        protected Entity() : base() { }
        protected Entity(Guid id) : base(id) { }
    }
}


