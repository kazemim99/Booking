## Why

The hours editor displays breaks as read-only chips — a provider can't add their lunch break or remove a stale one without re-running onboarding. The backend fully supports per-weekday breaks (the hours PUT already round-trips them, live-verified), so this is pure Flutter state on an existing screen.

## What Changes

- Break chips in the hours editor become **removable** (chip delete affordance).
- Each open day gains an **«+ استراحت»** action: pick a start and end time → the break is added to that day's row and saved with the whole week.
- Both edits are pure cubit state until the existing «ذخیرهٔ ساعات کاری» save, which already sends breaks.

## Capabilities

### New Capabilities
- `provider-break-editing`: adding and removing per-weekday breaks in the hours editor.

### Modified Capabilities
<!-- None. -->

## Impact

- Flutter only: `BusinessHoursCubit.addBreak/removeBreak`, hours-editor chip/add UI, `AppStrings`, cubit + widget tests. No backend, no new endpoints. Per-date breaks (mid-day block-time holes) remain the separate flagged backend gap.
