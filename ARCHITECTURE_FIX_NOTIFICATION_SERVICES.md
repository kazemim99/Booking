# Notification Services Architecture Fix

## Issue
The notification service implementations were incorrectly placed in `Booksy.Infrastructure.External`, which violated Clean Architecture and Bounded Context principles by creating an illegal dependency:

```
Booksy.Infrastructure.External → Booksy.ServiceCatalog.Application ❌ WRONG
```

This is a violation because:
1. `Booksy.Infrastructure.External` is **shared infrastructure** across all bounded contexts
2. It should NOT reference bounded context-specific Application layers
3. This creates tight coupling and violates the Dependency Inversion Principle

## Solution
All notification service implementations have been moved to the correct location:

```
Booksy.ServiceCatalog.Infrastructure → Booksy.ServiceCatalog.Application ✅ CORRECT
```

### Files Moved

**From:** `Booksy.Infrastructure.External/Notifications/`
**To:** `Booksy.ServiceCatalog.Infrastructure/Notifications/`

1. `TemplateEngine.cs` → `Booksy.ServiceCatalog.Infrastructure/Notifications/`
2. `NotificationTemplateService.cs` → `Booksy.ServiceCatalog.Infrastructure/Notifications/`
3. `InAppNotificationService.cs` → `Booksy.ServiceCatalog.Infrastructure/Notifications/`
4. `SendGridEmailNotificationService.cs` → `Booksy.ServiceCatalog.Infrastructure/Notifications/Email/`
5. `RahyabSmsNotificationService.cs` → `Booksy.ServiceCatalog.Infrastructure/Notifications/Sms/`
6. `FirebasePushNotificationService.cs` → `Booksy.ServiceCatalog.Infrastructure/Notifications/Push/`

### Files Kept in External

Only the **NotificationHub** (SignalR hub) remains in External:
- `Booksy.Infrastructure.External/Hubs/NotificationHub.cs` ✅

This is correct because:
- SignalR Hub is pure infrastructure
- It has NO dependencies on Application layer
- It's reusable across bounded contexts

## DI Registration Changes

### Before (WRONG)
```csharp
// In ExternalServicesExtensions.cs
services.AddScoped<Booksy.ServiceCatalog.Application.Services.Notifications.IEmailNotificationService,
    Booksy.Infrastructure.External.Notifications.Email.SendGridEmailNotificationService>();
// ... more registrations
```

### After (CORRECT)
```csharp
// In ServiceCatalogInfrastructureExtensions.cs
public static IServiceCollection AddNotificationServices(this IServiceCollection services)
{
    services.AddSingleton<ITemplateEngine, TemplateEngine>();
    services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
    services.AddScoped<IEmailNotificationService, SendGridEmailNotificationService>();
    services.AddScoped<ISmsNotificationService, RahyabSmsNotificationService>();
    services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();
    services.AddScoped<IInAppNotificationService, InAppNotificationService>();

    services.AddHttpClient<SendGridEmailNotificationService>();
    services.AddHttpClient<RahyabSmsNotificationService>();

    return services;
}
```

Called from `AddServiceCatalogInfrastructure()`:
```csharp
// Notification Services
services.AddNotificationServices();
```

## Architecture Layers (Clean Architecture)

```
┌─────────────────────────────────────────────────────────┐
│ Presentation (API)                                      │
│ - Controllers                                           │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ Application                                             │
│ - Commands, Queries, Handlers                          │
│ - Interfaces (IEmailNotificationService, etc.)          │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ Domain                                                  │
│ - Aggregates, Entities, Value Objects                  │
│ - Domain Events, Repository Interfaces                 │
└─────────────────────────────────────────────────────────┘
                          ↑
┌─────────────────────────────────────────────────────────┐
│ Infrastructure                                          │
│ - Repository Implementations                           │
│ - Notification Service Implementations ✅ HERE         │
│ - Database Context                                     │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ External (Shared)                                       │
│ - SignalR Hubs                                         │
│ - Storage Services                                     │
│ - Analytics Services                                   │
└─────────────────────────────────────────────────────────┘
```

## Bounded Context Boundaries

```
ServiceCatalog Bounded Context:
├── Application
│   └── Services/Notifications
│       ├── IEmailNotificationService (interface)
│       ├── ISmsNotificationService (interface)
│       ├── IPushNotificationService (interface)
│       ├── IInAppNotificationService (interface)
│       ├── ITemplateEngine (interface)
│       └── INotificationTemplateService (interface)
│
└── Infrastructure
    └── Notifications ✅ Implementations belong here
        ├── TemplateEngine
        ├── NotificationTemplateService
        ├── InAppNotificationService
        ├── Email/SendGridEmailNotificationService
        ├── Sms/RahyabSmsNotificationService
        └── Push/FirebasePushNotificationService
```

## Benefits of This Fix

1. ✅ **Respects Bounded Context Boundaries** - ServiceCatalog implementations stay within ServiceCatalog
2. ✅ **Follows Dependency Inversion Principle** - Infrastructure depends on Application, not vice versa
3. ✅ **Maintains Clean Architecture** - Clear separation of concerns
4. ✅ **Prevents Cross-Bounded-Context Pollution** - External infrastructure stays generic
5. ✅ **Improves Testability** - Easier to mock and test within bounded context
6. ✅ **Enables Future Scaling** - Other bounded contexts can have their own notification implementations

## No Breaking Changes for Users

This is an **internal architectural refactoring**. The public API and functionality remain identical:
- Same interfaces
- Same DI registration (just moved location)
- Same behavior
- Documentation unchanged (doesn't reference internal structure)

## Verification

Zero architectural violations remain:
```bash
grep -r "Booksy.ServiceCatalog.Application" src/Infrastructure/Booksy.Infrastructure.External --include="*.cs"
# Result: No matches ✅
```

---

**Date:** 2025-11-06
**Reason:** Architectural compliance and Clean Architecture principles
**Impact:** Internal only, no breaking changes to API
