# Staff Members Display Fix

## Problem
Staff members were showing as "undefined undefined" with wrong dates (۱۱ دی ‎−۶۲۱) and the "مشاهده پروفایل" button was not working.

## Root Cause
The backend `GetStaffMembersQuery` was returning incomplete data with only 4 fields:
- `providerId`
- `businessName`
- `status`
- `joinedAt` (with default value `0001-01-01T00:00:00`)

The frontend expected 16+ fields including `firstName`, `lastName`, `fullName`, `email`, `phone`, etc.

## Solution

### 1. Updated Backend DTO (StaffMemberDto)

**File:** `GetStaffMembersQuery.cs`

Added all required fields to match frontend expectations:

```csharp
public sealed record StaffMemberDto(
    Guid Id,
    Guid ProviderId,
    Guid OrganizationId,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? PhoneNumber,
    string? PhotoUrl,
    string Role,
    string? Title,
    string? Bio,
    IReadOnlyList<string>? Specializations,
    bool IsActive,
    DateTime JoinedAt,
    DateTime? LeftAt);
```

### 2. Updated Query Handler Mapping

**File:** `GetStaffMembersQueryHandler.cs`

Updated the mapping logic to populate all fields from the Provider aggregate:

```csharp
var staffDtos = staffMembers.Select(s =>
{
    var fullName = $"{s.OwnerFirstName} {s.OwnerLastName}".Trim();
    // Use LastModifiedAt if available (when they joined), otherwise RegisteredAt
    var joinedAt = s.LastModifiedAt ?? s.RegisteredAt;

    return new StaffMemberDto(
        Id: s.Id.Value,
        ProviderId: s.Id.Value,
        OrganizationId: organizationId.Value,
        FirstName: s.OwnerFirstName,
        LastName: s.OwnerLastName,
        FullName: string.IsNullOrEmpty(fullName) ? s.Profile.BusinessName : fullName,
        Email: s.ContactInfo.Email?.Value,
        PhoneNumber: s.ContactInfo.PrimaryPhone?.Value,
        PhotoUrl: s.Profile.ProfileImageUrl,
        Role: "Staff",
        Title: null,
        Bio: s.Profile.BusinessDescription,
        Specializations: null,
        IsActive: s.Status == ProviderStatus.Active,
        JoinedAt: joinedAt,
        LeftAt: null);
}).ToList();
```

### 3. Fixed View Profile Button

**File:** `StaffManagementDashboard.vue`

Implemented navigation to staff member's provider profile:

```typescript
function viewStaffDetails(staff: StaffMember): void {
  // Navigate to the staff member's provider profile page
  window.open(`/provider/${staff.providerId}`, '_blank')
}
```

## Field Mapping Details

| Frontend Field | Backend Source | Notes |
|---------------|----------------|-------|
| `id` | `s.Id.Value` | Provider ID |
| `providerId` | `s.Id.Value` | Same as ID |
| `organizationId` | `organizationId.Value` | From query parameter |
| `firstName` | `s.OwnerFirstName` | Provider owner's first name |
| `lastName` | `s.OwnerLastName` | Provider owner's last name |
| `fullName` | Calculated | `firstName + lastName`, fallback to `BusinessName` |
| `email` | `s.ContactInfo.Email?.Value` | Contact email |
| `phoneNumber` | `s.ContactInfo.PrimaryPhone?.Value` | Primary phone |
| `photoUrl` | `s.Profile.ProfileImageUrl` | Profile image |
| `role` | `"Staff"` | Hard-coded for now |
| `title` | `null` | Not stored in Provider aggregate |
| `bio` | `s.Profile.BusinessDescription` | Business description |
| `specializations` | `null` | Not stored in Provider aggregate |
| `isActive` | `s.Status == ProviderStatus.Active` | Active status |
| `joinedAt` | `s.LastModifiedAt ?? s.RegisteredAt` | Best available date |
| `leftAt` | `null` | Not tracked currently |

## Date Fix

The `joinedAt` date was showing `0001-01-01T00:00:00` (default DateTime value) because we were using `s.CreatedAt` which might not be set.

**Solution:** Use `LastModifiedAt` (when they were linked to organization) with fallback to `RegisteredAt`:

```csharp
var joinedAt = s.LastModifiedAt ?? s.RegisteredAt;
```

## Testing Steps

1. **Restart the backend API** to apply changes
2. **Clear browser cache** or wait for cache TTL (5 minutes)
3. Navigate to Staff Management page
4. Verify:
   - ✅ Staff member names display correctly (e.g., "مصطفی کاظمی")
   - ✅ Joined dates show actual dates (not ۱۱ دی ‎−۶۲۱)
   - ✅ "مشاهده پروفایل" button opens provider profile in new tab
   - ✅ Email and phone display if available
   - ✅ Active/Inactive status shows correctly

## Future Enhancements

1. **Add dedicated join date tracking** - Store when staff member was linked to organization
2. **Add role/title fields** - Allow setting custom roles and titles for staff members
3. **Add specializations** - Track staff member specialties/skills
4. **Track leftAt date** - Record when staff members leave the organization

## Files Modified

### Backend
- `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/ProviderHierarchy/GetStaffMembers/GetStaffMembersQuery.cs`
- `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/ProviderHierarchy/GetStaffMembers/GetStaffMembersQueryHandler.cs`

### Frontend
- `booksy-frontend/src/modules/provider/components/staff/StaffManagementDashboard.vue`

## Build Status
✅ Backend builds successfully with 0 errors, 31 warnings (pre-existing)
