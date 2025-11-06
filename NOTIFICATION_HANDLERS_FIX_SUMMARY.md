# Notification Command Handlers - Exception Handling Fix

## Problem

All notification command handlers are using an anti-pattern:
1. Wrapping return type in `Result<T>` (should be just `T`)
2. Using try-catch to return `Result.Failure()` (should throw exceptions)
3. Not letting ExceptionHandlingMiddleware handle exceptions

## Correct Pattern (from CreateBookingCommandHandler)

```csharp
public async Task<CreateBookingResult> Handle(...)  // ✅ Direct return type
{
    // Validation
    if (provider == null)
        throw new NotFoundException(...);  // ✅ Throw exceptions directly

    if (service == null)
        throw new NotFoundException(...);

    // Business logic
    var booking = Booking.Create(...);

    // Save
    await _repository.SaveAsync(booking, ct);

    return new CreateBookingResult(...);  // ✅ Direct return
}
```

## What Needs to Change in All 6 Handlers

### 1. **SendNotificationCommandHandler** ✅ FIXED
- Change: `Task<Result<SendNotificationResult>>` → `Task<SendNotificationResult>`
- Remove: Outer try-catch block
- Remove: `using Booksy.Core.Application.Results;`
- Return: Direct `new SendNotificationResult(...)` instead of `Result.Success(...)`

### 2. **ScheduleNotificationCommandHandler** ⚠️ NEEDS FIX
**Current**:
```csharp
public async Task<Result<ScheduleNotificationResult>> Handle(...)
{
    try
    {
        // Validation
        if (command.ScheduledFor <= DateTime.UtcNow)
            return Result<ScheduleNotificationResult>.Failure("Scheduled time must be in the future");

        // Logic...
        return Result<ScheduleNotificationResult>.Success(new ScheduleNotificationResult(...));
    }
    catch (Exception ex)
    {
        return Result<ScheduleNotificationResult>.Failure($"Failed...");
    }
}
```

**Should Be**:
```csharp
public async Task<ScheduleNotificationResult> Handle(...)
{
    // Validation with exceptions
    if (command.ScheduledFor <= DateTime.UtcNow)
        throw new DomainValidationException("ScheduledFor", "Scheduled time must be in the future");

    if (command.ScheduledFor > DateTime.UtcNow.AddDays(90))
        throw new DomainValidationException("ScheduledFor", "Scheduled time cannot be more than 90 days in the future");

    // Logic...
    return new ScheduleNotificationResult(...);
}
```

### 3. **CancelNotificationCommandHandler** ⚠️ NEEDS FIX
**Changes**:
- `Task<Result<CancelNotificationResult>>` → `Task<CancelNotificationResult>`
- `Result.Failure("Notification not found")` → `throw new NotFoundException(...)`
- `Result.Failure("Cannot cancel...")` → `throw new DomainValidationException(...)`

### 4. **ResendNotificationCommandHandler** ⚠️ NEEDS FIX
**Changes**:
- `Task<Result<ResendNotificationResult>>` → `Task<ResendNotificationResult>`
- `Result.Failure("Notification not found")` → `throw new NotFoundException(...)`
- `Result.Failure("Cannot resend...")` → `throw new DomainValidationException(...)`

### 5. **SendBulkNotificationCommandHandler** ⚠️ NEEDS FIX
**Changes**:
- `Task<Result<SendBulkNotificationResult>>` → `Task<SendBulkNotificationResult>`
- `Result.Failure("Maximum 1000 recipients...")` → `throw new DomainValidationException("RecipientIds", "Maximum 1000 recipients...")`

### 6. **UpdatePreferencesCommandHandler** ⚠️ NEEDS FIX
**Changes**:
- `Task<Result<UpdatePreferencesResult>>` → `Task<UpdatePreferencesResult>`
- `Result.Failure("User preferences not found")` → Create new or return default
- Remove try-catch wrapping

## Exception Types to Use

From `/home/user/Booking/src/Core/Booksy.Core.Domain/Exceptions/`:

1. **DomainValidationException** - For validation errors
   ```csharp
   throw new DomainValidationException("PropertyName", "Error message");
   throw new DomainValidationException("PropertyName", new[] { "Error 1", "Error 2" });
   ```

2. **NotFoundException** (from `Booksy.Core.Application.Exceptions`)
   ```csharp
   throw new NotFoundException("Entity with ID {id} not found");
   ```

3. **ConflictException** (from `Booksy.Core.Application.Exceptions`)
   ```csharp
   throw new ConflictException("Operation conflicts with current state");
   ```

4. **OperationNotAllowedException** (from `Booksy.Core.Domain.Domain.Exceptions`)
   ```csharp
   throw new OperationNotAllowedException("Cannot perform this operation");
   ```

## Benefits of This Approach

1. ✅ **Consistent** - Matches existing handlers (CreateBooking, ProcessPayment, etc.)
2. ✅ **Clean** - Exception middleware handles all exceptions globally
3. ✅ **Proper HTTP Status Codes** - DomainException.HttpStatusCode returns correct codes
4. ✅ **Structured Error Response** - RFC 7807 Problem Details format
5. ✅ **Less Boilerplate** - No try-catch in every handler

## Controllers Impact

Controllers using `.Match<IActionResult>()` need to be updated:

**Current (WRONG)**:
```csharp
var result = await _mediator.Send(command, ct);
return result.Match<IActionResult>(
    success => Ok(success),
    failure => BadRequest(failure));
```

**Should Be**:
```csharp
var result = await _mediator.Send(command, ct);
return Ok(result);  // Exceptions handled by middleware
```

## Next Steps

1. Fix remaining 5 command handlers
2. Update controllers to remove `.Match()` calls
3. Remove `using Booksy.Core.Application.Results;` from all handlers
4. Test that ExceptionHandlingMiddleware properly handles the exceptions
