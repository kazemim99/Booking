# Implementation Tasks

## Overview

This document breaks down the customer mobile app implementation into ordered, verifiable tasks. Tasks are grouped by phase and include dependencies, validation criteria, and estimated effort.

**Total Effort Estimate**: ~560 hours (14 weeks for 1 full-time Flutter developer)

---

## Phase 1: Foundation & Design System (2 weeks, ~80 hours)

### 1.1 Project Setup & Configuration

- [ ] **Task 1.1.1**: Review and validate existing Flutter project structure
  - **Validation**: Run `flutter doctor`, all checks pass
  - **Dependencies**: None
  - **Effort**: 1 hour

- [x] **Task 1.1.2**: Update `pubspec.yaml` with required dependencies
  - Add: `go_router` (navigation), `cached_network_image` (image caching)
  - Add: `shamsi_date` (Jalali calendar), `intl` (number formatting)
  - Add: `shimmer` (skeleton loaders), `pull_to_refresh` (refresh gestures)
  - **Validation**: `flutter pub get` succeeds, no conflicts
  - **Dependencies**: Task 1.1.1
  - **Effort**: 2 hours
  - **Status**: ✅ Completed - Added shamsi_date and pull_to_refresh

- [x] **Task 1.1.3**: Configure code generation
  - Verify `build_runner`, `retrofit_generator`, `injectable_generator` setup
  - Update `build.yaml` if needed
  - Run: `flutter pub run build_runner build`
  - **Validation**: All generated files created without errors
  - **Dependencies**: Task 1.1.2
  - **Effort**: 2 hours
  - **Status**: ✅ Completed - Code generation successful

### 1.2 Design System Implementation

- [x] **Task 1.2.1**: Create color palette constants
  - File: `lib/config/theme/app_colors.dart`
  - Define: Primary (#1A365D), neutral palette, semantic colors
  - Support light/dark modes
  - **Validation**: Colors compile, no hardcoded color values elsewhere
  - **Dependencies**: Task 1.1.2
  - **Effort**: 2 hours
  - **Status**: ✅ Completed - Professional color palette created

- [x] **Task 1.2.2**: Implement typography system
  - File: `lib/config/theme/app_text_styles.dart`
  - Configure Vazir font weights (regular, medium, semibold, bold)
  - Create text styles: h1, h2, h3, body, caption, small
  - Use ScreenUtil for responsive sizing
  - **Validation**: Text renders correctly in RTL, all weights load
  - **Dependencies**: Task 1.2.1
  - **Effort**: 3 hours
  - **Status**: ✅ Completed - Typography system with Vazir font

- [ ] **Task 1.2.3**: Build reusable button components
  - Files: `lib/shared/components/buttons/`
  - Implement: `PrimaryButton`, `SecondaryButton`, `TextButton`
  - Flat design (elevation: 0), 48dp min height, 12dp border radius
  - Loading states, disabled states
  - **Validation**: Buttons match design spec, pass accessibility tests
  - **Dependencies**: Task 1.2.1, 1.2.2
  - **Effort**: 4 hours

- [ ] **Task 1.2.4**: Build card components
  - Files: `lib/shared/components/cards/`
  - Implement: `BaseCard`, `ProviderCard`, `BookingCard`
  - 1px border, 16dp radius, no shadows (flat)
  - **Validation**: Cards render with correct spacing, borders
  - **Dependencies**: Task 1.2.1, 1.2.2
  - **Effort**: 4 hours

- [ ] **Task 1.2.5**: Build input field components
  - Files: `lib/shared/components/inputs/`
  - Implement: `AppTextField`, `AppSearchField`
  - RTL support, focus states (dark blue border), error states (red)
  - **Validation**: Inputs work with Persian text, keyboard types correct
  - **Dependencies**: Task 1.2.1, 1.2.2
  - **Effort**: 5 hours

- [ ] **Task 1.2.6**: Create loading and empty state components
  - Files: `lib/shared/components/states/`
  - Implement: `SkeletonLoader`, `EmptyState`, `ErrorState`
  - Shimmer effect for skeletons, illustrations for empty states
  - **Validation**: States display correctly, animations smooth (60 FPS)
  - **Dependencies**: Task 1.2.1
  - **Effort**: 4 hours

### 1.3 Navigation Setup

- [ ] **Task 1.3.1**: Configure GoRouter routes
  - File: `lib/config/routes/app_router.dart`
  - Define routes: `/splash`, `/login`, `/otp`, `/home`, `/search`, `/booking/{id}`, etc.
  - Implement route guards for auth protection
  - **Validation**: Navigation works, deep links resolve
  - **Dependencies**: Task 1.1.2
  - **Effort**: 6 hours

- [ ] **Task 1.3.2**: Build bottom navigation scaffold
  - File: `lib/shared/layouts/main_scaffold.dart`
  - 4 tabs: Home, Search, Bookings, Profile
  - Dark blue active state, gray inactive
  - **Validation**: Tab switching works, active tab highlights correctly
  - **Dependencies**: Task 1.2.1, 1.3.1
  - **Effort**: 4 hours

### 1.4 Utilities and Helpers

- [x] **Task 1.4.1**: Implement Persian number formatter
  - File: `lib/core/utils/persian_formatter.dart`
  - Extension: `String.toPersianDigits()`, `int.toPersianString()`
  - **Validation**: "123" → "۱۲۳", prices formatted correctly
  - **Dependencies**: None
  - **Effort**: 2 hours
  - **Status**: ✅ Completed - Persian number formatting with extensions

- [x] **Task 1.4.2**: Implement Jalali date formatter
  - File: `lib/core/utils/date_formatter.dart`
  - Use `shamsi_date` package
  - Format: "چهارشنبه، ۱۲ فروردین ۱۴۰۳"
  - **Validation**: Dates convert correctly, weekday names in Persian
  - **Dependencies**: Task 1.1.2
  - **Effort**: 3 hours
  - **Status**: ✅ Completed - Jalali calendar formatting

- [x] **Task 1.4.3**: Implement price formatter
  - File: `lib/core/utils/price_formatter.dart`
  - Format: 450000 → "۴۵۰٬۰۰۰ تومان"
  - Thousands separator, Persian digits
  - **Validation**: Prices display correctly in cards
  - **Dependencies**: Task 1.4.1
  - **Effort**: 2 hours
  - **Status**: ✅ Completed - Price formatting with Persian separators

- [ ] **Task 1.4.4**: Create user initial color generator
  - File: `lib/core/utils/user_color.dart`
  - Hash user ID to select from 4-color palette
  - Consistent color for same user
  - **Validation**: Same user always gets same color
  - **Dependencies**: Task 1.2.1
  - **Effort**: 2 hours

---

## Phase 2: Search & Discovery (3 weeks, ~120 hours)

### 2.1 Home Screen

- [ ] **Task 2.1.1**: Build home page layout
  - File: `lib/features/home/presentation/pages/home_page.dart`
  - Implement: Greeting, search bar, location indicator, sections
  - Pull-to-refresh
  - **Validation**: Layout matches design, RTL correct
  - **Dependencies**: Phase 1 complete
  - **Effort**: 6 hours

- [ ] **Task 2.1.2**: Implement popular categories widget
  - File: `lib/features/home/presentation/widgets/popular_categories.dart`
  - Horizontal scroll, circular icons, category labels
  - Fetch: `GET /api/v1/Categories/popular`
  - **Validation**: Categories load, tap navigates to search with filter
  - **Dependencies**: Task 2.1.1
  - **Effort**: 8 hours

- [ ] **Task 2.1.3**: Implement upcoming bookings widget
  - File: `lib/features/home/presentation/widgets/upcoming_bookings_widget.dart`
  - Display next 2 bookings
  - Fetch: `GET /api/v1/Bookings/my-bookings?filter=upcoming&limit=2`
  - Quick actions: Details, Directions
  - **Validation**: Bookings display, actions work
  - **Dependencies**: Task 2.1.1, booking API service
  - **Effort**: 10 hours

- [ ] **Task 2.1.4**: Implement top providers widget
  - File: `lib/features/home/presentation/widgets/top_providers.dart`
  - Horizontal scroll, provider cards
  - Fetch: `POST /api/v1/Providers/search` with filters
  - **Validation**: Providers load, tap navigates to details
  - **Dependencies**: Task 2.1.1, search API service
  - **Effort**: 10 hours

- [ ] **Task 2.1.5**: Add promotions banner (optional)
  - File: `lib/features/home/presentation/widgets/promotions_banner.dart`
  - Swipeable carousel with auto-advance (5s)
  - **Validation**: Slides transition smoothly
  - **Dependencies**: Task 2.1.1
  - **Effort**: 6 hours

- [x] **Task 2.1.6**: Implement home BLoC
  - Files: `lib/features/home/presentation/bloc/`
  - Events: LoadHomeData, RefreshHome
  - States: Loading, Success, Error
  - **Validation**: State management works, data fetches correctly
  - **Dependencies**: Tasks 2.1.2, 2.1.3, 2.1.4
  - **Effort**: 8 hours
  - **Status**: ✅ Completed - BLoC with domain/data layers complete

### 2.2 Search Functionality

- [ ] **Task 2.2.1**: Create search API service
  - File: `lib/features/search/data/datasources/search_api_service.dart`
  - Retrofit endpoints for provider search, categories, locations
  - **Validation**: API calls succeed, DTOs serialize correctly
  - **Dependencies**: Phase 1 core API setup
  - **Effort**: 6 hours

- [ ] **Task 2.2.2**: Implement search repository
  - Files: `lib/features/search/data/repositories/search_repository_impl.dart`
  - Map DTOs to domain entities
  - Cache search results (5min TTL)
  - **Validation**: Repository returns entities, caching works
  - **Dependencies**: Task 2.2.1
  - **Effort**: 6 hours

- [ ] **Task 2.2.3**: Build search page layout
  - File: `lib/features/search/presentation/pages/search_page.dart`
  - Search input, location chip, category chips, filter button
  - Provider results list
  - **Validation**: Layout renders, RTL correct
  - **Dependencies**: Phase 1 design system
  - **Effort**: 8 hours

- [ ] **Task 2.2.4**: Implement debounced search input
  - File: `lib/features/search/presentation/widgets/search_input.dart`
  - 500ms debounce, cancel previous requests
  - **Validation**: Search only fires after 500ms idle, no request spam
  - **Dependencies**: Task 2.2.3
  - **Effort**: 4 hours

- [ ] **Task 2.2.5**: Implement category filter chips
  - File: `lib/features/search/presentation/widgets/category_chips.dart`
  - Multi-select chips, dark blue fill when active
  - **Validation**: Chips toggle correctly, filters apply
  - **Dependencies**: Task 2.2.3
  - **Effort**: 5 hours

- [ ] **Task 2.2.6**: Implement location filter picker
  - File: `lib/features/search/presentation/widgets/location_picker.dart`
  - City/district selection, GPS location option
  - **Validation**: Location updates, search re-runs
  - **Dependencies**: Task 2.2.3
  - **Effort**: 8 hours

- [ ] **Task 2.2.7**: Build advanced filters panel
  - File: `lib/features/search/presentation/widgets/filters_panel.dart`
  - Bottom sheet with rating, price, distance, availability filters
  - **Validation**: Filters apply, badge shows active filter count
  - **Dependencies**: Task 2.2.3
  - **Effort**: 10 hours

- [ ] **Task 2.2.8**: Implement provider result cards
  - File: `lib/features/search/presentation/widgets/provider_result_card.dart`
  - Thumbnail, name, rating, distance, price, status, favorite icon
  - **Validation**: Cards render correctly, favorite toggles work
  - **Dependencies**: Task 2.2.3, favorite API
  - **Effort**: 8 hours

- [ ] **Task 2.2.9**: Implement infinite scroll pagination
  - Trigger at list bottom, fetch next page, append results
  - **Validation**: Pages load seamlessly, no duplicate items
  - **Dependencies**: Task 2.2.8
  - **Effort**: 6 hours

- [ ] **Task 2.2.10**: Implement search BLoC
  - Files: `lib/features/search/presentation/bloc/`
  - Events: SearchProviders, ApplyFilters, LoadMore, ToggleFavorite
  - States: Loading, Success, Error, Empty
  - **Validation**: Search flow works end-to-end
  - **Dependencies**: Task 2.2.2, all search widgets
  - **Effort**: 10 hours

- [ ] **Task 2.2.11**: Add recent searches feature
  - Store last 5 searches in local storage
  - Display when search input is empty
  - **Validation**: Recent searches display, tap executes search
  - **Dependencies**: Task 2.2.3
  - **Effort**: 4 hours

### 2.3 Provider Detail Page

- [ ] **Task 2.3.1**: Build provider detail page layout
  - File: `lib/features/provider/presentation/pages/provider_detail_page.dart`
  - Gallery, provider info, services, staff, reviews sections
  - **Validation**: Layout renders, sections scroll smoothly
  - **Dependencies**: Phase 1 design system
  - **Effort**: 10 hours

- [ ] **Task 2.3.2**: Implement provider gallery
  - Swipeable image carousel, tap to fullscreen
  - **Validation**: Images load, swipe transitions smooth
  - **Dependencies**: Task 2.3.1
  - **Effort**: 6 hours

- [ ] **Task 2.3.3**: Implement services list
  - Display provider services with pricing, duration
  - "انتخاب" button opens booking wizard
  - **Validation**: Services load, tap opens booking
  - **Dependencies**: Task 2.3.1
  - **Effort**: 6 hours

- [ ] **Task 2.3.4**: Implement reviews section
  - Display recent reviews, rating breakdown
  - **Validation**: Reviews load, pagination works
  - **Dependencies**: Task 2.3.1
  - **Effort**: 6 hours

- [ ] **Task 2.3.5**: Add sticky "رزرو نوبت" button
  - Fixed at bottom, scrolls with page
  - **Validation**: Button always visible, tap opens booking
  - **Dependencies**: Task 2.3.1
  - **Effort**: 3 hours

---

## Phase 3: Booking Flow (3 weeks, ~120 hours)

### 3.1 Booking Wizard Foundation

- [ ] **Task 3.1.1**: Create booking API service
  - File: `lib/features/booking/data/datasources/booking_api_service.dart`
  - Endpoints: services, staff, availability, create booking
  - **Validation**: API calls work, DTOs serialize
  - **Dependencies**: Phase 1 core API
  - **Effort**: 6 hours

- [ ] **Task 3.1.2**: Implement booking repository
  - File: `lib/features/booking/data/repositories/booking_repository_impl.dart`
  - Handle booking creation, validation, errors
  - **Validation**: Repository methods return correct entities/failures
  - **Dependencies**: Task 3.1.1
  - **Effort**: 6 hours

- [ ] **Task 3.1.3**: Build booking wizard scaffold
  - File: `lib/features/booking/presentation/pages/booking_wizard_page.dart`
  - Step indicator, progress bar, back navigation
  - **Validation**: Wizard navigation works, progress updates
  - **Dependencies**: Phase 1 navigation
  - **Effort**: 8 hours

- [ ] **Task 3.1.4**: Implement booking state management
  - File: `lib/features/booking/domain/entities/booking_state.dart`
  - Store: selected services, staff, date/time, notes
  - Persist to local storage (30min expiry)
  - **Validation**: State persists across steps, restores on app reopen
  - **Dependencies**: None
  - **Effort**: 6 hours

### 3.2 Step 1: Service Selection

- [ ] **Task 3.2.1**: Build service selection page
  - File: `lib/features/booking/presentation/pages/steps/service_selection_step.dart`
  - Display services list with checkboxes
  - Summary footer with total price/duration
  - **Validation**: Services load, multi-select works, summary updates
  - **Dependencies**: Task 3.1.3
  - **Effort**: 8 hours

- [ ] **Task 3.2.2**: Implement service selection logic
  - Add to booking state, calculate totals, enable "ادامه"
  - **Validation**: Selected services stored, totals correct
  - **Dependencies**: Task 3.2.1, Task 3.1.4
  - **Effort**: 4 hours

### 3.3 Step 2: Staff Selection

- [ ] **Task 3.3.1**: Build staff selection page
  - File: `lib/features/booking/presentation/pages/steps/staff_selection_step.dart`
  - "فرقی ندارد" option (default), staff cards with photos/ratings
  - **Validation**: Staff loads, selection works, auto-skips if solo provider
  - **Dependencies**: Task 3.1.3
  - **Effort**: 8 hours

- [ ] **Task 3.3.2**: Implement staff selection logic
  - Store staff ID or null (any available)
  - **Validation**: Staff stored in booking state
  - **Dependencies**: Task 3.3.1, Task 3.1.4
  - **Effort**: 3 hours

### 3.4 Step 3: Date and Time Selection

- [ ] **Task 3.4.1**: Implement Jalali calendar widget
  - File: `lib/features/booking/presentation/widgets/jalali_calendar.dart`
  - Month view, RTL weekday headers, selectable dates
  - Highlight available dates (green dot)
  - **Validation**: Calendar displays correctly, dates selectable
  - **Dependencies**: Task 1.4.2 (Jalali formatter)
  - **Effort**: 12 hours

- [ ] **Task 3.4.2**: Build time slots grid
  - File: `lib/features/booking/presentation/widgets/time_slots_grid.dart`
  - Fetch availability: `GET /api/v1/Availability/slots`
  - Display available (green), booked (gray strikethrough), past (disabled)
  - **Validation**: Slots load correctly, selection highlights
  - **Dependencies**: Task 3.1.1
  - **Effort**: 10 hours

- [ ] **Task 3.4.3**: Build date/time selection page
  - File: `lib/features/booking/presentation/pages/steps/datetime_selection_step.dart`
  - Combine calendar + time slots
  - Show selected date/time summary
  - **Validation**: Date and time selection works end-to-end
  - **Dependencies**: Task 3.4.1, Task 3.4.2, Task 3.1.3
  - **Effort**: 8 hours

- [ ] **Task 3.4.4**: Implement availability checking logic
  - Real-time slot validation on "ادامه" click
  - Handle conflicts (slot taken by another user)
  - **Validation**: Conflicts detected, user prompted to select new slot
  - **Dependencies**: Task 3.4.3
  - **Effort**: 6 hours

### 3.5 Step 4: Review and Confirmation

- [ ] **Task 3.5.1**: Build review page
  - File: `lib/features/booking/presentation/pages/steps/review_confirmation_step.dart`
  - Display all booking details, notes field, policy checkbox
  - **Validation**: All data displays correctly, notes save
  - **Dependencies**: Task 3.1.3, Task 3.1.4
  - **Effort**: 10 hours

- [ ] **Task 3.5.2**: Implement booking creation
  - Submit to: `POST /api/v1/Bookings`
  - Handle success/failure responses
  - **Validation**: Booking creates successfully, errors handled
  - **Dependencies**: Task 3.5.1, Task 3.1.2
  - **Effort**: 8 hours

- [ ] **Task 3.5.3**: Build confirmation page
  - File: `lib/features/booking/presentation/pages/booking_confirmation_page.dart`
  - Success animation, booking reference, quick actions
  - **Validation**: Confirmation displays, actions work (calendar, share)
  - **Dependencies**: Task 3.5.2
  - **Effort**: 6 hours

- [ ] **Task 3.5.4**: Implement calendar integration
  - Add event to device calendar (iOS/Android native)
  - **Validation**: Event added with correct details
  - **Dependencies**: Task 3.5.3
  - **Effort**: 6 hours

### 3.6 Booking BLoC

- [ ] **Task 3.6.1**: Implement booking BLoC
  - Files: `lib/features/booking/presentation/bloc/`
  - Events: SelectService, SelectStaff, SelectDateTime, ConfirmBooking
  - States: StepLoading, StepSuccess, BookingCreated, Error
  - **Validation**: State management works across all steps
  - **Dependencies**: All Step tasks, Task 3.1.2
  - **Effort**: 12 hours

---

## Phase 4: Bookings Management (2 weeks, ~80 hours)

### 4.1 Bookings List

- [ ] **Task 4.1.1**: Build bookings page with tabs
  - File: `lib/features/bookings/presentation/pages/bookings_page.dart`
  - Tabs: Upcoming, Past, Cancelled
  - Pull-to-refresh
  - **Validation**: Tabs switch, data fetches for each tab
  - **Dependencies**: Phase 1 design system
  - **Effort**: 8 hours

- [ ] **Task 4.1.2**: Implement booking card component
  - File: `lib/features/bookings/presentation/widgets/booking_card.dart`
  - Display booking details, status badge, action buttons
  - **Validation**: Cards render correctly, badges colored properly
  - **Dependencies**: Task 4.1.1
  - **Effort**: 6 hours

- [ ] **Task 4.1.3**: Implement empty states for each tab
  - Different messages/actions per tab (upcoming, past, cancelled)
  - **Validation**: Empty states display correctly
  - **Dependencies**: Task 4.1.1
  - **Effort**: 4 hours

- [ ] **Task 4.1.4**: Implement bookings pagination
  - Infinite scroll, load more on bottom reached
  - **Validation**: Pages load seamlessly
  - **Dependencies**: Task 4.1.2
  - **Effort**: 6 hours

### 4.2 Booking Details

- [ ] **Task 4.2.1**: Build booking detail page
  - File: `lib/features/bookings/presentation/pages/booking_detail_page.dart`
  - Comprehensive booking info, quick actions
  - **Validation**: All details display correctly
  - **Dependencies**: Phase 1 design system
  - **Effort**: 10 hours

- [ ] **Task 4.2.2**: Implement directions integration
  - Open maps app (Google Maps / Neshan Maps)
  - **Validation**: Maps app opens with destination
  - **Dependencies**: Task 4.2.1
  - **Effort**: 4 hours

- [ ] **Task 4.2.3**: Implement phone call integration
  - Open phone dialer with provider number
  - **Validation**: Dialer opens pre-filled
  - **Dependencies**: Task 4.2.1
  - **Effort**: 2 hours

### 4.3 Cancel Booking

- [ ] **Task 4.3.1**: Build cancellation dialog
  - Confirmation, policy reminder, reason dropdown
  - **Validation**: Dialog displays, policy shows time-based info
  - **Dependencies**: Task 4.2.1
  - **Effort**: 6 hours

- [ ] **Task 4.3.2**: Implement cancel API call
  - POST to `/api/v1/Bookings/{id}/cancel`
  - Handle success/failure
  - **Validation**: Booking cancelled, moved to cancelled tab
  - **Dependencies**: Task 4.3.1
  - **Effort**: 4 hours

### 4.4 Reschedule Booking

- [ ] **Task 4.4.1**: Build reschedule page
  - Date/time picker (reuse from booking wizard)
  - Show current vs new time comparison
  - **Validation**: New time selectable, comparison displays
  - **Dependencies**: Task 4.2.1, Task 3.4.1, Task 3.4.2
  - **Effort**: 8 hours

- [ ] **Task 4.4.2**: Implement reschedule API call
  - POST to `/api/v1/Bookings/{id}/reschedule`
  - Handle conflicts
  - **Validation**: Booking time updated
  - **Dependencies**: Task 4.4.1
  - **Effort**: 6 hours

### 4.5 Rebook and Review

- [ ] **Task 4.5.1**: Implement rebook flow
  - Pre-fill booking wizard with past booking data
  - Skip to date/time selection
  - **Validation**: Previous service/staff pre-filled
  - **Dependencies**: Task 4.1.2, Phase 3 booking wizard
  - **Effort**: 6 hours

- [ ] **Task 4.5.2**: Build review submission modal
  - Star rating, text area, character counter
  - **Validation**: Review submits, form validates
  - **Dependencies**: Task 4.1.2
  - **Effort**: 6 hours

- [ ] **Task 4.5.3**: Implement review API calls
  - POST `/api/v1/Reviews` (create)
  - **Validation**: Review saved, "ثبت نظر" button hides
  - **Dependencies**: Task 4.5.2
  - **Effort**: 4 hours

### 4.6 Bookings BLoC

- [ ] **Task 4.6.1**: Implement bookings BLoC
  - Files: `lib/features/bookings/presentation/bloc/`
  - Events: LoadBookings, CancelBooking, RescheduleBooking, SubmitReview
  - States: Loading, Success, Error for each operation
  - **Validation**: All booking operations work
  - **Dependencies**: All bookings management tasks
  - **Effort**: 10 hours

---

## Phase 5: Profile & Settings (2 weeks, ~80 hours)

### 5.1 Profile Page

- [ ] **Task 5.1.1**: Build profile page layout
  - File: `lib/features/profile/presentation/pages/profile_page.dart`
  - Header with user initial, sections for account/settings/support
  - **Validation**: Layout renders correctly, RTL correct
  - **Dependencies**: Phase 1 design system
  - **Effort**: 8 hours

- [ ] **Task 5.1.2**: Implement user initial circle
  - Color based on user ID hash
  - Display first character of name
  - **Validation**: Circle displays, color consistent for user
  - **Dependencies**: Task 5.1.1, Task 1.4.4
  - **Effort**: 4 hours

### 5.2 Edit Profile

- [ ] **Task 5.2.1**: Build edit profile page
  - File: `lib/features/profile/presentation/pages/edit_profile_page.dart`
  - Name, last name, email inputs (phone read-only)
  - **Validation**: Form renders, phone disabled
  - **Dependencies**: Task 5.1.1
  - **Effort**: 6 hours

- [ ] **Task 5.2.2**: Implement profile update logic
  - PUT `/api/v1/Customers/profile`
  - Client-side validation, server error handling
  - **Validation**: Profile updates, errors display
  - **Dependencies**: Task 5.2.1
  - **Effort**: 6 hours

### 5.3 Favorites Management

- [ ] **Task 5.3.1**: Build favorites page
  - File: `lib/features/profile/presentation/pages/favorites_page.dart`
  - Grid layout (2 columns), provider cards
  - **Validation**: Favorites load, grid responsive
  - **Dependencies**: Task 5.1.1
  - **Effort**: 8 hours

- [ ] **Task 5.3.2**: Implement remove favorite
  - DELETE `/api/v1/Customers/favorites/{providerId}`
  - Confirmation dialog, fade-out animation
  - **Validation**: Favorite removed, count updates
  - **Dependencies**: Task 5.3.1
  - **Effort**: 4 hours

- [ ] **Task 5.3.3**: Implement favorites empty state
  - **Validation**: Empty state displays with action button
  - **Dependencies**: Task 5.3.1
  - **Effort**: 2 hours

### 5.4 Reviews Management

- [ ] **Task 5.4.1**: Build reviews page
  - File: `lib/features/profile/presentation/pages/reviews_page.dart`
  - List of submitted reviews, edit/delete buttons
  - **Validation**: Reviews load, actions enabled/disabled correctly
  - **Dependencies**: Task 5.1.1
  - **Effort**: 8 hours

- [ ] **Task 5.4.2**: Implement edit review
  - Modal with rating + text, save changes
  - PUT `/api/v1/Reviews/{id}`
  - **Validation**: Review updates, "(ویرایش شده)" label appears
  - **Dependencies**: Task 5.4.1
  - **Effort**: 6 hours

- [ ] **Task 5.4.3**: Implement delete review
  - DELETE `/api/v1/Reviews/{id}`
  - Confirmation dialog, fade-out animation
  - **Validation**: Review deleted from list
  - **Dependencies**: Task 5.4.1
  - **Effort**: 4 hours

### 5.5 Notification Settings

- [ ] **Task 5.5.1**: Build notification settings page
  - File: `lib/features/profile/presentation/pages/notification_settings_page.dart`
  - Toggles for push/SMS, reminder timing options
  - **Validation**: Settings display, toggles work
  - **Dependencies**: Task 5.1.1
  - **Effort**: 6 hours

- [ ] **Task 5.5.2**: Implement notification preferences save
  - PUT `/api/v1/Customers/notification-preferences`
  - Immediate save on toggle, warning for disabling all
  - **Validation**: Preferences save, warning shows
  - **Dependencies**: Task 5.5.1
  - **Effort**: 4 hours

### 5.6 Support and Help

- [ ] **Task 5.6.1**: Build FAQ page
  - File: `lib/features/profile/presentation/pages/faq_page.dart`
  - Accordion-style expandable questions
  - **Validation**: Questions expand/collapse
  - **Dependencies**: Task 5.1.1
  - **Effort**: 6 hours

- [ ] **Task 5.6.2**: Implement support contact options
  - Phone dialer, email app, in-app chat (optional)
  - **Validation**: Contact methods work
  - **Dependencies**: Task 5.1.1
  - **Effort**: 4 hours

- [ ] **Task 5.6.3**: Build about app page
  - App version, build number, links to T&C/Privacy
  - **Validation**: Info displays correctly
  - **Dependencies**: Task 5.1.1
  - **Effort**: 3 hours

### 5.7 Logout

- [ ] **Task 5.7.1**: Implement logout flow
  - Confirmation dialog, API call (best effort), clear storage
  - Navigate to login, clear navigation stack
  - **Validation**: Logout works online and offline
  - **Dependencies**: Task 5.1.1
  - **Effort**: 4 hours

### 5.8 Profile BLoC

- [ ] **Task 5.8.1**: Implement profile BLoC
  - Files: `lib/features/profile/presentation/bloc/`
  - Events: UpdateProfile, LoadFavorites, LoadReviews, Logout
  - States: Loading, Success, Error
  - **Validation**: All profile operations work
  - **Dependencies**: All profile tasks
  - **Effort**: 8 hours

---

## Phase 6: Polish, Testing & QA (2 weeks, ~80 hours)

### 6.1 Performance Optimization

- [ ] **Task 6.1.1**: Implement image caching strategy
  - Use `cached_network_image` throughout
  - Configure memory/disk cache sizes
  - **Validation**: Images load faster on revisit
  - **Dependencies**: All UI complete
  - **Effort**: 6 hours

- [ ] **Task 6.1.2**: Optimize API response caching
  - Cache categories (1h), search results (5min), bookings (2min)
  - Implement cache invalidation logic
  - **Validation**: API calls reduced, data fresh when needed
  - **Dependencies**: All features complete
  - **Effort**: 8 hours

- [ ] **Task 6.1.3**: Profile and fix jank (dropped frames)
  - Use Flutter DevTools performance view
  - Optimize list builders, image loading
  - **Validation**: 60 FPS maintained during scrolling
  - **Dependencies**: All UI complete
  - **Effort**: 10 hours

- [ ] **Task 6.1.4**: Reduce app bundle size
  - Remove unused assets, optimize images
  - Enable Dart code obfuscation
  - **Validation**: APK size <30MB
  - **Dependencies**: All features complete
  - **Effort**: 4 hours

### 6.2 Error Handling and Edge Cases

- [ ] **Task 6.2.1**: Implement global error handler
  - Catch unhandled exceptions, log to Sentry/Firebase Crashlytics
  - **Validation**: Crashes logged, app doesn't crash fatally
  - **Dependencies**: None
  - **Effort**: 6 hours

- [ ] **Task 6.2.2**: Add offline detection and banners
  - Show "اتصال اینترنت قطع است" banner at top
  - Disable actions when offline
  - **Validation**: Offline state detected, banner shows
  - **Dependencies**: All UI complete
  - **Effort**: 6 hours

- [ ] **Task 6.2.3**: Test and handle all API error scenarios
  - 401 (auth refresh), 404, 409 (conflict), 500, timeout
  - **Validation**: Each error handled gracefully with user message
  - **Dependencies**: All API calls complete
  - **Effort**: 8 hours

### 6.3 Accessibility

- [ ] **Task 6.3.1**: Add semantic labels to all interactive elements
  - **Validation**: TalkBack/VoiceOver announces elements correctly
  - **Dependencies**: All UI complete
  - **Effort**: 8 hours

- [ ] **Task 6.3.2**: Ensure WCAG AA color contrast
  - Verify dark blue (#1A365D) on white meets 4.5:1 ratio
  - **Validation**: All text readable, contrast checker passes
  - **Dependencies**: Phase 1 design system
  - **Effort**: 4 hours

- [ ] **Task 6.3.3**: Implement keyboard navigation support
  - Focus indicators (2px dark blue), RTL tab order
  - **Validation**: App navigable with external keyboard
  - **Dependencies**: All UI complete
  - **Effort**: 6 hours

### 6.4 Testing

- [ ] **Task 6.4.1**: Write unit tests for use cases
  - Test all domain use cases with mocked repositories
  - **Validation**: 90%+ coverage for domain layer
  - **Dependencies**: All use cases implemented
  - **Effort**: 12 hours

- [ ] **Task 6.4.2**: Write BLoC tests
  - Use `bloc_test` package, test all events/states
  - **Validation**: 90%+ coverage for BLoCs
  - **Dependencies**: All BLoCs implemented
  - **Effort**: 12 hours

- [ ] **Task 6.4.3**: Write widget tests
  - Test critical user flows (login, search, booking)
  - **Validation**: Key widgets tested, flows work
  - **Dependencies**: All widgets implemented
  - **Effort**: 8 hours

- [ ] **Task 6.4.4**: Perform manual testing on real devices
  - Test on Android (multiple screen sizes) and iOS
  - **Validation**: App works on target devices, no crashes
  - **Dependencies**: All features complete
  - **Effort**: 10 hours

### 6.5 Analytics and Monitoring

- [ ] **Task 6.5.1**: Integrate Firebase Analytics
  - Track screen views, user actions (search, booking, etc.)
  - **Validation**: Events logged to Firebase console
  - **Dependencies**: None
  - **Effort**: 6 hours

- [ ] **Task 6.5.2**: Integrate Sentry for crash reporting
  - Capture exceptions, log breadcrumbs
  - **Validation**: Crashes reported to Sentry dashboard
  - **Dependencies**: Task 6.2.1
  - **Effort**: 4 hours

### 6.6 Final QA and Bug Fixes

- [ ] **Task 6.6.1**: Perform end-to-end testing
  - Complete user journeys: Sign up → Search → Book → Manage
  - **Validation**: All flows work without errors
  - **Dependencies**: All features complete
  - **Effort**: 8 hours

- [ ] **Task 6.6.2**: Fix identified bugs
  - Address bugs from testing phases
  - **Validation**: Bug list cleared
  - **Dependencies**: Task 6.6.1
  - **Effort**: 12 hours (variable)

- [ ] **Task 6.6.3**: Prepare release build
  - Configure production API URLs, enable obfuscation
  - Generate signed APK/IPA
  - **Validation**: Release build installs and runs
  - **Dependencies**: Task 6.6.2
  - **Effort**: 4 hours

---

## Validation Strategy

**After each task**:
1. Manual testing on emulator/device
2. Code review (if team environment)
3. Update task status in project tracker

**After each phase**:
1. Regression testing of previous phases
2. Performance profiling
3. Demo to stakeholders

**Before production release**:
1. All tasks ✅ complete
2. Test coverage ≥85%
3. No critical/high bugs
4. Performance SLAs met (load <2s, 60 FPS)
5. Accessibility tests pass
6. App store assets prepared (screenshots, descriptions)

---

## Dependencies Graph

```
Phase 1 (Foundation)
  ↓
Phase 2 (Search & Home) ─┐
  ↓                      │
Phase 3 (Booking) ───────┤
  ↓                      │
Phase 4 (Bookings Mgmt)  ├─→ Phase 6 (Polish & QA)
  ↓                      │
Phase 5 (Profile) ───────┘
```

**Parallelization opportunities**:
- Phases 2 and 3 can partially overlap (search and booking are independent)
- Phases 4 and 5 can partially overlap (bookings management and profile are independent)
- Testing (Phase 6.4) can start during Phase 4-5 for completed features

---

## Risk Mitigation

**High-risk tasks** (need extra attention):
1. **Task 3.4.1-3.4.4** (Jalali calendar and availability): Complex date/time logic
2. **Task 3.1.4, 3.6.1** (Booking state management): Critical for user experience
3. **Task 6.2.3** (API error handling): Ensures reliability
4. **Task 6.1.3** (Performance profiling): Ensures smooth UX

**Mitigation strategies**:
- Allocate extra buffer time for high-risk tasks
- Conduct early prototypes for Jalali calendar
- Add comprehensive logging for debugging
- Schedule mid-phase reviews with stakeholders
