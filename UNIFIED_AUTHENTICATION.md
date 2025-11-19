# Unified Authentication System - Implementation Guide

## Overview

This document describes the unified authentication system that combines phone verification, user creation, and Customer/Provider aggregate creation into a single atomic operation.

## Architecture

### Before (Old 3-Step Flow)
```
1. POST /api/v1/phone-verification/send-code
   → Returns verificationId

2. POST /api/v1/phone-verification/verify-code
   → Verifies OTP, returns success

3. POST /api/v1/phone-verification/register
   → Creates User + Customer/Provider, returns tokens
```

**Problems:**
- Multiple API calls required
- Not atomic - failures could leave incomplete state
- Customer aggregate creation was manual and often forgotten
- Confusing for frontend developers

### After (New 2-Step Flow)
```
1. POST /api/v1/auth/send-verification-code
   → Returns verificationId

2. POST /api/v1/auth/customer/complete-authentication
   OR
   POST /api/v1/auth/provider/complete-authentication
   → Verifies OTP + Creates User + Creates Customer/Provider + Returns tokens
   → All in ONE atomic transaction
```

**Benefits:**
- ✅ Simpler - One API call instead of two for authentication
- ✅ Atomic - Everything happens in one transaction
- ✅ Automatic Customer Creation - Customer aggregate is automatically created on first login
- ✅ Type-Safe - Separate endpoints for Customer vs Provider
- ✅ Better UX - Faster authentication flow

---

## Backend Implementation

### 1. New Command Handlers

#### CompleteCustomerAuthenticationCommandHandler
**Location:** `src/UserManagement/Booksy.UserManagement.Application/CQRS/Commands/PhoneVerification/CompleteCustomerAuthentication/`

**Responsibilities:**
1. Verify OTP code by calling `VerifyPhoneCommand`
2. Lookup or create User entity
3. **Automatically create Customer aggregate** if it doesn't exist
4. Generate JWT tokens with customer claims
5. Return authentication response

**Key Code:**
```csharp
// Step 1: Verify OTP
var verifyCommand = new VerifyPhoneCommand(verification.Id.Value, request.Code);
var verifyResult = await _mediator.Send(verifyCommand, cancellationToken);

// Step 2: Get or create User
var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber.Value, cancellationToken);
if (user == null)
{
    user = await CreateNewCustomerUser(request, phoneNumber, cancellationToken);
}

// Step 3: AUTOMATICALLY create Customer aggregate
var customer = await _customerRepository.GetByUserIdAsync(user.Id, cancellationToken);
if (customer == null)
{
    customer = Customer.Create(user.Id, user.Profile);
    await _customerRepository.SaveAsync(customer, cancellationToken);
}

// Step 4: Generate tokens
var accessToken = _jwtTokenService.GenerateAccessToken(userId: user.Id, userType: UserType.Customer, ...);
```

#### CompleteProviderAuthenticationCommandHandler
**Location:** `src/UserManagement/Booksy.UserManagement.Application/CQRS/Commands/PhoneVerification/CompleteProviderAuthentication/`

**Similar to Customer handler, but:**
- Queries ServiceCatalog for provider information
- Includes `providerId` and `providerStatus` in JWT claims
- Returns `requiresOnboarding` flag for new providers

### 2. Controller Endpoints

**Location:** `src/UserManagement/Booksy.UserManagement.API/Controllers/V1/AuthController.cs`

#### Customer Authentication
```csharp
[HttpPost("customer/complete-authentication")]
public async Task<IActionResult> CompleteCustomerAuthentication(
    [FromBody] CompleteCustomerAuthenticationRequest request,
    CancellationToken cancellationToken = default)
```

**Request:**
```json
{
  "phoneNumber": "+989121234567",
  "code": "123456",
  "firstName": "علی",      // Optional
  "lastName": "محمدی",     // Optional
  "email": "ali@example.com" // Optional
}
```

**Response:**
```json
{
  "isNewCustomer": true,
  "userId": "guid",
  "customerId": "guid",
  "phoneNumber": "+989121234567",
  "email": null,
  "fullName": "علی محمدی",
  "accessToken": "jwt-token",
  "refreshToken": "refresh-token",
  "expiresIn": 86400,
  "tokenType": "Bearer",
  "message": "ثبت‌نام شما با موفقیت انجام شد!"
}
```

#### Provider Authentication
```csharp
[HttpPost("provider/complete-authentication")]
public async Task<IActionResult> CompleteProviderAuthentication(
    [FromBody] CompleteProviderAuthenticationRequest request,
    CancellationToken cancellationToken = default)
```

**Response includes additional fields:**
```json
{
  "isNewProvider": false,
  "userId": "guid",
  "providerId": "guid",
  "providerStatus": "Active",
  "requiresOnboarding": false,
  "accessToken": "jwt-token",
  "refreshToken": "refresh-token",
  ...
}
```

---

## Frontend Implementation

### 1. API Layer

**Location:** `booksy-frontend/src/modules/auth/api/phoneVerification.api.ts`

#### New Methods
```typescript
async completeCustomerAuthentication(
  request: CompleteCustomerAuthenticationRequest
): Promise<ApiResponse<CompleteCustomerAuthenticationResponse>>

async completeProviderAuthentication(
  request: CompleteProviderAuthenticationRequest
): Promise<ApiResponse<CompleteProviderAuthenticationResponse>>
```

#### Deprecated Methods
```typescript
// ⚠️ DEPRECATED - Will throw error
async verifyCode()
async registerFromVerifiedPhone()
```

### 2. Composable

**Location:** `booksy-frontend/src/modules/auth/composables/usePhoneVerification.ts`

#### New Methods
```typescript
const completeCustomerAuthentication = async (
  code: string,
  userInfo?: { firstName?: string; lastName?: string; email?: string }
) => {
  // 1. Calls unified API endpoint
  // 2. Stores tokens in authStore
  // 3. Stores user info in authStore
  // 4. Returns result with isNewCustomer flag
}

const completeProviderAuthentication = async (
  code: string,
  userInfo?: { firstName?: string; lastName?: string; email?: string }
) => {
  // Similar to customer, but returns provider-specific fields
}
```

### 3. View Component

**Location:** `booksy-frontend/src/modules/auth/views/VerificationView.vue`

**Updated Logic:**
```typescript
const verifyOtp = async () => {
  // Get userType from route query params
  const userType = route.query.userType as 'Customer' | 'Provider'

  // Call appropriate authentication method
  let result
  if (userType === 'Customer') {
    result = await completeCustomerAuthentication(otpCode.value)
  } else {
    result = await completeProviderAuthentication(otpCode.value)
  }

  if (result.success) {
    // Tokens and user info already stored by composable
    // Just redirect based on user type
    if (userType === 'Provider' && result.requiresOnboarding) {
      await router.push({ name: 'ProviderOnboarding' })
    } else {
      await redirectBasedOnProviderStatus()
    }
  }
}
```

---

## Technical Fixes

### 1. EF Core Owned Entity Configuration

**Problem:** `PhoneNumber` value object was configured as owned entity, causing EF Core to try creating a back-reference navigation property.

**Error:**
```
No backing field could be found for property 'PhoneVerification.PhoneNumber#PhoneNumber.PhoneVerificationId'
```

**Solution 1:** Added `WithOwner()` to prevent back-reference
```csharp
// PhoneVerificationConfiguration.cs
builder.OwnsOne(v => v.PhoneNumber, pn =>
{
    pn.Property(p => p.Value).HasColumnName("phone_number");
    // ... other properties
    pn.WithOwner(); // ✅ Prevents EF Core from creating back-reference
});
```

**Solution 2:** Compare value properties in queries
```csharp
// PhoneVerificationRepository.cs - BEFORE (Wrong)
.Where(v => v.PhoneNumber == phoneNumber && v.ExpiresAt > DateTime.UtcNow)

// AFTER (Correct)
.Where(v => v.PhoneNumber.Value == phoneNumber.Value && v.ExpiresAt > DateTime.UtcNow)
```

### 2. Nested Transaction Error

**Problem:** When `CompleteCustomerAuthenticationCommandHandler` calls `_mediator.Send(VerifyPhoneCommand)`, both handlers tried to create transactions, causing:
```
The connection is already in a transaction and cannot participate in another transaction.
```

**Root Cause:** `EfCoreUnitOfWork.ExecuteInTransactionAsync()` was creating local transaction variables not tracked in `_currentTransaction`, so `HasActiveTransaction` always returned false.

**Solution 1:** Remove `SaveChangesAsync()` from nested handler
```csharp
// VerifyPhoneCommandHandler.cs
// Update the verification (but don't save yet - let the calling handler decide)
await _repository.UpdateAsync(verification, cancellationToken);
// NOTE: SaveChangesAsync is NOT called here to allow this command to be used
// within other handlers that manage their own transaction scope
```

**Solution 2:** Fix `EfCoreUnitOfWork` to track active transactions
```csharp
// EfCoreUnitOfWork.cs
public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, ...)
{
    // Check if transaction is already active
    if (_currentTransaction != null)
    {
        _logger.LogDebug("Transaction already active, reusing existing transaction");
        return await operation(); // ✅ Reuse existing transaction
    }

    _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    try
    {
        var result = await operation();
        await CommitAndPublishEventsAsync(cancellationToken);
        await _currentTransaction.CommitAsync(cancellationToken);
        return result;
    }
    finally
    {
        _currentTransaction?.Dispose();
        _currentTransaction = null; // ✅ Clear after commit/rollback
    }
}
```

---

## Migration Guide

### Backend Checklist
- [x] Create `CompleteCustomerAuthenticationCommand` and handler
- [x] Create `CompleteProviderAuthenticationCommand` and handler
- [x] Add new endpoints to `AuthController`
- [x] Fix `PhoneVerificationConfiguration` with `WithOwner()`
- [x] Fix `PhoneVerificationRepository` to compare value properties
- [x] Fix `VerifyPhoneCommandHandler` to not call `SaveChangesAsync`
- [x] Fix `EfCoreUnitOfWork` to track active transactions
- [x] Deprecate old `PhoneVerificationController` endpoints

### Frontend Checklist
- [x] Update `phoneVerification.api.ts` with new methods
- [x] Add new types to `phoneVerification.types.ts`
- [x] Update `usePhoneVerification.ts` composable
- [x] Update `VerificationView.vue` to use new flow
- [x] Add deprecation warnings to old methods
- [ ] Update integration tests
- [ ] Test customer login/registration flow
- [ ] Test provider login/registration flow

---

## Testing

### Manual Testing

#### Customer Flow
1. Go to customer login page
2. Enter phone number → Receive OTP
3. Enter OTP code → Should automatically:
   - Verify phone
   - Create User (if new)
   - Create Customer aggregate (if new)
   - Return tokens
   - Redirect to home page

#### Provider Flow
1. Go to provider login page
2. Enter phone number → Receive OTP
3. Enter OTP code → Should automatically:
   - Verify phone
   - Create User (if new)
   - Query ServiceCatalog for provider info
   - Return tokens with provider claims
   - Redirect to dashboard or onboarding

### Database Verification

After authentication, verify:
```sql
-- Check User was created
SELECT * FROM user_management.users WHERE phone_number = '+989121234567';

-- Check Customer aggregate was created
SELECT * FROM user_management.customers WHERE user_id = '<user-id>';

-- Check PhoneVerification was marked as verified
SELECT * FROM user_management.phone_verifications
WHERE phone_number = '+989121234567'
ORDER BY created_at DESC LIMIT 1;
```

---

## Error Handling

### Common Errors

#### 1. "No verification found for this phone number"
**Cause:** User didn't request OTP first, or OTP expired
**Solution:** Request new OTP code

#### 2. "Invalid verification code"
**Cause:** Wrong OTP code entered
**Solution:** Check code and retry (limited attempts)

#### 3. "This phone number is registered as Provider"
**Cause:** Using customer endpoint for a provider phone number
**Solution:** Use correct endpoint for user type

#### 4. "Maximum attempts reached"
**Cause:** Too many failed OTP attempts
**Solution:** Wait 1 hour or request new code

---

## Performance Considerations

### Transaction Scope
All operations happen in a single transaction:
- Verify phone
- Create/update User
- Create Customer/Provider aggregate
- Publish domain events

**Benefits:**
- Atomic - all or nothing
- Consistent - no partial states
- Isolated - concurrent operations don't interfere

**Trade-offs:**
- Slightly longer transaction duration
- Database locks held longer
- Acceptable for authentication flow (infrequent operation)

### Database Queries
Each authentication performs:
1. `SELECT` phone verification by phone number (1 query)
2. `UPDATE` phone verification status (1 query)
3. `SELECT` user by phone number (1 query)
4. `INSERT` user (if new) (1 query)
5. `SELECT` customer by user ID (1 query)
6. `INSERT` customer (if new) (1 query)

**Total:** 4-6 queries depending on if user/customer exist

---

## Security Considerations

### 1. OTP Security
- OTP stored as SHA256 hash in database
- Limited verification attempts (default: 5)
- Account blocked for 1 hour after max attempts
- OTP expires after 10 minutes

### 2. Rate Limiting
Apply rate limiting to authentication endpoints:
```csharp
[RateLimit(MaxRequests = 5, WindowSeconds = 60)]
[HttpPost("customer/complete-authentication")]
```

### 3. JWT Claims
Customer token includes:
- `sub`: UserId
- `userType`: "Customer"
- `email`: User's email
- `phoneNumber`: Verified phone
- Custom claims for authorization

Provider token additionally includes:
- `providerId`: Provider aggregate ID
- `providerStatus`: Active/Pending/Suspended

---

## Troubleshooting

### Backend

#### Build Errors
```bash
# Check for compilation errors
dotnet build Booksy.sln

# Common issues:
# - Property name mismatches (user.UserType vs user.Type)
# - Namespace conflicts (Customer.Create vs Commands.Customer)
# - Missing using statements
```

#### Runtime Errors
```bash
# Enable detailed logging
"Logging": {
  "LogLevel": {
    "Booksy.UserManagement": "Debug",
    "Booksy.Infrastructure.Core": "Debug"
  }
}
```

### Frontend

#### Type Errors
```bash
# Run type checking
cd booksy-frontend
npm run type-check

# Common issues:
# - Missing response properties
# - Wrong property types
# - Deprecated method usage
```

#### API Errors
Check browser console for:
- Deprecation warnings
- API error responses
- Network failures

---

## Future Improvements

### Potential Enhancements
1. **Social Login Integration** - Add OAuth providers (Google, Apple)
2. **Biometric Authentication** - Support fingerprint/face ID
3. **Multi-Factor Authentication** - Optional second factor
4. **Email Verification** - Send verification link to email
5. **SMS Provider Fallback** - Multiple SMS providers for reliability

### Code Quality
1. Add integration tests for authentication flow
2. Add unit tests for command handlers
3. Add E2E tests for frontend flow
4. Implement proper retry logic for SMS sending
5. Add telemetry and monitoring

---

## References

- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [EF Core Owned Types](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities)
- [JWT Best Practices](https://datatracker.ietf.org/doc/html/rfc8725)
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)

---

**Last Updated:** 2025-11-19
**Version:** 1.0.0
**Author:** Development Team
