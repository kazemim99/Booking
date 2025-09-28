

// ========================================
// Persistence/Outbox/IOutboxProcessor.cs
// ========================================
using Microsoft.EntityFrameworkCore;

namespace Booksy.Infrastructure.Core.Persistence.Outbox;

/// <summary>
/// Processes outbox messages for reliable event publishing
/// </summary>
public interface IOutboxProcessor<TDbContext> where TDbContext : DbContext
{
    /// <summary>
    /// Adds a message to the outbox
    /// </summary>
    Task AddMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple messages to the outbox
    /// </summary>
    Task AddMessagesAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes pending outbox messages
    /// </summary>
    Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}



