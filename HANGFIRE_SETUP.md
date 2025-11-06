# Hangfire Setup Instructions

## Overview
Hangfire is required to run the `ProcessScheduledNotificationsJob` for scheduled notification processing.

## 1. Install Hangfire NuGet Packages

Add to `Booksy.ServiceCatalog.Infrastructure.csproj`:

```bash
dotnet add package Hangfire.Core
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql
```

## 2. Register Hangfire in DI

Add to `ServiceCatalogInfrastructureExtensions.cs`:

```csharp
using Hangfire;
using Hangfire.PostgreSql;

public static IServiceCollection AddServiceCatalogInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing code ...

    // Add Hangfire with PostgreSQL storage
    var connectionString = configuration.GetConnectionString("ServiceCatalog")
        ?? configuration.GetConnectionString("DefaultConnection");

    services.AddHangfire(config =>
    {
        config.UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
        {
            SchemaName = "hangfire",
            QueuePollInterval = TimeSpan.FromSeconds(15)
        });

        config.UseSimpleAssemblyNameTypeSerializer();
        config.UseRecommendedSerializerSettings();
    });

    // Add Hangfire server
    services.AddHangfireServer(options =>
    {
        options.WorkerCount = 5; // Adjust based on load
        options.Queues = new[] { "default", "notifications" };
        options.ServerName = "ServiceCatalog-Worker";
    });

    return services;
}
```

## 3. Configure in Program.cs

Add to `ServiceCatalog.Api/Program.cs` (AFTER `app.UseAuthorization()`):

```csharp
using Hangfire;
using Booksy.ServiceCatalog.Infrastructure.BackgroundJobs;

// Map Hangfire Dashboard (AFTER app.UseAuthorization())
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "Booksy - Notification Jobs"
});

// Register recurring job
RecurringJob.AddOrUpdate<ProcessScheduledNotificationsJob>(
    "process-scheduled-notifications",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.EveryMinute, // Runs every minute
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc,
        QueueName = "notifications"
    });
```

## 4. Create Hangfire Authorization Filter

Create `Filters/HangfireAuthorizationFilter.cs`:

```csharp
using Hangfire.Dashboard;

namespace Booksy.ServiceCatalog.Api.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow only Admin users
            return httpContext.User.IsInRole("Admin")
                || httpContext.User.IsInRole("SuperAdmin");
        }
    }
}
```

## 5. Run Hangfire Migrations

Hangfire will automatically create its schema and tables on first run. You can also manually create the schema:

```sql
CREATE SCHEMA IF NOT EXISTS hangfire;
```

## 6. Access Hangfire Dashboard

After setup, access the dashboard at:
- **URL:** `https://localhost:7001/hangfire`
- **Auth:** Admin/SuperAdmin role required

## 7. Monitor Job Execution

The dashboard shows:
- **Recurring Jobs:** `process-scheduled-notifications` (runs every minute)
- **Processing:** Currently running jobs
- **Succeeded:** Completed jobs count
- **Failed:** Failed jobs with error details

## 8. Testing

To test scheduled notifications:

1. Create a notification with `ScheduledFor` in the future:
```csharp
var notification = Notification.Create(
    userId: UserId.From(userId),
    type: NotificationType.System,
    channel: NotificationChannel.Email,
    recipient: "user@example.com",
    subject: "Test Notification",
    content: "This is a test",
    scheduledFor: DateTime.UtcNow.AddMinutes(2) // Schedule 2 minutes from now
);
```

2. Wait for the job to run (every minute)
3. Check the dashboard for job execution
4. Check notification status in database (should be `Delivered` or `Failed`)

## 9. Production Configuration

For production, consider:

```csharp
services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2; // Scale with CPU
    options.Queues = new[] { "critical", "default", "notifications", "low" };
    options.ServerName = $"{Environment.MachineName}-{Guid.NewGuid():N}";
});
```

## 10. Troubleshooting

**Job not running:**
- Check Hangfire dashboard for errors
- Verify database connection
- Check worker server is running

**Performance issues:**
- Adjust `WorkerCount` based on load
- Use priority queues (critical > default > notifications > low)
- Monitor database connection pool

**Memory issues:**
- Set `JobExpirationCheckInterval` to reduce cleanup frequency
- Limit succeeded job retention
