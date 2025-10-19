// ========================================
// EventBus/SimpleDomainEventDispatcher.cs
// Simple, explicit domain event dispatcher without MediatR
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Core.EventBus;

/// <summary>
/// Simple domain event dispatcher that resolves handlers from DI container
/// No MediatR, no magic - just explicit, testable event handling
/// </summary>
public sealed class SimpleDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SimpleDomainEventDispatcher> _logger;

    public SimpleDomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<SimpleDomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchEventsAsync(
        IEnumerable<IAggregateRoot> aggregates,
        CancellationToken cancellationToken = default)
    {
        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        if (!domainEvents.Any())
            return;

        _logger.LogDebug("Dispatching {Count} domain events", domainEvents.Count);

        // Create a scope for handler resolution
        using var scope = _serviceProvider.CreateScope();

        foreach (var domainEvent in domainEvents)
        {
            await DispatchEventAsync(domainEvent, scope.ServiceProvider, cancellationToken);
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
        using var scope = _serviceProvider.CreateScope();
        await DispatchEventAsync(domainEvent, scope.ServiceProvider, cancellationToken);
    }

    private async Task DispatchEventAsync(
        IDomainEvent domainEvent,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        _logger.LogDebug(
            "Dispatching event {EventType} with Id {EventId}",
            eventType.Name,
            domainEvent.EventId);

        try
        {
            // Get all handlers for this event type
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                // Call HandleAsync method (IDomainEventHandler interface)
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    var task = (Task?)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
                    if (task != null)
                    {
                        await task;

                        _logger.LogDebug(
                            "Handler {HandlerType} executed successfully for event {EventType}",
                            handler.GetType().Name,
                            eventType.Name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to dispatch event {EventType} with Id {EventId}",
                eventType.Name,
                domainEvent.EventId);
            throw;
        }
    }
}
