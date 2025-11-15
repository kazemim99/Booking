# Customer Search and Booking Implementation Proposal

## Why

Currently, the Booksy platform has comprehensive **provider-facing features** (registration, dashboard, services, staff, gallery) but **limited customer-facing functionality**. The backend APIs for customer search and booking are implemented, but the frontend integration is incomplete:

- **Search UI**: Frontend has routes (`/customer/browse`, `/customer/providers`) but uses placeholder/mock data
- **Booking Flow**: Backend APIs exist for creating bookings, checking availability, and managing appointments, but the frontend booking wizard is not connected to real data
- **Time Selection**: No interactive calendar/time slot picker for selecting appointment times
- **Payment Integration**: Payment API infrastructure exists but UI flow is incomplete

This proposal implements a **complete end-to-end customer experience** for discovering service providers, viewing available time slots, and booking appointments.

## What Changes

### Frontend (Primary Focus)
1. **Provider Search & Discovery**
   - Connect `/customer/browse` and `/customer/providers` to real backend APIs
   - Implement search filters (location, category, rating, price range)
   - Add pagination and infinite scroll
   - Integrate provider details view with services, staff, gallery, reviews

2. **Service Browse & Details**
   - Connect service browsing to backend service catalog API
   - Display service details (price, duration, description, options)
   - Show service availability indicators

3. **Booking Wizard Flow**
   - **Step 1**: Select provider (from search or favorites)
   - **Step 2**: Select service (with options/add-ons)
   - **Step 3**: Select date and time (Persian calendar with available slots)
   - **Step 4**: Add customer notes and confirm details
   - **Step 5**: Payment (deposit or full payment)
   - **Step 6**: Confirmation and booking details

4. **Time Slot Selection**
   - Interactive Persian (Jalali) calendar component
   - Available time slots display based on:
     - Provider business hours
     - Service duration
     - Staff availability
     - Existing bookings
   - Real-time availability checking

5. **My Bookings Dashboard**
   - Connect `/customer/my-bookings` to backend API
   - Display upcoming, past, and cancelled bookings
   - Quick actions: cancel, reschedule, view details
   - Booking status indicators

6. **Payment Flow**
   - Payment method selection
   - Deposit calculation and display
   - ZarinPal integration for Iranian market
   - Payment confirmation and receipt

### Backend (Enhancements)
1. **API Response Optimization**
   - Add DTOs optimized for frontend needs
   - Include related data to reduce API calls
   - Add caching for frequently accessed data

2. **Availability Calculation**
   - Optimize time slot calculation algorithm
   - Consider buffer times and preparation times
   - Handle staff member scheduling

3. **Notification System** (New)
   - Email/SMS booking confirmations
   - Appointment reminders (24h, 1h before)
   - Booking status change notifications

## Impact

### Affected Specs
- **NEW**: `customer-search` - Customer provider/service discovery
- **NEW**: `customer-booking` - Appointment booking lifecycle
- **MODIFIED**: `authentication` - Add customer registration flow refinements
- **MODIFIED**: `provider-management` - Add customer-facing provider views

### Affected Code

#### Frontend
- **Views**:
  - `booksy-frontend/src/modules/customer/views/browse/ProviderListView.vue` - Connect to API
  - `booksy-frontend/src/modules/customer/views/browse/ProviderDetailView.vue` - Complete implementation
  - `booksy-frontend/src/modules/customer/views/browse/ServiceBrowseView.vue` - Connect to API
  - `booksy-frontend/src/modules/booking/views/BookingWizardView.vue` - Complete multi-step flow
  - `booksy-frontend/src/modules/customer/views/bookings/MyBookingsView.vue` - Connect to API

- **Components** (New):
  - `booksy-frontend/src/modules/booking/components/TimeSlotPicker.vue` - Time selection
  - `booksy-frontend/src/modules/booking/components/PersianCalendar.vue` - Date picker
  - `booksy-frontend/src/modules/booking/components/ServiceSelector.vue` - Service selection
  - `booksy-frontend/src/modules/booking/components/BookingSummary.vue` - Confirmation
  - `booksy-frontend/src/modules/customer/components/ProviderCard.vue` - Search results
  - `booksy-frontend/src/modules/customer/components/SearchFilters.vue` - Filter UI

- **Services**:
  - `booksy-frontend/src/modules/customer/services/providerService.ts` - API integration
  - `booksy-frontend/src/modules/booking/services/bookingService.ts` - Booking APIs
  - `booksy-frontend/src/modules/booking/services/availabilityService.ts` - Time slots

- **Stores** (Pinia):
  - `booksy-frontend/src/modules/customer/stores/searchStore.ts` - Search state
  - `booksy-frontend/src/modules/booking/stores/bookingStore.ts` - Booking wizard state

#### Backend
- **API Enhancements**:
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs` - Add customer-optimized endpoints
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/BookingsController.cs` - Add availability optimization
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ServicesController.cs` - Add search improvements

- **Application Layer** (New):
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/GetAvailableTimeSlots/` - Optimized availability query
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/CreateBooking/` - Enhanced booking creation

- **Notifications** (New Bounded Context):
  - `src/BoundedContexts/Notifications/` - New domain for notifications
  - Email templates for booking confirmations
  - SMS integration for appointment reminders

### Database
- **No schema changes required** - All necessary tables exist
- **Performance**: Add indexes for common search queries
  ```sql
  CREATE INDEX idx_providers_location ON ServiceCatalog.Providers (Latitude, Longitude);
  CREATE INDEX idx_bookings_customer_date ON ServiceCatalog.Bookings (CustomerId, StartTime);
  CREATE INDEX idx_services_provider_category ON ServiceCatalog.Services (ProviderId, Category);
  ```

### External Integrations
- **ZarinPal Payment Gateway** - Already integrated, complete UI flow
- **SMS Provider** (Optional) - For appointment reminders
- **Email Service** - For notifications (use existing infrastructure)

### Breaking Changes
**None** - This is purely additive functionality. All existing provider features remain unchanged.

### Performance Considerations
1. **Search Performance**: Implement pagination, use database indexes
2. **Availability Calculation**: Cache provider schedules, optimize time slot queries
3. **Frontend Performance**: Lazy load components, optimize bundle size
4. **API Response Times**: Target <200ms for search, <500ms for availability

## Implementation Phases

### Phase 1: MVP Search & Browse (Week 1)
- Connect provider search to backend API
- Implement basic search filters (category, location)
- Complete provider details view
- Service browsing functionality

### Phase 2: Booking Flow Core (Week 2)
- Implement booking wizard steps 1-4
- Time slot picker with Persian calendar
- Availability checking integration
- Booking creation (skip payment)

### Phase 3: Payment Integration (Week 3)
- Complete payment flow UI
- ZarinPal integration for deposits
- Booking confirmation and receipt
- My Bookings dashboard

### Phase 4: Notifications & Polish (Week 4)
- Email booking confirmations
- SMS appointment reminders
- Advanced search filters
- Performance optimizations
- Mobile responsiveness

## Success Criteria

1. **Customer can search providers**
   - Search by category, location, rating
   - Filter results by distance, price, availability
   - View provider details with services and gallery

2. **Customer can book appointment**
   - Select service and provider
   - Choose available time slot using Persian calendar
   - Complete payment (deposit or full)
   - Receive booking confirmation

3. **Customer can manage bookings**
   - View all bookings (upcoming, past, cancelled)
   - Cancel booking with refund calculation
   - Reschedule appointment
   - View booking details and history

4. **Performance meets targets**
   - Search results < 200ms
   - Availability calculation < 500ms
   - Booking creation < 1s
   - Page load times < 2s

5. **Mobile experience**
   - All views responsive and mobile-optimized
   - Touch-friendly interactions
   - Fast loading on mobile networks

## Testing Strategy

1. **Unit Tests**
   - Availability calculation logic
   - Booking validation rules
   - Search filter logic

2. **Integration Tests**
   - End-to-end booking flow
   - Payment processing
   - Notification sending

3. **E2E Tests** (Cypress)
   - Complete user journey: search → select → book → confirm
   - Payment flow testing
   - Booking management scenarios

4. **Performance Tests**
   - Load test search endpoints
   - Stress test availability calculations
   - Frontend bundle size analysis

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Persian calendar complexity | High | Use existing vue-shamsi-calendar library, add comprehensive tests |
| Payment gateway failures | High | Implement retry logic, fallback payment methods, clear error messages |
| Availability calculation performance | Medium | Cache provider schedules, optimize queries, add database indexes |
| Mobile performance | Medium | Lazy load components, optimize images, use service workers |
| Time zone handling | Low | Store all times in UTC, convert to Tehran timezone on display |

## Dependencies

- **Backend APIs**: ✅ Already implemented
- **Authentication**: ✅ Customer auth already working
- **Database Schema**: ✅ All tables exist
- **Payment Gateway**: ✅ ZarinPal integrated
- **Frontend Framework**: ✅ Vue 3 + TypeScript + Pinia
- **Persian Calendar Library**: ⚠️ Need to install `vue-shamsi-calendar` or similar
- **Date/Time Utils**: ⚠️ Need Persian date conversion utilities

## Open Questions

1. **Should customers be able to book without registration?** (Guest checkout)
   - Recommendation: Require registration for booking management

2. **Should we implement real-time availability updates?** (WebSockets)
   - Recommendation: Phase 2 feature, start with polling

3. **Should customers rate/review providers immediately after appointment?**
   - Recommendation: Yes, but in separate phase

4. **Maximum advance booking window?**
   - Recommendation: Configurable per provider (default 90 days)

5. **Cancellation policy enforcement?**
   - Recommendation: Use existing `BookingPolicy` in domain model

## Notes

- This proposal focuses on **frontend implementation** since backend APIs are already complete
- All backend code follows **Clean Architecture + CQRS + DDD** patterns
- Frontend uses **Vue 3 Composition API** with TypeScript
- Persian/Farsi RTL support is already implemented in the frontend
- Design system uses **Tailwind CSS** with custom RTL utilities
