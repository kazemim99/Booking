# GetAvailableSlotsQueryHandler - Hierarchy Migration Update

**Date**: 2025-12-03
**Status**: ✅ COMPLETED
**Priority**: High

---

## Overview

The `GetAvailableSlotsQueryHandler` currently uses the **legacy Staff entity model** and needs to be updated to use the new **Provider Hierarchy model** where staff members are now **Individual Providers** within an Organization.

---

## Current Implementation Issue

### Problem Code (Lines 55-61)

```csharp
// Get specific staff if requested
Domain.Entities.Staff? staff = null;
if (request.StaffId.HasValue)
{
    staff = provider.Staff.FirstOrDefault(s => s.Id == request.StaffId.Value);
    if (staff == null)
        throw new NotFoundException($"Staff member with ID {request.StaffId.Value} not found");
}
```

**Issues:**
1. ❌ Uses `Domain.Entities.Staff` which is the old model
2. ❌ Accesses `provider.Staff` collection (legacy)
3. ❌ Doesn't support the new hierarchy where staff = Individual Providers

---

## Provider Hierarchy Model

### Key Changes

With the Provider Hierarchy implementation:

1. **Staff are now Individual Providers**
   - Each staff member is a full `Provider` entity
   - They have `HierarchyType = ProviderHierarchyType.Individual`
   - They are linked to organization via `ParentProviderId`

2. **Organization Structure**
   ```
   Organization Provider (HierarchyType = Organization)
   ├── Individual Provider 1 (ParentProviderId = OrganizationId)
   ├── Individual Provider 2 (ParentProviderId = OrganizationId)
   └── Individual Provider 3 (ParentProviderId = OrganizationId)
   ```

3. **Repository Support**
   - `GetStaffByOrganizationIdAsync(ProviderId organizationId)` - Get all staff for an organization
   - `GetOrganizationByStaffIdAsync(ProviderId staffProviderId)` - Get parent organization

---

## Proposed Solution

### Updated Implementation

```csharp
// Get specific individual provider (staff) if requested
Provider? individualProvider = null;
if (request.StaffId.HasValue)
{
    var staffProviderId = ProviderId.From(request.StaffId.Value);

    // Load the individual provider
    individualProvider = await _providerRepository.GetByIdAsync(
        staffProviderId,
        cancellationToken);

    if (individualProvider == null)
        throw new NotFoundException($"Individual provider with ID {request.StaffId.Value} not found");

    // Verify they belong to this organization
    if (individualProvider.ParentProviderId != provider.Id)
        throw new DomainValidationException(
            $"Individual provider {request.StaffId.Value} does not belong to organization {request.ProviderId}");

    // Verify they are actually an individual (not an organization)
    if (individualProvider.HierarchyType != ProviderHierarchyType.Individual)
        throw new DomainValidationException(
            $"Provider {request.StaffId.Value} is not an individual provider");
}
```

### Alternative: Load All Staff

If you need to get all staff members for the organization:

```csharp
// Get all staff members for this organization
var staffMembers = await _providerRepository.GetStaffByOrganizationIdAsync(
    provider.Id,
    cancellationToken);

// Filter for specific staff if requested
Provider? selectedStaff = null;
if (request.StaffId.HasValue)
{
    selectedStaff = staffMembers.FirstOrDefault(s => s.Id.Value == request.StaffId.Value);
    if (selectedStaff == null)
        throw new NotFoundException(
            $"Individual provider with ID {request.StaffId.Value} not found in organization {request.ProviderId}");
}
```

---

## Updated GetAvailableSlotsQueryHandler

### Complete Implementation

```csharp
public async Task<GetAvailableSlotsResult> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
{
    _logger.LogInformation(
        "Getting available slots for Provider {ProviderId}, Service {ServiceId} on {Date}",
        request.ProviderId, request.ServiceId, request.Date);

    // Load provider (organization)
    var provider = await _providerRepository.GetByIdAsync(
        ProviderId.From(request.ProviderId),
        cancellationToken);

    if (provider == null)
        throw new NotFoundException($"Provider with ID {request.ProviderId} not found");

    // Load service
    var service = await _serviceRepository.GetByIdAsync(
        ServiceId.From(request.ServiceId),
        cancellationToken);

    if (service == null)
        throw new NotFoundException($"Service with ID {request.ServiceId} not found");

    // Get specific individual provider (staff) if requested
    Provider? individualProvider = null;
    if (request.StaffId.HasValue)
    {
        var staffProviderId = ProviderId.From(request.StaffId.Value);

        // Load the individual provider
        individualProvider = await _providerRepository.GetByIdAsync(
            staffProviderId,
            cancellationToken);

        if (individualProvider == null)
            throw new NotFoundException($"Individual provider with ID {request.StaffId.Value} not found");

        // Verify they belong to this organization
        if (individualProvider.ParentProviderId != provider.Id)
            throw new DomainValidationException(
                $"Individual provider {request.StaffId.Value} does not belong to organization {request.ProviderId}");

        // Verify they are actually an individual (not an organization)
        if (individualProvider.HierarchyType != ProviderHierarchyType.Individual)
            throw new DomainValidationException(
                $"Provider {request.StaffId.Value} is not an individual provider");
    }

    // Validate date-level constraints
    var validationResult = await _availabilityService.ValidateDateConstraintsAsync(
        provider,
        service,
        request.Date,
        cancellationToken);

    // Get available slots - pass the individual provider instead of old Staff entity
    var availableSlots = await _availabilityService.GetAvailableTimeSlotsAsync(
        provider,
        service,
        request.Date,
        individualProvider, // Pass Provider instead of Staff
        cancellationToken);

    // Map to DTOs
    var slotDtos = availableSlots
        .Select(slot => new TimeSlotDto(
            slot.StartTime,
            slot.EndTime,
            slot.Duration.Value,
            slot.StaffId,
            slot.StaffName)
        {
            IsAvailable = true,
            AvailableStaffId = slot.StaffId,
            AvailableStaffName = slot.StaffName
        })
        .ToList();

    _logger.LogInformation("Found {Count} available slots", slotDtos.Count);

    // Include validation messages if no slots are available
    List<string>? validationMessages = null;
    if (slotDtos.Count == 0)
    {
        if (!validationResult.IsValid)
        {
            validationMessages = validationResult.Errors;
            _logger.LogInformation(
                "No slots available due to validation constraints: {ValidationErrors}",
                string.Join(", ", validationMessages));
        }
        else
        {
            validationMessages = new List<string>();

            // Check if organization has staff (individual providers)
            var staffCount = await _providerRepository.CountStaffByOrganizationAsync(
                provider.Id,
                cancellationToken);

            if (staffCount == 0)
            {
                validationMessages.Add("این ارائه‌دهنده هنوز کارمندی اضافه نکرده است. لطفاً بعداً دوباره تلاش کنید.");
            }
            else
            {
                validationMessages.Add("متأسفانه هیچ کارمند واجد شرایطی برای این سرویس در دسترس نیست.");
            }

            _logger.LogInformation(
                "No slots available: {Reason}",
                string.Join(", ", validationMessages));
        }
    }

    return new GetAvailableSlotsResult(
        request.ProviderId,
        request.ServiceId,
        request.Date,
        slotDtos,
        validationMessages);
}
```

---

## IAvailabilityService Update Needed

⚠️ **IMPORTANT**: The `IAvailabilityService.GetAvailableTimeSlotsAsync` method signature also needs to be updated:

### Current Signature
```csharp
Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(
    Provider provider,
    Service service,
    DateTime date,
    Domain.Entities.Staff? staff, // OLD
    CancellationToken cancellationToken);
```

### Updated Signature
```csharp
Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(
    Provider provider,
    Service service,
    DateTime date,
    Provider? individualProvider, // NEW - Individual Provider instead of Staff
    CancellationToken cancellationToken);
```

---

## Required Changes Summary

### 1. GetAvailableSlotsQueryHandler.cs ✅ COMPLETED
- ✅ Replaced `Domain.Entities.Staff` with `ProviderAggregate` (using alias)
- ✅ Uses `_providerRepository.GetByIdAsync()` to load individual provider
- ✅ Added validation for hierarchy relationship (ParentProviderId)
- ✅ Updated staff count check to use `CountStaffByOrganizationAsync()`
- ✅ Added namespace alias to resolve conflicts

### 2. IAvailabilityService.cs ✅ COMPLETED
- ✅ Updated `GetAvailableTimeSlotsAsync()` signature to accept `Provider?` instead of `Staff?`
- ✅ Updated `IsTimeSlotAvailableAsync()` to accept `Provider` instead of `Staff`
- ✅ Updated `GetAvailableStaffAsync()` to return `Task<IReadOnlyList<Provider>>`

### 3. AvailabilityService.cs (Implementation) ✅ COMPLETED
- ✅ Added `IProviderReadRepository` dependency
- ✅ Updated implementation to work with `Provider` entities
- ✅ Implemented `GetQualifiedIndividualProvidersAsync()` method
- ✅ Implemented `GenerateTimeSlotsForIndividualAsync()` method
- ✅ Updated all Staff-specific logic to use Provider properties

### 4. RescheduleBookingCommandHandler.cs ✅ COMPLETED
- ✅ Updated to load individual provider via repository
- ✅ Added hierarchy validation
- ✅ Uses namespace alias for Provider type

### 5. Build & Database ✅ COMPLETED
- ✅ All code compiles successfully (0 errors, only warnings)
- ✅ All database migrations applied
- ✅ Provider Hierarchy tables exist and ready

---

## Migration Strategy

### Phase 1: Backward Compatibility (Optional)
If you need to support both models temporarily:

```csharp
// Support both Staff and Individual Provider
Provider? individualProvider = null;
if (request.StaffId.HasValue)
{
    // Try new hierarchy model first
    individualProvider = await TryLoadIndividualProviderAsync(request.StaffId.Value);

    if (individualProvider == null)
    {
        // Fall back to legacy Staff model
        staff = provider.Staff.FirstOrDefault(s => s.Id == request.StaffId.Value);
    }
}
```

### Phase 2: Full Migration
- Remove all `Staff` entity references
- Update all services to use `Provider` hierarchy
- Update database to migrate Staff → Individual Providers
- Remove legacy Staff table

---

## Testing Checklist

### Code Implementation ✅
- [x] Code compiles without errors
- [x] All dependencies injected correctly
- [x] Namespace conflicts resolved
- [x] Database migrations applied

### Testing Required ⚠️
- [ ] Test booking with organization (no staff selected)
- [ ] Test booking with specific individual provider
- [ ] Test validation when individual provider doesn't belong to organization
- [ ] Test validation when provider is not an individual
- [ ] Test error message when organization has no staff
- [ ] Test error message when no qualified staff available
- [ ] Load test hierarchy queries performance
- [ ] Integration test with GetStaffMembersQuery
- [ ] Unit tests for new methods

---

## Related Files

### Backend Files to Update
1. `GetAvailableSlotsQueryHandler.cs` ⚠️ High Priority
2. `IAvailabilityService.cs` ⚠️ High Priority
3. `AvailabilityService.cs` ⚠️ High Priority
4. `TimeSlot.cs` (if it references Staff)

### Documentation to Update
1. `STAFF_COMPONENTS_CLEANUP.md` ✅ Already mentions hierarchy migration
2. API documentation (Swagger)
3. Booking flow documentation

---

## References

- **Hierarchy Implementation**: [HIERARCHY_API_STATUS.md](../booksy-frontend/HIERARCHY_API_STATUS.md)
- **Provider Hierarchy Proposal**: [openspec/changes/add-provider-hierarchy/README.md](../openspec/changes/add-provider-hierarchy/README.md)
- **GetStaffMembersQueryHandler**: Example of hierarchy usage
- **IProviderReadRepository**: Has all hierarchy query methods

---

## Implementation Status

### ✅ Completed (2025-12-03)
1. ✅ **Code Implementation** - All changes implemented
2. ✅ **IAvailabilityService** - Interface updated
3. ✅ **AvailabilityService** - Implementation updated with hierarchy
4. ✅ **GetAvailableSlotsQueryHandler** - Updated to use hierarchy
5. ✅ **RescheduleBookingCommandHandler** - Updated to use hierarchy
6. ✅ **Build Verification** - Compiles successfully
7. ✅ **Database Migration** - All migrations applied

### ⚠️ Next Steps (Testing Phase)
1. ⚠️ **Unit Tests** - Add tests for new hierarchy methods
2. ⚠️ **Integration Tests** - Test booking flow end-to-end
3. ⚠️ **Manual Testing** - Verify in development environment
4. ⚠️ **Performance Testing** - Load test hierarchy queries
5. ⚠️ **API Documentation** - Update Swagger docs
6. 📋 **Plan Migration** - Migrate remaining Staff entity usages

---

**Last Updated**: 2025-12-03
**Author**: AI Assistant
**Reviewers**: [To be assigned]
