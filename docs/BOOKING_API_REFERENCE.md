# Booking API Reference - Updated for Hierarchy Architecture

**Last Updated**: December 8, 2025
**API Version**: v1

## Create Booking

Creates a new booking request.

### Endpoint
```
POST /api/v1/bookings
```

### Authentication
Required: `Bearer Token`

### Request Body

```json
{
  "providerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "serviceId": "8b4c6f2a-1234-4567-89ab-cdef01234567",
  "staffProviderId": "9d5e7f3b-5678-4321-abcd-ef0123456789",
  "startTime": "2025-01-15T10:00:00Z",
  "customerNotes": "Please arrive 10 minutes early"
}
```

### Request Parameters

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `providerId` | `Guid` | ✅ Yes | Organization provider ID |
| `serviceId` | `Guid` | ✅ Yes | Service to be booked |
| `staffProviderId` | `Guid` | ✅ Yes | **Individual provider (staff) who will perform the service** |
| `startTime` | `DateTime` | ✅ Yes | Booking start time (ISO 8601 format) |
| `customerNotes` | `string` | ❌ No | Customer notes or special requests (max 1000 chars) |

⚠️ **Important**: `staffProviderId` must be an individual provider with `ParentProviderId` equal to the `providerId`.

### Response

**Status**: `201 Created`
**Location**: `/api/v1/bookings/{bookingId}`

```json
{
  "id": "7c8d9e0f-4567-4321-bcde-f12345678901",
  "customerId": "2b3c4d5e-1234-5678-90ab-cdef12345678",
  "providerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "serviceId": "8b4c6f2a-1234-4567-89ab-cdef01234567",
  "staffProviderId": "9d5e7f3b-5678-4321-abcd-ef0123456789",
  "status": "Pending",
  "startTime": "2025-01-15T10:00:00Z",
  "endTime": "2025-01-15T11:00:00Z",
  "durationMinutes": 60,
  "totalPrice": 500000,
  "currency": "IRR",
  "paymentStatus": "Pending",
  "createdAt": "2025-01-10T08:30:00Z"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Unique booking identifier |
| `customerId` | `Guid` | Customer ID (from auth context) |
| `providerId` | `Guid` | Organization provider ID |
| `serviceId` | `Guid` | Service ID |
| `staffProviderId` | `Guid` | **Staff provider ID (individual provider)** |
| `status` | `string` | Booking status: `Pending`, `Confirmed`, `Completed`, `Cancelled`, `NoShow` |
| `startTime` | `DateTime` | Booking start time |
| `endTime` | `DateTime` | Booking end time (calculated from service duration) |
| `durationMinutes` | `int` | Service duration in minutes |
| `totalPrice` | `decimal` | Total price in smallest currency unit |
| `currency` | `string` | Currency code (e.g., "IRR") |
| `paymentStatus` | `string` | Payment status |
| `createdAt` | `DateTime` | Booking creation timestamp |

### Error Responses

#### 400 Bad Request - Validation Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "StaffProviderId": [
      "The StaffProviderId field is required."
    ]
  }
}
```

#### 404 Not Found - Provider/Service/Staff Not Found
```json
{
  "error": "NotFoundException",
  "message": "Staff provider with ID 9d5e7f3b-5678-4321-abcd-ef0123456789 not found"
}
```

#### 409 Conflict - Invalid Hierarchy
```json
{
  "error": "ConflictException",
  "message": "Staff provider does not belong to the specified organization"
}
```

#### 409 Conflict - Inactive Staff Provider
```json
{
  "error": "ConflictException",
  "message": "Staff provider John Doe is not currently active"
}
```

#### 409 Conflict - Time Slot Conflict
```json
{
  "error": "ConflictException",
  "message": "This time slot conflicts with an existing booking"
}
```

#### 409 Conflict - Booking Validation Failed
```json
{
  "error": "ConflictException",
  "message": "Booking validation failed: Provider is not accepting bookings, Outside business hours"
}
```

#### 401 Unauthorized
```json
{
  "error": "Unauthorized",
  "message": "Authentication required"
}
```

## Get Booking Details

Retrieves detailed information about a specific booking.

### Endpoint
```
GET /api/v1/bookings/{id}
```

### Response
```json
{
  "id": "7c8d9e0f-4567-4321-bcde-f12345678901",
  "customerId": "2b3c4d5e-1234-5678-90ab-cdef12345678",
  "providerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "serviceId": "8b4c6f2a-1234-4567-89ab-cdef01234567",
  "staffProviderId": "9d5e7f3b-5678-4321-abcd-ef0123456789",
  "serviceName": "مو کوتاهی مردانه",
  "serviceCategory": "HairCare",
  "providerBusinessName": "آرایشگاه مردانه تهران",
  "providerCity": "تهران",
  "staffName": "احمد محمدی",
  "startTime": "2025-01-15T10:00:00Z",
  "endTime": "2025-01-15T11:00:00Z",
  "durationMinutes": 60,
  "status": "Confirmed",
  "paymentStatus": "Paid",
  "paymentInfo": {
    "totalAmount": 500000,
    "currency": "IRR",
    "depositAmount": 100000,
    "paidAmount": 500000,
    "refundedAmount": 0,
    "remainingAmount": 0,
    "paymentStatus": "Paid"
  },
  "customerNotes": "Please arrive 10 minutes early",
  "staffNotes": null,
  "createdAt": "2025-01-10T08:30:00Z",
  "confirmedAt": "2025-01-10T09:00:00Z",
  "completedAt": null,
  "cancelledAt": null
}
```

## Get Available Slots

Get available time slots for booking a service with a specific staff provider.

### Endpoint
```
GET /api/v1/bookings/available-slots
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `providerId` | `Guid` | ✅ Yes | Organization provider ID |
| `serviceId` | `Guid` | ✅ Yes | Service ID |
| `date` | `DateTime` | ✅ Yes | Date to check availability (YYYY-MM-DD) |
| `staffId` | `Guid` | ❌ No | **Filter by specific staff provider** |

### Example Request
```
GET /api/v1/bookings/available-slots?providerId=3fa85f64-5717-4562-b3fc-2c963f66afa6&serviceId=8b4c6f2a-1234-4567-89ab-cdef01234567&date=2025-01-15&staffId=9d5e7f3b-5678-4321-abcd-ef0123456789
```

### Response
```json
{
  "providerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "serviceId": "8b4c6f2a-1234-4567-89ab-cdef01234567",
  "date": "2025-01-15T00:00:00Z",
  "availableSlots": [
    {
      "startTime": "2025-01-15T09:00:00Z",
      "endTime": "2025-01-15T10:00:00Z",
      "staffProviderId": "9d5e7f3b-5678-4321-abcd-ef0123456789",
      "staffName": "احمد محمدی",
      "available": true
    },
    {
      "startTime": "2025-01-15T10:00:00Z",
      "endTime": "2025-01-15T11:00:00Z",
      "staffProviderId": "9d5e7f3b-5678-4321-abcd-ef0123456789",
      "staffName": "احمد محمدی",
      "available": true
    }
  ],
  "totalSlots": 2
}
```

## Migration Notes

### Breaking Changes from Previous Version

#### Field Rename: `staffId` → `staffProviderId`

**Affected Endpoints:**
- `POST /api/v1/bookings` - Request body
- `GET /api/v1/bookings/{id}` - Response
- `GET /api/v1/bookings/my-bookings` - Response items
- `GET /api/v1/bookings/provider/{providerId}` - Response items
- `GET /api/v1/bookings/search` - Response items

**Migration Guide:**

Before (Old):
```json
{
  "providerId": "...",
  "serviceId": "...",
  "staffId": "...",  // ❌ Old field
  "startTime": "..."
}
```

After (New):
```json
{
  "providerId": "...",
  "serviceId": "...",
  "staffProviderId": "...",  // ✅ New field
  "startTime": "..."
}
```

**Frontend Update Required:**

```typescript
// Before
const request = {
  providerId: '...',
  serviceId: '...',
  staffId: selectedStaff.id,  // ❌ Old
  startTime: '...'
}

// After
const request = {
  providerId: '...',
  serviceId: '...',
  staffProviderId: selectedStaff.id,  // ✅ New
  startTime: '...'
}
```

## Validation Rules

### Hierarchy Validation
1. **Staff Provider Exists**: Must be a valid provider in the system
2. **Parent Relationship**: `staffProviderId` must have `ParentProviderId` equal to `providerId`
3. **Active Status**: Staff provider must have status `Active`
4. **Service Ownership**: Service must belong to the organization provider

### Business Rules
1. **Future Booking**: Start time must be in the future
2. **Business Hours**: Must be within provider's business hours
3. **No Conflicts**: No overlapping bookings for the staff provider
4. **Advance Booking**: Must meet minimum advance booking hours
5. **Max Advance**: Cannot exceed maximum advance booking days
6. **Provider Status**: Organization provider must be active and accepting bookings

## Example: Complete Booking Flow

### Step 1: Get Available Slots
```bash
GET /api/v1/bookings/available-slots?providerId=3fa85f64-5717-4562-b3fc-2c963f66afa6&serviceId=8b4c6f2a-1234-4567-89ab-cdef01234567&date=2025-01-15
```

### Step 2: Select a Slot and Create Booking
```bash
POST /api/v1/bookings
Content-Type: application/json
Authorization: Bearer {token}

{
  "providerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "serviceId": "8b4c6f2a-1234-4567-89ab-cdef01234567",
  "staffProviderId": "9d5e7f3b-5678-4321-abcd-ef0123456789",
  "startTime": "2025-01-15T10:00:00Z",
  "customerNotes": "Please arrive 10 minutes early"
}
```

### Step 3: Get Booking Details
```bash
GET /api/v1/bookings/7c8d9e0f-4567-4321-bcde-f12345678901
Authorization: Bearer {token}
```

## Testing with cURL

### Create Booking
```bash
curl -X POST "https://api.booksy.com/api/v1/bookings" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "providerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "serviceId": "8b4c6f2a-1234-4567-89ab-cdef01234567",
    "staffProviderId": "9d5e7f3b-5678-4321-abcd-ef0123456789",
    "startTime": "2025-01-15T10:00:00Z",
    "customerNotes": "Test booking"
  }'
```

## See Also

- [BOOKING_HIERARCHY_MIGRATION.md](./BOOKING_HIERARCHY_MIGRATION.md) - Detailed migration guide
- [HIERARCHY_MIGRATION_README.md](./HIERARCHY_MIGRATION_README.md) - Overall hierarchy architecture
- [API_DOCUMENTATION.md](./API_DOCUMENTATION.md) - Complete API reference

---

**Last Updated**: December 8, 2025
**API Version**: v1
**Architecture**: Hierarchy-Based
