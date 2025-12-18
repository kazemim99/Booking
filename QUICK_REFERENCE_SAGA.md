# SAGA Pattern Quick Reference

## 🚀 Quick Start

### What is SAGA Pattern?
A pattern for managing distributed transactions across multiple services by breaking them into a sequence of local transactions, each with a compensating transaction.

### When to Use?
✅ Operations span multiple databases/services
✅ Need consistency without distributed locks
✅ External API calls that can't be rolled back

## 📊 Flow Diagram

```
┌──────────────────────────────────────┐
│  1. Create User (External API)      │ ← Can't rollback via DB transaction
│     Compensation: DeleteUserAsync    │
└──────────────────────────────────────┘
                ↓
┌──────────────────────────────────────┐
│  2-5. Database Operations            │ ← Auto-rollback on failure
│  • Create Provider                   │
│  • Clone Data                        │
│  • Accept Invitation                 │
└──────────────────────────────────────┘
                ↓
┌──────────────────────────────────────┐
│  6. Generate Tokens                  │
└──────────────────────────────────────┘
```

## 🔧 Implementation Pattern

### Template for New SAGA Handlers

```csharp
public async Task<TResult> Handle(TCommand request, CancellationToken cancellationToken)
{
    // Step 1: Validation (outside transaction)
    ValidateRequest(request);

    // Step 2: Track state for compensation
    TExternalResource? externalResource = null;
    bool externalCreated = false;

    try
    {
        // Step 3: External operation (can't rollback via DB)
        externalResource = await _externalService.CreateAsync(...);
        externalCreated = true;

        // Step 4: Database operations (auto-rollback)
        var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // All database operations here
            var entity = await CreateEntity(...);
            await ProcessData(...);
            return entity;
        }, cancellationToken);

        return BuildSuccessResult(result);
    }
    catch (Exception ex)
    {
        // Step 5: Compensation
        if (externalCreated && externalResource != null)
        {
            await _externalService.DeleteAsync(externalResource, ex.Message);
        }
        throw;
    }
}
```

## 📝 Code Examples

### Compensation Method

```csharp
public async Task<bool> DeleteUserAsync(UserId userId, string reason, CancellationToken ct)
{
    try
    {
        _logger.LogWarning("COMPENSATION: Deleting {Resource} {Id}. Reason: {Reason}",
            nameof(User), userId, reason);

        var client = _httpClientFactory.CreateClient("ExternalAPI");
        var response = await client.DeleteAsync($"/api/resource/{userId}?reason={Uri.EscapeDataString(reason)}", ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("COMPENSATION SUCCESS: {Resource} {Id} deleted", nameof(User), userId);
            return true;
        }

        _logger.LogWarning("COMPENSATION PARTIAL: Failed to delete {Resource} {Id}. Manual cleanup required.",
            nameof(User), userId);
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "COMPENSATION ERROR: Exception deleting {Resource} {Id}",
            nameof(User), userId);
        return false; // Never throw
    }
}
```

### Transaction Wrapper

```csharp
var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
{
    // Create entities
    var entity = await _repository.CreateAsync(...);

    // Process related data
    await ProcessRelatedData(entity);

    // Update state
    entity.MarkAsProcessed();
    await _repository.UpdateAsync(entity);

    // Return results
    return (entity, metadata);
}, cancellationToken);
```

## 🎯 Logging Guidelines

### Success Logs
```csharp
_logger.LogInformation("✓ SAGA STEP {Step}: {Description}",
    1, "User account created with ID {UserId}", userId);
```

### Failure Logs
```csharp
_logger.LogError(ex, "✗ SAGA FAILED: {Description}. {Resource}Created: {Created}",
    "Error during processing", nameof(User), userCreated);
```

### Compensation Logs
```csharp
_logger.LogWarning("⚠ COMPENSATION: Attempting to delete {Resource} {Id}",
    nameof(User), userId);

_logger.LogInformation("✓ COMPENSATION SUCCESS: {Resource} {Id} deleted",
    nameof(User), userId);

_logger.LogWarning("⚠ COMPENSATION PARTIAL: Manual cleanup required for {Resource} {Id}",
    nameof(User), userId);
```

## 🔍 Monitoring Queries

### Find Failed Compensations
```sql
SELECT *
FROM Logs
WHERE Message LIKE '%COMPENSATION PARTIAL%'
   OR Message LIKE '%COMPENSATION FAILED%'
ORDER BY Timestamp DESC
LIMIT 100;
```

### SAGA Success Rate (Last 24h)
```sql
SELECT
    COUNT(CASE WHEN Message LIKE '%SAGA COMPLETED%' THEN 1 END) as Successes,
    COUNT(CASE WHEN Message LIKE '%SAGA FAILED%' THEN 1 END) as Failures,
    COUNT(*) as Total
FROM Logs
WHERE Message LIKE '%SAGA%'
  AND Timestamp > NOW() - INTERVAL '24 hours';
```

### Compensation Trigger Rate
```sql
SELECT
    DATE_TRUNC('hour', Timestamp) as Hour,
    COUNT(*) as CompensationAttempts
FROM Logs
WHERE Message LIKE '%COMPENSATION: Attempting%'
  AND Timestamp > NOW() - INTERVAL '24 hours'
GROUP BY Hour
ORDER BY Hour DESC;
```

## 🛠️ Troubleshooting

### Problem: Compensation Not Triggered
**Symptoms**: User created but not deleted after failure
**Check**:
```csharp
// Ensure flag is set
externalCreated = true; // ← Must be AFTER successful creation

// Ensure compensation check
if (externalCreated && externalResource != null)
{
    await CompensateAsync(...);
}
```

### Problem: Transaction Not Rolling Back
**Symptoms**: Partial data in database
**Check**:
```csharp
// ✅ Correct: Wrapped in transaction
await _unitOfWork.ExecuteInTransactionAsync(async () => {
    // DB operations here
}, cancellationToken);

// ❌ Wrong: Not wrapped
await CreateEntity(...);
await _unitOfWork.SaveChangesAsync(); // Manual save
```

### Problem: Compensation Fails Silently
**Symptoms**: No logs about compensation
**Check**:
```csharp
// ✅ Correct: Try-catch with logging
catch (Exception ex)
{
    if (externalCreated)
    {
        _logger.LogWarning("⚠ COMPENSATION: Attempting...");
        await CompensateAsync(...);
    }
    throw; // Re-throw original exception
}

// ❌ Wrong: No compensation logic
catch (Exception ex)
{
    throw; // Missing compensation
}
```

## ✅ Checklist

### Before Implementing SAGA

- [ ] Identified external operations (can't rollback via DB)
- [ ] Identified database operations (can rollback)
- [ ] Designed compensation logic for external operations
- [ ] Planned transaction boundaries
- [ ] Defined success/failure logging

### During Implementation

- [ ] State tracking variables declared
- [ ] External operations have compensation methods
- [ ] Database operations wrapped in transaction
- [ ] Comprehensive logging added
- [ ] Error handling with compensation trigger
- [ ] Original exception re-thrown

### After Implementation

- [ ] Unit tests for happy path
- [ ] Unit tests for each failure scenario
- [ ] Unit tests for compensation logic
- [ ] Integration tests for full flow
- [ ] Monitoring queries created
- [ ] Alerts configured
- [ ] Documentation updated

## 📚 Further Reading

- [SAGA_PATTERN_IMPLEMENTATION.md](docs/SAGA_PATTERN_IMPLEMENTATION.md) - Complete guide
- [Microsoft SAGA Pattern](https://learn.microsoft.com/en-us/azure/architecture/reference-architectures/saga/saga)
- [Compensating Transaction Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction)

## 🎓 Common Pitfalls

### ❌ Don't
```csharp
// Don't set flag before operation completes
externalCreated = true;
externalResource = await _externalService.CreateAsync(...); // Might fail!

// Don't throw in compensation
catch (Exception ex)
{
    await CompensateAsync(...);
    throw new CompensationException(); // ❌ Hides original error
}

// Don't save manually inside transaction
await _unitOfWork.ExecuteInTransactionAsync(async () => {
    await CreateEntity(...);
    await _unitOfWork.SaveChangesAsync(); // ❌ Unnecessary
    return entity;
});
```

### ✅ Do
```csharp
// Do set flag after successful operation
externalResource = await _externalService.CreateAsync(...);
externalCreated = true; // ✓ Only if creation succeeded

// Do re-throw original exception
catch (Exception ex)
{
    await CompensateAsync(...);
    throw; // ✓ Preserves original error
}

// Do let transaction handle saves
await _unitOfWork.ExecuteInTransactionAsync(async () => {
    await CreateEntity(...);
    return entity; // ✓ Transaction commits automatically
});
```

---

**Quick Access**: Keep this reference handy when implementing new SAGA patterns!
