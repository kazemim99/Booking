// ========================================
// EventBus/DomainEventDispatcher.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Infrastructure.Core.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
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
    private readonly IOutboxProcessor<DbContext> _outboxProcessor;
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(
        IOutboxProcessor<DbContext> outboxProcessor,
        ILogger<IntegrationEventPublisher> logger)
    {
        _outboxProcessor = outboxProcessor;
        _logger = logger;
    }

    public async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = integrationEvent.EventId,
            Type = integrationEvent.GetType().ToString(),
            Payload = JsonSerializer.Serialize(integrationEvent),
            OccurredOn = integrationEvent.OccurredAt,
            ProcessedOn = null
        };

        await _outboxProcessor.AddMessageAsync(message, cancellationToken);

        _logger.LogInformation("Integration event {EventType} added to outbox", integrationEvent.GetType().ToString());
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