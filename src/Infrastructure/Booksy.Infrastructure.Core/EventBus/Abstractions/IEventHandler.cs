// ========================================
// EventBus/Abstractions/IEventHandler.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Events;

namespace Booksy.Infrastructure.Core.EventBus.Abstractions;

/// <summary>
/// Handles domain events
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
