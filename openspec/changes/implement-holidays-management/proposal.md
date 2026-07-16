## Why

The Home's closed-day state — designed, spec'd, resolver-tested, and banner-railed since `design-provider-home-workspace` — has never fired: the repository hardcodes `availability = open` because "vacation/closed are backend-managed" (resolved decision #6) and no backend concept was known. The previous change discovered that concept fully shipped: the Holidays API (`GET/POST/DELETE /providers/{id}/holidays`, now ownership-guarded). Managing days off is also a real provider need with no UI.

## What Changes

- **Flutter — holidays management** (`/more/holidays`, «تعطیلات و مرخصی»): list upcoming holidays (date, reason, recurring badge), add via a form sheet (date picker + reason + recurring toggle), remove with confirmation.
- **Home consumption**: `fetchSnapshot` reads the holidays best-effort; when today matches one, `availability = closedToday` — which the existing resolver/registry/banners turn into the designed closed-day Home (banner + agenda hidden + coming-up elevated) with zero composition changes.
- **Banner copy**: the closed-today and vacation banner kinds get real Persian copy (they previously fell through to a generic fallback because they were unreachable).
- Recurring holidays match by month/day annually; failures to load holidays degrade to `open` (never lock a provider's Home over a side-signal).

## Capabilities

### New Capabilities
- `provider-holidays-management`: managing days off from the app and the Home's consumption of them as the closed-day availability state.

### Modified Capabilities
<!-- None. provider-home-workspace already specifies the closed-day state; this supplies its input. -->

## Impact

- Flutter only (backend shipped + guarded last change): entity, api (`getHolidays`/`addHoliday`/`deleteHoliday`), repository (+ snapshot availability), `HolidaysCubit`, page, hub row, banner copy, `AppStrings`, tests.
- Queued follow-ups: block-time via the availability-exceptions API; date-range vacation UX (the current API is per-date).
