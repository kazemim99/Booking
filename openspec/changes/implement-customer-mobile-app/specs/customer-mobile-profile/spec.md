# customer-mobile-profile Specification

## Purpose

Enable customers to manage their personal profile, favorites, reviews, and app preferences through a mobile interface. The profile section should feel personal yet professional, provide easy access to account settings, and allow customers to control their experience.

## ADDED Requirements

### Requirement: Profile Page Layout

The profile page SHALL display user information with organized sections for account, preferences, and app settings.

#### Scenario: Customer opens profile page

**GIVEN** a customer is authenticated
**WHEN** the customer taps "پروفایل" in bottom navigation
**THEN** the system displays profile page with:
  - **Header Section**:
    - User initial in colored circle (60dp, based on user ID hash)
    - Full name (e.g., "علی رضایی")
    - Phone number (e.g., "۰۹۱۲۳۴۵۶۷۸۹")
    - "ویرایش پروفایل" button (text button, dark blue)
  - **Account Section**:
    - "اطلاعات شخصی" row with chevron (>)
    - "علاقه‌مندی‌ها (۵)" row with heart icon
    - "نظرات من" row with star icon
  - **Settings Section**:
    - "اعلان‌ها" row with toggle
    - "زبان" row (currently: "فارسی")
  - **Support Section**:
    - "تماس با پشتیبانی" row
    - "سوالات متداول" row
    - "درباره برنامه" row
  - **Logout Section**:
    - "خروج از حساب" row (red text)
  - **App Version**: Footer showing "نسخه ۱.۰.۰"

#### Scenario: User initial color assignment

**GIVEN** a customer named "سارا احمدی" with ID "abc123"
**WHEN** profile page loads
**THEN** the system:
  - Extracts first character: "س"
  - Hashes user ID to select color from palette:
    - Option 1: #1A365D (dark blue)
    - Option 2: #059669 (green)
    - Option 3: #0284C7 (blue)
    - Option 4: #6366F1 (indigo)
  - Displays colored circle with "س" in white text
  - Color remains consistent for that user

### Requirement: Edit Profile

Customers SHALL be able to update their name and email (phone number read-only).

#### Scenario: Customer opens profile editor

**GIVEN** the customer is viewing profile page
**WHEN** the customer taps "ویرایش پروفایل"
**THEN** the system navigates to edit profile page with:
  - Page title: "ویرایش پروفایل"
  - Form fields:
    - "نام" input (pre-filled with current first name)
    - "نام خانوادگی" input (pre-filled with current last name)
    - "شماره موبایل" input (read-only, grayed out)
    - "ایمیل" input (optional, pre-filled if exists)
  - Helper text under phone: "برای تغییر شماره با پشتیبانی تماس بگیرید"
  - "ذخیره" button (primary, dark blue)
**AND** fetches current profile: `GET /api/v1/Customers/profile`

#### Scenario: Customer updates profile

**GIVEN** the edit profile page is displayed
**WHEN** the customer changes:
  - نام: "سارا"
  - نام خانوادگی: "کریمی"
  - ایمیل: "sara.karimi@example.com"
**AND** taps "ذخیره"
**THEN** the system:
  - Validates name (3-50 chars, Persian/English letters only)
  - Validates email format (regex: RFC 5322)
  - Sends: `PUT /api/v1/Customers/profile`
    ```json
    {
      "firstName": "سارا",
      "lastName": "کریمی",
      "email": "sara.karimi@example.com"
    }
    ```
  - On success:
    - Shows toast: "اطلاعات با موفقیت ذخیره شد"
    - Updates header with new name
    - Returns to profile page

#### Scenario: Validation fails

**GIVEN** the customer is editing profile
**WHEN** the customer enters:
  - نام: "س" (too short)
  - ایمیل: "invalid-email"
**AND** taps "ذخیره"
**THEN** the system:
  - Shows inline error under "نام": "نام باید حداقل ۳ حرف باشد"
  - Shows inline error under "ایمیل": "فرمت ایمیل معتبر نیست"
  - Does not submit to server
  - Keeps form open for correction

#### Scenario: API returns error

**GIVEN** the customer submits valid changes
**WHEN** API returns 409 Conflict (email already in use)
**THEN** the system:
  - Shows error toast: "این ایمیل قبلاً ثبت شده است"
  - Keeps form open
  - Allows customer to modify email

### Requirement: Manage Favorites

Customers SHALL be able to view and remove favorite providers.

#### Scenario: Customer opens favorites list

**GIVEN** the customer has 5 favorite providers
**WHEN** the customer taps "علاقه‌مندی‌ها (۵)" row
**THEN** the system:
  - Navigates to favorites page
  - Fetches: `GET /api/v1/Customers/favorites`
  - Displays grid of 5 provider cards (2 columns on phone, 3 on tablet):
    - Provider thumbnail (square, 120dp)
    - Provider name
    - Category tag (e.g., "آرایشگاه")
    - Rating (e.g., "⭐ ۴.۸")
    - Filled heart icon (top-right corner)
    - Tap card → opens provider detail page

#### Scenario: Customer removes favorite

**GIVEN** favorites list is displayed
**WHEN** the customer taps filled heart icon on a provider
**THEN** the system shows confirmation dialog:
  - "این ارائه‌دهنده از لیست علاقه‌مندی‌ها حذف شود؟"
  - "حذف" (red) | "انصراف" buttons
**AND** upon "حذف":
  - Sends: `DELETE /api/v1/Customers/favorites/{providerId}`
  - Removes card from grid with fade-out animation
  - Shows toast: "از علاقه‌مندی‌ها حذف شد"
  - Updates counter: "علاقه‌مندی‌ها (۴)"

#### Scenario: No favorites exist

**GIVEN** the customer has no favorites
**WHEN** favorites page loads
**THEN** the system displays empty state:
  - Heart icon illustration (gray)
  - Message: "شما هنوز ارائه‌دهنده‌ای را به علاقه‌مندی‌ها اضافه نکرده‌اید"
  - "جستجوی ارائه‌دهندگان" button
**AND** tapping button navigates to search page

### Requirement: Manage Reviews

Customers SHALL be able to view, edit, and delete their submitted reviews.

#### Scenario: Customer opens reviews list

**GIVEN** the customer has submitted 3 reviews
**WHEN** the customer taps "نظرات من" row
**THEN** the system:
  - Navigates to reviews page
  - Fetches: `GET /api/v1/Reviews/my-reviews`
  - Displays 3 review cards showing:
    - Provider logo and name
    - Service name
    - Star rating (1-5, filled stars in gold #EAB308)
    - Review text
    - Submission date (e.g., "۳ روز پیش")
    - "ویرایش" button (if review <7 days old)
    - "حذف" button (text, red)

#### Scenario: Customer edits recent review

**GIVEN** the customer submitted a review 2 days ago
**WHEN** the customer taps "ویرایش" button
**THEN** the system displays edit review modal with:
  - Provider name (read-only)
  - Star rating selector (pre-filled with current rating)
  - Text area (pre-filled with current review text)
  - Character counter (۱۵۰/۵۰۰)
  - "ذخیره تغییرات" | "انصراف" buttons

#### Scenario: Customer saves edited review

**GIVEN** the edit review modal is displayed
**WHEN** the customer:
  - Changes rating from 4 to 5 stars
  - Updates text: "خدمات عالی شد!"
  - Taps "ذخیره تغییرات"
**THEN** the system:
  - Validates rating (required) and text (max 500 chars)
  - Sends: `PUT /api/v1/Reviews/{reviewId}`
    ```json
    {
      "rating": 5,
      "comment": "خدمات عالی شد!",
      "customerId": "customer-uuid"
    }
    ```
  - On success:
    - Closes modal
    - Updates review card with new rating/text
    - Shows "(ویرایش شده)" label
    - Shows toast: "نظر شما ویرایش شد"

#### Scenario: Customer tries to edit old review

**GIVEN** the customer submitted a review 10 days ago
**WHEN** the customer views that review
**THEN** the "ویرایش" button is hidden
**AND** message shows: "فقط نظرات کمتر از ۷ روز قابل ویرایش هستند"

#### Scenario: Customer deletes review

**GIVEN** reviews list is displayed
**WHEN** the customer taps "حذف" button on a review
**THEN** the system shows confirmation dialog:
  - "نظر شما حذف شود؟"
  - "این عمل قابل بازگشت نیست"
  - "حذف" (red) | "انصراف" buttons
**AND** upon "حذف":
  - Sends: `DELETE /api/v1/Reviews/{reviewId}`
  - Removes review card with fade-out animation
  - Shows toast: "نظر شما حذف شد"

#### Scenario: No reviews submitted

**GIVEN** the customer has not submitted any reviews
**WHEN** reviews page loads
**THEN** the system displays empty state:
  - Star icon illustration
  - Message: "شما هنوز نظری ثبت نکرده‌اید"
  - Subtitle: "نظرات خود را درباره خدمات دریافتی ثبت کنید"
  - "رزروهای من" button (navigates to bookings)

### Requirement: Notification Preferences

Customers SHALL be able to control notification channels and settings.

#### Scenario: Customer opens notification settings

**GIVEN** the customer is viewing profile page
**WHEN** the customer taps "اعلان‌ها" row
**THEN** the system navigates to notifications page with:
  - Page title: "تنظیمات اعلان‌ها"
  - **Channels Section**:
    - "اعلان‌های push" toggle (on/off)
    - "پیامک" toggle (on/off)
    - Helper text: "یادآوری نوبت‌ها و اطلاعیه‌های مهم"
  - **Reminder Timing**:
    - Radio buttons:
      - "۱ ساعت قبل از نوبت"
      - "۳ ساعت قبل از نوبت"
      - "۱ روز قبل از نوبت" (selected by default)
      - "بدون یادآوری"
  - **Marketing Section** (optional):
    - "دریافت پیشنهادات ویژه" toggle
**AND** fetches current preferences: `GET /api/v1/Customers/notification-preferences`

#### Scenario: Customer toggles notification channel

**GIVEN** notification settings are displayed
**WHEN** the customer toggles "پیامک" from ON to OFF
**THEN** the system:
  - Immediately sends: `PUT /api/v1/Customers/notification-preferences`
    ```json
    {
      "pushEnabled": true,
      "smsEnabled": false,
      "reminderHoursBefore": 24,
      "marketingEnabled": false
    }
    ```
  - Shows brief success toast: "تنظیمات ذخیره شد"
  - Updates toggle state

#### Scenario: Customer disables all notifications

**GIVEN** notification settings are displayed
**WHEN** the customer toggles both "اعلان‌های push" and "پیامک" OFF
**THEN** the system displays warning dialog:
  - "⚠️ هشدار"
  - "با غیرفعال کردن تمام اعلان‌ها، یادآوری نوبت دریافت نخواهید کرد"
  - "ادامه می‌دهید؟"
  - "بله، غیرفعال کن" | "انصراف" buttons
**AND** upon confirmation:
  - Saves preferences
  - Shows warning icon next to "اعلان‌ها" in profile page

#### Scenario: Customer changes reminder timing

**GIVEN** notification settings are displayed
**WHEN** the customer selects "۳ ساعت قبل از نوبت"
**THEN** the system:
  - Updates selection (radio button)
  - Immediately saves: `PUT /api/v1/Customers/notification-preferences`
  - Shows toast: "یادآوری ۳ ساعت قبل فعال شد"
  - Future bookings will use new timing

### Requirement: App Settings

The profile SHALL provide access to app-level preferences.

#### Scenario: Customer views language setting

**GIVEN** the customer is viewing profile page
**WHEN** the customer taps "زبان" row
**THEN** the system displays language picker with:
  - "فارسی" (selected, with checkmark)
  - "English" (grayed out - not yet implemented)
  - Message: "زبان‌های دیگر به زودی اضافه می‌شوند"

#### Scenario: Dark mode preference (optional)

**GIVEN** the customer is viewing profile settings
**WHEN** the customer sees "حالت شب" toggle
**THEN** toggling ON:
  - Switches app theme to dark mode
  - Background: #0F1419, Text: #F7FAFC
  - Saves preference to local storage
  - Applies immediately across all screens

### Requirement: Support and Help

Customers SHALL have easy access to support channels.

#### Scenario: Customer opens support

**GIVEN** the customer is viewing profile page
**WHEN** the customer taps "تماس با پشتیبانی"
**THEN** the system displays support options modal:
  - "تماس تلفنی" → Opens phone dialer with support number
  - "ارسال ایمیل" → Opens email app with support@booksy.ir
  - "چت آنلاین" (if available) → Opens in-app chat
  - "بستن" button

#### Scenario: Customer views FAQ

**GIVEN** the customer is viewing profile page
**WHEN** the customer taps "سوالات متداول"
**THEN** the system navigates to FAQ page with:
  - Searchable list of common questions
  - Expandable accordion items:
    - "چگونه رزرو کنم؟"
    - "چگونه رزرو خود را لغو کنم؟"
    - "هزینه لغو رزرو چقدر است؟"
    - "چگونه نظر ثبت کنم؟"
  - Tapping question expands answer

#### Scenario: Customer views app info

**GIVEN** the customer is viewing profile page
**WHEN** the customer taps "درباره برنامه"
**THEN** the system displays about page with:
  - App logo
  - App version (e.g., "۱.۰.۰")
  - Build number
  - "شرایط و قوانین" link
  - "حریم خصوصی" link
  - Copyright notice

### Requirement: Logout

Customers SHALL be able to securely log out and clear session.

#### Scenario: Customer initiates logout

**GIVEN** the customer is viewing profile page
**WHEN** the customer taps "خروج از حساب" (red text row)
**THEN** the system displays confirmation dialog:
  - Title: "خروج از حساب"
  - Message: "آیا می‌خواهید از حساب کاربری خود خارج شوید؟"
  - "خروج" (red) | "انصراف" buttons

#### Scenario: Customer confirms logout

**GIVEN** the logout confirmation is displayed
**WHEN** the customer taps "خروج"
**THEN** the system:
  - Shows loading: "در حال خروج..."
  - Sends: `POST /api/v1/Auth/logout` (best effort, ignores errors)
  - Clears local storage:
    - Deletes access_token, refresh_token
    - Deletes user_id, customer_id
    - Clears cached bookings, favorites, reviews
  - Navigates to login page (`/login`)
  - Removes all navigation history (no back button)
  - Shows toast: "با موفقیت خارج شدید"

#### Scenario: Logout during network failure

**GIVEN** the customer confirms logout
**WHEN** network is offline or API fails
**THEN** the system:
  - Clears local storage anyway (force logout)
  - Navigates to login page
  - Does not show error (graceful degradation)

### Requirement: Performance

Profile pages SHALL load quickly with minimal data fetching.

#### Scenario: Profile page loads

**GIVEN** the customer opens profile page
**WHEN** page is rendered
**THEN** the system:
  - Displays user info from local cache immediately (<100ms)
  - Does not make API call (data from auth session)
  - Loads favorites count in background (optional)

#### Scenario: Favorites/Reviews load efficiently

**GIVEN** the customer opens favorites or reviews
**WHEN** lists are fetched
**THEN** the system:
  - Shows skeleton loader immediately
  - Fetches data from API
  - Renders within 1 second
  - Caches for 5 minutes

### Requirement: Error Handling

Profile SHALL handle errors gracefully.

#### Scenario: Profile update fails

**GIVEN** the customer saves profile changes
**WHEN** API returns 500 error
**THEN** the system:
  - Shows error toast: "خطا در ذخیره اطلاعات. لطفاً دوباره تلاش کنید"
  - Keeps form open with entered data
  - Provides "تلاش مجدد" button

#### Scenario: Favorites fetch fails

**GIVEN** the customer opens favorites
**WHEN** API returns error
**THEN** the system displays error state:
  - Error icon
  - Message: "خطا در بارگذاری علاقه‌مندی‌ها"
  - "تلاش مجدد" button

### Requirement: Accessibility

Profile SHALL be accessible to all users.

#### Scenario: Screen reader announces profile

**GIVEN** a customer using TalkBack
**WHEN** navigating profile page
**THEN** each row is announced:
  - "اطلاعات شخصی، دکمه، رفتن به ویرایش پروفایل"
  - "علاقه‌مندی‌ها ۵ مورد، دکمه"
  - "اعلان‌ها، کلید روشن"
  - "خروج از حساب، دکمه"

#### Scenario: Keyboard navigation

**GIVEN** a customer using keyboard
**WHEN** navigating profile settings
**THEN** focus moves through rows in RTL order
**AND** focused rows show 2px dark blue outline
**AND** Enter activates row/button
**AND** Space toggles switches

### Requirement: Analytics

Profile SHALL track user preferences and behavior.

#### Scenario: Profile viewed

**GIVEN** customer opens profile page
**WHEN** page loads
**THEN** system logs:
  - `profile_page_viewed`
  - `user_id`: customer ID

#### Scenario: Notification preferences changed

**GIVEN** customer toggles notifications
**WHEN** settings saved
**THEN** system logs:
  - `notification_preferences_changed`: { pushEnabled, smsEnabled, reminderHours }

#### Scenario: Logout

**GIVEN** customer logs out
**WHEN** logout completes
**THEN** system logs:
  - `user_logged_out`: { sessionDuration }
