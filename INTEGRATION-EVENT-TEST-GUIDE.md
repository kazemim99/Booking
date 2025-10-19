# Integration Event Testing Guide

This guide walks you through testing the complete cross-context integration event flow from ServiceCatalog to UserManagement.

## Prerequisites

Before testing, ensure the following services are running:

### 1. PostgreSQL Database
```bash
# Should be running on localhost:54321
# ServiceCatalog DB: booksy_service_catalog_dev
# UserManagement DB: booksy_user_management
```

### 2. RabbitMQ Message Broker
```bash
# Should be running on localhost:5672
# Username: booksy_admin
# Password: Booksy@2024!

# Management UI (optional): http://localhost:15672
```

### 3. Redis Cache
```bash
# Should be running on localhost:6379
# Password: Redis@2024!
```

## Test Flow Overview

When a provider registers in ServiceCatalog:

1. **Domain Event** `ProviderRegisteredEvent` is raised within the Provider aggregate
2. **SimpleDomainEventDispatcher** dispatches to `ProviderRegisteredEventHandler`
3. **Integration Event** `ProviderRegisteredIntegrationEvent` is published to CAP outbox
4. **RabbitMQ** transports the message to the `booksy.events` exchange
5. **UserManagement** CAP subscriber receives via `ProviderRegisteredEventSubscriber`
6. **User aggregate** is updated:
   - Role: Add "Provider"
   - Status: Change from Draft to Pending

## Step-by-Step Test Procedure

### Step 1: Start Both APIs

Start ServiceCatalog API:
```bash
cd c:\Repos\Booksy\src\BoundedContexts\ServiceCatalog\Booksy.ServiceCatalog.Api
dotnet run
```
- API will run on: https://localhost:7002
- CAP Dashboard: https://localhost:7002/cap

Start UserManagement API (in a new terminal):
```bash
cd c:\Repos\Booksy\src\UserManagement\Booksy.UserManagement.API
dotnet run
```
- API will run on: https://localhost:7001
- CAP Dashboard: https://localhost:7001/cap

### Step 2: Register a New User

First, create a user that will become a provider:

```bash
POST https://localhost:7001/api/v1/authentication/register
Content-Type: application/json

{
  "email": "provider@test.com",
  "password": "Test@1234",
  "confirmPassword": "Test@1234",
  "firstName": "Test",
  "lastName": "Provider",
  "phoneNumber": "+989123456789"
}
```

**Expected Response:**
```json
{
  "userId": "guid-user-id-here",
  "email": "provider@test.com",
  "status": "Draft"
}
```

Save the `userId` for the next step.

### Step 3: Login and Get JWT Token

```bash
POST https://localhost:7001/api/v1/authentication/login
Content-Type: application/json

{
  "email": "provider@test.com",
  "password": "Test@1234"
}
```

**Expected Response:**
```json
{
  "accessToken": "jwt-token-here",
  "refreshToken": "refresh-token-here",
  "expiresIn": 7200
}
```

Save the `accessToken` for authenticated requests.

### Step 4: Register Provider with Full Information

Now register the provider in ServiceCatalog:

```bash
POST https://localhost:7002/api/v1/providers/register-full
Authorization: Bearer {your-jwt-token}
Content-Type: application/json

{
  "businessName": "Test Beauty Salon",
  "description": "A premium beauty salon providing excellent services",
  "contactEmail": "contact@testbeauty.com",
  "contactPhone": "+989123456789",
  "website": "https://testbeauty.com",
  "providerType": "BeautySalon",
  "address": {
    "street": "123 Main Street",
    "city": "Tehran",
    "state": "Tehran",
    "postalCode": "1234567890",
    "country": "Iran",
    "latitude": 35.6892,
    "longitude": 51.3890
  },
  "workingHours": [
    {
      "dayOfWeek": 1,
      "isOpen": true,
      "openTime": { "hours": 9, "minutes": 0 },
      "closeTime": { "hours": 18, "minutes": 0 },
      "breaks": []
    },
    {
      "dayOfWeek": 2,
      "isOpen": true,
      "openTime": { "hours": 9, "minutes": 0 },
      "closeTime": { "hours": 18, "minutes": 0 },
      "breaks": []
    },
    {
      "dayOfWeek": 3,
      "isOpen": true,
      "openTime": { "hours": 9, "minutes": 0 },
      "closeTime": { "hours": 18, "minutes": 0 },
      "breaks": []
    },
    {
      "dayOfWeek": 4,
      "isOpen": true,
      "openTime": { "hours": 9, "minutes": 0 },
      "closeTime": { "hours": 18, "minutes": 0 },
      "breaks": []
    },
    {
      "dayOfWeek": 5,
      "isOpen": true,
      "openTime": { "hours": 9, "minutes": 0 },
      "closeTime": { "hours": 18, "minutes": 0 },
      "breaks": []
    },
    {
      "dayOfWeek": 6,
      "isOpen": false,
      "openTime": null,
      "closeTime": null,
      "breaks": []
    },
    {
      "dayOfWeek": 0,
      "isOpen": false,
      "openTime": null,
      "closeTime": null,
      "breaks": []
    }
  ],
  "services": [
    {
      "name": "Haircut",
      "description": "Professional haircut service",
      "categoryId": "some-category-guid",
      "price": {
        "amount": 50.00,
        "currency": "USD"
      },
      "duration": 60,
      "isActive": true
    }
  ],
  "teamMembers": []
}
```

**Expected Response:**
```json
{
  "providerId": "guid-provider-id-here",
  "businessName": "Test Beauty Salon",
  "status": "Active",
  "createdAt": "2025-10-14T..."
}
```

### Step 5: Check ServiceCatalog Logs

Look for these log messages in ServiceCatalog API console:

```
🎉 ProviderRegisteredEvent received: Provider {ProviderId} registered by owner {OwnerId}
✅ Published ProviderRegisteredIntegrationEvent to message bus for provider: {ProviderId}
```

### Step 6: Check UserManagement Logs

Look for these log messages in UserManagement API console:

```
📨 Received ProviderRegisteredIntegrationEvent for User {UserId}, Provider {ProviderId}
➕ Adding Provider role to user {UserId}
📝 Updated user {UserId} status to Pending
✅ Successfully processed ProviderRegisteredIntegrationEvent for User {UserId}
```

### Step 7: Verify CAP Dashboard

**ServiceCatalog Dashboard** (https://localhost:7002/cap):
1. Navigate to "Published" tab
2. Look for message with topic: `booksy.servicecatalog.providerregistered`
3. Status should be "Succeeded"
4. Check the message content and headers

**UserManagement Dashboard** (https://localhost:7001/cap):
1. Navigate to "Received" tab
2. Look for message with topic: `booksy.servicecatalog.providerregistered`
3. Status should be "Succeeded"
4. Check retry count (should be 0 if successful first time)

### Step 8: Verify Database Changes

**Check CAP Outbox Tables:**

```sql
-- ServiceCatalog: Published messages
SELECT
    id,
    name,
    content::json->>'ProviderId' as provider_id,
    content::json->>'OwnerId' as owner_id,
    status_name,
    added,
    expires_at,
    retries
FROM cap.published
WHERE name = 'booksy.servicecatalog.providerregistered'
ORDER BY added DESC
LIMIT 5;

-- UserManagement: Received messages
SELECT
    id,
    name,
    content::json->>'ProviderId' as provider_id,
    content::json->>'OwnerId' as owner_id,
    status_name,
    added,
    expires_at,
    retries
FROM cap.received
WHERE name = 'booksy.servicecatalog.providerregistered'
ORDER BY added DESC
LIMIT 5;
```

**Check User Was Updated:**

```sql
-- UserManagement: Check user roles and status
SELECT
    u.id,
    u.email,
    u.first_name,
    u.last_name,
    u.status,
    ARRAY_AGG(r.role_name) as roles,
    u.created_at,
    u.updated_at
FROM user_management.users u
LEFT JOIN user_management.user_roles r ON u.id = r.user_id
WHERE u.email = 'provider@test.com'
GROUP BY u.id, u.email, u.first_name, u.last_name, u.status, u.created_at, u.updated_at;
```

**Expected User State:**
- Status: `Pending` (was `Draft` before)
- Roles: Should include `"Provider"`

**Check Provider Was Created:**

```sql
-- ServiceCatalog: Check provider details
SELECT
    p.id,
    p.owner_id,
    p.business_name,
    p.provider_type,
    p.status,
    p.created_at
FROM service_catalog.providers p
WHERE p.business_name = 'Test Beauty Salon';
```

## Troubleshooting

### Issue: No logs in UserManagement

**Check:**
1. RabbitMQ is running and accessible
2. Both APIs can connect to RabbitMQ (check startup logs)
3. CAP consumer thread is running (should see in startup logs)
4. Check RabbitMQ Management UI for queues and messages

**Common Causes:**
- RabbitMQ not started
- Wrong port (should be 5672, not 56721)
- Connection credentials incorrect
- Firewall blocking connection

### Issue: Message stuck in "Failed" status

**Check:**
1. CAP dashboard for error details
2. Application logs for exception stack traces
3. Database connectivity from UserManagement
4. User exists in UserManagement database

**Common Causes:**
- User not found (UserId mismatch)
- Database connection issue
- Transaction conflict with ExecutionStrategy
- Validation error in domain logic

### Issue: "ExecutionStrategy" error

**Error Message:**
```
The configured execution strategy 'NpgsqlRetryingExecutionStrategy' does not support user-initiated transactions
```

**Solution:**
The ProviderRegisteredEventSubscriber should use `IUnitOfWork.ExecuteInTransactionAsync()` method, which properly handles the ExecutionStrategy. Check that the handler looks like this:

```csharp
await _unitOfWork.ExecuteInTransactionAsync(async () =>
{
    // ... handler logic
});
```

### Issue: CAP tables not created

**Check:**
1. Connection string is correct
2. Database user has CREATE TABLE permissions
3. CAP schema "cap" exists

**Manual Creation (if needed):**
```sql
-- Create CAP schema
CREATE SCHEMA IF NOT EXISTS cap;

-- CAP will auto-create tables on first run
```

## Expected Success Indicators

✅ All these should be true after successful test:

1. ServiceCatalog logs show: "✅ Published ProviderRegisteredIntegrationEvent"
2. UserManagement logs show: "✅ Successfully processed ProviderRegisteredIntegrationEvent"
3. CAP dashboard shows message as "Succeeded" in both contexts
4. User status changed from "Draft" to "Pending"
5. User has "Provider" role added
6. No error messages in either API console
7. CAP outbox tables have matching published/received records
8. Provider record exists in ServiceCatalog database

## Next Steps After Successful Test

Once the integration event flow is working:

1. **Test Error Scenarios:**
   - Invalid user ID
   - Database connection loss
   - RabbitMQ connection loss
   - Verify CAP retry mechanism

2. **Performance Testing:**
   - Bulk provider registrations
   - Monitor message processing time
   - Check CAP cleanup job

3. **Add More Integration Events:**
   - ProviderUpdatedIntegrationEvent
   - ProviderDeactivatedIntegrationEvent
   - ServiceAddedIntegrationEvent

4. **Monitoring:**
   - Set up Prometheus metrics for CAP
   - Add structured logging with correlation IDs
   - Configure alerting for failed messages

## Reference Files

### Key Implementation Files:

**ServiceCatalog:**
- [ProviderRegisteredEvent.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Events/ProviderRegisteredEvent.cs)
- [ProviderRegisteredEventHandler.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/ProviderRegisteredEventHandler.cs)
- [ProviderRegisteredIntegrationEvent.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/IntegrationEvents/ProviderRegisteredIntegrationEvent.cs)

**UserManagement:**
- [ProviderRegisteredEventSubscriber.cs](src/UserManagement/Booksy.UserManagement.Application/EventHandlers/IntegrationEventHandlers/ProviderRegisteredEventSubscriber.cs)

**Infrastructure:**
- [SimpleDomainEventDispatcher.cs](src/Infrastructure/Booksy.Infrastructure.Core/EventBus/SimpleDomainEventDispatcher.cs)
- [CapIntegrationEventPublisher.cs](src/Infrastructure/Booksy.Infrastructure.Core/EventBus/CapIntegrationEventPublisher.cs)
- [CapEventBusExtensions.cs](src/Infrastructure/Booksy.Infrastructure.Core/EventBus/CapEventBusExtensions.cs)
- [EfCoreUnitOfWork.cs](src/Infrastructure/Booksy.Infrastructure.Core/Persistence/Base/EfCoreUnitOfWork.cs) - See ExecuteInTransactionAsync method

## Documentation

- [INTEGRATION-EVENTS-IMPLEMENTATION.md](INTEGRATION-EVENTS-IMPLEMENTATION.md) - Complete implementation guide
- [CAP-HANDLERS-TRANSACTION-GUIDE.md](CAP-HANDLERS-TRANSACTION-GUIDE.md) - Transaction handling in CAP handlers
