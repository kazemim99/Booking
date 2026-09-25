// ========================================
// AsanRezerve.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ProviderActivatedEventHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Events;
using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
using AsanRezerve.ServiceCatalog.Application.IntegrationEvents;
using AsanRezerve.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
    public sealed class ProviderActivatedEventHandler : IDomainEventHandler<ProviderActivatedEvent>
    {
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly ILogger<ProviderActivatedEventHandler> _logger;

        public ProviderActivatedEventHandler(
            IIntegrationEventPublisher eventPublisher,
            ILogger<ProviderActivatedEventHandler> logger)
        {
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task HandleAsync(ProviderActivatedEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Provider activated: {ProviderId} at {ActivatedAt}",
                domainEvent.ProviderId,
                domainEvent.ActivatedAt);

            // Publish integration event for other contexts
            var integrationEvent = new ProviderActivatedIntegrationEvent(
                domainEvent.ProviderId.Value,
                domainEvent.ActivatedAt);

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

            // Additional business logic
            // - Send welcome email to provider
            // - Create default business settings
            // - Initialize analytics tracking

            _logger.LogInformation("Published ProviderActivatedIntegrationEvent for provider: {ProviderId}", domainEvent.ProviderId);
        }
    }
}