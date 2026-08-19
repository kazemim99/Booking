# customer-mobile-search Specification

## Purpose

Enable customers to discover service providers through intuitive search, filtering, and browsing capabilities optimized for mobile Persian users. The search experience should be fast, visual, and help customers find the right provider based on category, location, rating, and availability.

## ADDED Requirements

### Requirement: Search Input and Real-Time Results

The search page SHALL provide a text input with debounced real-time search across provider names, services, and locations.

#### Scenario: Customer opens search page

**GIVEN** a customer is authenticated
**WHEN** the customer taps "جستجو" in bottom navigation or home search bar
**THEN** the system displays search page with:
  - Search input field with placeholder: "جستجوی نام ارائه‌دهنده، خدمت، یا محل"
  - Keyboard auto-focused on input field
  - Location filter chip showing current city (e.g., "📍 تهران، ونک")
  - Category filter chips (e.g., "همه", "آرایشگاه", "ناخن", "ماساژ")
  - Recent searches list (if customer searched before)
**AND** layout is RTL with Persian font

#### Scenario: Customer types search query

**GIVEN** the customer is on search page
**WHEN** the customer types "آرایشگاه رز" in search input
**THEN** the system:
  - Waits 500ms after last keystroke (debounce)
  - Sends `POST /api/v1/Providers/search` with `{ keyword: "آرایشگاه رز" }`
  - Displays loading skeleton during fetch
  - Renders matching providers within 2 seconds
**AND** search is case-insensitive
**AND** supports partial matching (e.g., "رز" matches "آرایشگاه رز")

#### Scenario: Customer clears search input

**GIVEN** the customer has typed a query
**WHEN** the customer taps "X" button in search input or clears text
**THEN** the system:
  - Clears search results
  - Displays recent searches or popular categories
  - Cancels any pending API requests

#### Scenario: No results found

**GIVEN** the customer searches for "xyz123"
**WHEN** the API returns empty results
**THEN** the system displays empty state:
  - Illustration (magnifying glass with sad face)
  - Message: "نتیجه‌ای یافت نشد"
  - Suggestion: "فیلترها را تغییر دهید یا کلمات دیگری جستجو کنید"
  - "پاک کردن فیلتر" button (if filters active)

### Requirement: Location-Based Filtering

The search page SHALL allow customers to filter providers by city, district, or proximity.

#### Scenario: Customer changes location filter

**GIVEN** the customer is searching in Tehran
**WHEN** the customer taps location chip "📍 تهران، ونک"
**THEN** the system displays location picker modal with:
  - Current location highlighted
  - List of major cities (Tehran, Isfahan, Shiraz, Mashhad, etc.)
  - District dropdown if city selected
  - "استفاده از موقعیت فعلی" button (GPS)
  - "تایید" button

#### Scenario: Customer selects new location

**GIVEN** the location picker is open
**WHEN** the customer selects "تهران، تجریش" and taps "تایید"
**THEN** the system:
  - Updates location chip to "📍 تهران، تجریش"
  - Re-runs search with new location filter
  - Fetches providers near selected location
  - Sorts by distance from location center

#### Scenario: Customer enables GPS location

**GIVEN** the customer grants location permission
**WHEN** the customer taps "استفاده از موقعیت فعلی"
**THEN** the system:
  - Gets device GPS coordinates
  - Reverse geocodes to city/district name
  - Updates location chip with current location
  - Searches providers within 10km radius
  - Sorts by distance (nearest first)

#### Scenario: Location permission denied

**GIVEN** the customer denied location permission
**WHEN** the customer taps "استفاده از موقعیت فعلی"
**THEN** the system displays dialog:
  - Message: "برای استفاده از مکان فعلی، دسترسی مکان را در تنظیمات فعال کنید"
  - "رفتن به تنظیمات" button (opens app settings)
  - "انصراف" button

### Requirement: Category Filtering

The search page SHALL provide quick filtering by service category.

#### Scenario: Customer selects category filter

**GIVEN** the search page displays category chips
**WHEN** the customer taps "آرایشگاه" chip
**THEN** the system:
  - Highlights "آرایشگاه" chip (fills with dark blue)
  - Deselects "همه" chip
  - Adds `categoryIds: ["haircut-category-uuid"]` to search request
  - Fetches only providers offering haircut services
  - Updates result count: "48 ارائه‌دهنده"

#### Scenario: Customer selects multiple categories

**GIVEN** the customer selected "آرایشگاه" category
**WHEN** the customer taps "ماساژ" chip (multi-select)
**THEN** the system:
  - Highlights both "آرایشگاه" and "ماساژ" chips
  - Combines filters: `categoryIds: ["haircut-uuid", "massage-uuid"]`
  - Fetches providers offering either service
  - Shows union of results

#### Scenario: Customer deselects all categories

**GIVEN** the customer has selected filters active
**WHEN** the customer taps "همه" chip
**THEN** the system:
  - Deselects all other category chips
  - Highlights only "همه"
  - Removes category filter from search
  - Fetches all providers (location-based only)

### Requirement: Advanced Filters

The search page SHALL provide a filters panel for rating, price range, distance, and availability.

#### Scenario: Customer opens filters panel

**GIVEN** the customer is viewing search results
**WHEN** the customer taps "⚙️ فیلترها" button
**THEN** the system displays bottom sheet/modal with:
  - **Rating filter**: "⭐ ۵ ستاره", "⭐ ۴+", "⭐ ۳+"
  - **Price range**: Slider with min/max (0 - 1,000,000 تومان)
  - **Distance**: "کمتر از ۵ کیلومتر", "کمتر از ۱۰ کیلومتر", "همه"
  - **Availability**: "فقط نوبت‌های آزاد امروز", "آزاد در ۳ روز آینده"
  - **Open now**: Toggle "فقط باز"
  - "اعمال فیلتر" and "پاک کردن همه" buttons

#### Scenario: Customer applies filters

**GIVEN** the filters panel is open
**WHEN** the customer:
  - Selects "⭐ ۴+" rating
  - Sets price range: 100,000 - 500,000 تومان
  - Selects "کمتر از ۵ کیلومتر"
  - Taps "اعمال فیلتر"
**THEN** the system:
  - Closes filters panel
  - Sends `POST /api/v1/Providers/search` with filters:
    ```json
    {
      "minRating": 4.0,
      "minPrice": 100000,
      "maxPrice": 500000,
      "maxDistance": 5,
      "locationId": "current-location-uuid"
    }
    ```
  - Updates result count
  - Shows active filter indicator on "⚙️ فیلترها" button (e.g., badge "3")

#### Scenario: Customer clears all filters

**GIVEN** the customer has active filters
**WHEN** the customer taps "پاک کردن همه" in filters panel
**THEN** the system:
  - Resets all filter controls to defaults
  - Removes filter parameters from search
  - Re-fetches unfiltered results
  - Hides filter badge on button

### Requirement: Provider Result Cards

The search results SHALL display providers as visually rich cards with key information.

#### Scenario: Search returns results

**GIVEN** the customer searched for providers
**WHEN** the API returns 48 matching providers
**THEN** the system displays scrollable list of provider cards
**AND** each card shows:
  - Provider thumbnail image (16:9 aspect ratio, 120dp wide)
  - Provider name (e.g., "آرایشگاه رز")
  - Rating with Persian numbers (e.g., "⭐ ۴.۸ (۱۲۷ نظر)")
  - Distance with Persian numbers (e.g., "📍 ۱.۲ کیلومتر")
  - Starting price (e.g., "💰 از ۱۰۰٬۰۰۰ تومان")
  - Open/closed status (e.g., "⏰ باز تا ۲۲:۰۰" or "بسته")
  - Quick action: "رزرو" button (dark blue, prominent)
**AND** cards use 1px border (#E2E8F0) for definition (flat design, no shadows)

#### Scenario: Provider image fails to load

**GIVEN** a provider card is rendering
**WHEN** the image URL is invalid or fails to load
**THEN** the system displays:
  - Placeholder icon (calendar or building icon)
  - Gray background color
  - No broken image indicator

#### Scenario: Customer taps provider card

**GIVEN** search results display providers
**WHEN** the customer taps anywhere on a provider card (except "رزرو" button)
**THEN** the system navigates to provider detail page (`/provider/{providerId}`)
**AND** displays provider profile, services, gallery, reviews

#### Scenario: Customer taps "رزرو" button

**GIVEN** search results display providers
**WHEN** the customer taps "رزرو" button on a provider card
**THEN** the system navigates directly to booking wizard (`/booking/{providerId}`)
**AND** skips provider detail page (quick booking flow)

### Requirement: Pagination and Infinite Scroll

The search results SHALL support pagination with infinite scroll for large result sets.

#### Scenario: Customer scrolls to bottom of results

**GIVEN** the search returned 48 providers (page 1 of 3)
**WHEN** the customer scrolls to the bottom of the list
**THEN** the system:
  - Displays loading indicator at list bottom
  - Fetches next page: `POST /api/v1/Providers/search?pageNumber=2&pageSize=20`
  - Appends results to existing list
  - Hides loading indicator

#### Scenario: Customer reaches last page

**GIVEN** the customer scrolled to page 3 (last page)
**WHEN** the customer scrolls to bottom
**THEN** the system displays end-of-list message:
  - "همه نتایج نمایش داده شد"
  - Does not attempt to fetch more pages

#### Scenario: Pagination request fails

**GIVEN** the customer triggered pagination
**WHEN** the API request fails (network error)
**THEN** the system:
  - Displays error toast: "خطا در بارگذاری نتایج بیشتر"
  - Shows "تلاش مجدد" button at list bottom
  - Allows customer to retry without losing current results

### Requirement: Sort Options

The search page SHALL provide sorting options for result ordering.

#### Scenario: Customer changes sort order

**GIVEN** the customer is viewing search results
**WHEN** the customer taps sort dropdown and selects "بیشترین امتیاز"
**THEN** the system:
  - Updates sort parameter: `sortBy: "rating", sortOrder: "desc"`
  - Re-fetches results with new sort
  - Resets pagination to page 1
  - Displays sorted results

**Available sort options**:
- "نزدیک‌ترین" (distance ascending) - default if location available
- "بیشترین امتیاز" (rating descending)
- "کمترین قیمت" (price ascending)
- "بیشترین نظرات" (review count descending)

### Requirement: Recent Searches

The search page SHALL display customer's recent search queries for quick access.

#### Scenario: Customer views empty search page

**GIVEN** the customer opened search page
**WHEN** search input is empty
**THEN** the system displays "جستجوهای اخیر" section
**AND** shows last 5 search queries (e.g., "آرایشگاه رز", "ماساژ تهران")
**AND** each query has clock icon and delete "X" button

#### Scenario: Customer taps recent search

**GIVEN** recent searches are displayed
**WHEN** the customer taps "آرایشگاه رز" from recent list
**THEN** the system:
  - Populates search input with "آرایشگاه رز"
  - Executes search automatically
  - Displays results

#### Scenario: Customer deletes recent search

**GIVEN** recent searches are displayed
**WHEN** the customer taps "X" next to a search query
**THEN** the system:
  - Removes query from recent searches list
  - Deletes from local storage
  - Does not execute search

#### Scenario: No recent searches exist

**GIVEN** the customer never searched before
**WHEN** search page loads with empty input
**THEN** the system displays popular categories instead
**AND** hides "جستجوهای اخیر" section

### Requirement: Favorite Providers in Search

The search results SHALL display heart icon for favoriting providers.

#### Scenario: Customer favorites provider from search

**GIVEN** search results display a provider
**WHEN** the customer taps the heart icon (outlined) on provider card
**THEN** the system:
  - Sends `POST /api/v1/Customers/favorites/{providerId}`
  - Fills heart icon with red color
  - Displays toast: "به علاقه‌مندی‌ها اضافه شد"
  - Updates heart icon instantly (optimistic UI)

#### Scenario: Customer unfavorites provider

**GIVEN** a provider is already favorited (filled heart)
**WHEN** the customer taps the heart icon
**THEN** the system displays confirmation dialog:
  - "این ارائه‌دهنده از لیست علاقه‌مندی‌ها حذف شود؟"
  - "حذف" and "انصراف" buttons
**AND** upon "حذف":
  - Sends `DELETE /api/v1/Customers/favorites/{providerId}`
  - Outlines heart icon
  - Displays toast: "از علاقه‌مندی‌ها حذف شد"

#### Scenario: Favorite request fails

**GIVEN** the customer taps heart icon
**WHEN** the API request fails (network error or 500)
**THEN** the system:
  - Reverts heart icon to previous state
  - Displays error toast: "خطا در افزودن به علاقه‌مندی‌ها"
  - Does not update UI (rollback optimistic change)

### Requirement: Search Performance

The search page SHALL provide fast, responsive search with caching.

#### Scenario: Repeated search

**GIVEN** the customer searched for "آرایشگاه" 2 minutes ago
**WHEN** the customer searches for "آرایشگاه" again
**THEN** the system:
  - Checks cache for results (TTL: 5 minutes)
  - Displays cached results immediately (<200ms)
  - Refreshes data in background
  - Updates UI if results changed

#### Scenario: Search throttling (rate limiting)

**GIVEN** the customer types rapidly in search input
**WHEN** multiple keystrokes occur within 500ms
**THEN** the system:
  - Cancels previous pending API requests
  - Only sends final request after 500ms idle
  - Prevents API spam
  - Shows loading state during debounce

#### Scenario: Search loads within SLA

**GIVEN** the customer executed a search
**WHEN** the API is called
**THEN** the system:
  - Displays skeleton loader within 100ms
  - Fetches results from API
  - Renders results within 2 seconds (P95)
  - Logs slow queries (>2s) for monitoring

### Requirement: Error Handling

The search page SHALL gracefully handle errors with user-friendly messages.

#### Scenario: Network offline

**GIVEN** the customer has no internet connection
**WHEN** the customer tries to search
**THEN** the system displays:
  - Offline banner: "اتصال اینترنت قطع است"
  - Cached search results (if available)
  - Disabled search input and filters
  - Message: "نتایج از حافظه موقت نمایش داده می‌شوند"

#### Scenario: API returns 500 error

**GIVEN** the customer searches for providers
**WHEN** `POST /api/v1/Providers/search` returns 500 Internal Server Error
**THEN** the system displays error state:
  - Error icon (warning triangle)
  - Message: "خطا در جستجو. لطفاً دوباره تلاش کنید."
  - "تلاش مجدد" button

#### Scenario: Customer retries failed search

**GIVEN** a search failed with error
**WHEN** the customer taps "تلاش مجدد"
**THEN** the system:
  - Shows loading state
  - Re-executes search with same parameters
  - Displays results if successful
  - Shows error again if still failing

### Requirement: Accessibility

The search page SHALL be accessible to screen readers and keyboard users.

#### Scenario: Screen reader announces results

**GIVEN** a customer using TalkBack/VoiceOver
**WHEN** search results load
**THEN** the system announces:
  - "۴۸ ارائه‌دهنده یافت شد" (result count)
  - Each provider card: "آرایشگاه رز، امتیاز ۴.۸، فاصله ۱.۲ کیلومتر، از ۱۰۰ هزار تومان"
  - "دکمه رزرو" for each booking button

#### Scenario: Keyboard navigation

**GIVEN** a customer using external keyboard (accessibility)
**WHEN** the customer presses Tab key
**THEN** focus moves through elements in RTL order:
  - Search input → Location chip → Category chips → Filter button → Provider cards
**AND** focused elements show visible outline (2px dark blue)
**AND** Enter key activates buttons/links

### Requirement: Analytics

The search page SHALL track search behavior for insights.

#### Scenario: Search executed

**GIVEN** a customer performs a search
**WHEN** results are displayed
**THEN** the system logs event:
  - `search_performed`: query text
  - `filters_applied`: category, location, rating, price
  - `results_count`: number of providers
  - `search_source`: "search_page" or "home_search_bar"

#### Scenario: Provider selected from search

**GIVEN** search results are displayed
**WHEN** customer taps a provider
**THEN** the system logs event:
  - `provider_clicked_from_search`: provider ID
  - `search_query`: original query
  - `result_rank`: position in results (1-indexed)
