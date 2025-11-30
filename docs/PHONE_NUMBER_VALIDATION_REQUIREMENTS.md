# Phone Number Validation Requirements

## Overview

To prevent duplicate accounts and ensure proper user flows, phone number validation is **CRITICAL** at multiple points in the invitation system.

## Current Status

### ✅ Implemented Validations

1. **Duplicate Pending Invitations** (Already Working)
   - **Location:** `SendInvitationCommandHandler.cs:51-55`
   - **Check:** Prevents sending multiple pending invitations to the same phone number from the same organization
   - **Error:** "A pending invitation already exists for phone number {phoneNumber}"

### ⚠️ Missing Validations (TODO Comments Added)

2. **Phone Number Already Registered - Send Invitation** (CRITICAL - Not Yet Implemented)
   - **Location:** `SendInvitationCommandHandler.cs:57-66` (TODO comment added)
   - **Check:** Before sending invitation, verify phone number doesn't belong to existing user
   - **Error:** "A user account with phone number {phoneNumber} already exists. This user can join the organization by accepting the invitation through their account."
   - **Impact:** Without this, you can send invitations to registered users who should use the login-based flow instead

3. **Phone Number Already Registered - Accept with Registration** (CRITICAL - Not Yet Implemented)
   - **Location:** `AcceptInvitationWithRegistrationCommandHandler.cs:90-98` (TODO comment added)
   - **Check:** Before creating new account, verify phone number isn't already registered
   - **Error:** "A user account with this phone number already exists. Please use the regular invitation acceptance flow instead."
   - **Impact:** Without this, duplicate user accounts can be created with the same phone number

## Implementation Requirements

### Step 1: Create User Lookup Interface

```csharp
public interface IUserService
{
    /// <summary>
    /// Find user by phone number (returns null if not found)
    /// </summary>
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Create new user account
    /// </summary>
    Task<User> CreateUserAsync(
        string phoneNumber,
        string firstName,
        string lastName,
        string? email,
        CancellationToken cancellationToken);
}
```

### Step 2: Add Validation to SendInvitationCommandHandler

**Before line 68 in SendInvitationCommandHandler.cs:**

```csharp
// Check if user with this phone number already exists
var existingUser = await _userService.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
if (existingUser != null)
{
    // If user exists, they should use the regular join request flow instead
    throw new DomainValidationException(
        $"A user account with phone number {request.PhoneNumber} already exists. " +
        "This user can join the organization by accepting the invitation through their account.");
}

_logger.LogInformation("Phone number {PhoneNumber} is not registered, proceeding with invitation", request.PhoneNumber);
```

### Step 3: Add Validation to AcceptInvitationWithRegistrationCommandHandler

**Before line 100 in AcceptInvitationWithRegistrationCommandHandler.cs:**

```csharp
// Check if user with this phone number already exists
var existingUser = await _userService.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
if (existingUser != null)
{
    throw new DomainValidationException(
        "A user account with this phone number already exists. " +
        "Please use the regular invitation acceptance flow instead.");
}

_logger.LogInformation("Phone number {PhoneNumber} is available for registration", request.PhoneNumber);
```

## User Flow Scenarios

### Scenario 1: Unregistered User (OTP Flow) ✅ Correct

1. Organization sends invitation to `+989121234567`
2. Check: Phone number doesn't exist ✅
3. SMS sent with invitation link
4. User opens link → sees registration form
5. User enters OTP code
6. Check: Phone number still doesn't exist ✅
7. Create new user account
8. Create provider profile
9. Clone data
10. Accept invitation

### Scenario 2: Registered User (Login Flow) ⚠️ Needs Implementation

1. Organization tries to send invitation to `+989121234567`
2. Check: Phone number exists ❌ **VALIDATION MISSING**
3. **WITHOUT VALIDATION:** SMS sent (wrong flow!)
4. **WITH VALIDATION:** Error - "User already exists, they should login and accept"

**Correct Flow for Registered Users:**
- Registered user should login to their account
- Navigate to invitations section
- See pending invitation
- Click "Accept" (no registration needed, they already have an account)
- Their existing provider profile gets linked to the organization

### Scenario 3: Duplicate Account Attempt ⚠️ Needs Implementation

1. User receives invitation to `+989121234567`
2. Before accepting, user registers through normal signup flow
3. Later, user tries to accept invitation with OTP registration
4. Check: Phone number exists ❌ **VALIDATION MISSING**
5. **WITHOUT VALIDATION:** Creates duplicate account!
6. **WITH VALIDATION:** Error - "Account exists, please login to accept"

## Error Messages (Persian)

For frontend display:

```typescript
// In SendInvitationCommandHandler
"حساب کاربری با این شماره موبایل از قبل وجود دارد. این کاربر می‌تواند با ورود به حساب خود، دعوت را قبول کند."

// In AcceptInvitationWithRegistrationCommandHandler
"حساب کاربری با این شماره موبایل از قبل وجود دارد. لطفاً برای قبول دعوت، وارد حساب کاربری خود شوید."
```

## Testing Checklist

### Unit Tests

- [ ] Test SendInvitation with non-existent phone number (should succeed)
- [ ] Test SendInvitation with existing phone number (should fail)
- [ ] Test AcceptWithRegistration with non-existent phone number (should succeed)
- [ ] Test AcceptWithRegistration with existing phone number (should fail)

### Integration Tests

- [ ] Register user → Send invitation to same phone → Should get validation error
- [ ] Send invitation → Register user → Try accept with OTP → Should get validation error
- [ ] Send invitation to unregistered number → Accept with OTP → Should create account
- [ ] Send invitation to registered user → User logs in → Accepts via account → Should link provider

### Edge Cases

- [ ] Phone number format variations (+98 vs 98 vs 0)
- [ ] Case sensitivity (shouldn't matter for phone numbers)
- [ ] Concurrent registration attempts (race condition)
- [ ] User registered between invitation send and acceptance

## Database Considerations

### Required Index

Ensure fast phone number lookups:

```sql
CREATE INDEX IX_Users_PhoneNumber ON Users(PhoneNumber);
```

This is critical for performance when checking if phone number exists.

## Security Considerations

1. **Rate Limiting:** Prevent enumeration attacks by limiting phone number checks
2. **Error Messages:** Don't reveal whether a phone number is registered (for security)
   - Consider: "Unable to send invitation to this number"
   - Instead of: "This phone number is already registered"
3. **Phone Normalization:** Ensure +98XXXXXXXXXX format for consistency

## Implementation Priority

| Priority | Task | Reason |
|----------|------|--------|
| **P0** | Implement IUserService.GetByPhoneNumberAsync | Blocks all validations |
| **P0** | Add validation to AcceptInvitationWithRegistrationCommandHandler | Prevents duplicate accounts |
| **P1** | Add validation to SendInvitationCommandHandler | Better UX, prevents wrong flow |
| **P2** | Add database index on Users.PhoneNumber | Performance |
| **P3** | Update error messages for security | Security hardening |

## Summary

**Current Risk:** Without phone number existence validation, the system can:
- Create duplicate user accounts
- Send invitations to registered users (who should login instead)
- Create confusing user experiences

**Solution:**
1. Implement `IUserService.GetByPhoneNumberAsync`
2. Add validation checks at both invitation send and acceptance points
3. Test thoroughly with all scenarios

**Time Estimate:** 2-3 hours for implementation + testing

The TODO comments have already been added to the codebase. Implementation can proceed when the User service is ready.
