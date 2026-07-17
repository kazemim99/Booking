## Context

Implements `provider-break-editing` on the existing hours editor (`BusinessHoursView`). The backend round-trips breaks (verified live in the hours change); chips render already — they just lack mutations.

## Goals / Non-Goals

**Goals:** add/remove weekly breaks as pure cubit state; zero backend work.
**Non-Goals:** per-date breaks (backend gap), break overlap validation beyond start<end (the domain's `BreakPeriod.Create` enforces its own rules server-side).

## Decisions

- **D1 — Mutations mirror the existing `_editDay` pattern**: `addBreak(day, BreakTime)` appends; `removeBreak(day, index)` removes by position (chips are positional; times can repeat).
- **D2 — Add-break UX = two sequential `showTimePicker`s** (start then end) from an «+ استراحت» text button on open days — no new sheet; client-side guard start<end.
- **D3 — Chips gain `onDeleted`** (Material chip delete affordance) — no confirmation: the edit is unsaved until «ذخیره», which is the safety net.

## Risks / Trade-offs

- **[Overlapping breaks]** allowed client-side; the domain validates on save and the failure surfaces via the existing failure-preserves-edits path.

## Migration Plan

Cubit mutations (+tests) → chip/add UI (+widget tests) → suite, commit, push.

## Open Questions

- None.
