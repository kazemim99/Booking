// ========================================
// EventBus/RabbitMQ/RabbitMqEventBus.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booksy.Infrastructure.Core.EventBus.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of event bus using MassTransit for production use
/// </summary>
public sealed class RabbitMqEventBus : IDisposable
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly RabbitMqSettings _settings;
    private bool _disposed;

    public RabbitMqEventBus(
        IPublishEndpoint publishEndpoint,
        ILogger<RabbitMqEventBus> logger,
        IOptions<RabbitMqSettings> settings)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Publishes a single domain event to RabbitMQ
    /// </summary>
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        var eventName = domainEvent.GetType().Name;

        try
        {
            _logger.LogInformation(
                "Publishing event {EventName} with ID {EventId} to RabbitMQ",
                eventName,
                domainEvent.EventId);

            await _publishEndpoint.Publish(
                domainEvent,
                context =>
                {
                    context.MessageId = domainEvent.EventId;
                    context.CorrelationId = domainEvent.EventId;

                    // Add custom headers for metadata
                    context.Headers.Set("EventType", eventName);
                    context.Headers.Set("EventVersion", domainEvent.EventVersion);
                    context.Headers.Set("OccurredAt", domainEvent.OccurredAt.ToString("o"));

                    if (!string.IsNullOrEmpty(domainEvent.AggregateType))
                    {
                        context.Headers.Set("AggregateType", domainEvent.AggregateType);
                    }

                    if (!string.IsNullOrEmpty(domainEvent.AggregateId))
                    {
                        context.Headers.Set("AggregateId", domainEvent.AggregateId);
                    }
                },
                cancellationToken);

            _logger.LogInformation(
                "Successfully published event {EventName} with ID {EventId}",
                eventName,
                domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventName} with ID {EventId} to RabbitMQ",
                eventName,
                domainEvent.EventId);
            throw;
        }
    }

    /// <summary>
    /// Publishes multiple domain events in a batch
    /// </summary>
    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        if (domainEvents == null)
        {
            throw new ArgumentNullException(nameof(domainEvents));
        }

        var eventsList = domainEvents.ToList();
        if (!eventsList.Any())
        {
            _logger.LogWarning("Attempted to publish empty batch of events");
            return;
        }

        _logger.LogInformation("Publishing batch of {EventCount} events to RabbitMQ", eventsList.Count);

        var publishTasks = eventsList.Select(domainEvent => PublishAsync(domainEvent, cancellationToken));

        try
        {
            await Task.WhenAll(publishTasks);

            _logger.LogInformation(
                "Successfully published batch of {EventCount} events",
                eventsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish batch of {EventCount} events. Some events may have been published successfully.",
                eventsList.Count);
            throw;
        }
    }

    /// <summary>
    /// Publishes multiple domain events in a batch (alternative method name)
    /// </summary>
    public Task PublishBatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        return PublishAsync(domainEvents, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing RabbitMqEventBus");
        _disposed = true;
    }
}

/// <summary>
/// Configuration settings for RabbitMQ connection
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// RabbitMQ host name or IP address
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port (default: 5672)
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// RabbitMQ virtual host
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// RabbitMQ username
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ password
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Number of retry attempts for failed messages
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Enable or disable dead letter queue
    /// </summary>
    public bool UseDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Prefix for queue names
    /// </summary>
    public string QueuePrefix { get; set; } = "booksy";

    /// <summary>
    /// Number of concurrent consumers per queue
    /// </summary>
    public int ConcurrentMessageLimit { get; set; } = 16;

    /// <summary>
    /// Prefetch count for consumers
    /// </summary>
    public int PrefetchCount { get; set; } = 16;

    /// <summary>
    /// Use SSL/TLS for connection
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Heartbeat interval in seconds
    /// </summary>
    public ushort HeartbeatInterval { get; set; } = 60;
}
