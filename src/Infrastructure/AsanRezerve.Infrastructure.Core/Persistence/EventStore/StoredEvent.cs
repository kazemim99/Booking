// AsanRezerve.SharedKernel.Infrastructure.EventStore/IEventStore.cs
namespace AsanRezerve.Infrastructure.Core.Persistence.EventStore;

/// <summary>
/// Represents a stored event with metadata
/// </summary>
public sealed record StoredEvent(
    string EventId,
    string AggregateId,
    string AggregateType,
    string EventType,
    string EventData,
    long Version,
    DateTime Timestamp);