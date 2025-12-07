# Cache Invalidation Fix: UpdateProviderProfileCommandHandler

**Date**: 2025-12-02
**Issue**: Profile image updates were not invalidating cache
**Status**: ✅ Fixed

---

## Problem

The `UpdateProviderProfileCommandHandler` was **NOT dispatching domain events** for cache invalidation when updating provider profile images.

### Root Cause

The handler was calling `provider.Profile.UpdateProfileImage()` directly on the `BusinessProfile` entity (line 41), which:
- ❌ Only updates the entity property
- ❌ Does NOT raise any domain events
- ❌ Does NOT trigger cache invalidation
- ❌ Results in stale cached data being served to users

```csharp
// OLD CODE - No event dispatched
if (!string.IsNullOrWhiteSpace(request.ProfileImageUrl))
{
    provider.Profile.UpdateProfileImage(request.ProfileImageUrl);  // ❌ No events!
}
```

### Impact

When providers updated their profile images:
1. ✅ Database was updated correctly
2. ❌ Cache was **NOT** invalidated
3. ❌ Users continued seeing **old profile images** until cache expired naturally (15 minutes)
4. ❌ Provider changes appeared to "not work" from user perspective

---

## Solution

Modified the handler to use the `Provider.UpdateBusinessProfile()` method, which properly raises the `BusinessProfileUpdatedEvent` domain event.

### Fixed Code

```csharp
// NEW CODE - Events properly dispatched
// Use UpdateBusinessProfile to ensure domain events are raised for cache invalidation
if (!string.IsNullOrWhiteSpace(request.ProfileImageUrl))
{
    provider.UpdateBusinessProfile(
        provider.Profile.BusinessName,
        provider.Profile.BusinessDescription,
        request.ProfileImageUrl);  // ✅ Raises BusinessProfileUpdatedEvent!
}
```

### Event Flow

1. **Command Handler** calls `provider.UpdateBusinessProfile()`
2. **Provider Aggregate** raises `BusinessProfileUpdatedEvent` (line 211 in Provider.cs)
3. **ProviderCacheInvalidationEventHandler** catches the event (line 44)
4. **Cache Service** invalidates cached provider data:
   - Removes `Provider:{providerId}` cache entry
   - Removes all `Provider:owner:*` cache entries
5. ✅ **Next request fetches fresh data from database**

---

## Files Changed

### Modified
- [UpdateProviderProfileCommandHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UpdateProviderProfile/UpdateProviderProfileCommandHandler.cs)
  - Changed from `Profile.UpdateProfileImage()` → `UpdateBusinessProfile()`
  - Added comment explaining the change

### Related Files (No changes needed)
- ✅ [Provider.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Provider.cs) - Already raises `BusinessProfileUpdatedEvent`
- ✅ [ProviderCacheInvalidationEventHandler.cs](../src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ProviderCacheInvalidationEventHandler.cs) - Already handles `BusinessProfileUpdatedEvent`

---

## Cache Invalidation Pattern

The application uses a robust domain event-driven cache invalidation pattern:

### Supported Events (all trigger cache invalidation)

The `ProviderCacheInvalidationEventHandler` handles these events:

1. ✅ `BusinessProfileUpdatedEvent` - Profile/description/image changes
2. ✅ `BusinessHoursUpdatedEvent` - Operating hours changes
3. ✅ `GalleryImageUploadedEvent` - Gallery image additions
4. ✅ `GalleryImageDeletedEvent` - Gallery image removals
5. ✅ `ProviderLocationUpdatedEvent` - Address/coordinates changes
6. ✅ `StaffAddedEvent` - Staff member additions
7. ✅ `StaffRemovedEvent` - Staff member removals
8. ✅ `StaffUpdatedEvent` - Staff member updates
9. ✅ `ProviderActivatedEvent` - Provider activation
10. ✅ `ProviderDeactivatedEvent` - Provider deactivation
11. ✅ `ExceptionAddedEvent` - Exception schedule additions
12. ✅ `ExceptionRemovedEvent` - Exception schedule removals
13. ✅ `HolidayAddedEvent` - Holiday additions
14. ✅ `HolidayRemovedEvent` - Holiday removals

### Cache Invalidation Strategy

```csharp
// Invalidate provider ID-based cache
var providerIdKey = $"Provider:{providerId.Value}";
await _cacheService.RemoveAsync(providerIdKey, cancellationToken);

// Invalidate all owner-based caches using pattern matching
var ownerPattern = "Provider:owner:*";
await _cacheService.RemoveByPatternAsync(ownerPattern, cancellationToken);
```

**Note**: Cache invalidation failures are logged but **don't throw exceptions** - cache will expire naturally after TTL (15 minutes default).

---

## Testing

### Manual Testing Steps

1. **Update Profile Image**:
   ```bash
   POST /api/v1/providers/{providerId}/profile
   {
     "profileImageUrl": "https://new-image.jpg"
   }
   ```

2. **Verify Event Dispatched**:
   - Check logs for: `"Cache invalidated for provider {ProviderId} due to event: BusinessProfileUpdated"`

3. **Verify Cache Cleared**:
   - Subsequent GET requests should fetch fresh data
   - Profile image should update immediately

### Expected Behavior

- ✅ Profile image updates immediately (no 15-minute delay)
- ✅ Domain event logged in console
- ✅ Cache invalidation logged
- ✅ Next API call returns fresh data from database

---

## Related Documentation

- [Cache Invalidation Implementation](./CACHE_INVALIDATION_IMPLEMENTATION_SUMMARY.md)
- [Cache Invalidation Problem & Solution](./CACHE_INVALIDATION_PROBLEM_AND_SOLUTION.md)
- [Provider Hierarchy Proposal](../openspec/changes/add-provider-hierarchy/README.md)

---

## Best Practices

### ✅ DO

1. **Always use aggregate methods** that raise domain events
2. **Never directly modify entity properties** for state changes
3. **Follow the event-driven pattern** for cache invalidation
4. **Log domain events** for debugging and monitoring

### ❌ DON'T

1. **Don't call entity methods directly** (like `Profile.UpdateProfileImage()`)
2. **Don't manually invalidate cache** in command handlers
3. **Don't skip event publishing** to "optimize" performance
4. **Don't throw exceptions** on cache invalidation failures

---

## Future Considerations

When implementing the [provider hierarchy proposal](../openspec/changes/add-provider-hierarchy/README.md), ensure:

1. Individual Providers (staff) also trigger cache invalidation events
2. Parent-child relationships are considered in cache invalidation
3. Organization cache is invalidated when staff profiles change
4. New domain events are added to `ProviderCacheInvalidationEventHandler`

---

## Summary

✅ **Fixed**: `UpdateProviderProfileCommandHandler` now properly dispatches domain events
✅ **Cache Invalidation**: Works correctly for profile image updates
✅ **User Experience**: Profile changes reflect immediately
✅ **Pattern Established**: All provider updates follow event-driven cache invalidation

The fix ensures consistency across all provider update operations and maintains the integrity of the domain event-driven architecture.
