# Customer Profile - Minimal Implementation Tasks (3 Weeks)

## Week 1: Backend Implementation

### 1. Database Schema (Day 1-2)

- [ ] 1.1 Create migration to extend users table
  - [ ] Add `full_name VARCHAR(100)` column
  - [ ] Add `email VARCHAR(255)` column
  - [ ] Test migration on local database

- [ ] 1.2 Create migration for `favorite_providers` table
  - [ ] Create table with: id, customer_id, provider_id, created_at
  - [ ] Add UNIQUE constraint on (customer_id, provider_id)
  - [ ] Add index on customer_id
  - [ ] Test migration

- [ ] 1.3 Create migration for `customer_booking_history` table
  - [ ] Create table with: booking_id (PK), customer_id, provider_id, provider_name, service_name, start_time, status, total_price, created_at
  - [ ] Add index on (customer_id, start_time DESC)
  - [ ] Add index on status
  - [ ] Test migration

- [ ] 1.4 Create migration for `customer_preferences` table
  - [ ] Create table with: customer_id (PK), sms_enabled, email_enabled, reminder_timing, updated_at
  - [ ] Test migration

- [ ] 1.5 Test all migrations together
  - [ ] Test rollback procedures
  - [ ] Verify indexes created correctly

### 2. Domain Layer (Day 2-3)

- [ ] 2.1 Create FavoriteProvider aggregate
  - [ ] FavoriteProvider.cs with properties (CustomerId, ProviderId, CreatedAt)
  - [ ] Add() method
  - [ ] Remove() method

- [ ] 2.2 Create CustomerBookingHistory read model
  - [ ] CustomerBookingHistoryEntry.cs with all booking fields
  - [ ] UpdateStatus() method

- [ ] 2.3 Create CustomerPreferences value object
  - [ ] CustomerPreferences.cs (SmsEnabled, EmailEnabled, ReminderTiming)

### 3. Application Layer - Commands (Day 3-4)

- [ ] 3.1 UpdateCustomerProfile command
  - [ ] UpdateCustomerProfileCommand.cs (fullName, email)
  - [ ] UpdateCustomerProfileCommandValidator.cs (FluentValidation)
  - [ ] UpdateCustomerProfileCommandHandler.cs
  - [ ] Unit test

- [ ] 3.2 AddFavoriteProvider command
  - [ ] AddFavoriteProviderCommand.cs (customerId, providerId)
  - [ ] AddFavoriteProviderCommandValidator.cs
  - [ ] AddFavoriteProviderCommandHandler.cs
  - [ ] Unit test

- [ ] 3.3 RemoveFavoriteProvider command
  - [ ] RemoveFavoriteProviderCommand.cs (customerId, providerId)
  - [ ] RemoveFavoriteProviderCommandHandler.cs
  - [ ] Unit test

- [ ] 3.4 UpdateNotificationPreferences command
  - [ ] UpdateNotificationPreferencesCommand.cs
  - [ ] UpdateNotificationPreferencesCommandHandler.cs
  - [ ] Unit test

### 4. Application Layer - Queries (Day 4-5)

- [ ] 4.1 GetCustomerProfile query
  - [ ] GetCustomerProfileQuery.cs
  - [ ] GetCustomerProfileQueryHandler.cs
  - [ ] CustomerProfileViewModel.cs (DTO)
  - [ ] Unit test

- [ ] 4.2 GetUpcomingBookings query
  - [ ] GetUpcomingBookingsQuery.cs (limit = 5)
  - [ ] GetUpcomingBookingsQueryHandler.cs
  - [ ] UpcomingBookingViewModel.cs
  - [ ] Unit test

- [ ] 4.3 GetBookingHistory query
  - [ ] GetBookingHistoryQuery.cs (page, size = 20)
  - [ ] GetBookingHistoryQueryHandler.cs
  - [ ] BookingHistoryViewModel.cs
  - [ ] Unit test

- [ ] 4.4 GetFavoriteProviders query
  - [ ] GetFavoriteProvidersQuery.cs
  - [ ] GetFavoriteProvidersQueryHandler.cs
  - [ ] FavoriteProviderViewModel.cs
  - [ ] Unit test

- [ ] 4.5 GetCustomerReviews query
  - [ ] GetCustomerReviewsQuery.cs
  - [ ] GetCustomerReviewsQueryHandler.cs
  - [ ] CustomerReviewViewModel.cs
  - [ ] Unit test

### 5. Infrastructure Layer (Day 5)

- [ ] 5.1 Create repositories
  - [ ] IFavoriteProviderRepository.cs interface
  - [ ] FavoriteProviderRepository.cs implementation
  - [ ] FavoriteProviderConfiguration.cs (EF Core)

- [ ] 5.2 Create event handlers
  - [ ] BookingEventHandlers.cs
  - [ ] Handle BookingCreatedEvent → Insert to customer_booking_history
  - [ ] Handle BookingCompletedEvent → Update status
  - [ ] Handle BookingCancelledEvent → Update status
  - [ ] Handle ProviderDeletedEvent → Soft-delete favorites

### 6. API Layer (Day 5)

- [ ] 6.1 Create CustomersController
  - [ ] GET /api/v1/customers/profile
  - [ ] PATCH /api/v1/customers/profile
  - [ ] GET /api/v1/customers/bookings/upcoming?limit=5
  - [ ] GET /api/v1/customers/bookings/history?page=1&size=20
  - [ ] GET /api/v1/customers/favorites
  - [ ] POST /api/v1/customers/favorites/{providerId}
  - [ ] DELETE /api/v1/customers/favorites/{providerId}
  - [ ] GET /api/v1/customers/reviews
  - [ ] PATCH /api/v1/customers/reviews/{id}
  - [ ] GET /api/v1/customers/preferences
  - [ ] PATCH /api/v1/customers/preferences

- [ ] 6.2 Add DTOs
  - [ ] UpdateCustomerProfileRequest.cs
  - [ ] CustomerProfileResponse.cs
  - [ ] UpcomingBookingResponse.cs
  - [ ] BookingHistoryResponse.cs
  - [ ] FavoriteProviderResponse.cs
  - [ ] UpdatePreferencesRequest.cs

- [ ] 6.3 Document APIs in Swagger

- [ ] 6.4 Write integration tests
  - [ ] Test all 11 endpoints
  - [ ] Test validation errors
  - [ ] Test authorization (customer can only access own data)

---

## Week 2: Frontend Core Implementation

### 7. Project Setup (Day 1)

- [ ] 7.1 Create module structure
  - [ ] Create `booksy-frontend/src/modules/customer/` directory
  - [ ] Create subdirectories: components/, stores/, api/, types/

- [ ] 7.2 Create TypeScript types
  - [ ] types/customer.types.ts
  - [ ] CustomerProfile interface
  - [ ] UpcomingBooking interface
  - [ ] BookingHistoryEntry interface
  - [ ] FavoriteProvider interface
  - [ ] CustomerReview interface
  - [ ] NotificationPreferences interface

### 8. API Service (Day 1)

- [ ] 8.1 Create customer API service
  - [ ] api/customer.service.ts
  - [ ] getProfile()
  - [ ] updateProfile(data)
  - [ ] getUpcomingBookings(limit)
  - [ ] getBookingHistory(page, size)
  - [ ] getFavoriteProviders()
  - [ ] addFavoriteProvider(providerId)
  - [ ] removeFavoriteProvider(providerId)
  - [ ] getReviews()
  - [ ] updateReview(id, data)
  - [ ] getPreferences()
  - [ ] updatePreferences(data)

### 9. Pinia Store (Day 2)

- [ ] 9.1 Create customer store
  - [ ] stores/customer.store.ts
  - [ ] State: profile, upcomingBookings, bookingHistory, favorites, reviews, preferences, activeModal
  - [ ] Actions: fetchProfile, updateProfile, fetchUpcomingBookings, fetchBookingHistory
  - [ ] Actions: fetchFavorites, addFavorite, removeFavorite
  - [ ] Actions: fetchReviews, updateReview
  - [ ] Actions: fetchPreferences, updatePreferences
  - [ ] Actions: openModal, closeModal
  - [ ] Getters: userInitial, userColor

### 10. User Menu Dropdown (Day 2)

- [ ] 10.1 Create UserMenuDropdown component
  - [ ] components/UserMenuDropdown.vue
  - [ ] Display user initial circle (colored, with first letter)
  - [ ] Display full name and phone number
  - [ ] Menu items: Edit Profile, Bookings, Favorites, Reviews, Settings, Logout
  - [ ] RTL dropdown positioning
  - [ ] Click outside to close
  - [ ] ESC key to close

- [ ] 10.2 Integrate into AppHeader
  - [ ] Add UserMenuDropdown to header (right side for RTL)
  - [ ] Show "ورود / ثبت‌نام" button for guests
  - [ ] Show user menu for authenticated users

### 11. ProfileEditModal (Day 2)

- [ ] 11.1 Create ProfileEditModal component
  - [ ] components/ProfileEditModal.vue
  - [ ] Full name input (pre-filled)
  - [ ] Phone number display (disabled field)
  - [ ] Email input (optional, pre-filled)
  - [ ] Validation: name 3-100 chars, email format
  - [ ] Save/Cancel buttons
  - [ ] Success/error toasts
  - [ ] Modal overlay and animations

### 12. BookingsSidebar (Day 3)

- [ ] 12.1 Create BookingsSidebar component
  - [ ] components/BookingsSidebar.vue
  - [ ] Slide-in from left animation
  - [ ] Two tabs: "آینده" (Upcoming) and "گذشته" (History)
  - [ ] Tab switching logic

- [ ] 12.2 Create BookingCard component
  - [ ] components/BookingCard.vue
  - [ ] Display provider logo, name
  - [ ] Display service name
  - [ ] Display date/time (Jalali, Persian numbers)
  - [ ] Display status badge
  - [ ] Action buttons: Cancel, Reschedule (upcoming), Rebook (history)

- [ ] 12.3 Implement upcoming bookings tab
  - [ ] Fetch upcoming bookings (limit 5)
  - [ ] Display booking cards
  - [ ] Empty state: "شما نوبت آینده‌ای ندارید"

- [ ] 12.4 Implement history tab
  - [ ] Fetch booking history (paginated, 20 per page)
  - [ ] "بارگذاری بیشتر" button
  - [ ] Rebook functionality (redirect to booking wizard with pre-fill)

### 13. FavoritesModal and Favorite Button (Day 4)

- [ ] 13.1 Create FavoriteButton component
  - [ ] components/FavoriteButton.vue
  - [ ] Heart icon (outlined/filled based on favorite status)
  - [ ] Click toggles favorite
  - [ ] Confirmation dialog on remove
  - [ ] Toast notifications

- [ ] 13.2 Add FavoriteButton to provider cards
  - [ ] Integrate FavoriteButton into FeaturedProviders.vue
  - [ ] Position heart icon (top-right corner)
  - [ ] Fetch favorite status on mount

- [ ] 13.3 Create FavoritesModal component
  - [ ] components/FavoritesModal.vue
  - [ ] Grid layout (2 columns desktop, 1 mobile)
  - [ ] Display favorite provider cards
  - [ ] Each card: logo, name, category, rating, Quick Book button, Remove heart
  - [ ] Empty state: "شما هنوز ارائه‌دهنده‌ای را به علاقه‌مندی‌ها اضافه نکرده‌اید"

### 14. ReviewsModal (Day 4)

- [ ] 14.1 Create ReviewsModal component
  - [ ] components/ReviewsModal.vue
  - [ ] List of customer's reviews
  - [ ] Each review: provider logo/name, service, rating, text, date
  - [ ] Edit button (if review <7 days old)
  - [ ] Empty state: "شما هنوز نظری ثبت نکرده‌اید"

- [ ] 14.2 Create ReviewEditForm component
  - [ ] Inline edit form (or nested modal)
  - [ ] Star rating selector
  - [ ] Text area (max 500 chars)
  - [ ] Character counter
  - [ ] Save/Cancel buttons

### 15. SettingsModal (Day 5)

- [ ] 15.1 Create SettingsModal component
  - [ ] components/SettingsModal.vue
  - [ ] Notifications section
  - [ ] SMS notifications toggle
  - [ ] Email notifications toggle
  - [ ] Reminder timing dropdown ("۱ ساعت قبل", "۱ روز قبل", "۳ روز قبل")
  - [ ] Auto-save on change (with brief toast)
  - [ ] Account section with support contact info

- [ ] 15.2 Warning for disabling all notifications
  - [ ] Show confirmation dialog if both SMS and Email toggled off

### 16. Persian Translations (Day 5)

- [ ] 16.1 Add translations to fa.json
  - [ ] User menu items (~10 keys)
  - [ ] Profile edit modal (~10 keys)
  - [ ] Bookings sidebar (~15 keys)
  - [ ] Favorites modal (~8 keys)
  - [ ] Reviews modal (~10 keys)
  - [ ] Settings modal (~10 keys)
  - [ ] Toast messages (~10 keys)
  - [ ] Error messages (~10 keys)
  - [ ] Total: ~80 translation keys

### 17. Styling (Day 5 - Weekend)

- [ ] 17.1 Create customer profile styles
  - [ ] User menu dropdown styles (RTL positioning)
  - [ ] Modal overlay and animations
  - [ ] Sidebar slide-in animations
  - [ ] User initial circle colors
  - [ ] Responsive breakpoints (desktop, tablet, mobile)

---

## Week 3: Mobile, Integration & Deployment

### 18. Mobile Bottom Navigation (Day 1)

- [ ] 18.1 Create BottomNavigation component
  - [ ] components/BottomNavigation.vue
  - [ ] 5 tabs: Home, Search, Bookings, Favorites, Profile
  - [ ] Icons for each tab
  - [ ] Active tab highlighting
  - [ ] Fixed at bottom (only show on mobile <768px)

- [ ] 18.2 Create BottomSheet component
  - [ ] components/BottomSheet.vue
  - [ ] Swipe down to dismiss
  - [ ] Snap to half/full height
  - [ ] Overlay background

- [ ] 18.3 Convert modals to bottom sheets on mobile
  - [ ] ProfileEditModal → BottomSheet
  - [ ] BookingsSidebar → Full-screen BottomSheet
  - [ ] FavoritesModal → BottomSheet
  - [ ] ReviewsModal → BottomSheet
  - [ ] SettingsModal → BottomSheet

### 19. Integration with Booking Flow (Day 1-2)

- [ ] 19.1 Connect Cancel Booking action
  - [ ] Integrate with existing cancel booking API
  - [ ] Update booking status in sidebar
  - [ ] Remove from upcoming list

- [ ] 19.2 Connect Reschedule Booking action
  - [ ] Redirect to booking wizard with booking ID
  - [ ] Pre-fill provider, service, customer info
  - [ ] Allow date/time change

- [ ] 19.3 Connect Rebook action
  - [ ] Redirect to booking wizard `/booking/{providerId}`
  - [ ] Pre-fill provider and service from history
  - [ ] Allow customer to select new date/time

- [ ] 19.4 Connect Review Edit to Reviews API
  - [ ] Use existing review update endpoint
  - [ ] Validate 7-day edit window
  - [ ] Update review display after edit

### 20. Performance Optimization (Day 2-3)

- [ ] 20.1 Implement caching
  - [ ] Cache profile data in store (5 min TTL)
  - [ ] Cache upcoming bookings (2 min TTL)
  - [ ] Cache favorites (5 min TTL)
  - [ ] Invalidate cache on mutations

- [ ] 20.2 Lazy loading
  - [ ] Lazy load modal components (defineAsyncComponent)
  - [ ] Load modal content only when opened
  - [ ] Prefetch on hover (for desktop)

- [ ] 20.3 Optimize images
  - [ ] Lazy load provider logos
  - [ ] Use responsive images (different sizes for mobile/desktop)

- [ ] 20.4 Bundle optimization
  - [ ] Verify customer module is code-split
  - [ ] Check bundle size (<30KB target)

### 21. Testing (Day 3-4)

- [ ] 21.1 Unit tests for store
  - [ ] customer.store.spec.ts
  - [ ] Test all actions
  - [ ] Test state mutations

- [ ] 21.2 Unit tests for components
  - [ ] UserMenuDropdown.spec.ts
  - [ ] ProfileEditModal.spec.ts
  - [ ] BookingsSidebar.spec.ts
  - [ ] FavoriteButton.spec.ts

- [ ] 21.3 E2E tests (Cypress or Playwright)
  - [ ] User menu dropdown interaction
  - [ ] Profile edit flow
  - [ ] Add/remove favorite provider
  - [ ] View bookings and rebook
  - [ ] Edit review flow
  - [ ] Update notification preferences

- [ ] 21.4 Accessibility testing
  - [ ] Keyboard navigation (Tab, ESC, Enter)
  - [ ] Screen reader labels (ARIA)
  - [ ] Focus management in modals
  - [ ] Color contrast (WCAG AA)

### 22. Bug Fixes & Polish (Day 4-5)

- [ ] 22.1 Cross-browser testing
  - [ ] Chrome (desktop & mobile)
  - [ ] Safari (desktop & mobile)
  - [ ] Firefox
  - [ ] Edge

- [ ] 22.2 RTL layout verification
  - [ ] Check all modals/sidebars RTL
  - [ ] Verify Jalali dates
  - [ ] Verify Persian numbers
  - [ ] Check dropdown positioning

- [ ] 22.3 Loading states
  - [ ] Add skeleton screens for modals
  - [ ] Loading spinners for async actions
  - [ ] Disable buttons during API calls

- [ ] 22.4 Error handling
  - [ ] Toast notifications for all errors
  - [ ] Retry buttons for failed requests
  - [ ] Offline detection and messaging

### 23. Deployment (Day 5 - Weekend)

- [ ] 23.1 Staging deployment
  - [ ] Deploy backend to staging
  - [ ] Run database migrations on staging
  - [ ] Deploy frontend to staging
  - [ ] Smoke test all features

- [ ] 23.2 UAT (User Acceptance Testing)
  - [ ] Create test accounts
  - [ ] Test all workflows with stakeholders
  - [ ] Collect feedback
  - [ ] Fix critical issues

- [ ] 23.3 Production deployment
  - [ ] Create deployment plan
  - [ ] Schedule maintenance window (if needed)
  - [ ] Deploy database migrations to production
  - [ ] Deploy backend
  - [ ] Deploy frontend
  - [ ] Monitor error rates

- [ ] 23.4 Post-deployment monitoring
  - [ ] Monitor application logs
  - [ ] Track API response times
  - [ ] Monitor error rates
  - [ ] Check user adoption metrics

---

## Total: ~60 Tasks (vs 200+ in original proposal)

**Duration**: 3 weeks (vs 6 weeks)
**Complexity**: Minimal (vs Comprehensive)
**Focus**: Essential features only
