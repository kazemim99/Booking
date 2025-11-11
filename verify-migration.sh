#!/bin/bash

echo "=== Checking EF Core Migration Status ==="
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

echo ""
echo "1. Listing pending migrations..."
dotnet ef migrations list --startup-project ../Booksy.ServiceCatalog.Api --context ServiceCatalogDbContext

echo ""
echo "2. Checking database connection..."
dotnet ef dbcontext info --startup-project ../Booksy.ServiceCatalog.Api --context ServiceCatalogDbContext

echo ""
echo "=== Checking Database Schema Directly ==="
echo "3. Verifying Providers table columns..."

# Connect to PostgreSQL and check if PrimaryPhoneNationalNumber column exists
PGPASSWORD='Booksy@2024!' psql -h localhost -p 54321 -U booksy_admin -d booksy_service_catalog_dev -c "
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'ServiceCatalog'
AND table_name = 'Providers'
AND column_name LIKE '%Phone%'
ORDER BY column_name;
"

echo ""
echo "4. Checking migration history..."
PGPASSWORD='Booksy@2024!' psql -h localhost -p 54321 -U booksy_admin -d booksy_service_catalog_dev -c "
SELECT \"MigrationId\", \"ProductVersion\"
FROM \"ServiceCatalog\".\"__EFMigrationsHistory\"
ORDER BY \"MigrationId\" DESC
LIMIT 5;
"

echo ""
echo "=== If column is missing, run the migration ==="
echo "5. Apply migrations..."
dotnet ef database update --startup-project ../Booksy.ServiceCatalog.Api --context ServiceCatalogDbContext
