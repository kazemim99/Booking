// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/BusinessProfileUpdatedEventHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
    public sealed class BusinessProfileUpdatedEventHandler : IDomainEventHandler<BusinessProfileUpdatedEvent>
    {
        private readonly ILogger<BusinessProfileUpdatedEventHandler> _logger;

        public BusinessProfileUpdatedEventHandler(ILogger<BusinessProfileUpdatedEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(BusinessProfileUpdatedEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Business profile updated for provider: {ProviderId}",
                domainEvent.ProviderId);

            // Business logic for profile updates
            // - Update search indexes with new business info
            // - Invalidate cached provider data
            // - Update SEO metadata
            // - Sync with external directory services

            await Task.CompletedTask;

            _logger.LogInformation("Business profile update processed for provider: {ProviderId}", domainEvent.ProviderId);
        }
    }
}