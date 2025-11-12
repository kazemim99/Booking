# How to Apply the AddCustomerIdToUserProfile Migration

## Problem
The EF Core configuration for `UserProfile` includes a `CustomerId` shadow property, but the database column doesn't exist yet, causing this error:

```
Npgsql.PostgresException: '42703: column u0.customer_id does not exist
```

## Solution
Apply the migration to add the `customer_id` column to the `user_profiles` table.

## Option 1: Manual SQL Execution (Recommended for Quick Fix)

### Apply Migration:
```bash
psql -U your_username -d your_database_name -f 20251111120000_AddCustomerIdToUserProfile.sql
```

Or connect to your database and run the SQL directly:
```sql
-- Add customer_id column to user_profiles table
ALTER TABLE user_management.user_profiles
    ADD COLUMN customer_id uuid NULL;

-- Create index on customer_id for better query performance
CREATE INDEX ix_user_profiles_customer_id
    ON user_management.user_profiles(customer_id);
```

### Rollback (if needed):
```bash
psql -U your_username -d your_database_name -f 20251111120000_AddCustomerIdToUserProfile_Rollback.sql
```

## Option 2: Entity Framework Core Migration (If dotnet is available)

### Apply Migration:
```bash
cd src/UserManagement/Booksy.UserManagement.Infrastructure
dotnet ef database update --startup-project ../Booksy.UserManagement.API --context UserManagementDbContext
```

### Rollback:
```bash
dotnet ef database update FixUserProfileCustomerIdConflict --startup-project ../Booksy.UserManagement.API --context UserManagementDbContext
```

## Verification

After applying the migration, verify the column exists:

```sql
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'user_management'
  AND table_name = 'user_profiles'
  AND column_name = 'customer_id';
```

Expected output:
```
 column_name  | data_type | is_nullable
--------------+-----------+-------------
 customer_id  | uuid      | YES
```

## What This Migration Does

1. **Adds `customer_id` column**: Nullable UUID column to link user profiles to customers
2. **Creates index**: `ix_user_profiles_customer_id` for performance
3. **Optional FK constraint**: Commented out by default, can be enabled if needed

## Files Created

- `20251111120000_AddCustomerIdToUserProfile.cs` - C# migration file
- `20251111120000_AddCustomerIdToUserProfile.sql` - SQL script to apply
- `20251111120000_AddCustomerIdToUserProfile_Rollback.sql` - SQL script to rollback
- `README_APPLY_MIGRATION.md` - This file

## Related Changes

This migration corresponds to the value converter added in:
- `UserProfileConfiguration.cs` (lines 149-153)

The value converter tells EF Core how to map the `CustomerId` value object to a database Guid.
