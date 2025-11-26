# Cache Invalidation Problem and Solution

## Problem Description

### Issue
When updating a Provider Profile through the API and refreshing the page, changes are not visible because the data is cached in `CachedProviderReadRepository` without proper cache invalidation.

### Root Cause
The application uses a caching decorator pattern ([CachedProviderReadRepository.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/CachedProviderReadRepository.cs:17)) that caches provider data with the following keys:
- `Provider:{providerId}` - for GetByIdAsync
- `Provider:owner:{ownerId}` - for GetByOwnerIdAsync

**The Problem:** When provider data is updated through command handlers (e.g., UpdateProviderProfile, UpdateBusinessProfile, UpdateContactInfo), the cache is NOT invalidated, causing stale data to be served until the cache expires (default: 15 minutes).

### Affected Operations
All provider update operations suffer from this issue:
- [UpdateProviderProfileCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UpdateProviderProfile/UpdateProviderProfileCommandHandler.cs:8)
- [UpdateBusinessProfileCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UpdateBusinessProfile/UpdateBusinessProfileCommandHandler.cs:8)
- [UpdateContactInfoCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UpdateContactInfo/UpdateContactInfoCommandHandler.cs)
- [UpdateLocationCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UpdateLocation/UpdateLocationCommandHandler.cs)
- [UpdateBusinessHoursCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UpdateBusinessHours/UpdateBusinessHoursCommandHandler.cs)
- [UpdateWorkingHoursCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UpdateWorkingHours/UpdateWorkingHoursCommandHandler.cs)
- [UploadGalleryImagesCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UploadGalleryImages/UploadGalleryImagesCommandHandler.cs)
- [DeleteGalleryImageCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/DeleteGalleryImage/DeleteGalleryImageCommandHandler.cs)

## Technical Analysis

### Current Architecture

```
┌─────────────────────┐
│  Command Handler    │
│  (Update Provider)  │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ ProviderWrite       │
│ Repository          │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│   Save to DB        │
└─────────────────────┘

❌ Cache NOT Invalidated

┌─────────────────────┐
│ Next Read Request   │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ CachedProviderRead  │
│ Repository          │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Returns STALE Data  │ ⚠️
└─────────────────────┘
```

### Cache Service Available Methods
The [ICacheService](../src/Infrastructure/Booksy.Infrastructure.Core/Caching/ICacheService.cs:10) interface provides:
- `RemoveAsync(string key)` - Remove single cache key
- `RemoveByPatternAsync(string pattern)` - Remove keys matching pattern

## Solutions

### Solution 1: Domain Event-Based Cache Invalidation (RECOMMENDED) ⭐

**Benefits:**
- ✅ Follows Domain-Driven Design principles
- ✅ Decouples cache invalidation from business logic
- ✅ Automatically works for all provider updates
- ✅ Easy to extend for other entities
- ✅ Leverages existing domain events

**Implementation Steps:**

#### 1. Create Domain Event Handler for Cache Invalidation

```csharp
// File: src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/EventHandlers/CacheInvalidationEventHandlers.cs

using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.Caching;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers;

/// <summary>
/// Handles cache invalidation for provider-related domain events
/// </summary>
public sealed class ProviderCacheInvalidationEventHandler :
    IDomainEventHandler<BusinessProfileUpdatedEvent>,
    IDomainEventHandler<BusinessHoursUpdatedEvent>,
    IDomainEventHandler<GalleryImageUploadedEvent>,
    IDomainEventHandler<GalleryImageDeletedEvent>,
    IDomainEventHandler<ProviderLocationUpdatedEvent>,
    IDomainEventHandler<StaffAddedEvent>,
    IDomainEventHandler<StaffRemovedEvent>,
    IDomainEventHandler<StaffUpdatedEvent>
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

    public async Task Handle(BusinessProfileUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateProviderCacheAsync(notification.ProviderId, cancellationToken);
    }

    public async Task Handle(BusinessHoursUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateProviderCacheAsync(notification.ProviderId, cancellationToken);
    }

    public async Task Handle(GalleryImageUploadedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateProviderCacheAsync(notification.ProviderId, cancellationToken);
    }

    public async Task Handle(GalleryImageDeletedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateProviderCacheAsync(notification.ProviderId, cancellationToken);
    }

    public async Task Handle(ProviderLocationUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateProviderCacheAsync(notification.ProviderId, cancellationToken);
    }

    public async Task Handle(StaffAddedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateProviderCacheAsync(notification.ProviderId, cancellationToken);
    }

    public async Task Handle(StaffRemovedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateProviderCacheAsync(notification.ProviderId, cancellationToken);
    }

    public async Task Handle(StaffUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateProviderCacheAsync(notification.ProviderId, cancellationToken);
    }

    private async Task InvalidateProviderCacheAsync(ProviderId providerId, CancellationToken cancellationToken)
    {
        try
        {
            // Invalidate both cache keys for this provider
            var providerIdKey = $"Provider:{providerId.Value}";
            await _cacheService.RemoveAsync(providerIdKey, cancellationToken);

            // Also invalidate owner-based cache (requires getting ownerId from context or event)
            // This might need adjustment based on your event structure
            var ownerPattern = $"Provider:owner:*";
            await _cacheService.RemoveByPatternAsync(ownerPattern, cancellationToken);

            _logger.LogInformation(
                "Cache invalidated for provider {ProviderId}",
                providerId.Value);
        }
        catch (Exception ex)
        {
            // Log but don't throw - cache invalidation failures shouldn't break the application
            _logger.LogError(ex,
                "Failed to invalidate cache for provider {ProviderId}",
                providerId.Value);
        }
    }
}
```

#### 2. Register Event Handler

The handler will be automatically registered by MediatR if you follow the existing pattern in your DI configuration.

**Verification:**
- Check that domain events are properly raised in Provider aggregate methods
- Verify [BusinessProfileUpdatedEvent](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Events/BusinessProfileUpdatedEvent.cs:9) is raised when profile is updated
- Ensure UnitOfWork dispatches domain events after SaveChangesAsync

---

### Solution 2: Direct Cache Invalidation in Command Handlers (ALTERNATIVE)

**Benefits:**
- ✅ Simple and straightforward
- ✅ Explicit cache invalidation
- ✅ Easy to understand

**Drawbacks:**
- ❌ Violates separation of concerns
- ❌ Requires adding ICacheService to every command handler
- ❌ Easy to forget in new handlers
- ❌ Couples business logic with caching

**Implementation Example:**

```csharp
public sealed class UpdateProviderProfileCommandHandler : ICommandHandler<UpdateProviderProfileCommand, UpdateProviderProfileResult>
{
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly ICacheService _cacheService; // Add this
    private readonly ILogger<UpdateProviderProfileCommandHandler> _logger;

    public UpdateProviderProfileCommandHandler(
        IProviderWriteRepository providerWriteRepository,
        ICacheService cacheService, // Add this
        ILogger<UpdateProviderProfileCommandHandler> logger)
    {
        _providerWriteRepository = providerWriteRepository;
        _cacheService = cacheService; // Add this
        _logger = logger;
    }

    public async Task<UpdateProviderProfileResult> Handle(
        UpdateProviderProfileCommand request,
        CancellationToken cancellationToken)
    {
        // ... existing code ...

        // Save changes
        await _providerWriteRepository.SaveProviderAsync(provider, cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync($"Provider:{request.ProviderId}", cancellationToken);
        await _cacheService.RemoveByPatternAsync("Provider:owner:*", cancellationToken);

        // ... existing code ...
    }
}
```

**Note:** This approach requires updating ~15-20 command handlers.

---

### Solution 3: Decorator Pattern on Write Repository (CLEAN ARCHITECTURE)

**Benefits:**
- ✅ Centralized cache invalidation
- ✅ Follows decorator pattern (like CachedProviderReadRepository)
- ✅ No changes to command handlers
- ✅ Maintains separation of concerns

**Implementation:**

```csharp
// File: src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/CachedProviderWriteRepository.cs

using Booksy.Infrastructure.Core.Caching;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Cached decorator for ProviderWriteRepository that invalidates cache on mutations
/// </summary>
public sealed class CachedProviderWriteRepository : IProviderWriteRepository
{
    private readonly IProviderWriteRepository _inner;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedProviderWriteRepository> _logger;

    public CachedProviderWriteRepository(
        IProviderWriteRepository inner,
        ICacheService cacheService,
        ILogger<CachedProviderWriteRepository> logger)
    {
        _inner = inner;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Provider?> GetByIdAsync(ProviderId id, CancellationToken cancellationToken = default)
    {
        return await _inner.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Provider?> GetByOwnerIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await _inner.GetByOwnerIdAsync(id, cancellationToken);
    }

    public async Task<Provider?> GetDraftProviderByOwnerIdAsync(UserId ownerId, CancellationToken cancellationToken = default)
    {
        return await _inner.GetDraftProviderByOwnerIdAsync(ownerId, cancellationToken);
    }

    public async Task SaveProviderAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        await _inner.SaveProviderAsync(provider, cancellationToken);
        await InvalidateCacheAsync(provider.Id, provider.OwnerId, cancellationToken);
    }

    public async Task UpdateProviderAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        await _inner.UpdateProviderAsync(provider, cancellationToken);
        await InvalidateCacheAsync(provider.Id, provider.OwnerId, cancellationToken);
    }

    public async Task DeleteProviderAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteProviderAsync(provider, cancellationToken);
        await InvalidateCacheAsync(provider.Id, provider.OwnerId, cancellationToken);
    }

    private async Task InvalidateCacheAsync(ProviderId providerId, UserId ownerId, CancellationToken cancellationToken)
    {
        try
        {
            // Invalidate both cache keys
            await _cacheService.RemoveAsync($"Provider:{providerId.Value}", cancellationToken);
            await _cacheService.RemoveAsync($"Provider:owner:{ownerId.Value}", cancellationToken);

            _logger.LogDebug(
                "Invalidated cache for provider {ProviderId} and owner {OwnerId}",
                providerId.Value,
                ownerId.Value);
        }
        catch (Exception ex)
        {
            // Log but don't throw - cache invalidation failures shouldn't break the application
            _logger.LogError(ex,
                "Failed to invalidate cache for provider {ProviderId}",
                providerId.Value);
        }
    }
}
```

**Register in DI:**

```csharp
// In ServiceCatalogInfrastructureExtensions.cs
public static IServiceCollection AddServiceCatalogInfrastructureWithCache(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddServiceCatalogInfrastructure(configuration);

    // Add caching decorators
    services.Decorate<IProviderReadRepository, CachedProviderReadRepository>();
    services.Decorate<IProviderWriteRepository, CachedProviderWriteRepository>(); // Add this
    services.Decorate<IServiceReadRepository, CachedServiceReadRepository>();

    return services;
}
```

---

## Recommendation

**Use Solution 1 (Domain Event-Based)** for the following reasons:

1. **Best Practices:** Aligns with DDD and CQRS principles already used in the codebase
2. **Existing Infrastructure:** You already have domain events defined (BusinessProfileUpdatedEvent, etc.)
3. **Extensibility:** Easy to add other side effects (notifications, analytics) without modifying handlers
4. **Testability:** Event handlers can be tested independently
5. **Maintainability:** New developers naturally follow the pattern

**Fallback:** If domain events are not properly configured or you need a quick fix, use Solution 3 (Decorator Pattern) as it requires minimal changes and maintains clean architecture.

**Avoid:** Solution 2 unless you have very few command handlers and simple requirements.

---

## Testing the Fix

### 1. Verify Cache Invalidation

```csharp
// Integration test example
[Fact]
public async Task UpdateProviderProfile_ShouldInvalidateCache()
{
    // Arrange
    var provider = await CreateTestProvider();

    // Act 1: Read to populate cache
    var cached1 = await _providerReadRepository.GetByIdAsync(provider.Id);

    // Act 2: Update profile
    await _mediator.Send(new UpdateProviderProfileCommand
    {
        ProviderId = provider.Id.Value,
        Email = "newemail@example.com"
    });

    // Act 3: Read again (should get fresh data, not cached)
    var cached2 = await _providerReadRepository.GetByIdAsync(provider.Id);

    // Assert
    cached2.ContactInfo.Email.Value.Should().Be("newemail@example.com");
}
```

### 2. Manual Testing Steps

1. **Enable Logging:** Set log level to Debug to see cache operations
2. **Update Profile:** Make a profile update via API
3. **Check Logs:** Verify "Cache invalidated for provider X" message appears
4. **Refresh Page:** Confirm new data appears immediately
5. **Monitor Redis:** Use Redis CLI to verify keys are removed

```bash
# Monitor Redis keys
redis-cli KEYS "booksy:Provider:*"

# Watch cache operations in real-time
redis-cli MONITOR
```

---

## Performance Considerations

### Cache Invalidation Strategy

**Current Approach:** Cache-aside pattern with manual invalidation
- Read-through: Load from cache, fetch from DB if miss
- Write-through: Update DB, then invalidate cache

**Pros:**
- Simple to understand
- Predictable behavior
- No race conditions with proper ordering

**Cons:**
- Cache miss after every update (temporary performance hit)
- Next read will be slower (needs DB fetch)

**Alternative:** Write-through with cache update
```csharp
// After saving
await _cacheService.SetAsync($"Provider:{providerId}", updatedProvider, cacheDuration);
```

**Recommendation:** Stick with invalidation approach for now. It's safer and the temporary performance hit is acceptable for profile updates (infrequent operation).

---

## Additional Improvements

### 1. Add Cache Versioning
```csharp
private const string CACHE_VERSION = "v2";
var cacheKey = $"{CACHE_VERSION}:Provider:{id}";
```

### 2. Add Monitoring
```csharp
// Track cache hit/miss rates
_metrics.IncrementCounter("cache.hits", new { entity = "provider" });
_metrics.IncrementCounter("cache.invalidations", new { entity = "provider" });
```

### 3. Consider Cache Warming
For frequently accessed providers, consider warming the cache after invalidation:
```csharp
// After invalidation
await _cacheService.SetAsync(cacheKey, provider, cacheDuration);
```

---

## Related Files

### Infrastructure
- [CachedProviderReadRepository.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/CachedProviderReadRepository.cs)
- [ProviderWriteRepository.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/ProviderWriteRepository.cs)
- [ICacheService.cs](../src/Infrastructure/Booksy.Infrastructure.Core/Caching/ICacheService.cs)
- [RedisCacheService.cs](../src/Infrastructure/Booksy.Infrastructure.Core/Caching/RedisCacheService.cs)

### Domain Events
- [BusinessProfileUpdatedEvent.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Events/BusinessProfileUpdatedEvent.cs)
- [BusinessHoursUpdatedEvent.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Events/BusinessHoursUpdatedEvent.cs)
- [GalleryImageUploadedEvent.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Events/GalleryImageUploadedEvent.cs)

### Command Handlers (Need Cache Invalidation)
- [UpdateProviderProfileCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UpdateProviderProfile/UpdateProviderProfileCommandHandler.cs)
- [UpdateBusinessProfileCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UpdateBusinessProfile/UpdateBusinessProfileCommandHandler.cs)

---

## Implementation Summary

### ✅ Implementation Complete (November 26, 2025)

**Solution 1 (Domain Event-Based)** has been successfully implemented.

#### Files Created/Modified:

1. **Created:** [ProviderCacheInvalidationEventHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ProviderCacheInvalidationEventHandler.cs)
   - Handles 14 different provider-related domain events
   - Invalidates cache automatically when provider data changes
   - Includes comprehensive logging and error handling

2. **Modified:** [Provider.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Provider.cs#L738-L783)
   - Added gallery management wrapper methods that raise domain events

3. **Modified:** Command handlers to use new Provider methods
   - [UploadGalleryImagesCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UploadGalleryImages/UploadGalleryImagesCommandHandler.cs#L60-L64)
   - [DeleteGalleryImageCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/DeleteGalleryImage/DeleteGalleryImageCommandHandler.cs#L47-L48)

#### Events Handled:

✅ BusinessProfileUpdatedEvent
✅ BusinessHoursUpdatedEvent
✅ GalleryImageUploadedEvent
✅ GalleryImageDeletedEvent
✅ ProviderLocationUpdatedEvent
✅ StaffAddedEvent / StaffRemovedEvent / StaffUpdatedEvent
✅ ProviderActivatedEvent / ProviderDeactivatedEvent
✅ ExceptionAddedEvent / ExceptionRemovedEvent
✅ HolidayAddedEvent / HolidayRemovedEvent

#### How It Works:

1. User updates provider → Command handler calls aggregate method
2. Aggregate raises domain event → Event captured by UnitOfWork
3. UnitOfWork commits → Domain events dispatched after SaveChanges
4. Event handler executes → Cache invalidated automatically
5. Next read → Fresh data fetched from database

#### Build Status:

✅ Domain project builds successfully
✅ Application project builds successfully
✅ No compilation errors

---

**Last Updated:** November 26, 2025
**Status:** ✅ **IMPLEMENTED** - Solution 1 (Domain Event-Based) Complete
**Priority:** High - User experience fix deployed
