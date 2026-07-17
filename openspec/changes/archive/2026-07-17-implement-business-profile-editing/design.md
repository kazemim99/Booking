## Context

Implements `provider-business-profile-editing`. Shapes verified in source: `GET /Providers/{id}` → `ProviderDetailsResponse {businessName, description, …}`; `PUT /Providers/business` → `{businessName, description?, logoUrl?}` (204). The endpoint's provider resolution uses the `providerId` claim unconditionally — the same first-session defect class as `BookingsController.CanManageProvider` (fixed) — while `UpdateProfile` in the same controller already does the owner-query lookup.

## Goals / Non-Goals

**Goals:** name/description editing with the app's form-sheet-era patterns (gated save, failure-preserves-input); the endpoint reachable in the first session.
**Non-Goals:** working hours (new backend command needed — queued next), logo/cover upload, category/address editing (address implies geocoding UX — future).

## Decisions

- **D1 — Backend fallback mirrors `UpdateProfile`**: claim if present, else `GetProviderByOwnerIdQuery(userId)`; 404 when neither resolves. No shape change; no new tests beyond existing suites (glue mirroring adjacent proven code; live-verifiable with the standing scripts).
- **D2 — Full-page form, not a sheet**: two fields where one is multiline description — a page (`/more/business`) matches the More sub-page pattern and keeps room for future fields (category, contact).
- **D3 — `BusinessProfileCubit` = load (details → prefill) + `save() → Failure?`** — the established mutation semantics.
- **D4 — Details parsed defensively** (`businessName|name`, `description`) via the existing envelope helpers.

## Risks / Trade-offs

- **[PUT replaces description with empty when cleared]** intended behavior (clearing is a valid edit); name is gated non-empty.
- **[No live E2E this change]** the fallback mirrors adjacent proven code; the standing verify scripts cover it next run.

## Migration Plan

1. Backend fallback → build + unit suite. 2. Flutter entity/api/repo (+tests) → cubit (+tests) → page/route/hub (+widget tests). 3. Suite, commit, push.

## Open Questions

- None blocking. Queued: `UpdateBusinessHoursCommand` + `PUT /Providers/{id}/business-hours` for the hours editor (reusing the registration step-6 DTO mapping).
