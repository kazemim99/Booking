# Implementation Roadmap - Ready to Execute

**Status:** Ready for implementation
**Estimated Total Time:** ~10 hours
**Current Branch:** `claude/notification-communication-system-011CUqTSNUvx1YVDjTFAb3v7`

---

## ✅ Completed Work (This Session)

### Architecture & Patterns
- ✅ Fixed 14 notification command handlers (removed Result<> wrapper)
- ✅ Fixed 3 controllers (Notifications, NotificationPreferences, PhoneVerification)
- ✅ Fixed 9 domain event handlers (Handle → HandleAsync)
- ✅ Moved 6 notification services to correct bounded context
- ✅ Removed duplicate PhoneVerification implementation
- ✅ Fixed interface signatures and parameter matching
- ✅ Added IdempotencyKey to all commands
- ✅ Created comprehensive documentation (3 files, 1,400+ lines)

### Infrastructure
- ✅ NotificationTemplateSeeder exists with 15 templates
- ✅ All notification repositories implemented
- ✅ All EF Core configurations exist
- ✅ All notification services implemented

**Progress:** ~45% → ~50% (Session 1 was already done!)

---

## 🎯 Remaining Work - Execution Plan

### Part 1: Controller Fixes (4-5 hours)
**Pattern to Apply:** Same as NotificationsController

**Files to Fix:**
1. `PaymentsController.cs` (324 lines, ~6 endpoints)
2. `PayoutsController.cs` (220 lines, ~5 endpoints)
3. `AvailabilityController.cs` (~4 endpoints)
4. `FinancialController.cs` (~5 endpoints)
5. `ProvidersController.cs` (~8 endpoints)
6. `BookingsController.cs` (~10 endpoints)
7. `ProviderSettingsController.cs` (~4 endpoints)
8. `ServicesController.cs` (~7 endpoints)
9. `AuthController.cs` (~5 endpoints)
10. `UsersController.cs` (~8 endpoints)
11. `AuthenticationController.cs` (~6 endpoints)

**Changes per controller:**
```csharp
// REMOVE
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;
[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status500InternalServerError)]
new ApiErrorResult { Message = ..., StatusCode = ... }
return result.Match<IActionResult>(...);

// ADD
throw new DomainValidationException("Field", "Message");
throw new NotFoundException("Entity not found");
return Ok(result);
```

---

### Part 2: Background Job (2 hours)

**File to Create:**
`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/BackgroundJobs/ProcessScheduledNotificationsJob.cs`

```csharp
public class ProcessScheduledNotificationsJob
{
    private readonly INotificationReadRepository _readRepo;
    private readonly INotificationWriteRepository _writeRepo;
    private readonly IEmailNotificationService _emailService;
    private readonly ISmsNotificationService _smsService;
    private readonly IPushNotificationService _pushService;
    private readonly IInAppNotificationService _inAppService;
    private readonly ILogger<ProcessScheduledNotificationsJob> _logger;

    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Get notifications due now
        var notifications = await _readRepo.GetScheduledNotificationsDueAsync(ct);

        foreach (var notification in notifications)
        {
            try
            {
                // Send based on channel
                var messageId = await SendNotificationAsync(notification, ct);
                notification.MarkAsDelivered(messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification {Id}", notification.Id);
                notification.MarkAsFailed(ex.Message);
            }
            finally
            {
                await _writeRepo.UpdateNotificationAsync(notification, ct);
            }
        }
    }

    private async Task<string?> SendNotificationAsync(Notification notification, CancellationToken ct)
    {
        return notification.Channel switch
        {
            NotificationChannel.Email => await SendEmailAsync(notification, ct),
            NotificationChannel.SMS => await SendSmsAsync(notification, ct),
            NotificationChannel.PushNotification => await SendPushAsync(notification, ct),
            NotificationChannel.InApp => await SendInAppAsync(notification, ct),
            _ => throw new NotSupportedException($"Channel {notification.Channel} not supported")
        };
    }

    // Implementation methods...
}
```

**Add to Repository:**
```csharp
// INotificationReadRepository.cs
Task<List<Notification>> GetScheduledNotificationsDueAsync(CancellationToken ct = default);

// NotificationReadRepository.cs
public async Task<List<Notification>> GetScheduledNotificationsDueAsync(CancellationToken ct = default)
{
    return await _context.Notifications
        .Where(n => n.Status == NotificationStatus.Queued
            && n.ScheduledFor.HasValue
            && n.ScheduledFor.Value <= DateTime.UtcNow)
        .OrderBy(n => n.ScheduledFor)
        .Take(100)
        .ToListAsync(ct);
}
```

**Register with Hangfire:**
```csharp
// In ServiceCatalogInfrastructureExtensions.cs
services.AddHangfire(config => config.UsePostgreSqlStorage(connectionString));
services.AddHangfireServer();

// In Program.cs
RecurringJob.AddOrUpdate<ProcessScheduledNotificationsJob>(
    "process-scheduled-notifications",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.EveryMinute);
```

---

### Part 3: Move NotificationHub (15 minutes)

**Current:** `src/Infrastructure/Booksy.Infrastructure.External/Hubs/NotificationHub.cs`
**Target:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Hubs/NotificationHub.cs`

**Steps:**
1. Create `Hubs` directory in ServiceCatalog.Infrastructure
2. Move NotificationHub.cs
3. Update namespace: `Booksy.ServiceCatalog.Infrastructure.Hubs`
4. Update SignalR registration in DI

---

### Part 4: IdempotencyBehavior (1 hour)

**File to Create:**
`src/Core/Booksy.Core.Application/Behaviors/IdempotencyBehavior.cs`

```csharp
public class IdempotencyBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey == null)
            return await next();

        var cacheKey = $"idempotency:{typeof(TRequest).Name}:{request.IdempotencyKey}";

        // Check cache
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation("Duplicate request: {Key}", cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        // Process request
        var response = await next();

        // Cache for 24 hours
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            options,
            cancellationToken);

        return response;
    }
}
```

**Register:**
```csharp
// In CoreApplicationExtensions.cs
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
```

---

### Part 5: Configuration (30 minutes)

**File to Create:**
`src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/appsettings.Notifications.example.json`

```json
{
  "NotificationSettings": {
    "SendGrid": {
      "ApiKey": "SG.xxxxxxxxxxxxx",
      "FromEmail": "noreply@booksy.com",
      "FromName": "Booksy Platform",
      "Templates": {
        "BookingConfirmation": "d-template-id-xxx",
        "PaymentReceipt": "d-template-id-xxx"
      }
    },
    "Sms": {
      "Provider": "Rahyab",
      "ApiKey": "xxxxxxxxxxxxx",
      "ApiUrl": "https://api.rahyab.ir/v1",
      "SenderNumber": "+989123456789",
      "Username": "booksy_user",
      "Password": "secure_password"
    },
    "Firebase": {
      "ProjectId": "booksy-notifications",
      "ServerKey": "xxxxxxxxxxxxx",
      "SenderId": "123456789",
      "ServiceAccountJsonPath": "firebase-service-account.json"
    },
    "SignalR": {
      "HubUrl": "/notifications-hub",
      "EnableDetailedErrors": true,
      "KeepAliveInterval": "00:00:15",
      "ClientTimeoutInterval": "00:00:30"
    },
    "RateLimiting": {
      "MaxNotificationsPerUserPerHour": 50,
      "MaxNotificationsPerUserPerDay": 200,
      "MaxBulkNotificationRecipients": 1000
    },
    "Retry": {
      "MaxAttempts": 3,
      "InitialDelay": "00:00:05",
      "MaxDelay": "00:05:00"
    }
  }
}
```

---

### Part 6: Testing (3 hours)

**Create Test Files:**

1. `NotificationCommandHandlersTests.cs` - Unit tests for all command handlers
2. `NotificationQueriesTests.cs` - Unit tests for query handlers
3. `NotificationServicesTests.cs` - Unit tests for email/SMS/push services
4. `NotificationControllerTests.cs` - Integration tests for API endpoints
5. `NotificationBackgroundJobTests.cs` - Tests for scheduled notification processing

**Test Coverage:**
- ✅ Command handlers throw correct exceptions
- ✅ Queries return expected data
- ✅ Services handle failures gracefully
- ✅ Controllers return correct HTTP status codes
- ✅ Background job processes scheduled notifications
- ✅ Idempotency prevents duplicate processing
- ✅ Rate limiting works correctly

---

## 📋 Execution Checklist

### Session 2: Controllers (2 hours)
- [ ] Fix PaymentsController
- [ ] Fix PayoutsController
- [ ] Fix AvailabilityController
- [ ] Fix FinancialController
- [ ] Fix ProvidersController
- [ ] Fix BookingsController
- [ ] Commit: "refactor: Remove ApiErrorResult from 6 ServiceCatalog controllers"

### Session 3: Background Processing (2 hours)
- [ ] Move NotificationHub to ServiceCatalog.Infrastructure
- [ ] Create ProcessScheduledNotificationsJob
- [ ] Add GetScheduledNotificationsDueAsync to repository
- [ ] Register Hangfire
- [ ] Test scheduled notification flow
- [ ] Commit: "feat: Add scheduled notification background processing"

### Session 4: Middleware & Config (1.5 hours)
- [ ] Create IdempotencyBehavior
- [ ] Register in DI
- [ ] Create appsettings.Notifications.example.json
- [ ] Verify all DI registrations
- [ ] Commit: "feat: Add idempotency middleware and notification configuration"

### Session 5: Remaining Controllers (1.5 hours)
- [ ] Fix ProviderSettingsController
- [ ] Fix ServicesController
- [ ] Fix AuthController (UserManagement)
- [ ] Fix UsersController (UserManagement)
- [ ] Fix AuthenticationController (UserManagement)
- [ ] Commit: "refactor: Remove ApiErrorResult from remaining controllers"

### Session 6: Testing (3 hours)
- [ ] Create unit test files
- [ ] Create integration test files
- [ ] Add test cases for happy paths
- [ ] Add test cases for error scenarios
- [ ] Commit: "test: Add comprehensive notification system tests"

### Final Steps
- [ ] Run verification commands (all should return 0)
- [ ] Create PR description
- [ ] Update REMAINING_WORK.md → mark as COMPLETE
- [ ] Push all changes

---

## 🚀 Quick Start Commands

```bash
# Verify no ApiErrorResult (should be 0 after Session 5)
grep -r "ApiErrorResult" src --include="*Controller.cs" | grep -v "obj/" | wc -l

# Verify no Result<> in handlers (should be 0)
grep -r "Task<Result<" src --include="*Handler.cs" | grep -v "obj/" | wc -l

# Run tests
dotnet test

# Create PR
gh pr create --title "feat: Complete Notification & Communication System" \
  --body-file PR_DESCRIPTION.md
```

---

## 📊 Progress Tracking

| Component | Current | Target | Progress |
|-----------|---------|--------|----------|
| Domain Models | 100% | 100% | ✅ Complete |
| Commands/Handlers | 100% | 100% | ✅ Complete |
| Queries/Handlers | 100% | 100% | ✅ Complete |
| Repositories | 100% | 100% | ✅ Complete |
| Services | 100% | 100% | ✅ Complete |
| Controllers | 25% (3/14) | 100% (14/14) | 🟡 In Progress |
| Background Jobs | 0% | 100% | ⏳ Pending |
| Middleware | 0% | 100% | ⏳ Pending |
| Configuration | 0% | 100% | ⏳ Pending |
| Tests | 0% | 80% | ⏳ Pending |
| Documentation | 100% | 100% | ✅ Complete |

**Overall:** 50% → 100% (10 hours remaining)

---

## 🎯 Next Action

**Recommendation:** Continue with Session 2 (fix controllers) → Session 3 (background job) → Session 4 (middleware) → Session 5 (remaining controllers) → Session 6 (tests).

**Alternative:** If you prefer, I can:
1. Do all controller fixes in one commit (Sessions 2 + 5 combined)
2. Then do infrastructure work (Sessions 3 + 4 combined)
3. Then tests (Session 6)

This reduces from 6 commits to 3 commits and might be more efficient.

**Your call!** 🚀
