# customer-profile Specification Delta

## ADDED Requirements

### Requirement: Customer Profile Display
The system SHALL provide a comprehensive profile page where customers can view and manage their account information, booking history, preferences, and settings.

#### Scenario: Customer accesses profile page
**GIVEN** a customer is authenticated
**WHEN** the customer navigates to "/customer/profile"
**THEN** the system displays the profile page with:
- Profile header showing avatar, name, phone, email, and verification badges
- Tabbed navigation with sections: Bookings, Favorites, History, Payments, Reviews, Settings
- Quick actions sidebar with "New Booking", "Messages", and "Loyalty Points"
**AND** the page loads in under 2 seconds

#### Scenario: Customer views profile on mobile
**GIVEN** a customer accesses the profile page on a mobile device
**WHEN** the page loads
**THEN** the system displays:
- Mobile-optimized layout with bottom navigation
- Swipeable tabs for section switching
- Touch-optimized buttons with minimum 44px height
- Responsive grid layout adapting to screen size

### Requirement: Personal Information Management
Customers MUST be able to view and edit their personal information including name, phone number, email, and profile picture.

#### Scenario: Customer updates profile information
**GIVEN** a customer is on the profile page
**WHEN** the customer clicks "ویرایش" (Edit) button
**THEN** the system displays editable form fields for:
- Full name (3-100 characters, Persian/English letters only)
- Email address (optional, validated email format)
- Birth date (optional, Jalali calendar picker)
**AND** phone number is displayed but not editable (requires verification flow)

#### Scenario: Customer uploads profile avatar
**GIVEN** a customer is editing their profile
**WHEN** the customer clicks on the avatar placeholder
**THEN** the system opens file picker accepting JPEG, PNG (max 5MB)
**AND** displays crop interface for selected image
**AND** upon confirming crop, uploads to S3 via pre-signed URL
**AND** updates profile with new avatar URL
**AND** displays new avatar with CDN URL

#### Scenario: Customer saves profile changes
**GIVEN** a customer has edited profile fields
**WHEN** the customer clicks "ذخیره" (Save)
**THEN** the system validates all fields (name length, email format)
**AND** if validation passes, sends PATCH request to /api/v1/customers/profile
**AND** displays success toast: "اطلاعات با موفقیت ذخیره شد"
**AND** updates local state optimistically
**AND** if API fails, reverts state and shows error toast

### Requirement: Upcoming Appointments Widget
The system SHALL display a dashboard widget showing the customer's next 3 upcoming bookings with quick action buttons.

#### Scenario: Customer views upcoming bookings
**GIVEN** a customer has upcoming confirmed bookings
**WHEN** the customer views the profile page
**THEN** the system displays up to 3 upcoming bookings sorted by date ascending
**AND** each booking shows:
- Provider name and avatar
- Service name
- Persian date and time (Jalali format)
- Countdown timer ("۲ روز و ۳ ساعت باقی مانده")
- Status badge (Confirmed, Pending, etc.)
- Quick action buttons: "لغو" (Cancel), "تغییر زمان" (Reschedule), "جزئیات" (Details)

#### Scenario: Customer has no upcoming bookings
**GIVEN** a customer has no confirmed future bookings
**WHEN** the customer views the profile page
**THEN** the system displays empty state with:
- Illustration or icon
- Message: "شما نوبت آینده‌ای ندارید"
- Call-to-action button: "رزرو نوبت جدید"

### Requirement: Booking History
Customers MUST be able to view their complete booking history with filtering, sorting, and export capabilities.

#### Scenario: Customer views booking history
**GIVEN** a customer selects the "تاریخچه" (History) tab
**WHEN** the tab loads
**THEN** the system displays a paginated table (20 items per page) with columns:
- Date (Jalali format, sortable)
- Provider (name + avatar)
- Service (name + duration)
- Staff member (if assigned)
- Status (badge: Completed, Cancelled, No-Show)
- Amount paid (Persian numbers, تومان)
- Actions (View Receipt, Rebook, Review)

#### Scenario: Customer filters booking history
**GIVEN** a customer is viewing booking history
**WHEN** the customer applies filters (date range, provider, status, service type)
**THEN** the system sends filtered query to backend
**AND** displays filtered results
**AND** updates URL query parameters for bookmarkability
**AND** shows count: "۴۵ نتیجه یافت شد"

#### Scenario: Customer exports booking history
**GIVEN** a customer is viewing booking history
**WHEN** the customer clicks "دانلود PDF" or "دانلود Excel"
**THEN** the system generates export file with applied filters
**AND** initiates download with filename: "booking-history-YYYY-MM-DD.pdf"

#### Scenario: Customer rebooks from history
**GIVEN** a customer views a completed booking in history
**WHEN** the customer clicks "رزرو مجدد" (Rebook)
**THEN** the system redirects to booking wizard
**AND** pre-fills provider, service, and staff selections from historical booking
**AND** allows customer to select new date/time

### Requirement: Favorite Providers Management
Customers MUST be able to save favorite providers for quick access and receive personalized recommendations.

#### Scenario: Customer views favorite providers
**GIVEN** a customer selects the "علاقه‌مندی‌ها" (Favorites) tab
**WHEN** the tab loads
**THEN** the system displays saved providers in grid view (3 columns on desktop, 1 on mobile)
**AND** each provider card shows:
- Provider logo/avatar
- Business name
- Category (رستوران, سالن زیبایی, etc.)
- Rating (۴.۵ ⭐ - ۱۲۳ نظر)
- Distance from customer ("۲.۵ کیلومتر")
- Last visit date ("آخرین بازدید: ۱۵ آذر")
- Total bookings count ("۸ نوبت")
- Quick book button and view profile link

#### Scenario: Customer sorts favorite providers
**GIVEN** a customer is viewing favorites
**WHEN** the customer selects sort option (Recent, Rating, Distance, Total Visits)
**THEN** the system reorders favorites based on selected criterion
**AND** preserves sort preference in local storage

#### Scenario: Customer removes favorite provider
**GIVEN** a customer is viewing favorites
**WHEN** the customer clicks heart icon (filled) on a provider card
**THEN** the system prompts confirmation: "این ارائه‌دهنده از لیست علاقه‌مندی‌ها حذف شود؟"
**AND** upon confirmation, sends DELETE /api/v1/customers/favorites/{providerId}
**AND** removes card from view with fade-out animation

#### Scenario: Customer has no favorites
**GIVEN** a customer has not saved any favorite providers
**WHEN** the customer views favorites tab
**THEN** the system displays empty state with:
- Illustration of heart icon
- Message: "شما هنوز ارائه‌دهنده‌ای را به علاقه‌مندی‌ها اضافه نکرده‌اید"
- Call-to-action: "جستجوی ارائه‌دهندگان"

### Requirement: Payment Methods Management
Customers MUST be able to securely view, add, and manage saved payment methods for faster checkout.

#### Scenario: Customer views saved payment methods
**GIVEN** a customer selects the "پرداخت‌ها" (Payments) tab
**WHEN** the tab loads
**THEN** the system displays saved payment methods with:
- Card type icon (Visa, Mastercard, etc.)
- Masked card number (•••• •••• •••• ۱۲۳۴)
- Card holder name
- Expiry date (MM/YY)
- "Default" badge for primary payment method
- Actions: Set as Default, Remove

#### Scenario: Customer adds new payment method
**GIVEN** a customer is viewing payment methods
**WHEN** the customer clicks "افزودن کارت جدید" (Add New Card)
**THEN** the system displays secure payment form (Stripe Elements or similar)
**AND** validates card number, expiry, CVV as customer types
**AND** upon submission, tokenizes card data via payment provider API
**AND** stores payment method token in backend (never stores actual card data)
**AND** adds new card to list with success notification

#### Scenario: Customer removes payment method
**GIVEN** a customer has multiple payment methods saved
**WHEN** the customer clicks "حذف" (Remove) on a non-default payment method
**THEN** the system prompts confirmation: "این کارت حذف شود؟"
**AND** upon confirmation, sends DELETE /api/v1/customers/payment-methods/{id}
**AND** removes from list
**AND** if removing default, prompts to select new default

### Requirement: Loyalty Points Tracking
The system MUST display customer's loyalty points balance, transaction history, and tier status.

#### Scenario: Customer views loyalty points
**GIVEN** a customer views the loyalty card widget
**WHEN** the widget loads
**THEN** the system displays:
- Current points balance (Persian numbers: "۱۲۵ امتیاز")
- Tier status (Bronze, Silver, Gold, Platinum) with icon
- Progress bar to next tier ("۷۵ امتیاز تا سطح طلایی")
- Expiring points warning ("۲۰ امتیاز تا ۳۰ روز دیگر منقضی می‌شود")
- Points earning rate (e.g., "۱ امتیاز به ازای هر ۱۰٬۰۰۰ تومان")

#### Scenario: Customer views loyalty transaction history
**GIVEN** a customer clicks "مشاهده تراکنش‌ها" (View Transactions)
**WHEN** the transactions modal opens
**THEN** the system displays chronological list of transactions:
- Transaction type (Earned, Redeemed, Expired, Adjusted)
- Points amount (+۱۰, -۵۰, etc.)
- Balance after transaction
- Reason ("نوبت تکمیل شد", "استفاده در تخفیف", etc.)
- Related booking link (if applicable)
- Date (Jalali format)

### Requirement: Review Management
Customers MUST be able to view all their submitted reviews, edit recent reviews, and track review impact.

#### Scenario: Customer views submitted reviews
**GIVEN** a customer selects the "نظرات" (Reviews) tab
**WHEN** the tab loads
**THEN** the system displays list of customer's reviews with:
- Provider name and logo
- Service name
- Review date (Jalali format)
- Star rating (۱-۵ ⭐)
- Review text
- Provider response (if any)
- Helpful votes count ("۱۲ نفر مفید دانستند")
- Edit/Delete buttons (if within edit window)

#### Scenario: Customer edits recent review
**GIVEN** a customer's review is less than 7 days old
**WHEN** the customer clicks "ویرایش" (Edit)
**THEN** the system displays edit form with current rating and text
**AND** allows modification of rating and text
**AND** upon save, sends PATCH /api/v1/reviews/{id}
**AND** adds "(ویرایش شده)" (Edited) label to review
**AND** updates display with new content

#### Scenario: Customer deletes review
**GIVEN** a customer wants to remove their review
**WHEN** the customer clicks "حذف" (Delete)
**THEN** the system prompts confirmation: "این نظر حذف شود؟"
**AND** upon confirmation, sends DELETE /api/v1/reviews/{id}
**AND** removes review from list
**AND** notifies provider of deletion

### Requirement: Notification Preferences
Customers MUST be able to configure notification channels and frequency for booking reminders, offers, and updates.

#### Scenario: Customer manages notification settings
**GIVEN** a customer is in the "تنظیمات" (Settings) tab
**WHEN** the customer views notification preferences section
**THEN** the system displays toggle switches for:
- SMS notifications (on/off)
- Email notifications (on/off)
- Push notifications (on/off)
- Marketing communications (opt-in)
**AND** dropdowns for reminder timing:
- Booking reminders (1 hour, 24 hours, 3 days before)
- Special offers (Weekly, Monthly, Never)

#### Scenario: Customer updates notification preferences
**GIVEN** a customer changes notification settings
**WHEN** the customer toggles a switch or changes dropdown
**THEN** the system immediately saves preference via PATCH /api/v1/customers/preferences
**AND** displays brief confirmation toast (auto-dismiss in 2 seconds)
**AND** respects preferences in future notifications

### Requirement: Privacy and Security Settings
Customers MUST be able to manage account security, privacy controls, and view active sessions.

#### Scenario: Customer views privacy settings
**GIVEN** a customer is in the Settings tab
**WHEN** the customer views privacy section
**THEN** the system displays options:
- Profile visibility (Public, Private)
- Show booking history to providers (toggle)
- Allow personalized recommendations (toggle)
- Data sharing preferences (toggle for analytics)

#### Scenario: Customer enables two-factor authentication
**GIVEN** a customer wants to enhance account security
**WHEN** the customer toggles "فعال‌سازی احراز هویت دو مرحله‌ای" (Enable 2FA)
**THEN** the system initiates OTP verification flow
**AND** sends verification code to registered phone number
**AND** upon successful verification, enables 2FA
**AND** displays backup codes for emergency access

#### Scenario: Customer views active sessions
**GIVEN** a customer clicks "مدیریت دستگاه‌ها" (Manage Devices)
**WHEN** the sessions modal opens
**THEN** the system displays list of active sessions:
- Device type (Desktop, Mobile)
- Browser (Chrome, Safari, etc.)
- Location (approximate city)
- IP address (masked: 192.168.x.x)
- Last active timestamp
- "Current session" badge for active device
- Logout button for each session

#### Scenario: Customer logs out other sessions
**GIVEN** a customer views active sessions
**WHEN** the customer clicks "خروج" (Logout) on a non-current session
**THEN** the system invalidates that session's tokens
**AND** removes session from list
**AND** that device must re-authenticate to access account

### Requirement: Account Data Export and Deletion (GDPR)
The system MUST provide customers with ability to export their data or delete their account in compliance with GDPR.

#### Scenario: Customer requests data export
**GIVEN** a customer is in Settings tab
**WHEN** the customer clicks "دانلود اطلاعات من" (Download My Data)
**THEN** the system confirms: "فایل شامل تمام اطلاعات شما تهیه خواهد شد. این کار چند دقیقه طول می‌کشد."
**AND** upon confirmation, queues background job for data export
**AND** displays: "درخواست شما در حال پردازش است. لینک دانلود به ایمیل شما ارسال خواهد شد."
**AND** within 15 minutes, sends email with time-limited S3 download link
**AND** export includes: profile data, booking history, reviews, payment history (JSON format)

#### Scenario: Customer requests account deletion
**GIVEN** a customer wants to delete their account
**WHEN** the customer clicks "حذف حساب کاربری" (Delete Account)
**THEN** the system displays warning: "این عملیات غیرقابل بازگشت است. تمام اطلاعات، نوبت‌ها و امتیازها حذف خواهند شد."
**AND** requires password/OTP re-authentication
**AND** upon confirmation, queues account deletion job
**AND** cancels all future bookings with notifications to providers
**AND** anonymizes historical data (replaces name with "کاربر حذف شده")
**AND** permanently deletes personal data within 30 days
**AND** logs out customer immediately

### Requirement: Mobile Responsiveness
All profile page features MUST be fully functional and optimized for mobile devices with touch-friendly interactions.

#### Scenario: Customer uses profile on mobile
**GIVEN** a customer accesses profile on screen width < 768px
**WHEN** the page loads
**THEN** the system displays:
- Single-column layout (no sidebar)
- Bottom navigation bar instead of horizontal tabs
- Swipeable tabs with smooth transitions
- Touch-optimized buttons (minimum 44px height)
- Pull-to-refresh functionality
- Bottom sheets for modals (not centered popups)
**AND** maintains <2 second load time on 3G connection

### Requirement: Accessibility Compliance
The customer profile page MUST comply with WCAG 2.1 AA standards for accessibility.

#### Scenario: Screen reader navigation
**GIVEN** a customer uses a screen reader
**WHEN** the customer navigates the profile page
**THEN** the system provides:
- Semantic HTML structure (header, nav, main, section)
- ARIA labels for all interactive elements
- Descriptive alt text for images
- Live region announcements for dynamic updates
- Proper heading hierarchy (h1 → h2 → h3)
- Skip navigation link

#### Scenario: Keyboard-only navigation
**GIVEN** a customer navigates without a mouse
**WHEN** the customer uses Tab key to navigate
**THEN** the system:
- Follows logical RTL tab order (right to left, top to bottom)
- Displays visible focus indicators (2px solid outline)
- Allows Enter/Space to activate buttons
- Enables Escape to close modals
- Provides keyboard shortcuts for common actions

### Requirement: Performance Optimization
The profile page MUST load within 2 seconds and maintain smooth 60fps interactions.

#### Scenario: Initial page load
**GIVEN** a customer navigates to /customer/profile
**WHEN** the page begins loading
**THEN** the system:
- Loads critical CSS inline (<10KB)
- Defers non-critical JavaScript
- Shows skeleton screens for loading content
- Loads profile data in parallel with upcoming bookings
- Lazy loads tab content (only load active tab)
- Uses CDN for images and static assets
- Achieves <2 second Time to Interactive (TTI)

#### Scenario: Tab switching
**GIVEN** a customer switches between tabs
**WHEN** the customer clicks a new tab
**THEN** the system:
- Uses KeepAlive to cache previously viewed tabs (max 3)
- Lazy loads tab content on first visit
- Shows loading skeleton during data fetch
- Maintains scroll position when returning to cached tab
- Completes transition in <200ms

### Requirement: Error Handling and Edge Cases
The system MUST gracefully handle errors, network failures, and edge cases with user-friendly messaging.

#### Scenario: API request fails
**GIVEN** a customer performs an action requiring API call
**WHEN** the API returns error or times out
**THEN** the system displays error toast with:
- User-friendly Persian message (not technical error)
- Retry button for retryable errors
- Support contact link for persistent errors
**AND** logs error to monitoring system
**AND** does not break page functionality

#### Scenario: Customer has no data
**GIVEN** a new customer with no bookings, favorites, or reviews
**WHEN** the customer views various tabs
**THEN** the system displays appropriate empty states with:
- Relevant illustration or icon
- Encouraging message
- Clear call-to-action to populate data
**AND** does not show errors or broken UI

#### Scenario: Offline access
**GIVEN** a customer loses internet connection
**WHEN** the customer interacts with profile page
**THEN** the system:
- Displays cached profile data (if available)
- Shows offline indicator banner at top
- Queues write operations for retry when online
- Prevents actions requiring network (e.g., avatar upload)
- Provides helpful message: "اتصال اینترنت قطع است. برخی ویژگی‌ها در دسترس نیستند."
