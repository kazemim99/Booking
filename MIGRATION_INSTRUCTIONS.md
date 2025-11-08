# Database Migration Instructions for Memento Pattern Implementation

## Overview
This implementation adds the Memento pattern to capture booking state snapshots for complete audit trail and state restoration capabilities.

## Migration Command

Run the following command from the repository root to create and apply the migration:

```bash
dotnet ef migrations add AddBookingHistorySnapshots \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --startup-project src/API/Booksy.API \
  --context ServiceCatalogDbContext \
  --output-dir Persistence/Migrations
```

## Apply Migration

```bash
dotnet ef database update \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --startup-project src/API/Booksy.API \
  --context ServiceCatalogDbContext
```

## What This Migration Creates

The migration will create the `BookingHistorySnapshots` table in the `ServiceCatalog` schema with the following structure:

### Table: ServiceCatalog.BookingHistorySnapshots

| Column | Type | Description |
|--------|------|-------------|
| Id | uuid (Guid) | Primary key |
| BookingId | uuid (Guid) | Foreign key to Bookings table |
| StateId | uuid (Guid) | Unique identifier for the state snapshot |
| StateName | varchar(200) | Descriptive name (e.g., "Before Confirm - Requested") |
| StateJson | text | JSON-serialized booking state |
| CreatedAt | timestamp with time zone | When the snapshot was created |
| TriggeredBy | varchar(100) | User who triggered the state change |
| Description | varchar(500) | Optional description |

### Indexes

- `IX_BookingHistorySnapshots_BookingId` - For querying snapshots by booking
- `IX_BookingHistorySnapshots_BookingId_StateId` - Unique constraint
- `IX_BookingHistorySnapshots_CreatedAt` - For temporal queries

## New API Endpoints

After applying the migration, the following endpoints will be available:

1. **GET** `/api/v1/bookings/{id}/history`
   - Returns all state snapshots for a booking
   - Requires authentication

2. **GET** `/api/v1/bookings/{id}/audit-trail`
   - Returns detailed audit trail with parsed state information
   - Requires authentication

3. **POST** `/api/v1/bookings/{id}/restore/{stateId}`
   - Restores booking to a previous state
   - Requires Admin role
   - Request body: `{ "reason": "explanation for restore" }`

## Features Enabled

- ✅ Complete audit trail for all booking state changes
- ✅ Automatic snapshot creation before state transitions (Confirm, Cancel, Reschedule, Complete)
- ✅ Admin capability to restore bookings to previous states
- ✅ Immutable state snapshots stored in database
- ✅ GOF Memento pattern implementation
- ✅ Domain event: `BookingRestoredFromHistoryEvent`

## Testing the Implementation

### 1. Create a Booking
```bash
POST /api/v1/bookings
```

### 2. Confirm the Booking
```bash
POST /api/v1/bookings/{id}/confirm
```

### 3. View History
```bash
GET /api/v1/bookings/{id}/history
```

You should see snapshots for both the initial state and before-confirm state.

### 4. Restore to Previous State (Admin Only)
```bash
POST /api/v1/bookings/{id}/restore/{stateId}
{
  "reason": "Customer requested rollback"
}
```

## Rollback

If you need to rollback this migration:

```bash
dotnet ef database update <PreviousMigrationName> \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --startup-project src/API/Booksy.API \
  --context ServiceCatalogDbContext
```

Then remove the migration:

```bash
dotnet ef migrations remove \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --startup-project src/API/Booksy.API \
  --context ServiceCatalogDbContext
```
