# OTP Invitation Flow - Troubleshooting Guide

## Common Issues and Solutions

### 1. Frontend Shows Error Despite Backend Success

**Symptom:** Backend returns 200 OK but frontend displays error message

**Diagnosis Steps:**
1. Check browser console for the actual response
2. Look for `response.data: { ... }` in console logs
3. Check if `response.data?.data` is `undefined`

**Common Causes:**

#### Response Structure Mismatch
```javascript
// If you see this in console:
response.data: {userId: '...', providerId: '...', ...}
response.data?.data: undefined

// The service is trying to unwrap twice when it should unwrap once
```

**Solution:**
```typescript
// Change from:
return response.data!.data!

// To:
return response.data!
```

#### Type Mismatch
If TypeScript expects `HierarchyApiResponse<T>` but backend returns `T` directly:
```typescript
// Change the return type
async method(): Promise<T> {  // Not Promise<HierarchyApiResponse<T>>
  const response = await client.post<T>(...);
  return response.data!;
}
```

### 2. Phone Number Not Displaying Correctly

**Symptom:** Phone number shows as "+989123135149" instead of "0912 313 5149"

**Diagnosis:**
1. Check if `formatPhone` function is being called
2. Verify phone number format in invitation data

**Solution:**
```typescript
function formatPhone(phoneNumber?: string): string {
  if (!phoneNumber) return ''

  // Remove +98 prefix
  const cleaned = phoneNumber.replace(/^\+98/, '')

  // Format as: 0912 XXX XXXX
  if (cleaned.length === 10 && cleaned.startsWith('9')) {
    return `0${cleaned.substring(0, 3)} ${cleaned.substring(3, 6)} ${cleaned.substring(6)}`
  }

  return phoneNumber
}
```

### 3. OTP Verification Always Fails

**Current Status:** OTP verification is a placeholder (always passes)

**When implementing real OTP verification:**
- Check OTP expiration (typically 2-5 minutes)
- Verify code format (exactly 6 digits)
- Implement rate limiting to prevent brute force
- Clear OTP after successful verification

### 4. User/Provider IDs Are Not Persisted

**Current Status:** User and provider creation are placeholders (generate temp GUIDs)

**When implementing:**
- Ensure user is saved to database before creating provider
- Link provider to user via UserId foreign key
- Return the actual database-generated IDs

### 5. Data Cloning Returns 0 Counts

**Current Status:** Cloning services are placeholders (return 0)

**When implementing:**
- Verify source provider has data to clone
- Check permissions/ownership before cloning
- Log cloning operations for debugging
- Return actual counts of cloned items

### 6. JWT Tokens Don't Work for Authentication

**Current Status:** JWT tokens are placeholders (format: `temp_access_token_{userId}`)

**When implementing:**
- Use proper JWT library (System.IdentityModel.Tokens.Jwt)
- Include required claims: userId, providerId, roles
- Sign with secure secret key
- Set appropriate expiration times
- Implement refresh token rotation

## Validation Errors

### Phone Number Format
**Error:** "Phone number must be in format +98XXXXXXXXXX"

**Fix:** Ensure phone number:
- Starts with +98
- Has exactly 10 digits after +98
- Example: +989123135149

### OTP Code Format
**Error:** "OTP code must be exactly 6 digits"

**Fix:** OTP code must be:
- Exactly 6 characters
- All digits (0-9)
- No spaces or special characters

### Name Validation
**Error:** "FirstName must be between 2 and 50 characters"

**Fix:** Ensure:
- Minimum 2 characters
- Maximum 50 characters
- Not empty or whitespace only

## Network Issues

### CORS Errors
**Solution:** Ensure backend CORS policy allows:
- Origin: Frontend URL
- Methods: POST
- Headers: Content-Type, Authorization

### 404 Not Found
**Diagnosis:**
1. Check API is running
2. Verify endpoint URL matches controller route
3. Ensure API was restarted after adding new endpoint

### 401 Unauthorized
**Note:** This endpoint uses `[AllowAnonymous]`

If you get 401:
1. Check if attribute is present on controller action
2. Verify authentication middleware isn't blocking anonymous access
3. Check if endpoint route is correct

## Database Issues

### Invitation Not Found
**Check:**
1. Invitation ID is valid GUID
2. Invitation exists in database
3. Invitation status is "Pending"
4. Invitation hasn't expired

### Phone Number Mismatch
**Error:** "Phone number does not match invitation"

**Cause:** The phone number in the request doesn't match the invitation's phone number

**Fix:** Ensure request uses the exact phone number from invitation (including +98 prefix)

### Invitation Already Accepted
**Error:** "Invitation is no longer pending"

**Cause:** Invitation was already accepted, rejected, or expired

**Fix:** Check invitation status in database

## Frontend Debugging

### Enable Detailed Logging
Add to service method:
```typescript
console.log('Request:', request)
console.log('Response:', response)
console.log('Response.data:', response.data)
```

### Check Response Structure
```typescript
// Log the full response to understand its structure
console.log('Full axios response:', JSON.stringify(response, null, 2))
```

### Verify Form Data
```typescript
// Before sending request
console.log('Form data:', {
  firstName: registrationForm.firstName,
  lastName: registrationForm.lastName,
  email: registrationForm.email,
  otpCode: otpCode.value,
  cloneServices: registrationForm.cloneServices,
  cloneWorkingHours: registrationForm.cloneWorkingHours,
  cloneGallery: registrationForm.cloneGallery
})
```

## Backend Debugging

### Check Command Handler Logs
```csharp
_logger.LogInformation("Processing invitation {InvitationId}", request.InvitationId);
_logger.LogInformation("OTP verification successful for {PhoneNumber}", request.PhoneNumber);
_logger.LogInformation("User account created with ID {UserId}", userId);
```

### Verify Database Updates
```sql
-- Check invitation status
SELECT * FROM ProviderInvitations WHERE Id = @invitationId;

-- Check if user was created (when implemented)
SELECT * FROM Users WHERE PhoneNumber = @phoneNumber;

-- Check if provider was created (when implemented)
SELECT * FROM Providers WHERE Id = @providerId;
```

## Performance Issues

### Slow OTP Entry
**Check:**
- Console for excessive re-renders
- Remove any console.log in frequently-called functions (like formatPhone)
- Verify computed properties are memoized

### Slow API Response
**Monitor:**
- Database query performance
- External service calls (OTP verification, SMS)
- Data cloning operations (can be slow with many items)

## Production Readiness Checklist

Before deploying to production:

- [ ] Replace all placeholder implementations with real services
- [ ] Implement phone number existence validation (CRITICAL - see PHONE_NUMBER_VALIDATION_REQUIREMENTS.md)
- [ ] Add proper error handling and logging
- [ ] Implement rate limiting for OTP verification
- [ ] Use real JWT tokens with secure signing
- [ ] Test all cloning operations with real data
- [ ] Add database indexes for performance
- [ ] Test with various phone number formats
- [ ] Verify all Persian error messages display correctly
- [ ] Test OTP expiration handling
- [ ] Implement idempotency key handling
- [ ] Add monitoring and alerting

## Need More Help?

See also:
- [OTP_IMPLEMENTATION_SUMMARY.md](./OTP_IMPLEMENTATION_SUMMARY.md) - Complete implementation status
- [BACKEND_OTP_IMPLEMENTATION_GUIDE.md](./BACKEND_OTP_IMPLEMENTATION_GUIDE.md) - Detailed backend guide
- [PHONE_NUMBER_VALIDATION_REQUIREMENTS.md](./PHONE_NUMBER_VALIDATION_REQUIREMENTS.md) - Critical validation requirements
- [STAFF_INVITATION_OTP_IMPLEMENTATION.md](./STAFF_INVITATION_OTP_IMPLEMENTATION.md) - Original specification
