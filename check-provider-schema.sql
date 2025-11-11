-- Quick check for PrimaryPhoneNationalNumber column in Providers table

-- 1. Check if column exists
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'ServiceCatalog'
AND table_name = 'Providers'
AND column_name LIKE '%Phone%'
ORDER BY column_name;

-- 2. Check migration history
SELECT "MigrationId", "ProductVersion"
FROM "ServiceCatalog"."__EFMigrationsHistory"
ORDER BY "MigrationId" DESC;

-- 3. If you need to verify the table structure
\d "ServiceCatalog"."Providers"
