# Week 7-8 Frontend Development Plan

## Overview

Build customer-facing React/Vue components that consume the Week 5-6 backend APIs for provider discovery and booking management.

**Tech Stack:** Vue 3 + TypeScript + Vite + Pinia + Vue Router

---

## 🎯 Goals

### Week 7: Provider Discovery
1. **Provider Search Interface** - Search with filters (service category, price range, location, rating)
2. **Provider Profile Page** - Comprehensive customer-facing profile display
3. **Availability Calendar** - Interactive calendar for slot selection

### Week 8: Booking Management
4. **Booking Creation Flow** - Multi-step booking process
5. **My Bookings Dashboard** - View upcoming/past bookings
6. **Reschedule/Cancel UI** - Manage existing bookings

---

## 📋 Features Implementation Plan

### 1. Provider Search Interface (Days 1-2)

**Components:**
- `ProviderSearchView.vue` - Main search page
- `ProviderSearchFilters.vue` - Filter sidebar
- `ProviderSearchResults.vue` - Results grid/list
- `ProviderCard.vue` - Individual provider card

**Features:**
- ✅ Service category dropdown (haircut, massage, spa, etc.)
- ✅ Price range filter (budget, moderate, premium)
- ✅ Location search (city, state, coordinates)
- ✅ Rating filter (minimum stars)
- ✅ Sort options (rating, price, distance, name)
- ✅ Grid/List view toggle
- ✅ Real-time search results
- ✅ Pagination

**API Integration:**
```typescript
GET /api/v1/providers/search?serviceCategory=haircut&priceRange=moderate&city=Tehran&sortBy=rating
```

---

### 2. Provider Profile Page (Days 3-4)

**Components:**
- `ProviderProfilePage.vue` - Main profile container
- `ProfileHeader.vue` - Business info, rating, photos
- `ProfileServices.vue` - Service list with pricing
- `ProfileReviews.vue` - Customer reviews
- `ProfileAvailability.vue` - Quick availability summary
- `ProfileGallery.vue` - Business photos lightbox
- `ProfileAbout.vue` - Description, hours, location

**Features:**
- ✅ Comprehensive provider information
- ✅ Photo gallery with lightbox
- ✅ Service listings with prices
- ✅ Recent customer reviews
- ✅ Business hours display
- ✅ Location map integration (Neshan Maps)
- ✅ "Book Now" CTA with next available slot
- ✅ Share profile button
- ✅ Add to favorites

**API Integration:**
```typescript
GET /api/v1/providers/{id}/profile
```

---

### 3. Availability Calendar (Days 5-6)

**Components:**
- `AvailabilityCalendar.vue` - Interactive calendar
- `TimeSlotPicker.vue` - Time slot selection
- `AvailabilityHeatmap.vue` - Visual availability indicator

**Features:**
- ✅ 7/14/30-day calendar view
- ✅ Available/Booked/Blocked slot indicators
- ✅ Real-time availability (15s polling)
- ✅ Click to select slot
- ✅ Service duration consideration
- ✅ Staff member selection
- ✅ Persian calendar support (Jalaali)

**API Integration:**
```typescript
GET /api/v1/providers/{id}/availability?startDate=2025-11-20&days=7

// Polling every 15 seconds for live updates
```

---

### 4. Booking Creation Flow (Days 7-8)

**Components:**
- `BookingWizard.vue` - Multi-step wizard
- `ServiceSelection.vue` - Choose service
- `SlotSelection.vue` - Choose date/time
- `CustomerInfo.vue` - Contact details
- `BookingConfirmation.vue` - Review and confirm

**Features:**
- ✅ Step-by-step booking process
- ✅ Service selection with pricing
- ✅ Date & time slot selection
- ✅ Customer information form
- ✅ Special requests/notes
- ✅ Booking summary
- ✅ Payment integration (future)
- ✅ Booking confirmation

**API Integration:**
```typescript
POST /api/v1/bookings
{
  "providerId": "xxx",
  "serviceId": "yyy",
  "startTime": "2025-11-20T14:00:00Z",
  "staffId": "zzz",
  "customerNotes": "..."
}
```

---

### 5. My Bookings Dashboard (Days 9-10)

**Components:**
- `BookingsDashboard.vue` - Main dashboard
- `BookingsList.vue` - Upcoming/past bookings
- `BookingCard.vue` - Individual booking card
- `BookingDetails.vue` - Detailed view modal

**Features:**
- ✅ Upcoming bookings list
- ✅ Past bookings history
- ✅ Booking status badges (Requested, Confirmed, Completed, Cancelled)
- ✅ Quick actions (Reschedule, Cancel, Review)
- ✅ Filter/sort bookings
- ✅ Booking countdown timer
- ✅ Provider contact info

**API Integration:**
```typescript
GET /api/v1/bookings?customerId={id}&status=Confirmed
```

---

### 6. Reschedule/Cancel UI (Days 11-12)

**Components:**
- `RescheduleBookingModal.vue` - Reschedule flow
- `CancelBookingModal.vue` - Cancellation flow
- `RefundInfo.vue` - Refund policy display

**Features:**
- ✅ Reschedule booking wizard
- ✅ New slot selection
- ✅ Policy validation (24-hour notice)
- ✅ Cancel booking with reason
- ✅ Refund calculation display
- ✅ Confirmation prompts
- ✅ Success/error feedback

**API Integration:**
```typescript
// Reschedule
POST /api/v1/bookings/{id}/reschedule
{
  "newStartTime": "2025-11-21T15:00:00Z",
  "newStaffId": "...",
  "reason": "..."
}

// Cancel
POST /api/v1/bookings/{id}/cancel
{
  "reason": "...",
  "cancelledBy": "...",
  "byProvider": false
}
```

---

## 🎨 UI/UX Priorities

### Persian/RTL Support
- ✅ Full RTL layout support
- ✅ Persian number formatting
- ✅ Jalaali calendar dates
- ✅ Persian translations (Vue I18n)

### Responsive Design
- ✅ Mobile-first approach
- ✅ Tablet optimization
- ✅ Desktop enhancements

### Performance
- ✅ Lazy loading
- ✅ Image optimization
- ✅ Virtual scrolling for long lists
- ✅ Debounced search
- ✅ Cached API responses

### Accessibility
- ✅ ARIA labels
- ✅ Keyboard navigation
- ✅ Screen reader support
- ✅ High contrast mode

---

## 📦 State Management (Pinia)

### Stores to Create

1. **`providerStore.ts`** - Provider search and profiles
2. **`bookingStore.ts`** - Booking management
3. **`availabilityStore.ts`** - Real-time availability
4. **`reviewStore.ts`** - Reviews and ratings

---

## 🔌 API Client Integration

### Composables to Create

1. **`useProviderSearch`** - Search providers
2. **`useProviderProfile`** - Get provider profile
3. **`useAvailability`** - Availability with polling
4. **`useBooking`** - Create/manage bookings
5. **`useReviews`** - Fetch/submit reviews

---

## 📱 Routing

### New Routes

```typescript
{
  path: '/search',
  component: () => import('@/modules/service-catalog/views/ProviderSearchView.vue')
},
{
  path: '/providers/:id',
  component: () => import('@/modules/service-catalog/views/ProviderProfilePage.vue')
},
{
  path: '/book/:providerId',
  component: () => import('@/modules/booking/views/BookingWizard.vue')
},
{
  path: '/my-bookings',
  component: () => import('@/modules/booking/views/MyBookingsView.vue'),
  meta: { requiresAuth: true }
}
```

---

## ✅ Success Criteria

### Week 7
- [x] Users can search providers by service, price, location
- [x] Users can view comprehensive provider profiles
- [x] Users can see real-time availability

### Week 8
- [x] Users can create bookings through wizard
- [x] Users can view their upcoming/past bookings
- [x] Users can reschedule/cancel bookings

---

## 🚀 Tech Stack Summary

- **Framework:** Vue 3 (Composition API)
- **Language:** TypeScript
- **Build Tool:** Vite
- **State Management:** Pinia
- **Routing:** Vue Router 4
- **I18n:** Vue I18n (Persian support)
- **Date/Time:** Jalaali-js (Persian calendar)
- **Maps:** Neshan Maps (Iranian)
- **Charts:** ECharts
- **Testing:** Vitest + Cypress
- **Styling:** SCSS + CSS Variables

---

## 📊 Estimated Timeline

| Week | Days | Features |
|------|------|----------|
| Week 7 | Days 1-2 | Provider Search |
| Week 7 | Days 3-4 | Provider Profile |
| Week 7 | Days 5-6 | Availability Calendar |
| Week 8 | Days 7-8 | Booking Creation |
| Week 8 | Days 9-10 | Bookings Dashboard |
| Week 8 | Days 11-12 | Reschedule/Cancel |

**Total:** 12 working days

---

## 🎯 Next Steps

1. Start with Provider Search (highest impact)
2. Implement Provider Profile
3. Build Availability Calendar
4. Complete Booking Wizard
5. Add Booking Management

---

**Status:** ⏸️ Ready to Start
**Priority:** High (customer-facing features)
**Dependencies:** Week 5-6 Backend APIs ✅ Complete
