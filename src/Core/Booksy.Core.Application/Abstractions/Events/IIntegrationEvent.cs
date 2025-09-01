// ========================================
// Booksy.Core.Application/Abstractions/Events/IIntegrationEvent.cs
// ========================================
namespace Booksy.Core.Application.Abstractions.Events
{
    /// <summary>
    /// Represents an integration event that can be published across bounded contexts
    /// </summary>
    public interface IIntegrationEvent
    {
        /// <summary>
        /// Gets the unique identifier of the event
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// Gets the date and time when the event occurred
        /// </summary>
        DateTime OccurredAt { get; }

        /// <summary>
        /// Gets the version of the event schema
        /// </summary>
        int EventVersion { get; }

        /// <summary>
        /// Gets the name of the bounded context that raised this event
        /// </summary>
        string? SourceContext { get; }

        /// <summary>
        /// Gets correlation ID for distributed tracing
        /// </summary>
        string? CorrelationId { get; }

        /// <summary>
        /// Gets causation ID linking this event to its cause
        /// </summary>
        string? CausationId { get; }

        /// <summary>
        /// Gets the user who triggered the event
        /// </summary>
        string? UserId { get; }
    }

    /// <summary>
    /// Base implementation of integration event
    /// </summary>
    public abstract class IntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; }
        public DateTime OccurredAt { get; }
        public int EventVersion { get; protected set; }
        public string? SourceContext { get; protected set; }
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public string? UserId { get; set; }

        protected IntegrationEvent()
        {
            EventId = Guid.NewGuid();
            OccurredAt = DateTime.UtcNow;
            EventVersion = 1;
        }

        protected IntegrationEvent(string sourceContext) : this()
        {
            SourceContext = sourceContext;
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
                ["SourceContext"] = SourceContext ?? string.Empty,
                ["CorrelationId"] = CorrelationId ?? string.Empty,
                ["CausationId"] = CausationId ?? string.Empty,
                ["UserId"] = UserId ?? string.Empty
            };
        }
    }
}