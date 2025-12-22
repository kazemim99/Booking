# Provider Registration Flow Update - December 2025

## Overview

As of December 21, 2025, the provider registration flow has been simplified to support only **Organization Registration**. The Individual Registration flow has been disabled and all new providers register as Organizations.

---

## Changes Made

### 1. **Disabled Individual Registration Route**

**File**: `booksy-frontend/src/core/router/routes/provider.routes.ts`

The `IndividualRegistration` route has been commented out and is no longer accessible:

```typescript
// Individual Registration Flow - DISABLED: Everyone registers as Organization
// {
//   path: '/provider/registration/individual',
//   name: 'IndividualRegistration',
//   ...
// }
```

### 2. **Automatic Redirect to Organization Registration**

**File**: `booksy-frontend/src/modules/provider/views/registration/ProviderRegistrationView.vue`

When users access `/provider/registration`, they are automatically redirected to the Organization Registration flow:

```typescript
onMounted(() => {
  initializeRTL()
  // Automatically redirect to OrganizationRegistration since everyone registers as an Organization
  router.push({ name: 'OrganizationRegistration' })
})
```

### 3. **Updated Auth Guard**

**File**: `booksy-frontend/src/core/router/guards/auth.guard.ts`

Removed `IndividualRegistration` from the list of allowed registration routes:

```typescript
// Note: IndividualRegistration is disabled - everyone registers as Organization
const registrationRoutes = ['ProviderRegistration', 'OrganizationRegistration']
```

### 4. **Updated Auth Store**

**File**: `booksy-frontend/src/core/stores/modules/auth.store.ts`

Removed `IndividualRegistration` from registration route checks:

```typescript
// Note: IndividualRegistration is disabled - everyone registers as Organization
const registrationRoutes = ['ProviderRegistration', 'OrganizationRegistration']
```

---

## Current Registration Flow

### Organization Registration (8 Steps)

**Route**: `/provider/registration/organization`

**Component**: `OrganizationRegistrationFlow.vue`

**Steps**:

1. **Organization Info** - Business name, legal structure, tax ID, owner information
2. **Category Selection** - Choose business category/industry
3. **Location** - Physical address and map location
4. **Services** - Define services offered with pricing and duration
5. **Working Hours** - Set business operating hours for each day
6. **Gallery** - Upload business photos and portfolio images
7. **Preview & Confirm** - Review all information before submission
8. **Completion** - Success screen with dashboard navigation

**Progress Indicator**: Shows "Step X of 8" throughout the flow

---

## User Journey

### Before (Previous Flow)

```
User visits /provider/registration
  ↓
Chooses between Individual or Organization
  ↓
Individual → IndividualRegistrationFlow (8 steps)
Organization → OrganizationRegistrationFlow (8 steps)
```

### After (Current Flow)

```
User visits /provider/registration
  ↓
Automatically redirected to /provider/registration/organization
  ↓
OrganizationRegistrationFlow (8 steps)
  ↓
Complete registration as Organization
```

---

## Backend Implications

### Provider Hierarchy

All new providers are created with:
- **HierarchyType**: `Organization`
- **Provider entity** is created in the ServiceCatalog bounded context
- Owner is automatically added as the first staff member

### API Endpoints Used

- `POST /api/hierarchy/register-organization` - Create new organization
- `PUT /api/providers/{id}/services` - Save services
- `PUT /api/providers/{id}/business-hours` - Save working hours
- `POST /api/providers/{id}/gallery` - Upload gallery images
- `POST /api/providers/{id}/complete-registration` - Finalize registration

---

## Migration Notes

### For Existing Code

- **IndividualRegistrationFlow.vue** still exists in the codebase but is **not accessible** via routing
- The component can be safely removed in a future cleanup if needed
- All hierarchy guard checks (`independentIndividualOnlyGuard`) remain functional for existing individual providers

### For Future Development

If Individual Registration needs to be re-enabled:

1. Uncomment the route in `provider.routes.ts` (lines 106-127)
2. Add `'IndividualRegistration'` back to `registrationRoutes` arrays in:
   - `auth.guard.ts`
   - `auth.store.ts`
3. Remove or modify the auto-redirect in `ProviderRegistrationView.vue`
4. Restore the provider type selection UI

---

## Testing Checklist

- [x] Users accessing `/provider/registration` are redirected to organization flow
- [x] Organization registration completes successfully
- [x] All 8 steps save data correctly to backend
- [x] Progress indicator shows "X of 8" correctly
- [x] Completion step navigates to dashboard
- [x] Auth guard allows dashboard access for incomplete providers
- [x] No broken links or 404 errors for individual registration

---

## Related Files

### Frontend
- `booksy-frontend/src/core/router/routes/provider.routes.ts` - Route definitions
- `booksy-frontend/src/core/router/guards/auth.guard.ts` - Authentication guard
- `booksy-frontend/src/core/stores/modules/auth.store.ts` - Auth state management
- `booksy-frontend/src/modules/provider/views/registration/ProviderRegistrationView.vue` - Registration entry point
- `booksy-frontend/src/modules/provider/views/registration/OrganizationRegistrationFlow.vue` - 8-step organization flow
- `booksy-frontend/src/modules/provider/views/registration/IndividualRegistrationFlow.vue` - Disabled individual flow
- `booksy-frontend/src/modules/provider/components/registration/RegistrationProgressIndicator.vue` - Step indicator

### Backend
- `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.API/Controllers/HierarchyController.cs` - Organization registration endpoint
- `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.API/Controllers/ProvidersController.cs` - Provider management endpoints

---

## Questions & Answers

**Q: Why was Individual Registration disabled?**

A: Business decision to simplify the registration process. All providers now register as Organizations, which provides more flexibility for future growth (adding staff, managing teams, etc.).

**Q: Can existing Individual providers still use the system?**

A: Yes, existing Individual providers are unaffected. The hierarchy guard (`independentIndividualOnlyGuard`) still works for them. This change only affects **new registrations**.

**Q: What happens to the `ProviderTypeSelection` component?**

A: It still renders briefly in `ProviderRegistrationView.vue` but users are immediately redirected before they can interact with it. This can be cleaned up in a future refactor.

**Q: Is this change reversible?**

A: Yes, the Individual Registration flow can be re-enabled by uncommenting the route and removing the auto-redirect logic. See "For Future Development" section above.

---

## Impact Summary

### Positive Impacts
✅ Simplified user experience - no confusion about Individual vs Organization
✅ Reduced maintenance - only one registration flow to maintain
✅ Consistent provider structure - all providers have organization capabilities
✅ Easier onboarding - fewer choices to make during registration

### Considerations
⚠️ Individual providers (solopreneurs) must register as organizations
⚠️ Migration path exists if business requirements change
⚠️ Documentation and help content should be updated to reflect this change

---

**Date**: December 21, 2025
**Version**: 1.0
**Author**: Development Team
**Status**: Implemented & Active
