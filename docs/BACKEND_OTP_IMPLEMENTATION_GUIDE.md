# Backend Implementation Guide: OTP-Based Invitation Acceptance

## Overview

This guide provides step-by-step instructions for completing the backend implementation of the OTP-based invitation acceptance flow with data cloning.

## Current Status

✅ **Completed:**
- Command structure created (`AcceptInvitationWithRegistrationCommand`)
- Command validator implemented with proper validation rules
- Command handler skeleton created with TODOs
- Controller endpoint added (`POST /api/v1/providers/{providerId}/hierarchy/invitations/{invitationId}/accept-with-registration`)
- Request/Response DTOs defined

⚠️ **Needs Implementation:**
- OTP verification service integration
- User account creation service
- Provider profile creation service
- Data cloning services (services, working hours, gallery)
- JWT token generation service

## Files Created

### 1. Command Files
- **Location:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/AcceptInvitationWithRegistration/`

#### AcceptInvitationWithRegistrationCommand.cs
```csharp
public sealed record AcceptInvitationWithRegistrationCommand(
    Guid InvitationId,
    Guid OrganizationId,
    string PhoneNumber,
    string FirstName,
    string LastName,
    string? Email,
    string OtpCode,
    bool CloneServices,
    bool CloneWorkingHours,
    bool CloneGallery,
    Guid? IdempotencyKey = null) : ICommand<AcceptInvitationWithRegistrationResult>;
```

#### AcceptInvitationWithRegistrationCommandValidator.cs
Validates:
- InvitationId and OrganizationId (not empty)
- PhoneNumber (format: +98XXXXXXXXXX)
- FirstName/LastName (2-50 characters)
- Email (valid format when provided)
- OtpCode (exactly 6 digits)

### 2. Controller Endpoint
- **Location:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProviderHierarchyController.cs`
- **Route:** `POST /api/v1/providers/{providerId}/hierarchy/invitations/{invitationId}/accept-with-registration`
- **Authorization:** `[AllowAnonymous]` - No authentication required (unregistered users)

## Implementation Steps

### Step 1: Build the Project

First, ensure all new files compile:

```bash
cd c:\Repos\Booking
dotnet build src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application
dotnet build src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
```

If there are compilation errors about missing namespaces, the project references are correct but the IDE needs to refresh.

### Step 2: Implement OTP Verification Service

**Service Interface to Create:**
```csharp
public interface IOtpVerificationService
{
    Task<bool> VerifyCodeAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken);
}
```

**Integration Point in CommandHandler:**
```csharp
// Line ~80-86 in AcceptInvitationWithRegistrationCommandHandler.cs
var otpValid = await _otpVerificationService.VerifyCodeAsync(
    request.PhoneNumber,
    request.OtpCode,
    cancellationToken);

if (!otpValid)
    throw new DomainValidationException("Invalid or expired OTP code");
```

### Step 2.5: Check for Existing User (CRITICAL)

**Service Interface Addition:**
```csharp
public interface IUserService
{
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken);
    Task<User> CreateUserAsync(string phoneNumber, string firstName, string lastName, string? email, CancellationToken cancellationToken);
}
```

**Integration Point in CommandHandler:**
```csharp
// Line ~92-98 in AcceptInvitationWithRegistrationCommandHandler.cs
var existingUser = await _userService.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
if (existingUser != null)
    throw new DomainValidationException(
        "A user account with this phone number already exists. " +
        "Please use the regular invitation acceptance flow instead.");
```

**Why This Is Critical:**
- Prevents duplicate user accounts with the same phone number
- Ensures registered users use the proper invitation acceptance flow (with login)
- Unregistered users use the OTP-based registration flow

**Also Add to SendInvitationCommandHandler:**
```csharp
// Line ~59-66 in SendInvitationCommandHandler.cs
var existingUser = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
if (existingUser != null)
{
    throw new DomainValidationException(
        $"A user account with phone number {request.PhoneNumber} already exists. " +
        "This user can join the organization by accepting the invitation through their account.");
}
```

### Step 3: Implement User Creation Service

**Service Interface to Create:**
```csharp
public interface IUserService
{
    Task<User> CreateUserAsync(
        string phoneNumber,
        string firstName,
        string lastName,
        string? email,
        CancellationToken cancellationToken);
}
```

**Integration Point:**
```csharp
// Line ~100 in AcceptInvitationWithRegistrationCommandHandler.cs
var user = await _userService.CreateUserAsync(
    phoneNumber: request.PhoneNumber,
    firstName: request.FirstName,
    lastName: request.LastName,
    email: request.Email,
    cancellationToken);
```

### Step 4: Implement Provider Profile Creation

**Service Interface to Create:**
```csharp
public interface IProviderService
{
    Task<Provider> CreateIndividualProviderAsync(
        Guid userId,
        string firstName,
        string lastName,
        string phoneNumber,
        string? email,
        CancellationToken cancellationToken);
}
```

**Integration Point:**
```csharp
// Line ~110 in AcceptInvitationWithRegistrationCommandHandler.cs
var individualProvider = await _providerService.CreateIndividualProviderAsync(
    userId: user.Id,
    firstName: request.FirstName,
    lastName: request.LastName,
    phoneNumber: request.PhoneNumber,
    email: request.Email,
    cancellationToken);
```

### Step 5: Implement Data Cloning Services

#### 5.1 Service Cloning

**Interface:**
```csharp
public interface IServiceCloningService
{
    Task<int> CloneServicesAsync(
        ProviderId sourceProviderId,
        ProviderId targetProviderId,
        CancellationToken cancellationToken);
}
```

**Requirements:**
- Copy all services from organization provider to individual provider
- Maintain service structure (categories, pricing, duration)
- Return count of cloned services

#### 5.2 Working Hours Cloning

**Interface:**
```csharp
public interface IWorkingHoursCloningService
{
    Task<int> CloneWorkingHoursAsync(
        ProviderId sourceProviderId,
        ProviderId targetProviderId,
        CancellationToken cancellationToken);
}
```

**Requirements:**
- Copy all working hours/availability templates
- Clone weekly schedules, breaks, holidays
- Return count of cloned working hour entries

#### 5.3 Gallery Cloning

**Interface:**
```csharp
public interface IGalleryCloningService
{
    Task<int> CloneGalleryAsync(
        ProviderId sourceProviderId,
        ProviderId targetProviderId,
        bool markAsCloned,
        CancellationToken cancellationToken);
}
```

**Requirements:**
- Copy all gallery images from organization to individual
- **IMPORTANT:** Mark images with cloning metadata:
  - `IsCloned = true`
  - `SourceProviderId = organizationProviderId`
  - `SourceType = "Organization"`
- This allows tracking where images came from
- Return count of cloned images

**Integration Points:**
```csharp
// Lines ~125-160 in AcceptInvitationWithRegistrationCommandHandler.cs
if (request.CloneServices)
{
    clonedServicesCount = await _serviceCloningService.CloneServicesAsync(
        sourceProviderId: organizationProviderId,
        targetProviderId: providerId,
        cancellationToken);
}

if (request.CloneWorkingHours)
{
    clonedWorkingHoursCount = await _workingHoursCloningService.CloneWorkingHoursAsync(
        sourceProviderId: organizationProviderId,
        targetProviderId: providerId,
        cancellationToken);
}

if (request.CloneGallery)
{
    clonedGalleryCount = await _galleryCloningService.CloneGalleryAsync(
        sourceProviderId: organizationProviderId,
        targetProviderId: providerId,
        markAsCloned: true,
        cancellationToken);
}
```

### Step 6: Implement Authentication Token Service

**Service Interface:**
```csharp
public interface IAuthTokenService
{
    Task<AuthTokens> GenerateTokensAsync(
        Guid userId,
        ProviderId providerId,
        CancellationToken cancellationToken);
}

public record AuthTokens(string AccessToken, string RefreshToken);
```

**Integration Point:**
```csharp
// Line ~195 in AcceptInvitationWithRegistrationCommandHandler.cs
var tokens = await _authTokenService.GenerateTokensAsync(
    userId: user.Id,
    providerId: providerId,
    cancellationToken);
```

### Step 7: Update Command Handler Constructor

Once all services are implemented, update the constructor:

```csharp
public AcceptInvitationWithRegistrationCommandHandler(
    IProviderReadRepository providerReadRepository,
    IProviderWriteRepository providerWriteRepository,
    IProviderInvitationReadRepository invitationReadRepository,
    IProviderInvitationWriteRepository invitationWriteRepository,
    IUnitOfWork unitOfWork,
    ILogger<AcceptInvitationWithRegistrationCommandHandler> logger,
    IOtpVerificationService otpVerificationService,
    IUserService userService,
    IProviderService providerService,
    IAuthTokenService authTokenService,
    IServiceCloningService serviceCloningService,
    IWorkingHoursCloningService workingHoursCloningService,
    IGalleryCloningService galleryCloningService)
{
    _providerReadRepository = providerReadRepository;
    _providerWriteRepository = providerWriteRepository;
    _invitationReadRepository = invitationReadRepository;
    _invitationWriteRepository = invitationWriteRepository;
    _unitOfWork = unitOfWork;
    _logger = logger;
    _otpVerificationService = otpVerificationService;
    _userService = userService;
    _providerService = providerService;
    _authTokenService = authTokenService;
    _serviceCloningService = serviceCloningService;
    _workingHoursCloningService = workingHoursCloningService;
    _galleryCloningService = galleryCloningService;
}
```

### Step 8: Register Services in DI Container

Add to your DI configuration (likely in `Program.cs` or `Startup.cs`):

```csharp
// Service registrations
services.AddScoped<IOtpVerificationService, OtpVerificationService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IProviderService, ProviderService>();
services.AddScoped<IAuthTokenService, AuthTokenService>();
services.AddScoped<IServiceCloningService, ServiceCloningService>();
services.AddScoped<IWorkingHoursCloningService, WorkingHoursCloningService>();
services.AddScoped<IGalleryCloningService, GalleryCloningService>();
```

## Testing Checklist

### Unit Tests
- [ ] Test command validator with valid/invalid inputs
- [ ] Test OTP verification success/failure scenarios
- [ ] Test user creation with duplicate phone numbers
- [ ] Test provider profile creation
- [ ] Test each cloning service independently
- [ ] Test token generation

### Integration Tests
- [ ] Test complete flow: OTP → User → Provider → Clone → Accept
- [ ] Test with invitation that doesn't exist
- [ ] Test with expired invitation
- [ ] Test with invalid OTP
- [ ] Test with phone number mismatch
- [ ] Test cloning with different combinations (services only, all data, etc.)
- [ ] Verify cloned gallery images have correct metadata
- [ ] Verify JWT tokens are valid and contain correct claims

### End-to-End Tests
- [ ] Frontend sends request → backend creates account → returns tokens
- [ ] Frontend stores tokens → redirects to dashboard
- [ ] User can see cloned services in their profile
- [ ] User can see cloned working hours
- [ ] User can see gallery images marked as "from organization"

## API Contract

### Request
```json
{
  "phoneNumber": "+989123456789",
  "firstName": "رضا",
  "lastName": "احمدی",
  "email": "reza@example.com",
  "otpCode": "123456",
  "cloneServices": true,
  "cloneWorkingHours": true,
  "cloneGallery": true
}
```

### Response (Success - 200 OK)
```json
{
  "success": true,
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "providerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "def50200...",
    "clonedServicesCount": 15,
    "clonedWorkingHoursCount": 7,
    "clonedGalleryCount": 23,
    "acceptedAt": "2025-01-27T10:30:00Z"
  }
}
```

### Error Responses

**400 Bad Request - Invalid OTP**
```json
{
  "success": false,
  "message": "Invalid or expired OTP code"
}
```

**404 Not Found - Invitation Not Found**
```json
{
  "success": false,
  "message": "Invitation with ID {id} not found"
}
```

**400 Bad Request - Phone Mismatch**
```json
{
  "success": false,
  "message": "Phone number does not match invitation"
}
```

## Next Steps

1. **Immediate:** Build the backend project to resolve IDE errors
2. **Priority 1:** Implement OTP verification (blocks testing)
3. **Priority 2:** Implement user/provider creation
4. **Priority 3:** Implement data cloning services
5. **Priority 4:** Implement JWT token generation
6. **Testing:** Test each service independently, then integration test

## Notes

- The command handler is complete except for service integrations
- All validation logic is in place
- The invitation acceptance flow follows the existing pattern
- Frontend is 100% complete and ready to consume this API
- Remember to handle idempotency (IdempotencyKey parameter)
- Consider adding rate limiting to prevent OTP brute force
- Log all operations for debugging and audit trail

## Questions?

If you need clarification on any step or encounter issues:
1. Check existing command handlers for patterns (e.g., `AcceptInvitationCommandHandler`)
2. Review domain aggregates (Provider, ProviderInvitation)
3. Check if services already exist with different names
4. Verify dependency injection is configured correctly
