// ========================================
// AsanRezerve.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ServiceCreatedEventHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.Events;
using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
using AsanRezerve.ServiceCatalog.Application.IntegrationEvents;
using AsanRezerve.ServiceCatalog.Domain.Enums.Extensions;
using AsanRezerve.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
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