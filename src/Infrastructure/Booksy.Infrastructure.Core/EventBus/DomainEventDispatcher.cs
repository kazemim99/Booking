// ========================================
// EventBus/DomainEventDispatcher.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Core.EventBus;

/// <summary>
/// Dispatches domain events from aggregates
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IDomainEventDispatcher _eventBus;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IDomainEventDispatcher eventBus,
        ILogger<DomainEventDispatcher> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task DispatchEventsAsync(
        IEnumerable<IAggregateRoot> aggregates,
        CancellationToken cancellationToken = default)
    {
        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        _logger.LogDebug("Dispatching {Count} domain events", domainEvents.Count);

        foreach (var domainEvent in domainEvents)
        {
            await DispatchEventAsync(domainEvent, cancellationToken);
        }

        // Clear events after dispatching
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }

    public async Task DispatchEventAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Dispatching event {EventType} with Id {EventId}",
            domainEvent.GetType().Name,
            domainEvent.EventId);

        try
        {
            await _eventBus.DispatchEventAsync(domainEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch event {EventType}", domainEvent.GetType().Name);
            throw;
        }
    }
}
