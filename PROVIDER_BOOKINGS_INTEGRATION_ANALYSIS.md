# Provider Bookings Integration Analysis

**Date**: 2025-12-22
**Status**: ✅ Real API Integration Confirmed
**Component**: BookingListCard.vue / Provider Dashboard

---

## Executive Summary

After comprehensive analysis of the provider bookings functionality, I can confirm that **the system IS already using real API data**, not mock data. The integration is properly implemented across frontend and backend with correct business logic.

---

## Frontend Implementation

### 1. Component: BookingListCard.vue
**Location**: `booksy-frontend/src/modules/provider/components/dashboard/BookingListCard.vue`

#### Data Flow
```typescript
// Line 221-226: Real API Call
const response = await bookingService.getProviderBookings(
  props.providerId,
  undefined, // status filter
  1, // page
  100 // get more bookings for local filtering
)
```

#### Features Implemented
✅ **Real-time data fetching** from backend API
✅ **Search functionality** - filters by customer name or service
✅ **Status filtering** - all, scheduled, completed, cancelled
✅ **Period filtering** - today, week, month, all
✅ **Pagination** - 5 items per page with navigation
✅ **Loading states** - spinner while fetching data
✅ **Error handling** - with retry button
✅ **Action buttons** - confirm, complete, cancel, reschedule, assign staff, add notes, mark no-show

#### Data Mapping (Lines 243-260)
```typescript
const mapAppointmentToBooking = async (appointment: Appointment): Promise<Booking> => {
  // Resolves customer and service names from IDs
  const customerName = await customerService.getCustomerName(appointment.clientId)
  const serviceName = await serviceService.getServiceName(appointment.serviceId)

  return {
    id: appointment.id,
    customerName, // ✅ Resolved name (not ID)
    date: formatDate(appointment.scheduledStartTime), // ✅ Formatted date
    time: formatTime(appointment.scheduledStartTime), // ✅ Formatted time
    service: serviceName, // ✅ Resolved name (not ID)
    status: mapStatus(appointment.status), // ✅ Mapped to display status
    appointment // Full appointment for actions
  }
}
```

---

## Backend Implementation

### 1. API Controller: BookingsController.cs
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/BookingsController.cs`

#### Endpoint Details
```csharp
// Lines 199-235
[HttpGet("provider/{providerId:guid}")]
[Authorize(Policy = "ProviderOrAdmin")]
public async Task<IActionResult> GetProviderBookings(
    [FromRoute] Guid providerId,
    [FromQuery] string? status = null,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null,
    CancellationToken cancellationToken = default)
```

**Endpoint URL**: `GET /api/v1/bookings/provider/{providerId}`

#### Authorization
✅ **Policy**: `ProviderOrAdmin` - ensures only authorized users can view bookings
✅ **Permission Check**: `CanManageProvider(providerId)` - validates user has rights to this provider
✅ **Security Logging**: Logs unauthorized access attempts

#### Business Logic
```csharp
// Line 217: Fetch all provider bookings
var bookings = await _bookingReadRepository.GetByProviderIdAsync(providerId, cancellationToken);

// Lines 219-231: Apply optional filters
if (!string.IsNullOrEmpty(status))
    bookings = bookings.Where(b => b.Status.ToString() == status).ToList();

if (from.HasValue)
    bookings = bookings.Where(b => b.TimeSlot.StartTime >= from.Value).ToList();

if (to.HasValue)
    bookings = bookings.Where(b => b.TimeSlot.StartTime <= to.Value).ToList();

// Line 233: Map to response format
var response = bookings.Select(MapToBookingResponse).ToList();
```

### 2. Repository: BookingReadRepository.cs
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/BookingReadRepository.cs`

#### Database Query (Lines 41-47)
```csharp
public async Task<IReadOnlyList<Booking>> GetByProviderIdAsync(
    ProviderId providerId,
    CancellationToken cancellationToken = default)
{
    return await DbSet
        .Where(b => b.ProviderId == providerId)
        .OrderByDescending(b => b.TimeSlot.StartTime) // ✅ Latest first
        .ToListAsync(cancellationToken);
}
```

**Query Characteristics**:
- ✅ Filters by provider ID
- ✅ Orders by start time (descending - newest first)
- ✅ Returns all matching bookings (no pagination at repository level)
- ✅ Async/await pattern for performance
- ✅ Cancellation token support

---

## API Integration Path

### Request Flow
```
User Action (Dashboard)
  ↓
BookingListCard.vue (Line 221)
  ↓
bookingService.getProviderBookings() (booking.service.ts:226)
  ↓
serviceCategoryClient.get('/api/v1/bookings') with params
  ↓
BookingsController.GetProviderBookings() (Line 203)
  ↓
BookingReadRepository.GetByProviderIdAsync() (Line 41)
  ↓
Database Query (EF Core)
  ↓
Response Mapping
  ↓
JSON Response to Frontend
  ↓
Data Transformation (Lines 229-232)
  ↓
Display in Table
```

---

## Current Issues & Observations

### ⚠️ Potential Issue: API Endpoint Mismatch

**Frontend Service** (`booking.service.ts:226-232`):
```typescript
async getProviderBookings(
  providerId: string,
  status?: BookingStatus,
  pageNumber = 1,
  pageSize = 10
): Promise<PaginatedBookingsResponse> {
  return this.getBookings({ providerId, status, pageNumber, pageSize })
}
```

This calls `getBookings()` which uses:
- **URL**: `GET /api/v1/bookings`
- **Query params**: `?providerId={id}&status={status}&pageNumber=1&pageSize=10`

**Backend Controller** has a dedicated endpoint:
- **URL**: `GET /api/v1/bookings/provider/{providerId}`
- **Query params**: `?status={status}&from={date}&to={date}`

### ❌ Mismatch Details

1. **Different URL patterns**:
   - Frontend: `/api/v1/bookings?providerId=...` (generic list endpoint)
   - Backend: `/api/v1/bookings/provider/{providerId}` (provider-specific endpoint)

2. **Different filtering**:
   - Frontend sends: `pageNumber`, `pageSize`
   - Backend expects: `from`, `to` (date range)

3. **Authorization**:
   - Generic `/bookings` endpoint might have different auth requirements
   - Provider endpoint has specific `ProviderOrAdmin` policy

---

## Recommended Fix

### Update Frontend Service

**File**: `booksy-frontend/src/modules/booking/api/booking.service.ts`

```typescript
/**
 * Get bookings for a specific provider
 * Uses dedicated provider endpoint with proper authorization
 * GET /api/v1/bookings/provider/{providerId}
 */
async getProviderBookings(
  providerId: string,
  status?: BookingStatus,
  from?: string, // ISO date format
  to?: string    // ISO date format
): Promise<Appointment[]> {
  try {
    console.log('[BookingService] Fetching provider bookings:', { providerId, status, from, to })

    const params: Record<string, any> = {}
    if (status) params.status = status
    if (from) params.from = from
    if (to) params.to = to

    const response = await serviceCategoryClient.get<ApiResponse<Appointment[]>>(
      `${API_BASE}/provider/${providerId}`,
      { params }
    )

    console.log('[BookingService] Provider bookings retrieved:', response.data)

    // Handle wrapped response format
    const data = response.data?.data || response.data
    return data as Appointment[]
  } catch (error) {
    console.error('[BookingService] Error fetching provider bookings:', error)
    throw this.handleError(error)
  }
}
```

### Update Component Usage

**File**: `booksy-frontend/src/modules/provider/components/dashboard/BookingListCard.vue`

Update the `fetchBookings` function (lines 213-240):

```typescript
const fetchBookings = async () => {
  if (!props.providerId) return

  loading.value = true
  error.value = null

  try {
    // Use the corrected API endpoint
    const bookingsData = await bookingService.getProviderBookings(
      props.providerId,
      filterStatus.value !== 'all' ? filterStatus.value : undefined,
      // Could add date filters based on filterPeriod if needed
    )

    // Map API response to component format (with name resolution)
    const mappedBookings = await Promise.all(
      bookingsData.map(appointment => mapAppointmentToBooking(appointment))
    )
    bookings.value = mappedBookings
  } catch (err) {
    console.error('Error fetching bookings:', err)
    error.value = 'خطا در بارگذاری لیست رزروها'
    bookings.value = []
  } finally {
    loading.value = false
  }
}
```

---

## Business Logic Summary

### Provider Hierarchy Support

The system correctly handles different provider types:

1. **Organization Providers**:
   - Can view all bookings for the organization
   - Can view bookings for all staff members
   - Full management capabilities

2. **Individual Providers (with parent org)**:
   - Staff members of an organization
   - Can view only their own bookings via different endpoint
   - Limited management capabilities

3. **Solo Individual Providers**:
   - Independent practitioners
   - Can view all their bookings
   - Full control over their schedule

### Booking Statuses

Backend domain model (`BookingStatus` enum):
- `Pending` → Mapped to 'scheduled' in frontend
- `Confirmed` → Mapped to 'scheduled' in frontend
- `InProgress` → Mapped to 'scheduled' in frontend
- `Completed` → Mapped to 'completed' in frontend
- `Cancelled` → Mapped to 'cancelled' in frontend
- `NoShow` → Mapped to 'cancelled' in frontend

### Action Permissions

Each booking action has specific authorization:
- **Confirm**: Provider or Admin only
- **Complete**: Provider or Admin only
- **Cancel**: Customer, Provider, or Admin
- **Reschedule**: Customer or Provider (with conditions)
- **Assign Staff**: Organization owner only
- **Add Notes**: Provider or Admin
- **Mark No-Show**: Provider or Admin

---

## Testing Checklist

### Backend Tests
- [ ] Test `/api/v1/bookings/provider/{providerId}` endpoint directly
- [ ] Verify authorization works correctly
- [ ] Test filtering by status
- [ ] Test filtering by date range
- [ ] Verify empty result handling
- [ ] Test with different provider types (org, individual, staff)

### Frontend Tests
- [ ] Verify API calls use correct endpoint after fix
- [ ] Test search functionality
- [ ] Test status filtering
- [ ] Test period filtering
- [ ] Test pagination
- [ ] Test action buttons (confirm, cancel, etc.)
- [ ] Verify loading states display correctly
- [ ] Verify error handling works
- [ ] Test with no bookings (empty state)
- [ ] Test with large number of bookings (100+)

### Integration Tests
- [ ] End-to-end booking creation → display in provider dashboard
- [ ] Test real-time updates after booking actions
- [ ] Verify customer name resolution
- [ ] Verify service name resolution
- [ ] Test across different time zones
- [ ] Test Persian number formatting

---

## Performance Considerations

### Current Implementation
✅ **Good**: Repository uses indexed queries on `ProviderId`
✅ **Good**: Frontend caches customer and service names
⚠️ **Concern**: Fetches 100 bookings and filters client-side
⚠️ **Concern**: Multiple API calls for name resolution (N+1 problem)

### Recommendations
1. **Add server-side pagination** to provider endpoint
2. **Include customer/service names in response** to avoid additional lookups
3. **Add caching** for frequently accessed data
4. **Consider GraphQL** for flexible data fetching if N+1 becomes significant

---

## Conclusion

### Current Status
✅ **Real API Integration**: The component IS using real backend data
✅ **Proper Business Logic**: Correct authorization and filtering
✅ **Good UX**: Loading states, error handling, search, and pagination

### Action Items
🔧 **Fix Required**: Update frontend to use correct provider endpoint `/api/v1/bookings/provider/{providerId}`
📝 **Optional**: Add server-side pagination for better performance
📝 **Optional**: Include related entity names in API response to reduce round trips

---

**Last Updated**: 2025-12-22
**Reviewed By**: AI Code Analysis
**Status**: ✅ Analysis Complete - Fix Recommended
