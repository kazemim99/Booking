// Booksy.SharedKernel.Infrastructure.EventStore/EventSourcedRepository.cs
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.Domain.Abstractions;
using Booksy.Core.Domain.Domain.Exceptions;
using Booksy.Core.Domain.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.Core.Domain.EventStore;

/// <summary>
/// Base repository for event-sourced aggregates
/// </summary>
/// <typeparam name="TAggregate">The aggregate type</typeparam>
/// <typeparam name="TId">The identifier type</typeparam>
public abstract class EventSourcedRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot
    where TId : notnull
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<EventSourcedRepository<TAggregate, TId>> _logger;
    protected abstract string AggregateTypeName { get; }

    protected EventSourcedRepository(
        IEventStore eventStore,
        ILogger<EventSourcedRepository<TAggregate, TId>> logger)
    {
        _eventStore = eventStore;
        _logger = logger;
    }

    /// <summary>
    /// Gets an aggregate by reconstructing it from events
    /// </summary>
    public virtual async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var aggregateId = GetAggregateId(id);

        try
        {
            var events = await _eventStore.GetEventsAsync(
                aggregateId,
                AggregateTypeName,
                fromVersion: 0,
                cancellationToken);

            if (!events.Any())
            {
                _logger.LogDebug("No events found for aggregate {AggregateType} with ID {AggregateId}",
                    AggregateTypeName, aggregateId);
                return null;
            }

            // Reconstruct aggregate from events
            var aggregate = await ReconstructFromEventsAsync(id, events, cancellationToken);

            _logger.LogDebug("Reconstructed aggregate {AggregateType} with ID {AggregateId} from {EventCount} events",
                AggregateTypeName, aggregateId, events.Count());

            return aggregate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get aggregate {AggregateType} with ID {AggregateId}",
                AggregateTypeName, aggregateId);
            throw;
        }
    }

    /// <summary>
    /// Saves an aggregate by appending new events
    /// </summary>
    public virtual async Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var aggregateId = GetAggregateId(GetAggregateIdFromAggregate(aggregate));
        var uncommittedEvents = aggregate.DomainEvents.ToList();

        if (!uncommittedEvents.Any())
        {
            _logger.LogDebug("No uncommitted events for aggregate {AggregateType} with ID {AggregateId}",
                AggregateTypeName, aggregateId);
            return;
        }

        try
        {
            // Get expected version (optimistic concurrency control)
            var expectedVersion = await GetExpectedVersionAsync(aggregate, cancellationToken);

            // Append events to event store
            var newVersion = await _eventStore.AppendEventsAsync(
                aggregateId,
                AggregateTypeName,
                uncommittedEvents,
                expectedVersion,
                cancellationToken);

            // Clear events after successful save
            aggregate.ClearDomainEvents();

            _logger.LogInformation("Saved {EventCount} events for aggregate {AggregateType} with ID {AggregateId}, new version: {Version}",
                uncommittedEvents.Count, AggregateTypeName, aggregateId, newVersion);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning("Concurrency conflict saving aggregate {AggregateType} with ID {AggregateId}: {Error}",
                AggregateTypeName, aggregateId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save aggregate {AggregateType} with ID {AggregateId}",
                AggregateTypeName, aggregateId);
            throw;
        }
    }

    /// <summary>
    /// Removes an aggregate (typically by adding a deletion event)
    /// </summary>
    public virtual async Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        // For event sourcing, we typically don't delete but add a "deleted" event
        // This preserves the full audit trail

        // Implementation depends on your domain requirements
        // You might want to add a generic "AggregateDeleted" event

        await SaveAsync(aggregate, cancellationToken);
    }

    /// <summary>
    /// Abstract method to get aggregate ID as string
    /// </summary>
    protected abstract string GetAggregateId(TId id);

    /// <summary>
    /// Abstract method to extract ID from aggregate
    /// </summary>
    protected abstract TId GetAggregateIdFromAggregate(TAggregate aggregate);

    /// <summary>
    /// Abstract method to reconstruct aggregate from events
    /// </summary>
    protected abstract Task<TAggregate> ReconstructFromEventsAsync(
        TId id,
        IEnumerable<StoredEvent> events,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the expected version for optimistic concurrency control
    /// Override if your aggregate tracks version differently
    /// </summary>
    protected virtual async Task<long> GetExpectedVersionAsync(TAggregate aggregate, CancellationToken cancellationToken)
    {
        // Default implementation: get current version from event store
        var aggregateId = GetAggregateId(GetAggregateIdFromAggregate(aggregate));
        return await _eventStore.GetAggregateVersionAsync(aggregateId, AggregateTypeName, cancellationToken);
    }
}

/// <summary>
/// Helper extensions for event sourcing
/// </summary>
public static class EventSourcingExtensions
{
    /// <summary>
    /// Deserializes event data to domain event
    /// </summary>
    public static IDomainEvent DeserializeEvent(this StoredEvent storedEvent)
    {
        var eventType = Type.GetType(storedEvent.EventType);
        if (eventType == null)
            throw new InvalidOperationException($"Could not find type {storedEvent.EventType}");

        var eventData = JsonSerializer.Deserialize(storedEvent.EventData, eventType);
        if (eventData is not IDomainEvent domainEvent)
            throw new InvalidOperationException($"Event data is not a domain event: {storedEvent.EventType}");

        return domainEvent;
    }

    /// <summary>
    /// Serializes domain event to stored event format
    /// </summary>
    public static (string EventType, string EventData) SerializeEvent(this IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType().AssemblyQualifiedName!;
        var eventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        return (eventType, eventData);
    }
}