# Booking API Migration Guide

## Overview

The backend has been refactored to use proper CQRS patterns with `PaginatedQueryBase` and return enriched booking data. The frontend needs to be updated to use the new response types.

## What Changed

### Backend Changes

1. **New Query Pattern**: `GetCustomerBookingsQuery` now extends `PaginatedQueryBase<CustomerBookingDto>`
2. **Enriched Response**: Returns `PagedResult<CustomerBookingDto>` with service names, provider names, and staff names
3. **Proper Pagination**: Uses `PaginationRequest` with HTTP headers (`X-Pagination`, `Link`, etc.)
4. **New Endpoint Parameters**:
   - Old: `?pageNumber=1&pageSize=20`
   - New: `?page=1&size=20&sort=StartTime&sortDesc=true`

### Frontend Changes

Created three new files:

1. **`booking-api.types.ts`**: Type definitions matching backend DTOs
2. **`booking-dto.mapper.ts`**: Mappers to convert between backend and frontend models
3. **Updated `booking.service.ts`**: New methods using enriched types

## New Types

### CustomerBookingDto (from backend)

```typescript
interface CustomerBookingDto {
  bookingId: string
  customerId: string
  providerId: string
  serviceId: string
  staffId: string | null
  serviceName: string        // NEW - enriched data
  providerName: string       // NEW - enriched data
  staffName: string | null   // NEW - enriched data
  startTime: string
  endTime: string
  durationMinutes: number
  status: string
  totalPrice: number
  currency: string
  paymentStatus: string
  requestedAt: string
  confirmedAt: string | null
  customerNotes: string | null
}
```

### PagedResult<T>

```typescript
interface PagedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalPages: number
  totalCount: number
  count: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  isFirstPage: boolean
  isLastPage: boolean
  previousPageNumber: number | null
  nextPageNumber: number | null
  itemRange: string
}
```

## Migration Steps

### Step 1: Import New Types

```typescript
// OLD
import { bookingService, type PaginatedBookingsResponse } from '@/modules/booking/api/booking.service'

// NEW
import { bookingService } from '@/modules/booking/api/booking.service'
import type { CustomerBookingsResponse, CustomerBookingDto } from '@/modules/booking/types/booking-api.types'
import { mapToEnrichedBookingView, type EnrichedBookingView } from '@/modules/booking/mappers/booking-dto.mapper'
```

### Step 2: Update Service Calls

#### Option A: Use New Method with Enriched Data

```typescript
// OLD
const response = await bookingService.getMyBookings(status, pageNumber, pageSize)

// NEW
const response = await bookingService.getMyBookings({
  status: status,
  page: pageNumber,
  size: pageSize,
  from: fromDate,
  to: toDate,
  sort: 'StartTime',
  sortDesc: true
})
```

#### Option B: Use Legacy Method (backward compatible)

```typescript
// Still works, but marked as @deprecated
const response = await bookingService.getMyBookingsLegacy(status, pageNumber, pageSize)
```

### Step 3: Update Response Handling

```typescript
// OLD
response.items.forEach(booking => {
  console.log(booking.id) // Appointment type
  // No service name, provider name available
})

// NEW - Access enriched data
response.items.forEach(booking => {
  console.log(booking.bookingId)      // Note: bookingId not id
  console.log(booking.serviceName)    // NEW - enriched
  console.log(booking.providerName)   // NEW - enriched
  console.log(booking.staffName)      // NEW - enriched
})

// NEW - With formatting helper
const enriched = response.items.map(mapToEnrichedBookingView)
enriched.forEach(booking => {
  console.log(booking.formattedDate)
  console.log(booking.formattedPrice)
  console.log(booking.statusLabel)     // Persian label
  console.log(booking.isUpcoming)      // Boolean flag
})
```

### Step 4: Update Pagination

```typescript
// OLD
const totalPages = response.totalPages
const currentPage = response.pageNumber

// NEW - More information available
const {
  totalPages,
  totalCount,
  pageNumber,
  hasPreviousPage,
  hasNextPage,
  previousPageNumber,
  nextPageNumber,
  itemRange  // "1-20 of 150"
} = response
```

## Component Migration Examples

### Example 1: MyBookingsView.vue

```vue
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { bookingService } from '@/modules/booking/api/booking.service'
import type { CustomerBookingsResponse } from '@/modules/booking/types/booking-api.types'
import { mapToEnrichedBookingView, type EnrichedBookingView } from '@/modules/booking/mappers/booking-dto.mapper'

// State
const bookings = ref<EnrichedBookingView[]>([])
const pagination = ref({
  page: 1,
  size: 20,
  totalPages: 0,
  totalCount: 0
})

// Fetch bookings
async function fetchBookings() {
  try {
    const response = await bookingService.getMyBookings({
      page: pagination.value.page,
      size: pagination.value.size,
      sort: 'StartTime',
      sortDesc: true
    })

    // Map to enriched view models
    bookings.value = response.items.map(mapToEnrichedBookingView)

    // Update pagination
    pagination.value = {
      page: response.pageNumber,
      size: response.pageSize,
      totalPages: response.totalPages,
      totalCount: response.totalCount
    }
  } catch (error) {
    console.error('Failed to fetch bookings:', error)
  }
}

onMounted(() => {
  fetchBookings()
})
</script>

<template>
  <div class="bookings-container">
    <div v-for="booking in bookings" :key="booking.bookingId">
      <h3>{{ booking.serviceName }}</h3>
      <p>ارائه‌دهنده: {{ booking.providerName }}</p>
      <p v-if="booking.staffName">کارمند: {{ booking.staffName }}</p>
      <p>{{ booking.formattedDate }} - {{ booking.formattedTime }}</p>
      <p>{{ booking.formattedPrice }}</p>
      <span :class="`badge badge-${booking.statusColor}`">
        {{ booking.statusLabel }}
      </span>
    </div>

    <!-- Pagination -->
    <div class="pagination">
      <span>{{ response.itemRange }} از {{ pagination.totalCount }}</span>
      <button
        :disabled="!response.hasPreviousPage"
        @click="pagination.page--; fetchBookings()">
        قبلی
      </button>
      <button
        :disabled="!response.hasNextPage"
        @click="pagination.page++; fetchBookings()">
        بعدی
      </button>
    </div>
  </div>
</template>
```

### Example 2: BookingsSidebar.vue

```vue
<script setup lang="ts">
import { ref, computed } from 'vue'
import { bookingService } from '@/modules/booking/api/booking.service'
import type { CustomerBookingDto } from '@/modules/booking/types/booking-api.types'
import { mapToEnrichedBookingView } from '@/modules/booking/mappers/booking-dto.mapper'

const upcomingBookings = ref<CustomerBookingDto[]>([])

async function loadUpcomingBookings() {
  try {
    const bookings = await bookingService.getUpcomingBookings(5)
    upcomingBookings.value = bookings
  } catch (error) {
    console.error('Failed to load upcoming bookings:', error)
  }
}

// Computed enriched bookings
const enrichedBookings = computed(() =>
  upcomingBookings.value.map(mapToEnrichedBookingView)
)
</script>

<template>
  <aside class="bookings-sidebar">
    <h3>رزروهای آینده</h3>
    <div v-for="booking in enrichedBookings" :key="booking.bookingId">
      <div class="booking-card">
        <h4>{{ booking.serviceName }}</h4>
        <p>{{ booking.providerName }}</p>
        <p>{{ booking.formattedDate }}</p>
        <span :class="`status-${booking.statusColor}`">
          {{ booking.statusLabel }}
        </span>
        <button
          v-if="booking.canCancel"
          @click="cancelBooking(booking.bookingId)">
          لغو رزرو
        </button>
      </div>
    </div>
  </aside>
</template>
```

## Benefits of New Approach

### 1. **Enriched Data** - No More N+1 Queries
```typescript
// OLD - Need separate API calls to get names
const booking = bookings[0]
const service = await serviceService.getById(booking.serviceId)
const provider = await providerService.getById(booking.providerId)

// Display
console.log(service.name, provider.name)

// NEW - Names already included
const booking = bookings[0]
console.log(booking.serviceName, booking.providerName)
```

### 2. **Better Pagination**
```typescript
// OLD - Manual calculation
const totalPages = Math.ceil(totalItems / pageSize)
const hasNext = currentPage < totalPages

// NEW - All calculated by backend
console.log(response.hasNextPage)
console.log(response.nextPageNumber)
console.log(response.itemRange) // "1-20 of 150"
```

### 3. **Type Safety**
```typescript
// OLD - Any type, easy to make mistakes
const booking: any = response.items[0]

// NEW - Full type safety
const booking: CustomerBookingDto = response.items[0]
// TypeScript knows all fields
```

### 4. **Consistent Casing**
```typescript
// Backend uses PascalCase in JSON responses
// But our types use camelCase for JavaScript conventions
// Mappers handle the conversion automatically
```

## Components to Update

Migration status:

1. ✅ **COMPLETED** - User-facing booking displays:
   - ✅ `MyBookingsView.vue` - Main bookings page (UPDATED)
   - ✅ `BookingsSidebar.vue` - Sidebar upcoming bookings (UPDATED)
   - ✅ `BookingDetailView.vue` - Single booking details (UPDATED)

2. ⚠️ **Medium Priority** - Provider dashboards:
   - `BookingListCard.vue` - Provider booking list
   - `BookingStatsCard.vue` - Booking statistics

3. 📝 **Low Priority** - Can use legacy methods:
   - `BookingWizard.vue` - Booking creation flow
   - `RescheduleBookingModal.vue` - Reschedule modal
   - `AddNotesModal.vue` - Notes modal

## Backward Compatibility

The old `getMyBookings()` method has been renamed to `getMyBookingsLegacy()` and marked as `@deprecated`. It still works but should be migrated eventually.

## Testing Checklist

- [x] My Bookings page loads correctly
- [x] Pagination works (prev/next buttons)
- [x] Filter by status works
- [x] Service names display correctly
- [x] Provider names display correctly
- [x] Staff names display (when assigned)
- [x] Date/time formatting is correct
- [x] Price formatting is correct
- [x] Status badges show correct colors
- [x] Cancel/Reschedule buttons work
- [x] Upcoming bookings sidebar works
- [x] Booking detail view shows enriched data
- [x] Payment status displays correctly

## API Documentation

### GET /api/v1/bookings/my-bookings

**Query Parameters:**
- `page` (number, default: 1): Page number
- `size` (number, default: 20, max: 100): Page size
- `status` (string, optional): Filter by status
- `from` (DateTime, optional): Filter bookings starting from date
- `to` (DateTime, optional): Filter bookings until date
- `sort` (string, optional): Sort field
- `sortDesc` (boolean, optional): Sort descending

**Response Headers:**
- `X-Pagination`: JSON with pagination metadata
- `X-Total-Count`: Total item count
- `X-Total-Pages`: Total page count
- `X-Current-Page`: Current page number
- `X-Page-Size`: Page size
- `Link`: RFC 5988 links for prev/next pages

**Response Body:**
```json
{
  "items": [
    {
      "bookingId": "guid",
      "customerId": "guid",
      "providerId": "guid",
      "serviceId": "guid",
      "staffId": "guid",
      "serviceName": "کوتاهی مو",
      "providerName": "آرایشگاه نهال",
      "staffName": "علی رضایی",
      "startTime": "2025-01-15T10:00:00Z",
      "endTime": "2025-01-15T11:00:00Z",
      "durationMinutes": 60,
      "status": "Confirmed",
      "totalPrice": 500000,
      "currency": "IRR",
      "paymentStatus": "Pending",
      "requestedAt": "2025-01-10T12:00:00Z",
      "confirmedAt": "2025-01-10T12:30:00Z",
      "customerNotes": "لطفا موهای بلند را کوتاه کنید"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 5,
  "totalCount": 95,
  "count": 20,
  "hasPreviousPage": false,
  "hasNextPage": true,
  "isFirstPage": true,
  "isLastPage": false,
  "previousPageNumber": null,
  "nextPageNumber": 2,
  "itemRange": "1-20 of 95"
}
```

## Migration Summary

### What Was Changed

All three high-priority customer-facing components have been successfully migrated to use the new enriched booking API:

#### 1. MyBookingsView.vue
- **Updated**: Uses `CustomerBookingsResponse` with `PagedResult<CustomerBookingDto>`
- **Benefits**:
  - Service, provider, and staff names displayed directly (no extra API calls)
  - Proper pagination with `hasPreviousPage`/`hasNextPage` flags
  - Filter by status, date range, and sorting
  - Pre-formatted dates, times, and prices for Persian locale

#### 2. BookingsSidebar.vue
- **Updated**: Uses helper methods `getUpcomingBookings()` and `getPastBookings()`
- **Benefits**:
  - Simplified logic (pagination handled by service)
  - Enriched data with readable names
  - Pre-formatted display values
  - Status badges with proper colors

#### 3. BookingDetailView.vue
- **Updated**: Uses `EnrichedBookingView` with all formatted values
- **Benefits**:
  - Displays service name, provider name, staff name (instead of GUIDs)
  - Payment status with Persian labels
  - Pre-formatted dates, times, and prices
  - Status badges using enriched `statusColor` and `statusLabel`
  - Simplified cancel logic using `canCancel` flag

### Key Improvements

1. **No More N+1 Queries**: Service/provider/staff names come with booking data
2. **Type Safety**: Full TypeScript types matching backend DTOs
3. **Consistent Formatting**: Persian/Jalali dates and currency formatting
4. **Better Pagination**: Rich pagination metadata from backend
5. **Cleaner Code**: Reusable mappers and enriched view models

## Questions?

Contact the backend team or check:
- Backend implementation: `GetCustomerBookingsQueryHandler.cs`
- Controller: `BookingsController.cs:GetMyBookings`
- DTOs: `CustomerBookingDto.cs`
- Frontend mappers: `booking-dto.mapper.ts`
- Frontend types: `booking-api.types.ts`
