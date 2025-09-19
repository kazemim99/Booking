// ========================================
// EventBus/Abstractions/IEventBus.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Core.Domain.Abstractions.Events;

namespace Booksy.Infrastructure.Core.EventBus.Abstractions;

/// <summary>
/// Defines the contract for publishing domain events
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a single domain event
    /// </summary>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events
    /// </summary>
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to a specific event type
    /// </summary>
    void Subscribe<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : IDomainEventHandler<TEvent>;

    /// <summary>
    /// Unsubscribes from a specific event type
    /// </summary>
    void Unsubscribe<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : IDomainEventHandler<TEvent>;
}
