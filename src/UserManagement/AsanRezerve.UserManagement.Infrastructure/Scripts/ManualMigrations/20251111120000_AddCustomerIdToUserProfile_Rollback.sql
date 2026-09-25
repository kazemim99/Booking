-- Rollback Migration: AddCustomerIdToUserProfile
-- Date: 2025-11-11
-- Description: Removes customer_id column from user_profiles table

-- Drop foreign key constraint if it exists
-- ALTER TABLE user_management.user_profiles
--     DROP CONSTRAINT IF EXISTS fk_user_profiles_customers;

-- Drop index
DROP INDEX IF EXISTS user_management.ix_user_profiles_customer_id;

-- Drop column
ALTER TABLE user_management.user_profiles
    DROP COLUMN IF EXISTS customer_id;
