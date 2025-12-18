# Provider Hierarchy Migration - Complete Guide

**Last Updated**: 2025-12-03
**Status**: ✅ Implementation Complete | ⚠️ Testing Required
**Version**: 1.0

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [What Changed](#what-changed)
3. [Implementation Status](#implementation-status)
4. [Documentation Index](#documentation-index)
5. [Quick Start Guide](#quick-start-guide)
6. [Testing Guide](#testing-guide)
7. [Troubleshooting](#troubleshooting)

---

## Overview

The Provider Hierarchy Migration updates the availability and booking system to use the **Provider Hierarchy model** instead of the legacy **Staff entity model**. Staff members are now **Individual Providers** within Organizations.

### Why This Change?

**Before (Legacy Model)**:
```
Organization Provider
└── Staff[] (simple entity)
    ├── Staff.Id
    ├── Staff.FullName
    └── Staff.IsActive
```

**After (Hierarchy Model)**:
```
Organization Provider (HierarchyType = Organization)
└── Individual Providers (HierarchyType = Individual)
    ├── Provider.Id
    ├── Provider.ParentProviderId
    ├── Provider.OwnerFirstName + OwnerLastName
    └── Provider.Status
```

### Benefits

- ✅ Staff are first-class Provider entities
- ✅ Better data integrity and validation
- ✅ Support for nested hierarchies
- ✅ Individual providers can have their own services
- ✅ Scalable architecture

---

## What Changed

### Backend Changes

#### 1. Domain Layer
- **IAvailabilityService.cs** - Interface updated to use `Provider` instead of `Staff`

#### 2. Application Layer
- **AvailabilityService.cs** - Implementation using hierarchy model
- **GetAvailableSlotsQueryHandler.cs** - Updated to load individual providers
- **RescheduleBookingCommandHandler.cs** - Updated to use hierarchy

#### 3. Infrastructure Layer
- **Database Migrations** - Provider Hierarchy tables created
  - `20251122131949_AddProviderHierarchy` - Main hierarchy migration
  - `20251122145237_AddIndividualProviderIdToBookings` - Booking support

### Frontend Changes

No frontend changes required for this migration. The API contracts remain the same.

---

## Implementation Status

### ✅ Completed (2025-12-03)

| Component | Status | Notes |
|-----------|--------|-------|
| Code Implementation | ✅ Complete | All files updated |
| IAvailabilityService | ✅ Updated | Interface signatures changed |
| AvailabilityService | ✅ Updated | New hierarchy methods added |
| GetAvailableSlotsQueryHandler | ✅ Updated | Uses hierarchy validation |
| RescheduleBookingCommandHandler | ✅ Updated | Uses hierarchy validation |
| Build Verification | ✅ Success | 0 errors, only warnings |
| Database Migrations | ✅ Applied | All migrations up to date |
| Documentation | ✅ Complete | All docs updated |

### ⚠️ Testing Phase (In Progress)

| Task | Status | Priority |
|------|--------|----------|
| Unit Tests | ⚠️ Pending | High |
| Integration Tests | ⚠️ Pending | High |
| Manual Testing | ⚠️ Pending | High |
| Performance Testing | ⚠️ Pending | Medium |
| End-to-End Tests | ⚠️ Pending | Medium |

### 📋 Deployment Phase (Future)

| Task | Status | Priority |
|------|--------|----------|
| Code Review | ⚠️ Pending | High |
| Staging Deployment | ⚠️ Pending | High |
| Production Deployment | ⚠️ Pending | High |
| Monitoring Setup | ⚠️ Pending | Medium |

---

## Documentation Index

### Primary Documentation

1. **[AVAILABLE_SLOTS_HIERARCHY_UPDATE.md](./AVAILABLE_SLOTS_HIERARCHY_UPDATE.md)**
   - Detailed migration guide
   - Code examples and proposed solutions
   - Implementation steps
   - **Status**: ✅ Updated with completion status

2. **[HIERARCHY_MIGRATION_COMPLETED.md](./HIERARCHY_MIGRATION_COMPLETED.md)**
   - Complete implementation summary
   - All files modified
   - Testing checklist
   - **Status**: ✅ Complete with latest changes

3. **[HIERARCHY_MIGRATION_BUILD_SUCCESS.md](../HIERARCHY_MIGRATION_BUILD_SUCCESS.md)**
   - Build verification results
   - Database migration status
   - Deployment checklist
   - **Status**: ✅ Build and database verified

### Related Documentation

4. **[HIERARCHY_API_STATUS.md](../booksy-frontend/HIERARCHY_API_STATUS.md)**
   - Provider Hierarchy API status (100% MVP complete)
   - Available endpoints
   - Frontend integration status

5. **[STAFF_COMPONENTS_CLEANUP.md](./STAFF_COMPONENTS_CLEANUP.md)**
   - Staff component cleanup notes
   - Legacy code references

6. **[add-provider-hierarchy/README.md](../openspec/changes/add-provider-hierarchy/README.md)**
   - Original hierarchy proposal
   - Architecture design

---

## Quick Start Guide

### For Developers

#### 1. Pull Latest Changes
```bash
git pull origin feature/ux-role-based-navigation
```

#### 2. Verify Build
```bash
dotnet build src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application
```

Expected: ✅ Build succeeded (warnings ok, 0 errors)

#### 3. Verify Database
```bash
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet ef migrations list --project ../Booksy.ServiceCatalog.Infrastructure
```

Expected: All migrations listed, including hierarchy migrations

#### 4. Update Database (if needed)
```bash
dotnet ef database update --project ../Booksy.ServiceCatalog.Infrastructure
```

Expected: "No migrations were applied. The database is already up to date."

### For Testers

#### 1. Test Booking Flow
1. Navigate to provider search
2. Select a provider
3. Select a service
4. View available time slots
5. Select a specific staff member (optional)
6. Complete booking

#### 2. Verify Staff Display
1. Check that staff members are displayed correctly
2. Verify staff names show (FirstName + LastName)
3. Confirm available slots are generated per staff

#### 3. Test Error Cases
1. Try booking with invalid staff ID
2. Try booking with staff from different organization
3. Verify error messages are clear

---

## Testing Guide

### Unit Tests to Add

#### AvailabilityService Tests
```csharp
[Fact]
public async Task GetQualifiedIndividualProvidersAsync_WithQualifiedStaff_ReturnsStaff()
{
    // Arrange: Organization with 3 staff, 2 qualified for service
    // Act: Call GetQualifiedIndividualProvidersAsync
    // Assert: Returns 2 qualified individual providers
}

[Fact]
public async Task GenerateTimeSlotsForIndividualAsync_WithAvailableProvider_ReturnsSlots()
{
    // Arrange: Individual provider with no bookings
    // Act: Generate slots for a date
    // Assert: Returns available time slots
}
```

#### GetAvailableSlotsQueryHandler Tests
```csharp
[Fact]
public async Task Handle_WithValidIndividualProvider_ReturnsSlots()
{
    // Arrange: Valid request with staffId
    // Act: Handle request
    // Assert: Returns available slots for that staff member
}

[Fact]
public async Task Handle_WithInvalidHierarchy_ThrowsNotFoundException()
{
    // Arrange: StaffId that doesn't belong to organization
    // Act & Assert: Throws NotFoundException
}
```

### Integration Tests

```csharp
[Fact]
public async Task BookingFlow_WithHierarchy_CompletesSuccessfully()
{
    // 1. Create organization provider
    // 2. Create individual provider (staff)
    // 3. Link staff to organization
    // 4. Query available slots
    // 5. Verify slots returned
    // 6. Complete booking
}
```

### Manual Testing Checklist

#### Basic Flow
- [ ] View provider list
- [ ] Select provider
- [ ] View services
- [ ] Select service
- [ ] View available slots
- [ ] Verify staff names displayed
- [ ] Select time slot
- [ ] Complete booking
- [ ] Verify booking confirmation

#### Staff Selection
- [ ] Filter by specific staff member
- [ ] Verify only that staff's slots shown
- [ ] Booking assigned to correct staff
- [ ] Staff name appears in booking details

#### Edge Cases
- [ ] Organization with no staff (error message)
- [ ] Organization with unqualified staff (no slots)
- [ ] Invalid staff ID (404 error)
- [ ] Staff from different org (validation error)
- [ ] Multiple staff with overlapping availability

#### Performance
- [ ] Load time for available slots
- [ ] Response time with 10+ staff members
- [ ] Database query count (check logs)
- [ ] Memory usage during slot generation

---

## Troubleshooting

### Build Issues

#### Error: "Provider is a namespace but is used like a type"
**Solution**: Add namespace alias in using statements
```csharp
using ProviderAggregate = Booksy.ServiceCatalog.Domain.Aggregates.Provider;
```

#### Error: "cannot convert from Staff to Provider"
**Solution**: Update method to load Provider via repository
```csharp
var individualProvider = await _providerRepository.GetByIdAsync(staffProviderId);
```

### Runtime Issues

#### No slots returned for organization
**Check**:
1. Does organization have staff? Query `CountStaffByOrganizationAsync()`
2. Are staff active? Check `Status == ProviderStatus.Active`
3. Are staff qualified? Check `IsStaffQualified(staffId)`
4. Check logs for detailed error messages

#### Staff not found error
**Check**:
1. Does staff have `ParentProviderId` set?
2. Is `ParentProviderId` correct organization?
3. Is `HierarchyType = Individual`?
4. Run query: `SELECT * FROM Providers WHERE ParentProviderId = @orgId`

### Database Issues

#### Migration not applied
```bash
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet ef database update --project ../Booksy.ServiceCatalog.Infrastructure
```

#### Check migration status
```bash
dotnet ef migrations list --project ../Booksy.ServiceCatalog.Infrastructure
```

Look for:
- ✅ `20251122131949_AddProviderHierarchy`
- ✅ `20251122145237_AddIndividualProviderIdToBookings`

---

## Code Examples

### Loading Individual Provider

```csharp
// Load individual provider with hierarchy validation
var individualProvider = await _providerRepository.GetByIdAsync(
    ProviderId.From(staffId),
    cancellationToken);

if (individualProvider == null)
    throw new NotFoundException($"Individual provider not found");

// Verify hierarchy
if (individualProvider.ParentProviderId != organizationId)
    throw new NotFoundException("Staff doesn't belong to organization");

if (individualProvider.HierarchyType != ProviderHierarchyType.Individual)
    throw new NotFoundException("Not an individual provider");
```

### Getting All Staff for Organization

```csharp
// Load all staff members
var staffMembers = await _providerRepository.GetStaffByOrganizationIdAsync(
    organizationId,
    cancellationToken);

// Filter for qualified and active
var qualifiedStaff = staffMembers
    .Where(s => s.Status == ProviderStatus.Active &&
               service.IsStaffQualified(s.Id.Value))
    .ToList();
```

### Generating Staff Name

```csharp
// Build staff name from Provider
var staffName = $"{individualProvider.OwnerFirstName} {individualProvider.OwnerLastName}".Trim();
if (string.IsNullOrEmpty(staffName))
    staffName = individualProvider.Profile.BusinessName;
```

---

## Performance Considerations

### Database Queries

Each availability request now makes:
1. **Load Organization**: 1 query
2. **Load Service**: 1 query
3. **Load Individual Provider** (if staffId specified): 1 query
4. **Load Staff Members** (if no specific staff): 1 query
5. **Load Bookings**: 1 query per staff member

### Optimization Opportunities

1. **Eager Loading**: Load related entities in single query
2. **Caching**: Cache staff lists per organization
3. **Indexes**: Already exist on `ParentProviderId` and `HierarchyType`
4. **Query Batching**: Load bookings for all staff in one query

### Expected Performance

- **Single staff request**: ~50-100ms
- **Organization with 5 staff**: ~100-200ms
- **Organization with 20 staff**: ~200-500ms

Monitor these metrics and optimize if needed.

---

## Migration Checklist

### Pre-Deployment
- [x] Code implementation complete
- [x] Code compiles successfully
- [x] Database migrations created
- [x] Database migrations tested locally
- [ ] Unit tests added
- [ ] Integration tests added
- [ ] Code review completed
- [ ] Performance testing completed

### Deployment
- [ ] Backup production database
- [ ] Deploy to staging
- [ ] Run migrations on staging
- [ ] Test on staging environment
- [ ] Deploy to production
- [ ] Run migrations on production
- [ ] Verify functionality
- [ ] Monitor error logs

### Post-Deployment
- [ ] Verify booking flow works
- [ ] Check performance metrics
- [ ] Monitor error rates
- [ ] Collect user feedback
- [ ] Document any issues
- [ ] Plan follow-up improvements

---

## Support & Contact

### Questions?
- Review documentation in `docs/` folder
- Check [HIERARCHY_API_STATUS.md](../booksy-frontend/HIERARCHY_API_STATUS.md)
- Refer to [HIERARCHY_MIGRATION_COMPLETED.md](./HIERARCHY_MIGRATION_COMPLETED.md)

### Found a Bug?
1. Check [Troubleshooting](#troubleshooting) section
2. Review error logs
3. Create detailed bug report with:
   - Steps to reproduce
   - Expected behavior
   - Actual behavior
   - Error messages
   - Screenshots (if applicable)

### Need Help?
Contact the development team with:
- Description of issue
- What you've tried
- Relevant log excerpts
- Environment details

---

## Summary

✅ **Implementation**: Complete
✅ **Build**: Success
✅ **Database**: Updated
⚠️ **Testing**: Required
📋 **Deployment**: Pending

The Provider Hierarchy migration is **code complete** and ready for the testing phase. All backend changes have been implemented, built successfully, and database migrations are applied.

**Next immediate steps**:
1. Run existing tests to verify no regressions
2. Add unit tests for new hierarchy methods
3. Perform manual testing of booking flow
4. Conduct integration testing
5. Prepare for staging deployment

---

**Last Updated**: 2025-12-03
**Maintained By**: Development Team
**Version**: 1.0
