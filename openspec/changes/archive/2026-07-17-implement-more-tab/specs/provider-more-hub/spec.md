## ADDED Requirements

### Requirement: More Hub Structure

The More tab SHALL present a grouped hub: a business section linking to Services, Staff, and Insights (plus a share-booking-link action and visibly-disabled coming-soon rows for profile/hours/gallery editing), and an account section showing the signed-in provider's name, phone, and status with a logout action. The hub itself MUST require no network load (session data only).

#### Scenario: Hub renders and navigates
- **WHEN** the provider opens بیشتر
- **THEN** the business and account sections render with the provider's identity
- **AND** tapping Services/Staff/Insights opens the corresponding screen

#### Scenario: Logout signs out
- **WHEN** the provider taps خروج
- **THEN** the session is logged out and the app returns to the login flow

### Requirement: Read-Only Catalog Surfaces

Services SHALL list the provider's services with name, duration, and price; Staff SHALL list team members with name, role, and active state. Both use the standard loading/empty/error treatments with retry, and pull-to-refresh. Editing affordances are not offered until their flows ship.

#### Scenario: Services list shows the catalog
- **WHEN** the provider opens خدمات
- **THEN** each service shows its name, duration, and price

#### Scenario: Staff list shows the team
- **WHEN** the provider opens تیم
- **THEN** each member shows name and role, and inactive members are visually distinct

#### Scenario: Failures are retryable
- **WHEN** a list fails to load
- **THEN** the standard error state with retry is shown

### Requirement: Insights Summary

Insights SHALL show the provider's booking statistics: all-time totals (bookings, completed, cancelled, no-show) with revenue and currency, and the trailing-30-days booking count. Values come from the statistics endpoint; a load failure is retryable. Insights remains two navigation levels from the Home (IA: analytics is never the front door).

#### Scenario: Insights shows the numbers
- **WHEN** the provider opens گزارش‌ها
- **THEN** all-time bookings/completed/cancelled/no-show, revenue with currency, and the last-30-days count are displayed
