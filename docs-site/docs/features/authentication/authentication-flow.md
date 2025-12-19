# Authentication Flow Documentation

## Overview

This document describes the complete authentication flow for the Booksy application, covering both **Customer** and **Provider** user types. The system uses phone verification (OTP) for authentication and role-based routing for post-login redirects.

**🆕 UPDATE (2025-11-17):** The authentication flow has been simplified with **separate login pages** for customers and providers, removing complex redirect-path detection logic.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Separate Login Pages (NEW)](#separate-login-pages-new)
3. [Customer Booking Flow](#customer-booking-flow)
4. [Provider Registration Flow](#provider-registration-flow)
5. [Backend Implementation](#backend-implementation)
6. [Frontend Implementation](#frontend-implementation)
7. [Token Structure](#token-structure)
8. [Security Considerations](#security-considerations)
9. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

### Key Components

**Backend (C# / ASP.NET Core)**
- `RegisterFromVerifiedPhoneCommandHandler` - Creates/retrieves user account after phone verification
- `JwtTokenService` - Generates JWT tokens with role-based claims
- `AuthenticateUserCommandHandler` - Handles email/password authentication
- `IProviderInfoService` - Fetches provider status (only for Provider users)

**Frontend (Vue 3 / TypeScript)**
- **`LoginView.vue`** - Customer login (phone entry) with explicit userType = 'Customer'
- **`ProviderLoginView.vue`** - Provider login (phone entry) with explicit userType = 'Provider'
- `VerificationView.vue` - OTP verification and user registration (receives userType from route)
- `auth.store.ts` - Authentication state management, role-based token decoding
- `auth.guard.ts` - Navigation guard for protected routes
- `provider.routes.ts` - Provider-specific route guards

---

## Separate Login Pages (NEW)

### Overview

The application now uses **two distinct login pages** for better UX and simpler code:

| Login Page | Route | User Type | Audience | Messaging |
|------------|-------|-----------|----------|-----------|
| **Customer Login** | `/login` | `Customer` | People booking services | "به بوکسی خوش آمدید" / "برای رزرو نوبت" |
| **Provider Login** | `/provider/login` | `Provider` | Business owners | "ورود به پنل کسب و کار" |

### Key Changes:

✅ **Removed**: Complex redirect-path detection logic
✅ **Removed**: `sessionStorage.getItem('registration_user_type')`
✅ **Added**: Explicit userType on each login page
✅ **Added**: userType passed via route query params to VerificationView

### Navigation Flow:

**Customer Journey:**
```
Homepage → Search/Browse → Provider Detail → "رزرو نوبت"
  → Auth Guard (if not logged in) → /login → VerificationView (userType=Customer)
```

**Provider Journey:**
```
Homepage Footer "For Businesses" → /provider/login
  → VerificationView (userType=Provider)
```

### Implementation Details:

**LoginView.vue (Customer):**
```typescript
// Explicit customer type - no detection needed
router.push({
  name: 'PhoneVerification',
  query: {
    phone: phoneNumber.value,
    userType: 'Customer'  // ✅ Explicit
  }
})
```

**ProviderLoginView.vue (Provider):**
```typescript
// Explicit provider type - no detection needed
router.push({
  name: 'PhoneVerification',
  query: {
    phone: phoneNumber.value,
    userType: 'Provider'  // ✅ Explicit
  }
})
```

**VerificationView.vue:**
```typescript
// Receive userType from route query params
const userTypeFromQuery = route.query.userType as string | undefined

const userType = (userTypeFromQuery === 'Provider' || userTypeFromQuery === 'Customer'
  ? userTypeFromQuery
  : 'Customer') as 'Customer' | 'Provider'
```

---

## Customer Booking Flow

### Step-by-Step Process

#### 1. Customer Discovers Provider

```
Customer browses → Provider Detail Page → Clicks "رزرو نوبت" (Book Now)
```

**File**: `ProviderDetailView.vue`
```typescript
function bookNow() {
  router.push({ name: 'NewBooking', query: { providerId: provider.value.id } })
}
```

#### 2. Authentication Check

```
Route: /bookings/new?providerId=123
Auth Guard: Checks authentication → User is NOT authenticated
Redirect: /login?redirect=/bookings/new?providerId=123
```

**File**: `auth.guard.ts` (Line 34-39)
```typescript
if (requiresAuth && !authStore.isAuthenticated) {
  next({
    name: 'Login',
    query: { redirect: to.fullPath },  // ✅ Passes full path with query params
  })
  return
}
```

#### 3. User Type Handling (UPDATED)

**🆕 New Approach** - Explicit user type from login page:

The system no longer uses redirect-path detection. Instead, each login page explicitly sets the userType:

- **Customer Login (`/login`)**: Sets `userType = 'Customer'` and passes it via route query
- **Provider Login (`/provider/login`)**: Sets `userType = 'Provider'` and passes it via route query

**File**: `LoginView.vue` (Customer)
```typescript
// Simple: explicit customer type, no detection
router.push({
  name: 'PhoneVerification',
  query: {
    phone: phoneNumber.value,
    userType: 'Customer'  // ✅ Explicit
  }
})
```

**Benefits:**
- ✅ No complex detection logic
- ✅ No sessionStorage dependency
- ✅ Clearer user intent
- ✅ Easier to maintain

#### 4. Phone Verification

**User enters phone number** → Backend sends OTP SMS

**File**: `phoneVerification.api.ts`
```typescript
await sendVerificationCode({
  phoneNumber: '+989123456789',
  method: 'SMS',
  purpose: 'Registration'
})
```

#### 5. OTP Verification & Registration

**File**: `VerificationView.vue` (Line 121-157)
```typescript
const verifyOtp = async () => {
  // Prevent duplicate calls
  if (isLoading.value) return

  // Step 1: Verify OTP
  const result = await verifyCode(otpCode.value)

  if (result.success) {
    // Step 2: Get userType from sessionStorage
    const storedUserType = sessionStorage.getItem('registration_user_type')
    const userType = (storedUserType || 'Provider') as 'Customer' | 'Provider'

    console.log('[VerificationView] 🔑 Registering as:', userType)

    // Step 3: Create user account
    const registerResult = await phoneVerificationApi.registerFromVerifiedPhone({
      verificationId,
      userType: userType,  // ✅ 'Customer' for booking flow
      firstName: undefined,
      lastName: undefined
    })

    // Step 4: Store tokens and user info
    authStore.setToken(registerResult.data.accessToken)
    authStore.setRefreshToken(registerResult.data.refreshToken)
    authStore.setUser({ ...userData, userType, roles: [userType] })
  }
}
```

#### 6. Backend User Creation

**File**: `RegisterFromVerifiedPhoneCommandHandler.cs` (Line 100-113)
```csharp
// Create user with specified UserType
user = User.RegisterWithPhone(
    tempEmail,
    phoneNumber,
    profile,
    request.UserType);  // 'Customer' from frontend

// Check if user is Provider before fetching provider info
if (user.Roles.Any(r => r.Name == "Provider" || r.Name == "ServiceProvider"))
{
    var providerInfo = await _providerInfoService.GetProviderByOwnerIdAsync(
        user.Id.Value, cancellationToken);
    // Extract provider info...
}
else
{
    // ✅ SKIP provider fetch for Customers
}
```

#### 7. JWT Token Generation

**File**: `JwtTokenService.cs` (Line 34-83)
```csharp
public string GenerateAccessToken(
    UserId userId,
    UserType userType,     // 'Customer'
    Email email,
    string displayName,
    string status,
    IEnumerable<string> roles,   // ['Customer']
    string? providerId = null,   // ✅ null for customers
    string? providerStatus = null,  // ✅ null for customers
    int expirationHours = 24)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
        new(ClaimTypes.Email, email.Value),
        new("user_type", userType.ToString()),  // 'Customer'
        new(ClaimTypes.Role, "Customer")
    };

    // Provider claims NOT added for customers ✅

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Token Payload (Customer)**:
```json
{
  "sub": "user-guid-123",
  "email": "09123456789@booksy.temp",
  "user_type": "Customer",
  "role": "Customer",
  "iat": 1234567890,
  "exp": 1234654290
}
```
*Note: NO `providerId` or `provider_status` claims*

#### 8. Frontend Token Processing

**File**: `auth.store.ts` (Line 135-179)
```typescript
function setToken(newToken: string | null) {
  if (newToken) {
    const tokenData = decodeToken(newToken)

    const roles = Array.isArray(tokenData.roles) ? tokenData.roles : [tokenData.roles]
    const isProvider = roles.includes('Provider') || roles.includes('ServiceProvider')
    const isCustomer = roles.includes('Customer') || roles.includes('Client')

    if (isProvider) {
      // Extract provider info and set provider status
      const providerInfo = decodeTokenAndExtractProviderInfo(newToken)
      setProviderStatus(providerInfo.providerStatus, providerInfo.providerId)
    }
    else if (isCustomer) {
      // ✅ Customer flow
      const customerInfo = decodeTokenAndExtractCustomerInfo(newToken)
      console.log('✅ Customer info extracted:', customerInfo)

      // ✅ Explicitly set provider status to null (no provider queries)
      setProviderStatus(null, null)
    }
  }
}
```

#### 9. Post-Login Redirect

**File**: `VerificationView.vue` (Line 240-258)
```typescript
const redirectBasedOnProviderStatus = async () => {
  const redirectPath = sessionStorage.getItem('post_login_redirect')

  if (redirectPath) {
    // Clear sessionStorage
    sessionStorage.removeItem('post_login_redirect')

    // Redirect to intended destination
    await router.push(redirectPath)  // ✅ /bookings/new?providerId=123
  } else {
    // No redirect - use role-based routing
    await authStore.redirectToDashboard()
  }
}
```

#### 10. Final Navigation

```
Customer arrives at: /bookings/new?providerId=123
Auth Guard: ✅ Authenticated, ✅ Has access (no role restriction)
Booking Wizard: Loads with pre-selected provider
```

---

## Provider Registration Flow

### Step-by-Step Process

#### 1. Provider Accesses Login

```
Provider navigates directly to: /login
No redirect parameter → userType defaults to 'Provider'
```

**File**: `LoginView.vue`
```typescript
let userType: 'Customer' | 'Provider' = 'Provider'  // ✅ Default

if (!redirectPath) {
  console.log('[LoginView] ℹ️ No redirect path, defaulting to Provider')
}

sessionStorage.setItem('registration_user_type', 'Provider')
```

#### 2. Phone Verification & Registration

Same OTP flow as Customer, but with `userType: 'Provider'`

#### 3. Backend Provider Creation

**File**: `RegisterFromVerifiedPhoneCommandHandler.cs`
```csharp
user = User.RegisterWithPhone(
    tempEmail,
    phoneNumber,
    profile,
    UserType.Provider);  // ✅ Provider type

// ✅ Fetch provider status
if (user.Roles.Any(r => r.Name == "Provider"))
{
    var providerInfo = await _providerInfoService.GetProviderByOwnerIdAsync(
        user.Id.Value, cancellationToken);

    if (providerInfo != null)
    {
        providerId = providerInfo.ProviderId.ToString();
        providerStatus = providerInfo.Status;  // 'Drafted', 'Active', etc.
    }
    else
    {
        // New provider - no profile yet
        providerId = null;
        providerStatus = null;
    }
}
```

#### 4. JWT Token (Provider)

**Token Payload (New Provider)**:
```json
{
  "sub": "user-guid-456",
  "email": "09123456789@booksy.temp",
  "user_type": "Provider",
  "role": "Provider",
  "providerId": null,
  "provider_status": null
}
```

**Token Payload (Existing Provider)**:
```json
{
  "sub": "user-guid-456",
  "email": "provider@example.com",
  "user_type": "Provider",
  "role": "Provider",
  "providerId": "provider-guid-789",
  "provider_status": "Active"
}
```

#### 5. Frontend Token Processing

**File**: `auth.store.ts`
```typescript
if (isProvider) {
  const providerInfo = decodeTokenAndExtractProviderInfo(newToken)

  if (providerInfo) {
    console.log('✅ Provider info extracted:', providerInfo)
    setProviderStatus(providerInfo.providerStatus, providerInfo.providerId)
  } else {
    console.log('ℹ️ Provider user but no provider profile yet')
    setProviderStatus(null, null)  // New provider needs to complete registration
  }
}
```

#### 6. Post-Login Redirect

**File**: `auth.store.ts` - `redirectToDashboard()` (Line 359-387)
```typescript
if (roles.includes('Provider') || roles.includes('ServiceProvider')) {
  // Fetch provider status if not already loaded
  if (providerStatus.value === null && providerId.value === null) {
    await fetchProviderStatus()
  }

  if (providerStatus.value === ProviderStatus.Drafted || providerStatus.value === null) {
    // ✅ New provider → Complete registration
    router.push({ name: 'ProviderRegistration' })
  } else {
    // ✅ Existing provider → Dashboard
    router.push({ path: '/dashboard' })
  }
}
```

#### 7. Provider Status-Based Routing

**File**: `provider.routes.ts` (Line 48-73)
```typescript
beforeEnter(to, from, next) {
  const authStore = useAuthStore()
  const tokenProviderStatus = authStore.providerStatus

  // Allow access to registration if:
  // 1. No provider exists (null)
  // 2. Provider status is Drafted
  if (tokenProviderStatus === null || tokenProviderStatus === ProviderStatus.Drafted) {
    next()  // ✅ Allow registration
  } else {
    // Provider already registered → redirect to dashboard
    next({ name: 'ProviderDashboard' })
  }
}
```

---

## Backend Implementation

### Key Files & Components

#### 1. User Registration Command

**File**: `RegisterFromVerifiedPhoneCommandHandler.cs`

**Responsibilities**:
- Verify phone OTP was successful
- Check if user with phone already exists
- Create new user or return existing user
- Query provider information (only for Provider users)
- Generate JWT access token and refresh token
- Return authentication tokens

**Key Logic**:
```csharp
// Role-based provider info query
if (user.Roles.Any(r => r.Name == "Provider" || r.Name == "ServiceProvider"))
{
    try
    {
        var providerInfo = await _providerInfoService.GetProviderByOwnerIdAsync(
            user.Id.Value, cancellationToken);

        if (providerInfo != null)
        {
            providerId = providerInfo.ProviderId.ToString();
            providerStatus = providerInfo.Status;
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Error querying provider info, continuing without provider claims");
        // ✅ Graceful degradation - continue without provider claims
    }
}
```

#### 2. JWT Token Service

**File**: `JwtTokenService.cs`

**Token Claims**:
- `sub` or `ClaimTypes.NameIdentifier` - User ID
- `email` or `ClaimTypes.Email` - User email
- `user_type` - UserType enum ('Customer', 'Provider', 'Admin')
- `ClaimTypes.Role` - User roles (can be multiple)
- `providerId` - Provider GUID (optional, only for Providers)
- `provider_status` - Provider status enum (optional, only for Providers)

**Security Features**:
- HMAC SHA256 signing
- Configurable expiration (24 hours default, 168 hours with "Remember Me")
- Audience and Issuer validation

#### 3. Authorization Policies

**File**: `PolicyAuthorizationExtensions.cs`

**Policies**:
```csharp
// Provider-specific
options.AddPolicy("ProviderOnly", policy =>
    policy.RequireClaim("user_type", "Provider"));

options.AddPolicy("ProviderOrAdmin", policy =>
    policy.RequireAssertion(context =>
        context.User.IsInRole("Provider") ||
        context.User.IsInRole("Admin")));

// Customer-specific
options.AddPolicy("ClientOnly", policy =>
    policy.RequireClaim("user_type", "Client"));
```

---

## Frontend Implementation

### Key Files & Components

#### 1. Login View

**File**: `LoginView.vue`

**Responsibilities**:
- Collect phone number
- Detect user type from redirect parameter
- Store user type in sessionStorage
- Send OTP verification code
- Navigate to verification page

**User Type Detection**:
```typescript
const customerRoutes = ['/bookings/new', '/my-appointments', '/customer']
const isCustomerRoute = customerRoutes.some(route => redirectPath.includes(route))

if (isCustomerRoute) {
  userType = 'Customer'
} else {
  userType = 'Provider'  // Default
}
```

#### 2. Verification View

**File**: `VerificationView.vue`

**Responsibilities**:
- Display OTP input (6 digits)
- Verify OTP code
- Read user type from sessionStorage
- Call registration API with correct user type
- Store authentication tokens
- Handle post-login redirect

**Duplicate Call Prevention**:
```typescript
const verifyOtp = async () => {
  // ✅ Prevent race condition between auto-submit and manual submit
  if (isLoading.value) {
    console.log('[VerificationView] ⚠️ Already processing, skipping duplicate call')
    return
  }

  isLoading.value = true
  // ... verification logic
}
```

#### 3. Auth Store

**File**: `auth.store.ts`

**Key Functions**:

**`decodeToken()`** - Generic token decoder
```typescript
function decodeToken(jwtToken: string) {
  const payload = JSON.parse(atob(parts[1]))

  return {
    userId: payload.sub,
    email: payload.email,
    userType: payload.user_type,
    roles: payload.role,
    providerId: payload.providerId,
    providerStatus: payload.provider_status
  }
}
```

**`decodeTokenAndExtractProviderInfo()`** - Provider-only
```typescript
function decodeTokenAndExtractProviderInfo(jwtToken: string) {
  const tokenData = decodeToken(jwtToken)

  const isProvider = roles.includes('Provider') ||
                     tokenData.userType === 'Provider'

  if (isProvider && tokenData.providerId) {
    return {
      providerId: tokenData.providerId,
      providerStatus: tokenData.providerStatus || null
    }
  }

  return null
}
```

**`decodeTokenAndExtractCustomerInfo()`** - Customer-only
```typescript
function decodeTokenAndExtractCustomerInfo(jwtToken: string) {
  const tokenData = decodeToken(jwtToken)

  const isCustomer = roles.includes('Customer') ||
                     tokenData.userType === 'Customer'

  if (isCustomer) {
    return {
      userId: tokenData.userId,
      email: tokenData.email,
      userType: tokenData.userType,
      roles: roles
    }
  }

  return null
}
```

**`setToken()`** - Role-based token processing
```typescript
function setToken(newToken: string | null) {
  if (newToken) {
    const tokenData = decodeToken(newToken)
    const isProvider = /* check roles */
    const isCustomer = /* check roles */

    if (isProvider) {
      // Extract provider info, fetch provider status
      const providerInfo = decodeTokenAndExtractProviderInfo(newToken)
      setProviderStatus(providerInfo?.providerStatus, providerInfo?.providerId)
    } else if (isCustomer) {
      // Extract customer info, NO provider status
      const customerInfo = decodeTokenAndExtractCustomerInfo(newToken)
      setProviderStatus(null, null)  // ✅ Explicit null
    }
  }
}
```

**`redirectToDashboard()`** - Role-based routing
```typescript
async function redirectToDashboard(redirectPath?: string) {
  // Honor explicit redirect path first
  if (redirectPath) {
    router.push(redirectPath)
    return
  }

  // Role-based routing
  if (roles.includes('Admin')) {
    router.push('/admin/dashboard')
  } else if (roles.includes('Provider')) {
    // Check provider status
    if (providerStatus === null || providerStatus === 'Drafted') {
      router.push({ name: 'ProviderRegistration' })
    } else {
      router.push('/dashboard')
    }
  } else if (roles.includes('Customer')) {
    router.push('/customer/dashboard')
  } else {
    router.push('/')
  }
}
```

#### 4. Auth Guard

**File**: `auth.guard.ts`

**Responsibilities**:
- Check authentication status
- Redirect unauthenticated users to login with redirect parameter
- Handle provider status-based routing
- Enforce role-based access control

**Customer Booking Flow**:
```typescript
// Unauthenticated user tries to access /bookings/new
if (requiresAuth && !authStore.isAuthenticated) {
  next({
    name: 'Login',
    query: { redirect: to.fullPath }  // ✅ /bookings/new?providerId=123
  })
  return
}

// After authentication
if (authStore.isAuthenticated) {
  // Customer accessing /bookings/new
  // No provider status check needed (not a Provider)
  next()  // ✅ Allow access
}
```

**Provider Flow**:
```typescript
if (authStore.hasAnyRole(['Provider', 'ServiceProvider'])) {
  // Fetch provider status if not loaded
  if (authStore.providerStatus === null && authStore.providerId === null) {
    await authStore.fetchProviderStatus()
  }

  // Redirect based on status
  if (providerStatus === 'Drafted' || providerStatus === null) {
    // Allow: ProviderRegistration, ProviderProfile, ProviderSettings, Bookings
    const allowedRoutes = ['ProviderRegistration', 'ProviderProfile', ...]
    if (!allowedRoutes.includes(to.name)) {
      next({ name: 'ProviderRegistration' })
    }
  } else {
    // Active provider - redirect from Home/Registration to Dashboard
    if (to.name === 'Home' || to.name === 'ProviderRegistration') {
      next({ path: '/dashboard' })
    }
  }
}
```

---

## Token Structure

### Customer Token Example

```json
{
  "sub": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "09123456789@booksy.temp",
  "user_type": "Customer",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Customer",
  "user-status": "Active",
  "jti": "unique-token-id",
  "iat": 1731780000,
  "nbf": 1731780000,
  "exp": 1731866400,
  "iss": "Booksy",
  "aud": "Booksy.Users"
}
```

### Provider Token Example (New Provider)

```json
{
  "sub": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "09198765432@booksy.temp",
  "user_type": "Provider",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Provider",
  "user-status": "Active",
  "providerId": null,
  "provider_status": null,
  "jti": "unique-token-id",
  "iat": 1731780000,
  "nbf": 1731780000,
  "exp": 1731866400,
  "iss": "Booksy",
  "aud": "Booksy.Users"
}
```

### Provider Token Example (Active Provider)

```json
{
  "sub": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "provider@example.com",
  "user_type": "Provider",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Provider",
  "user-status": "Active",
  "providerId": "d4e5f6a7-b8c9-0123-def1-234567890123",
  "provider_status": "Active",
  "jti": "unique-token-id",
  "iat": 1731780000,
  "nbf": 1731780000,
  "exp": 1731866400,
  "iss": "Booksy",
  "aud": "Booksy.Users"
}
```

---

## Security Considerations

### 1. OTP Security

- **Expiration**: OTP codes expire after 5 minutes (300 seconds)
- **Rate Limiting**: Maximum 3 attempts per verification session
- **One-Time Use**: OTP codes are invalidated after successful verification
- **Secure Storage**: Verification records stored with hashed values

### 2. JWT Token Security

- **Signing Algorithm**: HMAC SHA256
- **Secret Key**: Stored in appsettings.json (should be environment variable in production)
- **Token Expiration**:
  - Default: 24 hours (86400 seconds)
  - "Remember Me": 7 days (604800 seconds)
- **Refresh Tokens**: 30-day expiration, rotating refresh tokens

### 3. Role-Based Access Control

- **Backend**: `[Authorize(Policy = "ProviderOrAdmin")]` attributes on controllers
- **Frontend**: Route guards check roles before navigation
- **Token Claims**: Roles embedded in JWT, validated on every request

### 4. SessionStorage Security

- **Temporary Data Only**: User type detection, redirect paths
- **Cleared After Use**: All sessionStorage items removed after successful login
- **Not Sensitive**: No passwords, tokens, or PII stored in sessionStorage

### 5. Provider Status Protection

- **Backend Validation**: Provider status checked in command handlers
- **Frontend Guards**: Routes protected based on provider status
- **Database Constraints**: Provider status transitions validated in domain layer

---

## Troubleshooting

### Issue: User Type Always 'Provider'

**Symptoms**:
- Customer trying to book, but registered as Provider
- sessionStorage shows `registration_user_type: 'Provider'`

**Solution**:
1. Check redirect parameter in URL: `/login?redirect=/bookings/new`
2. Verify LoginView detection logic includes the route
3. Clear sessionStorage before testing: `sessionStorage.clear()`
4. Check console logs for `[LoginView] ✅ Customer booking flow detected`

**Debug Steps**:
```javascript
// In browser console
console.log('Redirect:', new URLSearchParams(window.location.search).get('redirect'))
console.log('Stored UserType:', sessionStorage.getItem('registration_user_type'))
```

---

### Issue: Duplicate API Calls During Verification

**Symptoms**:
- Network tab shows two calls to `registerFromVerifiedPhone`
- Console shows warning: `⚠️ Already processing, skipping duplicate call`

**Cause**:
- Race condition between auto-submit (6 digits entered) and manual submit (button click)

**Solution**:
- Fixed with `isLoading` guard in VerificationView
- Prevents duplicate calls automatically

---

### Issue: Provider Status Fetch for Customers

**Symptoms**:
- Network error: `/api/providers/current/status` returns 404 for customers
- Console shows: `Error fetching provider status`

**Solution**:
- Fixed with role-based token decoding
- `setToken()` now checks user type before extracting provider info
- Customers explicitly get `providerStatus: null`

**Verification**:
```javascript
// Check token decoding in console
const authStore = useAuthStore()
console.log('Provider Status:', authStore.providerStatus)  // Should be null for customers
console.log('User Roles:', authStore.userRoles)  // Should be ['Customer']
```

---

### Issue: Infinite Redirect Loop

**Symptoms**:
- Browser keeps redirecting between `/login` and `/registration`
- Console shows repeated navigation logs

**Possible Causes**:
1. Provider with null status stuck between registration and dashboard
2. Auth guard and route guard conflicting
3. Provider status not properly set in token

**Solution**:
1. Check provider status in token:
   ```javascript
   const authStore = useAuthStore()
   console.log('Provider Status:', authStore.providerStatus)
   console.log('Provider ID:', authStore.providerId)
   ```

2. Verify route guard logic in `provider.routes.ts`:
   - Allows null and 'Drafted' status to access registration
   - Redirects other statuses to dashboard

3. Check `allowedGeneralRoutes` in auth.guard.ts:
   - Ensure necessary routes are whitelisted for incomplete providers

---

### Issue: Token Not Persisting After Page Refresh

**Symptoms**:
- User logged in, but after refresh shows as logged out
- localStorage appears empty

**Solution**:
1. Verify tokens are being stored:
   ```javascript
   console.log('Access Token:', localStorage.getItem('access_token'))
   console.log('Refresh Token:', localStorage.getItem('refresh_token'))
   ```

2. Check `loadFromStorage()` is being called on app initialization

3. Verify token expiration:
   ```javascript
   const authStore = useAuthStore()
   console.log('Token Expired:', authStore.isTokenExpired())
   ```

---

## Testing Guide

### Manual Testing - Customer Flow

1. **Clear all storage**:
   ```javascript
   localStorage.clear()
   sessionStorage.clear()
   ```

2. **Start booking flow**:
   - Navigate to: `http://localhost:3001`
   - Find a provider
   - Click "رزرو نوبت" (Book Now)

3. **Verify redirect**:
   - URL should be: `/login?redirect=/bookings/new?providerId=...`
   - Console should show: `[LoginView] ✅ Customer booking flow detected`

4. **Enter phone and OTP**:
   - Phone: `09123456789`
   - OTP: Check backend logs or use test code

5. **Verify registration**:
   - Console: `[VerificationView] 🔑 Final userType for registration: Customer`
   - Network: POST `/api/v1/phone-verification/register-from-verified-phone`
   - Payload: `{ "userType": "Customer" }`

6. **Verify token**:
   - Console: `[AuthStore] ✅ Customer info extracted from token`
   - Token should NOT have `providerId` or `provider_status`

7. **Verify redirect**:
   - Should land on: `/bookings/new?providerId=...`
   - Booking wizard should load

---

### Manual Testing - Provider Flow

1. **Clear storage**:
   ```javascript
   localStorage.clear()
   sessionStorage.clear()
   ```

2. **Navigate to login**:
   - Direct URL: `http://localhost:3001/login` (no redirect param)
   - Console: `[LoginView] ℹ️ No redirect path, defaulting to Provider`

3. **Verify registration**:
   - Console: `[VerificationView] 🔑 Final userType for registration: Provider`
   - Payload: `{ "userType": "Provider" }`

4. **Verify token**:
   - Console: `[AuthStore] ✅ Provider info extracted` or `ℹ️ Provider user but no provider profile yet`
   - New provider: `providerId: null, provider_status: null`

5. **Verify redirect**:
   - New provider: Should redirect to `/registration`
   - Existing provider: Should redirect to `/dashboard`

---

## API Endpoints

### Authentication Endpoints

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/api/v1/phone-verification/send` | POST | Send OTP code | No |
| `/api/v1/phone-verification/verify` | POST | Verify OTP code | No |
| `/api/v1/phone-verification/register-from-verified-phone` | POST | Create user from verified phone | No |
| `/api/v1/auth/login` | POST | Email/password login | No |
| `/api/v1/auth/logout` | POST | Logout user | Yes |
| `/api/v1/auth/refresh-token` | POST | Refresh access token | Yes |

### Provider Endpoints

| Endpoint | Method | Description | Auth Required | Roles |
|----------|--------|-------------|---------------|-------|
| `/api/v1/providers/current/status` | GET | Get provider status | Yes | Provider |
| `/api/v1/providers/registration` | POST | Complete provider registration | Yes | Provider |
| `/api/v1/providers/{id}` | GET | Get provider profile | No | - |

### Booking Endpoints

| Endpoint | Method | Description | Auth Required | Roles |
|----------|--------|-------------|---------------|-------|
| `/api/v1/bookings` | POST | Create booking | Yes | Any |
| `/api/v1/bookings/my-bookings` | GET | Get user's bookings | Yes | Any |
| `/api/v1/bookings/{id}` | GET | Get booking details | Yes | Any |
| `/api/v1/bookings/{id}/cancel` | POST | Cancel booking | Yes | Any |
| `/api/v1/bookings/available-slots` | GET | Get available time slots | Yes | Any |

---

## Conclusion

This authentication system provides a robust, role-aware flow for both Customers and Providers. Key features:

- ✅ **Context-Aware Registration**: Automatically detects user type from navigation context
- ✅ **Role-Based Token Processing**: Customers never trigger unnecessary provider queries
- ✅ **Secure OTP Verification**: Time-limited, rate-limited, one-time use codes
- ✅ **Flexible Redirect Handling**: Preserves user's intended destination through login flow
- ✅ **Provider Status Management**: Ensures providers complete registration before accessing full features
- ✅ **Comprehensive Logging**: Detailed console logs for debugging and monitoring

For questions or issues, please refer to the troubleshooting section or contact the development team.
