// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ProviderDraftCreatedEventHandler.cs
// ========================================
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Application.IntegrationEvents;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
    /// <summary>
    /// Handles ProviderDraftCreatedEvent domain event and publishes integration event
    /// Uses SimpleDomainEventDispatcher for clean, explicit event handling
    /// </summary>
    public sealed class ProviderDraftCreatedEventHandler : IDomainEventHandler<ProviderDraftCreatedEvent>
    {
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly ILogger<ProviderDraftCreatedEventHandler> _logger;

        public ProviderDraftCreatedEventHandler(
            IIntegrationEventPublisher eventPublisher,
            ILogger<ProviderDraftCreatedEventHandler> logger)
        {
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        /// <summary>
        /// Handles the ProviderDraftCreatedEvent
        /// Called by SimpleDomainEventDispatcher after provider draft creation is persisted
        /// Publishes integration event to update User profile in UserManagement context
        /// </summary>
        public async Task HandleAsync(ProviderDraftCreatedEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "📝 ProviderDraftCreatedEvent received: Provider {ProviderId} draft created for owner {OwnerId} ({FirstName} {LastName})",
                domainEvent.ProviderId,
                domainEvent.OwnerId,
                domainEvent.OwnerFirstName,
                domainEvent.OwnerLastName);

            // Publish integration event for UserManagement context to update User.Profile
            var integrationEvent = new ProviderDraftCreatedIntegrationEvent(
                domainEvent.ProviderId.Value,
                domainEvent.OwnerId.Value,
                domainEvent.OwnerFirstName,
                domainEvent.OwnerLastName,
                domainEvent.BusinessName,
                domainEvent.CreatedAt);

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "✅ Published ProviderDraftCreatedIntegrationEvent to message bus. UserManagement will update User profile for: {OwnerId}",
                domainEvent.OwnerId);
        }
    }
}
