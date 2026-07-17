## Why

Clients (مشتریان) is the third IA bottom-nav destination — "the book of business; core asset, top-level in every competitor" — and currently a stub. The blocker has always been the cross-context gap: booking rows carry only `customerId` (customer identity lives in UserManagement), so a client list would be anonymous. Feasibility work confirmed the monolith's single database makes a **read-only cross-schema lookup** viable: `ServiceCatalog.Bookings` aggregated per customer, joined to `user_management.user_profiles`/`users` for names and phones (join verified against live data).

## What Changes

- **Backend — `GET /api/v1/Providers/{providerId}/clients`** (ServiceCatalog, ProviderOrAdmin + `CanManageProvider`): the provider's client book derived from bookings — per customer: name, phone, total/completed/upcoming counts, last visit — sorted by recency. Implemented as an Application query backed by an Infrastructure read service doing a read-only SQL join across the two schemas (an explicit, documented integration seam; no domain coupling). The provider's own user is excluded (walk-in bookings are booked under it by the MVP convention).
- **Flutter — Clients tab** (`/clients`): searchable client list (Persian-normalized matching via the existing `PersianText`), client rows (initial avatar, name, phone, counts, last visit), and a client action sheet: call (copy), and **"ثبت نوبت" → the composer with the client's name/phone pre-filled** (`?client=&phone=`).
- **Composer prefill**: optional client name/phone query params seed the walk-in fields.
- `ProviderNavBar` gains the live Clients destination.

## Capabilities

### New Capabilities
- `provider-clients`: the provider's client book — derivation rules, search, client actions, and the cross-schema name-resolution seam.

### Modified Capabilities
<!-- None. -->

## Impact

- Backend: new query + read service + endpoint (no domain/aggregate changes; read-only SQL). The same seam can later enrich booking rows/reviews with customer names (both currently placeholder — tracked).
- Flutter: `clients_page.dart`, `clients_cubit.dart`, nav/router wiring, composer prefill, `AppStrings`, cubit + widget tests.
- Testing note: the read service is SQL-against-two-schemas; covered by live E2E against the running host rather than a unit test (documented reason — no test double captures the join), plus Flutter cubit/widget tests for all app-side behavior.
