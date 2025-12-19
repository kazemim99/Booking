# Booksy Platform - Comprehensive Technical Documentation

> **Living Document**: This documentation consolidates all technical documentation from the project. Last updated: 2025-11-13
>
> **Recent Updates (2025-11-13)**: Added 4 critical bug fixes and optimizations (Issues #11-14): Route conflict resolution, status API optimization, HTTP interceptor error handling, and cache validation. Redesigned provider bookings management page with modern UI/UX.

---

## Document Navigation

- [Overview](#overview)
- [Architecture & Patterns](#architecture--patterns)
- [Authentication & Phone Verification](#authentication--phone-verification)
- [Provider Registration Flow](#provider-registration-flow)
- [Location & Map Integration](#location--map-integration)
- [Event-Driven Architecture](#event-driven-architecture)
- [Database & EF Core Configuration](#database--ef-core-configuration)
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

## Location & Map Integration

### Neshan Maps Integration

**Overview:**
The platform uses Neshan Maps (Iranian map service) for location selection with bidirectional synchronization between map and form fields.

**Architecture:**
```typescript
LocationStep.vue
  ├─ NeshanMapPicker (Interactive map component)
  ├─ LocationSelector (Province/City dropdowns)
  └─ useLocations (Composable for location data)
```

### Bidirectional Synchronization

**1. Map → Form (Click on map updates dropdowns)**

```typescript
const handleLocationSelected = async (data: {
  lat: number
  lng: number
  addressDetails?: {
    state: string     // Province name from reverse geocoding
    city: string      // City name from reverse geocoding
    formattedAddress: string
    postalCode: string
  }
}) => {
  // Update coordinates
  formData.value.coordinates = { lat: data.lat, lng: data.lng }

  // Auto-select province from reverse geocoded state name
  if (data.addressDetails?.state) {
    const province = locationStore.getProvinceByName(data.addressDetails.state)
    if (province) {
      isUpdatingFromMap.value = true  // Prevent circular updates
      formData.value.provinceId = province.id

      // Load and auto-select city
      await locationStore.loadCitiesByProvinceId(province.id)
      if (data.addressDetails.city) {
        const cities = locationStore.getCitiesByProvinceId(province.id)
        const city = cities.find(c => c.name === data.addressDetails.city)
        if (city) {
          formData.value.cityId = city.id
        }
      }

      setTimeout(() => { isUpdatingFromMap.value = false }, 100)
    }
  }

  // Auto-fill address and postal code
  formData.value.formattedAddress = data.addressDetails.formattedAddress
  formData.value.postalCode = data.addressDetails.postalCode
}
```

**2. Form → Map (Select dropdown updates map)**

```typescript
// Helper function to geocode location names
const geocodeLocationName = async (locationName: string) => {
  const response = await fetch(
    `https://api.neshan.org/v1/search?term=${encodeURIComponent(locationName)}`,
    { headers: { 'Api-Key': neshanServiceKey } }
  )
  const data = await response.json()
  if (data.items?.[0]) {
    return {
      lat: data.items[0].location.y,
      lng: data.items[0].location.x
    }
  }
  return null
}

// Watch province changes
watch(() => formData.value.provinceId, async (newProvinceId) => {
  if (isUpdatingFromMap.value || !newProvinceId) return

  const province = locationStore.getLocationById(newProvinceId)
  if (province) {
    const coordinates = await geocodeLocationName(province.name)
    if (coordinates) {
      formData.value.coordinates = coordinates  // Triggers map center update
    }
  }
})

// Watch city changes
watch(() => formData.value.cityId, async (newCityId) => {
  if (isUpdatingFromMap.value || !newCityId) return

  const city = locationStore.getLocationById(newCityId)
  const province = formData.value.provinceId ?
    locationStore.getLocationById(formData.value.provinceId) : null

  if (city) {
    // Combine city + province for accurate geocoding
    const searchTerm = province ? `${city.name}, ${province.name}` : city.name
    const coordinates = await geocodeLocationName(searchTerm)
    if (coordinates) {
      formData.value.coordinates = coordinates
    }
  }
})
```

**3. Preventing Circular Updates**

```typescript
const isUpdatingFromMap = ref(false)

// Set flag when map triggers update
isUpdatingFromMap.value = true
// ... update form fields
setTimeout(() => { isUpdatingFromMap.value = false }, 100)

// Skip watchers when flag is set
if (isUpdatingFromMap.value) return
```

### Neshan Maps Configuration

**Environment Variables:**
```env
# .env.development
VITE_NESHAN_MAP_KEY=web.741ff28152504624a0b3942d3621b56d
VITE_NESHAN_SERVICE_KEY=service.qBDJpu7hKVBEAzERghfm9JM7vqGKXoNNNTdtrGy7
```

**API Endpoints Used:**
- **Reverse Geocoding**: `https://api.neshan.org/v5/reverse?lat={lat}&lng={lng}`
  - Returns: address, city, state, postalCode
- **Search/Geocoding**: `https://api.neshan.org/v1/search?term={query}`
  - Returns: coordinates for location name

**Key Files:**
- `NeshanMapPicker.vue` - Interactive map component
- `LocationSelector.vue` - Province/City dropdowns
- `LocationStep.vue` - Registration step with bidirectional sync
- `useLocations.ts` - Composable for location data (provinces, cities)

---

## Event-Driven Architecture

### Domain Events vs Integration Events

**Domain Events**: Internal to a bounded context
**Integration Events**: Cross-context communication via message bus

```csharp
// Domain Event (internal)
public sealed record ProviderDraftCreatedEvent(
    ProviderId ProviderId,
    UserId OwnerId,
    string OwnerFirstName,
    string OwnerLastName,
    string BusinessName,
    DateTime CreatedAt
) : DomainEvent;

// Integration Event (cross-context)
public sealed record ProviderDraftCreatedIntegrationEvent(
    Guid ProviderId,
    Guid OwnerId,
    string OwnerFirstName,
    string OwnerLastName,
    string BusinessName,
    DateTime CreatedAt
) : IntegrationEvent
```

### Event Flow: Owner Name Storage

**Use Case**: When provider registers, store owner's firstName/lastName in User.Profile

**Architecture:**
```
ServiceCatalog Context              UserManagement Context
      |                                     |
Provider.CreateDraft()                     |
      ↓                                     |
Raise: ProviderDraftCreatedEvent           |
      ↓                                     |
ProviderDraftCreatedEventHandler           |
      ↓                                     |
Publish: Integration Event → CAP → RabbitMQ
      |                                     ↓
      |                    ProviderDraftCreatedEventSubscriber
      |                                     ↓
      |                         User.Profile.UpdatePersonalInfo()
      |                                     ↓
      |                              Save to UserManagement DB
```

### Implementation Details

**1. Raise Domain Event in Aggregate**

```csharp
// Provider.cs
public static Provider CreateDraft(
    UserId ownerId,
    string ownerFirstName,    // ← Captured from registration
    string ownerLastName,
    string businessName,
    // ... other params
)
{
    var provider = new Provider { /* ... */ };

    // Raise domain event
    provider.RaiseDomainEvent(new ProviderDraftCreatedEvent(
        provider.Id,
        provider.OwnerId,
        ownerFirstName,
        ownerLastName,
        provider.Profile.BusinessName,
        DateTime.UtcNow
    ));

    return provider;
}
```

**2. Domain Event Handler → Integration Event**

```csharp
// ProviderDraftCreatedEventHandler.cs (ServiceCatalog)
public async Task HandleAsync(
    ProviderDraftCreatedEvent domainEvent,
    CancellationToken cancellationToken)
{
    var integrationEvent = new ProviderDraftCreatedIntegrationEvent(
        domainEvent.ProviderId.Value,
        domainEvent.OwnerId.Value,
        domainEvent.OwnerFirstName,
        domainEvent.OwnerLastName,
        domainEvent.BusinessName,
        domainEvent.CreatedAt
    );

    await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
}
```

**3. Integration Event Subscriber (Cross-Context)**

```csharp
// ProviderDraftCreatedEventSubscriber.cs (UserManagement)
[CapSubscribe("booksy.servicecatalog.providerdraftcreated")]
public async Task HandleAsync(ProviderDraftCreatedIntegrationEvent @event)
{
    await _unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        var userId = UserId.From(@event.OwnerId);
        var user = await _userRepository.GetByIdAsync(userId);

        // Update user profile with owner names
        user.Profile.UpdatePersonalInfo(
            @event.OwnerFirstName,
            @event.OwnerLastName,
            middleName: null
        );

        await _userRepository.UpdateAsync(user, CancellationToken.None);
    });
}
```

**Key Benefits:**
- ✅ Bounded contexts remain decoupled
- ✅ Each context maintains its own database
- ✅ Cross-context updates happen asynchronously
- ✅ Failure in one context doesn't affect the other

**Key Files:**
- `ProviderDraftCreatedEvent.cs` - Domain event
- `ProviderDraftCreatedIntegrationEvent.cs` - Integration event
- `ProviderDraftCreatedEventHandler.cs` - Event publisher (ServiceCatalog)
- `ProviderDraftCreatedEventSubscriber.cs` - Event subscriber (UserManagement)

---

## Database & EF Core Configuration

### PostgreSQL Setup

**Connection String Format:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=booksy_service_catalog;Username=postgres;Password=postgres;Include Error Detail=true"
  }
}
```

**❌ WRONG (SQL Server format):**
```
"Server=localhost;Database=booksy;..."
```

**✅ CORRECT (PostgreSQL format):**
```
"Host=localhost;Port=5432;Database=booksy;..."
```

### EF Core 9 Owned Entity Configuration

**Critical Issue**: Owned entities with manually-generated IDs require specific configuration in EF Core 9.

**The Problem:**
```
Error: The database operation was expected to affect 1 row(s),
       but actually affected 0 row(s); data may have been modified
       or deleted since entities were loaded.
```

**Root Cause**: Owned entities in EF Core 9 need explicit configuration for:
1. **Value generation strategy**
2. **Concurrency token behavior**
3. **No DbSet exposure**
4. **Shadow property for foreign key**

### Owned Entity Pattern (EF Core 9)

**Domain Entity:**
```csharp
// ServiceOption.cs - Owned by Service aggregate
public sealed class ServiceOption : Entity<Guid>
{
    public string Name { get; private set; }
    public Price AdditionalPrice { get; private set; }
    // ... other properties

    // ❌ NO explicit ServiceId property (use shadow property)
    // public ServiceId ServiceId { get; private set; }  // WRONG

    internal static ServiceOption Create(string name, Price price, ...)
    {
        return new ServiceOption
        {
            Id = Guid.NewGuid(),  // ← Generated in code
            Name = name,
            // ... no ServiceId assignment
        };
    }
}
```

**EF Core Configuration:**
```csharp
// ServiceConfiguration.cs
builder.OwnsMany(s => s.Options, option =>
{
    option.ToTable("ServiceOptions", "ServiceCatalog");

    // ✅ CRITICAL: Tell EF Core the ID is not database-generated
    option.Property(so => so.Id)
        .ValueGeneratedNever()      // ID set in code, not by DB
        .IsConcurrencyToken(false); // Don't use for concurrency checks

    option.HasKey(so => so.Id);
    option.Property(so => so.Id).HasColumnName("Id");

    // ✅ Use shadow property for foreign key
    option.WithOwner()
        .HasForeignKey("ServiceId");  // Shadow property, not exposed

    // Configure all properties...
    option.Property(so => so.Name)
        .IsRequired()
        .HasMaxLength(100);

    // ✅ Configure inherited base class properties
    option.Property(so => so.CreatedAt)
        .IsRequired()
        .HasColumnType("timestamp with time zone");

    option.Property(so => so.CreatedBy);
    option.Property(so => so.LastModifiedAt);
    option.Property(so => so.LastModifiedBy);
    option.Property(so => so.IsDeleted).HasDefaultValue(false);
});

// ✅ Configure navigation with backing field
builder.Navigation(s => s.Options)
    .UsePropertyAccessMode(PropertyAccessMode.Field)
    .HasField("_options");
```

**DbContext Configuration:**
```csharp
// ServiceCatalogDbContext.cs

// ❌ WRONG: Don't expose owned entities as DbSets
// public DbSet<ServiceOption> ServiceOptions => Set<ServiceOption>();

// ✅ CORRECT: Only aggregates are DbSets
public DbSet<Service> Services => Set<Service>();

// Comment why owned entities aren't exposed
// ServiceOption and PriceTier are owned entities (OwnsMany) - not exposed as DbSets
```

### Configuration Checklist for Owned Entities

**When using `OwnsMany` in EF Core 9:**

- ✅ Add `ValueGeneratedNever().IsConcurrencyToken(false)` for manually-generated IDs
- ✅ Use shadow property for foreign key: `WithOwner().HasForeignKey("PropertyName")`
- ✅ Remove explicit foreign key property from entity class
- ✅ Configure ALL properties (including inherited base class properties)
- ✅ Do NOT expose as `DbSet<T>` in DbContext
- ✅ Access only through parent aggregate's navigation property
- ✅ Use backing fields for collections in aggregate

**Common Mistakes:**

| Mistake | Solution |
|---------|----------|
| Explicit FK property (e.g., `ServiceId`) | Remove it, use shadow property |
| Exposed as `DbSet<ServiceOption>` | Remove from DbContext |
| Missing `.ValueGeneratedNever()` | Add to Id property configuration |
| Not configuring inherited properties | Configure `CreatedAt`, `CreatedBy`, etc. |
| Using `.Include(s => s.Options)` | Not needed, auto-loaded with `OwnsMany` |

### Database Migrations

**Creating Migrations:**
```bash
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

dotnet ef migrations add MigrationName \
  --startup-project ../../Apps/Booksy.ServiceCatalog.Api/Booksy.ServiceCatalog.Api.csproj \
  --context ServiceCatalogDbContext
```

**Applying Migrations:**
```bash
dotnet ef database update \
  --startup-project ../../Apps/Booksy.ServiceCatalog.Api/Booksy.ServiceCatalog.Api.csproj \
  --context ServiceCatalogDbContext
```

**Suppressing Pending Model Changes Warning:**
```csharp
// ServiceCatalogDbContext.cs
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    // Method signature changes don't require migrations
    optionsBuilder.ConfigureWarnings(warnings =>
        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

    base.OnConfiguring(optionsBuilder);
}
```

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

### Issue 7: EF Core 9 Owned Entity SaveChanges Error

**Symptom:**
```
DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s),
but actually affected 0 row(s); data may have been modified or deleted since entities were loaded.
```

**Context:**
Occurs when seeding ServiceOption entities that are configured as owned entities (`OwnsMany`) in EF Core 9.

**Root Causes (Multiple Issues):**

1. **Missing ValueGeneratedNever Configuration**
   - Owned entities with `Guid.NewGuid()` in code need `ValueGeneratedNever()`
   - EF Core 9 assumes DB-generated IDs by default

2. **Exposed as DbSet**
   - Owned entities should NOT be exposed as `DbSet<T>` in DbContext
   - Conflicts with `OwnsMany` configuration

3. **Explicit Foreign Key Property**
   - Owned entities should use shadow properties for FKs
   - Explicit `ServiceId` property causes tracking issues

4. **Missing Inherited Property Configuration**
   - Base class properties (CreatedAt, CreatedBy, etc.) must be configured
   - EF Core 9 is stricter about owned entity property mapping

**Solutions Applied:**

```csharp
// 1. Add ValueGeneratedNever to entity configuration
option.Property(so => so.Id)
    .ValueGeneratedNever()      // ← CRITICAL for manual ID generation
    .IsConcurrencyToken(false);

// 2. Remove DbSet from DbContext
// public DbSet<ServiceOption> ServiceOptions => Set<ServiceOption>(); // ← REMOVE

// 3. Remove explicit FK from entity, use shadow property
// public ServiceId ServiceId { get; private set; }  // ← REMOVE FROM ENTITY
option.WithOwner().HasForeignKey("ServiceId");  // ← Shadow property in config

// 4. Configure all inherited properties
option.Property(so => so.CreatedAt);
option.Property(so => so.CreatedBy);
option.Property(so => so.LastModifiedAt);
option.Property(so => so.LastModifiedBy);
option.Property(so => so.IsDeleted);
```

**Reference:**
https://stackoverflow.com/questions/79219671/ef-core-9-the-database-operation-was-expected-to-affect-1-rows-but-actually

**Files Changed:**
- `ServiceOption.cs` - Removed ServiceId property
- `PriceTier.cs` - Removed ServiceId property
- `ServiceConfiguration.cs` - Added ValueGeneratedNever, moved config inline
- `ServiceCatalogDbContext.cs` - Removed DbSet declarations
- `ServiceOptionSeeder.cs` - Updated to access through parent aggregate

**Key Takeaway:**
EF Core 9 has stricter validation for owned entities. Always use `ValueGeneratedNever()` for manually-generated IDs in owned entities.

### Issue 8: Gallery Images Not Submitted During Registration (Fixed 2025-11-11)

**Symptom:**
Images uploaded during Step 7 (Gallery) of registration flow were not being saved to the backend.

**Root Cause:**
The `saveGallery()` function in `useProviderRegistration.ts` was a no-op that just returned success without calling the registration API endpoint.

```typescript
// BEFORE (Broken):
const saveGallery = async (providerId: string) => {
  // Just returns success, doesn't actually call API
  return {
    success: true,
    message: 'Gallery step complete'
  }
}
```

**Solution (Commit: `e6273aa`):**
Modified `saveGallery()` to actually submit images to the registration endpoint:

```typescript
// AFTER (Fixed):
const saveGallery = async (providerId: string) => {
  // Extract File objects from gallery images
  const files = registrationState.value.data.galleryImages
    .filter(img => img.file)
    .map(img => img.file!)

  if (files.length > 0) {
    // Upload images via registration endpoint
    const response = await providerRegistrationService.saveStep7Gallery(files)
    return { success: true, message: response.message }
  }

  return { success: true, message: 'No new files to upload' }
}
```

**Files Changed:**
- `booksy-frontend/src/modules/provider/composables/useProviderRegistration.ts:500-554`

### Issue 9: CompletionStep UI Distortion (Fixed 2025-11-11)

**Symptom:**
The final completion screen (Step 9) had distorted UI with broken gradient backgrounds and incorrect spacing.

**Root Cause:**
Component was using broken Tailwind CSS escape sequences that don't render correctly:
```vue
<!-- BROKEN -->
<div class="bg-gradient-to-br from-primary\/5 to-accent\/20">
<div class="bg-primary\/10">
```

The escape sequences (`\/`) are invalid and cause the styles to not apply.

**Solution (Commit: `2cead84`):**
Completely rewrote the component using semantic scoped CSS instead of Tailwind utilities:

```vue
<!-- BEFORE -->
<div class="bg-gradient-to-br from-primary\/5 to-accent\/20">

<!-- AFTER -->
<div class="completion-container">

<style scoped>
.completion-container {
  background: linear-gradient(to bottom right, rgba(139, 92, 246, 0.05), rgba(236, 72, 153, 0.2));
}
</style>
```

**Files Changed:**
- `booksy-frontend/src/modules/provider/components/registration/steps/CompletionStep.vue`

**Related Fix:**
Same issue and solution applied to `OptionalFeedbackStep.vue` (Commit: `d7b8a79`)

### Issue 10: Registration Progress Query Returns "Not Found" After Completion (Fixed 2025-11-11)

**Symptom:**
After completing registration (Step 9), calling `GetRegistrationProgress` returned "not found" error, causing page refresh to fail.

**Root Cause:**
When registration completes, the provider status changes from `Drafted` to `PendingVerification`. The query only looked for providers with `Drafted` status:

```csharp
// BEFORE (Broken):
public async Task<Provider?> GetDraftProviderByOwnerIdAsync(UserId ownerId) {
  return await DbSet
    .FirstOrDefaultAsync(p => p.OwnerId == ownerId && p.Status == ProviderStatus.Drafted);
  // Returns null after status changes to PendingVerification
}
```

**Solution (Commit: `f4be06d`):**

**Backend Fix:**
Added fallback logic to check for completed providers:

```csharp
// AFTER (Fixed):
public async Task<GetRegistrationProgressResult> Handle(...) {
  // First, try to get draft provider (status = Drafted)
  var draftProvider = await _providerRepository
    .GetDraftProviderByOwnerIdAsync(userId, cancellationToken);

  // If no draft found, check if user has a completed/pending provider
  if (draftProvider == null) {
    var provider = await _providerRepository
      .GetByOwnerIdAsync(userId, cancellationToken);

    // If provider exists but registration is complete, return completed status
    if (provider != null && provider.IsRegistrationComplete) {
      return new GetRegistrationProgressResult(
        HasDraft: false,
        CurrentStep: 9, // Registration completed
        ProviderId: provider.Id.Value,
        DraftData: null);
    }

    // No provider found at all
    return new GetRegistrationProgressResult(
      HasDraft: false,
      CurrentStep: null,
      ProviderId: null,
      DraftData: null);
  }

  // Continue with draft provider mapping...
}
```

**Frontend Fix:**
Added handler for completed registration state:

```typescript
// AFTER (Fixed):
const response = await providerRegistrationService.getRegistrationProgress()

// Handle completed registration (hasDraft: false but providerId exists)
if (!response.hasDraft && response.providerId) {
  return {
    success: true,
    message: 'Registration already completed',
    providerId: response.providerId,
  }
}
```

**Files Changed:**
- `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Provider/GetRegistrationProgress/GetRegistrationProgressQueryHandler.cs`
- `booksy-frontend/src/modules/provider/composables/useProviderRegistration.ts:700-713`

**Impact:**
This fix ensures that:
- Provider ID remains accessible after registration completion
- Page refresh works correctly at any point in the registration flow
- No "not found" errors after completing Step 9
- Proper status tracking throughout the provider lifecycle (Drafted → PendingVerification → Active)

### Issue 11: Route Conflict Between Customer and Provider Bookings (Fixed 2025-11-13)

**Symptom:**
Clicking "رزروها" (Bookings) in the provider dashboard sidebar navigated to `AppointmentListView.vue` (customer appointments) instead of `ProviderBookingsView.vue` (provider bookings management).

**Root Cause:**
Two routes were competing for the `/bookings` path:
- **Customer route** (registered first in router): `/bookings` → `AppointmentListView.vue`
- **Provider route** (registered second): `/bookings` → `ProviderBookingsView.vue`

Since routes are matched in order, the customer route was always matching first.

```typescript
// Router configuration (router/index.ts)
const routes = [
  ...authRoutes,
  ...bookingRoutes,      // ← Registered FIRST (customer bookings)
  ...providerRoutes,     // ← Registered SECOND (provider bookings)
  ...adminRoutes,
]
```

**Solution (Commit: `f9ea81c3`):**
Changed the customer bookings route to a distinct path:

```typescript
// BEFORE (booking.routes.ts):
{
  path: '/bookings',
  name: 'Bookings',
  component: () => import('@/modules/booking/views/AppointmentListView.vue'),
}

// AFTER (booking.routes.ts):
{
  path: '/my-appointments',
  name: 'Bookings',
  component: () => import('@/modules/booking/views/AppointmentListView.vue'),
}
```

**Result:**
- Customer appointments: `/my-appointments` → `AppointmentListView.vue`
- Provider bookings: `/bookings` → `ProviderBookingsView.vue`

**Files Changed:**
- `booksy-frontend/src/core/router/routes/booking.routes.ts`

### Issue 12: Redundant Status API Calls in Registration Flow (Fixed 2025-11-13)

**Symptom:**
The `getCurrentProviderStatus()` API was being called **3 times** when users navigated to the registration page:
1. `VerificationView.vue:239` - After phone verification
2. `provider.routes.ts:42` - Route guard `beforeEnter`
3. `ProviderRegistrationFlow.vue:343` - Component `onMounted()`

**Root Cause:**
Defensive programming led to multiple layers checking the same status, causing unnecessary API calls and delays.

**Solution (Commit: `62132db`):**
Implemented **Option 2: Route-level protection only** - Use route guard as single source of truth:

```typescript
// BEFORE - VerificationView.vue (removed):
const redirectBasedOnProviderStatus = async () => {
  const providerStatus = await providerService.getCurrentProviderStatus() // ❌ Call 1
  if (!providerStatus || providerStatus.status === ProviderStatus.Drafted) {
    await router.push({ name: 'ProviderRegistration' })
  } else {
    await router.push({ name: 'ProviderDashboard' })
  }
}

// AFTER - VerificationView.vue (simplified):
const redirectBasedOnProviderStatus = async () => {
  // Simply redirect - route guard will handle status validation
  await router.push({ name: 'ProviderRegistration' })
}

// Route guard remains (single source of truth):
async beforeEnter(...) {
  const statusData = await authStore.providerStatus  // ✅ Single call from token
  // Validate and redirect as needed
}

// BEFORE - ProviderRegistrationFlow.vue (removed):
onMounted(async () => {
  const statusData = await providerService.getCurrentProviderStatus() // ❌ Call 3
  if (statusData.status === ProviderStatus.Drafted) {
    await loadDraft()
  }
})

// AFTER - ProviderRegistrationFlow.vue (simplified):
onMounted(async () => {
  // Route guard already validated access - just load draft
  await loadDraft()
})
```

**Result:**
- Status checks reduced from **3 API calls to 1** (from token, no API call)
- Faster navigation experience
- Cleaner code with single responsibility

**Files Changed:**
- `booksy-frontend/src/modules/auth/views/VerificationView.vue`
- `booksy-frontend/src/modules/provider/views/registration/ProviderRegistrationFlow.vue`
- `booksy-frontend/src/core/router/routes/provider.routes.ts`

### Issue 13: TypeError in HTTP Interceptors - toUpperCase() on Undefined (Fixed 2025-11-13)

**Symptom:**
```
TypeError: Cannot read properties of undefined (reading 'toUpperCase')
    at requestLoggingInterceptor
    at cacheRequestInterceptor
    at retryInterceptor
```

**Root Cause:**
Multiple HTTP interceptors were calling `.toUpperCase()` on the request `method` property without proper type checking:

```typescript
// BROKEN CODE:
loggerService.info('🚀 API Request', {
  method: method?.toUpperCase(),  // ❌ Optional chaining not enough
  url,
})

// If method is undefined, method?.toUpperCase() still tries to call toUpperCase() on undefined
```

The error occurred when cached responses or malformed requests had missing or undefined `method` properties.

**Solution (Commits: `605711b`, `d1448bf`):**
Added defensive type checking in all interceptors:

```typescript
// AFTER - Logging Interceptor (logging.interceptor.ts):
loggerService.info('🚀 API Request', {
  method: method && typeof method === 'string' ? method.toUpperCase() : 'UNKNOWN',
  url,
})

// AFTER - Cache Interceptor (request-cache.ts):
const method = config.method && typeof config.method === 'string'
  ? config.method.toUpperCase()
  : 'UNKNOWN'
console.log(`💾 Cache HIT: ${method} ${config.url}`)

// AFTER - Retry Interceptor (retry-handler.ts):
const method = config.method && typeof config.method === 'string'
  ? config.method.toUpperCase()
  : 'UNKNOWN'
console.log(`🔄 Retrying request: ${method} ${config.url}`)

// AFTER - Transform Interceptor (transform.interceptor.ts):
return Object.keys(obj).reduce((result, key) => {
  // Skip if key is not a valid string or is empty
  if (!key || typeof key !== 'string' || key.length === 0) {
    result[key] = toPascalCase(obj[key])
    return result
  }

  const pascalKey = key.charAt(0).toUpperCase() + key.slice(1)
  result[pascalKey] = toPascalCase(obj[key])
  return result
}, {} as any)
```

**Files Changed:**
- `booksy-frontend/src/core/api/interceptors/logging.interceptor.ts`
- `booksy-frontend/src/core/api/interceptors/request-cache.ts`
- `booksy-frontend/src/core/api/interceptors/retry-handler.ts`
- `booksy-frontend/src/core/api/interceptors/transform.interceptor.ts`

### Issue 14: Cache Returning Malformed Responses (Fixed 2025-11-13)

**Symptom:**
```
request-cache.ts:76 💾 Cache HIT: GET /v1/providers/current/status
error.interceptor.ts:21 خطا در برقراری ارتباط با سرور
TypeError: Cannot read properties of undefined (reading 'toUpperCase')
```

**Root Cause:**
The in-memory request cache was returning cached responses without validating that the response had a properly formed `config` object:

```typescript
// BEFORE (request-cache.ts):
get(config: InternalAxiosRequestConfig): AxiosResponse | null {
  const entry = this.cache.get(key)
  if (entry && this.isValid(entry)) {
    return { ...entry.data }  // ❌ No validation of response structure
  }
  return null
}
```

When cached responses were returned without a valid `config.method` property, downstream interceptors failed with TypeError.

**Solution (Commit: `a704cf3`):**
Added validation and sanitization of cached responses:

```typescript
// AFTER (request-cache.ts):
get(config: InternalAxiosRequestConfig): AxiosResponse | null {
  const entry = this.cache.get(key)
  if (!entry || !this.isValid(entry)) {
    return null
  }

  // Ensure the cached response has a valid config object with method
  const cachedResponse = { ...entry.data }
  if (cachedResponse.config) {
    cachedResponse.config = {
      ...cachedResponse.config,
      method: cachedResponse.config.method || config.method || 'get',
    }
  }

  return cachedResponse
}

set(config: InternalAxiosRequestConfig, response: AxiosResponse, ttl: number): void {
  // Don't cache responses without a valid config
  if (!response.config || !response.config.method) {
    if (import.meta.env.DEV) {
      console.warn('💾 Cache SKIP: Response missing valid config')
    }
    return
  }

  const key = this.generateKey(config)
  this.cache.set(key, {
    data: { ...response },
    timestamp: Date.now(),
    ttl,
  })
}
```

**Files Changed:**
- `booksy-frontend/src/core/api/interceptors/request-cache.ts`

**Impact:**
- Prevents malformed cached responses from causing runtime errors
- Ensures all cached responses have valid config objects
- Improves application stability when using cached data
- Better logging for debugging cache issues

### Feature Addition: Modern Provider Bookings Management UI (Added 2025-11-13)

**Commit:** `0ce5cf5`

**Overview:**
Completely redesigned the provider bookings management page (`ProviderBookingsView.vue`) with a modern, card-based UI/UX design.

**Key Features:**

1. **Page Header**
   - Title and subtitle
   - Action buttons: "Calendar View", "Create Booking"

2. **Quick Stats Dashboard**
   - 4 gradient stat cards with icons:
     - Today's bookings
     - Upcoming bookings
     - Completed bookings
     - Monthly revenue
   - Hover effects with lift animation

3. **Advanced Filtering System**
   - Tab-based status filter: All / Pending / Confirmed / Completed / Cancelled
   - Each tab shows count badges
   - Search box for customer name/service
   - Date range picker
   - Service type dropdown
   - Reset filters button

4. **Card-Based Booking List**
   - Replaced old table with modern cards
   - Customer avatar with initials
   - Color-coded status badges
   - Details grid with icons (date, time, service, price)
   - Contextual action buttons based on status:
     - Pending: Confirm / Decline
     - Confirmed: Complete / Reschedule / Cancel
     - Completed: View Details
   - Hover effects with elevation

5. **Loading & Empty States**
   - Spinner animation during data fetch
   - Empty state illustration with helpful message

6. **Full Persian/RTL Support**
   - Right-to-left layout
   - Persian labels and dates
   - Sample data for demonstration

**Files Changed:**
- `booksy-frontend/src/modules/provider/views/ProviderBookingsView.vue`

**Route:**
- Path: `/bookings`
- Name: `ProviderBookings`
- Component: `ProviderBookingsView.vue`

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

### Session: 2025-11-11 - Location Integration, Event Architecture & EF Core 9 Fixes

**Context:**
Continued from previous session. Implemented bidirectional map synchronization, event-driven owner name storage, and resolved critical EF Core 9 owned entity issues.

**Major Work Completed:**

1. ✅ **Bidirectional Location Synchronization**
   - Implemented Map → Form sync (click map → auto-select province/city)
   - Implemented Form → Map sync (select dropdown → move map)
   - Used Neshan Search API for geocoding location names
   - Prevented circular updates with `isUpdatingFromMap` flag
   - Files: `LocationStep.vue`, `NeshanMapPicker.vue`, `useLocations.ts`

2. ✅ **Event-Driven Owner Name Storage**
   - Created `ProviderDraftCreatedEvent` (domain event)
   - Created `ProviderDraftCreatedIntegrationEvent` (cross-context)
   - Implemented event handler to publish to CAP/RabbitMQ
   - Implemented event subscriber in UserManagement to update User.Profile
   - Owner names now stored in both ServiceCatalog and UserManagement contexts
   - Files: Event classes, handlers, Provider.cs, SaveStep3LocationCommand.cs

3. ✅ **EF Core 9 Owned Entity Configuration**
   - **Critical Issue Resolved**: "expected to affect 1 row(s), but actually affected 0 row(s)"
   - Added `ValueGeneratedNever().IsConcurrencyToken(false)` to ServiceOption Id
   - Removed explicit `ServiceId` property, used shadow property
   - Removed `DbSet<ServiceOption>` and `DbSet<PriceTier>` from DbContext
   - Configured all inherited base class properties (CreatedAt, CreatedBy, etc.)
   - Moved ServiceOption configuration inline with Service (removed separate file)
   - Files: ServiceOption.cs, PriceTier.cs, ServiceConfiguration.cs, ServiceCatalogDbContext.cs

4. ✅ **Database & Migration Fixes**
   - Fixed connection string format (SQL Server → PostgreSQL)
   - Removed SQL Server EF Core package
   - Added suppression for PendingModelChangesWarning
   - Updated seeder to work with owned entities
   - Files: appsettings.json, Infrastructure.csproj, ServiceOptionSeeder.cs

**Technical Details:**

**Location Synchronization Flow:**
```typescript
User clicks map → Reverse geocode → Get state/city names
→ Match to provinceId/cityId → Update dropdowns

User selects province/city → Geocode name → Get coordinates
→ Update map center → Prevent circular updates with flag
```

**Event-Driven Architecture Flow:**
```csharp
Provider.CreateDraft(ownerFirstName, ownerLastName)
→ Raise ProviderDraftCreatedEvent
→ ProviderDraftCreatedEventHandler
→ Publish ProviderDraftCreatedIntegrationEvent to CAP
→ RabbitMQ message bus
→ ProviderDraftCreatedEventSubscriber (UserManagement)
→ User.Profile.UpdatePersonalInfo()
→ Save to UserManagement database
```

**EF Core 9 Owned Entity Pattern:**
```csharp
// Entity: No explicit FK, manual ID generation
internal static ServiceOption Create(string name, ...) {
    return new ServiceOption { Id = Guid.NewGuid(), ... };
}

// Configuration: Shadow property, ValueGeneratedNever
builder.OwnsMany(s => s.Options, option => {
    option.Property(so => so.Id)
        .ValueGeneratedNever()
        .IsConcurrencyToken(false);
    option.WithOwner().HasForeignKey("ServiceId");
});

// DbContext: No DbSet for owned entities
// ❌ public DbSet<ServiceOption> ServiceOptions => Set<ServiceOption>();
```

**Key Decisions:**

1. **Location Synchronization**
   - Use Neshan Search API for geocoding (free tier sufficient)
   - Combine city + province for accurate results
   - 100ms delay before re-enabling watchers (prevents race conditions)

2. **Event Architecture**
   - Owner names stored in both contexts (ServiceCatalog + UserManagement)
   - Asynchronous cross-context communication via CAP/RabbitMQ
   - UpdatePersonalInfo instead of UpdateName (user preference)

3. **EF Core 9 Owned Entities**
   - Always use `ValueGeneratedNever()` for manual ID generation
   - Never expose owned entities as `DbSet<T>`
   - Always use shadow properties for foreign keys
   - Always configure inherited base class properties

**Files Modified:**
```
Backend:
- ServiceOption.cs (removed ServiceId, CreatedAt properties)
- PriceTier.cs (removed ServiceId property)
- ServiceConfiguration.cs (added ValueGeneratedNever, inline config)
- ServiceCatalogDbContext.cs (removed DbSet, added warning suppression)
- ServiceOptionSeeder.cs (updated for owned entities)
- Provider.cs (added owner name parameters, domain event)
- SaveStep3LocationCommand.cs (added owner name fields)
- ProviderDraftCreatedEvent.cs (NEW)
- ProviderDraftCreatedIntegrationEvent.cs (NEW)
- ProviderDraftCreatedEventHandler.cs (NEW)
- ProviderDraftCreatedEventSubscriber.cs (NEW)
- Infrastructure.csproj (removed SQL Server package)
- appsettings.json (PostgreSQL connection string)

Frontend:
- LocationStep.vue (bidirectional sync implementation)
- useLocations.ts (location data composable)
- .env.development (Neshan API keys)
```

**Testing Verified:**
- ✅ Location map-to-form synchronization
- ✅ Location form-to-map synchronization
- ✅ Owner name event flow (ServiceCatalog → UserManagement)
- ✅ ServiceOption seeder (no more concurrency errors)
- ✅ Database migrations (PostgreSQL)
- ✅ No circular update loops in location selection

**Key Learnings:**

1. **EF Core 9 Breaking Changes**
   - Owned entities require `ValueGeneratedNever()` for manual IDs
   - Stricter validation for entity configuration
   - Must configure ALL properties including inherited ones

2. **StackOverflow as Problem-Solving Resource**
   - Found exact solution for EF Core 9 owned entity issue
   - Reference: https://stackoverflow.com/questions/79219671/

3. **Bidirectional Sync Complexity**
   - Need flags to prevent infinite loops
   - Geocoding API calls can be slow (consider caching)
   - Combining location names improves accuracy

**Next Steps:**
- Test complete registration flow with all steps
- Implement service creation and management
- Add staff management functionality
- Implement business hours configuration
- Add gallery/image upload features

---

## Document Revision History

| Date       | Version | Changes                                           | Author       |
|------------|---------|---------------------------------------------------|--------------|
| 2025-11-11 | 4.0.0   | Added location integration, event architecture, EF Core 9 fixes | Claude (AI)  |
| 2025-11-09 | 3.0.0   | Comprehensive documentation of auth & registration| Claude (AI)  |
| 2025-11-06 | 2.0.0   | Consolidated all technical documentation          | Claude (AI)  |
| 2025-11-05 | 1.0.0   | Initial comprehensive documentation created       | Claude (AI)  |

---

*This is a living document. It consolidates all technical documentation from the project into a single, searchable reference. Update this file whenever significant changes are made to the architecture, authentication flow, or critical components.*
