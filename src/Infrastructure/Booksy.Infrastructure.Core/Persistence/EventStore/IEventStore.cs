// ========================================
// Persistence/EventStore/IEventStore.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;

namespace Booksy.Infrastructure.Core.Persistence.EventStore;

/// <summary>
/// Interface for event store operations
/// </summary>
public interface IEventStore<TId> where TId : notnull

{
    /// <summary>
    /// Appends events to an aggregate stream
    /// </summary>
    Task<long> AppendEventsAsync(
        TId aggregateId,
        string aggregateType,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events for an aggregate
    /// </summary>
    Task<IEnumerable<StoredEvent>> GetEventsAsync(
        TId aggregateId,
        string aggregateType,
        long fromVersion = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events from a specific timestamp
    /// </summary>
    Task<IEnumerable<StoredEvent>> GetAllEventsFromAsync(
        DateTime fromTimestamp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of an aggregate
    /// </summary>
    Task<long> GetAggregateVersionAsync(
        TId aggregateId,
        string aggregateType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a snapshot of an aggregate
    /// </summary>
    Task SaveSnapshotAsync(
        TId aggregateId,
        string aggregateType,
        object snapshot,
        long version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest snapshot for an aggregate
    /// </summary>
    Task<EventStoreSnapshot?> GetLatestSnapshotAsync(
        TId aggregateId,
        string aggregateType,
        CancellationToken cancellationToken = default);
}
