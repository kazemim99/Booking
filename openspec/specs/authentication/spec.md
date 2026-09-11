# authentication Specification

## Purpose
TBD - created by archiving change replace-registration-with-figma-design. Update Purpose after archive.
## Requirements
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

### Requirement: Phone number is the globally unique person identity

The system SHALL treat a normalized (canonical E.164) phone number as the globally unique identifier of a person, enforced at BOTH the database level (unique index) and the application level (uniqueness guard before any account creation). No two active person accounts MAY share the same phone number.

#### Scenario: Duplicate phone rejected at creation
- **WHEN** any registration path attempts to create a person with a phone number that already belongs to an active account
- **THEN** the creation is rejected and the existing account is used instead

#### Scenario: Phone normalized before lookup
- **WHEN** a phone number is provided in any accepted form (local `09…`, `+98…`, `0098…`)
- **THEN** it is normalized to canonical E.164 before uniqueness is checked

#### Scenario: Database enforces uniqueness
- **WHEN** two concurrent requests attempt to create accounts for the same phone number
- **THEN** at most one account is created
- **AND** the database unique index prevents the duplicate

#### Scenario: Every creation path is guarded
- **WHEN** an account would be created via OTP login, email/password registration, invitation acceptance, or seeding
- **THEN** the same phone canonicalization and uniqueness guard applies

### Requirement: OTP authentication is gated by account status

Passwordless (OTP) authentication SHALL verify the account status before issuing tokens and MUST refuse to authenticate `Banned`, `Suspended`, `Inactive`, or `Deleted` accounts.

#### Scenario: Blocked account cannot obtain tokens via OTP
- **WHEN** a person whose account is Banned or Suspended completes the OTP challenge
- **THEN** no tokens are issued and a clear status error is returned

#### Scenario: Active account authenticates
- **WHEN** an Active person completes the OTP challenge
- **THEN** tokens are issued as normal

