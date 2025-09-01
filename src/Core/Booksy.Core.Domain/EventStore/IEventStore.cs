// Booksy.SharedKernel.Infrastructure.EventStore/IEventStore.cs

// Booksy.SharedKernel.Infrastructure.EventStore/IEventStore.cs

// Booksy.SharedKernel.Infrastructure.EventStore/IEventStore.cs

// Booksy.SharedKernel.Infrastructure.EventStore/IEventStore.cs
using Booksy.Core.Domain.Domain.Abstractions;

namespace Booksy.Core.Domain.EventStore;

/// <summary>
/// Core interface for event store operations
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends events to an aggregate stream
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="aggregateType">The aggregate type name</param>
    /// <param name="events">Events to append</param>
    /// <param name="expectedVersion">Expected version for concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new version after append</returns>
    Task<long> AppendEventsAsync(
        string aggregateId,
        string aggregateType,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events for an aggregate from a specific version
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="aggregateType">The aggregate type name</param>
    /// <param name="fromVersion">Version to start from (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of events</returns>
    Task<IEnumerable<StoredEvent>> GetEventsAsync(
        string aggregateId,
        string aggregateType,
        long fromVersion = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events from a specific timestamp (for projections)
    /// </summary>
    /// <param name="fromTimestamp">Timestamp to start from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of all events</returns>
    Task<IEnumerable<StoredEvent>> GetAllEventsFromAsync(
        DateTime fromTimestamp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of an aggregate
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="aggregateType">The aggregate type name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current version, -1 if not found</returns>
    Task<long> GetAggregateVersionAsync(
        string aggregateId,
        string aggregateType,
        CancellationToken cancellationToken = default);
}
