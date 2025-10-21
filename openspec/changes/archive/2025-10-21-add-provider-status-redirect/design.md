# Design Document: Provider Status-Based Redirect on Login

## Context

Currently, the Booksy application has a Provider registration flow, but users with the Provider role are always redirected to their dashboard after login, regardless of whether they've completed their profile setup. This leads to a poor UX where:

1. Users with incomplete profiles see an empty or broken dashboard
2. There's no clear path to complete registration
3. Pending verification status is not communicated effectively

The Provider entity has a `ProviderStatus` enum with values: Drafted, PendingVerification, Verified, Active, Inactive, Suspended, and Archived. We need to leverage this status to guide users appropriately.

### Stakeholders
- **Provider users**: Need clear guidance on completing registration and understanding their status
- **Admins**: Need users to complete registration before accessing full features
- **Frontend team**: Implementing navigation guards and state management
- **Backend team**: Exposing Provider status API

## Goals / Non-Goals

### Goals
1. Redirect users with incomplete Provider profiles (Drafted status) to complete registration
2. Redirect users without Provider records to create their profile
3. Show appropriate messaging for users pending verification (PendingVerification status)
4. Prevent incomplete providers from accessing dashboard features
5. Maintain security by checking status server-side and client-side
6. Provide a seamless UX with minimal loading delays

### Non-Goals
1. Changing the Provider registration flow itself (already exists)
2. Modifying the ProviderStatus enum values
3. Implementing automated status transitions (e.g., auto-approval)
4. Creating new dashboard features based on status
5. Handling status transitions for Inactive/Suspended/Archived (future work)

## Decisions

### Decision 1: Server-Side Provider Status API
**What**: Create a dedicated API endpoint `GET /api/v1/providers/current/status` that returns only the authenticated user's Provider status and ID.

**Why**:
- Lightweight and focused endpoint (doesn't return full Provider aggregate)
- Faster response time for status checks
- Reduces frontend complexity (no need to fetch and filter full provider details)
- Can be called on every login without performance concerns
- Follows RESTful conventions for current user resources

**Alternatives Considered**:
- **Alt 1: Include status in JWT token claims**
  - Pros: No additional API call needed
  - Cons: Token becomes stale when status changes; requires re-login to update; increases token size
- **Alt 2: Fetch full Provider details**
  - Pros: Single endpoint for all provider data
  - Cons: Slower response; unnecessary data transfer; couples status check to full provider fetch
- **Alt 3: Include status in user profile endpoint**
  - Pros: Reuse existing endpoint
  - Cons: Mixes UserManagement and ServiceCatalog concerns; violates bounded context separation

**Decision**: Use dedicated endpoint for better separation of concerns and performance.

### Decision 2: Client-Side Status Storage in Auth Store
**What**: Store `providerStatus` and `providerId` in the authentication store (Pinia) alongside user data.

**Why**:
- Status is authentication-related state (affects routing and access)
- Auth store already handles post-login redirects
- Avoids creating a separate provider store just for status
- Easy to clear on logout alongside other auth state
- Accessible from navigation guards

**Alternatives Considered**:
- **Alt 1: Create separate provider store**
  - Pros: Better separation of concerns
  - Cons: Overkill for just status tracking; complicates state management; requires coordination between stores
- **Alt 2: Store only in localStorage**
  - Pros: Persists across page refreshes
  - Cons: No reactivity; harder to keep in sync; security concerns
- **Alt 3: Fetch status on every route navigation**
  - Pros: Always up-to-date
  - Cons: Performance impact; unnecessary API calls; bad UX

**Decision**: Auth store provides the right balance of reactivity, simplicity, and performance.

### Decision 3: Navigation Guard Enforcement
**What**: Enhance the existing `auth.guard.ts` to check Provider status and enforce routing rules.

**Why**:
- Navigation guards are the correct pattern for route protection in Vue Router
- Already have auth guard infrastructure
- Runs before route transition, preventing unauthorized access
- Can redirect before component mounting
- Works seamlessly with async operations

**Implementation Details**:
```typescript
// Pseudo-code for auth guard enhancement
if (user.hasRole('Provider')) {
  // Fetch status if not already in store
  if (!authStore.providerStatus) {
    await authStore.fetchProviderStatus()
  }

  // Enforce routing based on status
  if (authStore.providerStatus === 'Drafted' && to.name !== 'ProviderRegistration') {
    next({ name: 'ProviderRegistration' })
    return
  }

  if ((authStore.providerStatus === 'Verified' || authStore.providerStatus === 'Active')
      && to.name === 'ProviderRegistration') {
    next({ name: 'ProviderDashboard' })
    return
  }
}
```

**Alternatives Considered**:
- **Alt 1: Check in every component**
  - Pros: Flexible per-component logic
  - Cons: Repetitive code; easy to miss; inconsistent enforcement
- **Alt 2: Use route meta + separate guard**
  - Pros: Explicit route configuration
  - Cons: Duplicates logic; harder to maintain
- **Alt 3: Check only in redirectToDashboard**
  - Pros: Simple initial implementation
  - Cons: Doesn't prevent direct navigation to dashboard routes; security gap

**Decision**: Navigation guard provides centralized, consistent enforcement.

### Decision 4: Status Check Timing
**What**: Fetch Provider status immediately after successful login (for Provider users only), and cache in store for duration of session.

**Why**:
- Minimizes API calls (once per session)
- Ensures status is available before first navigation
- Acceptable UX trade-off (small delay after login)
- Guards can use cached status for fast routing decisions

**Cache Invalidation**:
- Clear on logout
- Refresh after completing registration
- Optional: Refresh on focus (future enhancement)

**Alternatives Considered**:
- **Alt 1: Fetch on every navigation**
  - Cons: Performance impact; poor UX
- **Alt 2: Never cache, always fetch**
  - Cons: Unnecessary load; slower navigation
- **Alt 3: Cache indefinitely**
  - Cons: Stale data if status changes server-side

**Decision**: Session-scoped cache with explicit invalidation points.

### Decision 5: Error Handling Strategy
**What**: If Provider status fetch fails, default to safe fallback behavior (redirect to home or show error page) and log error for monitoring.

**Why**:
- Security: Don't grant access if status is unknown
- UX: Better to show an error than a broken dashboard
- Monitoring: Alerts team to API issues

**Fallback Behavior**:
1. Log error to monitoring (Sentry)
2. Show user-friendly error message
3. Redirect to safe route (home page or error page)
4. Allow retry option

**Alternatives Considered**:
- **Alt 1: Allow access on error**
  - Cons: Security risk; could allow drafted providers to access dashboard
- **Alt 2: Block all access**
  - Cons: Too restrictive; breaks app for transient errors
- **Alt 3: Infinite retry**
  - Cons: Poor UX; could loop indefinitely

**Decision**: Graceful degradation with retry option.

### Decision 6: Provider Status Response Format
**What**: Return minimal JSON response from status endpoint:
```json
{
  "providerId": "uuid",
  "status": "Drafted",
  "userId": "uuid"
}
```

For users without Provider record, return 404 with:
```json
{
  "success": false,
  "message": "Provider record not found",
  "errorCode": "PROVIDER_NOT_FOUND"
}
```

**Why**:
- Lightweight response
- Clear semantics (404 = no provider exists, 200 = provider exists)
- Frontend can easily distinguish between "no provider" and "drafted provider"
- Follows existing API response patterns in the project

## Risks / Trade-offs

### Risk 1: Additional API Call on Login
**Impact**: Slight increase in login latency (estimated +50-200ms)

**Mitigation**:
- Optimize query to fetch only status (no joins unless necessary)
- Use indexed UserId lookup
- Consider caching strategy if performance becomes an issue
- Only call for Provider users (not all users)

**Trade-off**: Acceptable for better UX and security

### Risk 2: Status Sync Between Frontend and Backend
**Impact**: Frontend cache could become stale if status changes server-side (e.g., admin approves provider)

**Mitigation**:
- Clear cache on logout
- Refresh after registration completion
- Future: Implement WebSocket or polling for real-time updates
- Future: Add "refresh status" button in dashboard

**Trade-off**: Session-scoped staleness is acceptable for v1

### Risk 3: Redirect Loop Potential
**Impact**: If registration route is misconfigured or status check fails, could create redirect loop

**Mitigation**:
- Explicitly exclude registration route from status check guard
- Add redirect loop detection in navigation guard
- Comprehensive E2E tests for all status scenarios
- Logging to detect loops in production

**Trade-off**: Careful implementation and testing required

### Risk 4: Breaking Existing Provider Users
**Impact**: Existing users with null or unexpected status values could be blocked

**Mitigation**:
- Verify all existing Providers have valid status values before deployment
- Add migration script if needed to set default status
- Add defensive checks for null/undefined status
- Monitor error rates after deployment

**Trade-off**: Need pre-deployment data validation

## Migration Plan

### Pre-Deployment
1. **Database Audit**:
   - Query all Provider records to check status values
   - Identify any NULL or invalid status values
   - Create migration script if needed (e.g., set Drafted → Active for existing providers)

2. **Backend Deployment**:
   - Deploy API endpoint first (no breaking changes)
   - Verify endpoint works with existing data
   - Monitor error rates

3. **Frontend Deployment**:
   - Deploy navigation guard changes
   - Enable feature flag if using gradual rollout
   - Monitor redirect patterns and error rates

### Post-Deployment
1. **Monitoring**:
   - Track status API call rates and latency
   - Monitor redirect patterns (how many users hit registration vs dashboard)
   - Watch for error spikes or redirect loops
   - User feedback on UX

2. **Rollback Plan**:
   - If critical issues, disable navigation guard logic via feature flag
   - Revert to original redirectToDashboard behavior
   - Keep API endpoint (no harm)

### Future Enhancements
1. Real-time status updates via WebSocket
2. Status banner components for all status types
3. Admin interface to change provider status
4. Automated status transitions (e.g., Drafted → PendingVerification on registration submit)
5. Analytics on conversion rates (Drafted → Active)

## Open Questions

1. **Q: Should we show a loading indicator during status fetch on login?**
   - A: Yes, brief spinner or skeleton is good UX for the 50-200ms delay

2. **Q: What should happen to Inactive/Suspended/Archived providers?**
   - A: Out of scope for this change. Redirect to dashboard with status message. Future work: dedicated status pages.

3. **Q: Should status check happen on every page refresh or just login?**
   - A: Just login for v1. Future: refresh on focus or periodically.

4. **Q: Should we persist provider status in localStorage for offline scenarios?**
   - A: No for v1, to avoid stale data. Future: consider with proper invalidation strategy.

5. **Q: Do we need analytics events for tracking status-based redirects?**
   - A: Yes, recommended to add analytics events for monitoring user flow and identifying bottlenecks.

## Implementation Notes

### Backend
- Use existing MediatR pattern for query handler
- Follow existing repository pattern for data access
- Add to existing ProvidersController (v1)
- Use `[Authorize]` attribute and role checking
- Return 404 for non-existent provider (not 200 with null)

### Frontend
- Use existing Pinia auth store
- Follow existing navigation guard patterns
- Add TypeScript interfaces for status response
- Handle loading states with existing UI patterns
- Add error boundaries for graceful degradation

### Testing
- Unit test query handler with various status values
- Integration test API endpoint with auth contexts
- E2E test all login scenarios with different statuses
- Test redirect loop prevention
- Test error handling and retry flows
