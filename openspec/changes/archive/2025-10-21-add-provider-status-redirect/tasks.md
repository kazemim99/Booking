# Implementation Tasks

## 1. Backend Implementation

- [x] 1.1 Create `GetCurrentProviderStatusQuery` in `Booksy.ServiceCatalog.Application/Queries/Provider/GetCurrentProviderStatus/`
  - [x] 1.1.1 Create query class with no parameters (uses authenticated user context)
  - [x] 1.1.2 Create query result DTO with ProviderId and ProviderStatus
  - [x] 1.1.3 Create query handler to fetch Provider by UserId
  - [x] 1.1.4 Add FluentValidation validator (minimal, for consistency)

- [x] 1.2 Add API endpoint to `ProvidersController.cs`
  - [x] 1.2.1 Add `GET /api/v1/providers/current/status` endpoint
  - [x] 1.2.2 Add `[Authorize]` attribute requiring authentication
  - [x] 1.2.3 Add role-based authorization for Provider role
  - [x] 1.2.4 Handle case where Provider record doesn't exist (return 404)
  - [x] 1.2.5 Add Swagger/OpenAPI documentation for endpoint

- [x] 1.3 Update Provider Repository (if needed)
  - [x] 1.3.1 Add `GetByUserIdAsync` method if not already present (verified existing GetByOwnerIdAsync)
  - [x] 1.3.2 Ensure efficient query (select only necessary fields)

- [ ] 1.4 Add unit tests
  - [ ] 1.4.1 Test GetCurrentProviderStatusQueryHandler with existing provider
  - [ ] 1.4.2 Test handler with non-existent provider
  - [ ] 1.4.3 Test all ProviderStatus enum values

- [x] 1.5 Add integration tests
  - [x] 1.5.1 Test API endpoint with authenticated Provider user
  - [x] 1.5.2 Test endpoint returns 404 for user without Provider record
  - [x] 1.5.3 Test endpoint returns 401 for unauthenticated request
  - [x] 1.5.4 Test all ProviderStatus enum values (Drafted, PendingVerification, Active, Inactive, Suspended)
  - [x] 1.5.5 Test multiple providers returns only current user's provider

## 2. Frontend Implementation

- [x] 2.1 Create Provider API service
  - [x] 2.1.1 Add `getCurrentProviderStatus()` method in `booksy-frontend/src/modules/provider/services/provider.service.ts`
  - [x] 2.1.2 Define TypeScript interface for status response
  - [x] 2.1.3 Handle API errors gracefully

- [x] 2.2 Update authentication store
  - [x] 2.2.1 Add `providerStatus` state field in `auth.store.ts`
  - [x] 2.2.2 Add `providerId` state field
  - [x] 2.2.3 Add `setProviderStatus()` action
  - [x] 2.2.4 Add `fetchProviderStatus()` async action
  - [x] 2.2.5 Update `redirectToDashboard()` to check Provider status before redirecting
  - [x] 2.2.6 Clear provider status on logout
  - [x] 2.2.7 Add computed property `needsProfileCompletion` (status === 'Drafted' || no provider)
  - [x] 2.2.8 Add computed property `isPendingVerification` (status === 'PendingVerification')
  - [x] 2.2.9 Add `login()` method that calls `fetchProviderStatus()` for Provider users

- [x] 2.3 Update navigation guards
  - [x] 2.3.1 Enhance `auth.guard.ts` to check Provider status
  - [x] 2.3.2 Redirect Drafted providers to registration flow
  - [x] 2.3.3 Prevent completed providers from accessing registration route
  - [x] 2.3.4 Handle loading states during status check
  - [x] 2.3.5 Add error handling for failed status checks

- [ ] 2.4 Update router configuration
  - [ ] 2.4.1 Add Provider Registration route if not already present
  - [ ] 2.4.2 Configure route meta for proper guard behavior
  - [ ] 2.4.3 Add Provider Dashboard route with appropriate meta

- [x] 2.5 Create/Update Provider Dashboard
  - [x] 2.5.1 Add status banner for PendingVerification status
  - [x] 2.5.2 Add status banner for Inactive/Suspended/Archived status
  - [x] 2.5.3 Show appropriate messaging based on status

- [x] 2.6 Update login flow
  - [x] 2.6.1 Call `fetchProviderStatus()` after successful login for Provider users
  - [x] 2.6.2 Handle async status fetch with loading indicator
  - [x] 2.6.3 Redirect based on fetched status

## 3. Testing

- [ ] 3.1 Backend testing (see 1.4 and 1.5 above)

- [ ] 3.2 Frontend unit tests
  - [ ] 3.2.1 Test auth store `fetchProviderStatus()` action
  - [ ] 3.2.2 Test `redirectToDashboard()` with different Provider statuses
  - [ ] 3.2.3 Test navigation guard behavior with various status scenarios
  - [ ] 3.2.4 Test provider API service error handling

- [ ] 3.3 E2E tests
  - [ ] 3.3.1 Test login flow for Provider with Drafted status → redirects to registration
  - [ ] 3.3.2 Test login flow for Provider with PendingVerification → redirects to dashboard with message
  - [ ] 3.3.3 Test login flow for Provider with Active status → redirects to dashboard
  - [ ] 3.3.4 Test navigation guard prevents Drafted provider from accessing other routes
  - [ ] 3.3.5 Test completed provider cannot access registration route
  - [ ] 3.3.6 Test status update after completing registration

## 4. Documentation

- [ ] 4.1 Update API documentation
  - [ ] 4.1.1 Document new endpoint in Swagger/OpenAPI
  - [ ] 4.1.2 Add examples for different status values

- [ ] 4.2 Update frontend documentation
  - [ ] 4.2.1 Document Provider status flow in code comments
  - [ ] 4.2.2 Add JSDoc for new methods and types

## 5. Deployment Considerations

- [ ] 5.1 Verify backward compatibility
  - [ ] 5.1.1 Ensure existing users are not blocked
  - [ ] 5.1.2 Test with existing Provider records

- [ ] 5.2 Database check
  - [ ] 5.2.1 Verify all existing Providers have valid status values
  - [ ] 5.2.2 Check for any NULL status fields

- [ ] 5.3 Monitoring
  - [ ] 5.3.1 Add logging for status checks
  - [ ] 5.3.2 Monitor redirect patterns after deployment
