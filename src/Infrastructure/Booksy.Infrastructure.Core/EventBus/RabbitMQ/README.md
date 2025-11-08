# RabbitMQ Event Bus Implementation

This document describes the RabbitMQ Event Bus implementation for the Booksy project using MassTransit.

## Overview

The RabbitMQ Event Bus provides a robust, production-ready message broker implementation for publishing and consuming domain events across bounded contexts in the Booksy microservices architecture.

## Architecture

### Components

1. **RabbitMqEventBus** - Main event bus implementation for publishing events
2. **DomainEventConsumer<TEvent>** - Generic consumer for handling domain events
3. **MassTransitExtensions** - DI configuration and setup
4. **RabbitMqSettings** - Configuration settings

### Key Features

- ✅ **MassTransit Integration** - Uses MassTransit for reliable message delivery
- ✅ **Retry Policies** - Automatic retry with exponential backoff
- ✅ **Dead Letter Queue** - Failed messages moved to DLQ for analysis
- ✅ **Batch Publishing** - Efficient batch event publishing
- ✅ **Structured Logging** - Comprehensive logging with correlation IDs
- ✅ **Error Handling** - Graceful error handling and recovery
- ✅ **Message Metadata** - Event versioning, timestamps, and correlation tracking

## Installation

### 1. Add NuGet Packages

The following packages are already included in `Booksy.Infrastructure.Core.csproj`:

```xml
<PackageReference Include="MassTransit" Version="8.2.2" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.2.2" />
```

### 2. Configure appsettings.json

Add the RabbitMQ configuration section to your `appsettings.json`:

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "booksy_admin",
    "Password": "Booksy@2024!",
    "RetryCount": 5,
    "RetryDelayMs": 1000,
    "UseDeadLetterQueue": true,
    "QueuePrefix": "booksy.servicecatalog",
    "ConcurrentMessageLimit": 16,
    "PrefetchCount": 16,
    "UseSsl": false,
    "HeartbeatInterval": 60
  }
}
```

### 3. Register in DI Container

Add the following to your `Startup.cs` or `Program.cs`:

```csharp
using Booksy.Infrastructure.Core.EventBus.RabbitMQ;
using Booksy.ServiceCatalog.Domain.Events;

// In ConfigureServices method:
services.AddRabbitMqEventBus(Configuration, configurator =>
{
    // Register consumers for specific domain events
    configurator.AddDomainEventConsumer<BookingRequestedEvent>();
    configurator.AddDomainEventConsumer<BookingConfirmedEvent>();
    configurator.AddDomainEventConsumer<BookingCancelledEvent>();
    // Add more event consumers as needed
});
```

### 4. Register Event Handlers

Register your event handlers in the DI container:

```csharp
services.AddScoped<IDomainEventHandler<BookingRequestedEvent>, BookingRequestedEventHandler>();
services.AddScoped<IDomainEventHandler<BookingConfirmedEvent>, BookingConfirmedEventHandler>();
```

## Usage

### Publishing Events

#### Single Event

```csharp
public class BookingService
{
    private readonly RabbitMqEventBus _eventBus;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        RabbitMqEventBus eventBus,
        ILogger<BookingService> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task CreateBookingAsync(CreateBookingDto dto, CancellationToken cancellationToken)
    {
        // Create booking logic...
        var booking = new Booking(/* ... */);

        // Create domain event
        var domainEvent = new BookingRequestedEvent(
            booking.Id,
            dto.CustomerId,
            dto.ProviderId,
            dto.ServiceId,
            dto.StaffId,
            dto.StartTime,
            dto.EndTime,
            dto.TotalPrice,
            DateTime.UtcNow
        );

        // Publish event to RabbitMQ
        await _eventBus.PublishAsync(domainEvent, cancellationToken);

        _logger.LogInformation("Published BookingRequestedEvent for booking {BookingId}", booking.Id);
    }
}
```

#### Batch Events

```csharp
public async Task ProcessMultipleBookingsAsync(
    List<Booking> bookings,
    CancellationToken cancellationToken)
{
    var events = bookings.Select(b => new BookingRequestedEvent(
        b.Id,
        b.CustomerId,
        b.ProviderId,
        b.ServiceId,
        b.StaffId,
        b.StartTime,
        b.EndTime,
        b.TotalPrice,
        DateTime.UtcNow
    )).ToList();

    // Publish all events in a batch
    await _eventBus.PublishBatchAsync(events, cancellationToken);

    _logger.LogInformation("Published {Count} booking events", events.Count);
}
```

### Consuming Events

#### Create Event Handler

```csharp
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers;

public class BookingRequestedEventHandler : IDomainEventHandler<BookingRequestedEvent>
{
    private readonly ILogger<BookingRequestedEventHandler> _logger;
    private readonly INotificationService _notificationService;

    public BookingRequestedEventHandler(
        ILogger<BookingRequestedEventHandler> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task HandleAsync(
        BookingRequestedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling BookingRequestedEvent for booking {BookingId}",
            domainEvent.BookingId);

        try
        {
            // Send notification to customer
            await _notificationService.SendBookingConfirmationAsync(
                domainEvent.CustomerId,
                domainEvent.BookingId,
                cancellationToken);

            // Send notification to provider
            await _notificationService.SendNewBookingNotificationAsync(
                domainEvent.ProviderId,
                domainEvent.BookingId,
                cancellationToken);

            _logger.LogInformation(
                "Successfully handled BookingRequestedEvent for booking {BookingId}",
                domainEvent.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling BookingRequestedEvent for booking {BookingId}",
                domainEvent.BookingId);
            throw; // Will trigger retry policy
        }
    }
}
```

## Configuration Options

### RabbitMqSettings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Host` | string | "localhost" | RabbitMQ server hostname or IP |
| `Port` | int | 5672 | RabbitMQ server port |
| `VirtualHost` | string | "/" | RabbitMQ virtual host |
| `Username` | string | "guest" | RabbitMQ username |
| `Password` | string | "guest" | RabbitMQ password |
| `RetryCount` | int | 5 | Number of retry attempts |
| `RetryDelayMs` | int | 1000 | Initial retry delay in milliseconds |
| `UseDeadLetterQueue` | bool | true | Enable dead letter queue |
| `QueuePrefix` | string | "booksy" | Prefix for queue names |
| `ConcurrentMessageLimit` | int | 16 | Max concurrent messages |
| `PrefetchCount` | int | 16 | Consumer prefetch count |
| `UseSsl` | bool | false | Enable SSL/TLS |
| `HeartbeatInterval` | ushort | 60 | Heartbeat interval in seconds |

## Message Flow

```
┌─────────────────┐         ┌──────────────┐         ┌─────────────────┐
│                 │         │              │         │                 │
│  Service Layer  │────────▶│  Event Bus   │────────▶│   RabbitMQ      │
│                 │ Publish │              │         │                 │
└─────────────────┘         └──────────────┘         └─────────────────┘
                                                              │
                                                              │
                                                              ▼
                            ┌─────────────────────────────────────────┐
                            │         MassTransit Consumer             │
                            └─────────────────────────────────────────┘
                                              │
                                              │
                                              ▼
                            ┌─────────────────────────────────────────┐
                            │      DomainEventConsumer<TEvent>         │
                            └─────────────────────────────────────────┘
                                              │
                                              │
                                              ▼
                            ┌─────────────────────────────────────────┐
                            │    IDomainEventHandler<TEvent>          │
                            │         (Your Handler)                   │
                            └─────────────────────────────────────────┘
```

## Error Handling & Retry Policies

### Retry Strategy

The implementation uses an incremental retry strategy:

1. **First Retry**: After 1 second (RetryDelayMs)
2. **Second Retry**: After 2 seconds (RetryDelayMs * 2)
3. **Third Retry**: After 4 seconds (RetryDelayMs * 4)
4. **Fourth Retry**: After 8 seconds (RetryDelayMs * 8)
5. **Fifth Retry**: After 16 seconds (RetryDelayMs * 16)

### Dead Letter Queue

If all retries fail, the message is moved to the dead letter queue for manual inspection and recovery.

### Delayed Redelivery

Before entering the retry policy, messages go through delayed redelivery:
- 5 seconds
- 15 seconds
- 30 seconds

## Monitoring & Observability

### Logging

All events are logged with structured logging:

```csharp
_logger.LogInformation(
    "Publishing event {EventName} with ID {EventId} to RabbitMQ",
    eventName,
    domainEvent.EventId);
```

### Message Metadata

Each message includes:
- `MessageId` - Unique message identifier
- `CorrelationId` - For tracing across services
- `EventType` - Event type name
- `EventVersion` - Schema version
- `OccurredAt` - Timestamp
- `AggregateType` - Aggregate type (if applicable)
- `AggregateId` - Aggregate ID (if applicable)

## Best Practices

### 1. Event Design

✅ **DO**: Keep events immutable (use records)
```csharp
public sealed record BookingRequestedEvent(...) : DomainEvent;
```

✅ **DO**: Include all necessary data in the event
```csharp
public sealed record BookingRequestedEvent(
    BookingId BookingId,
    UserId CustomerId,
    ProviderId ProviderId,
    ServiceId ServiceId,
    Guid StaffId,
    DateTime StartTime,
    DateTime EndTime,
    Price TotalPrice,
    DateTime RequestedAt) : DomainEvent;
```

❌ **DON'T**: Include large binary data in events

### 2. Handler Design

✅ **DO**: Make handlers idempotent
```csharp
public async Task HandleAsync(BookingRequestedEvent domainEvent, CancellationToken ct)
{
    // Check if already processed
    if (await _repository.IsProcessedAsync(domainEvent.EventId))
    {
        _logger.LogInformation("Event {EventId} already processed", domainEvent.EventId);
        return;
    }

    // Process event...

    // Mark as processed
    await _repository.MarkAsProcessedAsync(domainEvent.EventId);
}
```

✅ **DO**: Use scoped services in handlers
```csharp
services.AddScoped<IDomainEventHandler<BookingRequestedEvent>, BookingRequestedEventHandler>();
```

❌ **DON'T**: Perform long-running operations without timeout

### 3. Error Handling

✅ **DO**: Log errors with context
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error handling event {EventId}", domainEvent.EventId);
    throw; // Let retry policy handle it
}
```

✅ **DO**: Use cancellation tokens
```csharp
await someService.DoWorkAsync(cancellationToken);
```

## Testing

### Unit Testing Event Publishing

```csharp
[Fact]
public async Task PublishAsync_ShouldPublishEvent()
{
    // Arrange
    var publishEndpoint = Substitute.For<IPublishEndpoint>();
    var logger = Substitute.For<ILogger<RabbitMqEventBus>>();
    var settings = Options.Create(new RabbitMqSettings());
    var eventBus = new RabbitMqEventBus(publishEndpoint, logger, settings);

    var domainEvent = new BookingRequestedEvent(/* ... */);

    // Act
    await eventBus.PublishAsync(domainEvent);

    // Assert
    await publishEndpoint.Received(1).Publish(
        Arg.Is<BookingRequestedEvent>(e => e.BookingId == domainEvent.BookingId),
        Arg.Any<CancellationToken>());
}
```

### Integration Testing with RabbitMQ

```csharp
[Fact]
public async Task EndToEnd_PublishAndConsume()
{
    // Use testcontainers to spin up RabbitMQ
    var rabbitContainer = new ContainerBuilder()
        .WithImage("rabbitmq:3-management")
        .WithPortBinding(5672, true)
        .Build();

    await rabbitContainer.StartAsync();

    // Configure services with test container
    // Publish event
    // Wait for handler to process
    // Assert handler was called
}
```

## Troubleshooting

### Connection Issues

**Problem**: Cannot connect to RabbitMQ

**Solution**:
1. Verify RabbitMQ is running: `docker ps | grep rabbitmq`
2. Check connection settings in appsettings.json
3. Verify network connectivity: `telnet localhost 5672`
4. Check RabbitMQ logs: `docker logs <container-id>`

### Messages Not Being Consumed

**Problem**: Events are published but not consumed

**Solution**:
1. Verify consumer is registered in DI
2. Check RabbitMQ management UI for queue bindings
3. Verify handler is registered: `services.AddScoped<IDomainEventHandler<TEvent>, THandler>()`
4. Check application logs for consumer errors

### Dead Letter Queue Filling Up

**Problem**: Too many messages in DLQ

**Solution**:
1. Check handler implementation for bugs
2. Review error logs to identify root cause
3. Fix the issue and replay messages from DLQ
4. Consider adjusting retry policy settings

## Example Files

### Publisher Example
See: `Consumers/Examples/BookingRequestedEventHandler.cs`

### Consumer Registration
See: `MassTransitExtensions.cs`

## Additional Resources

- [MassTransit Documentation](https://masstransit.io/)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Domain Events Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)

## License

Internal use only - Booksy Platform
