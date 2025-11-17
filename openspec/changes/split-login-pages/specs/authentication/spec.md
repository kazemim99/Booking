# Authentication Specification Changes

## MODIFIED Requirements

### Requirement: Phone-based login interface
The system SHALL provide separate login interfaces for Customers and Providers, each with audience-specific messaging and explicit user type handling.

**Customer Login** (`/login`):
- Displays modern RTL Persian interface focused on booking services
- Shows customer-specific messaging: "رزرو نوبت" or "ورود برای رزرو"
- Sets userType = 'Customer' explicitly
- No redirect path detection required

**Provider Login** (`/provider/login`):
- Displays modern RTL Persian interface focused on business management
- Shows provider-specific messaging: "ورود به پنل ارائه‌دهندگان"
- Sets userType = 'Provider' explicitly
- No redirect path detection required

Both interfaces MUST:
- Validate Iranian phone numbers (pattern: `/^09\d{9}$/`)
- Provide clear error messages in Persian
- Pass userType explicitly to verification flow
- Display: RTL layout, centered card design, application logo, phone input with LTR direction, primary action button "دریافت کد"

#### Scenario: Customer opens login page
**Given** the customer navigates to `/login`
**When** the page loads
**Then** the page displays:
- RTL layout with Persian text
- Customer-focused heading: "رزرو نوبت" or "به بوکسی خوش آمدید"
- Description: "برای رزرو نوبت، شماره موبایل خود را وارد کنید"
- Phone number input with placeholder "09123456789"
- Primary button: "دریافت کد"
**And** userType is set to 'Customer' internally

#### Scenario: Provider opens login page
**Given** the provider navigates to `/provider/login`
**When** the page loads
**Then** the page displays:
- RTL layout with Persian text
- Provider-focused heading: "ورود به پنل کسب و کار"
- Description: "برای ورود به پنل ارائه‌دهندگان، شماره موبایل خود را وارد کنید"
- Phone number input with placeholder "09123456789"
- Primary button: "دریافت کد"
**And** userType is set to 'Provider' internally

#### Scenario: User enters invalid phone number
**Given** the user is on any login page
**When** the user enters a phone number that doesn't match pattern `/^09\d{9}$/`
**And** clicks "دریافت کد"
**Then** an error message displays: "شماره موبایل وارد شده معتبر نیست"
**And** the form does not submit

#### Scenario: User submits valid phone number (Customer)
**Given** the customer is on `/login`
**When** the customer enters a valid Iranian phone number (09XXXXXXXXX)
**And** clicks "دریافت کد"
**Then** the system sends a verification code to the phone number
**And** navigates to the verification page with userType = 'Customer'

#### Scenario: User submits valid phone number (Provider)
**Given** the provider is on `/provider/login`
**When** the provider enters a valid Iranian phone number (09XXXXXXXXX)
**And** clicks "دریافت کد"
**Then** the system sends a verification code to the phone number
**And** navigates to the verification page with userType = 'Provider'

## REMOVED Requirements

### Requirement: User type detection via redirect path
**Reason**: Replaced with explicit user type per login page. This removes fragile detection logic and improves maintainability.

**Migration**:
- Users should use `/login` for customer authentication
- Users should use `/provider/login` for provider authentication
- Direct navigation to `/login` defaults to customer flow
- sessionStorage dependency for `registration_user_type` is removed

## ADDED Requirements

### Requirement: Explicit user type routing
The system SHALL provide clear navigation paths for each user type from the landing page and all entry points.

#### Scenario: Customer discovers service and wants to book
**Given** a customer is on the homepage
**When** they click the "رزرو کنید" button in HeroSection
**Then** they are navigated to `/login` (customer login)
**And** the login page displays customer-focused messaging

#### Scenario: Provider wants to access business panel
**Given** a visitor is on the homepage footer or header
**When** they click the "برای کسب‌وکارها" or "ورود ارائه‌دهندگان" link
**Then** they are navigated to `/provider/login`
**And** the login page displays provider-focused messaging

#### Scenario: Navigation links correctly route to appropriate login
**Given** the user is on any page
**When** they use navigation links
**Then** customer-focused CTAs navigate to `/login`
**And** provider-focused CTAs navigate to `/provider/login`
**And** each login page explicitly sets the correct userType
