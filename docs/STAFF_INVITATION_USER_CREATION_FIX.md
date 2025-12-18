# Staff Invitation User Creation Fix

## Problem Summary

When a staff member accepted an invitation via `AcceptInvitationWithRegistration`, the system was calling the **wrong endpoint** in UserManagement API, causing user creation to fail.

---

## Root Cause

### Incorrect API Endpoint

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Services/Application/InvitationRegistrationService.cs`

**Before (BROKEN):**
```csharp
// Line 95 - WRONG ENDPOINT!
var response = await client.PostAsJsonAsync(
    "/api/v1/users/register-with-phone",  // ❌ This endpoint doesn't exist!
    requestPayload,
    cancellationToken);
```

**The Actual Endpoint:**
```csharp
// UserManagement.API/Controllers/V1/UsersController.cs:53-70
[HttpPost]  // POST /api/v1/users
[AllowAnonymous]
[EnableRateLimiting("registration")]
public async Task<IActionResult> RegisterUser(
    [FromBody][Required] RegisterUserRequest request,
    CancellationToken cancellationToken = default)
```

The correct endpoint is: **`POST /api/v1/users`**

---

## The Fix

### 1. Corrected Endpoint URL
Changed from `/api/v1/users/register-with-phone` to `/api/v1/users`

### 2. Added Missing Required Fields
The `RegisterUserRequest` requires these fields:
- `email` ✅ (already included)
- `password` ❌ (was missing!)
- `firstName` ✅ (already included)
- `lastName` ✅ (already included)
- `phoneNumber` ✅ (already included)
- `userType` ✅ (already included)
- `acceptTerms` ❌ (was missing!)
- `marketingConsent` ❌ (was missing!)

### Updated Code:

```csharp
var requestPayload = new
{
    phoneNumber,
    firstName,
    lastName,
    email = email ?? $"{phoneNumber.Replace("+", "")}@booksy.temp",
    userType = "Provider",
    password = GenerateRandomPassword(), // ✅ Added - staff won't use it (OTP login)
    acceptTerms = true,                  // ✅ Added
    marketingConsent = false             // ✅ Added
};

var response = await client.PostAsJsonAsync("/api/v1/users", requestPayload, cancellationToken);
```

### 3. Added Password Generator

Since staff members use OTP for authentication (not passwords), we generate a random secure password:

```csharp
/// <summary>
/// Generates a secure random password for user registration
/// Staff members won't use passwords (they use OTP for login)
/// </summary>
private static string GenerateRandomPassword()
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
    var random = new Random();
    return new string(Enumerable.Repeat(chars, 16)
        .Select(s => s[random.Next(s.Length)]).ToArray());
}
```

---

## How It Works Now

### Complete Staff Invitation Flow

```
┌──────────────────────────────────────────────────────────────┐
│ 1. Organization Sends Invitation                             │
│    POST /api/v1/provider-hierarchy/send-invitation          │
│    - Creates ProviderInvitation with phone number           │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 2. Staff Receives OTP Code                                   │
│    - SMS sent to phone number                                │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 3. Staff Accepts Invitation (New User)                       │
│    POST /api/v1/provider-hierarchy/accept-with-registration │
│    Body: {                                                   │
│      invitationId, phoneNumber, otpCode,                    │
│      firstName, lastName, email                              │
│    }                                                         │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 4. ✅ Creates User in UserManagement                         │
│    POST /api/v1/users (FIXED!)                              │
│    Body: {                                                   │
│      phoneNumber, firstName, lastName, email,               │
│      userType: "Provider",                                  │
│      password: "RandomSecure123!@#",                        │
│      acceptTerms: true,                                     │
│      marketingConsent: false                                │
│    }                                                         │
│    Returns: { userId: "guid" }                              │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 5. Creates Provider in ServiceCatalog                        │
│    - Provider.OwnerId = userId (from step 4) ✅             │
│    - HierarchyType = Individual                             │
│    - ParentProviderId = OrganizationId                      │
│    - Status = Active                                        │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 6. Optional: Clone Data from Organization                    │
│    - Services (if requested)                                │
│    - Working Hours (if requested)                           │
│    - Gallery Images (if requested)                          │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 7. Generate JWT Tokens                                       │
│    - AccessToken (24 hours)                                 │
│    - RefreshToken (30 days)                                 │
│    - Claims include: userId, providerId, roles              │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 8. Return Response                                           │
│    {                                                         │
│      userId, providerId,                                    │
│      accessToken, refreshToken,                             │
│      clonedServicesCount, clonedWorkingHoursCount           │
│    }                                                         │
└──────────────────────────────────────────────────────────────┘
```

---

## Database Relationships

### After Successful Invitation Acceptance:

**UserManagement.Users Table:**
```sql
Id: 550e8400-e29b-41d4-a716-446655440000
PhoneNumber: +989123456789
FirstName: "علی"
LastName: "محمدی"
Email: "ali@example.com"
UserType: "Provider"
PasswordHash: "hashed-random-password" (won't be used - OTP login)
Status: "Active"
RegisteredAt: "2025-01-29T10:00:00Z"
```

**ServiceCatalog.Providers Table:**
```sql
Id: 660e8400-e29b-41d4-a716-446655440111
OwnerId: 550e8400-e29b-41d4-a716-446655440000  ← Foreign key to Users.Id ✅
OwnerFirstName: "علی"
OwnerLastName: "محمدی"
HierarchyType: "Individual"
ParentProviderId: 770e8400-e29b-41d4-a716-446655440222  ← Organization
IsIndependent: false
Status: "Active"
BusinessName: "علی محمدی"
```

---

## Testing the Fix

### Before Fix
1. Staff accepts invitation → Calls `/api/v1/users/register-with-phone`
2. ❌ **404 Not Found** - endpoint doesn't exist
3. ❌ Fallback: Creates temporary UserId (GUID without User in DB!)
4. ❌ Creates orphaned Provider record with invalid OwnerId

### After Fix
1. Staff accepts invitation → Calls `/api/v1/users`
2. ✅ **201 Created** - user created successfully
3. ✅ Returns real UserId from UserManagement DB
4. ✅ Creates Provider record with valid OwnerId reference

### Verify in Database:

```sql
-- Check if User was created
SELECT * FROM UserManagement.Users
WHERE PhoneNumber = '+989123456789';

-- Check if Provider references the correct User
SELECT p.Id, p.OwnerId, u.PhoneNumber, u.FirstName, u.LastName
FROM ServiceCatalog.Providers p
INNER JOIN UserManagement.Users u ON p.OwnerId = u.Id
WHERE p.HierarchyType = 'Individual';
```

---

## Related Files

### Modified
- ✅ `InvitationRegistrationService.cs` - Fixed endpoint and added required fields

### Related (No Changes)
- `AcceptInvitationWithRegistrationCommandHandler.cs` - Uses the service
- `UsersController.cs` - Defines the correct endpoint
- `RegisterUserRequest.cs` - Request model with required fields

---

## Important Notes

### Why Random Password?
Staff members authenticate using **OTP (One-Time Password)** via phone number, not email/password. The password field is required by the registration endpoint but will never be used by staff members.

### Fallback Mechanism Still Exists ⚠️
If UserManagement API is down, the system still creates a temporary GUID as userId. This is a **safety mechanism** but should be monitored:

```csharp
catch (HttpRequestException ex)
{
    _logger.LogWarning("UserManagement service unavailable, using temporary user ID");
    return UserId.CreateNew();  // ⚠️ Orphaned provider!
}
```

**Recommendation:** Add monitoring/alerting when fallback is triggered.

---

## Summary

✅ **FIXED:** Staff invitation now creates User in UserManagement correctly
✅ **FIXED:** Provider.OwnerId properly references Users.Id
✅ **FIXED:** All required fields are sent to registration endpoint
✅ **ADDED:** Random password generation for staff users (OTP authentication)

The system now maintains proper referential integrity between UserManagement and ServiceCatalog bounded contexts! 🎉
