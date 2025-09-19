// ========================================
// Persistence/EventStore/IEventStore.cs
// ========================================
namespace Booksy.Infrastructure.Core.Persistence.EventStore;


/// <summary>
/// Represents an aggregate snapshot
/// </summary>
public class EventStoreSnapshot
{
    public string AggregateId { get; set; }
    public string AggregateType { get; set; }
    public int Version { get; set; }
    public string Data { get; set; }
    public DateTime CreatedAt { get; set; }

    public EventStoreSnapshot(
        string aggregateId,
        string aggregateType,
        int version,
        string data,
        DateTime createdAt)
    {
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        Version = version;
        Data = data;
        CreatedAt = createdAt;
    }
}



