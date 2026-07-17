## ADDED Requirements

### Requirement: Client Book Derived From Bookings

The system SHALL provide the provider's client book at `GET /Providers/{providerId}/clients`, derived from the provider's bookings: one row per distinct customer with display name and phone (resolved read-only from the UserManagement schema), total bookings, completed count, upcoming count (future, not cancelled), and last visit (most recent past booking start). Rows SHALL be ordered by most-recent activity. The endpoint MUST be authorized like other provider-scoped reads (owner or admin), and the provider's own user account MUST be excluded (it hosts walk-in bookings by convention).

#### Scenario: Clients aggregate across bookings
- **WHEN** a customer has booked the provider three times (two past completed, one upcoming)
- **THEN** the client list shows one row for them with total 3, completed 2, upcoming 1, and their profile name and phone

#### Scenario: Unauthorized access is refused
- **WHEN** a caller who is neither the provider's owner nor an admin requests the list
- **THEN** the request is rejected with 403

#### Scenario: Walk-in host account is excluded
- **WHEN** the provider has walk-in bookings (booked under their own account)
- **THEN** no client row is produced for the provider's own user

### Requirement: Clients Tab With Persian Search

The provider app SHALL present the client book on the Clients tab (مشتریان) as a searchable list — search matching MUST use Persian text normalization (kaf/yeh variants, ZWNJ, whitespace) against name and phone. Each row shows the client's name (or a neutral fallback when unresolved), phone, booking counts, and last visit. The tab uses the shared bottom navigation and the standard loading/empty/error treatments.

#### Scenario: Search matches normalized Persian
- **WHEN** the provider types «كاوه» (Arabic kaf) and a client is stored as «کاوه» (Persian kaf)
- **THEN** the client matches

#### Scenario: Empty book is inviting
- **WHEN** the provider has no clients yet
- **THEN** an empty state explains the list fills as bookings come in

### Requirement: Client Actions

Tapping a client SHALL open an action sheet with the client's details and: call (phone affordance) and **book again** — opening the booking composer with the client's name and phone pre-filled so the walk-in convention carries their identity.

#### Scenario: Book again pre-fills the composer
- **WHEN** the provider taps «ثبت نوبت» on client «رضا کریمی»
- **THEN** the composer opens with the client name and phone fields pre-filled
