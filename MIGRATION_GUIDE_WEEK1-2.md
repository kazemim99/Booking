# Database Migration Guide - Phase 1 Week 1-2
**Date:** 2025-11-15
**Sprint:** Seed Data Enhancement + API Foundations

---

## Overview

This guide provides instructions for generating and applying EF Core migrations for the new domain entities created in Week 1-2:
- **ProviderAvailability** aggregate
- **Review** aggregate

---

## Prerequisites

1. ✅ Domain entities created:
   - `src/.../Aggregates/ProviderAvailabilityAggregate/ProviderAvailability.cs`
   - `src/.../Aggregates/ReviewAggregate/Review.cs`
   - `src/.../Enums/AvailabilityStatus.cs`

2. ✅ EF Core configurations created:
   - `src/.../Configurations/ProviderAvailabilityConfiguration.cs`
   - `src/.../Configurations/ReviewConfiguration.cs`

3. ✅ DbContext updated:
   - Added `DbSet<ProviderAvailability>` to ServiceCatalogDbContext
   - Added `DbSet<Review>` to ServiceCatalogDbContext

4. ✅ Seeders created:
   - `AvailabilitySeeder.cs` (90-day rolling window)
   - `ReviewSeeder.cs` (Persian comments)

---

## Migration Commands

### Step 1: Generate Migrations

Navigate to the Infrastructure project directory:

```bash
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure
```

Generate migration for ProviderAvailability and Review:

```bash
dotnet ef migrations add AddProviderAvailabilityAndReviewAggregates \
  --context ServiceCatalogDbContext \
  --output-dir Persistence/Migrations \
  --project Booksy.ServiceCatalog.Infrastructure.csproj \
  --startup-project ../../../Booksy.API/Booksy.API.csproj
```

---

### Step 2: Review Generated Migration

The migration should include:

#### ProviderAvailability Table:
```sql
CREATE TABLE "ProviderAvailability" (
    "Id" uuid NOT NULL,
    "ProviderId" uuid NOT NULL,
    "StaffId" uuid NULL,
    "Date" date NOT NULL,
    "StartTime" time NOT NULL,
    "EndTime" time NOT NULL,
    "Status" integer NOT NULL,
    "BookingId" uuid NULL,
    "BlockReason" varchar(500) NULL,
    "HoldExpiresAt" timestamp with time zone NULL,
    "Version" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" varchar(255) NULL,
    "LastModifiedAt" timestamp with time zone NULL,
    "LastModifiedBy" varchar(255) NULL,
    CONSTRAINT "PK_ProviderAvailability" PRIMARY KEY ("Id")
);

-- Indexes
CREATE INDEX "IX_ProviderAvailability_Provider_Date_StartTime"
    ON "ProviderAvailability" ("ProviderId", "Date", "StartTime");

CREATE INDEX "IX_ProviderAvailability_Date_Status"
    ON "ProviderAvailability" ("Date", "Status");

CREATE INDEX "IX_ProviderAvailability_BookingId"
    ON "ProviderAvailability" ("BookingId");

CREATE INDEX "IX_ProviderAvailability_HoldExpiration_Status"
    ON "ProviderAvailability" ("HoldExpiresAt", "Status")
    WHERE "HoldExpiresAt" IS NOT NULL;
```

#### Reviews Table:
```sql
CREATE TABLE "Reviews" (
    "Id" uuid NOT NULL,
    "ProviderId" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "RatingValue" decimal(3,1) NOT NULL,
    "Comment" varchar(2000) NULL,
    "IsVerified" boolean NOT NULL DEFAULT true,
    "ProviderResponse" varchar(1000) NULL,
    "ProviderResponseDate" timestamp with time zone NULL,
    "HelpfulCount" integer NOT NULL DEFAULT 0,
    "NotHelpfulCount" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" varchar(255) NULL,
    "LastModifiedAt" timestamp with time zone NULL,
    "LastModifiedBy" varchar(255) NULL,
    CONSTRAINT "PK_Reviews" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Reviews_Rating" CHECK ("RatingValue" >= 1.0 AND "RatingValue" <= 5.0)
);

-- Indexes
CREATE INDEX "IX_Reviews_ProviderId"
    ON "Reviews" ("ProviderId");

CREATE INDEX "IX_Reviews_CustomerId"
    ON "Reviews" ("CustomerId");

CREATE UNIQUE INDEX "IX_Reviews_BookingId"
    ON "Reviews" ("BookingId");

CREATE INDEX "IX_Reviews_Provider_Rating"
    ON "Reviews" ("ProviderId", "RatingValue");

CREATE INDEX "IX_Reviews_Provider_CreatedAt"
    ON "Reviews" ("ProviderId", "CreatedAt");

CREATE INDEX "IX_Reviews_Verified_CreatedAt"
    ON "Reviews" ("IsVerified", "CreatedAt");
```

---

### Step 3: Apply Migrations to Development Database

**IMPORTANT**: Always test migrations in development first!

```bash
# Update database to latest migration
dotnet ef database update \
  --context ServiceCatalogDbContext \
  --project Booksy.ServiceCatalog.Infrastructure.csproj \
  --startup-project ../../../Booksy.API/Booksy.API.csproj
```

---

### Step 4: Verify Migration Success

Connect to PostgreSQL and verify tables were created:

```sql
-- Check ProviderAvailability table
SELECT table_name, column_name, data_type
FROM information_schema.columns
WHERE table_name = 'ProviderAvailability';

-- Check Reviews table
SELECT table_name, column_name, data_type
FROM information_schema.columns
WHERE table_name = 'Reviews';

-- Verify indexes
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename IN ('ProviderAvailability', 'Reviews');
```

---

### Step 5: Run Seed Data

After migrations are applied, run the seeder orchestrator:

**Option A: Via API endpoint (if seeder endpoint exists)**
```bash
curl -X POST http://localhost:5000/api/admin/seed \
  -H "Content-Type: application/json"
```

**Option B: Via CLI or startup configuration**

Update `Program.cs` or create a CLI command to run:

```csharp
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider
        .GetRequiredService<ServiceCatalogDatabaseSeederOrchestrator>();

    await seeder.SeedAsync();
}
```

---

### Step 6: Verify Seed Data

Run these queries to verify data was seeded correctly:

```sql
-- Provider Availability Statistics
SELECT
    "Status",
    COUNT(*) as count,
    ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 2) as percentage
FROM "ProviderAvailability"
GROUP BY "Status"
ORDER BY count DESC;

-- Expected output:
-- Available: ~40-50%
-- Booked: ~35-45%
-- Blocked: ~5-10%

-- Review Statistics
SELECT
    CASE
        WHEN "RatingValue" >= 4.5 THEN 'Excellent (4.5-5.0)'
        WHEN "RatingValue" >= 3.5 THEN 'Good (3.5-4.4)'
        WHEN "RatingValue" >= 2.5 THEN 'Average (2.5-3.4)'
        ELSE 'Poor (<2.5)'
    END as rating_category,
    COUNT(*) as count,
    ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 2) as percentage
FROM "Reviews"
GROUP BY rating_category
ORDER BY count DESC;

-- Expected distribution:
-- Excellent: ~50%
-- Good: ~25%
-- Average: ~15%
-- Poor: ~10%

-- Verify review conversion rate
SELECT
    (SELECT COUNT(*) FROM "Reviews") as review_count,
    (SELECT COUNT(*) FROM "Bookings" WHERE "Status" = 4) as completed_booking_count,
    ROUND(
        (SELECT COUNT(*) FROM "Reviews")::numeric /
        (SELECT COUNT(*) FROM "Bookings" WHERE "Status" = 4) * 100,
        2
    ) as conversion_rate_percentage;

-- Expected conversion rate: ~60%
```

---

## Expected Data Volumes

Based on seeder configuration:

| Entity | Estimated Count | Notes |
|--------|----------------|-------|
| **ProviderAvailability** | 25,000-40,000 | 90 days × 20 providers × 15-25 slots/day |
| **Reviews** | 150-300 | 60% of ~250-500 completed bookings |

---

## Performance Considerations

### Index Usage:

**ProviderAvailability:**
- `IX_ProviderAvailability_Provider_Date_StartTime`: Used for availability queries
- `IX_ProviderAvailability_Date_Status`: Used for calendar heatmaps
- `IX_ProviderAvailability_HoldExpiration_Status`: Used for hold cleanup jobs

**Reviews:**
- `IX_Reviews_BookingId`: Ensures one review per booking
- `IX_Reviews_Provider_Rating`: Used for provider rating aggregations
- `IX_Reviews_Provider_CreatedAt`: Used for recent reviews display

### Query Performance Expectations:

```sql
-- Provider availability for specific date (should use index)
EXPLAIN ANALYZE
SELECT * FROM "ProviderAvailability"
WHERE "ProviderId" = 'some-guid'
  AND "Date" = '2025-11-20'
ORDER BY "StartTime";

-- Expected: Index Scan using IX_ProviderAvailability_Provider_Date_StartTime
-- Expected execution time: <10ms

-- Provider reviews sorted by date (should use index)
EXPLAIN ANALYZE
SELECT * FROM "Reviews"
WHERE "ProviderId" = 'some-guid'
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- Expected: Index Scan using IX_Reviews_Provider_CreatedAt
-- Expected execution time: <5ms
```

---

## Rollback Instructions

If migrations need to be rolled back:

```bash
# Revert to previous migration
dotnet ef database update <PreviousMigrationName> \
  --context ServiceCatalogDbContext \
  --project Booksy.ServiceCatalog.Infrastructure.csproj \
  --startup-project ../../../Booksy.API/Booksy.API.csproj

# Remove migration files
dotnet ef migrations remove \
  --context ServiceCatalogDbContext \
  --project Booksy.ServiceCatalog.Infrastructure.csproj \
  --startup-project ../../../Booksy.API/Booksy.API.csproj
```

---

## Troubleshooting

### Issue: Migration generation fails with "No DbContext was found"

**Solution:**
```bash
# Ensure you're in the correct directory
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

# Verify startup project path
dotnet ef migrations add TestMigration \
  --context ServiceCatalogDbContext \
  --startup-project ../../../Booksy.API/Booksy.API.csproj \
  --verbose
```

### Issue: "A connection could not be established" during migration

**Solution:**
- Verify PostgreSQL is running
- Check connection string in `appsettings.Development.json`
- Ensure database exists (create if needed)

```bash
# Create database if it doesn't exist
psql -U postgres -c "CREATE DATABASE booksy_dev;"
```

### Issue: Seeder fails with foreign key constraint errors

**Solution:**
- Ensure all prerequisite seeders have run first
- Check orchestrator seeder order (AvailabilitySeeder after BusinessHoursSeeder, ReviewSeeder after BookingSeeder)
- Verify existing data wasn't manually modified

---

## Next Steps (Week 3-4)

After migrations are applied and data is seeded:

1. **Build Provider Availability API** (`GET /api/v1/providers/{id}/availability`)
2. **Build Booking Creation API** (`POST /api/v1/bookings`)
3. **Build Review APIs** (`GET/POST /api/v1/reviews`)
4. **Implement caching strategy** (Redis for availability heatmaps)
5. **Add integration tests** for new endpoints

---

## Sign-Off Checklist

- [ ] Migrations generated successfully
- [ ] Migration files reviewed for correctness
- [ ] Development database updated
- [ ] Tables and indexes verified in database
- [ ] Seed data executed successfully
- [ ] Data volumes match expectations
- [ ] Query performance tested with EXPLAIN ANALYZE
- [ ] Staging database ready for Week 3 API development

---

**Last Updated:** 2025-11-15
**Next Review:** Week 3 Kickoff (API Development)
