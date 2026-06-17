# Database Extensions - Centralized Database Initialization

This document describes the centralized database initialization logic for all bounded contexts.

## Overview

The `DatabaseExtensions.MigrateAndSeedDatabaseAsync` method provides a unified way to handle database migrations and seeding across all bounded contexts in the Booksy application.

## Features

### 1. **Automatic Migration Detection**
- Checks for pending migrations before applying them
- Logs migration names for transparency
- Skips migration if database is unreachable

### 2. **Robust Error Handling**
- Handles PostgreSQL duplicate table errors (42P07) gracefully
- Handles duplicate object errors (42710) gracefully
- Continues execution even if tables already exist
- Comprehensive logging for troubleshooting

### 3. **Environment-Aware Seeding**
- Only seeds data when explicitly requested
- Typically enabled for Development and Test environments
- Production environments skip seeding by default

### 4. **Structured Logging**
- All log messages include the DbContext name for easy filtering
- Clear status messages for each step
- Error logging with full exception details

## Usage

### In Program.cs

```csharp
using Booksy.API.Extensions;
using YourBoundedContext.Infrastructure.Persistence.Context;
using YourBoundedContext.Infrastructure.Persistence.Seeders;

// ... app configuration ...

// Initialize Database (migrations + seeding for dev/test)
await app.MigrateAndSeedDatabaseAsync<YourDbContext, YourSeeder>(
    seedData: app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Contains("Test"));

app.Run();
```

### Parameters

- `TContext`: The DbContext type for your bounded context
- `TSeeder`: The seeder implementation (must implement `ISeeder`)
- `seedData`: Boolean flag to control data seeding (default: false)

## Example: UserManagement Bounded Context

```csharp
await app.MigrateAndSeedDatabaseAsync<UserManagementDbContext, UserManagementDatabaseSeederOrchestrator>(
    seedData: app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Contains("Test"));
```

## Benefits

### 1. **Consistency**
- All bounded contexts use the same migration and seeding logic
- Reduces code duplication
- Easier to maintain and update

### 2. **Reliability**
- Handles common edge cases (duplicate tables, connection failures)
- Prevents crashes due to pre-existing database objects
- Clear error messages for troubleshooting

### 3. **Visibility**
- Detailed logging shows exactly what's happening
- Easy to track which migrations are applied
- Context name in logs helps identify issues in multi-context scenarios

### 4. **Flexibility**
- Environment-based control over seeding
- Can be easily extended for additional scenarios
- Supports both development and production workflows

## PostgreSQL Error Codes Handled

| Error Code | Meaning | Handling |
|------------|---------|----------|
| 42P07 | `duplicate_table` | Logs warning and continues |
| 42710 | `duplicate_object` | Logs warning and continues |

## Migration History Sync

If you encounter errors about tables already existing but migrations not being applied, you may need to manually sync the migration history:

1. Check which migrations are missing:
   ```sql
   SELECT "MigrationId", "ProductVersion"
   FROM your_schema."__EFMigrationsHistory"
   ORDER BY "MigrationId";
   ```

2. Manually add missing migrations:
   ```sql
   INSERT INTO your_schema."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
   VALUES ('20251231055438_YourMigrationName', '9.0.4');
   ```

3. See `Scripts/FixMigrationHistory.sql` for examples

## Troubleshooting

### Problem: "Table already exists" error

**Cause**: Database has tables but migration history is incomplete

**Solution**: Use the centralized error handling (already implemented) or manually sync migration history

### Problem: Seeding runs in production

**Cause**: `seedData` parameter is set to `true`

**Solution**: Ensure `seedData` is only `true` for Development/Test environments

### Problem: Migrations not applied

**Cause**: Database connection failure or permissions issue

**Solution**: Check connection string and database user permissions

## Future Enhancements

Consider adding:
- Automatic migration history sync detection
- Retry logic for transient connection failures
- Health checks integration
- Migration rollback support
