-- Migration: AddCustomerIdToUserProfile
-- Date: 2025-11-11
-- Description: Adds customer_id column to user_profiles table for linking profiles to customers

-- Add customer_id column to user_profiles table
ALTER TABLE user_management.user_profiles
    ADD COLUMN customer_id uuid NULL;

-- Create index on customer_id for better query performance
CREATE INDEX ix_user_profiles_customer_id
    ON user_management.user_profiles(customer_id);

-- Optional: Add foreign key constraint (uncomment if needed)
-- ALTER TABLE user_management.user_profiles
--     ADD CONSTRAINT fk_user_profiles_customers
--     FOREIGN KEY (customer_id)
--     REFERENCES user_management.customers(id)
--     ON DELETE SET NULL;

-- Add comment to column
COMMENT ON COLUMN user_management.user_profiles.customer_id IS 'Optional reference to customer ID if this profile belongs to a customer';
