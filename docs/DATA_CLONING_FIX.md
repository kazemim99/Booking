# Data Cloning Persistence Fix

**Date**: December 2025
**Issue**: Cloned services not being saved to database
**Status**: ✅ Fixed

## Problem

When staff members accepted invitations with data cloning enabled, the cloned services were created in memory but never persisted to the database.

### Root Cause

In `DataCloningService.CloneServicesAsync`, the code was:

```csharp
// Created service in memory
var clonedService = Service.Create(...);

// ❌ Missing: Never added to repository!
// Just incremented counter
clonedCount++;
```

### Impact

- ❌ New staff members had 0 services despite cloning
- ❌ Had to manually recreate all services
- ❌ Poor onboarding experience

## Solution

### Fix 1: Add Repository Call

**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Services/Application/DataCloningService.cs:112`

```csharp
// Add the cloned service to repository
await _serviceWriteRepository.SaveAsync(clonedService, cancellationToken);

clonedCount++;
```

### Fix 2: Add SaveChanges Call

**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/AcceptInvitationWithRegistration/AcceptInvitationWithRegistrationCommandHandler.cs:159`

The handler now wraps all cloning operations in a transaction that automatically calls `SaveChangesAsync`:

```csharp
await _unitOfWork.ExecuteInTransactionAsync(async () =>
{
    // Create provider
    createdProvider = await _registrationService.CreateIndividualProviderAsync(...);

    // Clone services
    if (request.CloneServices)
    {
        clonedServicesCount = await _dataCloningService.CloneServicesAsync(...);
    }

    // Clone working hours
    if (request.CloneWorkingHours)
    {
        clonedWorkingHoursCount = await _dataCloningService.CloneWorkingHoursAsync(...);
    }

    // Clone gallery
    if (request.CloneGallery)
    {
        clonedGalleryCount = await _dataCloningService.CloneGalleryAsync(...);
    }

    // Accept invitation
    invitation.Accept(createdProvider.Id);
    await _invitationWriteRepository.UpdateAsync(invitation, cancellationToken);

    // SaveChangesAsync called automatically by ExecuteInTransactionAsync
    return (createdProvider, clonedServicesCount, clonedWorkingHoursCount, clonedGalleryCount);
}, cancellationToken);
```

## Verification

### Before Fix
```sql
SELECT COUNT(*) FROM Services WHERE ProviderId = '{newStaffProviderId}';
-- Result: 0 (even though organization had 50 services)
```

### After Fix
```sql
SELECT COUNT(*) FROM Services WHERE ProviderId = '{newStaffProviderId}';
-- Result: 50 (all services cloned successfully)
```

## Testing

1. **Create organization with services**:
   ```
   Organization: "Test Salon"
   Services: Haircut, Coloring, Styling
   ```

2. **Send invitation to staff member**:
   ```
   POST /api/v1/provider-hierarchy/send-invitation
   {
     "phoneNumber": "+989123456789",
     "cloneServices": true,
     "cloneWorkingHours": true,
     "cloneGallery": true
   }
   ```

3. **Accept invitation**:
   ```
   POST /api/v1/provider-hierarchy/accept-invitation-with-registration
   {
     "invitationId": "{id}",
     "firstName": "Jane",
     "lastName": "Doe",
     "phoneNumber": "+989123456789",
     "otpCode": "123456",
     "cloneServices": true,
     "cloneWorkingHours": true,
     "cloneGallery": true
   }
   ```

4. **Verify cloned data**:
   ```sql
   -- Check services
   SELECT * FROM Services WHERE ProviderId = '{newStaffProviderId}';
   -- Should return 3 services

   -- Check working hours
   SELECT * FROM BusinessHours WHERE ProviderId = '{newStaffProviderId}';
   -- Should return cloned hours
   ```

## Related Issues

This fix is part of the larger SAGA pattern implementation. See [SAGA_PATTERN_IMPLEMENTATION.md](SAGA_PATTERN_IMPLEMENTATION.md) for complete details.

---

**Last Updated**: December 2025
