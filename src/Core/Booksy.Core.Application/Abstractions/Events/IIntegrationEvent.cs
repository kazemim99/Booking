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
    public abstract record IntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
        public int EventVersion { get; protected init; } = 1;
        public string? SourceContext { get; protected init; }
        public string? CorrelationId { get; init; }
        public string? CausationId { get; init; }
        public string? UserId { get; init; }

        protected IntegrationEvent() { }

        protected IntegrationEvent(string sourceContext)
        {
            SourceContext = sourceContext;
        }

        public virtual Dictionary<string, object> GetMetadata() =>
            new()
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