// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ProviderRegisteredEventHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Application.IntegrationEvents;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
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

        public async Task HandleAsync(ProviderRegisteredEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Provider registered: {ProviderId} for owner {OwnerId}",
                domainEvent.ProviderId,
                domainEvent.OwnerId);

            // Publish integration event for other contexts
            var integrationEvent = new ProviderRegisteredIntegrationEvent(
                domainEvent.ProviderId.Value,
                domainEvent.OwnerId.Value,
                domainEvent.BusinessName,
                domainEvent.ProviderType,
                domainEvent.RegisteredAt);

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation("Published ProviderRegisteredIntegrationEvent for provider: {ProviderId}", domainEvent.ProviderId);
        }
    }
}