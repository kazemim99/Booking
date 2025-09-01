// Booksy.SharedKernel.Infrastructure.EventStore/IEventStore.cs
namespace Booksy.Core.Domain.EventStore;

/// <summary>
/// Represents a stored event with metadata
/// </summary>
public sealed record StoredEvent(
    string EventId,
    string AggregateId,
    string AggregateType,
    string EventType,
    string EventData,
    string? Metadata,
    long Version,
    DateTime Timestamp);
