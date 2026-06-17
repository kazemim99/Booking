-- ============================================
-- Fix Migration History
-- ============================================
-- This script manually adds missing migrations to the history table
-- when tables already exist but EF Core thinks migrations haven't been applied

-- Check current migration history
SELECT "MigrationId", "ProductVersion"
FROM user_management."__EFMigrationsHistory"
ORDER BY "MigrationId";

-- If the SyncModelSnapshot migration is missing, add it
-- Replace the timestamp with your actual migration file timestamp
INSERT INTO user_management."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20251231055438_SyncModelSnapshot', '9.0.4'
WHERE NOT EXISTS (
    SELECT 1 FROM user_management."__EFMigrationsHistory"
    WHERE "MigrationId" = '20251231055438_SyncModelSnapshot'
);

-- Verify the fix
SELECT "MigrationId", "ProductVersion"
FROM user_management."__EFMigrationsHistory"
ORDER BY "MigrationId";
