// ========================================
// EventBus/Abstractions/IEventHandler.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Events;

namespace AsanRezerve.Infrastructure.Core.EventBus.Abstractions;

/// <summary>
/// Handles domain events
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
