# Authentication UI - Spec Delta

## ADDED Requirements

### Requirement: Phone-based login interface
The phone login view MUST display a modern RTL Persian interface with improved UX matching the Figma design. The system SHALL validate Iranian phone numbers and provide clear error messages in Persian.

#### Scenario: User opens login page
**Given** the user navigates to the login page
**When** the page loads
**Then** the page displays:
- RTL layout with Persian text
- Centered card design with gradient background
- Application icon/logo (book icon) in colored circle
- Welcome heading: "خوش آمدید"
- Description: "برای ورود به پنل ارائه‌دهندگان، شماره موبایل خود را وارد کنید"
- Phone number input field with label "شماره موبایل" and placeholder "09123456789"
- LTR input direction for phone number (dir="ltr")
- Primary action button: "دریافت کد"
- Terms acceptance text: "با ورود به سیستم، شما قوانین و مقررات را می‌پذیرید"

#### Scenario: User enters invalid phone number
**Given** the user is on the login page
**When** the user enters a phone number that doesn't match pattern `/^09\d{9}$/`
**And** clicks "دریافت کد"
**Then** an error message displays: "شماره موبایل وارد شده معتبر نیست"
**And** the form does not submit

#### Scenario: User submits valid phone number
**Given** the user is on the login page
**When** the user enters a valid Iranian phone number (09XXXXXXXXX)
**And** clicks "دریافت کد"
**Then** the system sends a verification code to the phone number
**And** navigates to the verification page

### Requirement: Verification code input interface
The verification view MUST display a modern RTL Persian OTP input interface. The system SHALL provide back navigation to login and clear verification status feedback.

#### Scenario: User arrives at verification page
**Given** the user has submitted their phone number
**When** the verification page loads
**Then** the page displays:
- RTL layout with Persian text
- Centered card design
- Phone number display showing the number submitted
- OTP input fields (6 digits)
- Primary action button: "تایید کد"
- Back button to return to login
- Resend code option (if supported by backend)

#### Scenario: User enters verification code
**Given** the user is on the verification page
**When** the user enters the 6-digit verification code
**And** clicks "تایید کد"
**Then** the system validates the code with the backend
**And** on success, marks the user as authenticated
**And** navigates to the appropriate next step (registration or dashboard)

#### Scenario: User goes back to login
**Given** the user is on the verification page
**When** the user clicks the back button
**Then** the user returns to the login page
**And** can re-enter their phone number
