# customer-mobile-booking Specification

## Purpose

Enable customers to book services through an intuitive multi-step mobile wizard that guides them from service selection to booking confirmation. The booking flow must be simple, visual, and minimize decision fatigue while supporting Persian date/time formats and cultural expectations.

## ADDED Requirements

### Requirement: Booking Wizard Entry Points

Customers SHALL be able to initiate booking from multiple entry points with contextual pre-filling.

#### Scenario: Customer starts booking from provider detail page

**GIVEN** a customer is viewing provider detail page for "آرایشگاه رز"
**WHEN** the customer taps "رزرو نوبت" sticky button
**THEN** the system navigates to `/booking/{providerId}`
**AND** displays booking wizard with:
  - Provider name and logo in header
  - Step indicator: "۱ از ۴" (Step 1 of 4)
  - Progress bar: 25% filled
  - Step 1 title: "انتخاب خدمت"
**AND** provider context is pre-loaded (no API call needed)

#### Scenario: Customer starts booking from search results

**GIVEN** search results display providers
**WHEN** the customer taps "رزرو" button on a provider card
**THEN** the system navigates to `/booking/{providerId}`
**AND** fetches provider details if not cached
**AND** displays booking wizard starting at Step 1

#### Scenario: Customer rebooks from history

**GIVEN** the customer previously booked "رنگ مو" at "آرایشگاه رز"
**WHEN** the customer taps "رزرو مجدد" on past booking
**THEN** the system navigates to booking wizard
**AND** pre-fills:
  - Provider: آرایشگاه رز
  - Service: رنگ مو
  - Skips to Step 2 (staff selection) or Step 3 (date/time)

### Requirement: Step 1 - Service Selection

The booking wizard SHALL display provider's services with pricing and allow single or multiple selection.

#### Scenario: Customer views available services

**GIVEN** the booking wizard is at Step 1
**WHEN** the step loads
**THEN** the system:
  - Fetches services: `GET /api/v1/Providers/{providerId}/services`
  - Displays service list with:
    - Service icon (haircut, nail, massage)
    - Service name (e.g., "کوتاهی مو", "رنگ مو")
    - Duration with Persian numbers (e.g., "⏱ ۳۰ دقیقه", "⏱ ۱۲۰ دقیقه")
    - Price with Persian numbers (e.g., "💰 ۱۵۰٬۰۰۰ تومان")
    - Checkbox for selection (unchecked)
  - Shows services sorted by popularity or category

#### Scenario: Customer selects single service

**GIVEN** Step 1 displays services
**WHEN** the customer taps "رنگ مو" service
**THEN** the system:
  - Checks the checkbox for "رنگ مو"
  - Highlights card with dark blue border (#1A365D)
  - Updates summary footer:
    - "جمع: ۴۵۰٬۰۰۰ تومان | ۱۲۰ دقیقه"
  - Enables "ادامه" button

#### Scenario: Customer selects multiple services

**GIVEN** the customer selected "رنگ مو" (120 min, 450,000 تومان)
**WHEN** the customer taps "ماساژ صورت" (45 min, 200,000 تومان)
**THEN** the system:
  - Checks both checkboxes
  - Highlights both cards
  - Updates summary:
    - "جمع: ۶۵۰٬۰۰۰ تومان | ۱۶۵ دقیقه"
  - Recalculates total price and duration

#### Scenario: Customer deselects service

**GIVEN** the customer selected 2 services
**WHEN** the customer taps a selected service again
**THEN** the system:
  - Unchecks checkbox
  - Removes highlight border
  - Recalculates summary (removes service price and duration)

#### Scenario: No service selected

**GIVEN** Step 1 is displayed
**WHEN** the customer taps "ادامه" without selecting any service
**THEN** the system:
  - Disables "ادامه" button (greyed out)
  - Shows tooltip: "لطفاً حداقل یک خدمت انتخاب کنید"

#### Scenario: Customer proceeds to next step

**GIVEN** the customer selected 1+ services
**WHEN** the customer taps "ادامه"
**THEN** the system:
  - Saves selected services to booking state
  - Navigates to Step 2 (staff selection)
  - Updates progress bar to 50%
  - Updates step indicator: "۲ از ۴"

### Requirement: Step 2 - Staff Selection (Optional)

The wizard SHALL allow customers to select a specific staff member or choose "any available".

#### Scenario: Provider has multiple staff

**GIVEN** "آرایشگاه رز" has 3 staff members
**WHEN** Step 2 loads
**THEN** the system:
  - Fetches staff: `GET /api/v1/Providers/{providerId}/staff`
  - Displays step title: "انتخاب کارمند"
  - Shows "فرقی ندارد (زودترین نوبت)" option (checked by default)
  - Lists staff cards:
    - Staff photo (circular, 60dp)
    - Name (e.g., "مریم احمدی")
    - Specialty (e.g., "متخصص رنگ مو")
    - Rating (e.g., "⭐ ۴.۹")
    - Years of experience (e.g., "۵ سال سابقه")
    - Radio button

#### Scenario: Customer selects specific staff

**GIVEN** Step 2 displays staff list
**WHEN** the customer taps "مریم احمدی" card
**THEN** the system:
  - Selects radio button for "مریم"
  - Deselects "فرقی ندارد"
  - Highlights "مریم" card with dark blue border
  - Saves staff selection to booking state

#### Scenario: Customer chooses any available staff

**GIVEN** the customer selected a specific staff member
**WHEN** the customer taps "فرقی ندارد (زودترین نوبت)"
**THEN** the system:
  - Selects "فرقی ندارد" radio button
  - Deselects specific staff
  - Removes card highlight
  - Booking will use first available staff

#### Scenario: Provider is solo (no staff)

**GIVEN** the provider works alone (no staff members)
**WHEN** Step 2 loads
**THEN** the system:
  - Auto-selects provider as staff
  - Skips Step 2 entirely
  - Proceeds directly to Step 3 (date/time)
  - Updates progress to show "۲ از ۳" instead

#### Scenario: Customer proceeds from Step 2

**GIVEN** a staff selection is made
**WHEN** the customer taps "ادامه"
**THEN** the system:
  - Saves staff selection (or null if "any available")
  - Navigates to Step 3 (date/time selection)
  - Updates progress to 75%
  - Updates indicator: "۳ از ۴"

### Requirement: Step 3 - Date and Time Selection

The wizard SHALL display available dates and time slots using Jalali calendar and Persian numbers.

#### Scenario: Customer views date/time picker

**GIVEN** Step 3 loads
**WHEN** the wizard displays date/time step
**THEN** the system:
  - Shows step title: "انتخاب تاریخ و ساعت"
  - Displays Jalali calendar for current month (e.g., "فروردین ۱۴۰۳")
  - Marks today's date
  - Grays out past dates (not selectable)
  - Highlights available dates (green dot indicator)
  - Shows "شنبه", "یکشنبه", etc. as weekday headers (RTL)

#### Scenario: Customer selects a date

**GIVEN** the calendar is displayed
**WHEN** the customer taps "۱۲ فروردین" (a Wednesday)
**THEN** the system:
  - Highlights selected date with dark blue circle
  - Fetches available slots: `GET /api/v1/Availability/slots?providerId={id}&serviceId={id}&staffId={id}&date=2024-04-01`
  - Displays loading skeleton for time slots
  - Renders time slots grid below calendar

#### Scenario: Available time slots displayed

**GIVEN** the customer selected a date
**WHEN** availability API returns slots
**THEN** the system displays time chips in grid:
  - Available slots: Green background (e.g., "۹:۰۰", "۹:۳۰", "۱۰:۰۰")
  - Booked slots: Gray background, strikethrough (e.g., "~~۱۱:۰۰~~")
  - Past slots (for today): Grayed out
  - Slots displayed in 30-minute increments
**AND** slots scroll horizontally if many available

#### Scenario: Customer selects time slot

**GIVEN** available slots are displayed
**WHEN** the customer taps "۱۴:۰۰" slot
**THEN** the system:
  - Highlights slot with dark blue background
  - Shows confirmation summary:
    - "تاریخ: چهارشنبه، ۱۲ فروردین ۱۴۰۳"
    - "ساعت: ۱۴:۰۰"
    - "مدت: ۱۲۰ دقیقه"
    - "پایان تقریبی: ۱۶:۰۰"
  - Enables "ادامه" button

#### Scenario: No availability on selected date

**GIVEN** the customer selected a date
**WHEN** all time slots are booked
**THEN** the system displays:
  - Empty state: "متأسفانه در این تاریخ نوبت خالی وجود ندارد"
  - "انتخاب تاریخ دیگر" button
  - Suggestion: Shows next 3 dates with availability

#### Scenario: Customer changes date

**GIVEN** the customer selected date and time
**WHEN** the customer taps a different date
**THEN** the system:
  - Clears time slot selection
  - Fetches availability for new date
  - Displays new time slots
  - Disables "ادامه" until new time selected

#### Scenario: Real-time availability check

**GIVEN** the customer is viewing time slots
**WHEN** another customer books the same slot simultaneously
**THEN** the system:
  - Polls availability every 30 seconds (optional)
  - OR validates slot on "ادامه" click
  - If slot becomes unavailable, shows error:
    - "این نوبت به تازگی توسط مشتری دیگری رزرو شد"
    - "لطفاً زمان دیگری انتخاب کنید"

#### Scenario: Customer proceeds to review

**GIVEN** date and time are selected
**WHEN** the customer taps "ادامه"
**THEN** the system:
  - Saves date/time to booking state
  - Navigates to Step 4 (review & confirm)
  - Updates progress to 100%
  - Updates indicator: "۴ از ۴"

### Requirement: Step 4 - Review and Confirmation

The wizard SHALL display booking summary for final review before submission.

#### Scenario: Customer reviews booking details

**GIVEN** Step 4 loads
**WHEN** the review page displays
**THEN** the system shows:
  - Step title: "تایید رزرو"
  - **Provider Info**:
    - Logo and name
    - Address
    - "مسیریابی" and "تماس" buttons
  - **Service Summary**:
    - Service name(s)
    - Total duration
  - **Staff**:
    - Staff photo and name (or "کارمند در دسترس")
  - **Date/Time**:
    - Full date: "چهارشنبه، ۱۲ فروردین ۱۴۰۳"
    - Time: "ساعت ۱۴:۰۰"
  - **Pricing**:
    - Service price breakdown
    - Total: "۴۵۰٬۰۰۰ تومان"
    - Deposit (if required): "بیعانه: ۱۰۰٬۰۰۰ تومان"
  - **Notes** (optional):
    - Text area: "یادداشت (اختیاری)"
    - Placeholder: "توضیحات خود را وارد کنید..."
  - **Cancellation Policy**:
    - Checkbox: "با قوانین لغو رزرو موافقم"
    - Policy text: "لغو تا ۲۴ ساعت قبل: بدون جریمه. کمتر از ۲۴ ساعت: ۵۰٪ جریمه"
  - **CTA**: "تایید و پرداخت" button (large, dark blue primary)

#### Scenario: Customer adds notes

**GIVEN** the review page is displayed
**WHEN** the customer types in notes field: "لطفاً از رنگ روشن استفاده کنید"
**THEN** the system:
  - Stores notes in booking state
  - Character counter shows "۳۸/۵۰۰"
  - Allows up to 500 Persian characters

#### Scenario: Customer edits a step

**GIVEN** the review page shows booking summary
**WHEN** the customer taps "ویرایش" next to service/staff/date section
**THEN** the system navigates back to that step
**AND** preserves other selections
**AND** allows customer to modify and return to review

#### Scenario: Customer confirms booking

**GIVEN** all booking details are reviewed
**AND** cancellation policy checkbox is checked
**WHEN** the customer taps "تایید و پرداخت"
**THEN** the system:
  - Validates all inputs
  - Shows loading overlay: "در حال ثبت رزرو..."
  - Sends `POST /api/v1/Bookings`:
    ```json
    {
      "customerId": "customer-uuid",
      "providerId": "provider-uuid",
      "serviceIds": ["service-uuid"],
      "staffProviderId": "staff-uuid",
      "startTime": "2024-04-01T14:00:00Z",
      "customerNotes": "لطفاً از رنگ روشن استفاده کنید"
    }
    ```
  - Receives booking confirmation with booking ID

#### Scenario: Booking creation succeeds

**GIVEN** the customer confirmed booking
**WHEN** API returns 201 Created
**THEN** the system:
  - Navigates to confirmation page (`/booking/confirmation/{bookingId}`)
  - Displays success screen (see next requirement)
  - Clears booking wizard state

#### Scenario: Booking creation fails

**GIVEN** the customer confirmed booking
**WHEN** API returns 409 Conflict (slot no longer available)
**THEN** the system:
  - Hides loading overlay
  - Displays error dialog:
    - "این نوبت دیگر در دسترس نیست"
    - "لطفاً زمان دیگری انتخاب کنید"
    - "بازگشت به انتخاب زمان" button
  - Navigates back to Step 3 on button tap

#### Scenario: Cancellation policy not accepted

**GIVEN** the review page is displayed
**WHEN** the customer taps "تایید و پرداخت" without checking policy checkbox
**THEN** the system:
  - Shakes checkbox (animation)
  - Displays error toast: "لطفاً قوانین لغو رزرو را بپذیرید"
  - Does not submit booking

### Requirement: Booking Confirmation Page

After successful booking, the system SHALL display confirmation with booking details and next actions.

#### Scenario: Booking confirmed successfully

**GIVEN** booking creation succeeded
**WHEN** confirmation page loads
**THEN** the system displays:
  - Success icon (✅ green checkmark animation)
  - Title: "رزرو شما ثبت شد!"
  - Booking reference: "کد پیگیری: #BK۱۲۳۴۵"
  - Confirmation message: "یک پیامک حاوی جزئیات رزرو برای شما ارسال شد"
  - **Quick Actions**:
    - "مشاهده جزئیات رزرو" (navigates to `/bookings/{id}`)
    - "افزودن به تقویم" (adds to device calendar)
    - "اشتراک‌گذاری" (share booking details)
    - "بازگشت به خانه" (navigates to `/home`)
**AND** sends analytics event: `booking_created`

#### Scenario: Customer adds to calendar

**GIVEN** confirmation page is displayed
**WHEN** the customer taps "افزودن به تقویم"
**THEN** the system:
  - Opens device calendar app (iOS Calendar or Google Calendar)
  - Pre-fills event:
    - Title: "رنگ مو - آرایشگاه رز"
    - Date/time: Booking start time
    - Duration: Service duration
    - Location: Provider address
    - Notes: Booking reference and provider phone

#### Scenario: Customer shares booking

**GIVEN** confirmation page is displayed
**WHEN** the customer taps "اشتراک‌گذاری"
**THEN** the system opens native share sheet with text:
  ```
  رزرو من:
  📅 چهارشنبه، ۱۲ فروردین ۱۴۰۳
  ⏰ ساعت ۱۴:۰۰
  📍 آرایشگاه رز
  کد پیگیری: #BK۱۲۳۴۵
  ```
**AND** allows sharing via SMS, WhatsApp, Telegram, etc.

### Requirement: Wizard Navigation and State

The booking wizard SHALL support back navigation, step skipping, and state persistence.

#### Scenario: Customer navigates back

**GIVEN** the customer is on Step 3 (date/time)
**WHEN** the customer taps back button (hardware or app bar)
**THEN** the system:
  - Navigates to Step 2 (staff selection)
  - Preserves selected date/time (does not clear)
  - Shows previous progress: "۲ از ۴"

#### Scenario: Customer exits wizard

**GIVEN** the customer is in the middle of booking (Step 2)
**WHEN** the customer taps "X" close button or exits app
**THEN** the system displays confirmation dialog:
  - "رزرو شما ناتمام است. می‌خواهید خارج شوید؟"
  - "خروج" (discards booking) and "ادامه رزرو" buttons

#### Scenario: Booking state persisted

**GIVEN** the customer started booking and selected services
**WHEN** the customer exits wizard
**AND** returns within 30 minutes
**THEN** the system:
  - Restores booking wizard state
  - Shows toast: "رزرو ناتمام شما بازیابی شد"
  - Allows customer to continue from last step

#### Scenario: Booking state expired

**GIVEN** the customer started booking 2 hours ago
**WHEN** the customer reopens app
**THEN** the system clears expired booking state
**AND** starts fresh booking if customer tries again

### Requirement: Performance

The booking wizard SHALL load quickly and handle concurrent bookings gracefully.

#### Scenario: Wizard loads within SLA

**GIVEN** the customer initiates booking
**WHEN** wizard navigates between steps
**THEN** each step transition completes within 300ms
**AND** API calls (services, staff, availability) complete within 2 seconds
**AND** skeleton loaders show during fetch

#### Scenario: Optimistic availability display

**GIVEN** the customer selects a date
**WHEN** fetching time slots
**THEN** the system:
  - Displays skeleton chips (gray placeholders)
  - Renders actual slots as they load
  - Shows all slots within 1 second

### Requirement: Accessibility

The booking wizard SHALL be accessible to screen readers.

#### Scenario: Screen reader announces steps

**GIVEN** a customer using TalkBack
**WHEN** navigating wizard steps
**THEN** system announces:
  - "مرحله ۱ از ۴: انتخاب خدمت"
  - Each service: "رنگ مو، ۱۲۰ دقیقه، ۴۵۰ هزار تومان، انتخاب نشده"
  - Progress: "پیشرفت ۲۵ درصد"

### Requirement: Analytics

The wizard SHALL track booking funnel for optimization.

#### Scenario: Step completion tracked

**GIVEN** customer progresses through wizard
**WHEN** each step completes
**THEN** system logs events:
  - `booking_step_1_completed`: { serviceIds, totalPrice }
  - `booking_step_2_completed`: { staffId }
  - `booking_step_3_completed`: { date, time }
  - `booking_confirmed`: { bookingId, providerId, revenue }

#### Scenario: Drop-off tracked

**GIVEN** customer exits wizard mid-flow
**WHEN** exit occurs
**THEN** system logs:
  - `booking_abandoned`: { step: 2, reason: "back_button" }
