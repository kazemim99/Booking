// ========================================
// Persistence/EventStore/IEventStore.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.Infrastructure.Core.Persistence.EventStore;

public abstract class EventStoreBase<TAggregateId> : IEventStore<TAggregateId> where TAggregateId : notnull
{
    protected readonly ILogger<EventStoreBase<TAggregateId>> Logger;
    private readonly JsonSerializerOptions _jsonOptions;

    protected EventStoreBase(ILogger<EventStoreBase<TAggregateId>> logger)
    {
        Logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public abstract Task<long> AppendEventsAsync(
        TAggregateId aggregateId,
        string aggregateType,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    public abstract Task<IEnumerable<StoredEvent>> GetEventsAsync(
        TAggregateId aggregateId,
        string aggregateType,
        long fromVersion = 0,
        CancellationToken cancellationToken = default);

    public abstract Task<IEnumerable<StoredEvent>> GetAllEventsFromAsync(
        DateTime fromTimestamp,
        CancellationToken cancellationToken = default);

    public abstract Task<long> GetAggregateVersionAsync(
        TAggregateId aggregateId,
        string aggregateType,
        CancellationToken cancellationToken = default);

    public abstract Task SaveSnapshotAsync(
        TAggregateId aggregateId,
        string aggregateType,
        object snapshot,
        long version,
        CancellationToken cancellationToken = default);

    public abstract Task<Snapshot?> GetLatestSnapshotAsync(
        TAggregateId aggregateId,
        string aggregateType,
        CancellationToken cancellationToken = default);

    protected string SerializeEvent(IDomainEvent domainEvent)
    {
        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonOptions);
    }

    protected IDomainEvent? DeserializeEvent(string eventData, string eventType)
    {
        var type = Type.GetType(eventType);
        if (type == null)
        {
            Logger.LogWarning("Could not find type {EventType}", eventType);
            return null;
        }

        return JsonSerializer.Deserialize(eventData, type, _jsonOptions) as IDomainEvent;
    }

    protected string SerializeSnapshot(object snapshot)
    {
        return JsonSerializer.Serialize(snapshot, snapshot.GetType(), _jsonOptions);
    }

    protected object? DeserializeSnapshot(string snapshotData, string aggregateType)
    {
        var type = Type.GetType($"{aggregateType}Snapshot");
        if (type == null)
        {
            Logger.LogWarning("Could not find snapshot type for {AggregateType}", aggregateType);
            return null;
        }

        return JsonSerializer.Deserialize(snapshotData, type, _jsonOptions);
    }
}
