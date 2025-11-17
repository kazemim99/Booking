# Split Customer and Provider Login Pages

## Why

**Current Problem:**
The single `/login` page uses complex redirect-path-based detection logic to determine if a user is a Customer or Provider. This approach:
- Is fragile and error-prone (can register users as wrong type)
- Requires maintaining a hardcoded list of "customer routes"
- Creates confusion for users (page says "for providers" but customers use it)
- Relies on sessionStorage for state passing between login → verification
- Makes the codebase harder to maintain and test

**Opportunity:**
Separate login pages provide clear user journeys for each audience, eliminate complex detection logic, and follow the principle of "explicit over implicit."

## What Changes

### Frontend Changes:
1. **New Route**: Create `/provider/login` for provider authentication
2. **Update Route**: Keep `/login` for customer authentication (with updated messaging)
3. **Update Components**:
   - Create `ProviderLoginView.vue` (duplicate of current LoginView with provider-specific messaging)
   - Update `LoginView.vue` with customer-specific messaging ("رزرو کنید")
   - Update both views to pass `userType` explicitly (no detection logic)
4. **Remove Logic**:
   - Remove redirect path detection (lines 98-129 in LoginView.vue)
   - Remove `registration_user_type` from sessionStorage
   - Simplify VerificationView to receive userType via route state/query
5. **Update Navigation**:
   - HeroSection: "رزرو کنید" button → `/login`
   - Footer/Header: "برای کسب‌وکارها" link → `/provider/login`
   - Provider Dashboard nav links → `/provider/login`

### Benefits:
- ✅ **Simplified Logic**: Remove 35+ lines of complex detection code
- ✅ **Better UX**: Clear messaging for each audience
- ✅ **Easier Maintenance**: No need to update customer route lists
- ✅ **Better SEO**: Separate pages with audience-specific meta tags
- ✅ **Explicit State**: Direct userType passing, no sessionStorage dependency

## Impact

### Affected Specs:
- `authentication` - Login flow changes (MODIFIED)

### Affected Code:
- `booksy-frontend/src/modules/auth/views/LoginView.vue` - Update with customer-specific messaging
- `booksy-frontend/src/modules/auth/views/ProviderLoginView.vue` - NEW file
- `booksy-frontend/src/modules/auth/views/VerificationView.vue` - Simplify userType handling
- `booksy-frontend/src/core/router/routes/auth.routes.ts` - Add provider login route
- `booksy-frontend/src/components/landing/HeroSection.vue` - Update CTA to `/login`
- `booksy-frontend/src/components/layout/Footer.vue` - Add provider login link
- `booksy-frontend/src/components/layout/Header.vue` - Add provider login link

### Breaking Changes:
- **NONE** - This is backward compatible. Existing `/login` continues to work, just with clearer customer focus.
- Direct links to `/login?redirect=...` still work (but redirect detection is removed, so userType must be explicit or default to Customer)

### Migration:
- Provider users accessing `/login` directly will be shown customer login. They should use `/provider/login` instead.
- Update all internal links pointing to provider login to use new route.
