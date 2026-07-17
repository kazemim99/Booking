## Why

Working hours change seasonally and «ساعات کاری» is still a coming-soon row. Reconnaissance found the backend already ships a full hours API on `ProviderSettingsController` (`GET/PUT /providers/{id}/business-hours`, breaks included, matching the onboarding wire shape) — **but every write endpoint on that controller was unguarded: any authenticated user could rewrite any provider's hours, business info, location, holidays, exceptions, and service settings.**

## What Changes

- **Backend security fix**: all 11 write endpoints on `ProviderSettingsController` now require `CanManageProvider` (providerId claim, or in-process ownership fallback for first-session tokens — the established pattern). Live-verified: owner PUT 200 (via fallback), stranger PUT 403 on hours and business-info.
- **Flutter — working-hours editor** (`/more/hours`): Saturday-first weekly rows with an open/closed switch and open/close time pickers per day; existing breaks are shown and preserved on save (break *editing* is a follow-up). Pre-filled from `GET .../business-hours`; saved via `PUT .../business-hours`. The hub row activates.
- Reuses the onboarding `DayHours`/`ClockTime`/`BreakTime` entities and wire serialization.

## Capabilities

### New Capabilities
- `provider-working-hours-editing`: editing weekly hours from the app, break preservation, and the ownership guard on provider-settings writes.

### Modified Capabilities
<!-- None. -->

## Impact

- Backend: `ProviderSettingsController` (guards + helper only; no shape changes).
- Flutter: api (`getBusinessHours`/`updateBusinessHours`), repository, `BusinessHoursCubit`, `/more/hours` page, hub row, `AppStrings`, tests.
- Follow-ups queued: break editing UI; holidays/vacation UI (backend Holidays API discovered — enables the Home's vacation state).
