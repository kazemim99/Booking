using MediatR;
using Booksy.Core.Domain.Domain.Abstractions;

namespace Booksy.Core.Domain.Application.Abstractions;

/// <summary>
/// Interface for domain event handlers
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type</typeparam>
public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{ }