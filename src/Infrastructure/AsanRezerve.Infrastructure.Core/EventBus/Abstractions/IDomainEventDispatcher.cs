// ========================================
// EventBus/Abstractions/IDomainEventDispatcher.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Entities;
using AsanRezerve.Core.Domain.Abstractions.Events;

namespace AsanRezerve.Infrastructure.Core.EventBus.Abstractions;

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
