# OTP-Based Staff Invitation Acceptance Flow

## Overview
This document describes the implementation of the OTP-based invitation acceptance flow for unregistered users joining an organization as staff members.

## User Flow

### For Unregistered Users
1. **Preview Step**: User clicks invitation link → sees invitation details
2. **Registration Form**: User fills in name, email (optional), and sees data cloning options
3. **OTP Verification**: System sends OTP to invitee's phone → user enters 6-digit code
4. **Account Creation**: System verifies OTP, creates user account, creates individual provider profile, clones data, accepts invitation, and returns auth tokens
5. **Redirect**: User is redirected to provider dashboard

### For Registered Users
- Existing flow remains unchanged: user logs in and accepts invitation directly

## Frontend Implementation

### Components Created/Modified

#### 1. `OTPInput.vue` ✅ (NEW)
**Location**: `booksy-frontend/src/shared/components/ui/OTPInput.vue`

**Features**:
- 6-digit OTP input with individual boxes
- Auto-focus next input on digit entry
- Backspace/Delete key handling
- Arrow key navigation
- Paste support (automatically distributes pasted digits)
- Error state display
- Mobile-responsive design

**Props**:
```typescript
interface Props {
  length?: number         // Default: 6
  modelValue?: string
  error?: string
  autoFocus?: boolean    // Default: true
}
```

**Events**:
- `update:modelValue`: Emitted on every digit change
- `complete`: Emitted when all digits are filled

#### 2. `AcceptInvitationView.vue` ✅ (MODIFIED)
**Location**: `booksy-frontend/src/modules/provider/views/invitation/AcceptInvitationView.vue`

**New Features**:
- Three-step registration flow (preview → form → OTP)
- Pre-filled form fields from invitation data
- Form validation (name length, email format)
- OTP timer with 120-second countdown
- Resend OTP functionality
- Data cloning options (services, hours, gallery)

**State Variables**:
```typescript
// Registration flow
type RegistrationStep = 'preview' | 'form' | 'otp'
const registrationStep = ref<RegistrationStep>('preview')

const registrationForm = reactive({
  firstName: '',
  lastName: '',
  email: '',
  cloneServices: true,
  cloneWorkingHours: true,
  cloneGallery: true,
})

// OTP state
const otpCode = ref('')
const otpError = ref('')
const isSendingOTP = ref(false)
const isVerifyingOTP = ref(false)
const canResendOTP = ref(false)
const otpTimer = ref(120)
```

**Key Functions**:
- `startQuickRegistration()`: Moves to form step, pre-fills name
- `validateRegistrationField()`: Validates firstName, lastName, email
- `sendOTPForInvitation()`: Sends OTP to invitee's phone
- `startOTPTimer()`: Starts 120-second countdown timer
- `verifyOTPAndAccept()`: Verifies OTP and accepts invitation
- `formatPhone()`: Formats phone number for display (0912 XXX XXXX)

#### 3. `hierarchy.service.ts` ✅ (MODIFIED)
**Location**: `booksy-frontend/src/modules/provider/services/hierarchy.service.ts`

**New Method**:
```typescript
async acceptInvitationWithRegistration(request: {
  invitationId: string
  organizationId: string
  phoneNumber: string
  firstName: string
  lastName: string
  email?: string
  otpCode: string
  cloneServices: boolean
  cloneWorkingHours: boolean
  cloneGallery: boolean
}): Promise<HierarchyApiResponse<{
  userId: string
  providerId: string
  accessToken: string
  refreshToken: string
}>>
```

**API Endpoint**: `POST /api/v1/providers/{organizationId}/hierarchy/invitations/{invitationId}/accept-with-registration`

## Backend Implementation Required

### 1. Controller Endpoint ⚠️ (TO BE IMPLEMENTED)

**File**: `Booksy.ServiceCatalog.Api/Controllers/V1/ProviderHierarchyController.cs`

**Endpoint**:
```csharp
[HttpPost("invitations/{invitationId}/accept-with-registration")]
[AllowAnonymous]  // Important: must be anonymous for unregistered users
[ProducesResponseType(typeof(AcceptInvitationWithRegistrationResult), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> AcceptInvitationWithRegistration(
    Guid providerId,  // Organization ID
    Guid invitationId,
    [FromBody] AcceptInvitationWithRegistrationRequest request,
    CancellationToken cancellationToken)
{
    var result = await _mediator.Send(
        new AcceptInvitationWithRegistrationCommand(
            invitationId,
            providerId,
            request.PhoneNumber,
            request.FirstName,
            request.LastName,
            request.Email,
            request.OtpCode,
            request.CloneServices,
            request.CloneWorkingHours,
            request.CloneGallery
        ),
        cancellationToken);
    return Ok(result);
}
```

**Request DTO**:
```csharp
public record AcceptInvitationWithRegistrationRequest(
    string PhoneNumber,
    string FirstName,
    string LastName,
    string? Email,
    string OtpCode,
    bool CloneServices,
    bool CloneWorkingHours,
    bool CloneGallery
);
```

### 2. Command Handler ⚠️ (TO BE IMPLEMENTED)

**File**: `Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/AcceptInvitationWithRegistration/AcceptInvitationWithRegistrationCommandHandler.cs`

**Responsibilities**:
1. **Verify OTP**: Call UserManagement API to verify the OTP code
2. **Create User**: Create user account if doesn't exist
3. **Create Provider**: Create individual provider profile
4. **Clone Data**:
   - Clone services from organization (if `cloneServices` is true)
   - Clone working hours from organization (if `cloneWorkingHours` is true)
   - Clone gallery images from organization (if `cloneGallery` is true)
5. **Accept Invitation**: Link individual to organization
6. **Generate Tokens**: Return JWT tokens for immediate login

**Pseudocode**:
```csharp
public class AcceptInvitationWithRegistrationCommandHandler
    : IRequestHandler<AcceptInvitationWithRegistrationCommand, AcceptInvitationWithRegistrationResult>
{
    public async Task<AcceptInvitationWithRegistrationResult> Handle(
        AcceptInvitationWithRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Get invitation
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId);
        if (invitation == null) throw new NotFoundException("Invitation not found");

        // 2. Verify OTP (call UserManagement API)
        var otpVerified = await _userManagementClient.VerifyOTP(
            request.PhoneNumber,
            request.OtpCode
        );
        if (!otpVerified) throw new ValidationException("Invalid OTP code");

        // 3. Check if phone number already registered
        var existingUser = await _userManagementClient.GetUserByPhone(request.PhoneNumber);
        if (existingUser != null)
            throw new DomainException("Phone number already registered");

        // 4. Create user account
        var user = await _userManagementClient.CreateUser(new CreateUserRequest {
            PhoneNumber = request.PhoneNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserType = UserType.Provider,
            PhoneVerified = true  // Already verified via OTP
        });

        // 5. Create individual provider profile
        var provider = await _providerRepository.CreateIndividualProvider(new CreateIndividualProviderRequest {
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
        });

        // 6. Get organization data
        var organization = await _providerRepository.GetByIdAsync(request.OrganizationId);

        // 7. Clone services
        if (request.CloneServices && organization.Services?.Any() == true) {
            foreach (var service in organization.Services) {
                await _serviceRepository.CreateAsync(new Service {
                    ProviderId = provider.Id,
                    Name = service.Name,
                    Description = service.Description,
                    Duration = service.Duration,
                    Price = service.Price,
                    Category = service.Category,
                    // Mark as cloned
                    SourceProviderId = organization.Id,
                    IsCloned = true
                });
            }
        }

        // 8. Clone working hours
        if (request.CloneWorkingHours && organization.WorkingHours?.Any() == true) {
            foreach (var hour in organization.WorkingHours) {
                await _workingHoursRepository.CreateAsync(new WorkingHours {
                    ProviderId = provider.Id,
                    DayOfWeek = hour.DayOfWeek,
                    StartTime = hour.StartTime,
                    EndTime = hour.EndTime,
                    IsAvailable = hour.IsAvailable,
                    // Mark as cloned
                    SourceProviderId = organization.Id,
                    IsCloned = true
                });
            }
        }

        // 9. Clone gallery images
        if (request.CloneGallery && organization.GalleryImages?.Any() == true) {
            foreach (var image in organization.GalleryImages) {
                await _galleryRepository.CreateAsync(new GalleryImage {
                    ProviderId = provider.Id,
                    ImageUrl = image.ImageUrl,
                    ThumbnailUrl = image.ThumbnailUrl,
                    DisplayOrder = image.DisplayOrder,
                    // Mark as cloned from organization
                    SourceType = GalleryImageSourceType.Organization,
                    SourceProviderId = organization.Id,
                    IsCloned = true
                });
            }
        }

        // 10. Clone location
        if (organization.Location != null) {
            await _locationRepository.CreateAsync(new Location {
                ProviderId = provider.Id,
                AddressLine1 = organization.Location.AddressLine1,
                AddressLine2 = organization.Location.AddressLine2,
                City = organization.Location.City,
                Province = organization.Location.Province,
                PostalCode = organization.Location.PostalCode,
                Latitude = organization.Location.Latitude,
                Longitude = organization.Location.Longitude,
            });
        }

        // 11. Accept invitation (create hierarchy relationship)
        invitation.Accept(provider.Id);
        await _invitationRepository.UpdateAsync(invitation);

        // Create staff member relationship
        await _staffMemberRepository.CreateAsync(new StaffMember {
            OrganizationId = organization.Id,
            IndividualProviderId = provider.Id,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });

        // 12. Generate JWT tokens
        var tokens = await _userManagementClient.GenerateTokens(user.Id);

        // 13. Commit unit of work
        await _unitOfWork.CommitAsync(cancellationToken);

        return new AcceptInvitationWithRegistrationResult {
            UserId = user.Id,
            ProviderId = provider.Id,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken
        };
    }
}
```

### 3. Gallery Image Schema Updates ⚠️ (TO BE IMPLEMENTED)

**Add to `GalleryImage` entity**:
```csharp
public class GalleryImage
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string ImageUrl { get; set; }
    public string ThumbnailUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime UploadedAt { get; set; }

    // NEW: Source tracking for cloned images
    public GalleryImageSourceType SourceType { get; set; }  // Organization or Individual
    public Guid? SourceProviderId { get; set; }  // Original provider ID if cloned
    public bool IsCloned { get; set; }  // True if cloned from another provider
}

public enum GalleryImageSourceType
{
    Organization,
    Individual
}
```

## Security Considerations

### 1. OTP Verification
- OTP must be verified before creating any resources
- OTP should have 2-minute expiration (120 seconds)
- Maximum 3 OTP attempts per invitation
- Rate limiting: 3 OTP requests per 10 minutes per phone number

### 2. Invitation Validation
- Check invitation is not expired (7 days from creation)
- Check invitation status is "Pending"
- Check phone number matches invitation target
- Prevent duplicate acceptances

### 3. Authentication
- Return JWT tokens immediately after successful registration
- Store tokens in auth store
- Redirect to dashboard with authenticated session

## API Flow Diagram

```
User clicks invitation link
         ↓
Frontend: Load invitation details
         ↓
User fills registration form
         ↓
Frontend: POST /api/v1/auth/send-verification-code
         ↓
UserManagement API: Send OTP via SMS
         ↓
User enters OTP code
         ↓
Frontend: POST /api/v1/providers/{orgId}/hierarchy/invitations/{id}/accept-with-registration
         ↓
ServiceCatalog API:
  ├─→ Verify OTP (call UserManagement)
  ├─→ Create User
  ├─→ Create Provider
  ├─→ Clone Services
  ├─→ Clone Working Hours
  ├─→ Clone Gallery Images
  ├─→ Clone Location
  ├─→ Accept Invitation
  └─→ Generate JWT tokens
         ↓
Frontend: Store tokens, redirect to dashboard
```

## Testing Checklist

### Frontend Tests
- [ ] OTP Input component keyboard navigation
- [ ] OTP Input paste functionality
- [ ] Form validation (name length, email format)
- [ ] OTP timer countdown
- [ ] Resend OTP after timer expires
- [ ] Error display for invalid OTP
- [ ] Success flow with redirect

### Backend Tests
- [ ] OTP verification success/failure
- [ ] Duplicate phone number rejection
- [ ] Invitation expiry check
- [ ] Services cloning
- [ ] Working hours cloning
- [ ] Gallery images cloning
- [ ] Location cloning
- [ ] Staff member relationship creation
- [ ] JWT token generation
- [ ] Transaction rollback on error

### Integration Tests
- [ ] Complete flow from invitation link to dashboard
- [ ] Existing registered user flow unchanged
- [ ] Invitation link expiration
- [ ] Invalid OTP handling
- [ ] Network error handling

## Environment Variables

No new environment variables required. Existing configuration sufficient:
- `VITE_SERVICE_CATALOG_API_URL`: Service Catalog API base URL
- `VITE_USER_MANAGEMENT_API_URL`: UserManagement API base URL

## Deployment Notes

### Frontend Deployment
1. Build frontend with updated components
2. Deploy to hosting environment
3. No database migrations required

### Backend Deployment
1. Implement backend command handler
2. Add database migration for gallery image source tracking
3. Deploy ServiceCatalog API
4. Test OTP verification integration with UserManagement API

## Future Enhancements

1. **Email Verification**: Optional email verification step
2. **Profile Photo Upload**: Allow users to upload profile photo during registration
3. **Custom Service Selection**: Let users choose which services to clone
4. **Gallery Preview**: Show organization gallery images before cloning
5. **Multi-language Support**: Support multiple languages for OTP messages
6. **SMS Provider Integration**: Integrate with SMS gateway for OTP delivery

## Related Documents

- [Staff Invitation Flow](./STAFF_INVITATION_FLOW.md)
- [Provider Hierarchy Documentation](../openspec/changes/add-provider-hierarchy/)
- [Authentication Flow](./AUTH_FLOW.md)
