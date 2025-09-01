
// ========================================
// Persistence/EventStore/StoredEvent.cs
// ========================================
namespace Booksy.Infrastructure.Core.Persistence.EventStore;

/// <summary>
/// Represents a stored event with metadata
/// </summary>
public sealed record StoredEvent(
    Guid EventId,
    string AggregateId,
    string AggregateType,
    string EventType,
    string EventData,
    long Version,
    DateTime Timestamp,
    string? UserId);
