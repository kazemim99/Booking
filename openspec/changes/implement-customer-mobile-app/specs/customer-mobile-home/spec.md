# customer-mobile-home Specification

## Purpose

Provide customers with a personalized mobile home screen that serves as the primary entry point for discovering services, accessing bookings, and navigating the app efficiently. The home screen should feel intuitive for Persian users, load quickly, and guide them toward booking actions.

## ADDED Requirements

### Requirement: Home Screen Layout and Navigation

The mobile app SHALL display a home screen with bottom navigation, personalized greeting, search access, and quick action cards.

#### Scenario: Authenticated customer opens app

**GIVEN** a customer named "علی رضایی" is authenticated
**WHEN** the app launches and home screen loads
**THEN** the system displays:
- Top app bar with "Booksy Customer" title and notification bell icon
- Personalized greeting: "سلام علی! 👋"
- Search bar placeholder: "جستجوی آرایشگاه، ماساژ، اسپا..."
- Location indicator showing current city (e.g., "📍 تهران، ایران")
- Bottom navigation bar with 4 tabs: "خانه", "جستجو", "رزروها", "پروفایل"
- "خانه" tab is highlighted/active
**AND** layout direction is RTL (right-to-left)
**AND** all text uses Vazir Persian font
**AND** screen loads within 2 seconds

#### Scenario: Guest user opens app

**GIVEN** a user is not authenticated
**WHEN** the app launches
**THEN** the system redirects to login page
**AND** does not display home screen until authenticated

#### Scenario: Customer taps search bar on home

**GIVEN** the customer is viewing home screen
**WHEN** the customer taps the search bar
**THEN** the system navigates to full search page (`/search`)
**AND** focuses the search input field automatically

#### Scenario: Customer taps location indicator

**GIVEN** the customer is viewing home screen
**WHEN** the customer taps the location indicator (e.g., "📍 تهران، ایران")
**THEN** the system displays location picker modal
**AND** allows customer to change city/location
**AND** updates search results based on new location

### Requirement: Popular Categories Display

The home screen SHALL display a horizontal scrollable list of popular service categories with icons and labels.

#### Scenario: Home screen loads popular categories

**GIVEN** the customer is viewing home screen
**WHEN** the home screen finishes loading
**THEN** the system displays section header: "دسته‌بندی‌های محبوب"
**AND** fetches categories from `GET /api/v1/Categories/popular`
**AND** displays 8-12 categories in horizontal scroll
**AND** each category shows:
  - Circular icon (haircut, nail, massage, etc.)
  - Category label in Persian (e.g., "آرایشگاه", "ناخن", "ماساژ")
**AND** categories are ordered by popularity (from backend)
**AND** loading completes within 1 second

#### Scenario: Customer taps a category

**GIVEN** the home screen displays popular categories
**WHEN** the customer taps the "آرایشگاه" category
**THEN** the system navigates to search page (`/search`)
**AND** pre-filters results by selected category
**AND** displays providers matching that category

#### Scenario: API fails to load categories

**GIVEN** the customer is viewing home screen
**WHEN** `GET /api/v1/Categories/popular` returns 500 error
**THEN** the system displays fallback categories (hardcoded list)
**AND** shows toast: "دسته‌بندی‌ها از حافظه موقت بارگذاری شدند"
**AND** retries API call in background

### Requirement: Upcoming Bookings Widget

The home screen SHALL display the customer's next 1-2 upcoming bookings for quick access.

#### Scenario: Customer has upcoming bookings

**GIVEN** the customer has 3 upcoming bookings
**WHEN** the home screen loads
**THEN** the system displays section header: "رزروهای پیش‌رو"
**AND** fetches bookings from `GET /api/v1/Bookings/my-bookings?filter=upcoming&limit=2`
**AND** displays first 2 upcoming bookings as cards showing:
  - Provider name (e.g., "آرایشگاه رز")
  - Service name (e.g., "رنگ مو")
  - Date in Jalali format (e.g., "چهارشنبه، 12 فروردین")
  - Time with Persian numbers (e.g., "⏰ ۱۴:۰۰")
  - Price with Persian numbers (e.g., "💰 ۱۵۰٬۰۰۰ تومان")
  - Status badge (e.g., "✅ تایید شده")
  - Quick action buttons: "مشاهده جزئیات", "مسیریابی"
**AND** "مشاهده همه" button links to bookings tab

#### Scenario: Customer has no upcoming bookings

**GIVEN** the customer has no future bookings
**WHEN** the home screen loads upcoming bookings section
**THEN** the system displays empty state:
  - Calendar icon illustration
  - Message: "شما نوبت آینده‌ای ندارید"
  - "رزرو نوبت جدید" button
**AND** tapping button navigates to search page

#### Scenario: Customer taps booking card

**GIVEN** the home screen displays an upcoming booking
**WHEN** the customer taps "مشاهده جزئیات" button
**THEN** the system navigates to booking detail page (`/bookings/{id}`)
**AND** displays full booking information

#### Scenario: Customer taps directions on booking

**GIVEN** the home screen displays an upcoming booking
**WHEN** the customer taps "مسیریابی" button
**THEN** the system opens device maps app (Google Maps or Neshan Maps)
**AND** provides directions to provider location

### Requirement: Promotions and Offers Banner

The home screen SHALL display a swipeable carousel of promotional banners when promotions are available.

#### Scenario: Promotions are available

**GIVEN** the backend has active promotions
**WHEN** the home screen loads
**THEN** the system displays section header: "پیشنهاد ویژه"
**AND** shows swipeable banner carousel with:
  - Promotional image (e.g., "🎉 تخفیف 20٪ اولین رزرو!")
  - Swipe indicators (dots)
  - Auto-advance every 5 seconds
**AND** tapping banner opens promotion detail page

#### Scenario: No promotions available

**GIVEN** the backend has no active promotions
**WHEN** the home screen loads
**THEN** the system hides the promotions section entirely
**AND** does not show empty placeholder

### Requirement: Top Providers Recommendation

The home screen SHALL display 10-15 recommended providers based on location, ratings, and customer preferences.

#### Scenario: Home screen loads recommended providers

**GIVEN** the customer is in Tehran
**WHEN** the home screen loads
**THEN** the system displays section header: "ارائه‌دهندگان برتر"
**AND** fetches providers from `POST /api/v1/Providers/search` with filters:
  - Location: Customer's current city
  - MinRating: 4.5
  - SortBy: Rating descending
  - PageSize: 15
**AND** displays providers in horizontal scroll
**AND** each provider card shows:
  - Provider thumbnail image (circular, 80dp)
  - Provider name
  - Category tag (e.g., "آرایشگاه")
  - Rating with Persian numbers (e.g., "⭐ ۴.۸")
  - Distance in Persian (e.g., "۱.۲ کیلومتر")
**AND** loading shows skeleton placeholders

#### Scenario: Customer taps recommended provider

**GIVEN** the home screen displays recommended providers
**WHEN** the customer taps a provider card
**THEN** the system navigates to provider detail page (`/provider/{id}`)
**AND** displays provider profile, services, and gallery

#### Scenario: Location permission denied

**GIVEN** the customer denied location permission
**WHEN** the home screen tries to load recommendations
**THEN** the system uses last known city (from profile) or defaults to "تهران"
**AND** shows toast: "برای پیشنهادهای بهتر، دسترسی مکان را فعال کنید"

### Requirement: Bottom Navigation

The app SHALL provide persistent bottom navigation for quick access to main sections.

#### Scenario: Customer navigates between tabs

**GIVEN** the customer is on home screen
**WHEN** the customer taps "جستجو" tab in bottom navigation
**THEN** the system navigates to search page
**AND** highlights "جستجو" tab as active
**AND** navigation transition takes <300ms

#### Scenario: Customer returns to home tab

**GIVEN** the customer is on search page
**WHEN** the customer taps "خانه" tab
**THEN** the system navigates back to home screen
**AND** refreshes data if last loaded >5 minutes ago
**AND** otherwise displays cached home screen instantly

#### Scenario: Notification bell displays unread count

**GIVEN** the customer has 3 unread notifications
**WHEN** the home screen loads
**THEN** the notification bell icon displays badge with "۳"
**AND** tapping bell opens notifications modal

### Requirement: Performance and Caching

The home screen SHALL load quickly using caching and optimized API calls.

#### Scenario: First load (cold start)

**GIVEN** the app just launched
**WHEN** the home screen loads for first time
**THEN** the system:
  - Displays skeleton placeholders immediately (<100ms)
  - Fetches popular categories, bookings, and providers in parallel
  - Renders real content within 2 seconds total
  - Caches responses for 5 minutes

#### Scenario: Subsequent loads (warm start)

**GIVEN** the customer previously viewed home screen
**WHEN** the customer navigates back to home within 5 minutes
**THEN** the system displays cached content immediately (<500ms)
**AND** refreshes data in background
**AND** updates UI if data changed

#### Scenario: Pull-to-refresh

**GIVEN** the customer is viewing home screen
**WHEN** the customer pulls down to refresh
**THEN** the system shows loading indicator
**AND** fetches fresh data from API
**AND** updates all sections
**AND** completes within 2 seconds

### Requirement: Error Handling and Offline Mode

The home screen SHALL gracefully handle network failures and API errors.

#### Scenario: Network is offline

**GIVEN** the customer has no internet connection
**WHEN** the home screen tries to load
**THEN** the system displays:
  - Offline banner at top: "اتصال اینترنت قطع است"
  - Cached content from previous session (if available)
  - Disabled search bar and category taps
**AND** hides skeleton loaders

#### Scenario: API returns 500 error

**GIVEN** the home screen fetches upcoming bookings
**WHEN** `GET /api/v1/Bookings/my-bookings` returns 500
**THEN** the system displays error message in that section:
  - "خطا در بارگذاری رزروها"
  - "تلاش مجدد" button
**AND** other sections load normally (isolated failures)

#### Scenario: Customer retries failed section

**GIVEN** a section failed to load
**WHEN** the customer taps "تلاش مجدد" button
**THEN** the system re-fetches data for that section only
**AND** shows loading indicator
**AND** updates section if successful

### Requirement: Accessibility

The home screen SHALL be accessible to users with screen readers and keyboard navigation.

#### Scenario: Screen reader navigation

**GIVEN** a customer using TalkBack (Android)
**WHEN** the customer navigates home screen
**THEN** each element has descriptive label:
  - Search bar: "جستجوی ارائه‌دهندگان خدمات"
  - Category card: "دسته‌بندی آرایشگاه"
  - Booking card: "نوبت آرایشگاه رز در تاریخ چهارشنبه ۱۲ فروردین"
  - Bottom nav: "خانه", "جستجو", "رزروها", "پروفایل"
**AND** focus order follows RTL layout (right to left, top to bottom)

#### Scenario: Touch target sizes

**GIVEN** the customer is interacting with home screen
**WHEN** the customer taps any interactive element
**THEN** all buttons and tappable areas are ≥48dp × 48dp
**AND** adequate spacing between elements (≥8dp)

### Requirement: Analytics and Tracking

The home screen SHALL track user interactions for product insights.

#### Scenario: Home screen viewed

**GIVEN** the customer opens home screen
**WHEN** the screen finishes loading
**THEN** the system logs event:
  - `screen_view`: "home"
  - `user_id`: customer ID
  - `load_time_ms`: time to render

#### Scenario: Category tapped

**GIVEN** the customer taps a category
**WHEN** navigation completes
**THEN** the system logs event:
  - `category_tapped`: "haircut"
  - `source`: "home_popular_categories"

#### Scenario: Provider tapped

**GIVEN** the customer taps a recommended provider
**WHEN** navigation completes
**THEN** the system logs event:
  - `provider_viewed`: provider ID
  - `source`: "home_recommendations"
  - `rank`: position in list (1-15)
