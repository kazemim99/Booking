// ========================================
// Booksy.Core.Application/Abstractions/Events/IEventHandler.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;

namespace Booksy.Core.Application.Abstractions.Events
{
    /// <summary>
    /// Defines a handler for domain events
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event</typeparam>
    public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        /// <summary>
        /// Handles the domain event
        /// </summary>
        /// <param name="domainEvent">The domain event to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
