# Provider Hierarchy - Integration Testing Guide

**Date:** 2025-11-25
**Status:** ✅ Build Verified - Ready for Testing
**Build Result:** 0 Errors, 137 Warnings (non-blocking)

---

## Build Status

✅ **Application Layer**: Built successfully
✅ **API Layer**: Built successfully
✅ **All Endpoints**: Compilable and deployable

**Compilation completed with 0 errors.**

---

## Testing Overview

This guide provides step-by-step instructions for testing the newly implemented MVP endpoints and the complete Provider Hierarchy feature.

### New Endpoints to Test

1. **GET** `/api/v1/providers/{providerId}/hierarchy/join-requests/sent`
   - Get join requests sent by an individual provider

2. **DELETE** `/api/v1/providers/{providerId}/hierarchy/join-requests/{requestId}`
   - Cancel a pending join request

---

## Prerequisites

### Backend Setup

```bash
# Navigate to API project
cd c:\Repos\Booking\src\BoundedContexts\ServiceCatalog\Booksy.ServiceCatalog.Api

# Run database migrations (if not already done)
dotnet ef database update

# Start the API
dotnet run
```

**API will be available at:** `http://localhost:5010`

### Frontend Setup

```bash
# Navigate to frontend
cd c:\Repos\Booking\booksy-frontend

# Install dependencies (if needed)
npm install

# Start dev server
npm run dev
```

**Frontend will be available at:** `http://localhost:3000` (or configured port)

### Authentication

You'll need:
- Valid JWT token
- Test user accounts:
  - Organization owner account
  - Individual provider account

---

## Test Scenarios

### Scenario 1: Get Sent Join Requests

**Objective:** Individual provider views their sent join requests.

**Prerequisites:**
- Individual provider account with at least one join request sent

**Steps:**

1. **Authenticate as Individual Provider**
   ```http
   POST /api/v1/auth/login
   Content-Type: application/json

   {
     "email": "individual@test.com",
     "password": "Password123!"
   }
   ```

   Save the `token` from response.

2. **Get Sent Join Requests**
   ```http
   GET /api/v1/providers/{individualProviderId}/hierarchy/join-requests/sent
   Authorization: Bearer {token}
   ```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "individualProviderId": "guid",
    "joinRequests": [
      {
        "requestId": "guid",
        "organizationId": "guid",
        "organizationName": "Acme Salon",
        "organizationLogoUrl": "https://...",
        "message": "I'd like to join your team",
        "status": "Pending",
        "createdAt": "2025-11-25T10:00:00Z",
        "respondedAt": null,
        "rejectionReason": null
      }
    ]
  }
}
```

**Validation:**
- ✅ Status code: 200 OK
- ✅ Response includes all sent join requests (all statuses)
- ✅ Organization details populated (name, logo)
- ✅ Status values: "Pending", "Approved", "Rejected", or "Withdrawn"
- ✅ `respondedAt` populated for approved/rejected requests
- ✅ `rejectionReason` populated for rejected requests

**Error Cases:**

```http
# Provider not found
GET /api/v1/providers/{nonExistentId}/hierarchy/join-requests/sent
Expected: 404 Not Found

# Not an individual provider (organization ID used)
GET /api/v1/providers/{organizationId}/hierarchy/join-requests/sent
Expected: 400 Bad Request - "Provider is not an individual"

# No authentication
GET /api/v1/providers/{id}/hierarchy/join-requests/sent
Expected: 401 Unauthorized
```

---

### Scenario 2: Cancel Join Request

**Objective:** Individual provider cancels their pending join request.

**Prerequisites:**
- Individual provider with at least one Pending join request

**Steps:**

1. **Authenticate as Individual Provider**
   ```http
   POST /api/v1/auth/login
   ```

2. **Get Sent Join Requests** (to find requestId)
   ```http
   GET /api/v1/providers/{individualProviderId}/hierarchy/join-requests/sent
   ```

   Note a `requestId` with `status: "Pending"`.

3. **Cancel the Join Request**
   ```http
   DELETE /api/v1/providers/{individualProviderId}/hierarchy/join-requests/{requestId}
   Authorization: Bearer {token}
   ```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "requestId": "guid",
    "status": "Withdrawn"
  }
}
```

**Validation:**
- ✅ Status code: 200 OK
- ✅ Status changed to "Withdrawn"
- ✅ Request no longer appears as "Pending" in organization's view
- ✅ Idempotent: Calling again returns same result

**Error Cases:**

```http
# Request not found
DELETE /api/v1/providers/{id}/hierarchy/join-requests/{invalidId}
Expected: 404 Not Found

# Not the requester (different provider tries to cancel)
DELETE /api/v1/providers/{differentProviderId}/hierarchy/join-requests/{requestId}
Expected: 400 Bad Request - "Only the requester can cancel their own join request"

# Already processed (Approved/Rejected)
DELETE /api/v1/providers/{id}/hierarchy/join-requests/{approvedRequestId}
Expected: 400 Bad Request - "Cannot withdraw request with status Approved"

# No authentication
DELETE /api/v1/providers/{id}/hierarchy/join-requests/{requestId}
Expected: 401 Unauthorized
```

---

### Scenario 3: End-to-End Join Request Workflow

**Objective:** Test complete workflow from creation to cancellation.

**Participants:**
- Individual Provider (requester)
- Organization (target)

**Steps:**

1. **Individual Creates Join Request**
   ```http
   POST /api/v1/providers/{organizationId}/hierarchy/join-requests
   Authorization: Bearer {individualToken}
   Content-Type: application/json

   {
     "requesterId": "{individualProviderId}",
     "message": "I have 5 years of experience and would love to join your team."
   }
   ```

   **Expected:** 201 Created with requestId

2. **Individual Views Their Sent Requests**
   ```http
   GET /api/v1/providers/{individualProviderId}/hierarchy/join-requests/sent
   Authorization: Bearer {individualToken}
   ```

   **Expected:** Request appears with status "Pending"

3. **Organization Views Pending Requests**
   ```http
   GET /api/v1/providers/{organizationId}/hierarchy/join-requests
   Authorization: Bearer {organizationToken}
   ```

   **Expected:** Request appears in organization's pending list

4. **Individual Cancels Request**
   ```http
   DELETE /api/v1/providers/{individualProviderId}/hierarchy/join-requests/{requestId}
   Authorization: Bearer {individualToken}
   ```

   **Expected:** Status changes to "Withdrawn"

5. **Verify Withdrawal**

   a. Individual checks sent requests:
   ```http
   GET /api/v1/providers/{individualProviderId}/hierarchy/join-requests/sent
   ```
   **Expected:** Request shows status "Withdrawn"

   b. Organization checks pending requests:
   ```http
   GET /api/v1/providers/{organizationId}/hierarchy/join-requests
   ```
   **Expected:** Withdrawn request NOT in pending list

---

### Scenario 4: Approval Workflow (After Join Request)

**Objective:** Organization approves join request before individual can cancel.

**Steps:**

1. Individual creates join request
2. Organization approves before individual cancels
3. Individual attempts to cancel
4. **Expected:** Error - "Cannot withdraw request with status Approved"

---

### Scenario 5: Multiple Join Requests

**Objective:** Individual sends multiple join requests to different organizations.

**Steps:**

1. **Create Requests to 3 Different Organizations**
   - Send to Organization A
   - Send to Organization B
   - Send to Organization C

2. **View All Sent Requests**
   ```http
   GET /api/v1/providers/{individualProviderId}/hierarchy/join-requests/sent
   ```

   **Expected:** All 3 requests appear in response

3. **Organization A Approves**
4. **Individual Withdraws Request to Organization B**
5. **Organization C Rejects**

6. **View Final Status**
   ```http
   GET /api/v1/providers/{individualProviderId}/hierarchy/join-requests/sent
   ```

   **Expected:**
   - Request A: status "Approved", has `respondedAt`
   - Request B: status "Withdrawn"
   - Request C: status "Rejected", has `rejectionReason`

---

## Frontend Testing

### Testing via Browser

1. **Login as Individual Provider**
   - Navigate to `/login`
   - Use individual provider credentials

2. **Navigate to "My Join Requests"**
   - Should be accessible from provider dashboard
   - Route: `/provider/join-requests` or similar

3. **Verify Display**
   - ✅ All sent join requests display
   - ✅ Organization names and logos show
   - ✅ Status badges display correctly (Pending, Approved, Rejected, Withdrawn)
   - ✅ Created date formatted properly
   - ✅ Cancellation button only shows for "Pending" requests

4. **Test Cancel Functionality**
   - Click "Cancel Request" on a pending request
   - ✅ Confirmation dialog appears
   - ✅ After confirmation, status changes to "Withdrawn"
   - ✅ Cancel button disappears
   - ✅ Success notification displays

5. **Test Edge Cases**
   - ✅ Empty state displays when no join requests
   - ✅ Loading state shows while fetching
   - ✅ Error handling when API fails

### Browser Console Testing

```javascript
// Open browser console after login

// Get hierarchy store
const hierarchyStore = useHierarchyStore()

// Test 1: Load sent join requests
await hierarchyStore.loadSentJoinRequests()
console.log('Sent Requests:', hierarchyStore.sentJoinRequests)

// Test 2: Cancel a join request
const requestId = hierarchyStore.sentJoinRequests[0].requestId
await hierarchyStore.cancelJoinRequest(requestId)
console.log('After Cancel:', hierarchyStore.sentJoinRequests)

// Test 3: Verify status changed
const updated = hierarchyStore.sentJoinRequests.find(r => r.requestId === requestId)
console.assert(updated.status === 'Withdrawn', 'Status should be Withdrawn')
```

---

## Performance Testing

### Load Testing

Test with multiple concurrent users:

```bash
# Using Apache Bench (ab)
ab -n 100 -c 10 \
   -H "Authorization: Bearer {token}" \
   http://localhost:5010/api/v1/providers/{id}/hierarchy/join-requests/sent

# Expected: < 200ms average response time
```

### Database Query Performance

Monitor query execution time:

```sql
-- Check query plan for GetSentJoinRequests
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

SELECT * FROM ProviderJoinRequests
WHERE RequesterId = @RequesterId;

-- Expected: Index seek, < 50ms execution time
```

---

## Security Testing

### Authorization Tests

1. **Test Unauthorized Access**
   ```http
   GET /api/v1/providers/{id}/hierarchy/join-requests/sent
   # (no Authorization header)

   Expected: 401 Unauthorized
   ```

2. **Test Cross-Provider Access**
   - Provider A tries to view Provider B's sent requests
   - **Expected:** Should only see their own requests (filtered by token)

3. **Test CSRF Protection**
   - Attempt requests from different origin without proper headers
   - **Expected:** CORS policy enforced

### Input Validation

```http
# Test invalid GUIDs
GET /api/v1/providers/invalid-guid/hierarchy/join-requests/sent
Expected: 400 Bad Request

# Test SQL injection attempt
GET /api/v1/providers/'; DROP TABLE ProviderJoinRequests; --/hierarchy/join-requests/sent
Expected: 400 Bad Request (rejected by input validation)
```

---

## Error Handling Testing

### Network Failures

1. **Simulate Timeout**
   - Delay API response
   - **Expected:** Frontend shows loading state, then timeout error after 30s

2. **Simulate 500 Error**
   - Trigger server error
   - **Expected:** User-friendly error message, logged for debugging

3. **Simulate Database Connection Loss**
   - Stop database
   - **Expected:** 503 Service Unavailable

---

## Monitoring & Logging

### Check Logs

```bash
# Backend logs
tail -f c:\Repos\Booking\logs\application.log

# Look for:
- "Getting sent join requests for individual provider {ProviderId}"
- "Canceling join request {RequestId} by requester {RequesterId}"
- "Join request {RequestId} has been withdrawn"
```

### Monitor Metrics

- API response times
- Error rates
- Database query performance
- Memory usage

---

## Checklist: Ready for Production

### Backend
- [x] All endpoints compile without errors
- [ ] Unit tests pass (run `dotnet test`)
- [ ] Integration tests pass
- [ ] Database migrations applied
- [ ] Logging configured
- [ ] Error handling tested
- [ ] Authorization verified
- [ ] Performance acceptable (< 200ms avg)

### Frontend
- [x] Service layer fixed (using serviceCategoryClient)
- [ ] UI components tested
- [ ] Store actions tested
- [ ] Error states handled
- [ ] Loading states implemented
- [ ] Empty states implemented
- [ ] Responsive design verified
- [ ] Browser compatibility tested

### Integration
- [ ] End-to-end workflow tested
- [ ] Cross-browser testing completed
- [ ] Mobile testing completed
- [ ] Accessibility testing completed
- [ ] Security testing completed

### Documentation
- [x] API endpoints documented
- [x] Integration guide created
- [ ] User guide written
- [ ] Deployment guide written

---

## Known Issues & Limitations

### None Currently Identified

All MVP endpoints are implemented and buildable. Any issues discovered during testing should be documented here.

---

## Next Steps After Testing

1. **If Tests Pass:**
   - Deploy to staging environment
   - Conduct UAT with test users
   - Deploy to production

2. **If Issues Found:**
   - Document issues with severity
   - Create fix plan
   - Retest after fixes

3. **Phase 2 Enhancements:**
   - Implement 6 remaining convenience endpoints
   - Add notification system
   - Enhance search functionality

---

## Support

**Issues During Testing:**
- Check logs first
- Verify database migrations applied
- Ensure authentication tokens are valid
- Check API base URL configuration

**Questions:**
- Refer to [PROVIDER_HIERARCHY_MVP_COMPLETE.md](PROVIDER_HIERARCHY_MVP_COMPLETE.md)
- Check [HIERARCHY_API_STATUS.md](booksy-frontend/HIERARCHY_API_STATUS.md)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-25
**Status:** ✅ Ready for Integration Testing
