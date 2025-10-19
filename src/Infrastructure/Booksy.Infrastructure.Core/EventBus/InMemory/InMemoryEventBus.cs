//// ========================================
//// EventBus/InMemory/InMemoryEventBus.cs
//// ========================================
//using Booksy.Core.Domain.Abstractions.Events;
//using Booksy.Infrastructure.Core.EventBus.Abstractions;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;

//namespace Booksy.Infrastructure.Core.EventBus.InMemory;

///// <summary>
///// In-memory event bus implementation without MediatR
///// Directly resolves and invokes handlers from DI container
///// Suitable for development and testing
///// </summary>
//public sealed class InMemoryEventBus : IEventBus
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly ILogger<InMemoryEventBus> _logger;
//    private readonly Dictionary<string, List<Type>> _handlers = new();

//    public InMemoryEventBus(
//        IServiceProvider serviceProvider,
//        ILogger<InMemoryEventBus> logger)
//    {
//        _serviceProvider = serviceProvider;
//        _logger = logger;
//    }

//    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
//    {
//        _logger.LogDebug("Publishing event {EventType} with Id {EventId}",
//            domainEvent.GetType().Name,
//            domainEvent.EventId);

//        try
//        {
//            // Resolve and invoke handlers directly from DI container
//            await TriggerHandlersAsync(domainEvent, cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error publishing event {EventType}", domainEvent.GetType().Name);
//            throw;
//        }
//    }

//    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
//    {
//        foreach (var domainEvent in domainEvents)
//        {
//            await PublishAsync(domainEvent, cancellationToken);
//        }
//    }

//    public void Subscribe<TEvent, THandler>()
//        where TEvent : IDomainEvent
//        where THandler : IDomainEventHandler<TEvent>
//    {
//        var eventName = typeof(TEvent).Name;
//        var handlerType = typeof(THandler);

//        if (!_handlers.ContainsKey(eventName))
//        {
//            _handlers[eventName] = new List<Type>();
//        }

//        if (!_handlers[eventName].Contains(handlerType))
//        {
//            _handlers[eventName].Add(handlerType);
//            _logger.LogInformation("Subscribed {Handler} to {Event}", handlerType.Name, eventName);
//        }
//    }

//    public void Unsubscribe<TEvent, THandler>()
//        where TEvent : IDomainEvent
//        where THandler : IDomainEventHandler<TEvent>
//    {
//        var eventName = typeof(TEvent).Name;
//        var handlerType = typeof(THandler);

//        if (_handlers.ContainsKey(eventName))
//        {
//            _handlers[eventName].Remove(handlerType);
//            _logger.LogInformation("Unsubscribed {Handler} from {Event}", handlerType.Name, eventName);
//        }
//    }

//    private async Task TriggerHandlersAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
//    {
//        var eventType = domainEvent.GetType();
//        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

//        using var scope = _serviceProvider.CreateScope();

//        // Get all registered handlers for this event type
//        var handlers = scope.ServiceProvider.GetServices(handlerType);

//        foreach (var handler in handlers)
//        {
//            // Call HandleAsync method
//            var handleMethod = handlerType.GetMethod("HandleAsync");
//            if (handleMethod != null)
//            {
//                var task = (Task?)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
//                if (task != null)
//                {
//                    await task;

//                    _logger.LogDebug(
//                        "Handler {HandlerType} executed successfully for event {EventType}",
//                        handler.GetType().Name,
//                        eventType.Name);
//                }
//            }
//        }

//        // Also trigger any manually subscribed handlers
//        var eventName = eventType.Name;
//        if (_handlers.TryGetValue(eventName, out var handlerTypes))
//        {
//            foreach (var manualHandlerType in handlerTypes)
//            {
//                var handler = scope.ServiceProvider.GetService(manualHandlerType);
//                if (handler != null)
//                {
//                    var handleMethod = manualHandlerType.GetMethod("HandleAsync");
//                    if (handleMethod != null)
//                    {
//                        var task = (Task?)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
//                        if (task != null)
//                        {
//                            await task;
//                        }
//                    }
//                }
//            }
//        }
//    }
//}