# customer-mobile-bookings-management Specification

## Purpose

Enable customers to view, manage, and track their bookings through a mobile interface. Customers should easily see upcoming appointments, review past bookings, cancel or reschedule as needed, and access booking details with quick actions like navigation and calendar integration.

## ADDED Requirements

### Requirement: Bookings List View with Tabs

The bookings page SHALL display customer's bookings in categorized tabs (upcoming, past, cancelled) with clear visual hierarchy.

#### Scenario: Customer opens bookings page

**GIVEN** a customer is authenticated
**WHEN** the customer taps "رزروها" in bottom navigation
**THEN** the system displays bookings page with:
  - Page title: "رزروها"
  - Three tabs: "پیش‌رو" (active by default), "گذشته", "لغو شده"
  - Tab indicator (dark blue underline on active tab)
  - Pull-to-refresh capability
  - Loading skeleton if first load
**AND** fetches bookings: `GET /api/v1/Bookings/my-bookings?status=Confirmed,Pending&pageSize=20`

#### Scenario: Customer has upcoming bookings

**GIVEN** the customer has 3 upcoming bookings
**WHEN** "پیش‌رو" tab is active
**THEN** the system displays 3 booking cards sorted by date (nearest first)
**AND** each card shows:
  - Provider thumbnail image (circular, 48dp)
  - Provider name (e.g., "آرایشگاه رز")
  - Service name (e.g., "رنگ مو")
  - Date in Jalali format (e.g., "چهارشنبه، ۱۲ فروردین")
  - Time with Persian numbers (e.g., "⏰ ۱۴:۰۰")
  - Price with Persian numbers (e.g., "💰 ۴۵۰٬۰۰۰ تومان")
  - Status badge with color:
    - "✅ تایید شده" (green text, light green bg)
    - "⏳ در انتظار تایید" (amber text, light amber bg)
  - Two action buttons: "جزئیات" (secondary), "لغو" (text button, red)
**AND** tapping card opens booking detail page

#### Scenario: Customer has no upcoming bookings

**GIVEN** the customer has no future bookings
**WHEN** "پیش‌رو" tab is active
**THEN** the system displays empty state:
  - Calendar icon illustration (gray)
  - Message: "شما نوبت آینده‌ای ندارید"
  - Subtitle: "نوبت جدید رزرو کنید و از خدمات استفاده کنید"
  - "جستجوی خدمات" button (primary, dark blue)
**AND** tapping button navigates to search page (`/search`)

#### Scenario: Customer switches to past bookings tab

**GIVEN** the customer is viewing bookings page
**WHEN** the customer taps "گذشته" tab
**THEN** the system:
  - Activates "گذشته" tab (dark blue underline)
  - Fetches: `GET /api/v1/Bookings/my-bookings?status=Completed&pageSize=20`
  - Displays completed bookings sorted by date (most recent first)
  - Each card shows:
    - Provider image, name, service
    - Completion date
    - Status badge: "✅ تکمیل شده" (blue text, light blue bg)
    - Action button: "رزرو مجدد" (secondary)
    - "ثبت نظر" button (if review not yet submitted)

#### Scenario: Customer switches to cancelled tab

**GIVEN** the customer is viewing bookings page
**WHEN** the customer taps "لغو شده" tab
**THEN** the system:
  - Activates "لغو شده" tab
  - Fetches: `GET /api/v1/Bookings/my-bookings?status=Cancelled&pageSize=20`
  - Displays cancelled bookings
  - Each card shows:
    - Provider image, name, service
    - Original date (struck through)
    - Status badge: "❌ لغو شده" (red text, light red bg)
    - Cancellation reason (if provided)
    - Action: "رزرو مجدد" button

### Requirement: Booking Detail Page

The system SHALL display comprehensive booking information with quick actions.

#### Scenario: Customer views booking details

**GIVEN** the customer taps a booking card or "جزئیات" button
**WHEN** the booking detail page loads
**THEN** the system:
  - Fetches: `GET /api/v1/Bookings/{bookingId}`
  - Displays detail page with:
    - **Header**:
      - Booking reference: "کد پیگیری: #BK۱۲۳۴۵"
      - Status badge (large, prominent)
    - **Provider Section**:
      - Provider logo and name
      - Full address with map icon
      - Quick actions: "مسیریابی" | "تماس"
    - **Service Details**:
      - Service name (e.g., "💇 رنگ مو")
      - Staff name and photo (e.g., "👤 مریم احمدی")
      - Duration (e.g., "⏱ ۱۲۰ دقیقه")
    - **Date & Time**:
      - Full date: "چهارشنبه، ۱۲ فروردین ۱۴۰۳"
      - Time: "ساعت ۱۴:۰۰ - ۱۶:۰۰"
      - Countdown (if upcoming): "۲ روز و ۵ ساعت تا نوبت"
    - **Pricing**:
      - Service price
      - Total amount: "۴۵۰٬۰۰۰ تومان"
      - Deposit status (if applicable)
    - **Customer Notes** (if provided):
      - "📝 یادداشت شما:"
      - Note text
    - **Cancellation Policy**:
      - "⚠️ قوانین لغو:"
      - Policy text (e.g., "لغو تا ۲۴ ساعت قبل: بدون جریمه")
    - **Action Buttons** (bottom sticky):
      - "تغییر زمان" (secondary) | "لغو رزرو" (text, red)

#### Scenario: Customer gets directions to provider

**GIVEN** the booking detail page is displayed
**WHEN** the customer taps "مسیریابی" button
**THEN** the system:
  - Extracts provider address coordinates
  - Opens device maps app with options dialog:
    - "Google Maps" (if installed)
    - "Neshan Maps" (Iranian, if installed)
    - "Maps" (iOS default)
  - Passes destination coordinates and provider name

#### Scenario: Customer calls provider

**GIVEN** the booking detail page is displayed
**WHEN** the customer taps "تماس" button
**THEN** the system:
  - Opens phone dialer with provider's phone number pre-filled
  - Customer can initiate call with one tap

#### Scenario: Customer adds booking to calendar

**GIVEN** the booking detail page is displayed
**WHEN** the customer taps "افزودن به تقویم" button (overflow menu)
**THEN** the system:
  - Opens device calendar app (iOS Calendar or Google Calendar)
  - Creates calendar event with:
    - Title: "{Service} - {Provider}"
    - Date/Time: Booking start time
    - Duration: Service duration
    - Location: Provider address
    - Notes: Booking reference, provider phone, customer notes
    - Alert: 1 hour before (default)

### Requirement: Cancel Booking Flow

Customers SHALL be able to cancel upcoming bookings with confirmation and policy acknowledgment.

#### Scenario: Customer initiates cancellation

**GIVEN** the customer is viewing an upcoming booking
**WHEN** the customer taps "لغو رزرو" button
**THEN** the system displays confirmation dialog:
  - Title: "لغو رزرو"
  - Message: "آیا از لغو این نوبت مطمئن هستید؟"
  - Cancellation policy reminder:
    - "زمان باقی‌مانده تا نوبت: ۳۰ ساعت"
    - "لغو بدون جریمه (بیش از ۲۴ ساعت مانده)"
    - OR "لغو با ۵۰٪ جریمه (کمتر از ۲۴ ساعت)"
  - Optional: Reason dropdown (e.g., "تغییر برنامه", "بیماری", "سایر")
  - Actions: "لغو رزرو" (red, primary) | "انصراف" (secondary)

#### Scenario: Customer confirms cancellation

**GIVEN** the cancellation dialog is displayed
**WHEN** the customer selects reason (optional) and taps "لغو رزرو"
**THEN** the system:
  - Shows loading overlay: "در حال لغو رزرو..."
  - Sends: `POST /api/v1/Bookings/{bookingId}/cancel`
    ```json
    {
      "reason": "تغییر برنامه",
      "customerId": "customer-uuid"
    }
    ```
  - On success:
    - Dismisses dialog
    - Shows success toast: "رزرو شما با موفقیت لغو شد"
    - Removes booking from "پیش‌رو" list
    - Moves to "لغو شده" tab
    - Returns to bookings list

#### Scenario: Cancellation fails

**GIVEN** the customer confirmed cancellation
**WHEN** API returns 400 Bad Request (e.g., booking already started)
**THEN** the system:
  - Dismisses loading overlay
  - Displays error dialog:
    - "امکان لغو این رزرو وجود ندارد"
    - Error reason from API (e.g., "نوبت شما آغاز شده است")
    - "بستن" button
  - Keeps booking in list unchanged

#### Scenario: Customer cancels within penalty window

**GIVEN** booking is less than 24 hours away
**WHEN** customer confirms cancellation
**THEN** the system:
  - Processes cancellation
  - Shows toast with penalty info: "رزرو لغو شد. ۵۰٪ هزینه کسر می‌شود"
  - Updates booking status to Cancelled

### Requirement: Reschedule Booking Flow

Customers SHALL be able to reschedule upcoming bookings by selecting new date/time.

#### Scenario: Customer initiates reschedule

**GIVEN** the customer is viewing an upcoming booking
**WHEN** the customer taps "تغییر زمان" button
**THEN** the system:
  - Navigates to reschedule page (`/bookings/{id}/reschedule`)
  - Displays current booking summary (read-only):
    - Service, staff, current date/time
  - Shows date/time picker (similar to booking wizard Step 3):
    - Jalali calendar with available dates highlighted
    - Time slots grid
  - "تایید تغییر" button (disabled until new time selected)

#### Scenario: Customer selects new date and time

**GIVEN** the reschedule page is displayed
**WHEN** the customer selects new date "۱۵ فروردین" and time "۱۰:۰۰"
**THEN** the system:
  - Fetches availability: `GET /api/v1/Availability/slots?providerId={id}&date=2024-04-04`
  - Validates slot is available
  - Highlights selected date/time
  - Enables "تایید تغییر" button
  - Shows comparison:
    - "زمان فعلی: چهارشنبه ۱۲ فروردین، ۱۴:۰۰"
    - "زمان جدید: شنبه ۱۵ فروردین، ۱۰:۰۰"

#### Scenario: Customer confirms reschedule

**GIVEN** new date/time is selected
**WHEN** the customer taps "تایید تغییر"
**THEN** the system:
  - Shows loading: "در حال تغییر زمان..."
  - Sends: `POST /api/v1/Bookings/{bookingId}/reschedule`
    ```json
    {
      "newStartTime": "2024-04-04T10:00:00Z",
      "customerId": "customer-uuid"
    }
    ```
  - On success:
    - Shows toast: "زمان رزرو با موفقیت تغییر یافت"
    - Updates booking details with new time
    - Returns to booking detail page

#### Scenario: Reschedule conflicts with existing booking

**GIVEN** the customer confirmed new time
**WHEN** API returns 409 Conflict (slot taken by another customer)
**THEN** the system:
  - Shows error dialog:
    - "این زمان به تازگی توسط مشتری دیگری رزرو شد"
    - "لطفاً زمان دیگری انتخاب کنید"
  - Returns to time picker
  - Refreshes available slots

### Requirement: Rebook from History

Customers SHALL be able to quickly rebook past services with pre-filled information.

#### Scenario: Customer rebooks from past booking

**GIVEN** the customer is viewing past bookings tab
**WHEN** the customer taps "رزرو مجدد" on a completed booking
**THEN** the system:
  - Navigates to booking wizard (`/booking/{providerId}`)
  - Pre-fills booking context:
    - Provider: Same provider
    - Service: Same service(s)
    - Staff: Same staff (if still available)
  - Skips to date/time selection (Step 3 of wizard)
  - Allows customer to select new date/time
  - Proceeds to confirmation

#### Scenario: Previous staff no longer available

**GIVEN** the customer rebooking a past service
**WHEN** the previously selected staff is no longer employed
**THEN** the system:
  - Shows Step 2 (staff selection) instead of skipping
  - Displays message: "کارمند قبلی در دسترس نیست. لطفاً کارمند جدید انتخاب کنید"
  - Lists currently available staff
  - Allows customer to proceed

### Requirement: Review Submission from Past Bookings

Customers SHALL be able to submit reviews for completed bookings.

#### Scenario: Customer opens review form

**GIVEN** the customer completed a booking without submitting review
**WHEN** the customer taps "ثبت نظر" button on past booking
**THEN** the system displays review modal/page with:
  - Provider name and logo (read-only)
  - Service name (read-only)
  - Star rating selector (1-5 stars, none selected initially)
  - Text area for review:
    - Placeholder: "تجربه خود را با ما به اشتراک بگذارید..."
    - Character limit: 500
    - Character counter
  - "ارسال نظر" button (disabled until rating selected)

#### Scenario: Customer submits review

**GIVEN** the review form is displayed
**WHEN** the customer:
  - Selects 5 stars
  - Types: "خدمات عالی و کارکنان حرفه‌ای"
  - Taps "ارسال نظر"
**THEN** the system:
  - Validates rating (required) and text (optional)
  - Sends: `POST /api/v1/Reviews`
    ```json
    {
      "bookingId": "booking-uuid",
      "providerId": "provider-uuid",
      "rating": 5,
      "comment": "خدمات عالی و کارکنان حرفه‌ای",
      "customerId": "customer-uuid"
    }
    ```
  - On success:
    - Shows toast: "نظر شما با موفقیت ثبت شد"
    - Hides "ثبت نظر" button on booking card
    - Shows "ویرایش نظر" button (if within 7 days)

#### Scenario: Customer tries to review twice

**GIVEN** the customer already submitted a review for a booking
**WHEN** the customer views that booking
**THEN** the "ثبت نظر" button is hidden
**AND** "ویرایش نظر" button is shown (if review <7 days old)

### Requirement: Pagination for Long Lists

Bookings lists SHALL support pagination with infinite scroll.

#### Scenario: Customer has many past bookings

**GIVEN** the customer has 50 completed bookings
**WHEN** the customer opens "گذشته" tab
**THEN** the system:
  - Fetches first page: `GET /api/v1/Bookings/my-bookings?status=Completed&pageNumber=1&pageSize=20`
  - Displays 20 bookings
  - Shows loading indicator at list bottom

#### Scenario: Customer scrolls to load more

**GIVEN** the customer is viewing past bookings (page 1 of 3)
**WHEN** the customer scrolls to bottom of list
**THEN** the system:
  - Triggers pagination
  - Fetches: `GET /api/v1/Bookings/my-bookings?status=Completed&pageNumber=2&pageSize=20`
  - Appends 20 more bookings to list
  - Hides loading indicator

#### Scenario: Customer reaches end of list

**GIVEN** the customer scrolled to last page
**WHEN** all bookings are displayed
**THEN** the system shows end message:
  - "همه رزروها نمایش داده شد"
  - Does not attempt further pagination

### Requirement: Pull-to-Refresh

Customers SHALL be able to refresh bookings list by pulling down.

#### Scenario: Customer pulls to refresh

**GIVEN** the customer is viewing any bookings tab
**WHEN** the customer pulls down on the list
**THEN** the system:
  - Shows refresh indicator (circular spinner)
  - Re-fetches current tab's bookings (page 1)
  - Replaces list with fresh data
  - Hides refresh indicator
  - Completes within 2 seconds

### Requirement: Performance and Caching

Bookings SHALL load quickly with intelligent caching.

#### Scenario: Bookings load within SLA

**GIVEN** the customer opens bookings page
**WHEN** API is called
**THEN** the system:
  - Shows skeleton loader within 100ms
  - Fetches from API
  - Renders bookings within 1.5 seconds (P95)
  - Caches response for 2 minutes

#### Scenario: Cached bookings displayed

**GIVEN** the customer viewed bookings 1 minute ago
**WHEN** the customer reopens bookings page
**THEN** the system:
  - Displays cached bookings immediately (<200ms)
  - Refreshes data in background
  - Updates UI if changes detected (new booking, status change)

### Requirement: Error Handling

Bookings page SHALL handle errors gracefully.

#### Scenario: API fails to load bookings

**GIVEN** the customer opens bookings page
**WHEN** `GET /api/v1/Bookings/my-bookings` returns 500
**THEN** the system displays error state:
  - Error icon (warning triangle)
  - Message: "خطا در بارگذاری رزروها"
  - "تلاش مجدد" button
  - Does not show skeleton loader indefinitely

#### Scenario: Network is offline

**GIVEN** the customer has no internet connection
**WHEN** the customer opens bookings page
**THEN** the system:
  - Displays offline banner: "اتصال اینترنت قطع است"
  - Shows cached bookings (if available)
  - Disables action buttons (cancel, reschedule)
  - Shows toast: "نتایج از حافظه موقت نمایش داده می‌شوند"

### Requirement: Accessibility

Bookings management SHALL be accessible to all users.

#### Scenario: Screen reader announces booking

**GIVEN** a customer using TalkBack
**WHEN** navigating bookings list
**THEN** each booking is announced as:
  - "رزرو آرایشگاه رز، خدمت رنگ مو، تاریخ چهارشنبه ۱۲ فروردین، ساعت ۱۴:۰۰، وضعیت تایید شده، دکمه جزئیات، دکمه لغو"

#### Scenario: Keyboard navigation

**GIVEN** a customer using keyboard
**WHEN** navigating bookings page
**THEN** focus moves through elements in RTL order:
  - Tabs → Booking cards → Action buttons
**AND** focused elements show 2px dark blue outline
**AND** Enter key activates buttons

### Requirement: Analytics

Bookings management SHALL track user behavior.

#### Scenario: Bookings page viewed

**GIVEN** customer opens bookings page
**WHEN** page loads
**THEN** system logs:
  - `bookings_page_viewed`: { tab: "upcoming" }
  - `upcoming_bookings_count`: 3

#### Scenario: Booking cancelled

**GIVEN** customer cancels booking
**WHEN** cancellation succeeds
**THEN** system logs:
  - `booking_cancelled`: { bookingId, reason, hoursUntilBooking }

#### Scenario: Booking rescheduled

**GIVEN** customer reschedules booking
**WHEN** reschedule succeeds
**THEN** system logs:
  - `booking_rescheduled`: { bookingId, oldDate, newDate }
