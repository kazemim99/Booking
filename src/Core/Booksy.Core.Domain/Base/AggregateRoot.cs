// ========================================
// Booksy.Core.Domain/Base/Entity.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Core.Domain.Abstractions.Rules;
using Booksy.Core.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Booksy.Core.Domain.Base
{
    /// <summary>
    /// Base class for aggregate roots in Domain-Driven Design
    /// </summary>
    /// <typeparam name="TId">The type of the aggregate's identifier</typeparam>
    public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot<TId>
        where TId : notnull
    {
        private readonly List<IDomainEvent> _domainEvents = new();
        private int _version;


        public int Version
        {
            get => _version;
            protected set => _version = value;
        }

        [NotMapped]
        public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected AggregateRoot() : base() { }

        protected AggregateRoot(TId id) : base(id) { }

        /// <summary>
        /// Raises a domain event and optionally applies it to update state
        /// </summary>
        /// <param name="domainEvent">The domain event to raise</param>
        /// <param name="isNew">Whether this is a new event (true) or replaying an existing event (false)</param>
        protected void RaiseDomainEvent(IDomainEvent domainEvent, bool isNew = true)
        {
            if (isNew)
            {
                _domainEvents.Add(domainEvent);
            }

            ApplyEvent(domainEvent);
            _version++;
        }

        /// <summary>
        /// Applies an event to update the aggregate's state
        /// Override this method in derived classes to handle specific events
        /// </summary>
        public virtual void ApplyEvent(IDomainEvent domainEvent)
        {
            // Default implementation does nothing
            // Derived classes should override to handle specific events
        }

        /// <summary>
        /// Clears all domain events from this aggregate
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        /// <summary>
        /// Marks the aggregate as having domain events for persistence
        /// </summary>
        public bool HasDomainEvents()
        {
            return _domainEvents.Count > 0;
        }

        /// <summary>
        /// Checks a business rule and throws an exception if it's broken
        /// </summary>
        /// <param name="rule">The business rule to check</param>
        public static void CheckRule(IBusinessRule rule)
        {
            if (rule.IsBroken())
            {
                throw new BusinessRuleViolationException(rule);
            }
        }

        /// <summary>
        /// Ensures the aggregate is not in a transient state
        /// </summary>
        protected void EnsureNotTransient()
        {
            if (IsTransient())
            {
                throw new InvalidAggregateStateException(
                    GetType(),
                    "Operation",
                    "Transient");
            }
        }



        /// <summary>
        /// Ensures the aggregate is in a valid state for a specific operation
        /// </summary>
        protected void EnsureValidState(Func<bool> predicate, string operation, string currentState)
        {
            if (!predicate())
            {
                throw new InvalidAggregateStateException(
                    GetType(),
                    operation,
                    currentState);
            }
        }
    }
}


