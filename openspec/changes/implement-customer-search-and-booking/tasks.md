# Implementation Tasks: Customer Search and Booking

## Phase 1: MVP Search & Browse (Week 1)

### 1.1 Setup & Dependencies
- [ ] Install Persian calendar library (`npm install vue-shamsi-calendar` or similar)
- [ ] Add Persian date utility functions
- [ ] Configure API base URLs and endpoints
- [ ] Set up environment variables for payment gateway

### 1.2 Provider Search Implementation
- [ ] Create `searchStore.ts` Pinia store for search state management
- [ ] Implement `providerService.ts` with API integration
  - [ ] `searchProviders(filters)` method
  - [ ] `getProviderById(id)` method
  - [ ] `getProvidersByLocation(lat, lng, radius)` method
- [ ] Update `ProviderListView.vue` to use real API data
  - [ ] Connect to searchStore
  - [ ] Implement pagination
  - [ ] Add loading states
  - [ ] Add error handling
- [ ] Create `ProviderCard.vue` component
  - [ ] Display provider info (name, rating, category, distance)
  - [ ] Add "View Details" and "Book Now" actions
  - [ ] Show provider thumbnail/logo
  - [ ] Display business hours and status (open/closed)

### 1.3 Search Filters UI
- [ ] Create `SearchFilters.vue` component
  - [ ] Category filter (dropdown with provider types)
  - [ ] Location filter (address input with map)
  - [ ] Distance/radius slider
  - [ ] Rating filter (minimum rating)
  - [ ] Price range filter
  - [ ] Availability filter (open now, has slots today)
- [ ] Implement filter state management in searchStore
- [ ] Add "Clear Filters" functionality
- [ ] Persist filter preferences in localStorage

### 1.4 Provider Details View
- [ ] Update `ProviderDetailView.vue` with real data
  - [ ] Fetch provider details from API
  - [ ] Display business profile (description, contact, address)
  - [ ] Show business hours in Persian calendar format
  - [ ] Display provider rating and review count
  - [ ] Add "Add to Favorites" button
  - [ ] Show provider gallery (if available)
- [ ] Display provider services list
  - [ ] Group services by category
  - [ ] Show price, duration for each service
  - [ ] Add "Book This Service" buttons
- [ ] Display staff members (if applicable)
  - [ ] Show staff photos and specialties
  - [ ] Allow filtering availability by staff member
- [ ] Add map integration showing provider location
  - [ ] Use Leaflet or Google Maps
  - [ ] Show directions link

### 1.5 Service Browsing
- [ ] Create `serviceService.ts` for service APIs
  - [ ] `searchServices(filters)` method
  - [ ] `getServiceById(id)` method
  - [ ] `getServicesByProvider(providerId)` method
- [ ] Update `ServiceBrowseView.vue` with real data
  - [ ] Connect to service API
  - [ ] Implement service card grid
  - [ ] Add service filters (category, price, duration)
  - [ ] Show provider info for each service
- [ ] Create `ServiceDetailView.vue` (if needed)
  - [ ] Display service details (description, price, duration)
  - [ ] Show service options/add-ons
  - [ ] Display provider offering the service
  - [ ] Add "Book Now" CTA

---

## Phase 2: Booking Flow Core (Week 2)

### 2.1 Booking Store & Services
- [ ] Create `bookingStore.ts` Pinia store
  - [ ] Manage booking wizard steps (currentStep, completedSteps)
  - [ ] Store selected provider, service, date, time
  - [ ] Store customer notes and preferences
  - [ ] Calculate total price (service + options + deposits)
- [ ] Create `bookingService.ts` for booking APIs
  - [ ] `createBooking(bookingData)` method
  - [ ] `getAvailableSlots(providerId, serviceId, date)` method
  - [ ] `confirmBooking(bookingId, paymentData)` method
  - [ ] `cancelBooking(bookingId)` method
  - [ ] `rescheduleBooking(bookingId, newTime)` method
- [ ] Create `availabilityService.ts`
  - [ ] `getAvailableSlots(providerId, serviceId, staffId, date)` method
  - [ ] Client-side availability calculation helpers
  - [ ] Cache available slots to reduce API calls

### 2.2 Booking Wizard - Step 1: Select Service
- [ ] Update `BookingWizardView.vue` with multi-step layout
  - [ ] Step indicator component
  - [ ] Navigation (Next, Back, Cancel)
  - [ ] Progress tracking
- [ ] Create `ServiceSelector.vue` component
  - [ ] Display provider's services
  - [ ] Allow service selection with radio buttons
  - [ ] Show service details (price, duration)
  - [ ] Display service options/add-ons as checkboxes
  - [ ] Calculate and display subtotal
- [ ] Implement "Select Staff" option (if provider has multiple staff)
  - [ ] Show staff photos and specialties
  - [ ] Allow "No Preference" option
- [ ] Validate: Ensure service is selected before proceeding

### 2.3 Booking Wizard - Step 2: Select Date & Time
- [ ] Create `PersianCalendar.vue` component
  - [ ] Display Persian/Jalali calendar
  - [ ] Highlight available dates
  - [ ] Disable past dates and dates beyond max advance window
  - [ ] Mark dates with no availability
  - [ ] Handle date selection
- [ ] Create `TimeSlotPicker.vue` component
  - [ ] Fetch available slots for selected date
  - [ ] Display time slots in grid format
  - [ ] Show slot status (available, booked, blocked)
  - [ ] Display service duration for each slot
  - [ ] Handle slot selection
- [ ] Integrate calendar and time picker
  - [ ] Load available dates when provider/service selected
  - [ ] Load time slots when date selected
  - [ ] Show loading states during API calls
  - [ ] Handle errors (no availability, API failures)
- [ ] Add timezone handling
  - [ ] Display times in Tehran timezone
  - [ ] Store UTC times for API
  - [ ] Show timezone indicator

### 2.4 Booking Wizard - Step 3: Customer Details & Notes
- [ ] Create customer details form
  - [ ] Pre-fill from logged-in customer profile
  - [ ] Allow editing name, phone, email
  - [ ] Add customer notes textarea
  - [ ] Add special requirements checkboxes (e.g., mobility access)
- [ ] Validate customer information
  - [ ] Required fields: name, phone
  - [ ] Email format validation
  - [ ] Phone number format (Iranian format)

### 2.5 Booking Wizard - Step 4: Review & Confirm
- [ ] Create `BookingSummary.vue` component
  - [ ] Display all booking details:
    - [ ] Provider name, address, contact
    - [ ] Service name, duration, options
    - [ ] Selected date and time (Persian format)
    - [ ] Staff member (if selected)
    - [ ] Customer details
    - [ ] Customer notes
  - [ ] Show pricing breakdown:
    - [ ] Service base price
    - [ ] Options/add-ons prices
    - [ ] Subtotal
    - [ ] Deposit amount (if required)
    - [ ] Total to pay now
    - [ ] Remaining balance
  - [ ] Display cancellation policy
  - [ ] Add "Edit" links to go back to each step
- [ ] Implement booking creation (without payment initially)
  - [ ] Call `createBooking` API
  - [ ] Handle success: show booking ID, confirmation
  - [ ] Handle errors: display error message, allow retry
  - [ ] Store booking ID in local state

---

## Phase 3: Payment Integration (Week 3)

### 3.1 Payment UI Implementation
- [ ] Create `PaymentMethodSelector.vue` component
  - [ ] Display available payment methods
  - [ ] ZarinPal as primary method
  - [ ] Show payment icons and descriptions
  - [ ] Handle payment method selection
- [ ] Create `DepositCalculator.vue` component (if needed)
  - [ ] Display deposit percentage
  - [ ] Show deposit amount vs total
  - [ ] Explain refund policy
- [ ] Add payment step to booking wizard
  - [ ] Insert between "Review" and "Confirmation"
  - [ ] Display amount to pay
  - [ ] Show payment method selector
  - [ ] Add "Pay Now" button

### 3.2 ZarinPal Integration
- [ ] Create `paymentService.ts` for payment APIs
  - [ ] `initializePayment(bookingId, amount)` method
  - [ ] `verifyPayment(authority, status)` method
  - [ ] `getPaymentStatus(bookingId)` method
- [ ] Implement payment flow
  - [ ] Initialize payment with ZarinPal
  - [ ] Redirect to ZarinPal gateway
  - [ ] Handle callback from ZarinPal
  - [ ] Verify payment on return
  - [ ] Update booking status on success
- [ ] Add payment status handling
  - [ ] Success: show confirmation, send to My Bookings
  - [ ] Failure: show error, allow retry
  - [ ] Pending: show pending status, poll for updates
- [ ] Create payment callback route `/customer/payment/callback`
  - [ ] Extract payment authority and status
  - [ ] Verify payment with backend
  - [ ] Display appropriate message
  - [ ] Redirect to booking details

### 3.3 Booking Confirmation
- [ ] Create `BookingConfirmation.vue` component
  - [ ] Display success message
  - [ ] Show booking reference number
  - [ ] Display booking details summary
  - [ ] Show "Add to Calendar" button (iCal/Google Calendar)
  - [ ] Display provider contact for questions
  - [ ] Add "View Booking" and "Back to Home" CTAs
- [ ] Generate booking confirmation email (backend)
  - [ ] Email template with booking details
  - [ ] Include cancellation link
  - [ ] Add provider contact information
  - [ ] Send immediately after payment confirmation

### 3.4 My Bookings Dashboard
- [ ] Update `MyBookingsView.vue` with real data
  - [ ] Fetch customer's bookings from API
  - [ ] Group bookings: Upcoming, Past, Cancelled
  - [ ] Display booking cards with status indicators
  - [ ] Show countdown to next appointment
  - [ ] Add quick filters (all, upcoming, past, cancelled)
- [ ] Create `BookingCard.vue` component
  - [ ] Display booking summary (provider, service, date/time)
  - [ ] Show booking status with color coding
  - [ ] Add action buttons (View Details, Cancel, Reschedule)
  - [ ] Display booking reference number
  - [ ] Show payment status
- [ ] Update `BookingDetailView.vue`
  - [ ] Fetch booking details by ID
  - [ ] Display complete booking information
  - [ ] Show provider contact and address
  - [ ] Display payment details and receipt
  - [ ] Show booking history/timeline
  - [ ] Add "Get Directions" link
  - [ ] Add "Add to Calendar" button

### 3.5 Booking Actions
- [ ] Implement cancel booking
  - [ ] Create `CancelBookingModal.vue` component
  - [ ] Ask for cancellation reason
  - [ ] Display refund amount (based on cancellation policy)
  - [ ] Confirm cancellation
  - [ ] Call cancel API
  - [ ] Show success/error message
  - [ ] Refresh booking list
- [ ] Implement reschedule booking
  - [ ] Create `RescheduleBookingModal.vue` component
  - [ ] Show current booking time
  - [ ] Display available alternative slots
  - [ ] Allow new date/time selection
  - [ ] Show any price difference
  - [ ] Confirm reschedule
  - [ ] Call reschedule API
  - [ ] Show success/error message

---

## Phase 4: Notifications & Polish (Week 4)

### 4.1 Email Notifications
- [ ] Create email templates
  - [ ] Booking confirmation email
  - [ ] Booking cancellation email
  - [ ] Booking rescheduled email
  - [ ] Payment receipt email
- [ ] Implement email sending (backend)
  - [ ] Configure SMTP settings
  - [ ] Create email service in Notifications bounded context
  - [ ] Send emails on booking events (created, confirmed, cancelled, rescheduled)
  - [ ] Handle email failures gracefully

### 4.2 SMS Notifications (Optional)
- [ ] Select SMS provider (e.g., Kavenegar for Iran)
- [ ] Create SMS templates
  - [ ] Booking confirmation SMS
  - [ ] 24-hour reminder SMS
  - [ ] 1-hour reminder SMS
- [ ] Implement SMS sending service
  - [ ] Configure SMS provider credentials
  - [ ] Create SMS service
  - [ ] Schedule reminder jobs
  - [ ] Handle SMS failures

### 4.3 Advanced Search Features
- [ ] Add search suggestions/autocomplete
  - [ ] Provider name autocomplete
  - [ ] Service name autocomplete
  - [ ] Location/address autocomplete
- [ ] Implement "Search by Map" feature
  - [ ] Display providers on map
  - [ ] Filter by visible map area
  - [ ] Cluster providers at zoom out
  - [ ] Show provider info on marker click
- [ ] Add "Popular Near You" section
  - [ ] Use geolocation to find nearby providers
  - [ ] Display top-rated local providers
  - [ ] Show trending services
- [ ] Implement search history
  - [ ] Store recent searches in localStorage
  - [ ] Display recent searches on search page
  - [ ] Allow clearing search history

### 4.4 Performance Optimizations
- [ ] Frontend optimizations
  - [ ] Lazy load booking wizard components
  - [ ] Optimize images (use WebP, lazy loading)
  - [ ] Implement virtual scrolling for long lists
  - [ ] Add service worker for offline support
  - [ ] Analyze and reduce bundle size
- [ ] Backend optimizations
  - [ ] Add caching for provider search results
  - [ ] Optimize availability query with database indexes
  - [ ] Implement response compression
  - [ ] Add rate limiting for public endpoints

### 4.5 Mobile Responsiveness
- [ ] Test all views on mobile devices
  - [ ] Provider list and details
  - [ ] Service browse
  - [ ] Booking wizard (all steps)
  - [ ] My bookings dashboard
- [ ] Optimize touch interactions
  - [ ] Increase button sizes for touch targets
  - [ ] Add swipe gestures for navigation
  - [ ] Improve calendar touch interactions
- [ ] Improve mobile performance
  - [ ] Reduce network requests
  - [ ] Optimize images for mobile
  - [ ] Test on slow 3G connections

### 4.6 Accessibility (a11y)
- [ ] Add ARIA labels to all interactive elements
- [ ] Ensure keyboard navigation works
  - [ ] Calendar navigation
  - [ ] Time slot selection
  - [ ] Form inputs
  - [ ] Booking wizard steps
- [ ] Test with screen readers
- [ ] Ensure color contrast meets WCAG standards
- [ ] Add focus indicators for keyboard users
- [ ] Provide text alternatives for icons

### 4.7 Error Handling & Edge Cases
- [ ] Handle API errors gracefully
  - [ ] Network failures (offline mode)
  - [ ] Server errors (5xx)
  - [ ] Authentication errors (401/403)
  - [ ] Validation errors (400)
- [ ] Add user-friendly error messages
  - [ ] Translate errors to Persian
  - [ ] Provide actionable suggestions
  - [ ] Add retry mechanisms
- [ ] Handle edge cases
  - [ ] No search results found
  - [ ] No available time slots
  - [ ] Booking conflicts (slot taken)
  - [ ] Payment gateway timeouts
  - [ ] Double booking prevention

### 4.8 Testing
- [ ] Write unit tests
  - [ ] Booking store logic
  - [ ] Availability calculation helpers
  - [ ] Date/time conversion utilities
  - [ ] Form validation functions
- [ ] Write integration tests
  - [ ] Complete booking flow
  - [ ] Payment processing
  - [ ] Booking cancellation with refund
  - [ ] Booking rescheduling
- [ ] Write E2E tests (Cypress)
  - [ ] User journey: search → book → confirm
  - [ ] Cancel booking flow
  - [ ] Reschedule booking flow
  - [ ] Payment success/failure scenarios
- [ ] Performance testing
  - [ ] Load test search endpoints
  - [ ] Stress test availability calculations
  - [ ] Measure frontend bundle size
  - [ ] Test on slow networks

### 4.9 Documentation
- [ ] Update README with customer features
- [ ] Document API endpoints used by frontend
- [ ] Create user guide for customers (optional)
- [ ] Document payment integration setup
- [ ] Add inline code comments
- [ ] Update CHANGELOG.md

---

## Deployment Checklist

- [ ] Configure production environment variables
  - [ ] API base URLs
  - [ ] ZarinPal merchant ID
  - [ ] SMTP settings
  - [ ] SMS provider credentials (if used)
- [ ] Database migration for indexes (if needed)
- [ ] Deploy backend changes
- [ ] Deploy frontend changes
- [ ] Test in production environment
- [ ] Monitor error logs for first 24 hours
- [ ] Gather user feedback

---

## Success Metrics (Post-Launch)

- [ ] Track booking completion rate
- [ ] Monitor search → booking conversion
- [ ] Measure average booking time
- [ ] Track payment success rate
- [ ] Monitor API performance (response times)
- [ ] Collect customer satisfaction ratings
- [ ] Analyze most used filters and features
