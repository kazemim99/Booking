// ========================================
// EventBus/RabbitMQ/Consumers/DomainEventConsumer.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Core.EventBus.RabbitMQ.Consumers;

/// <summary>
/// Generic MassTransit consumer that wraps domain event handlers
/// This consumer receives events from RabbitMQ and dispatches them to registered handlers
/// </summary>
/// <typeparam name="TEvent">The type of domain event to consume</typeparam>
public class DomainEventConsumer<TEvent> : IConsumer<TEvent>
    where TEvent : class, IDomainEvent
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventConsumer<TEvent>> _logger;

    public DomainEventConsumer(
        IServiceProvider serviceProvider,
        ILogger<DomainEventConsumer<TEvent>> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<TEvent> context)
    {
        var domainEvent = context.Message;
        var eventName = typeof(TEvent).Name;

        _logger.LogInformation(
            "Consuming event {EventName} with ID {EventId} from RabbitMQ",
            eventName,
            domainEvent.EventId);

        try
        {
            // Create a new scope for handler resolution
            using var scope = _serviceProvider.CreateScope();

            // Resolve all handlers for this event type
            var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler<TEvent>>();

            if (!handlers.Any())
            {
                _logger.LogWarning(
                    "No handlers registered for event {EventName} with ID {EventId}",
                    eventName,
                    domainEvent.EventId);
                return;
            }

            // Execute all handlers
            var handleTasks = handlers.Select(handler =>
                ExecuteHandlerAsync(handler, domainEvent, eventName, context.CancellationToken));

            await Task.WhenAll(handleTasks);

            _logger.LogInformation(
                "Successfully consumed event {EventName} with ID {EventId} by {HandlerCount} handler(s)",
                eventName,
                domainEvent.EventId,
                handlers.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error consuming event {EventName} with ID {EventId}",
                eventName,
                domainEvent.EventId);
            throw;
        }
    }

    private async Task ExecuteHandlerAsync(
        IDomainEventHandler<TEvent> handler,
        TEvent domainEvent,
        string eventName,
        CancellationToken cancellationToken)
    {
        var handlerName = handler.GetType().Name;

        try
        {
            _logger.LogDebug(
                "Executing handler {HandlerName} for event {EventName}",
                handlerName,
                eventName);

            await handler.HandleAsync(domainEvent, cancellationToken);

            _logger.LogDebug(
                "Handler {HandlerName} completed successfully for event {EventName}",
                handlerName,
                eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Handler {HandlerName} failed for event {EventName} with ID {EventId}",
                handlerName,
                eventName,
                domainEvent.EventId);
            throw;
        }
    }
}
