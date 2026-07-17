## Context

Implements `provider-service-crud`. Backend verified in source: `POST /providers/{id}/services` `{serviceName, description?, durationHours, durationMinutes, price, currency?, category?, isMobileService}`; `PUT .../{serviceId}` same field set; `DELETE` → 204 — all `CanManageProvider`-guarded since the hours sweep.

## Goals / Non-Goals

**Goals:** the three mutations with the app's established form-sheet grammar; description round-trip.
**Non-Goals:** category picker, mobile-service flag, currency selection (server defaults IRR), service status management.

## Decisions

- **D1 — Duration entered as minutes**, split into hours/minutes for the wire (`h = m ~/ 60`).
- **D2 — `ComposerService` gains `description`** (single service model app-wide; composer unaffected) so PUT never wipes it.
- **D3 — One `_ServiceFormSheet`** for add and edit (nullable `initial`), mirroring the staff form; numeric fields with `TextInputType.number`, gating: name non-empty ∧ duration>0 ∧ price>0.
- **D4 — Mutations on `ServicesCubit`** via the shared reload-on-success pattern.

## Risks / Trade-offs

- **[Full-field PUT]** unedited fields are sent back from the pre-filled form — description carried explicitly (D2); category/mobile default server-side for MVP-created services.

## Migration Plan

api/repo (+tests) → cubit mutations (+tests) → form/list UI (+widget tests) → suite, commit, push.

## Open Questions

- None.
