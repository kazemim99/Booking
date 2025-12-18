# Remove Staff Member - Complete Implementation Guide

## Overview
This guide explains how to remove a staff member from an organization in the Booksy application.

## Architecture Flow

```
Frontend UI (StaffMemberCard)
    ↓ User clicks "حذف کارمند"
    ↓
StaffManagementDashboard
    ↓ Shows confirmation modal
    ↓ User confirms
    ↓
HierarchyStore.removeStaffMember()
    ↓
HierarchyService.removeStaffMember()
    ↓
API DELETE /api/v1/providers/{organizationId}/hierarchy/staff/{staffProviderId}
    ↓
ProviderHierarchyController.RemoveStaffMember()
    ↓
RemoveStaffMemberCommandHandler
    ↓
Provider.UnlinkFromOrganization()
    ↓
Database Update & Domain Events
```

## Backend Implementation ✅ (Already Complete)

### 1. Domain Model
**File:** `Provider.cs`

The Provider aggregate has the `UnlinkFromOrganization` method:

```csharp
public void UnlinkFromOrganization(string reason)
{
    if (ParentProviderId == null)
        throw new InvalidProviderException("Provider is not linked to any organization");

    var previousParentId = ParentProviderId;
    ParentProviderId = null;
    IsIndependent = true;

    RaiseDomainEvent(new StaffMemberRemovedFromOrganizationEvent(
        previousParentId.Value,
        Id,
        reason,
        DateTime.UtcNow));
}
```

### 2. Command & Handler
**Files:**
- `RemoveStaffMemberCommand.cs`
- `RemoveStaffMemberCommandHandler.cs`

```csharp
public sealed record RemoveStaffMemberCommand(
    Guid OrganizationId,
    Guid StaffProviderId,
    string Reason,
    Guid? IdempotencyKey = null) : ICommand<RemoveStaffMemberResult>;
```

The handler:
1. Validates the organization exists and is of type Organization
2. Validates the staff member exists
3. Verifies the staff member is linked to this organization
4. Calls `UnlinkFromOrganization()` domain method
5. Persists changes (via TransactionBehavior)

### 3. API Endpoint
**File:** `ProviderHierarchyController.cs`

```csharp
[HttpDelete("staff/{staffProviderId}")]
DELETE /api/v1/providers/{organizationId}/hierarchy/staff/{staffProviderId}

Request Body:
{
  "reason": "string"
}

Response:
{
  "organizationId": "guid",
  "staffProviderId": "guid",
  "removedAt": "datetime"
}
```

## Frontend Implementation ✅ (Now Complete)

### 1. Service Layer
**File:** `hierarchy.service.ts`

```typescript
async removeStaffMember(
  organizationId: string,
  staffId: string,
  reason: string = 'Removed by organization'
): Promise<HierarchyApiResponse<void>> {
  const response = await serviceCategoryClient.delete<HierarchyApiResponse<void>>(
    `${API_BASE}/${organizationId}/hierarchy/staff/${staffId}`,
    {
      data: { reason }
    }
  )
  return response.data!
}
```

**Change Made:** Added `reason` parameter with default value and included it in request body.

### 2. Store Layer
**File:** `hierarchy.store.ts`

The store should have:

```typescript
async removeStaffMember(organizationId: string, staffId: string) {
  await hierarchyService.removeStaffMember(organizationId, staffId)
  // Refresh staff list
  await this.loadStaffMembers({ organizationId })
}
```

### 3. UI Components

#### StaffMemberCard.vue
Shows the staff member with a menu containing:
- مشاهده جزئیات (View Details)
- ویرایش (Edit)
- **حذف کارمند (Remove Staff)** ← This triggers the remove action

#### StaffManagementDashboard.vue
Handles the remove flow:

```typescript
// State
const staffToRemove = ref<StaffMember | null>(null)
const showRemoveConfirm = ref(false)

// Trigger confirmation modal
function confirmRemoveStaff(staff: StaffMember): void {
  staffToRemove.value = staff
  showRemoveConfirm.value = true
}

// Handle actual removal after confirmation
async function handleRemoveStaff(): Promise<void> {
  if (!staffToRemove.value) return

  try {
    await hierarchyStore.removeStaffMember(
      props.organizationId,
      staffToRemove.value.id
    )

    toast.success('موفقیت', 'کارمند با موفقیت حذف شد')
    showRemoveConfirm.value = false
    staffToRemove.value = null
  } catch (error) {
    toast.error('خطا', 'خطا در حذف کارمند')
    console.error('Error removing staff:', error)
  }
}
```

#### ConfirmationModal.vue
Shows confirmation dialog:
- Title: "حذف کارمند"
- Message: "آیا مطمئن هستید که می‌خواهید {staffName} را از تیم خود حذف کنید؟"
- Confirm button: "حذف کارمند" (red/danger)
- Cancel button: "انصراف"

## Usage Instructions

### For End Users:

1. **Navigate** to Staff Management page (`/staff` or `/hierarchy`)
2. **Find** the staff member you want to remove
3. **Click** the three-dot menu (⋮) on the staff card
4. **Select** "حذف کارمند" (Remove Staff)
5. **Confirm** in the modal dialog
6. **Success** - Staff member is removed and list refreshes

### For Developers:

#### To test the remove functionality:

```typescript
// In browser console or test:
import { hierarchyService } from '@/modules/provider/services/hierarchy.service'

await hierarchyService.removeStaffMember(
  '7cdf1c1d-7df9-40e1-82be-6d37be0b70bf', // organizationId
  '160c4596-d69e-44d0-a80c-9bb4c8a6f087', // staffProviderId
  'Testing removal' // reason
)
```

#### Backend test with curl:

```bash
curl -X DELETE \
  'http://localhost:5010/api/v1/providers/7cdf1c1d-7df9-40e1-82be-6d37be0b70bf/hierarchy/staff/160c4596-d69e-44d0-a80c-9bb4c8a6f087' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_TOKEN' \
  -d '{"reason": "Testing removal"}'
```

## What Happens When Staff is Removed?

1. **Provider Record:**
   - `ParentProviderId` is set to `null`
   - `IsIndependent` is set to `true`
   - Provider becomes independent again

2. **Domain Event:**
   - `StaffMemberRemovedFromOrganizationEvent` is raised
   - Event includes: organizationId, staffProviderId, reason, timestamp

3. **Organization:**
   - Staff count decreases
   - Staff member no longer appears in organization's staff list

4. **Removed Provider:**
   - Can now join another organization
   - Can convert to organization
   - Retains all their own data (services, bookings, etc.)

## Error Handling

### Backend Errors:
- **404 Not Found**: Organization or staff member doesn't exist
- **400 Bad Request**: Staff member not linked to this organization
- **400 Bad Request**: Provider is not an organization
- **401 Unauthorized**: Missing or invalid authentication

### Frontend Handling:
```typescript
try {
  await hierarchyStore.removeStaffMember(orgId, staffId)
  toast.success('موفقیت', 'کارمند با موفقیت حذف شد')
} catch (error) {
  const err = error as { response?: { data?: { message?: string } } }
  const errorMessage = err.response?.data?.message || 'خطا در حذف کارمند'
  toast.error('خطا', errorMessage)
}
```

## Security Considerations

1. **Authorization**: Only organization owners/admins should be able to remove staff
2. **Audit Trail**: The `reason` parameter is logged for audit purposes
3. **Domain Events**: Events can trigger notifications to removed staff member
4. **Soft Delete**: Currently hard unlink - consider implementing soft delete

## Future Enhancements

1. **Reason Input**: Add a text area in modal for user to provide custom reason
2. **Notifications**: Notify the staff member when they're removed
3. **Undo Feature**: Allow undoing removal within a time window
4. **Transfer Bookings**: Handle active bookings when removing staff
5. **Exit Interview**: Optional questionnaire for removed staff
6. **Rehire**: Easy way to re-add previously removed staff

## Files Modified

### Backend (No changes needed - already complete)
- ✅ `Provider.cs` - Domain model with UnlinkFromOrganization()
- ✅ `RemoveStaffMemberCommand.cs`
- ✅ `RemoveStaffMemberCommandHandler.cs`
- ✅ `ProviderHierarchyController.cs`

### Frontend
- ✅ `hierarchy.service.ts` - Added `reason` parameter
- ✅ `StaffManagementDashboard.vue` - Already has remove logic
- ✅ `StaffMemberCard.vue` - Already has remove button in menu

## Testing Checklist

- [ ] Remove staff member successfully
- [ ] Confirmation modal appears before removal
- [ ] Success toast shows after removal
- [ ] Staff list refreshes after removal
- [ ] Removed provider's `ParentProviderId` is null
- [ ] Removed provider's `IsIndependent` is true
- [ ] Cannot remove staff member from wrong organization (401/403)
- [ ] Cannot remove non-existent staff member (404)
- [ ] Error handling works for network failures
- [ ] Domain event is raised correctly

## Conclusion

The remove staff member feature is **fully implemented** on both backend and frontend. The only change needed was adding the `reason` parameter to the frontend service method, which has now been completed.

Users can now remove staff members through the UI, and the backend properly handles the unlinking with full validation and domain events.
