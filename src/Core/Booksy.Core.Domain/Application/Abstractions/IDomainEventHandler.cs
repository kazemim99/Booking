using Booksy.Core.Domain.Domain.Abstractions;

namespace Booksy.Core.Domain.Application.Abstractions;

/// <summary>
/// Interface for domain event handlers
/// Pure domain layer abstraction - NO MediatR dependency!
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type</typeparam>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event
    /// Called by SimpleDomainEventDispatcher after aggregate changes are persisted
    /// </summary>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}