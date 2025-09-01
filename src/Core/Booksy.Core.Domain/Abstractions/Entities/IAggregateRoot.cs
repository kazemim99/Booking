// ========================================
// Booksy.Core.Domain/Abstractions/Entities/IAggregateRoot.cs
// ========================================
namespace Booksy.Core.Domain.Abstractions.Entities
{
    using Booksy.Core.Domain.Abstractions.Events;

    /// <summary>
    /// Represents an aggregate root in Domain-Driven Design
    /// </summary>
    /// <typeparam name="TId">The type of the aggregate's identifier</typeparam>
    public interface IAggregateRoot<TId> : IEntity<TId> where TId : notnull
    {
        /// <summary>
        /// Gets the version of the aggregate for optimistic concurrency control
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Gets all domain events that have been raised by this aggregate
        /// </summary>
        IReadOnlyList<IDomainEvent> DomainEvents { get; }

        /// <summary>
        /// Clears all domain events from this aggregate
        /// </summary>
        void ClearDomainEvents();

        /// <summary>
        /// Applies an event to update the aggregate's state (for event sourcing)
        /// </summary>
        void ApplyEvent(IDomainEvent domainEvent);
    }

    /// <summary>
    /// Represents an aggregate root with a default identifier type
    /// </summary>
    public interface IAggregateRoot : IAggregateRoot<Guid>
    {
    }
}