// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ServiceCreatedEventHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Application.IntegrationEvents;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
    public sealed class ServiceCreatedEventHandler : IDomainEventHandler<ServiceCreatedEvent>
    {
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly ILogger<ServiceCreatedEventHandler> _logger;

        public ServiceCreatedEventHandler(
            IIntegrationEventPublisher eventPublisher,
            ILogger<ServiceCreatedEventHandler> logger)
        {
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task HandleAsync(ServiceCreatedEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Service created: {ServiceId} for provider {ProviderId}",
                domainEvent.ServiceId,
                domainEvent.ProviderId);

            var integrationEvent = new ServiceCreatedIntegrationEvent(
                domainEvent.ServiceId.Value,
                domainEvent.ProviderId.Value,
                domainEvent.ServiceName,
                domainEvent.Category.ToEnglishName(),
                domainEvent.BasePrice.Amount,
                domainEvent.BasePrice.Currency,
                domainEvent.Duration.Value,
                domainEvent.CreatedAt);

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation("Published ServiceCreatedIntegrationEvent for service: {ServiceId}", domainEvent.ServiceId);
        }
    }
}