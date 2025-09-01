// ========================================
// Persistence/EventStore/IEventStore.cs
// ========================================
namespace Booksy.Infrastructure.Core.Persistence.EventStore;


/// <summary>
/// Represents an aggregate snapshot
/// </summary>
public sealed record Snapshot(
    string AggregateId,
    string AggregateType,
    string SnapshotData,
    long Version,
    DateTime Timestamp);


