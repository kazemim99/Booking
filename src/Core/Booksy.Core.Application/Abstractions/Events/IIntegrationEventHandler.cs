// ========================================
// Booksy.Core.Application/Abstractions/Events/IEventHandler.cs
// ========================================
namespace Booksy.Core.Application.Abstractions.Events
{

    /// <summary>
    /// Defines a handler for integration events
    /// </summary>
    /// <typeparam name="TEvent">The type of integration event</typeparam>
    public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
    {
        /// <summary>
        /// Handles the integration event
        /// </summary>
        /// <param name="integrationEvent">The integration event to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
    }
}
