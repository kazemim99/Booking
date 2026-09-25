# DbContext Configuration Fix

> **📋 See Also**: [Complete Session Summary](../../../../SESSION_SUMMARY_DDD_AND_CONFIGURATION_FIXES.md) - Includes all changes from this session

## Issue
The `ServiceCatalogDbContext` was **not being registered** in the production/development DI container. The `AddDbContext` configuration was entirely commented out in `ServiceCatalogInfrastructureExtensions.cs`.

## Root Cause
**File**: [ServiceCatalogInfrastructureExtensions.cs:46-71](../DependencyInjection/ServiceCatalogInfrastructureExtensions.cs#L46-L71)

The entire DbContext registration was commented out:
```csharp
//// Database Context
//services.AddDbContext<ServiceCatalogDbContext>(options =>
//{
//    var connectionString = configuration.GetConnectionString("ServiceCatalog")
//        ?? configuration.GetConnectionString("DefaultConnection");
//    // ... all commented out
//});
```

This meant:
- ✅ **Test environment**: DbContext was registered via `ServiceCatalogTestWebApplicationFactory`
- ❌ **Development environment**: DbContext was **NOT registered** at all
- ❌ **Production environment**: DbContext was **NOT registered** at all

## Solution

### Uncommented and Fixed DbContext Registration
**File**: [ServiceCatalogInfrastructureExtensions.cs:46-70](../DependencyInjection/ServiceCatalogInfrastructureExtensions.cs#L46-L70)

```csharp
// Database Context
services.AddDbContext<ServiceCatalogDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("ServiceCatalog")
        ?? configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(ServiceCatalogDbContext).Assembly.FullName);
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ServiceCatalog"); // ✅ Fixed schema
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });

    // Enable logging in development
    if (configuration.GetValue<bool>("DatabaseSettings:EnableSensitiveDataLogging"))
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
});
```

### Key Fix: Migration History Table Schema
**Changed**: `"user_management"` → `"ServiceCatalog"`

The original commented code had:
```csharp
npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "user_management"); // ❌ Wrong schema!
```

This was a copy-paste error from the UserManagement bounded context. Fixed to:
```csharp
npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ServiceCatalog"); // ✅ Correct schema
```

## How DbContext is Registered in Different Environments

### 1. Test Environment (Integration Tests)
**File**: `ServiceCatalogTestWebApplicationFactory.cs`

```csharp
// Test factory REMOVES the production DbContext registration
var descriptor = services.SingleOrDefault(
    d => d.ServiceType == typeof(DbContextOptions<ServiceCatalogDbContext>));
if (descriptor != null)
{
    services.Remove(descriptor);
}

// Test factory REPLACES it with Testcontainers connection
services.AddDbContext<ServiceCatalogDbContext>(options =>
{
    options.UseNpgsql(_postgresFixture.ConnectionString);
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
});
```

**Result**: Tests use a fresh PostgreSQL container for each test run.

### 2. Development/Production Environment
**File**: `ServiceCatalogInfrastructureExtensions.cs`

```csharp
services.AddDbContext<ServiceCatalogDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("ServiceCatalog")
        ?? configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, /* ... */);
});
```

**Result**: Uses connection string from `appsettings.json` or `appsettings.Development.json`.

## Connection String Configuration

### Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "ServiceCatalog": "Host=localhost;Port=54321;Database=asanrezerve_service_catalog_dev;Username=asanrezerve_user;Password=your_password",
    "DefaultConnection": "Host=localhost;Port=54321;Database=asanrezerve_service_catalog_dev;Username=asanrezerve_user;Password=your_password"
  },
  "DatabaseSettings": {
    "EnableSensitiveDataLogging": true
  }
}
```

### Test (ServiceCatalogTestWebApplicationFactory)
```csharp
// Automatically provided by PostgresTestContainerFixture
["ConnectionStrings:ServiceCatalog"] = _postgresFixture.ConnectionString,
["ConnectionStrings:DefaultConnection"] = _postgresFixture.ConnectionString,
["DatabaseSettings:EnableSensitiveDataLogging"] = "true"
```

## Benefits of This Fix

### ✅ Now Works in All Environments
- **Development**: Can run API locally with proper database connection
- **Test**: Continues to work with Testcontainers (no change)
- **Production**: Properly configured for deployment

### ✅ Proper Schema Configuration
- Migration history table now uses correct `ServiceCatalog` schema
- Prevents conflicts with other bounded contexts (e.g., UserManagement)

### ✅ Production-Ready Features
- **Connection Resilience**: Retry on failure (max 3 attempts, 5s delay)
- **Command Timeout**: 30 seconds for long-running queries
- **Development Logging**: Detailed errors and sensitive data logging (dev only)

## Testing the Fix

### 1. Run API in Development
```bash
dotnet run --project "C:\Repos\Booking\src\BoundedContexts\ServiceCatalog\AsanRezerve.ServiceCatalog.Api"
```

Expected: API starts successfully and seeder runs.

### 2. Run Integration Tests
```bash
dotnet test "C:\Repos\Booking\tests\AsanRezerve.ServiceCatalog.IntegrationTests"
```

Expected: Tests use Testcontainers and pass.

### 3. Verify Database Connection
Check that migrations are applied to the correct schema:
```sql
SELECT * FROM "ServiceCatalog"."__EFMigrationsHistory";
```

## Summary

| Environment | DbContext Registration | Connection String Source |
|-------------|----------------------|-------------------------|
| **Test** | `ServiceCatalogTestWebApplicationFactory` | Testcontainers (auto-generated) |
| **Development** | `AddServiceCatalogInfrastructure` | `appsettings.Development.json` |
| **Production** | `AddServiceCatalogInfrastructure` | `appsettings.Production.json` or environment variables |

✅ **All environments now properly configured**
✅ **Correct schema for migration history table**
✅ **Production-ready resilience and retry logic**
