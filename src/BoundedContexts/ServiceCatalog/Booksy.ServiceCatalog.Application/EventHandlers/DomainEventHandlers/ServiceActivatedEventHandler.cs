// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ServiceActivatedEventHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Application.IntegrationEvents;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
    public sealed class ServiceActivatedEventHandler : IDomainEventHandler<ServiceActivatedEvent>
    {
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly ILogger<ServiceActivatedEventHandler> _logger;

        public ServiceActivatedEventHandler(
            IIntegrationEventPublisher eventPublisher,
            ILogger<ServiceActivatedEventHandler> logger)
        {
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task HandleAsync(ServiceActivatedEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Service activated: {ServiceId} for provider {ProviderId}",
                domainEvent.ServiceId,
                domainEvent.ProviderId);

            // Publish integration event
            var integrationEvent = new ServiceActivatedIntegrationEvent(
                domainEvent.ServiceId.Value,
                domainEvent.ProviderId.Value,
                domainEvent.ServiceName,
                domainEvent.ActivatedAt);

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

            // Additional business logic
            // - Update search indexes
            // - Notify booking system of new availability
            // - Send notification to provider

            _logger.LogInformation("Published ServiceActivatedIntegrationEvent for service: {ServiceId}", domainEvent.ServiceId);
        }
    }
}