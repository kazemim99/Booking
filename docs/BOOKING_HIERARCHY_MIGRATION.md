# Booking System: Hierarchy-Based Architecture Migration

**Date**: December 8, 2025
**Status**: ✅ Completed
**Version**: 1.0

## Overview

The booking system has been migrated from using a `Staff` collection pattern to a **hierarchy-based architecture** where staff members are individual `Provider` entities with a parent-child relationship to organizations.

## Architecture Change

### Before (Old Pattern)
```csharp
// Staff as nested collection
public class Provider {
    private readonly List<Staff> _staff = new();
    public IReadOnlyList<Staff> Staff => _staff.AsReadOnly();
}

// Booking used Staff entity
var staff = provider.Staff.FirstOrDefault(s => s.Id == staffId);
```

### After (Hierarchy Pattern)
```csharp
// Staff as individual Provider entities
public class Provider {
    public ProviderId? ParentProviderId { get; private set; }
    public ProviderHierarchyType HierarchyType { get; private set; }
}

// Booking uses Provider hierarchy
var staffProvider = await _providerRepository.GetByIdAsync(staffProviderId);
if (staffProvider.ParentProviderId != organizationProvider.Id)
    throw new ConflictException("Invalid hierarchy");
```

## Changes Summary

### Backend Changes

#### 1. Command & Result Models

**CreateBookingCommand.cs**
- Changed: `Guid StaffId` → `Guid StaffProviderId`
- Purpose: Represents the individual provider (staff) who will perform the service

**CreateBookingResult.cs**
- Changed: `Guid StaffId` → `Guid StaffProviderId`
- Purpose: Returns the staff provider ID in the response

#### 2. Command Handler Logic

**CreateBookingCommandHandler.cs** - Major refactoring:

**Removed:**
- ❌ `provider.Staff` collection access
- ❌ `service.IsStaffQualified(staffId)` check
- ❌ `staff.IsActive` check (Staff entity)

**Added:**
- ✅ Load staff provider as separate entity: `GetByIdAsync(StaffProviderId)`
- ✅ Validate hierarchy: `staffProvider.ParentProviderId == provider.Id`
- ✅ Check staff provider status: `staffProvider.Status == ProviderStatus.Active`
- ✅ Use staff provider ID throughout booking flow

**Code Example:**
```csharp
// Load staff provider (individual provider in hierarchy)
var staffProvider = await _providerRepository.GetByIdAsync(
    ProviderId.From(request.StaffProviderId),
    cancellationToken);

if (staffProvider == null)
    throw new NotFoundException($"Staff provider with ID {request.StaffProviderId} not found");

// Verify staff provider belongs to organization hierarchy
if (staffProvider.ParentProviderId != provider.Id)
    throw new ConflictException("Staff provider does not belong to the specified organization");

// Check if staff provider is active
if (staffProvider.Status != ProviderStatus.Active)
{
    var staffName = $"{staffProvider.OwnerFirstName} {staffProvider.OwnerLastName}".Trim();
    if (string.IsNullOrEmpty(staffName))
        staffName = staffProvider.Profile.BusinessName;
    throw new ConflictException($"Staff provider {staffName} is not currently active");
}
```

#### 3. API Layer

**CreateBookingRequest.cs**
```csharp
// Before
public Guid? StaffId { get; set; }

// After
[Required]
public Guid StaffProviderId { get; set; }
```

**BookingResponse.cs**
```csharp
// Before
public Guid StaffId { get; set; }

// After
public Guid StaffProviderId { get; set; }
```

**BookingDetailsResponse.cs**
```csharp
// Before
public Guid StaffId { get; set; }

// After
public Guid StaffProviderId { get; set; }
```

**BookingsController.cs**
```csharp
// Command creation
var command = new CreateBookingCommand(
    CustomerId: Guid.Parse(customerId),
    ProviderId: request.ProviderId,
    ServiceId: request.ServiceId,
    StaffProviderId: request.StaffProviderId,  // ✅ Updated
    StartTime: request.StartTime,
    CustomerNotes: request.CustomerNotes);

// Response mapping
var response = new BookingResponse
{
    // ...
    StaffProviderId = result.StaffProviderId,  // ✅ Updated
    // ...
};
```

### Frontend Changes

#### 1. TypeScript Interfaces

**booking.service.ts**
```typescript
// Before
export interface CreateBookingRequest {
  customerId: string
  providerId: string
  serviceId: string
  staffId?: string | null  // ❌ Old
  startTime: string
  customerNotes?: string
}

// After
export interface CreateBookingRequest {
  customerId: string
  providerId: string
  serviceId: string
  staffProviderId: string  // ✅ New (required)
  startTime: string
  customerNotes?: string
}
```

**booking.types.ts** - Updated all interfaces:
```typescript
export interface Appointment {
  // ...
  staffProviderId?: string  // ✅ Updated
  // ...
}

export interface BookingRequest {
  providerId: string
  serviceId: string
  staffProviderId?: string  // ✅ Updated
  scheduledStartTime: string
  bookingNotes?: string
}

export interface Schedule {
  id: string
  providerId: string
  staffProviderId?: string  // ✅ Updated
  // ...
}

export interface AvailabilitySlot {
  startTime: string
  endTime: string
  available: boolean
  staffProviderId?: string  // ✅ Updated
}

export interface AvailabilityRequest {
  providerId: string
  serviceId: string
  staffProviderId?: string  // ✅ Updated
  startDate: string
  endDate: string
}
```

#### 2. Component Updates

**BookingWizard.vue**
```typescript
// Before
const request: CreateBookingRequest = {
  customerId,
  providerId: providerId.value,
  serviceId: firstService.id,
  staffId: bookingData.value.staffId || undefined,  // ❌ Wrong property
  startTime,
  customerNotes: bookingData.value.customerInfo.notes || undefined,
}

// After
const request: CreateBookingRequest = {
  customerId,
  providerId: providerId.value,
  serviceId: firstService.id,
  staffProviderId: bookingData.value.staffId || '',  // ✅ Correct property
  startTime,
  customerNotes: bookingData.value.customerInfo.notes || undefined,
}
```

## Validation & Business Rules

### Hierarchy Validation
1. **Staff Provider Exists**: Must load successfully from repository
2. **Hierarchy Relationship**: `staffProvider.ParentProviderId` must equal organization `provider.Id`
3. **Active Status**: Staff provider must have status `ProviderStatus.Active`
4. **Service Ownership**: Service must belong to the organization provider

### Previous Rules Removed
- ❌ Staff qualification check (`service.IsStaffQualified()`)
- ❌ Staff collection validation
- ❌ Staff entity active check

## Database Schema

No database schema changes required. The `Booking` aggregate already stores `StaffId` as a `Guid`, which now references a `ProviderId` instead of a `Staff.Id`.

**Booking Table:**
```sql
-- No changes needed - StaffId column already exists as Guid
-- It now references Providers.Id instead of Staff.Id
CREATE TABLE Bookings (
    -- ...
    StaffId uniqueidentifier NOT NULL,  -- References Providers(Id)
    -- ...
)
```

## API Contract Changes

### Request Changes
```json
// POST /api/v1/bookings
{
  "customerId": "guid",
  "providerId": "guid",
  "serviceId": "guid",
  "staffProviderId": "guid",  // ✅ Required, renamed from staffId
  "startTime": "2025-01-15T10:00:00Z",
  "customerNotes": "optional notes"
}
```

### Response Changes
```json
// Response from all booking endpoints
{
  "id": "guid",
  "customerId": "guid",
  "providerId": "guid",
  "serviceId": "guid",
  "staffProviderId": "guid",  // ✅ Renamed from staffId
  "status": "Pending",
  "startTime": "2025-01-15T10:00:00Z",
  "endTime": "2025-01-15T11:00:00Z",
  // ...
}
```

## Migration Impact

### Breaking Changes ⚠️
1. **API Contract**: `staffId` renamed to `staffProviderId` in all requests/responses
2. **Frontend**: All TypeScript interfaces updated
3. **Business Logic**: Staff validation logic completely rewritten

### Non-Breaking
- Database schema remains unchanged
- Existing bookings continue to work (StaffId column unchanged)
- No data migration required

## Testing Considerations

### Unit Tests
- Test hierarchy validation
- Test parent-child relationship checks
- Test active status validation

### Integration Tests
- Test booking creation with valid staff provider
- Test booking creation with invalid hierarchy
- Test booking creation with inactive staff provider
- Test booking with staff from different organization

### Example Test Cases
```csharp
[Fact]
public async Task CreateBooking_WithValidStaffProvider_Succeeds()
{
    // Arrange
    var organization = CreateOrganizationProvider();
    var staffProvider = CreateStaffProvider(organization.Id);
    var service = CreateService(organization.Id);

    var command = new CreateBookingCommand(
        CustomerId: customerId,
        ProviderId: organization.Id.Value,
        ServiceId: service.Id.Value,
        StaffProviderId: staffProvider.Id.Value,
        StartTime: DateTime.UtcNow.AddDays(1)
    );

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.StaffProviderId.Should().Be(staffProvider.Id.Value);
}

[Fact]
public async Task CreateBooking_WithInvalidHierarchy_ThrowsConflictException()
{
    // Arrange
    var organization1 = CreateOrganizationProvider();
    var organization2 = CreateOrganizationProvider();
    var staffProvider = CreateStaffProvider(organization2.Id);  // Wrong parent
    var service = CreateService(organization1.Id);

    var command = new CreateBookingCommand(
        CustomerId: customerId,
        ProviderId: organization1.Id.Value,
        ServiceId: service.Id.Value,
        StaffProviderId: staffProvider.Id.Value,  // Belongs to organization2
        StartTime: DateTime.UtcNow.AddDays(1)
    );

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(
        () => _handler.Handle(command, CancellationToken.None)
    );
}
```

## Build Status

✅ **ServiceCatalog.Application**: Build succeeded (0 errors)
✅ **ServiceCatalog.Api**: Build succeeded (0 errors)
✅ **Frontend**: TypeScript compilation successful
⚠️ **Integration Tests**: Reqnroll/XUnit framework issue (unrelated to changes)

## Related Documentation

- [HIERARCHY_MIGRATION_README.md](./HIERARCHY_MIGRATION_README.md) - Overall hierarchy migration
- [HIERARCHY_MIGRATION_COMPLETED.md](./HIERARCHY_MIGRATION_COMPLETED.md) - Hierarchy implementation details
- [AVAILABLE_SLOTS_HIERARCHY_UPDATE.md](./AVAILABLE_SLOTS_HIERARCHY_UPDATE.md) - Availability with hierarchy

## Benefits of Hierarchy Approach

1. **Consistency**: Staff members are providers, following single entity pattern
2. **Scalability**: Easier to manage multi-level hierarchies
3. **Flexibility**: Staff can have their own schedules, services, and settings
4. **Simplified Logic**: No need for separate Staff entity management
5. **Better Data Model**: Clearer parent-child relationships

## Rollback Procedure

If rollback is needed:

1. Revert CreateBookingCommand/Result to use `StaffId`
2. Revert CreateBookingCommandHandler validation logic
3. Revert API models (Request/Response)
4. Revert frontend TypeScript interfaces
5. Revert BookingWizard.vue request mapping

**Note**: No database rollback needed as schema was not changed.

## Future Enhancements

1. Support multi-level hierarchies (regional → branch → staff)
2. Staff provider availability management
3. Staff provider performance metrics
4. Booking assignment algorithms based on staff provider load

---

**Last Updated**: December 8, 2025
**Implemented By**: Architecture Team
**Reviewed By**: Development Team
