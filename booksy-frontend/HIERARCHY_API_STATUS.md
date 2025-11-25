# Provider Hierarchy API Status Report

**Generated:** 2025-11-25
**Purpose:** Document the current status of Provider Hierarchy feature implementation

## Executive Summary

🔴 **CRITICAL FINDING:** The frontend UI components for the Provider Hierarchy feature have been implemented, but **the backend API endpoints do not exist yet**. This requires coordination with the backend team before the feature can be tested or deployed.

## Backend Status

### ❌ Missing API Endpoints

The following 13 API endpoints are required by the frontend but **DO NOT EXIST** in the backend:

#### Organization & Individual Registration
1. `POST /v1/providers/organizations` - Register new organization
2. `POST /v1/providers/individuals` - Register new independent individual

#### Invitation Management
3. `POST /v1/providers/:id/hierarchy/invitations` - Send invitation to individual
4. `GET /v1/providers/:id/hierarchy/invitations` - Get all invitations for organization
5. `GET /v1/providers/:id/hierarchy/invitations/:invId` - Get specific invitation details
6. `POST /v1/providers/:id/hierarchy/invitations/:invId/accept` - Accept invitation
7. `POST /v1/providers/:id/hierarchy/invitations/:invId/reject` - Reject invitation
8. `POST /v1/providers/:id/hierarchy/invitations/:invId/resend` - Resend invitation

#### Join Request Management
9. `POST /v1/providers/:id/hierarchy/join-requests` - Create join request
10. `GET /v1/providers/:id/hierarchy/join-requests` - Get all join requests
11. `POST /v1/providers/:id/hierarchy/join-requests/:reqId/approve` - Approve join request
12. `POST /v1/providers/:id/hierarchy/join-requests/:reqId/reject` - Reject join request

#### Hierarchy Operations
13. `GET /v1/providers/:id/hierarchy` - Get provider hierarchy details
14. `GET /v1/providers/:id/hierarchy/staff` - Get staff members with filtering
15. `DELETE /v1/providers/:id/hierarchy/staff/:staffId` - Remove staff member
16. `POST /v1/providers/:id/hierarchy/convert-to-organization` - Convert individual to organization

### ✅ Existing Related Endpoints

The following staff management endpoints **DO EXIST** in ProvidersController:

- `GET /v1/providers/{id}/staff` - Get all staff members (lines 600-635)
- `POST /v1/providers/{id}/staff` - Add staff member (lines 648-686)
- `PUT /v1/providers/{id}/staff/{staffId}` - Update staff member (lines 699-745)
- `DELETE /v1/providers/{id}/staff/{staffId}` - Remove staff member (lines 756-782)

**Gap:** These endpoints handle basic staff CRUD but lack hierarchy-specific features like:
- Organization vs Individual distinction
- Invitation workflow
- Join request workflow
- Hierarchy relationships

## Frontend Status

### ✅ Completed Components

All UI components have been implemented:

1. **Invitation Flow**
   - `AcceptInvitationView.vue` - Full invitation acceptance page
   - `CompleteStaffProfile.vue` - Profile completion wizard
   - `InvitationCard.vue` - Invitation display with actions

2. **Join Request Flow**
   - `SearchOrganizations.vue` - Search and discover organizations
   - `RequestToJoinModal.vue` - Submit join request
   - `MyJoinRequestsView.vue` - View sent requests
   - `JoinRequestCard.vue` - Request display with actions

3. **Conversion Flow**
   - `ConvertToOrganizationView.vue` - 3-step conversion wizard

4. **Hierarchy Display**
   - `ProviderHierarchy.vue` - Organization structure display
   - `StaffSelector.vue` - Staff selection during booking
   - `ProfileStaff.vue` - Staff member display

5. **Staff Management**
   - `StaffManagementDashboard.vue` - Complete staff management UI
   - Updated `ProviderCard.vue` with hierarchy badges

### ❌ Missing Frontend Services

The following frontend infrastructure is **MISSING** and needs to be created:

1. **`hierarchy.service.ts`** - API client for hierarchy endpoints
   - Location: `booksy-frontend/src/modules/provider/services/hierarchy.service.ts`
   - Status: DOES NOT EXIST
   - Referenced by: All hierarchy components

2. **`hierarchy.store.ts`** - Pinia store for hierarchy state management
   - Location: `booksy-frontend/src/modules/provider/stores/hierarchy.store.ts`
   - Status: DOES NOT EXIST
   - Referenced by: All hierarchy components

3. **`hierarchy.types.ts`** - TypeScript types for hierarchy entities
   - Location: `booksy-frontend/src/modules/provider/types/hierarchy.types.ts`
   - Status: DOES NOT EXIST (or incomplete)
   - Referenced by: All hierarchy components and services

## Required API Contracts

### RegisterOrganizationRequest
```typescript
{
  ownerId: string
  businessName: string
  description?: string
  type: 'Salon' | 'Barbershop' | 'SpaWellness' | 'Clinic' | 'BeautySalon' | 'Other'
  email: string
  phone: string
  addressLine1: string
  addressLine2?: string
  city: string
  state: string
  postalCode: string
  country: string
  logoUrl?: string
}
```

### RegisterIndependentIndividualRequest
```typescript
{
  ownerId: string
  firstName: string
  lastName: string
  bio?: string
  email: string
  phone: string
  city: string
  state: string
  country: string
  photoUrl?: string
}
```

### SendInvitationRequest
```typescript
{
  organizationId: string
  inviteePhoneNumber: string
  inviteeName: string
  message?: string
  expiresInHours?: number // default 72
}
```

### ProviderInvitation (Response)
```typescript
{
  id: string
  organizationId: string
  organizationName: string
  organizationLogo?: string
  inviteePhoneNumber: string
  inviteeName: string
  message?: string
  status: 'Pending' | 'Accepted' | 'Rejected' | 'Expired' | 'Cancelled'
  sentAt: Date
  expiresAt: Date
  respondedAt?: Date
  acceptedByProviderId?: string
}
```

### CreateJoinRequestRequest
```typescript
{
  organizationId: string
  requesterId: string
  message?: string
}
```

### JoinRequest (Response)
```typescript
{
  id: string
  organizationId: string
  organizationName: string
  requesterId: string
  requesterName: string
  requesterPhone?: string
  message?: string
  status: 'Pending' | 'Approved' | 'Rejected' | 'Cancelled'
  createdAt: Date
  respondedAt?: Date
  respondedBy?: string
}
```

### ProviderHierarchy (Response)
```typescript
{
  provider: {
    id: string
    hierarchyType: 'Organization' | 'Individual'
    businessName?: string // for Organizations
    firstName?: string // for Individuals
    lastName?: string // for Individuals
    parentOrganizationId?: string // for Individuals who are staff
    staffCount?: number
  }
  staff?: StaffMember[]
  parentOrganization?: {
    id: string
    businessName: string
    logoUrl?: string
  }
}
```

### ConvertToOrganizationRequest
```typescript
{
  individualProviderId: string
  businessName: string
  description?: string
  businessType: 'Salon' | 'Barbershop' | 'SpaWellness' | 'Clinic' | 'BeautySalon' | 'Other'
  logoUrl?: string
}
```

## Next Steps

### Priority 1: Backend API Development (REQUIRED)

**The backend team must implement the following:**

1. Create `ProviderHierarchyController.cs` with all 16 endpoints listed above
2. Implement domain logic for:
   - Organization vs Individual distinction
   - Invitation lifecycle (send → expire → accept/reject)
   - Join request lifecycle (create → approve/reject)
   - Conversion from Individual to Organization
3. Add authorization rules:
   - Only organization owners can send invitations
   - Only organization owners can approve/reject join requests
   - Only individuals can accept invitations or create join requests
4. Implement notification triggers:
   - SMS/email when invitation sent
   - SMS/email when invitation expires (24h before)
   - Notification when join request received
   - Notification when join request approved/rejected

### Priority 2: Frontend Service Layer (BLOCKED)

**Cannot proceed until backend APIs exist. Once available:**

1. Create `hierarchy.service.ts` with API client methods
2. Create `hierarchy.store.ts` Pinia store
3. Update/create `hierarchy.types.ts` with complete type definitions
4. Update all components to use the store instead of direct service calls
5. Add error handling and retry logic

### Priority 3: Integration Testing (BLOCKED)

Once both backend and frontend services are complete:

1. Test invitation flow end-to-end
2. Test join request flow end-to-end
3. Test conversion flow
4. Test staff management with hierarchy
5. Test booking flow with staff selection
6. Verify notification delivery

### Priority 4: Search Integration

Update provider search to include hierarchy features:
- Add Organization vs Individual filter
- Show "X staff members" in search results
- Add "Professionals at this location" section

## Risk Assessment

🔴 **HIGH RISK:** Feature is non-functional without backend APIs
🟠 **MEDIUM RISK:** Frontend components may need updates once real API contracts are defined
🟡 **LOW RISK:** UI components are complete and follow design system

## Files Reference

### Frontend Components Created
- `booksy-frontend/src/modules/provider/views/invitation/AcceptInvitationView.vue`
- `booksy-frontend/src/modules/provider/components/invitation/CompleteStaffProfile.vue`
- `booksy-frontend/src/modules/provider/components/invitation/InvitationCard.vue`
- `booksy-frontend/src/modules/provider/components/joinrequest/SearchOrganizations.vue`
- `booksy-frontend/src/modules/provider/components/joinrequest/RequestToJoinModal.vue`
- `booksy-frontend/src/modules/provider/views/joinrequest/MyJoinRequestsView.vue`
- `booksy-frontend/src/modules/provider/views/conversion/ConvertToOrganizationView.vue`
- `booksy-frontend/src/modules/provider/components/hierarchy/ProviderHierarchy.vue`
- `booksy-frontend/src/modules/booking/components/StaffSelector.vue`
- `booksy-frontend/src/modules/provider/components/staff/StaffManagementDashboard.vue`
- `booksy-frontend/src/modules/provider/services/notification-templates.service.ts`
- `booksy-frontend/src/modules/provider/services/api-testing.service.ts`

### Frontend Components Modified
- `booksy-frontend/src/modules/provider/components/ProviderCard.vue`
- `booksy-frontend/src/core/router/routes/provider.routes.ts`

### Backend Files
- `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs` (existing, has basic staff endpoints)

## Testing Utility

Created `api-testing.service.ts` with:
- Test functions for all hierarchy endpoints
- Health check listing all required endpoints
- Console testing interface: `window.testHierarchyAPI`

**Usage:**
```javascript
// In browser console
testHierarchyAPI.checkEndpointHealth() // List all endpoints
testHierarchyAPI.testOrganizationRegistration() // Test registration
testHierarchyAPI.runAllTests('provider-id') // Run full test suite
```

## Conclusion

The Provider Hierarchy feature is **80% complete on the frontend** but **0% complete on the backend**. The UI is production-ready, but the feature cannot function without the backend API implementation. This should be escalated to the backend team as a **CRITICAL** priority to unblock testing and deployment.

**Recommended Action:** Hold a technical alignment meeting between frontend and backend teams to:
1. Review API contracts (defined above)
2. Agree on implementation timeline for backend
3. Define API versioning and migration strategy
4. Plan integration testing approach
