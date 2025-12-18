# Cache Invalidation Fix for Provider Profile Updates

## Problem Summary

When updating provider profile information (location, business info, business hours), the frontend was still showing **stale cached data** even after successful updates. This was caused by missing domain event in the backend.

---

## Root Cause Analysis

### 1. **Backend Caching Layer**

The system uses `CachedProviderReadRepository` which caches provider data in Redis with a **15-minute TTL**:

```csharp
// Cache keys used:
- Provider:{providerId}
- Provider:owner:{ownerId}
```

**Location:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/CachedProviderReadRepository.cs`

### 2. **Cache Invalidation Mechanism**

The system has a `ProviderCacheInvalidationEventHandler` that listens for domain events to invalidate cache:

- `BusinessProfileUpdatedEvent` ✅
- `BusinessHoursUpdatedEvent` ✅
- `ProviderLocationUpdatedEvent` ❌ **NOT RAISED**
- `GalleryImageUploadedEvent` ✅
- `StaffAddedEvent` ✅
- etc.

**Location:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ProviderCacheInvalidationEventHandler.cs`

### 3. **Missing Domain Event**

The `UpdateAddress()` method in the Provider aggregate **did not raise a domain event**:

**Before (BROKEN):**
```csharp
public void UpdateAddress(BusinessAddress newAddress)
{
    Address = newAddress;
    // ❌ NO EVENT RAISED - Cache never invalidated!
}
```

**After (FIXED):**
```csharp
public void UpdateAddress(BusinessAddress newAddress)
{
    var previousAddress = Address;
    var previousCoordinates = previousAddress.Latitude.HasValue && previousAddress.Longitude.HasValue
        ? Coordinates.Create(previousAddress.Latitude.Value, previousAddress.Longitude.Value)
        : null;

    Address = newAddress;

    var newCoordinates = Coordinates.Create(newAddress.Latitude ?? 0, newAddress.Longitude ?? 0);

    // ✅ NOW RAISES EVENT - Cache gets invalidated!
    RaiseDomainEvent(new ProviderLocationUpdatedEvent(
        providerId: Id,
        providerName: Profile.BusinessName,
        newAddress: newAddress,
        newCoordinates: newCoordinates,
        changeType: LocationChangeType.Relocation,
        updatedByUserId: OwnerId.Value.ToString(),
        effectiveDate: DateTime.UtcNow,
        affectedAppointmentIds: Array.Empty<string>().ToList().AsReadOnly(),
        previousAddress: previousAddress,
        previousCoordinates: previousCoordinates
    ));
}
```

---

## The Fix

### Changed File
`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Provider.cs`

### What Changed
Added `ProviderLocationUpdatedEvent` to the `UpdateAddress()` method so that when a provider's location is updated:

1. The domain event is raised
2. The event is dispatched by MediatR
3. `ProviderCacheInvalidationEventHandler` receives the event
4. Cache is invalidated for both keys:
   - `Provider:{providerId}`
   - `Provider:owner:*` (pattern match)
5. Next API call fetches fresh data from database

---

## How Cache Invalidation Works

```
┌─────────────────────────────────────────────────────────────┐
│ 1. User Updates Location via API                            │
│    PUT /api/v1/providers/{id}/location                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. UpdateLocationCommandHandler                             │
│    - Fetches provider from repository                       │
│    - Calls provider.UpdateAddress(newAddress)               │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Provider.UpdateAddress() - Domain Method                 │
│    - Updates Address property                               │
│    - ✅ Raises ProviderLocationUpdatedEvent                 │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. EF Core SaveChanges()                                    │
│    - Saves provider to database                             │
│    - Dispatches domain events via MediatR                   │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. ProviderCacheInvalidationEventHandler                    │
│    - Receives ProviderLocationUpdatedEvent                  │
│    - Calls ICacheService.RemoveAsync()                      │
│    - Invalidates: Provider:{id}                             │
│    - Invalidates: Provider:owner:* (pattern)                │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Next API Call - GET /api/v1/providers/{id}              │
│    - Cache MISS (invalidated)                               │
│    - Fetches fresh data from database                       │
│    - Returns updated location ✅                             │
│    - Caches for 15 minutes                                  │
└─────────────────────────────────────────────────────────────┘
```

---

## Additional Issues Found (Bonus)

### Frontend: Excessive API Calls

**Problem:** The ProfileManager component loads ALL cities for ALL provinces (31 provinces × ~50 cities = 1,550+ API calls!) on mount.

**Location:** `booksy-frontend/src/modules/provider/components/dashboard/ProfileManager.vue:676-686`

```typescript
onMounted(async () => {
  await loadProvinces()
  const provincesList = provinces.value
  for (const province of provincesList) {
    await loadCitiesByProvinceId(province.id)  // ⚠️ 31+ API calls!
  }
  // ...
})
```

**Recommendation:** Implement lazy loading - only load cities when user selects a province.

---

## Testing the Fix

### Before Fix
1. Update provider location via API
2. Call GET /api/v1/providers/{id}
3. ❌ Returns old cached location (stale for up to 15 minutes)

### After Fix
1. Update provider location via API
2. Call GET /api/v1/providers/{id}
3. ✅ Returns new location immediately (cache invalidated)

### Verify Cache Invalidation in Logs
Look for these log messages:
```
[Information] Cache invalidated for provider {ProviderId} due to event: ProviderLocationUpdated
```

---

## Related Files

### Backend
- `Provider.cs` - Domain aggregate (UPDATED)
- `ProviderLocationUpdatedEvent.cs` - Domain event
- `ProviderCacheInvalidationEventHandler.cs` - Event handler
- `CachedProviderReadRepository.cs` - Caching decorator
- `UpdateLocationCommandHandler.cs` - Command handler

### Frontend
- `ProfileManager.vue` - Profile management UI
- `provider.store.ts` - Pinia store
- `useLocations.ts` - Location composable
- `http-client.ts` - HTTP client with caching

---

## Summary

✅ **FIXED:** Provider location updates now properly invalidate the cache by raising the `ProviderLocationUpdatedEvent` domain event.

⚠️ **TODO:** Consider optimizing frontend to use lazy loading for cities to reduce API calls from 1,550+ to only what's needed.
