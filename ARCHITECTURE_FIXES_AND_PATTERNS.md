# Architecture Fixes and Patterns - Reference Guide

**Last Updated:** 2025-11-06
**Session:** Notification & Communication System Implementation

## Table of Contents
1. [Exception Handling Pattern](#exception-handling-pattern)
2. [Architectural Boundaries](#architectural-boundaries)
3. [Command Handler Patterns](#command-handler-patterns)
4. [Controller Patterns](#controller-patterns)
5. [Domain Model Design](#domain-model-design)
6. [Interface Contracts](#interface-contracts)
7. [Dependency Injection](#dependency-injection)
8. [Common Mistakes to Avoid](#common-mistakes-to-avoid)

---

## Exception Handling Pattern

### ✅ CORRECT Pattern

**Handlers throw exceptions, middleware catches them globally.**

```csharp
// ❌ WRONG - Using Result<T> wrapper
public async Task<Result<VerifyPhoneResult>> Handle(VerifyPhoneCommand command, CancellationToken ct)
{
    try
    {
        var verification = await _repository.GetByIdAsync(command.VerificationId, ct);
        if (verification == null)
            return Result<VerifyPhoneResult>.Failure("Not found");

        return Result<VerifyPhoneResult>.Success(new VerifyPhoneResult(...));
    }
    catch (Exception ex)
    {
        return Result<VerifyPhoneResult>.Failure(ex.Message);
    }
}

// ✅ CORRECT - Throw exceptions
public async Task<VerifyPhoneResult> Handle(VerifyPhoneCommand command, CancellationToken ct)
{
    var verification = await _repository.GetByIdAsync(command.VerificationId, ct);

    if (verification == null)
        throw new NotFoundException($"Verification with ID {command.VerificationId} not found");

    if (verification.IsBlocked())
        throw new DomainValidationException(
            nameof(command.VerificationId),
            "Too many attempts. Blocked for 1 hour.");

    // Business logic - let any exceptions propagate
    var result = verification.Verify(command.Code);

    return new VerifyPhoneResult(result, verification.Status, ...);
}
```

**Controller Pattern:**

```csharp
// ❌ WRONG - Manual error handling with ApiErrorResult
[HttpPost("verify")]
public async Task<IActionResult> VerifyPhone([FromBody] VerifyRequest request)
{
    var result = await _mediator.Send(command);

    if (!result.IsSuccess)
        return BadRequest(new ApiErrorResult { Message = result.Error, ... });

    return Ok(result.Value);
}

// ✅ CORRECT - Let middleware handle errors
[HttpPost("verify")]
public async Task<IActionResult> VerifyPhone([FromBody] VerifyRequest request)
{
    var result = await _mediator.Send(command);
    return Ok(result);  // Exceptions caught by ExceptionHandlingMiddleware
}
```

### Exception Types to Use

| Scenario | Exception Type | Example |
|----------|---------------|---------|
| Entity not found | `NotFoundException` | `throw new NotFoundException($"User {id} not found")` |
| Validation failure | `DomainValidationException` | `throw new DomainValidationException("Email", "Invalid format")` |
| Business rule violation | `DomainValidationException` | `throw new DomainValidationException("Status", "Cannot cancel completed booking")` |
| External service failure | `ExternalServiceException` | `throw new ExternalServiceException("SMS service unavailable", "SMS_ERROR")` |
| Unauthorized access | `UnauthorizedException` | `throw new UnauthorizedException("Access denied")` |

### Files Fixed
- ✅ `ScheduleNotificationCommandHandler.cs`
- ✅ `CancelNotificationCommandHandler.cs`
- ✅ `ResendNotificationCommandHandler.cs`
- ✅ `SendBulkNotificationCommandHandler.cs`
- ✅ `UpdatePreferencesCommandHandler.cs`
- ✅ `VerifyPhoneCommandHandler.cs`
- ✅ `RequestPhoneVerificationCommandHandler.cs`
- ✅ `ResendOtpCommandHandler.cs`
- ✅ `SendNotificationCommandHandler.cs`

---

## Architectural Boundaries

### ✅ CORRECT Dependency Flow

**Clean Architecture Rule: Dependencies point inward, never outward.**

```
Infrastructure.External (Shared)
    ↓ ✅ Can reference
Core.* (Shared primitives)

❌ CANNOT reference
BoundedContext.Application (Context-specific)
```

### Issue Fixed: Notification Services Location

**Problem:**
```
Booksy.Infrastructure.External/Notifications/
├── SendGridEmailNotificationService.cs  // ❌ WRONG LOCATION
├── RahyabSmsNotificationService.cs      // ❌ WRONG LOCATION
└── InAppNotificationService.cs          // ❌ WRONG LOCATION
```

These services were referencing `Booksy.ServiceCatalog.Application` interfaces, violating architectural boundaries.

**Solution:**
```
Booksy.ServiceCatalog.Infrastructure/Notifications/
├── Email/SendGridEmailNotificationService.cs     // ✅ CORRECT
├── Sms/RahyabSmsNotificationService.cs          // ✅ CORRECT
├── Push/FirebasePushNotificationService.cs      // ✅ CORRECT
├── InAppNotificationService.cs                   // ✅ CORRECT
├── NotificationTemplateService.cs                // ✅ CORRECT
└── TemplateEngine.cs                             // ✅ CORRECT
```

### Rule Summary

| Layer | Can Reference | Cannot Reference |
|-------|---------------|------------------|
| Infrastructure.External | Core.* only | Any BoundedContext.* |
| BoundedContext.Infrastructure | Core.*, BoundedContext.Application, BoundedContext.Domain | Other BoundedContext.* |
| BoundedContext.Application | Core.*, BoundedContext.Domain | BoundedContext.Infrastructure, Other BoundedContext.* |
| BoundedContext.Domain | Core.* only | Everything else |

**Verification Command:**
```bash
# Should return ZERO results
grep -r "Booksy.ServiceCatalog.Application" src/Infrastructure/Booksy.Infrastructure.External --include="*.cs"
```

---

## Command Handler Patterns

### ✅ Interface Implementation

**All commands must have `IdempotencyKey` property:**

```csharp
// ❌ WRONG - Missing IdempotencyKey
public sealed record SendNotificationCommand(
    Guid RecipientId,
    NotificationType Type,
    string Subject,
    string Body) : ICommand<SendNotificationResult>;

// ✅ CORRECT - Has IdempotencyKey
public sealed record SendNotificationCommand(
    Guid RecipientId,
    NotificationType Type,
    string Subject,
    string Body,
    Guid? IdempotencyKey = null) : ICommand<SendNotificationResult>;
```

**Files Fixed:**
- ✅ `SendNotificationCommand.cs`
- ✅ `ScheduleNotificationCommand.cs`
- ✅ `CancelNotificationCommand.cs`
- ✅ `ResendNotificationCommand.cs`
- ✅ `SendBulkNotificationCommand.cs`
- ✅ `UpdatePreferencesCommand.cs`

### ✅ Event Handler Method Names

**IDomainEventHandler uses `HandleAsync`, not `Handle`:**

```csharp
// ❌ WRONG - Using Handle
public class BookingConfirmedNotificationHandler
    : IDomainEventHandler<BookingConfirmedEvent>
{
    public async Task Handle(BookingConfirmedEvent @event, CancellationToken ct)
    {
        // ...
    }
}

// ✅ CORRECT - Using HandleAsync
public class BookingConfirmedNotificationHandler
    : IDomainEventHandler<BookingConfirmedEvent>
{
    public async Task HandleAsync(BookingConfirmedEvent @event, CancellationToken ct)
    {
        // ...
    }
}
```

**Files Fixed (9 handlers):**
- ✅ `BookingCancelledNotificationHandler.cs`
- ✅ `BookingConfirmedNotificationHandler.cs`
- ✅ `BookingRescheduledNotificationHandler.cs`
- ✅ `BookingNoShowNotificationHandler.cs`
- ✅ `BookingCompletedNotificationHandler.cs`
- ✅ `PaymentProcessedNotificationHandler.cs`
- ✅ `PaymentFailedNotificationHandler.cs`
- ✅ `PaymentRefundedNotificationHandler.cs`
- ✅ `PayoutCompletedNotificationHandler.cs`

---

## Controller Patterns

### ✅ CORRECT Controller Pattern

**Remove `ApiErrorResult` - it's internal to middleware:**

```csharp
// ❌ WRONG - Using ApiErrorResult
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;

[ApiController]
[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status500InternalServerError)]
public class NotificationsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SendNotification([FromBody] SendRequest request)
    {
        if (!Enum.TryParse<NotificationType>(request.Type, out var type))
        {
            return BadRequest(new ApiErrorResult
            {
                Message = "Invalid type",
                StatusCode = 400
            });
        }

        var result = await _mediator.Send(command);
        return result.Match<IActionResult>(
            success => Ok(success),
            failure => BadRequest(failure));
    }
}

// ✅ CORRECT - Clean controller
[ApiController]
public class NotificationsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SendNotificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendNotification([FromBody] SendRequest request)
    {
        if (!Enum.TryParse<NotificationType>(request.Type, out var type))
        {
            throw new DomainValidationException("Type", $"Invalid type: {request.Type}");
        }

        var result = await _mediator.Send(command);
        return Ok(result);  // ExceptionMiddleware handles all errors
    }
}
```

### Remove These Patterns

| ❌ Anti-Pattern | ✅ Correct Pattern |
|----------------|-------------------|
| `using static Booksy.API.Middleware.ExceptionHandlingMiddleware;` | Remove import |
| `[ProducesResponseType(typeof(ApiErrorResult), ...)]` | Remove attribute |
| `new ApiErrorResult { ... }` | `throw new DomainValidationException(...)` |
| `result.Match<IActionResult>(...)` | `return Ok(result);` |
| `if (!result.IsSuccess) return BadRequest(...)` | Let handler throw exception |

**Files Fixed:**
- ✅ `NotificationsController.cs` (5 endpoints)
- ✅ `NotificationPreferencesController.cs` (6 endpoints)
- ✅ `PhoneVerificationController.cs` (3 endpoints)

**Files Still Need Fixing (11 controllers):**
- ⏳ `PaymentsController.cs`
- ⏳ `PayoutsController.cs`
- ⏳ `AvailabilityController.cs`
- ⏳ `FinancialController.cs`
- ⏳ `ProvidersController.cs`
- ⏳ `BookingsController.cs`
- ⏳ `ProviderSettingsController.cs`
- ⏳ `ServicesController.cs`
- ⏳ `AuthController.cs`
- ⏳ `UsersController.cs`
- ⏳ `AuthenticationController.cs`

---

## Domain Model Design

### ✅ Aggregate vs Entity

**When to use Aggregate vs Entity:**

| Use Aggregate When | Use Entity When |
|-------------------|-----------------|
| Root of consistency boundary | Child entity within aggregate |
| Has domain events | Simple data holder |
| Rich business logic | Anemic properties |
| Independent lifecycle | Lifecycle tied to aggregate |
| Repository access needed | Accessed through aggregate |

### Issue Fixed: Duplicate PhoneVerification

**Problem:** Two implementations existed:

**❌ Entity-based (Anemic - REMOVED):**
```csharp
// Domain/Entities/PhoneVerification.cs
public class PhoneVerification : Entity<Guid>
{
    public string PhoneNumber { get; private set; }
    public string HashedCode { get; private set; }
    public bool IsVerified { get; private set; }
    public int AttemptCount { get; private set; }

    public void IncrementAttemptCount() => AttemptCount++;
    public void MarkAsVerified() => IsVerified = true;
}
```

**✅ Aggregate-based (Rich - KEPT):**
```csharp
// Domain/Aggregates/PhoneVerificationAggregate/PhoneVerification.cs
public sealed class PhoneVerification : AggregateRoot<VerificationId>
{
    public PhoneNumber PhoneNumber { get; private set; }
    public OtpCode OtpCode { get; private set; }
    public VerificationStatus Status { get; private set; }

    // Rich business logic
    public bool Verify(string inputCode)
    {
        if (Status == VerificationStatus.Verified)
            throw new InvalidOperationException("Already verified");

        if (IsExpired())
        {
            Status = VerificationStatus.Expired;
            RaiseDomainEvent(new PhoneVerificationExpiredEvent(...));
            throw new InvalidOperationException("OTP expired");
        }

        VerificationAttempts++;
        var isValid = OtpCode.IsValid(inputCode);

        if (isValid)
        {
            Status = VerificationStatus.Verified;
            RaiseDomainEvent(new PhoneVerificationVerifiedEvent(...));
        }
        else if (VerificationAttempts >= MaxAttempts)
        {
            Status = VerificationStatus.Blocked;
            BlockedUntil = DateTime.UtcNow.AddHours(1);
            RaiseDomainEvent(new PhoneVerificationBlockedEvent(...));
        }

        return isValid;
    }
}
```

### Files Removed
- ❌ `Domain/Entities/PhoneVerification.cs`
- ❌ `Application/Services/Interfaces/IPhoneVerificationService.cs`
- ❌ `Infrastructure/Services/Application/PhoneVerificationService.cs`
- ❌ `Application/CQRS/Commands/PhoneVerification/SendVerificationCode/*`
- ❌ `Application/CQRS/Commands/PhoneVerification/VerifyCode/*`

### Files Kept
- ✅ `Domain/Aggregates/PhoneVerificationAggregate/PhoneVerification.cs`
- ✅ `Application/Commands/PhoneVerification/RequestVerification/*`
- ✅ `Application/Commands/PhoneVerification/VerifyPhone/*`
- ✅ `Application/Commands/PhoneVerification/ResendOtp/*`
- ✅ `Infrastructure/Persistence/Configurations/PhoneVerificationConfiguration.cs`

---

## Interface Contracts

### ✅ Method Signature Matching

**Always check parameter lists match interface:**

```csharp
// Interface definition
public interface IInAppNotificationService
{
    Task<(bool Success, string? ErrorMessage)> SendToUserAsync(
        Guid userId,
        string title,
        string message,
        string type,  // ← Parameter 4
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);
}

// ❌ WRONG - Missing 'type' parameter
var result = await _inAppService.SendToUserAsync(
    notification.RecipientId.Value,
    notification.Subject,
    notification.Body,
    // Missing: notification.Type.ToString(),  ← ERROR!
    notification.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
    cancellationToken);

// ✅ CORRECT - All parameters provided
var result = await _inAppService.SendToUserAsync(
    notification.RecipientId.Value,
    notification.Subject,
    notification.Body,
    notification.Type.ToString(),  // ← Added
    notification.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
    cancellationToken);
```

**Issue Fixed:**
- Interface return type was `Task` but handlers expected `Task<(bool Success, string? ErrorMessage)>`
- Missing `type` parameter in all handler calls
- Files fixed: `IInAppNotificationService.cs`, `InAppNotificationService.cs`, 3 handler files

---

## Dependency Injection

### ✅ Service Registration

**Remove obsolete service registrations:**

```csharp
// ❌ WRONG - Registering old service
services.AddScoped<IPhoneVerificationService, PhoneVerificationService>();

// ✅ CORRECT - Only register repository (handlers use CQRS)
services.AddScoped<IPhoneVerificationRepository, PhoneVerificationRepository>();
```

**Pattern:**
- Domain Services → Register interface + implementation
- Application Services (old pattern) → Remove, use CQRS handlers
- Repositories → Register interface + implementation
- External Services → Register in correct bounded context

**File Fixed:**
- ✅ `UserManagementInfrastructureExtensions.cs` - Removed `IPhoneVerificationService` registration

---

## Common Mistakes to Avoid

### 1. Result<T> Anti-Pattern

**❌ DON'T** wrap handler responses in Result<T> when using ExceptionHandlingMiddleware:

```csharp
// ❌ WRONG
public async Task<Result<MyResult>> Handle(MyCommand cmd, CancellationToken ct)
{
    try
    {
        // logic
        return Result.Success(new MyResult(...));
    }
    catch (Exception ex)
    {
        return Result.Failure(ex.Message);
    }
}

// ✅ CORRECT
public async Task<MyResult> Handle(MyCommand cmd, CancellationToken ct)
{
    // logic - let exceptions propagate
    return new MyResult(...);
}
```

### 2. Double-Wrapping Results

**❌ DON'T** wrap results that already have success/failure properties:

```csharp
// Result type already has Success/Message
public record VerifyPhoneResult(bool Success, string Message, ...);

// ❌ WRONG - Double wrapping
public async Task<Result<VerifyPhoneResult>> Handle(...)
{
    return Result.Success(new VerifyPhoneResult(true, "Verified", ...));
}
// Controller: result.Value.Success ← Double access

// ✅ CORRECT - Direct return
public async Task<VerifyPhoneResult> Handle(...)
{
    return new VerifyPhoneResult(true, "Verified", ...);
}
// Controller: result.Success ← Direct access
```

### 3. ApiErrorResult Misuse

**❌ DON'T** use ApiErrorResult in controllers:

```csharp
// ❌ WRONG
return BadRequest(new ApiErrorResult
{
    Message = "Error",
    StatusCode = 400
});

// ✅ CORRECT
throw new DomainValidationException("FieldName", "Error message");
```

### 4. Architectural Boundary Violations

**❌ DON'T** reference bounded context layers from shared infrastructure:

```csharp
// In Booksy.Infrastructure.External project:
using Booksy.ServiceCatalog.Application; // ❌ WRONG
```

**✅ DO** keep services in their bounded context:

```csharp
// In Booksy.ServiceCatalog.Infrastructure project:
using Booksy.ServiceCatalog.Application; // ✅ CORRECT
```

### 5. Missing Interface Parameters

**❌ DON'T** skip interface parameters:

```csharp
// Interface requires 5 parameters
Task<Result> DoSomething(string a, int b, bool c, string d, DateTime e);

// ❌ WRONG - Only 4 provided
await service.DoSomething(valueA, valueB, valueC, valueE); // Missing valueD!

// ✅ CORRECT - All 5 provided
await service.DoSomething(valueA, valueB, valueC, valueD, valueE);
```

### 6. Event Handler Method Names

**❌ DON'T** use `Handle` for IDomainEventHandler:

```csharp
// ❌ WRONG
public async Task Handle(MyEvent @event, CancellationToken ct) { }

// ✅ CORRECT
public async Task HandleAsync(MyEvent @event, CancellationToken ct) { }
```

### 7. Anemic Domain Models

**❌ DON'T** create entities with just getters/setters:

```csharp
// ❌ WRONG - Anemic
public class Order
{
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
}

// Usage (business logic in service)
order.Status = OrderStatus.Cancelled;
order.Total = 0;

// ✅ CORRECT - Rich domain model
public class Order : AggregateRoot
{
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped)
            throw new DomainValidationException("Cannot cancel shipped order");

        Status = OrderStatus.Cancelled;
        RaiseDomainEvent(new OrderCancelledEvent(Id, reason));
    }
}
```

---

## Verification Checklist

Before committing code, verify:

### Exception Handling
- [ ] Handlers throw exceptions (not return Result.Failure)
- [ ] Controllers don't use ApiErrorResult
- [ ] Controllers don't use result.Match()
- [ ] Validation errors throw DomainValidationException
- [ ] Not found scenarios throw NotFoundException

### Architecture
- [ ] Shared infrastructure doesn't reference bounded context layers
- [ ] Dependencies flow inward (Infrastructure → Application → Domain)
- [ ] Services are in correct bounded context infrastructure

### Commands
- [ ] All ICommand implementations have IdempotencyKey property
- [ ] Command handlers return direct results (not Result<T>)
- [ ] No double-wrapping of results

### Event Handlers
- [ ] IDomainEventHandler uses HandleAsync (not Handle)
- [ ] Domain events are raised for state changes

### Interfaces
- [ ] All interface parameters are provided in calls
- [ ] Return types match interface signatures
- [ ] Parameter order matches interface definition

### Domain Models
- [ ] Aggregates have rich business logic
- [ ] State changes through methods (not property setters)
- [ ] Domain events for important state transitions
- [ ] Value objects for complex types

### Controllers
- [ ] No ApiErrorResult imports or usage
- [ ] ProducesResponseType uses domain result types
- [ ] Return Ok(result) directly
- [ ] Let middleware handle all errors

---

## Quick Reference Commands

```bash
# Check for Result<> anti-pattern in handlers
grep -r "Task<Result<" src --include="*Handler.cs" | grep -v "obj/"

# Check for ApiErrorResult usage
grep -r "ApiErrorResult" src --include="*Controller.cs" | grep -v "obj/"

# Check for architectural violations
grep -r "Booksy.ServiceCatalog.Application" src/Infrastructure/Booksy.Infrastructure.External --include="*.cs"

# Check for Handle instead of HandleAsync
grep -r "public async Task Handle" src --include="*EventHandler.cs" | grep -v "HandleAsync"

# Check for missing IdempotencyKey
grep -r "ICommand<" src --include="*Command.cs" -A 5 | grep -v "IdempotencyKey"

# Check for result.Match() in controllers
grep -r "result.Match" src --include="*Controller.cs" | grep -v "obj/"
```

---

## Summary of This Session's Fixes

### Exception Handling Pattern (14 files)
- ✅ Removed Result<> wrapper from 9 notification handlers
- ✅ Removed Result<> wrapper from 3 phone verification handlers
- ✅ Removed Result<> wrapper from SendNotificationCommandHandler
- ✅ Removed Result<> wrapper from UpdatePreferencesCommandHandler

### Controllers (3 files)
- ✅ Fixed NotificationsController (5 endpoints)
- ✅ Fixed NotificationPreferencesController (6 endpoints)
- ✅ Fixed PhoneVerificationController (3 endpoints)

### Architectural Boundaries (6 files)
- ✅ Moved notification services from Infrastructure.External to ServiceCatalog.Infrastructure
- ✅ Updated DI registration in ServiceCatalogInfrastructureExtensions

### Event Handlers (9 files)
- ✅ Changed Handle → HandleAsync in 9 domain event handlers

### Interface Contracts (4 files)
- ✅ Fixed IInAppNotificationService return type and parameter list
- ✅ Fixed 3 handler calls to include missing 'type' parameter

### Commands (6 files)
- ✅ Added IdempotencyKey to all notification commands

### Domain Models (10 files)
- ✅ Removed duplicate Entity-based PhoneVerification
- ✅ Removed old service layer (IPhoneVerificationService, PhoneVerificationService)
- ✅ Removed old CQRS handlers (SendVerificationCode, VerifyCode)

### Total Files Modified: 52 files
- Added: 600+ lines (documentation)
- Modified: 1,500+ lines
- Deleted: 1,700+ lines (duplicates and anti-patterns)
- Net: Cleaner, more maintainable codebase

---

**Remember:** Clean Architecture + DDD + CQRS = Simple, testable, maintainable code! 🚀
