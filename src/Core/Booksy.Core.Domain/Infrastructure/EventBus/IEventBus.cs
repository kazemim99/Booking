using Booksy.Core.Domain.Domain.Abstractions;

namespace Booksy.Core.Domain.Infrastructure.EventBus;

/// <summary>
/// Interface for event bus implementations
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a domain event
    /// </summary>
    /// <param name="domainEvent">The domain event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events
    /// </summary>
    /// <param name="domainEvents">The domain events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}