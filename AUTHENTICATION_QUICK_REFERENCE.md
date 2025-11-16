# Authentication Flow - Quick Reference

## TL;DR

**Customer Booking Flow**:
```
Provider Page → Book Now → /login?redirect=/bookings/new → Phone Verification → Register as Customer → Redirect to Booking
```

**Provider Registration Flow**:
```
/login (direct) → Phone Verification → Register as Provider → /registration → Complete Profile → Dashboard
```

---

## Key Files Reference

### Frontend

| File | Purpose | Key Functions |
|------|---------|---------------|
| `LoginView.vue` | Phone entry, user type detection | `handleSubmit()` - detects Customer vs Provider |
| `VerificationView.vue` | OTP verification | `verifyOtp()` - calls register API with correct user type |
| `auth.store.ts` | Auth state management | `setToken()`, `decodeToken()`, `redirectToDashboard()` |
| `auth.guard.ts` | Route protection | Checks auth, handles provider status routing |
| `provider.routes.ts` | Provider route guards | `beforeEnter` - validates provider status |

### Backend

| File | Purpose | Key Logic |
|------|---------|-----------|
| `RegisterFromVerifiedPhoneCommandHandler.cs` | User registration from OTP | Role-based provider info query |
| `JwtTokenService.cs` | Token generation | Optional provider claims |
| `AuthenticateUserCommandHandler.cs` | Email/password login | Provider status in token |
| `PolicyAuthorizationExtensions.cs` | Authorization policies | Role-based access control |

---

## User Type Detection Logic

### LoginView.vue (Lines 98-133)

```typescript
const redirectPath = route.query.redirect

// Customer routes
const customerRoutes = ['/bookings/new', '/my-appointments', '/customer']
const isCustomerRoute = customerRoutes.some(r => redirectPath?.includes(r))

const userType = isCustomerRoute ? 'Customer' : 'Provider'

sessionStorage.setItem('registration_user_type', userType)
```

**Result**:
- `/login?redirect=/bookings/new` → Customer
- `/login?redirect=/customer/dashboard` → Customer
- `/login` (no redirect) → Provider
- `/login?redirect=/dashboard` → Provider

---

## Token Decoding Flow

### auth.store.ts (Lines 135-179)

```typescript
function setToken(token) {
  const tokenData = decodeToken(token)

  if (isProvider(tokenData)) {
    // Extract provider info (providerId, providerStatus)
    setProviderStatus(...)
  } else if (isCustomer(tokenData)) {
    // Extract customer info (userId, email)
    setProviderStatus(null, null)  // ✅ No provider queries
  }
}
```

**Key Point**: Customers get `providerStatus: null` explicitly, preventing provider API calls.

---

## Common Console Logs

### Successful Customer Flow

```
[LoginView] Redirect path: /bookings/new?providerId=123
[LoginView] Checking if customer route: { redirectPath: "/bookings/new?providerId=123", isCustomerRoute: true }
[LoginView] ✅ Customer booking flow detected, userType set to Customer
[LoginView] 🔑 Registration userType set to: Customer

[VerificationView] 🔍 Raw stored userType from sessionStorage: Customer
[VerificationView] 🔑 Final userType for registration: Customer

[AuthStore] Decoded token payload: { user_type: 'Customer', role: 'Customer' }
[AuthStore] Token user type: { userType: 'Customer', roles: ['Customer'], isProvider: false, isCustomer: true }
[AuthStore] ✅ Customer info extracted from token: { userId: '...', userType: 'Customer' }

[VerificationView] Phone verification complete, redirecting to: /bookings/new?providerId=123
```

### Successful Provider Flow

```
[LoginView] ℹ️ No redirect path, defaulting to Provider
[LoginView] 🔑 Registration userType set to: Provider

[VerificationView] 🔍 Raw stored userType from sessionStorage: Provider
[VerificationView] 🔑 Final userType for registration: Provider

[AuthStore] Decoded token payload: { user_type: 'Provider', role: 'Provider', providerId: null }
[AuthStore] Token user type: { userType: 'Provider', roles: ['Provider'], isProvider: true, isCustomer: false }
[AuthStore] ℹ️ Provider user but no provider profile yet

[VerificationView] Phone verification complete, redirecting to dashboard
[AuthStore] Provider status: null, redirecting to ProviderRegistration
```

---

## Quick Debugging

### Check User Type in Browser Console

```javascript
// Get auth store
const authStore = useAuthStore()

// Check authentication
console.log('Authenticated:', authStore.isAuthenticated)
console.log('User:', authStore.user)
console.log('Roles:', authStore.userRoles)

// Check provider status (should be null for customers)
console.log('Provider Status:', authStore.providerStatus)
console.log('Provider ID:', authStore.providerId)

// Check stored values
console.log('Access Token:', localStorage.getItem('access_token'))
console.log('User Type from Session:', sessionStorage.getItem('registration_user_type'))
console.log('Redirect Path:', sessionStorage.getItem('post_login_redirect'))
```

### Decode Token Manually

```javascript
function decodeJWT(token) {
  const base64Url = token.split('.')[1]
  const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
  const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => {
    return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)
  }).join(''))

  return JSON.parse(jsonPayload)
}

const token = localStorage.getItem('access_token')
console.log('Token Payload:', decodeJWT(token))
```

### Reset Everything

```javascript
// Complete reset
localStorage.clear()
sessionStorage.clear()
location.href = '/login'
```

---

## API Request Examples

### Register from Verified Phone (Customer)

```http
POST /api/v1/phone-verification/register-from-verified-phone
Content-Type: application/json

{
  "verificationId": "guid-from-otp-verification",
  "userType": "Customer",
  "firstName": null,
  "lastName": null
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "userId": "user-guid-123",
    "phoneNumber": "+989123456789",
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "refresh-token-value",
    "expiresIn": 86400,
    "message": "Account created successfully"
  }
}
```

### Register from Verified Phone (Provider)

```http
POST /api/v1/phone-verification/register-from-verified-phone
Content-Type: application/json

{
  "verificationId": "guid-from-otp-verification",
  "userType": "Provider",
  "firstName": null,
  "lastName": null
}
```

**Response** (New Provider):
```json
{
  "success": true,
  "data": {
    "userId": "user-guid-456",
    "phoneNumber": "+989198765432",
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",  // Contains: providerId: null, provider_status: null
    "refreshToken": "refresh-token-value",
    "expiresIn": 86400,
    "message": "Account created successfully"
  }
}
```

---

## Common Issues & Quick Fixes

| Issue | Quick Fix |
|-------|-----------|
| Always registering as Provider | Check redirect parameter: `/login?redirect=/bookings/new` |
| Provider status error for Customer | Fixed - customers don't query provider status anymore |
| Duplicate API calls | Fixed - `isLoading` guard prevents race condition |
| Token not persisting | Check localStorage permissions, verify `loadFromStorage()` runs |
| Infinite redirect loop | Check provider status in token, verify route guard logic |
| sessionStorage not clearing | Fixed - all items removed after successful login |

---

## Backend Configuration

### Required appsettings.json

```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "Booksy",
    "Audience": "Booksy.Users"
  },
  "PhoneVerification": {
    "OtpLength": 6,
    "ExpirationMinutes": 5,
    "MaxAttempts": 3
  }
}
```

### Environment Variables (Production)

```bash
JWT_SECRET_KEY=your-production-secret-key
JWT_ISSUER=Booksy
JWT_AUDIENCE=Booksy.Users
SMS_API_KEY=your-sms-provider-api-key
```

---

## Testing Checklist

### Customer Booking Flow
- [ ] Navigate to provider profile (unauthenticated)
- [ ] Click "Book Now"
- [ ] Verify redirect: `/login?redirect=/bookings/new?providerId=xxx`
- [ ] Check console: `Customer booking flow detected`
- [ ] Enter phone number
- [ ] Enter OTP
- [ ] Verify registration: `userType: Customer` in network payload
- [ ] Check token: No `providerId` or `provider_status`
- [ ] Verify redirect: Landing on `/bookings/new`
- [ ] Confirm no provider API errors in network tab

### Provider Registration Flow
- [ ] Navigate directly to `/login`
- [ ] Check console: `defaulting to Provider`
- [ ] Enter phone and OTP
- [ ] Verify registration: `userType: Provider` in network payload
- [ ] Check token: `providerId: null` for new provider
- [ ] Verify redirect: Landing on `/registration`
- [ ] Complete provider profile
- [ ] Verify redirect to dashboard after completion

### Provider Existing User Flow
- [ ] Login as existing provider
- [ ] Check token: `providerId: xxx, provider_status: Active`
- [ ] Verify redirect: Landing on `/dashboard`
- [ ] Attempt to access `/registration` → Should redirect to dashboard

---

## Role-Based Access Matrix

| Route | Customer | Provider (Drafted) | Provider (Active) | Admin |
|-------|----------|-------------------|-------------------|-------|
| `/` (Home) | ✅ | ✅ | ✅ | ✅ |
| `/login` | ✅ | ✅ | ✅ | ✅ |
| `/bookings/new` | ✅ | ✅ | ✅ | ✅ |
| `/my-appointments` | ✅ | ❌ | ❌ | ✅ |
| `/customer/dashboard` | ✅ | ❌ | ❌ | ✅ |
| `/registration` | ❌ | ✅ | ❌ | ❌ |
| `/dashboard` | ❌ | ❌ | ✅ | ❌ |
| `/services` | ❌ | ❌ | ✅ | ❌ |
| `/admin/dashboard` | ❌ | ❌ | ❌ | ✅ |

---

## Performance Considerations

### Avoid Unnecessary API Calls

**❌ Before Fix**:
```
Customer registers → Token generated
Frontend decodes token → Tries to extract provider info
Calls GET /api/providers/current/status → 404 Error
```

**✅ After Fix**:
```
Customer registers → Token generated (no provider claims)
Frontend decodes token → Detects Customer role
Sets providerStatus = null → NO API call ✅
```

### Token Size

- **Customer Token**: ~500 bytes (no provider claims)
- **Provider Token (New)**: ~550 bytes (null provider claims)
- **Provider Token (Active)**: ~650 bytes (with provider claims)

---

## Security Best Practices

1. **Never expose JWT secret** - Use environment variables
2. **Validate tokens on every request** - Backend middleware
3. **Short token expiration** - 24 hours default, 7 days max
4. **Rotate refresh tokens** - Generate new on each refresh
5. **HTTPS only** - Enforce in production
6. **Rate limit OTP requests** - Prevent abuse
7. **Sanitize user input** - Prevent injection attacks
8. **Audit logs** - Track failed login attempts

---

## Useful Commands

### Development

```bash
# Frontend
cd booksy-frontend
npm run dev        # Start dev server (http://localhost:3001)
npm run build      # Build for production
npm run type-check # Check TypeScript

# Backend
cd src/UserManagement/Booksy.UserManagement.API
dotnet run         # Start API (https://localhost:7001)
dotnet watch run   # Start with hot reload
dotnet test        # Run tests
```

### Database

```bash
# Apply migrations
dotnet ef database update --project src/UserManagement/Booksy.UserManagement.Infrastructure

# Create new migration
dotnet ef migrations add MigrationName --project src/UserManagement/Booksy.UserManagement.Infrastructure
```

---

## Support

For issues or questions:
1. Check [Full Documentation](AUTHENTICATION_FLOW_DOCUMENTATION.md)
2. Review console logs (enhanced logging added)
3. Check Network tab for API errors
4. Verify token payload structure
5. Contact development team

---

**Last Updated**: 2025-01-16
**Version**: 2.0 (Role-Aware Authentication)
