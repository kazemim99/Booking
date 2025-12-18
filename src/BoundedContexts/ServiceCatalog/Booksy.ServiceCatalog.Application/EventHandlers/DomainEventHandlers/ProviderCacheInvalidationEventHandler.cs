// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ProviderCacheInvalidationEventHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.Caching;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
    /// <summary>
    /// Handles cache invalidation for provider-related domain events.
    /// This ensures that cached provider data is removed when provider information changes,
    /// preventing stale data from being served to users.
    /// </summary>
    public sealed class ProviderCacheInvalidationEventHandler :
        IDomainEventHandler<BusinessProfileUpdatedEvent>,
        IDomainEventHandler<BusinessHoursUpdatedEvent>,
        IDomainEventHandler<GalleryImageUploadedEvent>,
        IDomainEventHandler<GalleryImageDeletedEvent>,
        IDomainEventHandler<ProviderLocationUpdatedEvent>,
        IDomainEventHandler<StaffAddedEvent>,
        IDomainEventHandler<StaffRemovedEvent>,
        IDomainEventHandler<StaffUpdatedEvent>,
        IDomainEventHandler<ProviderActivatedEvent>,
        IDomainEventHandler<ProviderDeactivatedEvent>,
        IDomainEventHandler<ExceptionAddedEvent>,
        IDomainEventHandler<ExceptionRemovedEvent>,
        IDomainEventHandler<HolidayAddedEvent>,
        IDomainEventHandler<HolidayRemovedEvent>
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<ProviderCacheInvalidationEventHandler> _logger;

        public ProviderCacheInvalidationEventHandler(
            ICacheService cacheService,
            ILogger<ProviderCacheInvalidationEventHandler> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task HandleAsync(BusinessProfileUpdatedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "BusinessProfileUpdated", cancellationToken);
        }

        public async Task HandleAsync(BusinessHoursUpdatedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "BusinessHoursUpdated", cancellationToken);
        }

        public async Task HandleAsync(GalleryImageUploadedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "GalleryImageUploaded", cancellationToken);
        }

        public async Task HandleAsync(GalleryImageDeletedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "GalleryImageDeleted", cancellationToken);
        }

        public async Task HandleAsync(ProviderLocationUpdatedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "ProviderLocationUpdated", cancellationToken);
        }

        public async Task HandleAsync(StaffAddedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "StaffAdded", cancellationToken);
        }

        public async Task HandleAsync(StaffRemovedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "StaffRemoved", cancellationToken);
        }

        public async Task HandleAsync(StaffUpdatedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "StaffUpdated", cancellationToken);
        }

        public async Task HandleAsync(ProviderActivatedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "ProviderActivated", cancellationToken);
        }

        public async Task HandleAsync(ProviderDeactivatedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "ProviderDeactivated", cancellationToken);
        }

        public async Task HandleAsync(ExceptionAddedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "ExceptionAdded", cancellationToken);
        }

        public async Task HandleAsync(ExceptionRemovedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "ExceptionRemoved", cancellationToken);
        }

        public async Task HandleAsync(HolidayAddedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "HolidayAdded", cancellationToken);
        }

        public async Task HandleAsync(HolidayRemovedEvent domainEvent, CancellationToken cancellationToken)
        {
            await InvalidateProviderCacheAsync(domainEvent.ProviderId, "HolidayRemoved", cancellationToken);
        }

        /// <summary>
        /// Invalidates all cache entries related to a specific provider.
        /// This includes both the provider ID-based cache and the owner-based cache.
        /// </summary>
        /// <param name="providerId">The ID of the provider whose cache should be invalidated</param>
        /// <param name="eventName">The name of the event that triggered the invalidation (for logging)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task InvalidateProviderCacheAsync(
            ProviderId providerId,
            string eventName,
            CancellationToken cancellationToken)
        {
            try
            {
                // Invalidate provider ID-based cache
                var providerIdKey = $"Provider:{providerId.Value}";
                await _cacheService.RemoveAsync(providerIdKey, cancellationToken);

                // Invalidate all owner-based caches using pattern matching
                // This is necessary because we don't have the OwnerId in all events,
                // and a provider might be accessed by owner ID
                var ownerPattern = "Provider:owner:*";
                await _cacheService.RemoveByPatternAsync(ownerPattern, cancellationToken);

                _logger.LogInformation(
                    "Cache invalidated for provider {ProviderId} due to event: {EventName}",
                    providerId.Value,
                    eventName);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - cache invalidation failures shouldn't break the application
                // The cache will eventually expire naturally (default: 15 minutes)
                _logger.LogError(ex,
                    "Failed to invalidate cache for provider {ProviderId} after event {EventName}. " +
                    "Cache will expire naturally after TTL period.",
                    providerId.Value,
                    eventName);
            }
        }
    }
}
