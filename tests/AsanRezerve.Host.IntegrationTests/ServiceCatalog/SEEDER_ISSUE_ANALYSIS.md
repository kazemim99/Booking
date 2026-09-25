# Database Seeder Not Running - Root Cause Analysis

## Problem
The `ServiceCatalogDatabaseSeederOrchestrator` and its seeders (including `AvailabilitySeeder`) are never being executed in integration tests, causing booking creation tests to fail with "The requested time slot is not available".

## Root Causes

### 1. Production Bug (`Startup.cs:219`)
```csharp
// CURRENT CODE (BUGGY):
if (env.IsDevelopment())
{
    using var scope = app.ApplicationServices.CreateScope();
    var seeder = scope.ServiceProvider.InitializeDatabaseAsync(); // ❌ NOT AWAITED!
}
```

**Issue**: The `InitializeDatabaseAsync()` method returns a `Task` but is not awaited. The method runs asynchronously but the code doesn't wait for it to complete, so the seeder may never actually execute.

**Fix**:
```csharp
// CORRECTED CODE:
if (env.IsDevelopment())
{
    using var scope = app.ApplicationServices.CreateScope();
    await scope.ServiceProvider.InitializeDatabaseAsync(); // ✅ AWAITED
}
```

### 2. Test Environment Missing Seeder Call

**Location**: `ServiceCatalogTestWebApplicationFactory.cs` line 89-94

```csharp
// CURRENT CODE:
var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
dbContext.Database.EnsureCreated(); // ❌ Only creates schema, no seed data
```

**Issue**: The test factory only calls `EnsureCreated()` which creates the database schema but **does NOT** run the seeder. The seeder needs to be explicitly called.

**Fix**:
```csharp
// CORRECTED CODE:
var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

// Create database schema
dbContext.Database.EnsureCreated();

// Run seeders to populate test data
await scope.ServiceProvider.InitializeDatabaseAsync();
```

## Impact

Without the seeder running:

1. ❌ No `ProviderAvailability` records are created
2. ❌ No test data for providers, services, bookings
3. ❌ Booking creation fails because availability checks require `ProviderAvailability` entities
4. ❌ All availability-related tests fail

## Seeder Execution Flow

```
InitializeDatabaseAsync()
  ↓
ServiceCatalogDatabaseSeederOrchestrator.SeedAsync()
  ↓
├─ ProviderSeeder.SeedAsync()
├─ ServiceSeeder.SeedAsync()
├─ StaffSeeder.SeedAsync()
├─ AvailabilitySeeder.SeedAsync()  ← Creates ProviderAvailability for 90 days
├─ BookingSeeder.SeedAsync()
└─ ... other seeders
```

## Required Changes

### File 1: `Startup.cs` (Lines 216-220)

```diff
// Run database seeder in development
if (env.IsDevelopment())
{
    using var scope = app.ApplicationServices.CreateScope();
-   var seeder = scope.ServiceProvider.InitializeDatabaseAsync();
+   await scope.ServiceProvider.InitializeDatabaseAsync();
}
```

### File 2: `ServiceCatalogTestWebApplicationFactory.cs` (Lines 89-94)

```diff
// Build service provider and ensure database is created
var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

dbContext.Database.EnsureCreated();
+
+// Run database seeders to populate test data
+scope.ServiceProvider.InitializeDatabaseAsync().GetAwaiter().GetResult();
```

**Note**: In the test factory, we use `.GetAwaiter().GetResult()` because we're in a synchronous `ConfigureServices` method.

## Alternative: Conditional Seeding

If you want to control seeding per-test, you could make it optional:

```csharp
public class ServiceCatalogTestWebApplicationFactory<TStartup>
{
    public bool SeedDatabase { get; set; } = true; // Default to true

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ... existing code ...

            if (SeedDatabase)
            {
                scope.ServiceProvider.InitializeDatabaseAsync()
                    .GetAwaiter().GetResult();
            }
        });
    }
}
```

Then in tests where you don't want seed data:
```csharp
public class MyTest : ServiceCatalogIntegrationTestBase
{
    public MyTest(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
        factory.SeedDatabase = false; // Skip seeding for this test
    }
}
```

## Verification

After applying these fixes, you should see:

1. ✅ Seeder logs in console output during test runs
2. ✅ `ProviderAvailability` table populated with 90 days of data
3. ✅ Booking creation tests pass
4. ✅ Availability API tests work correctly

Check logs for:
```
[INF] Starting provider availability seeding for 90-day rolling window...
[INF] Successfully seeded 12960 availability slots for 2 providers over 90 days
```
