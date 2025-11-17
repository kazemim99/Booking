# Database Schema Updates - ServiceCatalog

**Date**: November 16, 2025
**Migration**: `20251115202010_InitialCreate`
**Status**: ✅ Applied Successfully

## Overview

This document details the database schema updates for the ServiceCatalog bounded context, including new tables for provider availability management and customer review systems.

---

## New Tables

### 1. ProviderAvailability Table

**Purpose**: Manages time slot availability for service providers, enabling booking calendar functionality and availability queries.

#### Schema

```sql
CREATE TABLE "ServiceCatalog"."ProviderAvailability" (
    "AvailabilityId" uuid NOT NULL,
    "ProviderId" uuid NOT NULL,
    "StaffId" uuid NULL,
    "Date" date NOT NULL,
    "StartTime" time NOT NULL,
    "EndTime" time NOT NULL,
    "Status" character varying(50) NOT NULL,
    "BookingId" uuid NULL,
    "BlockReason" character varying(500) NULL,
    "HoldExpiresAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(100) NULL,
    "LastModifiedAt" timestamp with time zone NULL,
    "LastModifiedBy" character varying(100) NULL,
    "IsDeleted" boolean NOT NULL,
    "Version" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_ProviderAvailability" PRIMARY KEY ("AvailabilityId")
);
```

#### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| AvailabilityId | uuid | No | Primary key, unique identifier for availability slot |
| ProviderId | uuid | No | Reference to provider (foreign key to Providers table) |
| StaffId | uuid | Yes | Optional staff member assigned to this slot |
| Date | date | No | Date of the availability slot |
| StartTime | time | No | Start time of the slot (time only, no timezone) |
| EndTime | time | No | End time of the slot (time only, no timezone) |
| Status | varchar(50) | No | Availability status (see Status Enum below) |
| BookingId | uuid | Yes | Reference to booking if status is Booked |
| BlockReason | varchar(500) | Yes | Reason for blocking if status is Blocked |
| HoldExpiresAt | timestamptz | Yes | Expiration time for tentative holds |
| CreatedAt | timestamptz | No | Record creation timestamp (UTC) |
| CreatedBy | varchar(100) | Yes | User who created the record |
| LastModifiedAt | timestamptz | Yes | Last modification timestamp (UTC) |
| LastModifiedBy | varchar(100) | Yes | User who last modified the record |
| IsDeleted | boolean | No | Soft delete flag |
| Version | integer | No | Optimistic concurrency version (default: 0) |

#### Status Enum Values

```csharp
public enum AvailabilityStatus
{
    Available,      // Slot is open for booking
    Booked,        // Slot is booked (BookingId populated)
    Blocked,       // Slot is blocked by provider (BlockReason populated)
    TentativeHold, // Temporary hold during booking process (HoldExpiresAt populated)
    Break          // Provider break period
}
```

#### Indexes

```sql
-- Primary lookup by booking
CREATE INDEX "IX_ProviderAvailability_BookingId"
ON "ServiceCatalog"."ProviderAvailability" ("BookingId");

-- Date and status queries
CREATE INDEX "IX_ProviderAvailability_Date_Status"
ON "ServiceCatalog"."ProviderAvailability" ("Date", "Status");

-- Expired hold cleanup (filtered index)
CREATE INDEX "IX_ProviderAvailability_HoldExpiration_Status"
ON "ServiceCatalog"."ProviderAvailability" ("HoldExpiresAt", "Status")
WHERE "HoldExpiresAt" IS NOT NULL;

-- Provider calendar queries (most important)
CREATE INDEX "IX_ProviderAvailability_Provider_Date_StartTime"
ON "ServiceCatalog"."ProviderAvailability" ("ProviderId", "Date", "StartTime");
```

#### Use Cases

1. **Calendar Display**: Fetch all slots for a provider within a date range
2. **Availability Heatmap**: Calculate availability statistics for UI visualization
3. **Booking Slot Reservation**: Mark slots as tentatively held during checkout
4. **Slot Management**: Block/unblock time periods for breaks, vacations, etc.
5. **Expired Hold Cleanup**: Background job to release expired tentative holds

#### Example Queries

```sql
-- Get available slots for provider in next 7 days
SELECT * FROM "ServiceCatalog"."ProviderAvailability"
WHERE "ProviderId" = '...'
  AND "Date" BETWEEN CURRENT_DATE AND CURRENT_DATE + INTERVAL '7 days'
  AND "Status" = 'Available'
  AND "IsDeleted" = false
ORDER BY "Date", "StartTime";

-- Get availability statistics for heatmap
SELECT
    DATE_TRUNC('day', "Date") as day,
    COUNT(*) FILTER (WHERE "Status" = 'Available') as available_count,
    COUNT(*) FILTER (WHERE "Status" = 'Booked') as booked_count,
    COUNT(*) FILTER (WHERE "Status" = 'Blocked') as blocked_count
FROM "ServiceCatalog"."ProviderAvailability"
WHERE "ProviderId" = '...'
  AND "Date" BETWEEN '2025-01-01' AND '2025-01-31'
  AND "IsDeleted" = false
GROUP BY DATE_TRUNC('day', "Date")
ORDER BY day;
```

---

### 2. Reviews Table

**Purpose**: Stores customer reviews and ratings for service providers, including helpful voting and provider responses.

#### Schema

```sql
CREATE TABLE "ServiceCatalog"."Reviews" (
    "ReviewId" uuid NOT NULL,
    "ProviderId" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "RatingValue" numeric(3,1) NOT NULL,
    "Comment" character varying(2000) NULL,
    "IsVerified" boolean NOT NULL DEFAULT true,
    "ProviderResponse" character varying(1000) NULL,
    "ProviderResponseAt" timestamp with time zone NULL,
    "HelpfulCount" integer NOT NULL DEFAULT 0,
    "NotHelpfulCount" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(100) NULL,
    "LastModifiedAt" timestamp with time zone NULL,
    "LastModifiedBy" character varying(100) NULL,
    "IsDeleted" boolean NOT NULL,
    "Version" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_Reviews" PRIMARY KEY ("ReviewId")
);
```

#### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| ReviewId | uuid | No | Primary key, unique identifier for review |
| ProviderId | uuid | No | Reference to reviewed provider |
| CustomerId | uuid | No | Reference to customer who wrote review |
| BookingId | uuid | No | Reference to booking (ensures verified reviews) |
| RatingValue | numeric(3,1) | No | Rating value 1.0-5.0 (e.g., 4.5) |
| Comment | varchar(2000) | Yes | Review text/comment (optional) |
| IsVerified | boolean | No | Verified review flag (default: true) |
| ProviderResponse | varchar(1000) | Yes | Provider's response to review |
| ProviderResponseAt | timestamptz | Yes | Timestamp when provider responded |
| HelpfulCount | integer | No | Number of "helpful" votes (default: 0) |
| NotHelpfulCount | integer | No | Number of "not helpful" votes (default: 0) |
| CreatedAt | timestamptz | No | Review creation timestamp (UTC) |
| CreatedBy | varchar(100) | Yes | User who created the record |
| LastModifiedAt | timestamptz | Yes | Last modification timestamp (UTC) |
| LastModifiedBy | varchar(100) | Yes | User who last modified the record |
| IsDeleted | boolean | No | Soft delete flag |
| Version | integer | No | Optimistic concurrency version (default: 0) |

#### Rating Values

- **Range**: 1.0 to 5.0
- **Precision**: 0.5 increments (half-star ratings)
- **Examples**: 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0

#### Indexes

```sql
-- One review per booking constraint
CREATE UNIQUE INDEX "IX_Reviews_BookingId"
ON "ServiceCatalog"."Reviews" ("BookingId");

-- Customer reviews lookup
CREATE INDEX "IX_Reviews_CustomerId"
ON "ServiceCatalog"."Reviews" ("CustomerId");

-- Provider reviews by creation date
CREATE INDEX "IX_Reviews_Provider_CreatedAt"
ON "ServiceCatalog"."Reviews" ("ProviderId", "CreatedAt");

-- Provider reviews by rating
CREATE INDEX "IX_Reviews_Provider_Rating"
ON "ServiceCatalog"."Reviews" ("ProviderId", "RatingValue");

-- Provider reviews lookup (non-unique)
CREATE INDEX "IX_Reviews_ProviderId"
ON "ServiceCatalog"."Reviews" ("ProviderId");

-- Verified reviews sorting
CREATE INDEX "IX_Reviews_Verified_CreatedAt"
ON "ServiceCatalog"."Reviews" ("IsVerified", "CreatedAt");
```

#### Use Cases

1. **Provider Rating Display**: Calculate average rating for provider profile
2. **Review List**: Display recent reviews for a provider
3. **Customer Reviews**: Show reviews written by a specific customer
4. **Verified Reviews**: Filter only verified reviews (tied to actual bookings)
5. **Provider Response**: Allow providers to respond to customer feedback
6. **Helpful Voting**: Let users vote on review helpfulness

#### Example Queries

```sql
-- Get provider's average rating
SELECT
    AVG("RatingValue") as average_rating,
    COUNT(*) as total_reviews,
    COUNT(*) FILTER (WHERE "RatingValue" >= 4.0) as positive_reviews
FROM "ServiceCatalog"."Reviews"
WHERE "ProviderId" = '...'
  AND "IsDeleted" = false;

-- Get recent reviews for provider
SELECT * FROM "ServiceCatalog"."Reviews"
WHERE "ProviderId" = '...'
  AND "IsDeleted" = false
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- Get reviews needing provider response
SELECT * FROM "ServiceCatalog"."Reviews"
WHERE "ProviderId" = '...'
  AND "ProviderResponse" IS NULL
  AND "RatingValue" < 3.0  -- Prioritize negative reviews
  AND "IsDeleted" = false
ORDER BY "CreatedAt" ASC;
```

---

## Modified Tables

### Payments Table

**Change**: Column type adjustment for PostgreSQL compatibility

```sql
-- Before (SQL Server style)
"Provider" nvarchar(50) NOT NULL

-- After (PostgreSQL style)
"Provider" character varying(50) NOT NULL
```

**Reason**: Ensures proper PostgreSQL data types across the schema.

---

## Migration History

### Applied Migrations

| Migration | Date | Description |
|-----------|------|-------------|
| 20251110114907_Init | 2025-11-10 | Initial database schema |
| 20251111035705_ModifyServiceOption | 2025-11-11 | Service option modifications |
| 20251112175221_AddOwnerName | 2025-11-12 | Added owner name field |
| 20251115202010_InitialCreate | 2025-11-15 | ✨ **Added ProviderAvailability & Reviews** |

### Removed Migrations

| Migration | Reason |
|-----------|--------|
| 20251109070253_AddBookingSystem2 | ❌ Broken - tried to alter non-existent table |

---

## Database Commands

### Apply Migrations

```bash
# From Infrastructure project
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

# Apply all pending migrations
dotnet ef database update \
  --startup-project ../Booksy.ServiceCatalog.Api \
  --context ServiceCatalogDbContext
```

### Create New Migration

```bash
# From API project
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api

# Create migration
dotnet ef migrations add MigrationName \
  --project ../Booksy.ServiceCatalog.Infrastructure \
  --context ServiceCatalogDbContext \
  --output-dir Migrations
```

### List Migrations

```bash
# Check migration status
dotnet ef migrations list \
  --project ../Booksy.ServiceCatalog.Infrastructure \
  --context ServiceCatalogDbContext
```

### Rollback Migration

```bash
# Rollback to specific migration
dotnet ef database update PreviousMigrationName \
  --startup-project ../Booksy.ServiceCatalog.Api \
  --context ServiceCatalogDbContext

# Rollback all migrations
dotnet ef database update 0 \
  --startup-project ../Booksy.ServiceCatalog.Api \
  --context ServiceCatalogDbContext
```

---

## Performance Considerations

### Index Strategy

1. **ProviderAvailability**: Composite index on (ProviderId, Date, StartTime) covers most calendar queries
2. **Reviews**: Separate indexes for provider lookups, customer lookups, and rating-based queries
3. **Filtered Index**: Hold expiration index only includes rows with non-null expiration times

### Query Optimization

1. Always include `IsDeleted = false` filter to leverage soft delete pattern
2. Use date range queries with proper indexes
3. Consider caching frequently accessed aggregations (average ratings)

### Scalability

1. **Partitioning**: Consider date-based partitioning for ProviderAvailability as data grows
2. **Archiving**: Archive old availability slots after booking completion
3. **Read Replicas**: Use read replicas for review aggregation queries

---

## Data Integrity

### Constraints

1. **Unique Constraint**: One review per booking (IX_Reviews_BookingId)
2. **Check Constraints** (enforced in domain):
   - Rating value between 1.0 and 5.0
   - StartTime < EndTime for availability slots
   - Slot duration >= 15 minutes, <= 8 hours

### Referential Integrity

While foreign keys aren't explicitly defined in migrations (microservices pattern), the following logical relationships exist:

- `ProviderAvailability.ProviderId` → `Providers.ProviderId`
- `ProviderAvailability.BookingId` → `Bookings.BookingId`
- `Reviews.ProviderId` → `Providers.ProviderId`
- `Reviews.CustomerId` → External UserManagement context
- `Reviews.BookingId` → `Bookings.BookingId`

---

## Seed Data

### Availability Seeder

The `AvailabilitySeeder` generates realistic availability data:

- 90-day rolling window
- Respects business hours and Iranian holidays
- Realistic availability patterns (peak hours 30% available, off-peak 70%)
- 30-minute time slots
- Occasional full-day blocks (5% chance)

### Review Seeder

The `ReviewSeeder` generates authentic Persian reviews:

- 60% of completed bookings receive reviews
- Rating distribution: 50% excellent, 25% good, 15% average, 10% poor
- Persian language comments for cultural authenticity
- Provider responses (30% of all reviews, 70% of negative reviews)
- Helpful voting based on rating quality

---

## References

- [PostgreSQL Data Types](https://www.postgresql.org/docs/current/datatype.html)
- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Database Indexing Best Practices](https://use-the-index-luke.com/)
