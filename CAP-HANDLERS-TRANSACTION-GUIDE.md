# CAP Event Handlers and Transactions Guide

## 🎯 Problem: Why TransactionBehavior Doesn't Execute

### **The Issue**

When you use **CAP's `[CapSubscribe]`** to handle integration events, the handler **does NOT go through MediatR's pipeline behaviors** like `TransactionBehavior`, `ValidationBehavior`, etc.

```csharp
// ❌ This does NOT use MediatR pipeline
[CapSubscribe("booksy.servicecatalog.providerregistered")]
public async Task HandleAsync(ProviderRegisteredIntegrationEvent @event)
{
    // No TransactionBehavior wrapping this!
    // No automatic transaction!
}
```

### **Why?**

```
┌─────────────────────────────────────────────────────────────┐
│ MediatR Pipeline (Commands/Queries)                        │
│                                                             │
│  Request → MediatR                                          │
│           ├─> LoggingBehavior                               │
│           ├─> ValidationBehavior                            │
│           ├─> TransactionBehavior ✅                        │
│           │   └─> BeginTransaction()                        │
│           │   └─> CommandHandler.Handle()                   │
│           │   └─> CommitTransaction()                       │
│           └─> CommandHandler                                │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ CAP Pipeline (Integration Events)                          │
│                                                             │
│  RabbitMQ → CAP Consumer → [CapSubscribe] Handler          │
│                                                             │
│  ❌ Bypasses MediatR completely                             │
│  ❌ No behaviors executed                                   │
│  ❌ No automatic transaction                                │
│  ❌ Must handle transactions manually!                      │
└─────────────────────────────────────────────────────────────┘
```

---

## ✅ Solution: Manual Transaction Management

### **Pattern 1: Inject IUnitOfWork and Manage Transactions**

```csharp
public sealed class ProviderRegisteredEventSubscriber : ICapSubscribe
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork; // ✅ Inject UnitOfWork
    private readonly ILogger<ProviderRegisteredEventSubscriber> _logger;

    [CapSubscribe("booksy.servicecatalog.providerregistered")]
    public async Task HandleAsync(ProviderRegisteredIntegrationEvent @event)
    {
        try
        {
            // ✅ Start transaction manually
            await _unitOfWork.BeginTransactionAsync();

            // Load aggregate
            var user = await _userRepository.GetByIdAsync(userId);

            // Modify aggregate
            user.AddRole("Provider");
            user.SetStatus(UserStatus.Pending);

            // Update repository
            await _userRepository.UpdateAsync(user);

            // ✅ Save changes and commit
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("✅ Successfully processed event");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to process event");

            // ✅ Rollback on error
            await _unitOfWork.RollbackTransactionAsync();

            throw; // CAP will retry
        }
    }
}
```

---

## 🔄 Complete Event Flow

### **ServiceCatalog → UserManagement Flow**

```
┌─────────────────────────────────────────────────────────────┐
│ 1. ServiceCatalog Context                                  │
│                                                             │
│  Provider.Register()                                       │
│  └─> Domain Event: ProviderRegisteredEvent                 │
│      └─> Domain Event Handler                              │
│          └─> Publishes Integration Event via CAP           │
│              └─> CAP saves to cap.published table          │
│                  (within ServiceCatalog DB transaction)     │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼ RabbitMQ
┌─────────────────────────────────────────────────────────────┐
│ 2. RabbitMQ Exchange: booksy.events                        │
│    Topic: booksy.servicecatalog.providerregistered         │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. UserManagement Context                                  │
│                                                             │
│  CAP Consumer receives message                             │
│  └─> ProviderRegisteredEventSubscriber.HandleAsync()       │
│      ├─> BeginTransaction() ✅                              │
│      ├─> Load User aggregate                                │
│      ├─> user.AddRole("Provider")                           │
│      ├─> user.SetStatus(UserStatus.Pending)                │
│      ├─> UpdateAsync(user)                                  │
│      ├─> SaveChangesAsync() ✅                              │
│      └─> CommitTransaction() ✅                             │
│                                                             │
│  CAP marks message as processed in cap.received table      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎭 Transaction Scenarios

### **Scenario 1: Success**

```csharp
[CapSubscribe("topic")]
public async Task HandleAsync(Event @event)
{
    await _unitOfWork.BeginTransactionAsync();

    // All operations succeed
    user.AddRole("Provider");
    await _userRepository.UpdateAsync(user);

    await _unitOfWork.SaveChangesAsync();      // ✅ Changes persisted
    await _unitOfWork.CommitTransactionAsync(); // ✅ Transaction committed

    // CAP marks message as "Succeeded" in cap.received
}
```

### **Scenario 2: Business Logic Error**

```csharp
[CapSubscribe("topic")]
public async Task HandleAsync(Event @event)
{
    await _unitOfWork.BeginTransactionAsync();

    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null) // ❌ Business error
    {
        await _unitOfWork.RollbackTransactionAsync(); // ✅ Rollback
        return; // Exit gracefully - message marked as "Succeeded"
    }

    // Continue...
}
```

### **Scenario 3: Exception**

```csharp
[CapSubscribe("topic")]
public async Task HandleAsync(Event @event)
{
    try
    {
        await _unitOfWork.BeginTransactionAsync();

        // Something throws exception
        throw new Exception("Database error"); // ❌
    }
    catch (Exception ex)
    {
        await _unitOfWork.RollbackTransactionAsync(); // ✅ Rollback
        throw; // CAP will retry based on retry policy
    }

    // CAP marks message as "Failed" after max retries
}
```

---

## 📊 Comparison: MediatR vs CAP Handlers

| Aspect | MediatR Command | CAP Event Handler |
|--------|----------------|-------------------|
| **Pipeline Behaviors** | ✅ Automatic | ❌ None |
| **TransactionBehavior** | ✅ Automatic | ❌ Manual |
| **ValidationBehavior** | ✅ Automatic | ❌ Manual |
| **LoggingBehavior** | ✅ Automatic | ❌ Manual |
| **Transaction Management** | Automatic via behavior | Must call BeginTransaction/Commit |
| **Error Handling** | MediatR handles | Must handle + rollback |
| **Retry Logic** | None (single request) | CAP automatic retry |
| **Idempotency** | Not needed | Should implement |

---

## 🛡️ Best Practices for CAP Handlers

### **1. Always Use Transactions**

```csharp
✅ DO:
await _unitOfWork.BeginTransactionAsync();
try {
    // Your logic
    await _unitOfWork.SaveChangesAsync();
    await _unitOfWork.CommitTransactionAsync();
} catch {
    await _unitOfWork.RollbackTransactionAsync();
    throw;
}

❌ DON'T:
await _userRepository.UpdateAsync(user); // No transaction!
```

### **2. Implement Idempotency**

```csharp
[CapSubscribe("topic")]
public async Task HandleAsync(Event @event)
{
    // ✅ Check if already processed
    if (await WasAlreadyProcessed(@event.EventId))
    {
        _logger.LogInformation("Event already processed, skipping");
        return;
    }

    // Process event
    // ...

    // Mark as processed
    await MarkAsProcessed(@event.EventId);
}
```

### **3. Log Everything**

```csharp
[CapSubscribe("topic")]
public async Task HandleAsync(Event @event)
{
    _logger.LogInformation("📨 Received event {@Event}", @event);

    try
    {
        // Process
        _logger.LogInformation("✅ Successfully processed");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Failed to process event");
        throw;
    }
}
```

### **4. Handle Missing Data Gracefully**

```csharp
[CapSubscribe("topic")]
public async Task HandleAsync(Event @event)
{
    var user = await _userRepository.GetByIdAsync(userId);

    if (user == null)
    {
        _logger.LogWarning("User not found, skipping");
        return; // ✅ Don't throw - message marked as succeeded
    }

    // Continue processing
}
```

### **5. Use Descriptive Error Messages**

```csharp
catch (Exception ex)
{
    _logger.LogError(ex,
        "Failed to handle ProviderRegisteredIntegrationEvent. " +
        "UserId: {UserId}, ProviderId: {ProviderId}, EventId: {EventId}",
        @event.OwnerId,
        @event.ProviderId,
        @event.EventId); // ✅ Include context for debugging

    throw;
}
```

---

## 🔍 Debugging CAP Handlers

### **Check CAP Dashboard**

```
http://localhost:5001/cap

Tabs:
- Published Messages (outbox)
- Received Messages (inbox)
- Subscribers (registered handlers)
```

### **Check Database Tables**

```sql
-- Check if message was received
SELECT * FROM cap.received
WHERE name = 'booksy.servicecatalog.providerregistered'
ORDER BY added DESC;

-- Check if message was published
SELECT * FROM cap.published
WHERE name = 'booksy.servicecatalog.providerregistered'
ORDER BY added DESC;

-- Check user was updated
SELECT u.id, u.status, r.role_name
FROM user_management.users u
LEFT JOIN user_management.user_roles r ON u.id = r.user_id
WHERE u.id = 'your-user-id';
```

### **Check Logs**

```
ServiceCatalog Logs:
🎉 ProviderRegisteredEvent received
✅ Published ProviderRegisteredIntegrationEvent

UserManagement Logs:
📨 Received ProviderRegisteredIntegrationEvent
➕ Adding Provider role to user
📝 Updated user status to Pending
✅ Successfully processed ProviderRegisteredIntegrationEvent
```

---

## 🎯 Summary

### **Key Takeaways**

1. ✅ **CAP handlers bypass MediatR** - No pipeline behaviors
2. ✅ **Must manage transactions manually** - BeginTransaction/Commit/Rollback
3. ✅ **Inject IUnitOfWork** - Handle SaveChanges and transactions
4. ✅ **Implement idempotency** - CAP may deliver same message twice
5. ✅ **Log extensively** - CAP dashboard + application logs
6. ✅ **Handle errors gracefully** - Rollback + throw for CAP retry

### **Transaction Pattern**

```csharp
public sealed class MyEventSubscriber : ICapSubscribe
{
    private readonly IRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    [CapSubscribe("my.topic")]
    public async Task HandleAsync(MyEvent @event)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Your business logic here

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw; // CAP will retry
        }
    }
}
```

---

**Remember**: CAP handlers are **infrastructure entry points**, not application commands. They need explicit transaction management because they don't go through MediatR's pipeline! 🚀
