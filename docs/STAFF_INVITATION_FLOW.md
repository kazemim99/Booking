# Staff Invitation Flow Documentation

## Overview

This document describes the complete staff invitation flow in the Booksy application, allowing organizations to invite individuals to join as staff members.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Backend Implementation](#backend-implementation)
- [Frontend Implementation](#frontend-implementation)
- [API Endpoints](#api-endpoints)
- [Data Flow](#data-flow)
- [Configuration](#configuration)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

---

## Architecture Overview

The staff invitation system follows a domain-driven design (DDD) approach with CQRS pattern:

```
┌─────────────────────────────────────────────────────────────────┐
│                     Staff Invitation Flow                        │
└─────────────────────────────────────────────────────────────────┘

1. Organization sends invitation
   ↓
2. Backend creates ProviderInvitation entity
   ↓
3. InvitationSentEvent raised
   ↓
4. InvitationSentNotificationHandler sends SMS
   ↓
5. Staff receives SMS with invitation link
   ↓
6. Staff clicks link → AcceptInvitationView loads
   ↓
7. Staff accepts invitation
   ↓
8. Staff member relationship created
   ↓
9. Staff redirected to dashboard
```

---

## Backend Implementation

### Domain Layer

#### Aggregates

**ProviderInvitation** (`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderInvitationAggregate/ProviderInvitation.cs`)

```csharp
public sealed class ProviderInvitation : AggregateRoot<Guid>
{
    public ProviderId OrganizationId { get; private set; }
    public PhoneNumber PhoneNumber { get; private set; }
    public string? InviteeName { get; private set; }
    public string? Message { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public ProviderId? AcceptedByProviderId { get; private set; }
}
```

**Key Methods:**
- `Create()` - Creates new invitation and raises `InvitationSentEvent`
- `Accept()` - Accepts invitation and raises `InvitationAcceptedEvent`
- `Reject()` - Rejects invitation
- `Cancel()` - Cancels invitation (by organization)
- `MarkAsExpired()` - Marks invitation as expired

#### Domain Events

**InvitationSentEvent** (`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Events/InvitationSentEvent.cs`)

```csharp
public sealed record InvitationSentEvent(
    Guid InvitationId,
    ProviderId OrganizationId,
    string PhoneNumber,
    DateTime SentAt) : DomainEvent;
```

**InvitationAcceptedEvent** (`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Events/InvitationAcceptedEvent.cs`)

```csharp
public sealed record InvitationAcceptedEvent(
    Guid InvitationId,
    ProviderId OrganizationId,
    ProviderId IndividualProviderId,
    DateTime AcceptedAt) : DomainEvent;
```

### Application Layer

#### Commands

**SendInvitationCommand** (`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/SendInvitation/`)

```csharp
public sealed record SendInvitationCommand(
    Guid OrganizationId,
    string PhoneNumber,
    string? InviteeName = null,
    string? Message = null) : ICommand<SendInvitationResult>;
```

**Handler:**
- Validates organization exists and is of type `Organization`
- Checks for existing pending invitations to same phone number
- Creates `ProviderInvitation` entity
- Saves to database
- Returns invitation details

**AcceptInvitationCommand** (`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/AcceptInvitation/`)

```csharp
public sealed record AcceptInvitationCommand(
    Guid InvitationId,
    Guid IndividualProviderId) : ICommand<AcceptInvitationResult>;
```

#### Queries

**GetInvitationQuery** (`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/ProviderHierarchy/GetInvitation/`)

```csharp
public sealed record GetInvitationQuery(
    Guid OrganizationId,
    Guid InvitationId) : IQuery<GetInvitationResult>;
```

**Result:**
```csharp
public sealed record GetInvitationResult(
    Guid InvitationId,
    Guid OrganizationId,
    string OrganizationName,
    string? OrganizationLogo,
    string PhoneNumber,
    string? InviteeName,
    string? Message,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RespondedAt);
```

#### Event Handlers

**InvitationSentNotificationHandler** (`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/EventHandlers/ProviderHierarchy/InvitationSentNotificationHandler.cs`)

```csharp
public sealed class InvitationSentNotificationHandler
    : IDomainEventHandler<InvitationSentEvent>
{
    public async Task HandleAsync(InvitationSentEvent domainEvent,
        CancellationToken cancellationToken)
    {
        // 1. Get organization details
        var organization = await _providerRepository.GetByIdAsync(...);

        // 2. Generate invitation link
        var invitationLink = $"{baseUrl}/provider/invitations/{invitationId}/accept?org={orgId}";

        // 3. Send SMS
        await _smsService.SendSmsAsync(phoneNumber, message, cancellationToken);
    }
}
```

**SMS Message Format (Persian):**
```
{OrganizationName} شما را به عنوان کارمند دعوت کرده است. برای پذیرش دعوت روی لینک کلیک کنید:
{invitationLink}
(اعتبار: 7 روز)
```

### API Layer

**ProviderHierarchyController** (`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProviderHierarchyController.cs`)

#### Endpoints

1. **Send Invitation**
   ```
   POST /api/v1/providers/{providerId}/hierarchy/invitations
   ```

2. **Get Invitation** (Public - No Auth Required)
   ```
   GET /api/v1/providers/{providerId}/hierarchy/invitations/{invitationId}
   ```

3. **Accept Invitation**
   ```
   POST /api/v1/providers/{providerId}/hierarchy/invitations/{invitationId}/accept
   ```

4. **Cancel Invitation**
   ```
   DELETE /api/v1/providers/{providerId}/hierarchy/invitations/{invitationId}
   ```

5. **Get Pending Invitations**
   ```
   GET /api/v1/providers/{providerId}/hierarchy/invitations
   ```

---

## Frontend Implementation

### Components

#### InvitationCard
**Location:** `booksy-frontend/src/modules/provider/components/staff/InvitationCard.vue`

**Purpose:** Displays invitation details in the staff management dashboard

**Features:**
- Shows invitee name, phone number, and status
- Displays sent date and expiry date
- Actions: View Details, Resend, Cancel
- Status badges with color coding
- Handles all invitation statuses (Pending, Accepted, Rejected, Expired, Cancelled)

**Key Functions:**
- `formatDate()` - Formats dates in Persian with validation
- `getStatusLabel()` - Maps status enum to Persian labels
- `isExpired` - Computed property checking expiration

#### AcceptInvitationView
**Location:** `booksy-frontend/src/modules/provider/views/invitation/AcceptInvitationView.vue`

**Purpose:** Public page for staff to accept invitations

**Features:**
- Loads invitation details via API
- Shows organization logo, name, and invitation message
- Displays invitation and expiry dates
- Two acceptance flows:
  - **Existing users:** Direct accept/reject buttons
  - **New users:** Profile completion form
- Handles expired invitations
- Image loading with fallback icon
- RTL layout for Persian text

**States:**
- Loading
- Error
- Expired
- Accepted (success)
- Ready to accept

**Key Functions:**
- `loadInvitation()` - Fetches invitation from API
- `handleAccept()` - Accepts invitation and redirects
- `handleReject()` - Rejects invitation
- `handleImageError()` - Handles logo loading failures
- `formatDate()` - Formats dates with validation

#### StaffManagementView
**Location:** `booksy-frontend/src/modules/provider/views/staff/StaffManagementView.vue`

**Purpose:** Main dashboard for managing staff and invitations

**Features:**
- Send new invitations via InviteStaffModal
- View pending invitations list
- Resend and cancel invitations
- View invitation details

### Services

#### HierarchyService
**Location:** `booksy-frontend/src/modules/provider/services/hierarchy.service.ts`

**Key Methods:**

```typescript
class HierarchyService {
  // Send invitation
  async sendInvitation(
    organizationId: string,
    request: SendInvitationRequest
  ): Promise<HierarchyApiResponse<ProviderInvitation>>

  // Get invitation by ID
  async getInvitation(
    organizationId: string,
    invitationId: string
  ): Promise<ProviderInvitation>

  // Get all sent invitations
  async getSentInvitations(
    organizationId: string
  ): Promise<ProviderInvitation[]>

  // Accept invitation
  async acceptInvitation(
    organizationId: string,
    invitationId: string,
    request: AcceptInvitationRequest
  ): Promise<HierarchyApiResponse<{...}>>

  // Cancel invitation
  async cancelInvitation(
    organizationId: string,
    invitationId: string
  ): Promise<HierarchyApiResponse<void>>

  // Resend invitation
  async resendInvitation(
    organizationId: string,
    invitationId: string
  ): Promise<HierarchyApiResponse<ProviderInvitation>>
}
```

**Helper Functions:**

```typescript
// Converts relative image URLs to absolute
function toAbsoluteUrl(url: string | undefined): string | undefined {
  // Converts /uploads/... to http://localhost:5010/uploads/...
}

// Maps backend response to frontend format
function mapInvitationResponse(backendInvitation: any): ProviderInvitation {
  // Handles field name differences (createdAt → sentAt)
  // Converts logo URLs to absolute paths
}
```

### Store

#### HierarchyStore
**Location:** `booksy-frontend/src/modules/provider/stores/hierarchy.store.ts`

**State:**
```typescript
{
  sentInvitations: ProviderInvitation[]
  receivedInvitations: ProviderInvitation[]
  loading: { invitations: boolean, ... }
  errors: { invitations?: string, ... }
}
```

**Actions:**
- `sendInvitation()`
- `getInvitation()`
- `loadSentInvitations()`
- `acceptInvitation()`
- `cancelInvitation()`
- `resendInvitation()`

### Routes

**Location:** `booksy-frontend/src/core/router/routes/provider.routes.ts`

```typescript
{
  path: '/provider/invitations/:id/accept',
  name: 'AcceptInvitation',
  component: () => import('@/modules/provider/views/invitation/AcceptInvitationView.vue'),
  meta: {
    requiresAuth: false, // Public route
    title: 'قبول دعوت'
  }
}
```

### Types

**Location:** `booksy-frontend/src/modules/provider/types/hierarchy.types.ts`

```typescript
export interface ProviderInvitation {
  id: string
  organizationId: string
  organizationName: string
  organizationLogo?: string
  organizationType?: string
  inviteePhoneNumber: string
  inviteeName?: string
  message?: string
  status: InvitationStatus
  sentAt: Date
  expiresAt: Date
  respondedAt?: Date
  acceptedByProviderId?: string
  createdBy?: string
  createdByName?: string
}

export enum InvitationStatus {
  Pending = 'Pending',
  Accepted = 'Accepted',
  Rejected = 'Rejected',
  Expired = 'Expired',
  Cancelled = 'Cancelled'
}
```

---

## API Endpoints

### Send Invitation

**Endpoint:** `POST /api/v1/providers/{providerId}/hierarchy/invitations`

**Authorization:** Required (Organization owner)

**Request:**
```json
{
  "inviteePhoneNumber": "+989123456789",
  "inviteeName": "مهدی محمدی",
  "message": "خوشحال می‌شویم به تیم ما بپیوندید"
}
```

**Response:** `201 Created`
```json
{
  "invitationId": "guid",
  "organizationId": "guid",
  "phoneNumber": "+989123456789",
  "status": "Pending",
  "expiresAt": "2025-12-04T13:03:56Z"
}
```

### Get Invitation

**Endpoint:** `GET /api/v1/providers/{providerId}/hierarchy/invitations/{invitationId}`

**Authorization:** None (Public endpoint)

**Response:** `200 OK`
```json
{
  "invitationId": "guid",
  "organizationId": "guid",
  "organizationName": "سالن نهال",
  "organizationLogo": "/uploads/providers/.../logo.jpg",
  "phoneNumber": "+989123456789",
  "inviteeName": "مهدی محمدی",
  "message": "خوشحال می‌شویم به تیم ما بپیوندید",
  "status": "Pending",
  "createdAt": "2025-11-27T13:03:56Z",
  "expiresAt": "2025-12-04T13:03:56Z"
}
```

### Accept Invitation

**Endpoint:** `POST /api/v1/providers/{providerId}/hierarchy/invitations/{invitationId}/accept`

**Authorization:** Required (Individual provider)

**Request:**
```json
{
  "invitationId": "guid"
}
```

**Response:** `200 OK`
```json
{
  "staffMemberId": "guid",
  "organizationId": "guid"
}
```

---

## Data Flow

### 1. Sending Invitation

```
Organization Dashboard
  ↓
Click "Invite Staff"
  ↓
Fill InviteStaffModal form
  ↓
POST /api/v1/providers/{orgId}/hierarchy/invitations
  ↓
SendInvitationCommandHandler
  ↓
Create ProviderInvitation entity
  ↓
Raise InvitationSentEvent
  ↓
InvitationSentNotificationHandler
  ↓
Send SMS via ISmsNotificationService
  ↓
Update UI with new invitation
```

### 2. Accepting Invitation

```
Staff receives SMS
  ↓
Click invitation link
  ↓
Navigate to /provider/invitations/{id}/accept?org={orgId}
  ↓
AcceptInvitationView loads
  ↓
GET /api/v1/providers/{orgId}/hierarchy/invitations/{id}
  ↓
Display invitation details
  ↓
User clicks "Accept"
  ↓
POST /api/v1/providers/{orgId}/hierarchy/invitations/{id}/accept
  ↓
AcceptInvitationCommandHandler
  ↓
Create staff member relationship
  ↓
Redirect to /provider/dashboard
```

---

## Configuration

### Backend Configuration

**appsettings.json:**
```json
{
  "App": {
    "BaseUrl": "https://yourdomain.com"
  },
  "Sms": {
    "Provider": "Kavenegar",
    "ApiKey": "your-api-key"
  }
}
```

### Frontend Configuration

**.env:**
```env
VITE_SERVICE_CATALOG_API_URL=http://localhost:5010/api
```

**.env.production:**
```env
VITE_SERVICE_CATALOG_API_URL=https://api.yourdomain.com/api
```

---

## Testing

### Backend Tests

**Unit Tests:**
```csharp
// Test invitation creation
[Fact]
public async Task SendInvitation_ValidRequest_CreatesInvitation()
{
    // Arrange
    var command = new SendInvitationCommand(...);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.InvitationId.Should().NotBeEmpty();
    result.Status.Should().Be("Pending");
}
```

**Integration Tests:**
```csharp
[Fact]
public async Task AcceptInvitation_ValidInvitation_CreatesStaffMember()
{
    // Test the complete flow from sending to accepting
}
```

### Frontend Tests

**Component Tests:**
```typescript
describe('InvitationCard', () => {
  it('displays invitation details correctly', () => {
    // Test component rendering
  })

  it('formats dates in Persian', () => {
    // Test date formatting
  })

  it('handles expired invitations', () => {
    // Test expiry logic
  })
})
```

### Manual Testing Checklist

- [ ] Organization can send invitation
- [ ] SMS is sent to invitee
- [ ] Invitation link is clickable
- [ ] Invitation page loads correctly
- [ ] Organization logo displays properly
- [ ] Dates show in Persian format
- [ ] Accept button works for authenticated users
- [ ] New users can complete profile
- [ ] Expired invitations show correct state
- [ ] Organization can cancel pending invitations
- [ ] Organization can resend invitations

---

## Troubleshooting

### Issue: Dates showing "نامشخص"

**Cause:** Backend sending `createdAt` but frontend expects `sentAt`

**Solution:** The `mapInvitationResponse` function handles both:
```typescript
sentAt: new Date(backendInvitation.createdAt || backendInvitation.sentAt)
```

### Issue: Organization logo not loading

**Cause:** Relative URL not converted to absolute

**Solution:** `toAbsoluteUrl()` function converts paths:
```typescript
// /uploads/... → http://localhost:5010/uploads/...
```

**Check:**
1. Verify `VITE_SERVICE_CATALOG_API_URL` is set correctly
2. Check browser console for image load errors
3. Verify image file exists on server
4. Check CORS settings on backend

### Issue: SMS not sending

**Cause:** Event handler not registered or SMS service not configured

**Solution:**
1. Verify `InvitationSentNotificationHandler` is registered in DI
2. Check `ISmsNotificationService` implementation
3. Verify SMS provider API key in configuration
4. Check logs for SMS sending errors

### Issue: 404 on invitation acceptance

**Cause:** Route not registered or invitation ID invalid

**Solution:**
1. Verify route is registered in `provider.routes.ts`
2. Check invitation URL format: `/provider/invitations/{id}/accept?org={orgId}`
3. Verify invitation exists in database
4. Check network tab for actual error response

---

## Best Practices

### Security

1. **Public endpoint** for GetInvitation is `[AllowAnonymous]` - this is intentional for invitation acceptance
2. **Validate invitation** belongs to organization before allowing operations
3. **Check expiration** before accepting invitations
4. **Prevent duplicate** invitations to same phone number

### Performance

1. **Cache organization** logos (already handled by browser)
2. **Index database** on `PhoneNumber` and `Status` for fast lookup
3. **Lazy load** invitation lists with pagination

### User Experience

1. **Clear error messages** in Persian
2. **Loading states** for all async operations
3. **Fallback icon** if logo fails to load
4. **Auto-redirect** after successful acceptance
5. **Confirmation dialogs** for destructive actions

---

## Future Enhancements

### Planned Features

1. **Email notifications** in addition to SMS
2. **Invitation templates** for common messages
3. **Bulk invitations** for multiple staff members
4. **Invitation analytics** (acceptance rate, time to accept)
5. **Reminder notifications** 24 hours before expiry
6. **Custom expiration** periods per invitation

### Technical Improvements

1. **Real-time updates** using SignalR when invitation is accepted
2. **Offline support** for viewing invitations
3. **QR code** generation for easy invitation sharing
4. **Deep linking** for mobile apps

---

## Related Documentation

- [Provider Hierarchy System](./PROVIDER_HIERARCHY.md)
- [SMS Notification Service](./SMS_NOTIFICATIONS.md)
- [Domain Events](./DOMAIN_EVENTS.md)
- [API Documentation](./API_REFERENCE.md)

---

## Critical Implementation Details

### Event Dispatching Order

**IMPORTANT:** The system uses a special event dispatching pattern to ensure SMS messages are sent AFTER the invitation is saved to the database.

**Problem:**
- Default `CommitAndPublishEventsAsync()` dispatches events BEFORE saving to database
- SMS was being sent with invitation IDs that didn't exist in DB yet
- Recipients couldn't find invitations when clicking links

**Solution:**
```csharp
// New method in EfCoreUnitOfWork
public async Task<int> SaveAndPublishEventsAsync(CancellationToken cancellationToken)
{
    // 1. Save to database FIRST
    var result = await _context.SaveChangesAsync(cancellationToken);

    // 2. THEN dispatch domain events (including SMS sending)
    await DispatchDomainEventsAsync(cancellationToken);

    return result;
}
```

**Usage in SendInvitationCommandHandler:**
```csharp
await _invitationWriteRepository.SaveAsync(invitation, cancellationToken);

// Use SaveAndPublishEventsAsync to ensure invitation is in DB before SMS is sent
await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
```

**Repository Pattern:**
- Repositories should NOT call `SaveChangesAsync()` directly
- UnitOfWork is responsible for saving and event dispatching
- This ensures proper transactional boundaries

### Provider ID vs Organization ID

**Critical Distinction:**

When **sending invitations**, use the **Organization's provider ID**:
```typescript
// StaffManagementView.vue
const organizationId = computed(() => {
  const hierarchy = hierarchyStore.currentHierarchy

  // If current provider is Organization, use their ID
  if (hierarchy?.provider?.hierarchyType === 'Organization') {
    return currentProvider?.id
  }

  // If Individual with parent, use parent organization's ID
  if (hierarchy?.provider?.hierarchyType === 'Individual' && hierarchy?.parentOrganization) {
    return hierarchy.parentOrganization.id
  }

  return ''
})
```

When **accepting invitations**, use the **Individual's provider ID**:
```typescript
// AcceptInvitationView.vue
const currentProviderId = authStore.providerId  // Individual's ID

await hierarchyStore.acceptInvitation(
  currentProviderId,  // NOT organizationId!
  invitationId.value,
  { invitationId: invitationId.value }
)
```

**Backend Validation:**
```csharp
// AcceptInvitationCommandHandler.cs
if (individualProvider.HierarchyType != ProviderHierarchyType.Individual)
    throw new DomainValidationException("Only individual providers can accept invitations");
```

### SendInvitationResult Enhancement

**Original Issue:** Modal wouldn't close after sending invitation because frontend couldn't map incomplete response.

**Fixed Result Structure:**
```csharp
public sealed record SendInvitationResult(
    Guid InvitationId,
    Guid OrganizationId,
    string OrganizationName,      // Added
    string? OrganizationLogo,     // Added
    string PhoneNumber,
    string? InviteeName,          // Added
    string? Message,              // Added
    DateTime CreatedAt,           // Added
    DateTime ExpiresAt,
    string Status);
```

**Why this matters:**
- Frontend `mapInvitationResponse()` expects all these fields
- Missing fields caused mapping failures
- Modal remained open because success handler didn't fire

### Toast Notification Pattern

**Correct Usage:**
```typescript
const toast = useToast()

// Success notification
toast.success('موفقیت', 'دعوت با موفقیت ارسال شد')

// Error notification
toast.error('خطا', 'خطا در ارسال دعوت. لطفاً دوباره تلاش کنید.')
```

**NOT:**
```typescript
// ❌ WRONG - These don't exist
const { showSuccess, showError } = useToast()
```

### Hierarchy Loading Requirement

**Must load hierarchy before determining organization ID:**

```typescript
async function loadData(): Promise<void> {
  // 1. Load current provider
  if (!providerStore.currentProvider) {
    await providerStore.loadCurrentProvider()
  }

  // 2. Load hierarchy to determine if Organization or Individual
  const currentProviderId = providerStore.currentProvider?.id
  if (currentProviderId && !hierarchyStore.currentHierarchy) {
    await hierarchyStore.loadProviderHierarchy(currentProviderId)
  }

  // 3. NOW we can safely determine organizationId
  const orgId = organizationId.value
  if (!orgId) {
    console.warn('No organization ID available')
    return
  }

  // 4. Load data using correct organization ID
  await Promise.all([...])
}
```

### Error Prevention Checklist

**Before Sending Invitation:**
- [ ] Current user hierarchy is loaded
- [ ] Organization ID is not empty
- [ ] Current user is Organization OR Individual with parent organization

**Before Accepting Invitation:**
- [ ] Current user is logged in
- [ ] Current user's provider ID is available
- [ ] Current user is Individual type (NOT Organization)
- [ ] Invitation exists and is pending
- [ ] Invitation is not expired

**Backend Validation:**
- [ ] Organization exists and is type Organization
- [ ] No pending invitation exists for phone number
- [ ] Individual exists and is type Individual
- [ ] Individual is not already linked to another organization

---

## Troubleshooting Guide

### SMS Not Being Sent

**Symptom:** Invitation created but no SMS received

**Debug Steps:**
1. Check logs for "Saving invitation to DB with ID: {InvitationId}"
2. Check logs for "Generated invitation link with InvitationId: {InvitationId}"
3. Verify both IDs match
4. Check SMS service configuration

**Common Causes:**
- Events dispatched before database save (use `SaveAndPublishEventsAsync`)
- Event handler not registered in DI container
- SMS service credentials invalid

### Modal Not Closing After Send

**Symptom:** Invitation sent successfully but modal stays open

**Debug Steps:**
1. Check browser console for errors
2. Verify response includes all required fields
3. Check `handleInvitationSent()` is called

**Common Causes:**
- `SendInvitationResult` missing required fields
- Store `sendInvitation` throwing error
- Event handler missing or misspelled

### "Only individual providers can accept invitations" Error

**Symptom:** User gets error when trying to accept invitation

**Debug Steps:**
1. Check browser console logs:
   - Current Provider ID from authStore
   - Organization ID from invitation
   - Current provider hierarchy type
2. Verify user is logged in as Individual
3. Check API URL being called

**Common Causes:**
- User logged in as Organization instead of Individual
- Frontend passing organizationId instead of currentProviderId
- Hierarchy not loaded correctly
- User registered with wrong provider type

### Double Slash in API URL (/providers//hierarchy/invitations)

**Symptom:** 404 error, URL contains `//`

**Debug Steps:**
1. Check `organizationId` value (should not be empty)
2. Verify hierarchy is loaded before accessing organizationId
3. Check provider type determination logic

**Common Causes:**
- Hierarchy not loaded
- Accessing organizationId before hierarchy loads
- Wrong provider ID being used

### Invitation ID Not Found in Database

**Symptom:** SMS contains invitation link but ID doesn't exist in database

**Debug Steps:**
1. Check event dispatch order in logs
2. Verify `SaveAndPublishEventsAsync` is being used
3. Check transaction boundaries

**Common Causes:**
- Using `CommitAndPublishEventsAsync` instead of `SaveAndPublishEventsAsync`
- Repository calling `SaveChangesAsync` directly
- Events dispatched before database commit

---

## Changelog

### Version 1.1.0 (2025-11-27)

**Critical Fixes:**
- Fixed event dispatching order (save before dispatch)
- Fixed provider ID vs organization ID confusion
- Fixed modal not closing after send
- Fixed double slash in API URLs
- Fixed toast notification function names
- Enhanced SendInvitationResult with all required fields

**Added:**
- `SaveAndPublishEventsAsync()` method in UnitOfWork
- Debug logging for invitation acceptance
- Frontend validation for provider types
- Hierarchy loading before organization ID determination
- Comprehensive troubleshooting guide

**Technical:**
- Repository pattern: removed direct `SaveChangesAsync` calls
- SendInvitationCommandHandler: uses `SaveAndPublishEventsAsync`
- AcceptInvitationView: validates Individual type before API call
- StaffManagementView: loads hierarchy before determining org ID
- InviteStaffModal: validates organization ID before submit

### Version 1.0.0 (2025-11-27)

**Added:**
- Complete staff invitation flow
- SMS notification on invitation sent
- Public invitation acceptance page
- Organization logo display with fallback
- Persian date formatting with validation
- Image URL conversion to absolute paths
- View details functionality
- Comprehensive error handling

**Fixed:**
- Date field mapping (`createdAt` → `sentAt`)
- Organization logo URL resolution
- Invalid date handling
- Missing `Cancelled` status support

**Technical:**
- Created `InvitationSentNotificationHandler`
- Added `GetInvitationQuery` and handler
- Added `GetInvitation` API endpoint
- Enhanced `toAbsoluteUrl` helper function
- Added image error handling in AcceptInvitationView
