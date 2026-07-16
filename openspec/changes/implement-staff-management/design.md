## Context

Implements `provider-staff-management`. Backend CRUD verified in source: `AddStaffRequest {firstName, lastName, email?, phoneNumber?, role}` (POST), `UpdateStaffRequest` (PUT, full-name fields), DELETE — all under `CanManageProvider` and already exercised by the keystone gate. The app's More → تیم is read-only; the Setup checklist's staff item is stubbed.

## Goals / Non-Goals

**Goals:** add/edit/remove staff with the app's established patterns (form in a bottom sheet, `Failure?` mutations, reload on success); light up the checklist path.
**Non-Goals:** staff photo upload; per-staff working hours; role taxonomy (free-text field with the backend default "ServiceProvider" preserved on add when blank).

## Decisions

- **D1 — One form sheet for add and edit** (`StaffFormSheet`): pre-filled = edit (shows remove), empty = add. Same field set: first name (required), last name, phone, role. Mirrors the composer's sheet grammar.
- **D2 — Mutations live on `StaffCubit`** (add/update/remove → `Failure?`, success reloads the list) — identical semantics to Home/Calendar mutations so failures surface the mapped Persian message and the UI never optimistically lies.
- **D3 — `ProviderStaffMember` gains firstName/lastName/phone** (API returns them; needed for edit prefill). `name` remains the display field (`fullName` fallback to first+last).
- **D4 — Remove is confirm-guarded** with a plain `AlertDialog` (destructive, per spec).
- **D5 — Checklist wiring is navigation-only**: the Home's checklist `staff` item pushes `/more/staff`; per-item completion signals (staff count > 0 ticking the item) remain part of the future completeness work — out of scope here.

## Risks / Trade-offs

- **[PUT is full-replace on name fields]** the form always submits all fields (pre-filled), so partial-update semantics don't bite.
- **[No live E2E this change]** endpoints already proven by keystone + earlier verifications; app-side covered by cubit/widget tests.

## Migration Plan

1. Entity + api + repo (+ tests) → 2. cubit mutations (+ tests) → 3. form sheet + StaffView actions + checklist wiring (+ widget tests) → 4. suite, commit.

## Open Questions

- None blocking.
