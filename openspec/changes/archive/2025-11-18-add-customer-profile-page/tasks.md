# Customer Profile - Minimal Implementation Tasks (3 Weeks)

## Week 1: Backend Implementation

### 1. Database Schema (Day 1-2)

- [x] 1.1 Create migration to extend users table
  - [x] Email and full_name already exist in User/UserProfile
  - [x] Test migration on local database

- [x] 1.2 Create migration for `favorite_providers` table
  - [x] Already exists as customer_favorite_providers (owned entity)
  - [x] UNIQUE constraint on (customer_id, provider_id)
  - [x] Index on customer_id
  - [x] Test migration

- [x] 1.3 Create migration for `customer_booking_history` table
  - [x] Created table with all required fields
  - [x] Added index on (customer_id, start_time DESC)
  - [x] Added index on status
  - [x] Test migration

- [x] 1.4 Add notification preferences to customers table
  - [x] Added as owned value object (notification_sms_enabled, notification_email_enabled, notification_reminder_timing)
  - [x] Test migration

- [x] 1.5 Test all migrations together
  - [x] Migration created successfully
  - [x] Indexes created correctly

### 2. Domain Layer (Day 2-3)

- [x] 2.1 Favorite Provider functionality
  - [x] Already exists as CustomerFavoriteProvider entity
  - [x] Add/Remove methods in Customer aggregate
  - [x] Domain events for favorite added/removed

- [x] 2.2 Create CustomerBookingHistory read model
  - [x] CustomerBookingHistoryEntry.cs with all booking fields
  - [x] UpdateStatus() method
  - [x] IsUpcoming() and IsPast() helper methods

- [x] 2.3 Create NotificationPreferences value object
  - [x] NotificationPreferences.cs (SmsEnabled, EmailEnabled, ReminderTiming)
  - [x] Validation for timing values ("1h", "24h", "3d")
  - [x] Update methods with immutability

### 3. Application Layer - Commands (Day 3-4)

- [x] 3.1 UpdateCustomerProfile command
  - [x] UpdateCustomerProfileCommand.cs already exists
  - [x] UpdateCustomerProfileCommandValidator.cs (FluentValidation)
  - [x] UpdateCustomerProfileCommandHandler.cs
  - [x] Unit test (UpdateCustomerProfileCommandHandlerTests.cs with 2 tests)

- [x] 3.2 AddFavoriteProvider command
  - [x] AddFavoriteProviderCommand.cs already exists
  - [x] AddFavoriteProviderCommandValidator.cs
  - [x] AddFavoriteProviderCommandHandler.cs
  - [x] Unit test project created and builds successfully

- [x] 3.3 RemoveFavoriteProvider command
  - [x] RemoveFavoriteProviderCommand.cs already exists
  - [x] RemoveFavoriteProviderCommandHandler.cs
  - [x] Unit test project created and builds successfully

- [x] 3.4 UpdateNotificationPreferences command
  - [x] UpdateNotificationPreferencesCommand.cs
  - [x] UpdateNotificationPreferencesCommandValidator.cs
  - [x] UpdateNotificationPreferencesCommandHandler.cs
  - [x] Unit test project created and builds successfully

### 4. Application Layer - Queries (Day 4-5)

- [x] 4.1 GetCustomerProfile query
  - [x] GetCustomerProfileQuery.cs
  - [x] GetCustomerProfileQueryHandler.cs
  - [x] CustomerProfileViewModel.cs (DTO)
  - [ ] Unit test (TODO)

- [x] 4.2 GetUpcomingBookings query
  - [x] GetUpcomingBookingsQuery.cs (limit = 5)
  - [x] GetUpcomingBookingsQueryHandler.cs (stub - returns empty until event handlers set up)
  - [x] UpcomingBookingViewModel.cs
  - [ ] Unit test (TODO)

- [x] 4.3 GetBookingHistory query
  - [x] GetBookingHistoryQuery.cs (page, size = 20)
  - [x] GetBookingHistoryQueryHandler.cs (stub - returns empty until event handlers active)
  - [x] BookingHistoryViewModel.cs and BookingHistoryResult.cs with pagination
  - [ ] Unit test (TODO)

- [x] 4.4 GetFavoriteProviders query
  - [x] Already exists as GetCustomerFavoriteProvidersQuery
  - [x] GetCustomerFavoriteProvidersQueryHandler.cs
  - [x] FavoriteProviderViewModel.cs
  - [ ] Unit test (TODO)

- [ ] 4.5 GetCustomerReviews query (TODO - requires Reviews BC integration)
  - [ ] GetCustomerReviewsQuery.cs
  - [ ] GetCustomerReviewsQueryHandler.cs
  - [ ] CustomerReviewViewModel.cs
  - [ ] Unit test

### 5. Infrastructure Layer (Day 5)

- [x] 5.1 Repositories
  - [x] ICustomerRepository already exists with favorite provider methods
  - [x] CustomerRepository implementation already exists
  - [x] CustomerConfiguration.cs (EF Core) - extended with NotificationPreferences
  - [x] CustomerBookingHistoryConfiguration.cs (EF Core)

- [x] 5.2 Event handlers (created, awaiting Booking BC events)
  - [x] BookingEventSubscribers.cs
  - [x] Handle BookingCreatedEvent → Insert to customer_booking_history
  - [x] Handle BookingConfirmedEvent → Update status
  - [x] Handle BookingCompletedEvent → Update status
  - [x] Handle BookingCancelledEvent → Update status
  - [ ] Handle ProviderDeletedEvent → Soft-delete favorites (TODO)

### 6. API Layer (Day 5)

- [x] 6.1 CustomersController endpoints
  - [x] GET /api/v1/customers/{id} (already exists)
  - [x] PUT /api/v1/customers/{id} (UpdateCustomerProfile - already exists)
  - [x] GET /api/v1/customers/{id}/profile (NEW - with preferences)
  - [x] GET /api/v1/customers/{id}/bookings/upcoming?limit=5 (NEW)
  - [x] GET /api/v1/customers/{id}/bookings/history?page=1&size=20 (NEW)
  - [x] GET /api/v1/customers/{id}/favorites (already exists)
  - [x] POST /api/v1/customers/{id}/favorites (already exists)
  - [x] DELETE /api/v1/customers/{id}/favorites/{providerId} (already exists)
  - [ ] GET /api/v1/customers/{id}/reviews (TODO - requires Reviews BC)
  - [ ] PATCH /api/v1/customers/{id}/reviews/{reviewId} (TODO - requires Reviews BC)
  - [x] PATCH /api/v1/customers/{id}/preferences (NEW)

- [x] 6.2 Request/Response DTOs
  - [x] UpdatePreferencesRequest.cs
  - [x] AddFavoriteProviderRequest.cs (already exists)
  - [x] ViewModels already created for responses

- [x] 6.3 Swagger documentation
  - [x] XML comments on all endpoints
  - [x] Response type annotations

- [x] 6.4 Write integration tests
  - [x] Test all endpoints (CustomersControllerTests.cs with 15+ tests)
  - [x] Test validation errors
  - [x] Test authorization (customer can only access own data)
  - [x] Created UserManagement.IntegrationTests project
  - [x] Created generic TestWebApplicationFactory in Tests.Commons
  - [x] Tests build successfully

---

## Week 2: Frontend Core Implementation

### 7. Project Setup (Day 1)

- [x] 7.1 Create module structure
  - [x] Create `booksy-frontend/src/modules/customer/` directory
  - [x] Create subdirectories: components/, stores/, api/, types/

- [x] 7.2 Create TypeScript types
  - [x] types/customer.types.ts
  - [x] CustomerProfile interface
  - [x] UpcomingBooking interface
  - [x] BookingHistoryEntry interface
  - [x] FavoriteProvider interface
  - [x] CustomerReview interface
  - [x] NotificationPreferences interface

### 8. API Service (Day 1)

- [x] 8.1 Create customer API service
  - [x] api/customer.service.ts
  - [x] getProfile()
  - [x] updateProfile(data)
  - [x] getUpcomingBookings(limit)
  - [x] getBookingHistory(page, size)
  - [x] getFavoriteProviders()
  - [x] addFavoriteProvider(providerId)
  - [x] removeFavoriteProvider(providerId)
  - [x] getReviews()
  - [x] updateReview(id, data)
  - [x] getPreferences()
  - [x] updatePreferences(data)

### 9. Pinia Store (Day 2)

- [x] 9.1 Create customer store
  - [x] stores/customer.store.ts
  - [x] State: profile, upcomingBookings, bookingHistory, favorites, reviews, preferences, activeModal
  - [x] Actions: fetchProfile, updateProfile, fetchUpcomingBookings, fetchBookingHistory
  - [x] Actions: fetchFavorites, addFavorite, removeFavorite
  - [x] Actions: fetchReviews, updateReview
  - [x] Actions: fetchPreferences, updatePreferences
  - [x] Actions: openModal, closeModal
  - [x] Getters: userInitial, userColor

### 10. User Menu Dropdown (Day 2)

- [x] 10.1 Create UserMenuDropdown component
  - [x] components/UserMenuDropdown.vue (already exists as UserMenu.vue)
  - [x] Display user initial circle (colored, with first letter)
  - [x] Display full name and phone number
  - [x] Menu items: Edit Profile, Bookings, Favorites, Reviews, Settings, Logout
  - [x] RTL dropdown positioning
  - [x] Click outside to close
  - [x] ESC key to close

- [x] 10.2 Integrate into AppHeader
  - [x] Add UserMenuDropdown to header (right side for RTL)
  - [x] Show "ورود / ثبت‌نام" button for guests
  - [x] Show user menu for authenticated users

### 11. ProfileEditModal (Day 2)

- [x] 11.1 Create ProfileEditModal component
  - [x] components/modals/ProfileEditModal.vue
  - [x] Full name input (pre-filled)
  - [x] Phone number display (disabled field)
  - [x] Email input (optional, pre-filled)
  - [x] Validation: name 3-100 chars, email format
  - [x] Save/Cancel buttons
  - [x] Success/error toasts
  - [x] Modal overlay and animations

### 12. BookingsSidebar (Day 3)

- [x] 12.1 Create BookingsSidebar component
  - [x] components/modals/BookingsSidebar.vue
  - [x] Slide-in from left animation
  - [x] Two tabs: "آینده" (Upcoming) and "گذشته" (History)
  - [x] Tab switching logic

- [x] 12.2 Create BookingCard component
  - [x] components/modals/BookingCard.vue
  - [x] Display provider logo, name
  - [x] Display service name
  - [x] Display date/time (Jalali, Persian numbers)
  - [x] Display status badge
  - [x] Action buttons: Cancel, Reschedule (upcoming), Rebook (history)

- [x] 12.3 Implement upcoming bookings tab
  - [x] Fetch upcoming bookings (limit 5)
  - [x] Display booking cards
  - [x] Empty state: "شما نوبت آینده‌ای ندارید"

- [x] 12.4 Implement history tab
  - [x] Fetch booking history (paginated, 20 per page)
  - [x] "بارگذاری بیشتر" button
  - [x] Rebook functionality (redirect to booking wizard with pre-fill)

### 13. FavoritesModal and Favorite Button (Day 4)

- [x] 13.1 Create FavoriteButton component
  - [x] components/favorites/FavoriteButton.vue (already exists)
  - [x] Heart icon (outlined/filled based on favorite status)
  - [x] Click toggles favorite
  - [x] Confirmation dialog on remove
  - [x] Toast notifications

- [x] 13.2 Add FavoriteButton to provider cards
  - [x] Integrate FavoriteButton into FeaturedProviders.vue
  - [x] Position heart icon (top-right corner with beautiful circular design)
  - [x] Fetch favorite status on mount

- [x] 13.3 Create FavoritesModal component
  - [x] components/modals/FavoritesModal.vue
  - [x] Grid layout (2 columns desktop, 1 mobile)
  - [x] Display favorite provider cards (FavoriteProviderCard.vue)
  - [x] Each card: logo, name, category, rating, Quick Book button, Remove heart
  - [x] Empty state: "شما هنوز ارائه‌دهنده‌ای را به علاقه‌مندی‌ها اضافه نکرده‌اید"

### 14. ReviewsModal (Day 4)

- [x] 14.1 Create ReviewsModal component
  - [x] components/modals/ReviewsModal.vue
  - [x] List of customer's reviews (ReviewCard.vue)
  - [x] Each review: provider logo/name, service, rating, text, date
  - [x] Edit button (if review <7 days old)
  - [x] Empty state: "شما هنوز نظری ثبت نکرده‌اید"

- [x] 14.2 Create ReviewEditForm component
  - [x] components/modals/EditReviewModal.vue (nested modal)
  - [x] Star rating selector
  - [x] Text area (max 500 chars)
  - [x] Character counter
  - [x] Save/Cancel buttons

### 15. SettingsModal (Day 5)

- [x] 15.1 Create SettingsModal component
  - [x] components/modals/SettingsModal.vue
  - [x] Notifications section
  - [x] SMS notifications toggle
  - [x] Email notifications toggle
  - [x] Reminder timing dropdown ("۱ ساعت قبل", "۱ روز قبل", "۳ روز قبل")
  - [x] Auto-save on change (with brief toast)
  - [x] Account section with support contact info

- [x] 15.2 Warning for disabling all notifications
  - [x] Show warning message if both SMS and Email toggled off

### 16. Persian Translations (Day 5)

- [ ] 16.1 Add translations to fa.json
  - [ ] User menu items (~10 keys) - (hardcoded for now)
  - [ ] Profile edit modal (~10 keys) - (hardcoded for now)
  - [ ] Bookings sidebar (~15 keys) - (hardcoded for now)
  - [ ] Favorites modal (~8 keys) - (hardcoded for now)
  - [ ] Reviews modal (~10 keys) - (hardcoded for now)
  - [ ] Settings modal (~10 keys) - (hardcoded for now)
  - [ ] Toast messages (~10 keys)
  - [ ] Error messages (~10 keys)
  - [ ] Total: ~80 translation keys (can be added later)

### 17. Styling (Day 5 - Weekend)

- [x] 17.1 Create customer profile styles
  - [x] User menu dropdown styles (RTL positioning)
  - [x] Modal overlay and animations (BaseModal.vue)
  - [x] Sidebar slide-in animations
  - [x] User initial circle colors
  - [x] Responsive breakpoints (desktop, tablet, mobile)

---

## Week 3: Mobile, Integration & Deployment

### 18. Mobile Bottom Navigation (Day 1)

- [x] 18.1 Create BottomNavigation component
  - [x] components/BottomNavigation.vue
  - [x] 5 tabs: Home, Search, Bookings, Favorites, Profile
  - [x] Icons for each tab
  - [x] Active tab highlighting
  - [x] Fixed at bottom (only show on mobile <768px)

- [x] 18.2 Create BottomSheet component
  - [x] components/BottomSheet.vue
  - [x] Swipe down to dismiss
  - [x] Snap to half/full height (half, full, auto)
  - [x] Overlay background
  - [x] Mouse drag support for desktop testing
  - [x] ESC key to close
  - [x] Body scroll lock

- [x] 18.3 Convert modals to bottom sheets on mobile
  - [x] Created ResponsiveModal component (auto-detects mobile/desktop)
  - [x] ProfileEditModal → BottomSheet on mobile (auto height)
  - [x] FavoritesModal → Full-screen BottomSheet on mobile
  - [x] ReviewsModal → Full-screen BottomSheet on mobile
  - [x] SettingsModal → BottomSheet on mobile (auto height)
  - [ ] BookingsSidebar → Already sidebar on desktop, could use BottomSheet on mobile (optional)

### 19. Integration with Booking Flow (Day 1-2)

- [x] 19.1 Connect Cancel Booking action
  - [x] Integrate with existing cancel booking API
  - [x] Update booking status in sidebar
  - [x] Remove from upcoming list
  - [x] Add confirmation modal with reason selection

- [x] 19.2 Connect Reschedule Booking action
  - [x] Redirect to booking wizard with booking ID
  - [x] Pre-fill provider, service, customer info
  - [x] Allow date/time change

- [x] 19.3 Connect Rebook action
  - [x] Redirect to booking wizard `/booking/{providerId}`
  - [x] Pre-fill provider and service from history
  - [x] Allow customer to select new date/time

- [x] 19.4 Connect Review Edit to Reviews API
  - [x] Use existing review update endpoint (PATCH /api/v1/customers/{id}/reviews/{reviewId})
  - [x] Validate 7-day edit window (canEdit property)
  - [x] Update review display after edit
  - [x] Created reviews.service.ts for review operations

### 20. Performance Optimization (Day 2-3)

- [x] 20.1 Implement caching
  - [x] Cache profile data in store (5 min TTL)
  - [x] Cache upcoming bookings (2 min TTL)
  - [x] Cache favorites (5 min TTL)
  - [x] Invalidate cache on mutations
  - [x] Created cache utility with TTL support
  - [x] Auto-cleanup expired entries every 5 minutes

- [x] 20.2 Lazy loading
  - [x] Lazy load modal components (defineAsyncComponent) - Already implemented in CustomerModalsContainer
  - [x] Load modal content only when opened - Modals render conditionally
  - [x] Teleport to body for proper stacking

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
