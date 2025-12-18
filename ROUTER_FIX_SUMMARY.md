# Router Configuration Fix - Summary

**Date**: 2025-12-12
**Issue**: Router error when clicking rebook/reschedule buttons: `Uncaught (in promise) Error: No match for {"name":"Unauthorized","params":{}}`

## Root Causes Identified

### 1. Missing "Unauthorized" Route ❌ → ✅
**Problem**:
- The `auth.guard.ts` tried to redirect to a route named `"Unauthorized"` (line 113)
- This route didn't exist in the router configuration
- Caused Vue Router error: `No match for {"name":"Unauthorized"}`

**Fix Applied**:
Added the missing route in [router/index.ts](booksy-frontend/src/core/router/index.ts:31-38):

```typescript
{
  path: '/unauthorized',
  name: 'Unauthorized',
  component: () => import('@/shared/components/layout/Forbidden.vue'),
  meta: {
    title: '401 - Unauthorized',
  },
}
```

### 2. Case Mismatch in Role Name ❌ → ✅
**Problem**:
- Backend returns role as `"Customer"` (capital C)
- Frontend route checked for `'customer'` (lowercase c)
- Role guard blocked access to all `/customer/*` routes

**Backend Evidence** (from C# code):
```csharp
[Authorize(Roles = "Customer,Admin")]  // Capital C
if (user.Roles.Any(r => r.Name == "Customer" || r.Name == "Client"))
```

**Fix Applied**:
Changed role requirement in [customer.routes.ts](booksy-frontend/src/core/router/routes/customer.routes.ts:14):

```diff
meta: {
  requiresAuth: true,
- roles: ['customer'],
+ roles: ['Customer'],
},
```

## How The Error Occurred

**Flow Breakdown**:

1. User clicks "رزرو مجدد" (Rebook) or "تغییر زمان" (Reschedule)
2. `BookingsSidebar.vue` calls `router.push({ name: 'CustomerBooking', ... })`
3. Vue Router starts navigation to `CustomerBooking` route
4. **Auth Guard** checks authentication ✅ (user is logged in)
5. **Auth Guard** checks role requirements:
   - Route requires: `['customer']`
   - User has: `['Customer']`
   - **Case mismatch** → role check fails ❌
6. Auth guard tries to redirect: `next({ name: 'Unauthorized' })`
7. **Router error**: `"Unauthorized"` route doesn't exist ❌
8. Vue Router throws error and navigation fails

## Files Modified

### 1. ✅ [booksy-frontend/src/core/router/index.ts](booksy-frontend/src/core/router/index.ts)
**Change**: Added missing "Unauthorized" route

**Before**:
```typescript
// Error routes
{
  path: '/forbidden',
  name: 'Forbidden',
  // ...
},
```

**After**:
```typescript
// Error routes
{
  path: '/unauthorized',
  name: 'Unauthorized',
  component: () => import('@/shared/components/layout/Forbidden.vue'),
  meta: {
    title: '401 - Unauthorized',
  },
},
{
  path: '/forbidden',
  name: 'Forbidden',
  // ...
},
```

### 2. ✅ [booksy-frontend/src/core/router/routes/customer.routes.ts](booksy-frontend/src/core/router/routes/customer.routes.ts)
**Change**: Fixed role case from `'customer'` to `'Customer'`

**Before**:
```typescript
{
  path: '/customer',
  component: () => import('@/modules/customer/layouts/CustomerLayout.vue'),
  meta: {
    requiresAuth: true,
    roles: ['customer'],  // ❌ Wrong case
  },
  children: [...]
}
```

**After**:
```typescript
{
  path: '/customer',
  component: () => import('@/modules/customer/layouts/CustomerLayout.vue'),
  meta: {
    requiresAuth: true,
    roles: ['Customer'],  // ✅ Matches backend
  },
  children: [...]
}
```

## Testing Checklist

### Test Scenarios

- [ ] **Rebook Flow**:
  1. Open BookingsSidebar
  2. Go to "گذشته" (Past) tab
  3. Click "رزرو مجدد" (Rebook) on a booking
  4. Should redirect to `/customer/book` successfully
  5. No router errors in console

- [ ] **Reschedule Flow**:
  1. Open BookingsSidebar
  2. Stay on "آینده" (Upcoming) tab
  3. Click "تغییر زمان" (Reschedule) on a booking
  4. Should redirect to `/customer/book?reschedule={id}` successfully
  5. No router errors in console

- [ ] **Cancel Flow** (should still work):
  1. Click "لغو رزرو" (Cancel)
  2. Modal opens
  3. Select reason and confirm
  4. Booking cancelled successfully

- [ ] **Unauthorized Access** (optional):
  1. Try to access `/customer/book` without authentication
  2. Should redirect to login page
  3. After login, should redirect back to booking page

## Related Files

### Router Guards
- [auth.guard.ts](booksy-frontend/src/core/router/guards/auth.guard.ts) - Contains the `Unauthorized` redirect
- [role.guard.ts](booksy-frontend/src/core/router/guards/role.guard.ts) - Checks role requirements

### Affected Components
- [BookingsSidebar.vue](booksy-frontend/src/modules/customer/components/modals/BookingsSidebar.vue) - Triggers navigation
- [BookingWizardView.vue](booksy-frontend/src/modules/customer/views/BookingWizardView.vue) - Target component

### Backend Role Configuration
- [CustomersController.cs](src/UserManagement/Booksy.UserManagement.API/Controllers/V1/CustomersController.cs) - Uses `"Customer"` role
- [AuthenticateUserCommandHandler.cs](src/UserManagement/Booksy.UserManagement.Application/CQRS/Commands/AuthenticateUser/AuthenticateUserCommandHandler.cs) - Returns `"Customer"` role in JWT

## Additional Notes

### Role Naming Convention

**Backend** (C# / .NET):
- Uses PascalCase: `"Customer"`, `"Provider"`, `"Admin"`
- Defined in JWT claims
- Used in `[Authorize(Roles = "Customer")]` attributes

**Frontend** (TypeScript / Vue):
- Should match backend exactly
- Case-sensitive comparison in `hasAnyRole()` method
- **Important**: Always use `'Customer'`, not `'customer'`

### JWT Token Structure

When user logs in, the backend returns a JWT with roles:

```json
{
  "sub": "user-id-123",
  "email": "customer@example.com",
  "roles": ["Customer"],  // ← Capital C
  "exp": 1234567890
}
```

The frontend auth store parses this and stores roles exactly as received.

### Auth Guard Logic

```typescript
// auth.guard.ts (line 108-116)
if (requiresAuth && requiredRoles && requiredRoles.length > 0) {
  const hasRequiredRole = authStore.hasAnyRole(requiredRoles)

  if (!hasRequiredRole) {
    next({ name: 'Unauthorized' })  // ← Now this route exists
    return
  }
}
```

### Common Pitfalls

1. **❌ Using lowercase role names in routes**
   ```typescript
   // Wrong
   meta: { roles: ['customer', 'provider'] }

   // Correct
   meta: { roles: ['Customer', 'Provider'] }
   ```

2. **❌ Not handling all redirect routes**
   - Always ensure routes referenced in guards exist
   - Common redirect targets: `Login`, `Unauthorized`, `Forbidden`, `NotFound`

3. **❌ Forgetting to update both parent and child routes**
   - Parent route role restrictions apply to ALL children
   - Child routes can add additional restrictions but can't remove parent restrictions

## Verification Steps

After applying these fixes, verify:

1. ✅ User with "Customer" role can access `/customer/*` routes
2. ✅ Clicking rebook/reschedule navigates successfully
3. ✅ No "Unauthorized" router errors in console
4. ✅ Proper error page shown for unauthorized access
5. ✅ All booking actions work end-to-end

## Status

**Status**: ✅ **RESOLVED**
**Fixes Applied**: 2
1. Added missing "Unauthorized" route
2. Fixed role case mismatch in customer routes

**Action Required**: Test the booking actions to confirm navigation works correctly
