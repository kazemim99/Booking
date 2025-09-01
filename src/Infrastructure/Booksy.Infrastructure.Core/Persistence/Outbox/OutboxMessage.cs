// ========================================
// Persistence/Outbox/OutboxMessage.cs
// ========================================
namespace Booksy.Infrastructure.Core.Persistence.Outbox;

/// <summary>
/// Represents an outbox message for reliable event publishing
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
