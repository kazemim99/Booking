# Provider Status-Based Redirect on Login

## Why
Currently, when users with the Provider role log in, they are redirected directly to their dashboard regardless of their Provider registration status now we check UserStatus guard processing. This creates a poor user experience when providers have not yet completed their profile setup (status = Drafted) or prvider with UserId(OwnerId). Users may access incomplete dashboards or encounter errors when trying to use features that require a complete provider profile.

## What Changes
- Add Provider status check during login and navigation guard processing
- Implement automatic redirection based on Provider status:
  - **Drafted status**: Redirect to Provider Registration Flow to complete profile
  - **No Provider record**: Redirect to Provider Registration Flow to create profile
  - **PendingVerification status**: Redirect to Provider Dashboard with appropriate status messaging
  - **Verified/Active status**: Normal dashboard access
- Create backend API endpoint to fetch current user's Provider status
- Update frontend authentication flow to check Provider status after successful login
- Update navigation guards to enforce Provider status-based routing

## Impact

### Affected Specs
- **provider-onboarding** (NEW): Provider registration and onboarding flow specification
- **authentication**: User login flow and post-authentication routing

### Affected Code

#### Backend
- `Booksy.ServiceCatalog.Application`: New query `GetCurrentProviderStatusQuery`
- `Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs`: New endpoint `GET /api/v1/providers/current/status`
- Provider status checking logic

#### Frontend
- `booksy-frontend/src/core/stores/modules/auth.store.ts`: Enhanced `redirectToDashboard()` with Provider status check
- `booksy-frontend/src/core/router/guards/auth.guard.ts`: Enhanced to check Provider status
- `booksy-frontend/src/modules/provider/api/provider.api.ts`: New API method `getCurrentProviderStatus()`
- Router configuration for Provider Registration route

### Breaking Changes
None. This is an enhancement to existing authentication flow that improves UX without breaking existing functionality.

### Dependencies
- Existing ProviderStatus enum (Drafted, PendingVerification, Verified, Active, Inactive, Suspended, Archived)
- Existing Provider aggregate and repository
- Existing authentication store and guards
- Existing Provider Registration Flow component
