# Remaining Work - Notification & Communication System

**Branch:** `claude/notification-communication-system-011CUqTSNUvx1YVDjTFAb3v7`
**Current Progress:** ~45% Complete
**Last Updated:** 2025-11-06

---

## 📊 High-Level Status

| Component | Status | Progress |
|-----------|--------|----------|
| Domain Models | ✅ Complete | 100% |
| Value Objects | ✅ Complete | 100% |
| Aggregates | ✅ Complete | 100% |
| Commands & Handlers | ✅ Complete | 100% |
| Queries & Handlers | ✅ Complete | 100% |
| Controllers | 🟡 Partial | 25% (3/14) |
| Repositories | ✅ Complete | 100% |
| EF Configurations | ✅ Complete | 100% |
| Services | ✅ Complete | 100% |
| Database Migrations | ❌ Not Started | 0% |
| Background Jobs | ❌ Not Started | 0% |
| SignalR Setup | 🟡 Needs Moving | 50% |
| Configuration | ❌ Not Started | 0% |
| Template Seeding | ❌ Not Started | 0% |
| Testing | ❌ Not Started | 0% |
| Documentation | ✅ Complete | 100% |

**Overall Progress: ~45%**

---

## 🎯 Critical Path (Must Do First)

### 1. Create Database Migrations ⚠️ CRITICAL
**Priority:** HIGH
**Estimated Time:** 30 minutes
**Complexity:** Low

**Why Critical:** Without migrations, notification tables don't exist in database.

**Tasks:**
```bash
# Navigate to ServiceCatalog.Infrastructure
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

# Create migration for notification tables
dotnet ef migrations add AddNotificationTables \
  --context ServiceCatalogDbContext \
  --output-dir Migrations

# Review migration file
# Apply migration to database
dotnet ef database update --context ServiceCatalogDbContext
```

**Tables to be created:**
- `notifications` (main notification records)
- `notification_templates` (email/SMS templates)
- `user_notification_preferences` (user settings)

**Files involved:**
- ✅ `NotificationConfiguration.cs` (exists)
- ✅ `NotificationTemplateConfiguration.cs` (exists)
- ✅ `UserNotificationPreferencesConfiguration.cs` (exists)
- ⏳ Migration file (needs creation)

---

### 2. Fix 11 Remaining Controllers with ApiErrorResult
**Priority:** HIGH
**Estimated Time:** 2 hours
**Complexity:** Low (Repetitive)

**Controllers to fix:**

| # | Controller | Location | Endpoints |
|---|------------|----------|-----------|
| 1 | PaymentsController | ServiceCatalog.Api | ~6 endpoints |
| 2 | PayoutsController | ServiceCatalog.Api | ~5 endpoints |
| 3 | AvailabilityController | ServiceCatalog.Api | ~4 endpoints |
| 4 | FinancialController | ServiceCatalog.Api | ~5 endpoints |
| 5 | ProvidersController | ServiceCatalog.Api | ~8 endpoints |
| 6 | BookingsController | ServiceCatalog.Api | ~10 endpoints |
| 7 | ProviderSettingsController | ServiceCatalog.Api | ~4 endpoints |
| 8 | ServicesController | ServiceCatalog.Api | ~7 endpoints |
| 9 | AuthController | UserManagement.API | ~5 endpoints |
| 10 | UsersController | UserManagement.API | ~8 endpoints |
| 11 | AuthenticationController | UserManagement.API | ~6 endpoints |

**Pattern to apply (see CODE_REVIEW_CHECKLIST.md):**
```csharp
// ❌ REMOVE
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;
[ProducesResponseType(typeof(ApiErrorResult), ...)]
new ApiErrorResult { ... }
result.Match(...)

// ✅ ADD
throw new DomainValidationException(...)
return Ok(result);
```

**Verification:**
```bash
# Should return ZERO after fixing
grep -r "ApiErrorResult" src --include="*Controller.cs" | grep -v "obj/"
```

---

### 3. Move NotificationHub to Correct Location
**Priority:** MEDIUM
**Estimated Time:** 15 minutes
**Complexity:** Low

**Current:** `Booksy.Infrastructure.External/Hubs/NotificationHub.cs`
**Target:** `Booksy.ServiceCatalog.Infrastructure/Hubs/NotificationHub.cs`

**Why:** Architectural boundary violation (same as notification services issue)

**Tasks:**
1. Create directory: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Hubs/`
2. Move `NotificationHub.cs` from Infrastructure.External
3. Update namespace: `Booksy.ServiceCatalog.Infrastructure.Hubs`
4. Update SignalR registration in `ServiceCatalogInfrastructureExtensions.cs`

**Registration code:**
```csharp
// In ServiceCatalogInfrastructureExtensions.cs
public static IServiceCollection AddNotificationSignalR(this IServiceCollection services)
{
    services.AddSignalR();
    return services;
}
```

**In Startup/Program.cs:**
```csharp
app.MapHub<NotificationHub>("/notifications-hub");
```

---

## 🔄 Medium Priority Tasks

### 4. Implement IdempotencyBehavior Middleware
**Priority:** MEDIUM
**Estimated Time:** 1 hour
**Complexity:** Medium

**Why:** IdempotencyKey property exists on all commands, but enforcement doesn't exist.

**Location:** `src/Core/Booksy.Core.Application/Behaviors/IdempotencyBehavior.cs`

**Implementation:**
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

        // Check if already processed
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation("Duplicate request detected: {Key}", cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        // Process request
        var response = await next();

        // Cache response for 24 hours
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

**Registration:**
```csharp
// In CoreApplicationExtensions.cs
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
```

---

### 5. Create Background Job for Scheduled Notifications
**Priority:** MEDIUM
**Estimated Time:** 2 hours
**Complexity:** Medium

**Why:** `ScheduleNotificationCommand` queues notifications but nothing processes them.

**Location:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/BackgroundJobs/ProcessScheduledNotificationsJob.cs`

**Implementation using Hangfire:**
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
        // Get notifications scheduled for now or earlier that are still Queued
        var notifications = await _readRepo.GetScheduledNotificationsDueAsync(ct);

        foreach (var notification in notifications)
        {
            try
            {
                await SendNotificationAsync(notification, ct);
                notification.MarkAsDelivered(messageId);
                await _writeRepo.UpdateNotificationAsync(notification, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send scheduled notification {Id}", notification.Id);
                notification.MarkAsFailed(ex.Message);
                await _writeRepo.UpdateNotificationAsync(notification, ct);
            }
        }
    }

    private async Task<string?> SendNotificationAsync(Notification notification, CancellationToken ct)
    {
        // Send logic based on channel (Email, SMS, Push, InApp)
        // Return messageId
    }
}
```

**Registration (Hangfire):**
```csharp
// In ServiceCatalogInfrastructureExtensions.cs
services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(connectionString);
});

services.AddHangfireServer();

// Schedule recurring job
RecurringJob.AddOrUpdate<ProcessScheduledNotificationsJob>(
    "process-scheduled-notifications",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.EveryMinute);
```

**New method needed in repository:**
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

---

### 6. Configure Notification Service Settings
**Priority:** MEDIUM
**Estimated Time:** 30 minutes
**Complexity:** Low

**Location:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/appsettings.json`

**Configuration structure:**
```json
{
  "NotificationSettings": {
    "SendGrid": {
      "ApiKey": "SG.xxxxxxxxxxxxx",
      "FromEmail": "noreply@booksy.com",
      "FromName": "Booksy Platform",
      "Templates": {
        "BookingConfirmation": "d-xxxxxxxxxxxxx",
        "BookingReminder": "d-xxxxxxxxxxxxx",
        "PaymentReceipt": "d-xxxxxxxxxxxxx"
      }
    },
    "Sms": {
      "Provider": "Rahyab",
      "ApiKey": "xxxxxxxxxxxxx",
      "SenderNumber": "+989123456789",
      "Username": "booksy",
      "Password": "secure_password"
    },
    "Firebase": {
      "ProjectId": "booksy-notifications",
      "ServerKey": "xxxxxxxxxxxxx",
      "SenderId": "xxxxxxxxxxxxx"
    },
    "SignalR": {
      "HubUrl": "/notifications-hub",
      "EnableDetailedErrors": true
    },
    "RateLimiting": {
      "MaxNotificationsPerUserPerHour": 50,
      "MaxNotificationsPerUserPerDay": 200,
      "MaxBulkNotificationRecipients": 1000
    }
  }
}
```

**Create example file:**
```bash
cp appsettings.json appsettings.Notifications.example.json
# Add notification settings to example
# Add to .gitignore to protect API keys
```

---

### 7. Seed Default Notification Templates
**Priority:** MEDIUM
**Estimated Time:** 1 hour
**Complexity:** Low

**Location:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/NotificationTemplateSeeder.cs`

**Implementation:**
```csharp
public class NotificationTemplateSeeder
{
    private readonly ServiceCatalogDbContext _context;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _context.NotificationTemplates.AnyAsync(ct))
            return; // Already seeded

        var templates = new[]
        {
            // Booking Confirmation Email
            NotificationTemplate.Create(
                "booking-confirmed-email",
                "Booking Confirmation - {{BookingId}}",
                NotificationType.BookingConfirmation,
                NotificationChannel.Email,
                @"
                <h2>Booking Confirmed!</h2>
                <p>Dear {{CustomerName}},</p>
                <p>Your booking has been confirmed.</p>
                <ul>
                    <li>Service: {{ServiceName}}</li>
                    <li>Provider: {{ProviderName}}</li>
                    <li>Date: {{BookingDate}}</li>
                    <li>Time: {{BookingTime}}</li>
                    <li>Price: {{Price}}</li>
                </ul>
                <p>Thank you for choosing Booksy!</p>
                ",
                "en"
            ),

            // Booking Confirmation SMS
            NotificationTemplate.Create(
                "booking-confirmed-sms",
                null,
                NotificationType.BookingConfirmation,
                NotificationChannel.SMS,
                "Booksy: Your booking with {{ProviderName}} is confirmed for {{BookingDate}} at {{BookingTime}}. ID: {{BookingId}}",
                "en"
            ),

            // Payment Receipt Email
            NotificationTemplate.Create(
                "payment-receipt-email",
                "Payment Receipt - {{PaymentId}}",
                NotificationType.PaymentProcessed,
                NotificationChannel.Email,
                @"
                <h2>Payment Receipt</h2>
                <p>Dear {{CustomerName}},</p>
                <p>Your payment has been processed successfully.</p>
                <ul>
                    <li>Amount: {{Amount}}</li>
                    <li>Payment Method: {{PaymentMethod}}</li>
                    <li>Transaction ID: {{TransactionId}}</li>
                    <li>Date: {{PaymentDate}}</li>
                </ul>
                ",
                "en"
            ),

            // Booking Reminder
            NotificationTemplate.Create(
                "booking-reminder-push",
                "Reminder: Upcoming Appointment",
                NotificationType.BookingReminder,
                NotificationChannel.PushNotification,
                "You have an appointment with {{ProviderName}} tomorrow at {{BookingTime}}",
                "en"
            )
        };

        _context.NotificationTemplates.AddRange(templates);
        await _context.SaveChangesAsync(ct);
    }
}
```

**Registration:**
```csharp
// In ServiceCatalogInfrastructureExtensions.cs
services.AddScoped<NotificationTemplateSeeder>();

// In Program.cs or Startup
public static async Task Main(string[] args)
{
    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<NotificationTemplateSeeder>();
        await seeder.SeedAsync();
    }

    await app.RunAsync();
}
```

---

## 📝 Low Priority Tasks

### 8. Add Notification Service Registrations to DI
**Priority:** LOW
**Estimated Time:** 15 minutes
**Complexity:** Low

**Why:** Services exist but might not all be registered.

**Location:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/DependencyInjection/ServiceCatalogInfrastructureExtensions.cs`

**Verify registration:**
```csharp
public static IServiceCollection AddNotificationServices(this IServiceCollection services)
{
    // Template engine
    services.AddSingleton<ITemplateEngine, TemplateEngine>();
    services.AddScoped<INotificationTemplateService, NotificationTemplateService>();

    // Notification services
    services.AddScoped<IEmailNotificationService, SendGridEmailNotificationService>();
    services.AddScoped<ISmsNotificationService, RahyabSmsNotificationService>();
    services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();
    services.AddScoped<IInAppNotificationService, InAppNotificationService>();

    // HTTP clients for external services
    services.AddHttpClient<SendGridEmailNotificationService>();
    services.AddHttpClient<RahyabSmsNotificationService>();

    return services;
}
```

**Verify in ServiceCatalogModule:**
```csharp
public static IServiceCollection AddServiceCatalogInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing code

    services.AddNotificationServices(); // ✅ Ensure this is called

    return services;
}
```

---

### 9. Test End-to-End Notification Flows
**Priority:** LOW
**Estimated Time:** 3 hours
**Complexity:** Medium

**Test scenarios:**

| Scenario | Description | Expected Result |
|----------|-------------|-----------------|
| 1. Send immediate email | POST /notifications with Email channel | Email sent via SendGrid |
| 2. Send immediate SMS | POST /notifications with SMS channel | SMS sent via Rahyab |
| 3. Send push notification | POST /notifications with Push channel | Push sent via Firebase |
| 4. Send in-app notification | POST /notifications with InApp channel | SignalR message received |
| 5. Schedule notification | POST /notifications/schedule | Queued, sent by background job |
| 6. Bulk notifications | POST /notifications/bulk | All recipients receive |
| 7. Cancel notification | POST /notifications/{id}/cancel | Status changed to Cancelled |
| 8. Resend failed | POST /notifications/{id}/resend | Notification retried |
| 9. Get history | GET /notifications/history | List of notifications |
| 10. Get analytics | GET /notifications/analytics | Statistics returned |
| 11. Update preferences | PUT /notifications/preferences | Settings saved |
| 12. Get preferences | GET /notifications/preferences | Current settings |
| 13. Quiet hours | Send during quiet hours | Delayed until allowed time |
| 14. Rate limiting | Exceed daily limit | 429 Too Many Requests |
| 15. Invalid template | Use non-existent template | 404 Not Found |
| 16. Idempotency | Send same request twice | Only processed once |

**Test tools:**
- Postman collection (exists: `Notification_API.postman_collection.json`)
- Integration tests (need creation)
- Unit tests (need creation)

---

### 10. Create API Documentation
**Priority:** LOW
**Estimated Time:** 1 hour
**Complexity:** Low

**Already created:** `NOTIFICATION_QUICK_REFERENCE.md`

**Additional documentation needed:**
- OpenAPI/Swagger annotations
- Postman collection examples
- Integration guide for other services

---

## 📋 Summary Checklist

### Critical (Do First)
- [ ] ⚠️ Create database migrations for notification tables
- [ ] Fix 11 controllers using ApiErrorResult
- [ ] Move NotificationHub to ServiceCatalog.Infrastructure

### Important (Do Next)
- [ ] Implement IdempotencyBehavior middleware
- [ ] Create background job for scheduled notifications
- [ ] Configure notification service settings (SendGrid, SMS, Firebase)
- [ ] Seed default notification templates

### Nice to Have
- [ ] Verify DI registrations
- [ ] Test end-to-end flows
- [ ] Complete API documentation

---

## 🎯 Recommended Order of Execution

**Session 1: Database & Core Setup (2 hours)**
1. Create database migrations
2. Apply migrations
3. Seed notification templates
4. Verify tables exist

**Session 2: Controller Fixes (2 hours)**
1. Fix PaymentsController
2. Fix PayoutsController
3. Fix AvailabilityController
4. Fix FinancialController
5. Fix ProvidersController
6. Fix BookingsController

**Session 3: Background Processing (2 hours)**
1. Move NotificationHub
2. Implement background job for scheduled notifications
3. Test scheduled notification flow
4. Configure Hangfire

**Session 4: Middleware & Configuration (1.5 hours)**
1. Implement IdempotencyBehavior
2. Configure notification service settings
3. Verify DI registrations
4. Test idempotency

**Session 5: Controller Fixes Continued (1.5 hours)**
1. Fix ProviderSettingsController
2. Fix ServicesController
3. Fix AuthController
4. Fix UsersController
5. Fix AuthenticationController

**Session 6: Testing & Documentation (3 hours)**
1. Test all notification flows
2. Fix any bugs found
3. Update documentation
4. Create integration tests

**Total Estimated Time: ~12 hours**

---

## 📞 Quick Commands

```bash
# Create migration
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure
dotnet ef migrations add AddNotificationTables --context ServiceCatalogDbContext

# Apply migration
dotnet ef database update --context ServiceCatalogDbContext

# Verify no ApiErrorResult
grep -r "ApiErrorResult" src --include="*Controller.cs" | grep -v "obj/"

# Verify no Result<> in handlers
grep -r "Task<Result<" src --include="*Handler.cs" | grep -v "obj/"

# Test API endpoints
curl -X POST http://localhost:5000/api/v1/notifications \
  -H "Content-Type: application/json" \
  -d '{"recipientId":"...","type":"Email",...}'
```

---

**Next Step:** Start with database migrations - they're blocking everything else! 🚀
