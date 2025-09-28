// ========================================
// Persistence/Outbox/OutboxMessage.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace Booksy.Infrastructure.Core.Persistence.Outbox;

/// <summary>
/// Implementation of outbox processor for reliable event publishing
/// </summary>
public class OutboxProcessor<TDbContext> : IOutboxProcessor<TDbContext>
    where TDbContext : DbContext
{
    private readonly TDbContext _context;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OutboxProcessor<TDbContext>> _logger;
    private readonly int _maxRetries;

    public OutboxProcessor(
        TDbContext context,
        IEventBus eventBus,
        ILogger<OutboxProcessor<TDbContext>> logger)
    {
        _context = context;
        _eventBus = eventBus;
        _logger = logger;
        _maxRetries = 3;
    }

    public async Task AddMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _context.Set<OutboxMessage>().AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMessagesAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        await _context.Set<OutboxMessage>().AddRangeAsync(messages, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        var pendingMessages = await _context.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn == null && m.RetryCount < _maxRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(100) // Process in batches
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                // Deserialize and publish the event
                var eventType = Type.GetType(message.Type);
                if (eventType != null)
                {
                    var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType);
                    if (domainEvent is IDomainEvent evt)
                    {
                        await _eventBus.PublishAsync(evt, cancellationToken);
                        await MarkAsProcessedAsync(message.Id, cancellationToken);

                        _logger.LogInformation("Outbox message {MessageId} processed successfully", message.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                await MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.Set<OutboxMessage>().FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.ProcessedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.Set<OutboxMessage>().FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.RetryCount++;
            message.Error = error;

            if (message.RetryCount >= _maxRetries)
            {
                _logger.LogError("Outbox message {MessageId} exceeded max retries", messageId);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}