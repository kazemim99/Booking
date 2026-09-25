// ========================================
// EventBus/Abstractions/IIntegrationEventPublisher.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Events;

namespace AsanRezerve.Infrastructure.Core.EventBus.Abstractions;

/// <summary>
/// Publishes integration events to external systems
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes an integration event
    /// </summary>
    Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple integration events
    /// </summary>
    Task PublishBatchAsync(
        IEnumerable<IIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken = default);
}
