// ========================================
// Persistence/EventStore/IEventStore.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Base;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Core.Persistence.EventStore;

/// <summary>
/// Base repository for event-sourced aggregates
/// </summary>
public abstract class EventSourcedRepository<TAggregate, TId>
    where TAggregate :  AggregateRoot<TId>
    where TId : notnull
{
    protected readonly IEventStore<TId> EventStore;
    protected readonly ILogger Logger;
    protected abstract string AggregateTypeName { get; }

    protected EventSourcedRepository(IEventStore<TId> eventStore, ILogger logger)
    {
        EventStore = eventStore;
        Logger = logger;
    }

    public virtual async  Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var aggregateId = GetAggregateId(id);

        // Try to get snapshot first
        var snapshot = await EventStore.GetLatestSnapshotAsync(aggregateId, AggregateTypeName, cancellationToken);

        var fromVersion = snapshot?.Version ?? 0;
        var events = await EventStore.GetEventsAsync(aggregateId, AggregateTypeName, fromVersion, cancellationToken);

        if (!events.Any() && snapshot == null)
        {
            return null;
        }

        // Reconstruct aggregate from snapshot and/or events
        var aggregate = await ReconstructFromEventsAsync(id, events, cancellationToken);

        return aggregate;
    }

    public virtual async Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var aggregateId = GetAggregateIdFromAggregate(aggregate);
        var expectedVersion = await EventStore.GetAggregateVersionAsync(aggregateId, AggregateTypeName, cancellationToken);

        if (aggregate.DomainEvents.Any())
        {
            await EventStore.AppendEventsAsync(
                aggregateId,
                AggregateTypeName,
                aggregate.DomainEvents,
                expectedVersion,
                cancellationToken);

            // Create snapshot every N events (e.g., 10)
            var newVersion = expectedVersion + aggregate.DomainEvents.Count();
            if (newVersion % 10 == 0)
            {
                await EventStore.SaveSnapshotAsync(
                    aggregateId,
                    AggregateTypeName,
                    aggregate,
                    newVersion,
                    cancellationToken);
            }

            aggregate.ClearDomainEvents();
        }
    }

    protected abstract TId GetAggregateId(TId id);
    protected abstract TId GetAggregateIdFromAggregate(TAggregate aggregate);
    protected abstract Task<TAggregate> ReconstructFromEventsAsync(
        TId id,
        IEnumerable<StoredEvent> events,
        CancellationToken cancellationToken);
}
