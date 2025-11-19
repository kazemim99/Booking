# customer-profile Specification Delta (Minimal)

## ADDED Requirements

### Requirement: User Menu Integration
The system SHALL provide a user menu dropdown in the header for authenticated customers to access profile features without leaving the landing page.

#### Scenario: Authenticated customer views user menu
**GIVEN** a customer is authenticated
**WHEN** the customer clicks on their name/initial in the header
**THEN** the system displays a dropdown menu with:
- Colored circle with user's first initial
- Full name and phone number
- Menu items: "ویرایش پروفایل", "نوبت‌های من", "علاقه‌مندی‌ها", "نظرات من", "تنظیمات", "خروج"
**AND** the menu is positioned correctly for RTL layout

#### Scenario: Guest user views header
**GIVEN** a user is not authenticated
**WHEN** the user views the landing page
**THEN** the system displays "ورود / ثبت‌نام" button instead of user menu
**AND** clicking the button redirects to login page

### Requirement: Basic Profile Management
Customers MUST be able to view and edit their name and email address through a simple modal dialog.

#### Scenario: Customer opens profile edit modal
**GIVEN** a customer is authenticated
**WHEN** the customer clicks "ویرایش پروفایل" in user menu
**THEN** the system displays ProfileEditModal with:
- Full name input field (pre-filled with current name)
- Phone number field (display only, not editable)
- Email input field (pre-filled with current email, optional)
- Save and Cancel buttons
**AND** the modal loads in under 500ms

#### Scenario: Customer updates profile
**GIVEN** the customer is editing their profile
**WHEN** the customer changes their name to "سارا احمدی" and email to "sara@example.com"
**AND** clicks "ذخیره"
**THEN** the system validates name (3-100 chars, letters only)
**AND** validates email format (if provided)
**AND** sends PATCH /api/v1/customers/profile
**AND** displays success toast: "اطلاعات با موفقیت ذخیره شد"
**AND** closes the modal
**AND** updates user menu to show new name

#### Scenario: Customer tries to edit phone number
**GIVEN** the customer is viewing their profile in edit modal
**WHEN** the customer clicks on phone number field
**THEN** the field is disabled (not editable)
**AND** a tooltip displays: "برای تغییر شماره موبایل با پشتیبانی تماس بگیرید"

### Requirement: Upcoming Bookings Display
The system SHALL display the customer's next 5 upcoming bookings in a sidebar for quick access.

#### Scenario: Customer opens bookings sidebar
**GIVEN** a customer has 3 upcoming bookings
**WHEN** the customer clicks "نوبت‌های من" in user menu
**THEN** the system displays BookingsSidebar (slides in from left)
**AND** shows "آینده" and "گذشته" tabs
**AND** "آینده" tab is selected by default
**AND** displays 3 booking cards with:
- Provider name and logo
- Service name
- Date and time (Jalali format, Persian numbers)
- Status badge (e.g., "تایید شده")
- "لغو" and "تغییر زمان" buttons

#### Scenario: Customer has no upcoming bookings
**GIVEN** a customer has no future bookings
**WHEN** the customer opens bookings sidebar on "آینده" tab
**THEN** the system displays empty state with:
- Illustration or icon
- Message: "شما نوبت آینده‌ای ندارید"
- "رزرو نوبت جدید" button

#### Scenario: Customer cancels booking from sidebar
**GIVEN** the customer is viewing upcoming bookings
**WHEN** the customer clicks "لغو" on a booking
**THEN** the system displays confirmation dialog: "آیا از لغو این نوبت مطمئن هستید؟"
**AND** upon confirmation, sends cancellation request
**AND** removes booking from upcoming list
**AND** displays toast: "نوبت با موفقیت لغو شد"

### Requirement: Booking History Display
Customers MUST be able to view their past bookings (last 20) with pagination and rebooking capability.

#### Scenario: Customer views booking history
**GIVEN** a customer has 25 completed bookings
**WHEN** the customer switches to "گذشته" tab in bookings sidebar
**THEN** the system displays first 20 bookings sorted by date (newest first)
**AND** each booking shows:
- Provider name
- Service name
- Date (Jalali format)
- Status badge (e.g., "تکمیل شده", "لغو شده")
- "رزرو مجدد" button
**AND** shows "بارگذاری بیشتر" button at bottom

#### Scenario: Customer rebooks from history
**GIVEN** the customer is viewing past bookings
**WHEN** the customer clicks "رزرو مجدد" on a booking
**THEN** the system redirects to booking wizard `/booking/{providerId}`
**AND** pre-fills provider and service selections
**AND** allows customer to select new date/time

### Requirement: Favorite Providers Management
Customers MUST be able to save favorite providers for quick access and rebooking.

#### Scenario: Customer adds provider to favorites
**GIVEN** the customer is viewing a provider card on landing page
**WHEN** the customer clicks the heart icon (outlined)
**THEN** the system sends POST /api/v1/customers/favorites/{providerId}
**AND** the heart icon fills with color
**AND** displays toast: "به علاقه‌مندی‌ها اضافه شد"

#### Scenario: Customer removes provider from favorites
**GIVEN** the customer has previously favorited a provider
**WHEN** the customer clicks the heart icon (filled)
**THEN** the system displays confirmation: "این ارائه‌دهنده از لیست علاقه‌مندی‌ها حذف شود؟"
**AND** upon confirmation, sends DELETE /api/v1/customers/favorites/{providerId}
**AND** the heart icon becomes outlined
**AND** displays toast: "از علاقه‌مندی‌ها حذف شد"

#### Scenario: Customer views favorites list
**GIVEN** the customer has 8 favorite providers
**WHEN** the customer clicks "علاقه‌مندی‌ها" in user menu
**THEN** the system displays FavoritesModal with:
- Grid layout (2 columns on desktop, 1 on mobile)
- 8 provider cards showing logo, name, category, rating
- Filled heart icon on each card (click to remove)
- "رزرو" button on each card (redirects to booking wizard)

#### Scenario: Customer has no favorites
**GIVEN** the customer has not favorited any providers
**WHEN** the customer opens favorites modal
**THEN** the system displays empty state:
- Heart icon illustration
- Message: "شما هنوز ارائه‌دهنده‌ای را به علاقه‌مندی‌ها اضافه نکرده‌اید"
- "جستجوی ارائه‌دهندگان" button (redirects to search page)

### Requirement: Review Management
Customers MUST be able to view all their submitted reviews and edit recent reviews (within 7 days).

#### Scenario: Customer views their reviews
**GIVEN** the customer has submitted 5 reviews
**WHEN** the customer clicks "نظرات من" in user menu
**THEN** the system displays ReviewsModal with list of 5 reviews
**AND** each review shows:
- Provider name and logo
- Service name
- Star rating (1-5)
- Review text
- Submission date (Jalali format)
- "ویرایش" button (if review is <7 days old)

#### Scenario: Customer edits recent review
**GIVEN** the customer submitted a review 3 days ago
**WHEN** the customer clicks "ویرایش" on the review
**THEN** the system displays edit form with:
- Star rating selector (pre-filled with current rating)
- Text area (pre-filled with current review text)
- Character counter (max 500 chars)
- "ذخیره" and "انصراف" buttons
**AND** upon clicking "ذخیره", validates input
**AND** sends PATCH /api/v1/customers/reviews/{id}
**AND** updates review display with "(ویرایش شده)" label
**AND** displays toast: "نظر شما با موفقیت ویرایش شد"

#### Scenario: Customer tries to edit old review
**GIVEN** the customer submitted a review 10 days ago
**WHEN** the customer views the review in reviews modal
**THEN** the "ویرایش" button is not displayed
**AND** a message shows: "فقط نظرات کمتر از ۷ روز قابل ویرایش هستند"

#### Scenario: Customer has no reviews
**GIVEN** the customer has not submitted any reviews
**WHEN** the customer opens reviews modal
**THEN** the system displays empty state:
- Star icon illustration
- Message: "شما هنوز نظری ثبت نکرده‌اید"
- "جستجوی خدمات" button

### Requirement: Notification Preferences
Customers MUST be able to control notification channels and reminder timing for their bookings.

#### Scenario: Customer opens settings modal
**GIVEN** a customer is authenticated
**WHEN** the customer clicks "تنظیمات" in user menu
**THEN** the system displays SettingsModal with:
- "اعلان‌ها" section
- SMS notifications toggle (current state: on/off)
- Email notifications toggle (current state: on/off)
- Reminder timing dropdown with options: "۱ ساعت قبل", "۱ روز قبل", "۳ روز قبل"
- "حساب کاربری" section with support contact info

#### Scenario: Customer updates notification preferences
**GIVEN** the customer has SMS enabled and email disabled
**WHEN** the customer toggles email notifications ON
**AND** changes reminder timing to "۳ روز قبل"
**THEN** the system immediately sends PATCH /api/v1/customers/preferences
**AND** displays brief success toast (auto-dismiss after 2 seconds): "تنظیمات ذخیره شد"
**AND** future bookings respect new preferences

#### Scenario: Customer disables all notifications
**GIVEN** the customer is in settings modal
**WHEN** the customer toggles both SMS and Email notifications OFF
**THEN** the system displays warning: "با غیرفعال کردن تمام اعلان‌ها، یادآوری نوبت دریافت نخواهید کرد. ادامه می‌دهید؟"
**AND** upon confirmation, saves preferences
**AND** customer receives no booking reminders

### Requirement: Mobile Bottom Navigation
On mobile devices, the system SHALL provide a bottom navigation bar for quick access to customer features.

#### Scenario: Customer views landing page on mobile
**GIVEN** the customer is authenticated on a mobile device (<768px width)
**WHEN** the landing page loads
**THEN** the system displays bottom navigation bar with 5 tabs:
- "خانه" (Home icon) - Scrolls to top of landing page
- "جستجو" (Search icon) - Opens provider search
- "نوبت‌ها" (Calendar icon) - Opens bookings bottom sheet
- "علاقه‌مندی‌ها" (Heart icon) - Opens favorites bottom sheet
- "پروفایل" (User icon) - Opens user menu as bottom sheet
**AND** the active tab is highlighted

#### Scenario: Customer opens bookings on mobile
**GIVEN** the customer is on mobile
**WHEN** the customer taps "نوبت‌ها" in bottom navigation
**THEN** the system displays bookings as bottom sheet (not sidebar)
**AND** bottom sheet can be swiped down to dismiss
**AND** bottom sheet snaps to half/full height

### Requirement: Persian RTL Support
All customer profile components MUST support Persian language with RTL layout and cultural formatting.

#### Scenario: Customer views profile components
**GIVEN** the customer is viewing any profile modal/sidebar
**WHEN** the component loads
**THEN** all text is displayed in Persian
**AND** layout direction is RTL (right-to-left)
**AND** dates are formatted in Jalali calendar
**AND** numbers are displayed in Persian digits (۰۱۲۳۴۵۶۷۸۹)
**AND** currency is formatted as "۱۲۳٬۴۵۶ تومان"

### Requirement: Performance Optimization
All modals and sidebars MUST load quickly with minimal impact on page performance.

#### Scenario: Customer opens any modal
**GIVEN** the customer clicks a menu item
**WHEN** the modal/sidebar begins to load
**THEN** the system displays loading skeleton within 100ms
**AND** fetches data from API
**AND** renders content within 500ms total
**AND** uses cached data if available (5 min cache)

#### Scenario: Customer repeatedly opens/closes modals
**GIVEN** the customer opens and closes the bookings sidebar multiple times
**WHEN** reopening within 5 minutes
**THEN** the system displays cached data immediately (no API call)
**AND** refreshes data in background
**AND** updates UI if data changed

### Requirement: Error Handling
The system SHALL gracefully handle API errors and network failures with user-friendly messages.

#### Scenario: API request fails
**GIVEN** the customer tries to update their profile
**WHEN** the API returns 500 Internal Server Error
**THEN** the system displays error toast: "خطایی رخ داد. لطفاً دوباره تلاش کنید."
**AND** provides retry button in toast
**AND** keeps modal open with user's entered data

#### Scenario: Network is offline
**GIVEN** the customer loses internet connection
**WHEN** the customer tries to open bookings sidebar
**THEN** the system displays offline banner at top: "اتصال اینترنت قطع است"
**AND** shows cached bookings if available
**AND** disables action buttons (Cancel, Rebook)

### Requirement: User Initial Display (No Avatar)
The system SHALL display a colored circle with the customer's first initial instead of avatar images.

#### Scenario: Customer views their initial
**GIVEN** a customer named "سارا احمدی" is authenticated
**WHEN** the customer views the user menu
**THEN** the system displays a colored circle
**AND** the circle contains the letter "س" (first letter of first name)
**AND** the circle has a consistent color (based on user ID)
**AND** color is chosen from predefined palette (4 colors)

#### Scenario: Customer with English name
**GIVEN** a customer named "Sara Ahmadi" is authenticated
**WHEN** the customer views the user menu
**THEN** the system displays "S" in the circle

### Requirement: Session Management
The system SHALL maintain customer authentication state and provide logout functionality.

#### Scenario: Customer logs out
**GIVEN** the customer is authenticated
**WHEN** the customer clicks "خروج" in user menu
**THEN** the system displays confirmation: "آیا می‌خواهید از حساب کاربری خود خارج شوید؟"
**AND** upon confirmation, clears authentication token
**AND** redirects to landing page (guest view)
**AND** clears all cached customer data

#### Scenario: Session expires
**GIVEN** the customer's session has expired (token invalid)
**WHEN** the customer tries to access profile features
**THEN** the system redirects to login page
**AND** displays message: "جلسه شما منقضی شده است. لطفاً دوباره وارد شوید."
**AND** returns to requested page after re-authentication

### Requirement: Accessibility (WCAG 2.1 AA)
Customer profile components MUST be accessible to users with disabilities.

#### Scenario: Screen reader navigation
**GIVEN** a customer using a screen reader
**WHEN** the customer navigates the user menu
**THEN** each menu item has descriptive ARIA label
**AND** modals announce their title when opened
**AND** focus is trapped within open modal
**AND** ESC key closes modal
**AND** focus returns to trigger element after closing

#### Scenario: Keyboard navigation
**GIVEN** a customer using keyboard only
**WHEN** the customer presses Tab key in user menu dropdown
**THEN** focus moves through menu items in RTL order (right to left)
**AND** each focused element has visible focus indicator (2px outline)
**AND** Enter/Space key activates buttons
**AND** ESC key closes dropdown

### Requirement: Data Validation
All user inputs MUST be validated on both client and server sides.

#### Scenario: Invalid name entry
**GIVEN** the customer is editing their profile
**WHEN** the customer enters name "A" (too short)
**AND** clicks "ذخیره"
**THEN** the system displays error: "نام باید حداقل ۳ کاراکتر باشد"
**AND** does not submit to server

#### Scenario: Invalid email format
**GIVEN** the customer is editing their profile
**WHEN** the customer enters email "invalid-email"
**AND** clicks "ذخیره"
**THEN** the system displays error: "فرمت ایمیل معتبر نیست"
**AND** does not submit to server

#### Scenario: Server-side validation failure
**GIVEN** the customer submits a valid-looking email
**WHEN** the server detects email is already in use
**THEN** the system displays error from server: "این ایمیل قبلاً ثبت شده است"
**AND** keeps the modal open for correction

## MODIFIED Requirements

None - This is a new feature with no modifications to existing specs.

## REMOVED Requirements

None - This is purely additive functionality.
