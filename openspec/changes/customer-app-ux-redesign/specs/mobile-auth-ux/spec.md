# mobile-auth-ux

## ADDED Requirements

### Requirement: Splash resolves session before routing
The splash screen SHALL restore any persisted session and route the user exactly once: authenticated users land on the home tab with their session active; unauthenticated users land on the home tab as guests. Splash SHALL NOT show a spinner longer than necessary and MUST NOT flash the login screen during cold start.

#### Scenario: Returning authenticated user
- **WHEN** the app cold-starts with a valid stored token
- **THEN** the user lands on home authenticated, without seeing the login screen

#### Scenario: Guest cold start
- **WHEN** the app cold-starts with no stored session
- **THEN** the user lands on home as a guest and can browse without logging in

### Requirement: Phone login with inline validation
The login screen SHALL accept an Iranian mobile number with inline, on-blur validation (format hint, digit normalization for Persian/Arabic numerals), a single obvious primary CTA, and keyboard type appropriate for phone entry. Submitting SHALL show the CTA loading state and disable resubmission until the request resolves.

#### Scenario: Persian digits normalized
- **WHEN** the user types their number using Persian numerals
- **THEN** the input is accepted and normalized before validation and submission

#### Scenario: Invalid number
- **WHEN** the user submits a number that is not a valid mobile format
- **THEN** an inline error appears under the field explaining the expected format, and no request is sent

#### Scenario: Network failure on submit
- **WHEN** the OTP-send request fails
- **THEN** the user sees an actionable error (retry) and the entered number is preserved

### Requirement: OTP verification with autofill and resend
The OTP screen SHALL show the destination number with an edit affordance, a segmented OTP input supporting paste and platform SMS autofill/auto-read, a visible resend countdown timer that enables a resend action at zero, and auto-submit when all digits are entered. Failed verification SHALL clear the code, keep focus in the input, and show an inline error.

#### Scenario: SMS auto-read
- **WHEN** the OTP SMS arrives on a supported device
- **THEN** the code fills automatically and verification submits without manual typing

#### Scenario: Wrong code entered
- **WHEN** verification fails due to an incorrect code
- **THEN** an inline error appears, the input clears, focus remains in the OTP field, and the resend timer is unaffected

#### Scenario: Resend flow
- **WHEN** the countdown reaches zero and the user taps resend
- **THEN** a new code is requested, the timer restarts, and a confirmation snackbar appears

#### Scenario: Edit number
- **WHEN** the user taps the displayed phone number
- **THEN** they return to the login screen with the number pre-filled for correction

### Requirement: Authentication demanded only at the point of need
Browsing capabilities (home, explore, provider detail) SHALL be available to guests. The app SHALL require authentication only when confirming a booking or opening the appointments or profile tabs, and after successful login SHALL return the user to the exact action they were attempting.

#### Scenario: Guest attempts to book
- **WHEN** a guest reaches booking confirmation
- **THEN** they are taken through phone/OTP login and returned to the confirmation step with their selections intact

#### Scenario: Guest opens profile tab
- **WHEN** a guest taps the profile tab
- **THEN** the login screen is shown in place, and completing login reveals the profile in the same tab
