
using Booksy.Core.Domain.Abstractions.Events;

namespace Booksy.Core.Domain.Base
{
    /// <summary>
    /// Base class for domain events
    /// </summary>
    public abstract class DomainEvent : IDomainEvent
    {
        public Guid EventId { get; }
        public DateTime OccurredAt { get; }
        public int EventVersion { get; protected set; }
        public string? AggregateType { get; protected set; }
        public string? AggregateId { get; protected set; }

        protected DomainEvent()
        {
            EventId = Guid.NewGuid();
            OccurredAt = DateTime.UtcNow;
            EventVersion = 1;
        }

        protected DomainEvent(string aggregateType, string aggregateId)
            : this()
        {
            AggregateType = aggregateType;
            AggregateId = aggregateId;
        }

        /// <summary>
        /// Creates metadata dictionary for event storage
        /// </summary>
        public virtual Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["EventId"] = EventId,
                ["EventType"] = GetType().Name,
                ["OccurredAt"] = OccurredAt,
                ["EventVersion"] = EventVersion,
                ["AggregateType"] = AggregateType ?? string.Empty,
                ["AggregateId"] = AggregateId ?? string.Empty
            };
        }

        /// <summary>
        /// Gets the event type name for storage
        /// </summary>
        public virtual string GetEventTypeName()
        {
            return GetType().Name;
        }

        public override string ToString()
        {
            return $"{GetEventTypeName()} [Id={EventId}, AggregateId={AggregateId}, OccurredAt={OccurredAt:yyyy-MM-dd HH:mm:ss}]";
        }
    }
}