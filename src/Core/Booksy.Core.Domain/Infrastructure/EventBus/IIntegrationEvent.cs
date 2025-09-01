//namespace Booksy.SharedKernel.Domain.Abstractions
//{
//    /// <summary>
//    /// Marker interface for integration events that cross bounded context boundaries
//    /// </summary>
//    public interface IIntegrationEvent
//    {
//        /// <summary>
//        /// Unique identifier for the event
//        /// </summary>
//        Guid EventId { get; }

//        /// <summary>
//        /// Timestamp when the event occurred
//        /// </summary>
//        DateTime OccurredOn { get; }

//        /// <summary>
//        /// Type of the event for routing/filtering
//        /// </summary>
//        string EventType { get; }

//        /// <summary>
//        /// Version of the event schema
//        /// </summary>
//        int EventVersion { get; }
//    }
//}