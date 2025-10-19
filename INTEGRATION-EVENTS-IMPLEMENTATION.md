# Integration Events Implementation with CAP

## 📋 Overview

This document describes the complete implementation of **cross-context integration events** using **DotNetCore.CAP** with **RabbitMQ** and **PostgreSQL Outbox Pattern** in the Booksy modular monolith.

---

## 🏗️ Architecture

### Components

1. **RabbitMQ** - Message broker for asynchronous communication
2. **CAP** - .NET library providing outbox pattern and reliable message delivery
3. **PostgreSQL** - Database for outbox tables (`cap.published` and `cap.received`)
4. **Domain Events** - Internal events within a bounded context
5. **Integration Events** - Cross-context events published via CAP

### Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ ServiceCatalog Context                                          │
│                                                                 │
│  Provider.Register()                                           │
│      │                                                          │
│      ├──> Raises: ProviderRegisteredEvent (Domain Event)      │
│      │                                                          │
│      └──> ProviderRegisteredEventHandler                       │
│              │                                                  │
│              ├──> Maps to: ProviderRegisteredIntegrationEvent  │
│              │                                                  │
│              └──> IIntegrationEventPublisher.PublishAsync()    │
│                      │                                          │
│                      └──> CAP Publisher                         │
│                              │                                  │
│                              ├──> Saves to cap.published table │
│                              │    (within same DB transaction) │
│                              │                                  │
│                              └──> Background process publishes  │
│                                   to RabbitMQ                   │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   │ RabbitMQ
                                   │ Topic: booksy.servicecatalog.providerregistered
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│ UserManagement Context                                          │
│                                                                 │
│  CAP Consumer (Auto-discovery)                                 │
│      │                                                          │
│      └──> ProviderRegisteredIntegrationEventHandler            │
│              │                                                  │
│              ├──> Loads User by OwnerId                         │
│              │                                                  │
│              ├──> Adds "Provider" role                          │
│              │                                                  │
│              └──> Saves changes                                 │
│                                                                 │
│  CAP saves message to cap.received table                        │
│  Marks as processed after successful handling                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Implementation Steps

### ✅ Step 1: Add CAP NuGet Packages

```bash
# In Booksy.Infrastructure.Core project
cd src/Infrastructure/Booksy.Infrastructure.Core
dotnet add package DotNetCore.CAP --version 8.2.0
dotnet add package DotNetCore.CAP.RabbitMQ --version 8.2.0
dotnet add package DotNetCore.CAP.PostgreSql --version 8.2.0
```

### ✅ Step 2: RabbitMQ Docker Configuration

RabbitMQ is already configured in `docker-compose.yml`:

```yaml
rabbitmq:
  image: rabbitmq:3.13.7-management
  environment:
    - RABBITMQ_DEFAULT_USER=booksy_admin
    - RABBITMQ_DEFAULT_PASS=Booksy@2024!
  ports:
    - "56721:5672"   # AMQP port
    - "15672:15672"  # Management UI
  volumes:
    - rabbitmq_data:/var/lib/rabbitmq
  networks:
    - booksy-network
  restart: unless-stopped
```

**Access RabbitMQ Management UI**: http://localhost:15672
- Username: `booksy_admin`
- Password: `Booksy@2024!`

### ✅ Step 3: CAP Publisher Implementation

**File**: `src/Infrastructure/Booksy.Infrastructure.Core/EventBus/CapIntegrationEventPublisher.cs`

Key features:
- ✅ Implements `IIntegrationEventPublisher`
- ✅ Uses CAP's outbox pattern automatically
- ✅ Convention-based topic naming: `booksy.{context}.{eventname}`
- ✅ Adds correlation headers for distributed tracing

```csharp
// Topics are automatically generated:
// ProviderRegisteredIntegrationEvent → booksy.servicecatalog.providerregistered
```

### ✅ Step 4: CAP Configuration Extension

**File**: `src/Infrastructure/Booksy.Infrastructure.Core/EventBus/CapEventBusExtensions.cs`

Configures:
- ✅ PostgreSQL outbox tables (schema: `cap`, tables: `cap.published`, `cap.received`)
- ✅ RabbitMQ connection and exchange (`booksy.events`)
- ✅ Consumer groups per context (`booksy.servicecatalog`, `booksy.usermanagement`)
- ✅ Retry policy (3 retries, 60s interval)
- ✅ Message expiration (7 days for failed messages)
- ✅ CAP Dashboard at `/cap`

### ✅ Step 5: Integration Event Definition

**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/IntegrationEvents/ProviderRegisteredIntegrationEvent.cs`

```csharp
public sealed record ProviderRegisteredIntegrationEvent(
    Guid ProviderId,
    Guid OwnerId,
    string BusinessName,
    string ProviderType, // String instead of enum for cross-context compatibility
    DateTime RegisteredAt) : IntegrationEvent
{
    // Parameterless constructor required for CAP deserialization
    public ProviderRegisteredIntegrationEvent()
        : this(Guid.Empty, Guid.Empty, string.Empty, string.Empty, DateTime.UtcNow) { }
}
```

**Design Principles**:
- ✅ Use `string` instead of `enum` for cross-context DTOs
- ✅ Include parameterless constructor for CAP deserialization
- ✅ Inherit from `IntegrationEvent` base class
- ✅ Document the topic and consumers in XML comments

### ✅ Step 6: Domain Event Handler (Publisher)

**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ProviderRegisteredEventHandler.cs`

```csharp
public sealed class ProviderRegisteredEventHandler : IDomainEventHandler<ProviderRegisteredEvent>
{
    private readonly IIntegrationEventPublisher _eventPublisher;

    public async Task HandleAsync(ProviderRegisteredEvent domainEvent, CancellationToken cancellationToken)
    {
        // Map domain event to integration event
        var integrationEvent = new ProviderRegisteredIntegrationEvent(
            domainEvent.ProviderId.Value,
            domainEvent.OwnerId.Value,
            domainEvent.BusinessName,
            domainEvent.ProviderType.ToString(), // Convert enum to string
            domainEvent.RegisteredAt);

        // Publish via CAP (automatically uses outbox pattern)
        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
```

### ✅ Step 7: Integration Event Handler (Consumer)

**File**: `src/UserManagement/Booksy.UserManagement.Application/EventHandlers/IntegrationEventHandlers/ProviderRegisteredIntegrationEventHandler.cs`

```csharp
public sealed class ProviderRegisteredIntegrationEventHandler : ICapSubscribe
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Subscribes to ProviderRegisteredIntegrationEvent
    /// Topic: booksy.servicecatalog.providerregistered
    /// Group: booksy.usermanagement (defined in CAP configuration)
    /// </summary>
    [CapSubscribe("booksy.servicecatalog.providerregistered")]
    public async Task HandleAsync(ProviderRegisteredIntegrationEvent @event)
    {
        var userId = UserId.Create(@event.OwnerId);
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null) return;

        // Add Provider role if not already present
        if (!user.HasRole("Provider") && !user.HasRole("ServiceProvider"))
        {
            user.AddRole("Provider");
        }

        await _userRepository.UpdateAsync(user);
    }
}
```

**Key Points**:
- ✅ Uses `[CapSubscribe("topic-name")]` attribute
- ✅ CAP automatically discovers handlers at startup
- ✅ Retries on failure (3 times by default)
- ✅ Messages saved to `cap.received` table

### ✅ Step 8: Register CAP in Each Context

**ServiceCatalog** - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/DependencyInjection/ServiceCatalogInfrastructureExtensions.cs`:

```csharp
// CAP Event Bus with Outbox Pattern
services.AddCapEventBus<ServiceCatalogDbContext>(configuration, "ServiceCatalog");
```

**UserManagement** - `src/UserManagement/Booksy.UserManagement.Infrastructure/DependencyInjection/UserManagementInfrastructureExtensions.cs`:

```csharp
// CAP Event Bus with Outbox Pattern
services.AddCapEventBus<UserManagementDbContext>(configuration, "UserManagement");
```

---

## 🗄️ Database Migrations

### Step 1: Create CAP Migrations

CAP automatically creates its tables on first run, but you should create proper migrations:

```bash
# ServiceCatalog Context
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure
dotnet ef migrations add AddCapTables --context ServiceCatalogDbContext

# UserManagement Context
cd src/UserManagement/Booksy.UserManagement.Infrastructure
dotnet ef migrations add AddCapTables --context UserManagementDbContext
```

### CAP Database Schema

CAP creates two tables in each database:

**1. `cap.published`** - Outbox table for published events
```sql
CREATE TABLE cap.published (
    id BIGSERIAL PRIMARY KEY,
    version VARCHAR(20),
    name VARCHAR(200) NOT NULL,
    content TEXT,
    retries INT,
    added TIMESTAMP NOT NULL,
    expiresat TIMESTAMP NULL,
    statusname VARCHAR(40) NOT NULL
);
```

**2. `cap.received`** - Inbox table for received events
```sql
CREATE TABLE cap.received (
    id BIGSERIAL PRIMARY KEY,
    version VARCHAR(20),
    name VARCHAR(200) NOT NULL,
    group VARCHAR(200) NULL,
    content TEXT,
    retries INT,
    added TIMESTAMP NOT NULL,
    expiresat TIMESTAMP NULL,
    statusname VARCHAR(40) NOT NULL
);
```

---

## 🧪 Testing the Implementation

### Manual Testing Steps

1. **Start Infrastructure**:
```bash
docker-compose up -d postgres rabbitmq
```

2. **Run Migrations**:
```bash
# ServiceCatalog
dotnet ef database update --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

# UserManagement
dotnet ef database update --project src/UserManagement/Booksy.UserManagement.Infrastructure
```

3. **Start Both APIs**:
```bash
# Terminal 1 - ServiceCatalog API
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run

# Terminal 2 - UserManagement API (if separate)
cd src/UserManagement/Booksy.UserManagement.API
dotnet run
```

4. **Register a Provider**:
```bash
POST http://localhost:5010/api/v1/providers/register-full
Authorization: Bearer <your-jwt-token>
Content-Type: application/json

{
  "ownerId": "user-guid-here",
  "categoryId": "category-guid",
  "businessInfo": { ... },
  "address": { ... },
  "location": { ... },
  "businessHours": { ... },
  "services": [ ... ],
  "assistanceOptions": [],
  "teamMembers": [ ... ]
}
```

5. **Verify Integration**:

**Check CAP Published Table**:
```sql
SELECT * FROM cap.published
WHERE name = 'booksy.servicecatalog.providerregistered'
ORDER BY added DESC LIMIT 10;
```

**Check CAP Received Table**:
```sql
SELECT * FROM cap.received
WHERE name = 'booksy.servicecatalog.providerregistered'
ORDER BY added DESC LIMIT 10;
```

**Check User Role**:
```sql
SELECT * FROM user_management.users u
JOIN user_management.user_roles r ON u.id = r.user_id
WHERE u.id = 'owner-id-from-provider-registration';
```

**Check RabbitMQ**:
- Open http://localhost:15672
- Go to "Queues" tab
- Verify exchange `booksy.events` exists
- Check queue `booksy.usermanagement` for consumer

---

## 📊 Monitoring & Observability

### CAP Dashboard

Access the built-in CAP dashboard at:
- **ServiceCatalog**: http://localhost:5010/cap
- **UserManagement**: http://localhost:5001/cap

Features:
- ✅ View published messages
- ✅ View received messages
- ✅ Check message status (Succeeded, Failed, Processing)
- ✅ Retry failed messages
- ✅ View subscribers and topics

### RabbitMQ Management

Access at http://localhost:15672

Monitor:
- ✅ Exchange: `booksy.events`
- ✅ Queues: `booksy.servicecatalog`, `booksy.usermanagement`
- ✅ Message rates
- ✅ Consumer connections

### Logs

Check application logs for:
```
[Information] Publishing integration event ProviderRegisteredIntegrationEvent to topic booksy.servicecatalog.providerregistered
[Information] Successfully published integration event ProviderRegisteredIntegrationEvent

[Information] Received ProviderRegisteredIntegrationEvent for User {UserId}
[Information] Adding Provider role to user {UserId}
[Information] Successfully processed ProviderRegisteredIntegrationEvent
```

---

## 🚨 Error Handling & Retry Strategy

### CAP Retry Configuration

```csharp
options.FailedRetryInterval = 60; // Retry every 60 seconds
options.FailedRetryCount = 3;     // Retry 3 times before moving to failed
```

### Retry Flow

1. **First Attempt**: Immediate consumption
2. **Retry 1**: After 60 seconds
3. **Retry 2**: After 120 seconds (cumulative)
4. **Retry 3**: After 180 seconds (cumulative)
5. **Failed**: Marked as failed, kept for 7 days

### Dead Letter Queue

Failed messages after all retries are moved to:
- **Exchange**: `booksy.deadletter`
- **Queue**: `booksy.deadletter.{context}`

Investigate failed messages:
```sql
SELECT * FROM cap.received
WHERE statusname = 'Failed'
ORDER BY added DESC;
```

---

## 🔐 Security Considerations

### Message Authentication

CAP messages include correlation headers:
```csharp
["EventId"] = integrationEvent.EventId
["CorrelationId"] = integrationEvent.CorrelationId
["UserId"] = integrationEvent.UserId
["SourceContext"] = "ServiceCatalog"
```

### RabbitMQ Security

Production recommendations:
- ✅ Use TLS/SSL for RabbitMQ connections
- ✅ Separate RabbitMQ users per bounded context
- ✅ Use virtual hosts for isolation
- ✅ Enable RabbitMQ authentication plugins

Example production connection string:
```
amqps://booksy_servicecatalog:SecurePassword@rabbitmq.production:5671/booksy_vhost
```

---

## 📝 Best Practices

### ✅ DO

1. **Use strings for cross-context DTOs** instead of enums
2. **Include parameterless constructors** for CAP deserialization
3. **Keep integration events immutable** (use `record` types)
4. **Version integration events** via `EventVersion` property
5. **Log all integration event publishes and consumes**
6. **Monitor CAP dashboard** regularly
7. **Handle idempotency** in event handlers (check if already processed)

### ❌ DON'T

1. **Don't share domain models** across contexts
2. **Don't use direct database calls** between contexts
3. **Don't expose internal enums** in integration events
4. **Don't ignore failed messages** - investigate and resolve
5. **Don't publish domain events** directly - map to integration events first

---

## 🔄 Adding New Integration Events

### Example: `ProviderActivatedIntegrationEvent`

**Step 1**: Define Integration Event
```csharp
public sealed record ProviderActivatedIntegrationEvent(
    Guid ProviderId,
    Guid OwnerId,
    DateTime ActivatedAt) : IntegrationEvent
{
    public ProviderActivatedIntegrationEvent()
        : this(Guid.Empty, Guid.Empty, DateTime.UtcNow) { }
}
```

**Step 2**: Publish from Domain Event Handler
```csharp
public sealed class ProviderActivatedEventHandler : IDomainEventHandler<ProviderActivatedEvent>
{
    public async Task HandleAsync(ProviderActivatedEvent domainEvent, CancellationToken ct)
    {
        var integrationEvent = new ProviderActivatedIntegrationEvent(
            domainEvent.ProviderId.Value,
            domainEvent.OwnerId.Value,
            domainEvent.ActivatedAt);

        await _eventPublisher.PublishAsync(integrationEvent, ct);
    }
}
```

**Step 3**: Subscribe in UserManagement
```csharp
public sealed class ProviderActivatedIntegrationEventHandler : ICapSubscribe
{
    [CapSubscribe("booksy.servicecatalog.provideractivated")]
    public async Task HandleAsync(ProviderActivatedIntegrationEvent @event)
    {
        var user = await _userRepository.GetByIdAsync(UserId.Create(@event.OwnerId));
        if (user != null)
        {
            // Change user status to Active
            user.Activate(activationToken: null);
            await _userRepository.UpdateAsync(user);
        }
    }
}
```

---

## 🎯 Summary

### What We Achieved

✅ **Decoupled Contexts**: ServiceCatalog and UserManagement communicate asynchronously
✅ **Reliable Delivery**: Outbox pattern ensures no message loss
✅ **Automatic Retries**: CAP handles transient failures
✅ **Distributed Tracing**: Correlation IDs for tracking events
✅ **Monitoring**: CAP Dashboard + RabbitMQ Management UI
✅ **Scalability**: Can add more contexts without changing existing code
✅ **Testability**: Can test each context independently

### Architecture Benefits

- **Consistency**: Outbox pattern guarantees at-least-once delivery
- **Performance**: Async processing doesn't block provider registration
- **Resilience**: Failed events are retried automatically
- **Maintainability**: Clear separation of concerns
- **Extensibility**: Easy to add new events and consumers

---

## 📚 Additional Resources

- [DotNetCore.CAP Documentation](https://cap.dotnetcore.xyz/)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [CAP GitHub Repository](https://github.com/dotnetcore/CAP)

---

**Implementation Date**: 2025-01-12
**Author**: Claude Code Assistant
**Version**: 1.0
