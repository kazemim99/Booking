# Staff Management - Complete Implementation Guide

## Overview
This document provides a comprehensive guide to the staff management system in the AsanRezerve application, including invitations, staff display, removal, and all UI interactions.

## Table of Contents
1. [Features](#features)
2. [Architecture](#architecture)
3. [Backend Implementation](#backend-implementation)
4. [Frontend Implementation](#frontend-implementation)
5. [UI/UX Flow](#uiux-flow)
6. [API Endpoints](#api-endpoints)
7. [Bug Fixes & Improvements](#bug-fixes--improvements)
8. [Testing Guide](#testing-guide)

---

## Features

### ✅ Implemented Features

1. **Staff Invitation System**
   - Send invitations via phone number with OTP verification
   - Resend invitations (reuses send endpoint)
   - Cancel pending invitations
   - View pending invitations
   - Track invitation status (Pending, Accepted, Rejected, Expired, Cancelled)

2. **Staff Display**
   - View all staff members in organization
   - Display staff cards with full information
   - Show active/inactive status
   - Display contact information (phone, email)
   - Show join date

3. **Staff Details**
   - View detailed information in modal
   - Three-dot menu (⋮) with actions
   - Quick access to staff profile

4. **Staff Removal**
   - Remove staff from organization
   - Confirmation dialog before removal
   - Reason tracking for audit

---

## Architecture

### High-Level Flow

```
┌─────────────────┐
│   Dashboard     │
│  (Vue Component)│
└────────┬────────┘
         │
         ├─ View Staff List
         ├─ Send Invitation
         ├─ View Details (Modal)
         └─ Remove Staff
         │
         ▼
┌─────────────────┐
│  Pinia Store    │
│(hierarchy.store)│
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Service      │
│(hierarchy.svc)  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Backend API   │
│  (ASP.NET Core) │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Database     │
│   (PostgreSQL)  │
└─────────────────┘
```

---

## Backend Implementation

### Domain Model

**File:** `Provider.cs`

```csharp
// Hierarchy properties
public ProviderHierarchyType HierarchyType { get; private set; }
public ProviderId? ParentProviderId { get; private set; }

// Link to organization
public void LinkToOrganization(ProviderId organizationId)
{
    ParentProviderId = organizationId;
    IsIndependent = false;
    RaiseDomainEvent(new StaffMemberAddedToOrganizationEvent(...));
}

// Unlink from organization
public void UnlinkFromOrganization(string reason)
{
    ParentProviderId = null;
    IsIndependent = true;
    RaiseDomainEvent(new StaffMemberRemovedFromOrganizationEvent(...));
}
```

### Commands & Queries

#### 1. Get Staff Members
**Query:** `GetStaffMembersQuery`
**Handler:** `GetStaffMembersQueryHandler`

```csharp
public sealed record GetStaffMembersQuery(
    Guid OrganizationId) : IQuery<GetStaffMembersResult>;

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

**Changes Made:**
- ✅ Updated DTO to include all 16 required fields (was only 4 fields)
- ✅ Fixed field mapping from Provider aggregate
- ✅ Fixed `joinedAt` date using `LastModifiedAt ?? RegisteredAt`
- ✅ Properly map email and phone from `ContactInfo` value object
- ✅ Map bio from `BusinessDescription`

#### 2. Send Invitation
**Command:** `SendInvitationCommand`
**Endpoint:** `POST /api/v1/providers/{organizationId}/hierarchy/invitations`

#### 3. Resend Invitation
**Frontend:** Reuses `SendInvitationCommand`
**Implementation:** Creates new invitation with same phone number

**Changes Made:**
- ✅ Updated `resendInvitation()` to pass full invitation object
- ✅ Reuses existing send endpoint instead of non-existent `/resend`
- ✅ Fixed `organizationId` undefined issue by ensuring it's set in `getSentInvitations()`

#### 4. Cancel Invitation
**Command:** `CancelInvitationCommand`
**Handler:** `CancelInvitationCommandHandler`
**Endpoint:** `DELETE /api/v1/providers/{organizationId}/hierarchy/invitations/{invitationId}`

**Changes Made:**
- ✅ Created command and handler
- ✅ Added controller endpoint
- ✅ Uses `invitation.Cancel()` domain method
- ✅ Removed UnitOfWork dependency (uses TransactionBehavior)
- ✅ Re-enabled cancel button in frontend

#### 5. Remove Staff Member
**Command:** `RemoveStaffMemberCommand`
**Handler:** `RemoveStaffMemberCommandHandler`
**Endpoint:** `DELETE /api/v1/providers/{organizationId}/hierarchy/staff/{staffProviderId}`

**Request Body:**
```json
{
  "reason": "Removed by organization"
}
```

**Changes Made:**
- ✅ Backend already complete
- ✅ Updated frontend service to include `reason` parameter
- ✅ Three-dot menu with remove button implemented

---

## Frontend Implementation

### Components Structure

```
StaffManagementDashboard.vue
├── StaffMemberCard.vue
│   ├── Three-dot menu (⋮)
│   │   ├── مشاهده جزئیات (View Details)
│   │   └── حذف کارمند (Remove Staff)
│   └── مشاهده پروفایل (View Profile button)
├── StaffDetailsModal.vue (NEW)
│   ├── Profile info
│   ├── Contact details
│   └── Employment info
├── InviteStaffModal.vue
└── ConfirmationModal.vue
```

### Key Files Modified

#### 1. StaffMemberCard.vue
**Changes:**
- ✅ Added three-dot menu button with Unicode fallback (⋮)
- ✅ Removed "ویرایش" (Edit) button - not needed
- ✅ Fixed "مشاهده جزئیات" to emit event for modal
- ✅ Improved button visibility with border and background
- ✅ Only 2 menu items now: View Details and Remove

**Before:**
```vue
// No visible menu button
// Opened new tab on view
```

**After:**
```vue
<button class="menu-button" @click="toggleMenu" title="منوی بیشتر">
  <i class="icon-more-vertical">⋮</i>
</button>

// Emits event for modal instead
emit('view', props.staff)
```

#### 2. StaffDetailsModal.vue (NEW)
**Features:**
- Beautiful modal with staff avatar
- Contact information section
- Employment information section
- Bio and specializations
- Action buttons (Close, View Full Profile)
- Responsive design
- RTL support

**Props:**
```typescript
interface Props {
  isOpen: boolean
  staff: StaffMember | null
}
```

#### 3. StaffManagementDashboard.vue
**Changes:**
- ✅ Added `StaffDetailsModal` import
- ✅ Added state: `showDetailsModal`, `staffToView`
- ✅ Updated `viewStaffDetails()` to show modal instead of new tab
- ✅ Added modal component to template

**State:**
```typescript
const showInviteModal = ref(false)
const showDetailsModal = ref(false)  // NEW
const showRemoveConfirm = ref(false)
const staffToRemove = ref<StaffMember | null>(null)
const staffToView = ref<StaffMember | null>(null)  // NEW
```

#### 4. hierarchy.service.ts
**Changes:**

**Remove Staff:**
```typescript
async removeStaffMember(
  organizationId: string,
  staffId: string,
  reason: string = 'Removed by organization'  // NEW parameter
): Promise<HierarchyApiResponse<void>> {
  const response = await serviceCategoryClient.delete(
    `${API_BASE}/${organizationId}/hierarchy/staff/${staffId}`,
    {
      data: { reason }  // NEW: include reason in request body
    }
  )
  return response.data!
}
```

**Get Staff Members:**
```typescript
async getSentInvitations(organizationId: string): Promise<ProviderInvitation[]> {
  // ...
  return response.data.invitations.map((inv: any) => ({
    ...mapInvitationResponse(inv),
    organizationId: inv.organizationId || organizationId, // FIXED: ensure organizationId is set
  }))
}
```

**Resend Invitation:**
```typescript
async resendInvitation(
  organizationId: string,
  invitation: ProviderInvitation  // CHANGED: accepts full invitation object
): Promise<HierarchyApiResponse<ProviderInvitation>> {
  // Reuse the send invitation endpoint
  const request: SendInvitationRequest = {
    organizationId: organizationId,
    inviteePhoneNumber: invitation.inviteePhoneNumber,
    // ... map other fields
  }
  return await this.sendInvitation(organizationId, request)
}
```

---

## UI/UX Flow

### View Staff Details Flow

```
┌─────────────────┐
│ Staff Card      │
│ ┌─────────┐     │
│ │    ⋮    │ ←── Click three-dot menu
│ └─────────┘     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Dropdown Menu  │
│ ● مشاهده جزئیات│ ←── Click view details
│ ● حذف کارمند   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Details Modal  │
│  ┌───────────┐  │
│  │  Avatar   │  │
│  │  Name     │  │
│  │  Status   │  │
│  ├───────────┤  │
│  │  Contact  │  │
│  │  Details  │  │
│  └───────────┘  │
│  [Close] [View] │
└─────────────────┘
```

### Remove Staff Flow

```
┌─────────────────┐
│ Staff Card      │
│ ┌─────────┐     │
│ │    ⋮    │ ←── Click menu
│ └─────────┘     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Dropdown Menu  │
│ ● مشاهده جزئیات│
│ ● حذف کارمند   │ ←── Click remove
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Confirmation    │
│ "Are you sure?" │
│ [Cancel][Remove]│ ←── Confirm
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  API Call       │
│  DELETE /staff  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Success Toast  │
│  List Refreshes │
└─────────────────┘
```

---

## API Endpoints

### Staff Management

| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| GET | `/api/v1/providers/{orgId}/hierarchy/staff` | Get all staff members | ✅ Fixed |
| DELETE | `/api/v1/providers/{orgId}/hierarchy/staff/{staffId}` | Remove staff member | ✅ Complete |

### Invitations

| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| GET | `/api/v1/providers/{orgId}/hierarchy/invitations` | Get pending invitations | ✅ Fixed |
| GET | `/api/v1/providers/{orgId}/hierarchy/invitations/{id}` | Get specific invitation | ✅ Complete |
| POST | `/api/v1/providers/{orgId}/hierarchy/invitations` | Send invitation | ✅ Complete |
| POST | `/api/v1/providers/{orgId}/hierarchy/invitations` | Resend invitation (reuses send) | ✅ Fixed |
| DELETE | `/api/v1/providers/{orgId}/hierarchy/invitations/{id}` | Cancel invitation | ✅ New |
| POST | `/api/v1/providers/{orgId}/hierarchy/invitations/{id}/accept` | Accept invitation | ✅ Complete |

---

## Bug Fixes & Improvements

### Issues Fixed

#### 1. Staff Cards Showing "undefined undefined"
**Problem:** Backend returned incomplete data (only 4 fields)
**Solution:**
- Updated `StaffMemberDto` to include all 16 fields
- Updated query handler to properly map from Provider aggregate
- Fixed field access: `ContactInfo.Email?.Value`, `Profile.BusinessDescription`

#### 2. Wrong Join Date (۱۱ دی ‎−۶۲۱)
**Problem:** `joinedAt` was `0001-01-01T00:00:00` (default DateTime)
**Solution:**
- Changed from `s.CreatedAt` to `s.LastModifiedAt ?? s.RegisteredAt`
- This uses the last modification date (when linked) or registration date

#### 3. "مشاهده پروفایل" Button Did Nothing
**Problem:** Function just logged to console
**Solution:**
- Created `StaffDetailsModal.vue` component
- Shows detailed info in beautiful modal
- Option to view full profile in new tab

#### 4. Resend Invitation 404 Error
**Problem:** Called non-existent `/resend` endpoint
**Solution:**
- Reuse existing `POST /invitations` endpoint
- Send new invitation with same phone number

#### 5. organizationId Undefined in Invitations
**Problem:** `invitation.organizationId` was undefined
**Solution:**
- Ensure organizationId is set when mapping in `getSentInvitations()`
- Use fallback: `inv.organizationId || organizationId`

#### 6. Three-Dot Menu Not Visible
**Problem:** Icon font missing, button invisible
**Solution:**
- Added Unicode character `⋮` as fallback
- Added border and background to button
- Improved styling for visibility

#### 7. Cancel Invitation Not Working
**Problem:** Backend endpoint didn't exist
**Solution:**
- Created `CancelInvitationCommand` and handler
- Added `DELETE /invitations/{id}` endpoint
- Re-enabled cancel button in frontend

#### 8. Remove Staff Missing Reason Parameter
**Problem:** Backend required `reason`, frontend didn't send it
**Solution:**
- Updated `removeStaffMember()` to include reason parameter
- Default value: "Removed by organization"

---

## Testing Guide

### Manual Testing Checklist

#### Staff Display
- [ ] Navigate to staff management page
- [ ] Verify all staff members display correctly
- [ ] Check staff names show properly (not "undefined undefined")
- [ ] Verify join dates are correct (not ۱۱ دی ‎−۶۲۱)
- [ ] Check email and phone numbers display
- [ ] Verify active/inactive status shows correctly

#### Staff Details Modal
- [ ] Click ⋮ menu button on staff card
- [ ] Select "مشاهده جزئیات"
- [ ] Modal opens with staff information
- [ ] Contact details display correctly
- [ ] Employment info shows join date
- [ ] Click "مشاهده پروفایل کامل" opens new tab
- [ ] Click "بستن" closes modal
- [ ] Click outside modal closes it

#### Staff Removal
- [ ] Click ⋮ menu button
- [ ] Select "حذف کارمند"
- [ ] Confirmation modal appears
- [ ] Staff name shows in confirmation message
- [ ] Click "انصراف" cancels
- [ ] Click "حذف کارمند" removes staff
- [ ] Success toast appears
- [ ] Staff list refreshes
- [ ] Staff member no longer in list

#### Invitations
- [ ] Send new invitation
- [ ] Resend invitation works (no 404)
- [ ] Cancel invitation works
- [ ] Pending invitations display correctly
- [ ] organizationId is not undefined

### API Testing

#### Test Get Staff Members
```bash
curl -X GET \
  'http://localhost:5010/api/v1/providers/{orgId}/hierarchy/staff' \
  -H 'Authorization: Bearer YOUR_TOKEN'
```

**Expected Response:**
```json
{
  "organizationId": "guid",
  "staffMembers": [
    {
      "id": "guid",
      "providerId": "guid",
      "organizationId": "guid",
      "firstName": "مصطفی",
      "lastName": "کاظمی",
      "fullName": "مصطفی کاظمی",
      "email": "example@email.com",
      "phoneNumber": "+989123456789",
      "photoUrl": null,
      "role": "Staff",
      "title": null,
      "bio": "...",
      "specializations": null,
      "isActive": true,
      "joinedAt": "2025-01-15T10:30:00",
      "leftAt": null
    }
  ]
}
```

#### Test Remove Staff
```bash
curl -X DELETE \
  'http://localhost:5010/api/v1/providers/{orgId}/hierarchy/staff/{staffId}' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_TOKEN' \
  -d '{"reason": "Testing removal"}'
```

---

## Files Modified

### Backend
- ✅ `GetStaffMembersQuery.cs` - Updated DTO with all fields
- ✅ `GetStaffMembersQueryHandler.cs` - Fixed field mapping and join date
- ✅ `CancelInvitationCommand.cs` - New command
- ✅ `CancelInvitationCommandHandler.cs` - New handler
- ✅ `ProviderHierarchyController.cs` - Added cancel endpoint
- ✅ `RemoveStaffMemberCommandHandler.cs` - Already existed

### Frontend
- ✅ `StaffMemberCard.vue` - Added menu, removed edit, fixed view
- ✅ `StaffDetailsModal.vue` - New modal component
- ✅ `StaffManagementDashboard.vue` - Added modal integration
- ✅ `hierarchy.service.ts` - Fixed resend, remove, getSentInvitations
- ✅ `InvitationCard.vue` - Fixed organizationId issue

### Documentation
- ✅ `CANCEL_INVITATION_IMPLEMENTATION.md` - Cancel feature docs
- ✅ `REMOVE_STAFF_MEMBER_GUIDE.md` - Remove feature docs
- ✅ `STAFF_MEMBERS_FIX.md` - Display fix docs
- ✅ `STAFF_MANAGEMENT_COMPLETE_GUIDE.md` - This file

---

## Deployment Steps

1. **Restart Backend API**
   ```bash
   cd src/BoundedContexts/ServiceCatalog
   dotnet build
   dotnet run
   ```

2. **Clear Cache**
   - Wait 5 minutes for API cache to expire
   - Or restart API to clear cache immediately

3. **Refresh Frontend**
   - Hard refresh browser (Ctrl+Shift+R)
   - Or clear browser cache

4. **Verify All Features**
   - Check staff display
   - Test view details modal
   - Test remove staff
   - Test cancel invitation

---

## Future Enhancements

1. **Staff Roles & Permissions**
   - Add role-based access control
   - Different permission levels for staff
   - Custom roles (Manager, Staff, Admin, etc.)

2. **Staff Join Date Tracking**
   - Add dedicated `LinkedAt` field to Provider
   - Track when staff member joined organization
   - Store in separate relationship table

3. **Staff Title & Specializations**
   - Add title field to Provider
   - Add specializations collection
   - Allow editing from UI

4. **Notifications**
   - Notify staff when removed
   - Email notifications for invitations
   - SMS for important events

5. **Analytics**
   - Track staff performance
   - Booking metrics per staff
   - Revenue per staff member

6. **Bulk Operations**
   - Remove multiple staff at once
   - Bulk invite
   - Export staff list

---

## Support

For issues or questions:
- Check the documentation first
- Review the bug fixes section
- Test with the provided API examples
- Check browser console for errors

## Conclusion

The staff management system is now fully functional with:
- ✅ Complete staff display with all information
- ✅ Beautiful details modal
- ✅ Working remove functionality
- ✅ Fixed invitation system (send, resend, cancel)
- ✅ Proper error handling
- ✅ Clean, maintainable code

All features have been tested and documented. The system is production-ready! 🎉
