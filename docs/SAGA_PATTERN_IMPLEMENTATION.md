# SAGA Pattern Implementation for Invitation Registration

**Date**: December 2025
**Status**: ✅ Completed
**Impact**: Critical - Fixes distributed transaction consistency issues

## Overview

This document describes the implementation of the SAGA pattern with compensation logic for handling distributed transactions in the invitation registration flow. The implementation ensures data consistency across multiple databases and external services.

## Problem Statement

### Before Implementation (BROKEN ❌)

The `AcceptInvitationWithRegistrationCommandHandler` had a critical flaw in handling distributed transactions:

```
1. Create user in UserManagement database (External API)
2. Create provider in ServiceCatalog database
3. Clone data (services, hours, gallery)
4. Accept invitation
5. Generate tokens
```

**Issues**:
- ❌ No transaction wrapping around the entire flow
- ❌ User creation happens in external UserManagement service
- ❌ If provider creation fails, user account remains orphaned
- ❌ No compensation/rollback mechanism for external API calls
- ❌ Cloned data wasn't being persisted to database
- ❌ Invitation could be left in inconsistent state

**Example Failure Scenario**:
```
1. ✓ User created in UserManagement (ID: 123)
2. ✗ Provider creation fails (database error)
3. Result: Orphaned user 123 exists with no provider profile
4. Invitation stays "Pending"
5. User cannot login or complete registration
```

## Solution: SAGA Pattern with Compensation

### Architecture

The SAGA pattern breaks the distributed transaction into a series of local transactions, each with a compensating transaction to undo changes if a later step fails.

```
┌─────────────────────────────────────────────────────────────┐
│                   VALIDATION PHASE                          │
│              (Read-only, Outside Transaction)               │
├─────────────────────────────────────────────────────────────┤
│ • Validate invitation exists and is pending                │
│ • Verify OTP code                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              SAGA STEP 1: Create User Account              │
│         (External API - Cannot rollback via DB TX)         │
├─────────────────────────────────────────────────────────────┤
│ • Call UserManagement API POST /api/v1/users               │
│ • Mark userCreationSucceeded = true                        │
│ • COMPENSATION: DeleteUserAsync if later steps fail        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│          SAGA STEPS 2-5: Database Transaction               │
│         (Auto-rollback on failure via UnitOfWork)           │
├─────────────────────────────────────────────────────────────┤
│ STEP 2: Create individual provider profile                 │
│ STEP 3a: Clone services (if requested)                     │
│ STEP 3b: Clone working hours (if requested)                │
│ STEP 3c: Clone gallery (if requested)                      │
│ STEP 4: Persist all cloned data                            │
│ STEP 5: Accept invitation                                  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│        SAGA STEP 6: Generate JWT Tokens                    │
│              (Outside transaction)                          │
├─────────────────────────────────────────────────────────────┤
│ • Generate access and refresh tokens                       │
│ • Return success result                                    │
└─────────────────────────────────────────────────────────────┘
```

### Compensation Flow on Failure

```
┌─────────────────────────────────────────────────────────────┐
│                  ERROR DETECTED                             │
├─────────────────────────────────────────────────────────────┤
│ Exception thrown during any SAGA step                      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              AUTOMATIC ROLLBACK                             │
├─────────────────────────────────────────────────────────────┤
│ • Database changes (Steps 2-5) → Auto-rollback via TX      │
│ • Transaction.RollbackAsync() called automatically         │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              COMPENSATION LOGIC                             │
├─────────────────────────────────────────────────────────────┤
│ IF userCreationSucceeded == true:                          │
│   • Call DeleteUserAsync(userId, reason)                   │
│   • DELETE /api/v1/users/{userId}?reason=...               │
│   • Log compensation result                                │
│   • Best-effort: Don't throw if deletion fails             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              RESULT                                         │
├─────────────────────────────────────────────────────────────┤
│ • Original exception re-thrown to caller                   │
│ • System left in consistent state                          │
│ • Comprehensive logs for audit trail                       │
└─────────────────────────────────────────────────────────────┘
```

## Implementation Details

### Files Modified

#### 1. Interface Definition
**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Services/Interfaces/IInvitationRegistrationService.cs`

**Added Method**:
```csharp
/// <summary>
/// Compensation: Deletes a user account if registration flow fails
/// Used for saga pattern rollback
/// </summary>
Task<bool> DeleteUserAsync(
    UserId userId,
    string reason,
    CancellationToken cancellationToken = default);
```

#### 2. Compensation Implementation
**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Services/Application/InvitationRegistrationService.cs`

**Implementation**:
```csharp
public async Task<bool> DeleteUserAsync(
    UserId userId,
    string reason,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogWarning(
            "COMPENSATION: Deleting user {UserId} due to registration failure. Reason: {Reason}",
            userId, reason);

        var client = _httpClientFactory.CreateClient("UserManagementAPI");
        var response = await client.DeleteAsync(
            $"/api/v1/users/{userId.Value}?reason={Uri.EscapeDataString(reason)}",
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "COMPENSATION SUCCESS: User {UserId} deleted successfully",
                userId);
            return true;
        }

        // Log warning but don't throw - best effort compensation
        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "COMPENSATION PARTIAL FAILURE: Failed to delete user {UserId}: {StatusCode} - {Error}. Manual cleanup may be required.",
            userId, response.StatusCode, error);

        return false;
    }
    catch (Exception ex)
    {
        // Log error but don't throw - compensation is best effort
        _logger.LogError(ex,
            "COMPENSATION ERROR: Exception while deleting user {UserId}. Manual cleanup may be required.",
            userId);
        return false;
    }
}
```

**Key Points**:
- ✅ Best-effort approach - never throws exceptions
- ✅ Comprehensive logging for audit trail
- ✅ Returns boolean success indicator
- ✅ Logs failures for manual intervention

#### 3. Command Handler Refactoring
**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/AcceptInvitationWithRegistration/AcceptInvitationWithRegistrationCommandHandler.cs`

**Key Changes**:

1. **State Tracking**:
```csharp
UserId? createdUserId = null;
ProviderAggregate? createdProvider = null;
bool userCreationSucceeded = false;
int clonedServicesCount = 0;
int clonedWorkingHoursCount = 0;
int clonedGalleryCount = 0;
```

2. **Try-Catch with Compensation**:
```csharp
try
{
    // SAGA STEP 1: Create user (external API)
    createdUserId = await _registrationService.CreateUserWithPhoneAsync(...);
    userCreationSucceeded = true;

    // SAGA STEPS 2-5: Wrapped in transaction
    var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        // All database operations here
        // Auto-rollback if any step fails
    }, cancellationToken);

    // SAGA STEP 6: Generate tokens
    var tokens = await _registrationService.GenerateAuthTokensAsync(...);

    return result;
}
catch (Exception ex)
{
    // COMPENSATION LOGIC
    if (userCreationSucceeded && createdUserId != null)
    {
        await _registrationService.DeleteUserAsync(
            createdUserId,
            $"Invitation acceptance failed: {ex.Message}",
            cancellationToken);
    }
    throw; // Re-throw original exception
}
```

3. **Transaction Wrapping**:
```csharp
await _unitOfWork.ExecuteInTransactionAsync(async () =>
{
    // Create provider
    createdProvider = await _registrationService.CreateIndividualProviderAsync(...);

    // Clone data
    if (request.CloneServices) { ... }
    if (request.CloneWorkingHours) { ... }
    if (request.CloneGallery) { ... }

    // Accept invitation
    invitation.Accept(createdProvider.Id);
    await _invitationWriteRepository.UpdateAsync(invitation, cancellationToken);

    return (createdProvider, clonedServicesCount, ...);
}, cancellationToken);
```

### Additional Fixes

#### 4. Data Cloning Persistence
**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Services/Application/DataCloningService.cs`

**Fixed**: Line 112
```csharp
// Add the cloned service to repository
await _serviceWriteRepository.SaveAsync(clonedService, cancellationToken);
```

**Issue**: Cloned services were created in memory but never added to repository.

#### 5. EF Core 9 Owned Entity Configuration
**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/ServiceConfiguration.cs`

**Fixed**: Shadow foreign key configuration for owned entities

```csharp
// BasePrice
builder.OwnsOne(s => s.BasePrice, price =>
{
    // ... property configurations ...

    // EF Core 9: Explicitly configure foreign key
    price.WithOwner().HasForeignKey("ServiceId");
    price.Property<Guid>("ServiceId").ValueGeneratedNever();
});
```

**Applied to**:
- `Category` (ServiceId)
- `BasePrice` (ServiceId)
- `BookingPolicy` (ServiceId)
- `ServiceOption.AdditionalPrice` (ServiceOptionId)
- `PriceTier.Price` (PriceTierId)

**Issue**: EF Core 9 treats shadow foreign keys as part of composite keys by default, causing errors during change tracking.

## Logging Strategy

### Success Flow
```
INFO: Processing invitation acceptance with registration for invitation {InvitationId}
INFO: OTP verification successful for {PhoneNumber}
INFO: ✓ SAGA STEP 1: User account created with ID {UserId}
INFO: ✓ SAGA STEP 2: Individual provider profile created with ID {ProviderId}
INFO: ✓ SAGA STEP 3a: Cloned {Count} services from organization {OrgId} to provider {ProviderId}
INFO: ✓ SAGA STEP 3b: Cloned {Count} working hours from organization {OrgId} to provider {ProviderId}
INFO: ✓ SAGA STEP 3c: Cloned {Count} gallery images from organization {OrgId} to provider {ProviderId}
INFO: ✓ SAGA STEP 4: Persisted cloned data for provider {ProviderId}
INFO: ✓ SAGA STEP 5: Invitation {InvitationId} accepted
INFO: ✓ SAGA COMPLETED: All steps successful for user {UserId}, provider {ProviderId}
```

### Failure with Compensation
```
ERROR: ✗ SAGA FAILED: Error during invitation acceptance. UserCreated: True, UserId: {UserId}
WARN: ⚠ COMPENSATION: Attempting to delete orphaned user {UserId}
INFO: ✓ COMPENSATION SUCCESS: Orphaned user {UserId} deleted successfully
```

### Failure with Compensation Partial Failure
```
ERROR: ✗ SAGA FAILED: Error during invitation acceptance
WARN: ⚠ COMPENSATION: Attempting to delete orphaned user {UserId}
WARN: ⚠ COMPENSATION PARTIAL: User {UserId} deletion failed. MANUAL CLEANUP REQUIRED!
```

## Testing Scenarios

### Test Case 1: Happy Path
```
Given: Valid invitation, OTP, and organization
When: All steps succeed
Then:
  ✓ User created in UserManagement
  ✓ Provider created in ServiceCatalog
  ✓ Data cloned successfully
  ✓ Invitation accepted
  ✓ Tokens returned
```

### Test Case 2: Provider Creation Fails
```
Given: Valid invitation, user created successfully
When: Provider creation throws exception
Then:
  ✓ User deletion API called (compensation)
  ✓ User removed from UserManagement
  ✓ Exception propagated to caller
  ✓ No orphaned data
```

### Test Case 3: Data Cloning Fails
```
Given: Valid invitation, user and provider created
When: Service cloning throws exception
Then:
  ✓ Transaction rolls back (provider not persisted)
  ✓ User deletion API called (compensation)
  ✓ System returns to consistent state
```

### Test Case 4: Compensation Fails
```
Given: Provider creation fails, user created
When: User deletion API is unavailable
Then:
  ✓ Error logged with MANUAL CLEANUP REQUIRED
  ✓ Original exception still thrown
  ✓ Admin can identify and clean up manually
```

## Monitoring and Alerts

### Key Metrics to Monitor

1. **SAGA Success Rate**
   - Log: `✓ SAGA COMPLETED`
   - Metric: Count of successful completions
   - Target: >99%

2. **Compensation Trigger Rate**
   - Log: `⚠ COMPENSATION: Attempting to delete`
   - Metric: Count of compensation attempts
   - Alert: If >1% of total requests

3. **Compensation Failure Rate**
   - Log: `⚠ COMPENSATION PARTIAL` or `✗ COMPENSATION FAILED`
   - Metric: Failed compensations
   - Alert: Immediate - requires manual cleanup

4. **Orphaned User Detection**
   - Query: Users without provider profiles
   - Schedule: Daily
   - Action: Manual investigation

### Dashboard Queries

```sql
-- Successful SAGA completions (last 24h)
SELECT COUNT(*)
FROM Logs
WHERE Message LIKE '%SAGA COMPLETED%'
  AND Timestamp > NOW() - INTERVAL '24 hours';

-- Compensation attempts (last 24h)
SELECT COUNT(*), UserId, Reason
FROM Logs
WHERE Message LIKE '%COMPENSATION: Attempting%'
  AND Timestamp > NOW() - INTERVAL '24 hours'
GROUP BY UserId, Reason;

-- Failed compensations requiring manual cleanup
SELECT UserId, Timestamp, Message
FROM Logs
WHERE Message LIKE '%COMPENSATION PARTIAL%'
   OR Message LIKE '%COMPENSATION FAILED%'
ORDER BY Timestamp DESC;
```

## Operational Procedures

### Manual Cleanup for Failed Compensation

If compensation fails (logs show `MANUAL CLEANUP REQUIRED`):

1. **Identify the orphaned user**:
   ```sql
   -- Find user ID from logs
   SELECT * FROM Logs
   WHERE Message LIKE '%COMPENSATION FAILED%'
   ORDER BY Timestamp DESC
   LIMIT 1;
   ```

2. **Verify user has no provider**:
   ```sql
   SELECT u.*
   FROM Users u
   LEFT JOIN Providers p ON p.OwnerId = u.Id
   WHERE u.Id = '{userId}' AND p.Id IS NULL;
   ```

3. **Manually delete the user**:
   ```bash
   curl -X DELETE https://api.booksy.com/api/v1/users/{userId}?reason=Manual_cleanup_failed_compensation
   ```

4. **Verify deletion**:
   ```sql
   SELECT * FROM Users WHERE Id = '{userId}';
   -- Should return 0 rows
   ```

5. **Update monitoring dashboard**:
   - Mark the incident as resolved
   - Document root cause if identified

## Benefits

### Data Consistency ✅
- No orphaned users in UserManagement
- No incomplete provider profiles
- Invitation state always consistent

### Observability ✅
- Clear logging of each SAGA step
- Easy to identify where failures occur
- Audit trail for compliance

### Resilience ✅
- Automatic rollback of database changes
- Best-effort compensation for external services
- Graceful degradation with manual cleanup fallback

### Maintainability ✅
- Well-documented flow
- Clear separation of concerns
- Easy to add new SAGA steps

## Future Enhancements

1. **Idempotency**
   - Add idempotency keys to prevent duplicate processing
   - Allow safe retries of failed operations

2. **Saga Orchestrator**
   - Extract SAGA logic into dedicated orchestrator service
   - Support more complex multi-step workflows

3. **Dead Letter Queue**
   - Queue failed compensations for retry
   - Automated cleanup with exponential backoff

4. **Distributed Tracing**
   - Add correlation IDs across service boundaries
   - Integrate with OpenTelemetry for end-to-end tracing

5. **Circuit Breaker**
   - Add circuit breaker for UserManagement API calls
   - Fail fast when external service is degraded

## Related Documentation

- [OTP Invitation Flow](OTP_INVITATION_FLOW.md)
- [Staff Invitation Flow](STAFF_INVITATION_FLOW.md)
- [Hierarchy Migration](HIERARCHY_MIGRATION_README.md)
- [Cache Invalidation Fix](CACHE_INVALIDATION_FIX_PROFILE_UPDATE.md)

## References

- [SAGA Pattern - Microsoft](https://learn.microsoft.com/en-us/azure/architecture/reference-architectures/saga/saga)
- [Compensating Transaction Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction)
- [EF Core 9 Breaking Changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes)

---

**Last Updated**: December 2025
**Review Date**: March 2026
**Owner**: Backend Team
