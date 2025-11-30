# Cancel Invitation Implementation

## Summary
Implemented the ability to cancel pending invitations from organizations to staff members.

## Changes Made

### Backend

1. **Command & Handler Created:**
   - `CancelInvitationCommand.cs` - Command with `InvitationId`, `OrganizationId`, and `IdempotencyKey`
   - `CancelInvitationCommandHandler.cs` - Handler that:
     - Validates invitation exists
     - Verifies invitation belongs to the organization
     - Calls the domain `Cancel()` method (already existed)
     - Saves changes to database

2. **Controller Updated:**
   - Added `DELETE /api/v1/providers/{providerId}/hierarchy/invitations/{invitationId}` endpoint
   - Added `using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CancelInvitation;`
   - Endpoint validates authorization and organization ownership

### Frontend

1. **Cancel Button Re-enabled:**
   - Uncommented the cancel button in `InvitationCard.vue`
   - Re-enabled `handleCancel()` function
   - Button only shows for pending, non-expired invitations

2. **Service Layer:**
   - `cancelInvitation()` method already existed in `hierarchy.service.ts`
   - Calls `DELETE /api/v1/providers/{organizationId}/hierarchy/invitations/{invitationId}`

## API Endpoint

```http
DELETE /api/v1/providers/{organizationId}/hierarchy/invitations/{invitationId}
```

**Authorization:** Required (organization owner)

**Response:**
```json
{
  "invitationId": "uuid",
  "success": true,
  "message": "Invitation cancelled successfully"
}
```

**Error Cases:**
- 404: Invitation not found
- 400: Invitation does not belong to organization
- 400: Cannot cancel invitation with status other than "Pending"

## Domain Logic

The `ProviderInvitation` aggregate already had a `Cancel()` method that:
- Validates invitation status is `Pending`
- Updates status to `Cancelled`
- Sets `RespondedAt` timestamp
- Throws `DomainValidationException` if invitation cannot be cancelled

## Files Modified

### Backend
- `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/CancelInvitation/CancelInvitationCommand.cs` (new)
- `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/CancelInvitation/CancelInvitationCommandHandler.cs` (new)
- `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProviderHierarchyController.cs` (updated)

### Frontend
- `booksy-frontend/src/modules/provider/components/staff/InvitationCard.vue` (updated - re-enabled cancel button)

## Testing

1. **Build Status:** ✅ Backend builds successfully with 0 errors
2. **Manual Testing Required:**
   - Send an invitation
   - Verify cancel button appears for pending invitations
   - Click cancel and confirm
   - Verify invitation status changes to "Cancelled"
   - Verify cancelled invitation is removed from pending list
   - Try to cancel an already accepted/rejected invitation (should fail)

## Notes

- The domain model already supported cancellation, so only application/API layer changes were needed
- Cancellation is only allowed for pending invitations
- Organization ownership is validated before cancellation
- The frontend service layer already had the method implemented, it just needed the backend endpoint
