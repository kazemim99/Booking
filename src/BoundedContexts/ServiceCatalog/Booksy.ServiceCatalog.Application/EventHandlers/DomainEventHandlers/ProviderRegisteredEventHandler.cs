// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ProviderRegisteredEventHandler.cs
// ========================================
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Application.IntegrationEvents;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
    /// <summary>
    /// Handles ProviderRegisteredEvent domain event and publishes integration event
    /// Uses SimpleDomainEventDispatcher (NO MediatR) for clean, explicit event handling
    /// </summary>
    public sealed class ProviderRegisteredEventHandler : IDomainEventHandler<ProviderRegisteredEvent>
    {
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly ILogger<ProviderRegisteredEventHandler> _logger;

        public ProviderRegisteredEventHandler(
            IIntegrationEventPublisher eventPublisher,
            ILogger<ProviderRegisteredEventHandler> logger)
        {
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        /// <summary>
        /// Handles the ProviderRegisteredEvent
        /// Called by SimpleDomainEventDispatcher after provider registration is persisted
        /// </summary>
        public async Task HandleAsync(ProviderRegisteredEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "🎉 ProviderRegisteredEvent received: Provider {ProviderId} registered by owner {OwnerId}",
                domainEvent.ProviderId,
                domainEvent.OwnerId);

            // Publish integration event for other contexts (UserManagement)
            var integrationEvent = new ProviderRegisteredIntegrationEvent(
                domainEvent.ProviderId.Value,
                domainEvent.OwnerId.Value,
                domainEvent.BusinessName,
                domainEvent.PrimaryCategory.ToString(), // Convert enum to string for cross-context compatibility
                domainEvent.RegisteredAt);

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "✅ Published ProviderRegisteredIntegrationEvent to message bus for provider: {ProviderId}",
                domainEvent.ProviderId);
        }

        
    }
}