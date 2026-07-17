## Context

Implements `provider-working-hours-editing`. Backend discovered complete: `ProviderSettingsController` `GET /providers/{id}/business-hours` → `{businessHours:[{dayOfWeek, dayName, isOpen, openTime:"HH:mm", closeTime, breaks:[{startTime,endTime,label}]}]}`; `PUT` takes the onboarding step-6 wire shape (`openTime:{hours,minutes}`, `breaks:[{start,end}]`). All writes on that controller were unguarded (live-confirmed exploitable) — fixed in this change and live-verified (owner 200 incl. first-session fallback; stranger 403).

## Goals / Non-Goals

**Goals:** weekly hours editing with break preservation; the settings controller safe.
**Non-Goals:** break editing UI (follow-up); holidays/vacation UI (backend Holidays API exists — queued, it unlocks the Home's vacation state); Jalali digits.

## Decisions

- **D1 — Guards at the controller via `CanManageProvider`** (claim → in-process ownership fallback), inserted mechanically into all 11 writes; the user-id short-circuit is deliberately omitted because this controller reads raw `sub`/`userId` claims that inbound claim mapping may rename — the claim check and fallback are authoritative. Covered by live E2E rather than unit tests (documented: controller auth glue, no handler seam; a Reqnroll scenario is the right future home).
- **D2 — Reuse onboarding's `DayHours`/`ClockTime`/`BreakTime` entities and serialization** — the wire shape is identical; a parallel model would drift.
- **D3 — Editor state lives in `BusinessHoursCubit`** (`load` → prefill; `toggleDay`/`setOpenTime`/`setCloseTime` pure state edits; `save → Failure?`). Time selection via `showTimePicker`; days ordered Saturday-first (Iranian week, same as the calendar).
- **D4 — Breaks render as read-only chips and round-trip untouched** in the payload, honoring the always-with-breaks backend path without risking silent erasure.

## Risks / Trade-offs

- **[Whole-week PUT]** the payload always carries all 7 days (prefill guarantees completeness) — matches the endpoint's replace semantics.
- **[Guard breadth]** 11 endpoints gained a guard in one change — mechanical, identical insertion; live-verified on two representative writes; keystone gate re-run.

## Migration Plan

1. Backend guards (done, live-verified) → 2. Flutter api/repo (+tests) → cubit (+tests) → page/hub (+widget tests) → 3. suites, commit, push.

## Open Questions

- None blocking. Queued: break editing; holidays UI (vacation state); Reqnroll authorization scenarios.
