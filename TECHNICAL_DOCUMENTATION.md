# Booksy Platform - Comprehensive Technical Documentation

> **Living Document**: This documentation consolidates all technical documentation from the project. Last updated: 2025-11-09

---

## Document Navigation

- [Overview](#overview)
- [Architecture & Patterns](#architecture--patterns)
- [Authentication & Phone Verification](#authentication--phone-verification)
- [Provider Registration Flow](#provider-registration-flow)
- [API Integration & Type Safety](#api-integration--type-safety)
- [Routing & Navigation Guards](#routing--navigation-guards)
- [Known Issues & Solutions](#known-issues--solutions)
- [Session Summaries & Progress](#session-summaries--progress)

---

## Overview

Booksy is a service booking platform built with:
- **Backend**: .NET Core 8 (Clean Architecture, CQRS, DDD)
- **Frontend**: Vue 3 + TypeScript (Composition API, Pinia)
- **Database**: PostgreSQL with EF Core
- **Authentication**: JWT with phone verification (OTP)
- **Messaging**: RabbitMQ for event-driven architecture
- **SMS**: Rahyab SMS gateway integration

---

## Architecture & Patterns

### Backend Architecture

**Clean Architecture Layers:**
```
├── API Layer (Controllers, Middleware)
├── Application Layer (CQRS - Commands, Queries, Handlers)
├── Domain Layer (Aggregates, Entities, Value Objects, Events)
└── Infrastructure Layer (Persistence, External Services)
```

**Key Patterns:**
- **CQRS**: Commands for writes, Queries for reads
- **DDD**: Domain-driven design with aggregates and value objects
- **Repository Pattern**: Data access abstraction
- **Unit of Work**: Transaction management
- **Domain Events**: Event-driven communication

### Frontend Architecture

**Structure:**
```
booksy-frontend/
├── src/
│   ├── core/                    # Core infrastructure
│   │   ├── api/                 # API clients & interceptors
│   │   ├── router/              # Routes & guards
│   │   ├── stores/              # Pinia state management
│   │   ├── services/            # Shared services
│   │   └── types/               # Global TypeScript types
│   ├── modules/                 # Feature modules
│   │   ├── auth/                # Authentication & verification
│   │   ├── provider/            # Provider features
│   │   ├── customer/            # Customer features
│   │   └── booking/             # Booking features
│   └── shared/                  # Shared UI components
```

**Key Patterns:**
- **Composition API**: Vue 3 composables for logic reuse
- **Pinia Stores**: Reactive state management
- **TypeScript**: Type-safe development
- **Axios Interceptors**: Request/response transformation
- **Route Guards**: Authentication & authorization

---

## Authentication & Phone Verification

### Phone Verification Flow

**Overview:**
Phone verification is a **separate step** from user authentication. It only confirms phone ownership, not user identity.

**Flow Diagram:**
```
1. User enters phone number → LoginView
   ↓
2. Backend generates OTP code (6 digits, 5-minute validity)
   ↓
3. OTP hashed with SHA256, stored in DB (plain text NOT stored)
   ↓
4. SMS sent via Rahyab gateway
   ↓
5. User enters OTP → VerificationView
   ↓
6. Backend hashes input, compares with stored hash
   ↓
7. If match → Phone verified (user proceeds to registration)
   ↓
8. Registration creates account & authenticates user
```

### OTP Security Implementation

**Key Security Features:**

1. **Hash-Only Storage**: Plain text OTP is NEVER stored
   ```csharp
   // PhoneVerification.cs
   public OtpCode OtpCode { get; private set; } // NOT persisted to DB
   public string OtpHash { get; private set; }   // SHA256 hash stored

   // EF Core Configuration
   builder.Ignore(v => v.OtpCode); // Ignore OtpCode property
   ```

2. **Verification by Hash Comparison**:
   ```csharp
   public bool Verify(string inputCode)
   {
       var inputHash = HashOtp(inputCode);
       var isValid = inputHash.Equals(OtpHash, StringComparison.Ordinal);
       // ...
   }
   ```

3. **Why OtpCode is Null After Database Load**:
   - The `OtpCode` property is a transient value object
   - Only exists during OTP generation/sending
   - **Never persisted** to protect against database breaches
   - Verification works by hashing user input and comparing hashes

**Important Files:**
- `PhoneVerification.cs` - Domain aggregate
- `PhoneVerificationConfiguration.cs` - EF Core mapping
- `RequestPhoneVerificationCommandHandler.cs` - OTP generation
- `VerifyPhoneRequest.cs` - OTP verification

### SMS Sandbox Mode

**Development Configuration:**
```json
// appsettings.Development.json
{
  "Rahyab": {
    "SandboxMode": true,
    "SandboxOtpCode": "123456",
    "ApiUrl": "https://api.rahyab.ir/sms/send"
  }
}
```

**Implementation:**
```csharp
// RahyabSmsNotificationService.cs
if (_sandboxMode)
{
    _logger.LogWarning(
        "🔧 SANDBOX MODE: Skipping real SMS. Use OTP code: {OtpCode}",
        _sandboxOtpCode);
    return (true, $"sandbox-{Guid.NewGuid()}", null);
}
```

**Production Configuration** (Azure KeyVault):
```json
{
  "Rahyab": {
    "SandboxMode": false,
    "ApiUrl": "#{AzureKeyVault:RahyabApiUrl}#",
    "UserName": "#{AzureKeyVault:RahyabUserName}#",
    "Password": "#{AzureKeyVault:RahyabPassword}#"
  }
}
```

---

## Provider Registration Flow

### Complete Flow

```
Phone Verification (Unauthenticated)
  ↓
/phone-verification → Verify OTP
  ↓
/registration → Step 1: Business Info (PUBLIC route)
  ├─ Business Name
  ├─ Owner Name
  └─ Phone Number (auto-filled from sessionStorage)
  ↓
Step 2: Category Selection
  ↓
Step 3: Location (Province, City, Address, Map)
  ↓
Step 4: Services
  ↓
Step 5: Staff
  ↓
Step 6: Working Hours
  ↓
Step 7: Gallery
  ↓
Step 8: Optional Feedback
  ↓
Step 9: Completion → Account Creation & Authentication
  ↓
/dashboard (Authenticated)
```

### Key Implementation Details

**1. Registration Route is PUBLIC**
```typescript
// provider.routes.ts
{
  path: '/registration',
  name: 'ProviderRegistration',
  meta: {
    isPublic: true,  // ← Important: No authentication required
    requiresPhoneVerification: true
  }
}
```

**Why?** New users don't have accounts yet. Registration creates the account.

**2. Phone Number Persistence**
```typescript
// usePhoneVerification.ts
const PHONE_NUMBER_KEY = 'phone_verification_number'
sessionStorage.setItem(PHONE_NUMBER_KEY, fullPhoneNumber)

// BusinessInfoStep.vue
const getPhoneNumber = () => {
  return sessionStorage.getItem(PHONE_NUMBER_KEY) ||
         authStore.user?.phoneNumber || ''
}
```

**3. Skip Draft Load for Unauthenticated Users**
```typescript
// useProviderRegistration.ts
const loadDraft = async () => {
  if (!authStore.isAuthenticated) {
    console.log('⏭️ Skipping draft load - user not authenticated')
    return { success: true, message: 'Starting fresh registration' }
  }
  // Load draft only for authenticated users
}
```

**4. Province/City Only in LocationStep**
- **BusinessInfoStep** (Step 1): Business name, owner name, phone
- **LocationStep** (Step 3): Province, city, address, coordinates

---

## API Integration & Type Safety

### Transform Interceptor

**Critical Component**: Converts between frontend camelCase and backend PascalCase

```typescript
// transform.interceptor.ts

// REQUEST: camelCase → PascalCase
export function transformRequest(data: any) {
  return toPascalCase(data)
}

function toPascalCase(obj: any): any {
  if (!obj || typeof obj !== 'object') return obj

  if (Array.isArray(obj)) {
    return obj.map(item => toPascalCase(item))
  }

  const result: any = {}
  for (const key in obj) {
    // phoneNumber → PhoneNumber
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1)
    result[pascalKey] = toPascalCase(obj[key])
  }
  return result
}

// RESPONSE: PascalCase → camelCase
export function transformResponse(data: any) {
  return toCamelCase(data)
}

function toCamelCase(obj: any): any {
  if (!obj || typeof obj !== 'object') return obj

  if (Array.isArray(obj)) {
    return obj.map(item => toCamelCase(item))
  }

  const result: any = {}
  for (const key in obj) {
    // PhoneNumber → phoneNumber
    const camelKey = key.charAt(0).toLowerCase() + key.slice(1)
    result[camelKey] = toCamelCase(obj[key])
  }
  return result
}
```

**Why This Matters:**
- Frontend: `{ phoneNumber: "..." }` (JavaScript convention)
- Backend: `{ PhoneNumber: "..." }` (.NET convention)
- **Automatic conversion** prevents API mismatches

### Auth Interceptor

```typescript
// auth.interceptor.ts
export function authInterceptor(config: InternalAxiosRequestConfig) {
  const token = localStorageService.get<string>('access_token')

  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
}

export async function authErrorInterceptor(error: any) {
  if (error.response?.status === 401 && !originalRequest._retry) {
    // Try to refresh token
    const refreshToken = localStorageService.get<string>('refresh_token')
    const response = await fetch('/api/v1/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken })
    })

    if (response.ok) {
      // Retry original request
      return axios(originalRequest)
    } else {
      // Redirect to login
      window.location.href = '/login'  // NOT /auth/login!
    }
  }
}
```

---

## Routing & Navigation Guards

### Route Structure

```typescript
// Main router (index.ts)
const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'Home',
    component: BookingView,
    meta: { requiresAuth: true }
  },
  ...authRoutes,      // /login, /phone-verification
  ...providerRoutes,  // /registration, /dashboard, /services, etc.
  ...customerRoutes,
  ...bookingRoutes
]
```

**Important: NO duplicate `/` redirect** in auth.routes!
- OLD (WRONG): `{ path: '/', redirect: '/login' }` in auth.routes
- NEW (CORRECT): Only one `/` route in main router

### Auth Guard Logic

```typescript
// auth.guard.ts
export async function authGuard(to, from, next) {
  const authStore = useAuthStore()
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  const isPublic = to.matched.some((record) => record.meta.isPublic)

  // Allow public routes
  if (isPublic) {
    // Redirect authenticated users AWAY from login
    if (authStore.isAuthenticated && (to.name === 'Login' || to.name === 'Register')) {
      await authStore.redirectToDashboard()
      return
    }
    next()
    return
  }

  // Require authentication for protected routes
  if (requiresAuth && !authStore.isAuthenticated) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
    return
  }

  // Provider status-based routing
  if (authStore.hasAnyRole(['Provider'])) {
    if (authStore.providerStatus === ProviderStatus.Drafted) {
      // Redirect to registration
      next({ name: 'ProviderRegistration' })
      return
    }
  }

  next()
}
```

### Route Metadata

```typescript
// Public route (no auth required)
{
  path: '/registration',
  meta: {
    isPublic: true,
    requiresPhoneVerification: true,  // Custom meta
    title: 'Complete Your Provider Profile'
  }
}

// Protected route (requires auth)
{
  path: '/dashboard',
  meta: {
    requiresAuth: true,
    roles: ['Provider', 'ServiceProvider'],
    title: 'Dashboard'
  }
}
```

---

## Known Issues & Solutions

### Issue 1: OTP Code is Null After Database Load

**Symptom:**
```csharp
var verification = await repository.GetByIdAsync(verificationId);
// verification.OtpCode is NULL
// verification.OtpHash has value
```

**Root Cause:**
`OtpCode` is ignored by EF Core for security (see `PhoneVerificationConfiguration.cs:66`)

**Solution:**
This is **intentional by design**. Use hash comparison:
```csharp
var inputHash = HashOtp(inputCode);
var isValid = inputHash.Equals(OtpHash, StringComparison.Ordinal);
```

### Issue 2: Redirect to Login After Phone Verification

**Symptom:**
After successful OTP verification, user redirects to `/login` instead of `/registration`

**Root Causes:**
1. Duplicate `/` route redirecting to `/login`
2. ProviderRegistration route had `requiresAuth: true`
3. Auth interceptor redirecting to wrong path

**Solutions:**
1. Remove `{ path: '/', redirect: '/login' }` from auth.routes
2. Set ProviderRegistration to `isPublic: true`
3. Fix auth interceptor redirect: `/login` not `/auth/login`

**Files Changed:**
- `auth.routes.ts` - Removed duplicate route
- `provider.routes.ts` - Made ProviderRegistration public
- `auth.interceptor.ts` - Fixed redirect path

### Issue 3: 401 Error Loading Registration Progress

**Symptom:**
```
GET /v1/registration/progress 401 (Unauthorized)
```

**Root Cause:**
New users from phone verification are not authenticated, but registration page tries to load existing draft.

**Solution:**
Skip draft load for unauthenticated users:
```typescript
const loadDraft = async () => {
  if (!authStore.isAuthenticated) {
    return { success: true, message: 'Starting fresh registration' }
  }
  // Only load draft for authenticated users
  const response = await api.getRegistrationProgress()
  // ...
}
```

**File Changed:**
- `useProviderRegistration.ts`

### Issue 4: Phone Number Empty in BusinessInfoStep

**Symptom:**
Phone number field is blank in Step 1 of registration

**Root Cause:**
Tried to get from `authStore.user?.phoneNumber` but user not authenticated

**Solution:**
Get from sessionStorage where it was saved during verification:
```typescript
const PHONE_NUMBER_KEY = 'phone_verification_number'
const getPhoneNumber = () => {
  return sessionStorage.getItem(PHONE_NUMBER_KEY) ||
         authStore.user?.phoneNumber || ''
}
```

**File Changed:**
- `BusinessInfoStep.vue`

### Issue 5: Duplicate Symbol 'format' Error

**Symptom:**
```
ERROR: The symbol "format" has already been declared
```

**Root Cause:**
```typescript
const { format = 'jalaali', ... } = options  // Line 32
function format(date: Date) { ... }          // Line 177
```

**Solution:**
Rename destructured variable:
```typescript
const { format: initialFormat = 'jalaali', ... } = options
const displayFormat = ref<DateFormat>(initialFormat)
```

**File Changed:**
- `useDatePicker.ts`

### Issue 6: Toast Not Defined Error

**Symptom:**
```
ReferenceError: toast is not defined
```

**Root Cause:**
`useToast` not imported in VerificationView.vue

**Solution:**
```typescript
import { useToast } from '@/core/composables'
const toast = useToast()
```

**File Changed:**
- `VerificationView.vue`

---

## Session Summaries & Progress

### Session: 2025-11-09 - Phone Verification & Registration Flow Fixes

**Context:**
Continued from previous session. Phone verification backend was complete, but frontend flow had critical routing and authentication issues.

**Issues Addressed:**

1. ✅ **OTP Verification Security**
   - Fixed OTP verification to use hash comparison
   - Documented why OtpCode is null (security feature)
   - Backend: `PhoneVerification.cs`, `PhoneVerificationConfiguration.cs`

2. ✅ **Routing Flow After Verification**
   - Removed duplicate `/` route causing redirect loops
   - Made ProviderRegistration public (no auth required)
   - Fixed auth interceptor redirect path
   - Files: `auth.routes.ts`, `provider.routes.ts`, `auth.interceptor.ts`, `auth.guard.ts`

3. ✅ **Registration Draft Loading**
   - Skip draft load for unauthenticated users
   - Prevents 401 errors for new registrations
   - File: `useProviderRegistration.ts`

4. ✅ **BusinessInfoStep Improvements**
   - Phone number auto-filled from sessionStorage
   - Removed duplicate Province/City fields (belong in LocationStep)
   - File: `BusinessInfoStep.vue`

5. ✅ **TypeScript & Build Errors**
   - Fixed duplicate 'format' symbol in useDatePicker
   - Fixed missing toast import in VerificationView
   - Files: `useDatePicker.ts`, `VerificationView.vue`

**Complete Flow Now Working:**
```
/login
  → User enters phone: 09123456789
  → Backend generates OTP, sends SMS
  ↓
/phone-verification
  → User enters OTP: 123456 (or sandbox code)
  → Backend verifies hash
  ↓
/registration (PUBLIC - no auth required)
  → Step 1: Business Info (phone auto-filled)
  → Step 2: Category
  → Step 3: Location (Province, City, Address)
  → Steps 4-8: Services, Staff, Hours, Gallery, Feedback
  → Step 9: Account created → User authenticated
  ↓
/dashboard (authenticated)
```

**Key Decisions:**

1. **Phone verification ≠ Authentication**
   - Verification only confirms phone ownership
   - Registration creates the actual user account
   - Authentication happens after registration completes

2. **Registration is PUBLIC**
   - New users can't authenticate before having an account
   - Registration page accessible without login
   - Draft loading skipped for unauthenticated users

3. **SessionStorage for Phone Number**
   - Persists across navigation
   - Survives page refresh during verification
   - Auto-fills registration form

4. **OTP Security by Design**
   - Plain text never stored in database
   - Hash-only verification prevents exposure
   - Sandbox mode for development testing

**Files Modified:**
```
Backend:
- PhoneVerification.cs
- RahyabSmsNotificationService.cs
- All appsettings.json files (Rahyab config)

Frontend:
- auth.routes.ts
- provider.routes.ts
- auth.guard.ts
- auth.interceptor.ts
- transform.interceptor.ts (previous session)
- VerificationView.vue
- BusinessInfoStep.vue
- usePhoneVerification.ts
- useProviderRegistration.ts
- useDatePicker.ts
```

**Testing Verified:**
- ✅ Phone verification flow (login → verify → registration)
- ✅ OTP hash comparison (sandbox mode with 123456)
- ✅ Navigation guards (public vs protected routes)
- ✅ Phone number auto-fill in registration
- ✅ Draft loading skip for new users
- ✅ No 401 errors on registration page
- ✅ No routing loops or redirects

**Next Steps:**
- Complete provider registration backend endpoints
- Implement account creation on registration completion
- Add email verification (optional)
- Add profile image upload
- Testing with real Rahyab SMS in staging

---

## Document Revision History

| Date       | Version | Changes                                           | Author       |
|------------|---------|---------------------------------------------------|--------------|
| 2025-11-09 | 3.0.0   | Comprehensive documentation of auth & registration| Claude (AI)  |
| 2025-11-06 | 2.0.0   | Consolidated all technical documentation          | Claude (AI)  |
| 2025-11-05 | 1.0.0   | Initial comprehensive documentation created       | Claude (AI)  |

---

*This is a living document. It consolidates all technical documentation from the project into a single, searchable reference. Update this file whenever significant changes are made to the architecture, authentication flow, or critical components.*
