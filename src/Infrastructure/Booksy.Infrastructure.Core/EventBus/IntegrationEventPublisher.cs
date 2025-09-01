// ========================================
// EventBus/DomainEventDispatcher.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Infrastructure.Core.Persistence.Outbox;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.Infrastructure.Core.EventBus;

// ========================================
// EventBus/IntegrationEventPublisher.cs
// ========================================

/// <summary>
/// Publishes integration events using the outbox pattern
/// </summary>
public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IOutboxProcessor _outboxProcessor;
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(
        IOutboxProcessor outboxProcessor,
        ILogger<IntegrationEventPublisher> logger)
    {
        _outboxProcessor = outboxProcessor;
        _logger = logger;
    }

    public async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        //var message = new OutboxMessage
        //{
        //    Id = integrationEvent.EventId,
        //    Type = integrationEvent.EventType,
        //    AggregateId = integrationEvent.AggregateId,
        //    Payload = JsonSerializer.Serialize(integrationEvent),
        //    OccurredOn = integrationEvent.OccurredOn,
        //    ProcessedOn = null
        //};

        //await _outboxProcessor.AddMessageAsync(message, cancellationToken);

        //_logger.LogInformation("Integration event {EventType} added to outbox", integrationEvent.EventType);
    }

    public async Task PublishBatchAsync(
        IEnumerable<IIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken = default)
    {
        //var messages = integrationEvents.Select(e => new OutboxMessage
        //{
        //    Id = e.Id,
        //    Type = e.EventType,
        //    AggregateId = e.AggregateId,
        //    Payload = JsonSerializer.Serialize(e),
        //    OccurredOn = e.OccurredOn,
        //    ProcessedOn = null
        //}).ToList();

        //await _outboxProcessor.AddMessagesAsync(messages, cancellationToken);

        //_logger.LogInformation("Added {Count} integration events to outbox", messages.Count);
    }
}