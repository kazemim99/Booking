# Provider Hierarchy Migration - Implementation Complete

**Date**: 2025-12-03
**Status**: ✅ Completed
**Priority**: High

---

## Overview

Successfully migrated the availability and booking slot generation system from the legacy **Staff entity model** to the new **Provider Hierarchy model**. Staff members are now treated as **Individual Providers** within an Organization.

---

## Changes Implemented

### 1. ✅ IAvailabilityService Interface (Domain Layer)

**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/DomainServices/IAvailabilityService.cs`

**Changes**:
- Updated `GetAvailableTimeSlotsAsync()` signature:
  - Changed parameter from `Staff? staff` to `Provider? individualProvider`
  - Updated documentation to reflect hierarchy model

- Updated `IsTimeSlotAvailableAsync()` signature:
  - Changed parameter from `Staff staff` to `Provider individualProvider`

- Updated `GetAvailableStaffAsync()` return type:
  - Changed from `Task<IReadOnlyList<Staff>>` to `Task<IReadOnlyList<Provider>>`

**Impact**: Interface now aligns with the Provider Hierarchy architecture.

---

### 2. ✅ AvailabilityService Implementation (Application Layer)

**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Services/AvailabilityService.cs`

**Changes**:

#### Constructor
- Added `IProviderReadRepository` dependency for accessing hierarchy data
- Updated constructor signature

#### GetAvailableTimeSlotsAsync()
- Changed to use `GetQualifiedIndividualProvidersAsync()` instead of `GetQualifiedStaff()`
- Now loads staff members from hierarchy using `_providerRepository.GetStaffByOrganizationIdAsync()`
- Updated to call `GenerateTimeSlotsForIndividualAsync()` instead of old staff-based method

#### IsTimeSlotAvailableAsync()
- Updated to accept `Provider individualProvider` instead of `Staff staff`
- Changed validation to check `individualProvider.Status == ProviderStatus.Active`
- Updated to use `individualProvider.Id.Value` for booking conflict checks

#### GetAvailableStaffAsync()
- Updated to work with `List<Provider>` instead of `List<Staff>`
- Now returns individual providers (staff members)

#### New Helper Methods
1. **GetQualifiedIndividualProvidersAsync()**
   - Loads staff members from hierarchy using repository
   - Filters for active individual providers qualified for the service
   - Replaces old `GetQualifiedStaff()` method
   - Logs staff count for debugging

2. **GenerateTimeSlotsForIndividualAsync()**
   - Generates time slots for individual provider instead of Staff entity
   - Uses `individualProvider.Id.Value` for booking queries
   - Builds staff name from `OwnerFirstName` and `OwnerLastName`
   - Falls back to `BusinessName` if name fields are empty

#### Removed/Deprecated Methods
- `GetQualifiedStaff()` - No longer used (now uses async hierarchy queries)
- `GenerateTimeSlotsForStaffAsync()` - Replaced by `GenerateTimeSlotsForIndividualAsync()`

---

### 3. ✅ GetAvailableSlotsQueryHandler (Application Layer)

**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsQueryHandler.cs`

**Changes**:

#### Added Using Statements
```csharp
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
```

#### Staff Loading Logic (Lines 54-79)
**Before**:
```csharp
Domain.Entities.Staff? staff = null;
if (request.StaffId.HasValue)
{
    staff = provider.Staff.FirstOrDefault(s => s.Id == request.StaffId.Value);
    if (staff == null)
        throw new NotFoundException(...);
}
```

**After**:
```csharp
Provider? individualProvider = null;
if (request.StaffId.HasValue)
{
    var staffProviderId = ProviderId.From(request.StaffId.Value);

    // Load the individual provider
    individualProvider = await _providerRepository.GetByIdAsync(
        staffProviderId,
        cancellationToken);

    if (individualProvider == null)
        throw new NotFoundException(...);

    // Verify they belong to this organization
    if (individualProvider.ParentProviderId != provider.Id)
        throw new NotFoundException(...);

    // Verify they are actually an individual
    if (individualProvider.HierarchyType != ProviderHierarchyType.Individual)
        throw new NotFoundException(...);
}
```

**Benefits**:
- ✅ Validates hierarchy relationship (ParentProviderId)
- ✅ Ensures provider is an Individual (not Organization)
- ✅ Uses async repository loading instead of in-memory collection
- ✅ Better error messages for hierarchy violations

#### Available Slots Call (Line 87-92)
**Before**:
```csharp
var availableSlots = await _availabilityService.GetAvailableTimeSlotsAsync(
    provider,
    service,
    request.Date,
    staff,
    cancellationToken);
```

**After**:
```csharp
var availableSlots = await _availabilityService.GetAvailableTimeSlotsAsync(
    provider,
    service,
    request.Date,
    individualProvider, // Now using Provider hierarchy
    cancellationToken);
```

#### Staff Count Check (Lines 131-143)
**Before**:
```csharp
if (!provider.Staff.Any())
{
    validationMessages.Add("این ارائه‌دهنده هنوز کارمندی اضافه نکرده است...");
}
```

**After**:
```csharp
var staffCount = await _providerRepository.CountStaffByOrganizationAsync(
    provider.Id,
    cancellationToken);

if (staffCount == 0)
{
    validationMessages.Add("این ارائه‌دهنده هنوز کارمندی اضافه نکرده است...");
}
```

**Benefits**:
- ✅ Uses hierarchy repository method
- ✅ Accurate count of individual providers in organization
- ✅ More efficient than loading full staff list

---

## Architecture Alignment

### Before Migration
```
Organization Provider
└── Staff[] (Legacy Entity)
    ├── Staff.Id (Guid)
    ├── Staff.FullName (string)
    └── Staff.IsActive (bool)
```

### After Migration
```
Organization Provider (HierarchyType = Organization)
└── Individual Providers (loaded via hierarchy repository)
    ├── Provider.Id (ProviderId)
    ├── Provider.HierarchyType = Individual
    ├── Provider.ParentProviderId = OrganizationId
    ├── Provider.OwnerFirstName + OwnerLastName
    └── Provider.Status (ProviderStatus.Active)
```

---

## Repository Methods Used

### IProviderReadRepository
1. `GetByIdAsync(ProviderId, CancellationToken)` - Load individual provider
2. `GetStaffByOrganizationIdAsync(ProviderId, CancellationToken)` - Load all staff
3. `CountStaffByOrganizationAsync(ProviderId, CancellationToken)` - Count staff

These methods were already implemented as part of the hierarchy feature (100% MVP complete).

---

## Validation Improvements

### Hierarchy Relationship Validation
```csharp
// Verify staff belongs to organization
if (individualProvider.ParentProviderId != provider.Id)
    throw new NotFoundException(...);

// Verify staff is an individual (not organization)
if (individualProvider.HierarchyType != ProviderHierarchyType.Individual)
    throw new NotFoundException(...);
```

### Staff Qualification Check
```csharp
// Check if individual provider is qualified for service
if (!service.IsStaffQualified(individualProvider.Id.Value))
{
    _logger.LogWarning("Individual Provider {ProviderId} is not qualified...");
    return false;
}
```

---

## Benefits of This Migration

### 1. **Consistency with Architecture**
- ✅ Aligns with Provider Hierarchy model
- ✅ Staff are now first-class Provider entities
- ✅ Supports organization structure properly

### 2. **Better Data Integrity**
- ✅ Validates Parent-Child relationship
- ✅ Enforces hierarchy type constraints
- ✅ Prevents orphaned staff references

### 3. **Improved Scalability**
- ✅ Can support nested hierarchies in future
- ✅ Staff have full Provider capabilities
- ✅ Easier to add staff-specific features

### 4. **Enhanced Functionality**
- ✅ Individual providers can have their own services
- ✅ Individual providers can have their own schedules
- ✅ Better support for multi-location organizations

---

## Testing Checklist

### Unit Tests Needed
- [ ] Test `GetQualifiedIndividualProvidersAsync()` with various scenarios
- [ ] Test `GenerateTimeSlotsForIndividualAsync()` with individual providers
- [ ] Test hierarchy validation in GetAvailableSlotsQueryHandler
- [ ] Test staff count check with hierarchy repository

### Integration Tests Needed
- [ ] Test booking with organization (no staff selected)
- [ ] Test booking with specific individual provider
- [ ] Test validation when individual provider doesn't belong to organization
- [ ] Test validation when provider is not an individual
- [ ] Test error message when organization has no staff
- [ ] Test error message when no qualified staff available

### End-to-End Tests
- [ ] Complete booking flow with hierarchy
- [ ] Staff selection in booking UI
- [ ] Multiple staff members with different qualifications
- [ ] Solo organization vs multi-staff organization

---

## Backward Compatibility

### Breaking Changes
⚠️ **This is a breaking change** - The Staff entity is no longer used in availability queries.

### Migration Required
- Existing staff data must be migrated to Individual Providers
- Database migration already created: `20251122131949_AddProviderHierarchy`
- All staff records should have `HierarchyType = Individual` and `ParentProviderId` set

### API Compatibility
- ✅ API endpoints remain unchanged (still use `staffId` parameter)
- ✅ Frontend can continue using existing booking flow
- ✅ DTOs remain compatible (TimeSlotDto uses Guid for StaffId)

---

## Performance Considerations

### Database Queries
1. **Loading Individual Provider**: 1 query
   ```sql
   SELECT * FROM Providers WHERE Id = @staffProviderId
   ```

2. **Loading Staff Members**: 1 query
   ```sql
   SELECT * FROM Providers WHERE ParentProviderId = @organizationId
   ```

3. **Counting Staff**: 1 query
   ```sql
   SELECT COUNT(*) FROM Providers WHERE ParentProviderId = @organizationId
   ```

### Optimization Opportunities
- ✅ Can add eager loading for common scenarios
- ✅ Can cache staff lists per organization
- ✅ Indexes already exist on `ParentProviderId` and `HierarchyType`

---

## Files Modified

### Domain Layer
1. `IAvailabilityService.cs` - Interface signature updates

### Application Layer
2. `AvailabilityService.cs` - Implementation with hierarchy
3. `GetAvailableSlotsQueryHandler.cs` - Query handler with validation

### Documentation
4. `AVAILABLE_SLOTS_HIERARCHY_UPDATE.md` - Migration guide
5. `HIERARCHY_MIGRATION_COMPLETED.md` - This summary

---

## Related Features

### Already Implemented
- ✅ Provider Hierarchy domain model
- ✅ GetStaffMembers query (hierarchy example)
- ✅ Organization & Individual registration
- ✅ Invitation & Join Request workflows
- ✅ Database migrations

### Still Using Legacy Staff Model
⚠️ **Other components may still reference Staff entity**:
- Booking creation/confirmation
- Staff profile management
- Service assignment to staff
- Earnings/commission calculations

**Recommendation**: Gradually migrate these as separate tasks.

---

## Next Steps

### Immediate (Required for deployment)
1. ✅ **Code complete** - All changes implemented
2. ⚠️ **Run database migration** - Ensure hierarchy fields exist
3. ⚠️ **Unit tests** - Add tests for new methods
4. ⚠️ **Integration tests** - Test end-to-end booking flow
5. ⚠️ **Build and verify** - Ensure no compilation errors

### Short Term (1-2 weeks)
1. Migrate booking creation to use individual provider ID
2. Update booking confirmation to reference individual providers
3. Add individual provider validation to other booking endpoints
4. Update staff management UI to use hierarchy

### Long Term (1-2 months)
1. Remove Staff entity completely
2. Migrate all legacy references to hierarchy model
3. Add advanced hierarchy features (nested organizations, etc.)
4. Performance optimization and caching

---

## Success Criteria

✅ **Implementation Complete**
- [x] Interface signatures updated
- [x] Service implementation migrated
- [x] Query handler uses hierarchy
- [x] Validation logic enhanced
- [x] Helper methods implemented
- [x] RescheduleBookingCommandHandler updated
- [x] Namespace conflicts resolved
- [x] Code compiles successfully (0 errors)
- [x] Database migrations applied

⏳ **Testing Required**
- [ ] Unit tests for new methods
- [ ] Integration tests added
- [ ] End-to-end verification
- [ ] Performance testing

⏳ **Deployment Ready**
- [ ] Code review completed
- [ ] All tests passing
- [ ] Performance verified
- [x] Documentation updated

---

## Rollback Plan

If issues arise after deployment:

1. **Code Rollback**: Revert commits to restore Staff entity usage
2. **Database**: Hierarchy fields can remain (backward compatible)
3. **Feature Flag**: Can add flag to switch between models
4. **Gradual Migration**: Can run both models in parallel temporarily

---

## Contact & Support

**Implementation**: AI Assistant
**Date**: 2025-12-03
**Status**: ✅ Code Complete, ⏳ Testing Required

For questions or issues:
- Review this document
- Check [AVAILABLE_SLOTS_HIERARCHY_UPDATE.md](./AVAILABLE_SLOTS_HIERARCHY_UPDATE.md)
- Refer to [HIERARCHY_API_STATUS.md](../booksy-frontend/HIERARCHY_API_STATUS.md)

---

**Last Updated**: 2025-12-03
**Version**: 1.1
**Migration Status**: Implementation Complete ✅ | Build Success ✅ | Database Updated ✅ | Ready for Testing ⚠️
