// ========================================
// Booksy.Core.Domain/Abstractions/Events/IDomainEvent.cs
// ========================================
namespace Booksy.Core.Domain.Abstractions.Events
{
    /// <summary>
    /// Represents a domain event that captures something that happened in the domain
    /// </summary>
    public interface IDomainEvent
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
        /// Gets the name of the aggregate that raised this event
        /// </summary>
        string? AggregateType { get; }

        /// <summary>
        /// Gets the identifier of the aggregate that raised this event
        /// </summary>
        string? AggregateId { get; }
    }
}