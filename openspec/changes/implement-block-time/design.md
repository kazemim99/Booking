## Context

Implements `provider-block-time`. Backend verified in source: `AddExceptionRequestDto {date: DateOnly, openTime: TimeOnly?, closeTime: TimeOnly?, reason}` (null times = closed all day; the domain rejects exceptions on holiday dates); `GET` → `{exceptions:[{id, date, openTime "HH:mm"?, closeTime?, reason, isClosed}]}`; `DELETE /{exceptionId}`. `AvailabilityService` consumes exceptions when generating slots, so blocks are effective, and the writes were guarded in the hours change.

## Goals / Non-Goals

**Goals:** the last stubbed action goes live; blocks manageable (list + delete); today's all-day block reaches the Home.
**Non-Goals:** mid-day holes (per-date breaks — backend model gap, flagged); recurring exceptions; calendar-timeline visualization of blocks (queued; the day's slot absence already reflects them).

## Decisions

- **D1 — One shared `BlockTimeSheet`** invoked from both ⊕ menus; `initialDate` param (Calendar passes its selected day). Mode toggle: all-day (default) vs modified hours with two time pickers.
- **D2 — `blockTime` lives on `HomeCubit` and `CalendarCubit`** (thin delegations to the repository with their existing refresh-on-success semantics) — the sheet stays cubit-agnostic by taking a submit callback.
- **D3 — Exceptions list rides the Holidays page** (both are "special dates"); its own `ExceptionsCubit` (`_MoreLoadCubit` + delete mutation) so the two lists load/fail independently.
- **D4 — `_todayAvailability` extends to exceptions**: closed = holiday match **or** a today-dated `isClosed` exception; both fetches best-effort, failure → open.
- **D5 — Times serialized as "HH:mm"** strings (TimeOnly binds them; GET already returns that shape).

## Risks / Trade-offs

- **[Two lookups per snapshot]** holidays + exceptions on Home load; both tiny, parallelized, best-effort.
- **[Whole-date semantics]** users may expect mid-day blocks; the sheet's copy says ساعات آن روز را تغییر می‌دهد and the modified-hours mode covers early-close/late-open, the common cases.

## Migration Plan

1. Entity/api/repo + snapshot extension (+ tests) → 2. cubits + sheet + Holidays section (+ tests) → 3. suites, commit, push.

## Open Questions

- None blocking. Queued: per-date breaks (backend), calendar visualization of blocked ranges.
