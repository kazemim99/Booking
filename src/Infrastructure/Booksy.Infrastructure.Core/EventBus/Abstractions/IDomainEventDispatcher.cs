// ========================================
// EventBus/Abstractions/IDomainEventDispatcher.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Abstractions.Events;

namespace Booksy.Infrastructure.Core.EventBus.Abstractions;

/// <summary>
/// Dispatches domain events from aggregates
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all domain events from the given aggregates
    /// </summary>
    Task DispatchEventsAsync(
        IEnumerable<IAggregateRoot> aggregates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a single domain event
    /// </summary>
    Task DispatchEventAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default);
}
