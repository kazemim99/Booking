## Why

«مسدود کردن زمان» in the ⊕ create menu is the last stubbed action in the app — the IA promised it from day one (block a lunch, leave early, close an odd day). The availability-exceptions API (`GET/POST/DELETE /providers/{id}/exceptions`, guarded two changes ago) is fully consumed by slot generation (`AvailabilityService` honors exceptions at three points), so blocking time genuinely removes bookable slots.

## What Changes

- **Block-time sheet** from the ⊕ menu on Home and Calendar: pick a date, choose **all-day closed** or **modified hours** (open/close pickers), give a reason — creating an availability exception for that date. Calendar pre-dates the sheet to its selected day; success refreshes the host screen so slots/agenda reflect it.
- **Exceptions are manageable**: the Holidays page gains a «ساعات استثنائی» section listing exceptions (date, hours-or-closed, reason) with confirm-guarded delete.
- **Home consumption completes**: a closed-all-day exception for today also resolves `availability = closedToday` (alongside holidays), same best-effort degradation.
- **Semantics honestly scoped**: the backend model overrides a whole date's hours — mid-day holes (block 13:00–14:00 only) are not expressible and are flagged as the remaining backend gap (per-date breaks).

## Capabilities

### New Capabilities
- `provider-block-time`: creating and removing per-date availability exceptions from the app, and the Home's consumption of a closed-today exception.

### Modified Capabilities
<!-- None. -->

## Impact

- Flutter only (API shipped, guarded, and consumed by availability): `AvailabilityException` entity, api/repository CRUD, `blockTime` on Home/Calendar cubits, shared `BlockTimeSheet`, exceptions section + `ExceptionsCubit` on the Holidays page, `AppStrings`, tests.
- With this, **no stubbed actions remain** — every IA affordance is live.
