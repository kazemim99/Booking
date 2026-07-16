## Context

Implements `provider-holidays-management`. Backend shipped and guarded (previous change): `GET /providers/{id}/holidays` → `{holidays:[{id, date:"yyyy-MM-dd", reason, isRecurring, pattern}]}`; `POST` `{date, reason, isRecurring, pattern?}`; `DELETE /holidays/{id}`. The Home's closed-day machinery (resolver precedence, `ClosedTodayBanner`, agenda visibility, elevated peek) exists and is fixture-tested — it only ever received `availability = open`.

## Goals / Non-Goals

**Goals:** days-off CRUD; the closed-day Home firing end-to-end; safe degradation.
**Non-Goals:** date-range vacation UX (API is per-date; the Home's VACATION state stays dormant until a range concept exists — the closed-day state covers single days honestly); block-time (exceptions API — queued); Jalali digits (queued); `pattern` semantics beyond yearly month/day recurrence.

## Decisions

- **D1 — Snapshot integration is best-effort**: `fetchSnapshot` fetches holidays alongside the catalog; any failure → `open`. Matching: exact `yyyy-MM-dd`, or month+day when `isRecurring`. This is *consumption* of backend-managed state per resolved decision #6, not client-side availability logic.
- **D2 — `HolidaysCubit` mirrors `StaffCubit`** (list + `Failure?` mutations reloading on success) — the established pattern.
- **D3 — Add-form uses `showDatePicker`** (Gregorian; Jalali remains the app-wide queued follow-up) with `initialDate = tomorrow`, gated on reason non-empty; recurring is a switch.
- **D4 — Banner copy becomes real** for `closedToday` («امروز تعطیل هستید») and `vacation` kinds in `StatusBannerRail`, replacing the generic fallback that existed while the states were unreachable.

## Risks / Trade-offs

- **[Extra call per snapshot]** one lightweight GET added to the Home load; best-effort so it never gates. 
- **[Timezone]** matching uses device-local today against the stored date string — consistent with how the provider entered it.

## Migration Plan

1. Entity/api/repo + snapshot integration (+ tests incl. closed-today lights-up) → 2. cubit (+ tests) → 3. page/hub/banner copy (+ widget tests) → 4. suites, commit, push.

## Open Questions

- None blocking. Queued: block-time (exceptions), range vacation, Jalali.
