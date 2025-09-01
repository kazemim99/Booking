using Booksy.Core.Domain.Abstractions;

namespace Booksy.Core.Domain.Infrastructure.EventBus;

/// <summary>
/// Helper to dispatch domain events from aggregates
/// </summary>
public class DomainEventDispatcher: IDomainEventDispatcher
{
    private readonly IEventBus _eventBus;

    public DomainEventDispatcher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>
    /// Dispatches all domain events from an aggregate and clears them
    /// </summary>
    public async Task DispatchEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default)
    {
        var events = aggregate.DomainEvents.ToList();

        if (events.Any())
        {
            await _eventBus.PublishAsync(events, cancellationToken);
            aggregate.ClearDomainEvents();
        }
    }

    /// <summary>
    /// Dispatches events from multiple aggregates
    /// </summary>
    public async Task DispatchEventsAsync(IEnumerable<IAggregateRoot> aggregates, CancellationToken cancellationToken = default)
    {
        foreach (var aggregate in aggregates)
        {
            await DispatchEventsAsync(aggregate, cancellationToken);
        }
    }
}