# Code Review Checklist - Before Commit

Use this checklist before committing any code to ensure architectural consistency.

## ✅ Exception Handling

- [ ] **Handlers throw exceptions** (not return `Result.Failure()`)
- [ ] **No try-catch blocks** in handlers (let middleware handle)
- [ ] **Validation errors** throw `DomainValidationException`
- [ ] **Not found scenarios** throw `NotFoundException`
- [ ] **External failures** throw `ExternalServiceException`
- [ ] **No Result<T> wrapper** on handler return types

**Example:**
```csharp
// ✅ CORRECT
public async Task<MyResult> Handle(MyCommand cmd, CancellationToken ct)
{
    if (entity == null)
        throw new NotFoundException($"Entity {id} not found");

    return new MyResult(...);
}
```

---

## ✅ Controllers

- [ ] **No ApiErrorResult** imports or usage
- [ ] **No result.Match()** pattern
- [ ] **Return Ok(result)** directly
- [ ] **No manual error handling** (throw exceptions instead)
- [ ] **ProducesResponseType** uses domain types (not ApiErrorResult)
- [ ] **Validation throws exceptions** (not returns BadRequest)

**Example:**
```csharp
// ✅ CORRECT
[HttpPost]
[ProducesResponseType(typeof(MyResult), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> DoSomething([FromBody] MyRequest request)
{
    var result = await _mediator.Send(command);
    return Ok(result);
}
```

---

## ✅ Architecture

- [ ] **Shared infrastructure** doesn't reference bounded context Application/Domain
- [ ] **Dependencies flow inward** (Infrastructure → Application → Domain)
- [ ] **Services in correct project** (bounded context infrastructure, not shared)
- [ ] **No circular references** between bounded contexts

**Verification:**
```bash
# Should return ZERO
grep -r "Booksy.ServiceCatalog.Application" src/Infrastructure/Booksy.Infrastructure.External --include="*.cs"
```

---

## ✅ Commands & Handlers

- [ ] **All ICommand** implementations have `IdempotencyKey` property
- [ ] **Command handlers** return direct results (not `Result<T>`)
- [ ] **No double-wrapping** (e.g., Result<ResultType> where ResultType has Success/Error)
- [ ] **Parameter lists match** interface signatures exactly

**Example:**
```csharp
// ✅ CORRECT
public sealed record MyCommand(
    Guid Id,
    string Name,
    Guid? IdempotencyKey = null) : ICommand<MyResult>;

public class MyCommandHandler : ICommandHandler<MyCommand, MyResult>
{
    public async Task<MyResult> Handle(MyCommand cmd, CancellationToken ct)
    {
        // Direct return, no Result<> wrapper
        return new MyResult(...);
    }
}
```

---

## ✅ Event Handlers

- [ ] **IDomainEventHandler** uses `HandleAsync` (not `Handle`)
- [ ] **Domain events** are raised for state changes
- [ ] **Event handlers** are registered in DI

**Example:**
```csharp
// ✅ CORRECT
public class MyEventHandler : IDomainEventHandler<MyEvent>
{
    public async Task HandleAsync(MyEvent @event, CancellationToken ct)
    {
        // Handle event
    }
}
```

---

## ✅ Domain Models

- [ ] **Aggregates** extend `AggregateRoot<TId>`
- [ ] **Rich business logic** (not anemic models)
- [ ] **State changes through methods** (not property setters)
- [ ] **Value objects** for complex types
- [ ] **Domain events** for important transitions
- [ ] **Private setters** on all properties
- [ ] **Factory methods** for creation (not public constructors)

**Example:**
```csharp
// ✅ CORRECT
public sealed class Order : AggregateRoot<OrderId>
{
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }

    private Order() { } // EF Core

    public static Order Create(CustomerId customerId, ...)
    {
        var order = new Order { ... };
        order.RaiseDomainEvent(new OrderCreatedEvent(...));
        return order;
    }

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

## ✅ Interfaces & Contracts

- [ ] **All parameters** provided in interface method calls
- [ ] **Return types** match interface signatures
- [ ] **Parameter order** matches interface definition
- [ ] **Async methods** return `Task` or `Task<T>`

**Verification:**
```csharp
// Interface
Task<Result> DoSomething(string a, int b, bool c);

// ✅ CORRECT implementation call
await service.DoSomething(valueA, valueB, valueC);

// ❌ WRONG - missing parameter
await service.DoSomething(valueA, valueB); // Missing valueC!
```

---

## ✅ Dependency Injection

- [ ] **Repositories** registered as scoped
- [ ] **Domain services** registered with interface
- [ ] **No old service registrations** (if using CQRS)
- [ ] **External services** in correct bounded context
- [ ] **HTTP clients** registered with typed client or named client

**Example:**
```csharp
// ✅ CORRECT
services.AddScoped<IMyRepository, MyRepository>();
services.AddScoped<IMyDomainService, MyDomainService>();

// ❌ WRONG - Old service pattern with CQRS
services.AddScoped<IMyApplicationService, MyApplicationService>();
```

---

## ✅ Testing Considerations

- [ ] **Unit tests** for domain logic
- [ ] **Integration tests** for repositories
- [ ] **Controller tests** verify correct HTTP responses
- [ ] **Test exception scenarios** (throw, not Result.Failure)

---

## Quick Automated Checks

Run these before committing:

```bash
# 1. Check for Result<> anti-pattern
grep -r "Task<Result<" src --include="*Handler.cs" | grep -v "obj/" | grep -v ".Test"

# 2. Check for ApiErrorResult usage
grep -r "ApiErrorResult" src --include="*Controller.cs" | grep -v "obj/"

# 3. Check architectural violations
grep -r "Booksy.ServiceCatalog.Application" src/Infrastructure/Booksy.Infrastructure.External --include="*.cs"
grep -r "Booksy.UserManagement.Application" src/Infrastructure/Booksy.Infrastructure.External --include="*.cs"

# 4. Check event handler method names
grep -r "public async Task Handle" src --include="*EventHandler.cs" | grep -v "HandleAsync" | grep -v "obj/"

# 5. Check for result.Match in controllers
grep -r "result.Match" src --include="*Controller.cs" | grep -v "obj/"

# 6. Check for using static ExceptionHandlingMiddleware
grep -r "using static.*ExceptionHandlingMiddleware" src --include="*Controller.cs"
```

**Expected output:** All commands should return **ZERO** results.

---

## Common Issues Quick Reference

| Issue | Solution |
|-------|----------|
| Handler returns `Result<T>` | Change to return `T` directly, throw exceptions |
| Controller uses `ApiErrorResult` | Remove, throw exceptions instead |
| Controller has `result.Match()` | Change to `return Ok(result);` |
| Missing `IdempotencyKey` | Add to command: `Guid? IdempotencyKey = null` |
| Event handler uses `Handle` | Rename to `HandleAsync` |
| Service in wrong project | Move to bounded context infrastructure |
| Double-wrapping results | Remove outer `Result<>` wrapper |
| Missing interface parameter | Add all parameters from interface signature |

---

## File Change Review

Before committing, review file changes:

### Modified Handlers?
- [ ] No `Result<>` wrapper on return type
- [ ] No try-catch blocks
- [ ] Throws appropriate exceptions

### Modified Controllers?
- [ ] No `ApiErrorResult` usage
- [ ] Returns `Ok(result)` directly
- [ ] Throws exceptions for validation

### New Commands?
- [ ] Has `IdempotencyKey` property
- [ ] Implements `ICommand<TResponse>`

### New Event Handlers?
- [ ] Uses `HandleAsync` method
- [ ] Implements `IDomainEventHandler<TEvent>`

### New Services?
- [ ] In correct bounded context infrastructure
- [ ] Registered in DI
- [ ] Interface defined in Application layer

### New Aggregates?
- [ ] Extends `AggregateRoot<TId>`
- [ ] Has rich business logic
- [ ] Raises domain events
- [ ] Private setters

---

## Final Checklist

Before pushing:

- [ ] ✅ All automated checks pass (zero results)
- [ ] ✅ Code compiles without warnings
- [ ] ✅ Follows exception handling pattern
- [ ] ✅ Controllers are clean (no manual error handling)
- [ ] ✅ Architectural boundaries respected
- [ ] ✅ Domain models are rich (not anemic)
- [ ] ✅ Tests updated/added
- [ ] ✅ No Result<> anti-pattern
- [ ] ✅ Git commit message is clear and descriptive

---

**Print this checklist and keep it visible while coding!** 📋
