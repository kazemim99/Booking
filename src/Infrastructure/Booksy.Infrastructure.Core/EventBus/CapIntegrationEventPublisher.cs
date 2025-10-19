// ========================================
// EventBus/CapIntegrationEventPublisher.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Core.EventBus;

/// <summary>
/// CAP-based integration event publisher with outbox pattern support
/// </summary>
public sealed class CapIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<CapIntegrationEventPublisher> _logger;

    public CapIntegrationEventPublisher(
        ICapPublisher capPublisher,
        ILogger<CapIntegrationEventPublisher> logger)
    {
        _capPublisher = capPublisher ?? throw new ArgumentNullException(nameof(capPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes integration event using CAP's outbox pattern
    /// CAP automatically stores the event in the outbox table within the same transaction
    /// </summary>
    public async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (integrationEvent == null)
            throw new ArgumentNullException(nameof(integrationEvent));

        var eventType = integrationEvent.GetType();
        var topicName = GetTopicName(eventType);

        _logger.LogInformation(
            "Publishing integration event {EventType} to topic {Topic} with EventId {EventId}",
            eventType.Name,
            topicName,
            integrationEvent.EventId);

        try
        {
            // CAP will automatically use the outbox pattern if within a transaction
            // The event is first saved to cap.published table, then sent to RabbitMQ
            await _capPublisher.PublishAsync(
                topicName,
                integrationEvent,
                headers: BuildHeaders(integrationEvent),
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Successfully published integration event {EventType} with EventId {EventId}",
                eventType.Name,
                integrationEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish integration event {EventType} with EventId {EventId}",
                eventType.Name,
                integrationEvent.EventId);
            throw;
        }
    }

    /// <summary>
    /// Publishes multiple integration events in batch
    /// </summary>
    public async Task PublishBatchAsync(
        IEnumerable<IIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken = default)
    {
        if (integrationEvents == null)
            throw new ArgumentNullException(nameof(integrationEvents));

        var events = integrationEvents.ToList();
        if (!events.Any())
            return;

        _logger.LogInformation(
            "Publishing batch of {Count} integration events",
            events.Count);

        foreach (var integrationEvent in events)
        {
            await PublishAsync(integrationEvent, cancellationToken);
        }

        _logger.LogInformation(
            "Successfully published batch of {Count} integration events",
            events.Count);
    }

    /// <summary>
    /// Gets the topic name for the event type
    /// Convention: namespace.eventname (e.g., booksy.servicecatalog.providerregistered)
    /// </summary>
    private static string GetTopicName(Type eventType)
    {
        // Extract namespace parts
        var namespaceParts = eventType.Namespace?.Split('.') ?? Array.Empty<string>();

        // Get bounded context name (e.g., ServiceCatalog from Booksy.ServiceCatalog.Application.IntegrationEvents)
        var contextName = namespaceParts.Length > 1 ? namespaceParts[1].ToLowerInvariant() : "booksy";

        // Remove "IntegrationEvent" suffix from event name
        var eventName = eventType.Name.Replace("IntegrationEvent", "").ToLowerInvariant();

        return $"booksy.{contextName}.{eventName}";
    }

    /// <summary>
    /// Builds message headers for distributed tracing and correlation
    /// </summary>
    private static Dictionary<string, string?> BuildHeaders(IIntegrationEvent integrationEvent)
    {
        return new Dictionary<string, string?>
        {
            ["EventId"] = integrationEvent.EventId.ToString(),
            ["EventType"] = integrationEvent.GetType().Name,
            ["EventVersion"] = integrationEvent.EventVersion.ToString(),
            ["SourceContext"] = integrationEvent.SourceContext,
            ["CorrelationId"] = integrationEvent.CorrelationId,
            ["CausationId"] = integrationEvent.CausationId,
            ["UserId"] = integrationEvent.UserId,
            ["OccurredAt"] = integrationEvent.OccurredAt.ToString("O")
        };
    }
}
