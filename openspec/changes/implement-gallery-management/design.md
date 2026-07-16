## Context

Implements `provider-gallery-management`. Backend verified in source: `GET /Providers/{id}/gallery` → `GalleryImageResponse {id, thumbnailUrl, mediumUrl, originalUrl, displayOrder, caption, altText, uploadedAt, isPrimary}`; `POST` multipart `files` (≤10 files/10MB, 50MB request); `PUT .../{imageId}/set-primary`; `DELETE .../{imageId}`. All five writes were `[Authorize]`-only — guarded in this change with the controller's existing `CanManageProvider` (route param is `providerId`).

## Goals / Non-Goals

**Goals:** grid + upload + primary + delete; the last coming-soon row gone; gallery writes safe.
**Non-Goals:** drag-reorder (endpoint exists; separate change), caption/alt editing, image cropping/compression beyond the picker's `imageQuality`.

## Decisions

- **D1 — Reuse the onboarding upload pattern**: multipart `files` of `GalleryImageUpload {bytes, name}` (already proven against the registration endpoint) — one upload idiom app-wide.
- **D2 — Injectable image picker**: the page takes a `pickImages` function defaulting to `ImagePicker.pickMultiImage(imageQuality: 80)`; widget tests inject a fake — no platform-channel mocking.
- **D3 — `GalleryCubit` mirrors the More mutation pattern** (`Failure?` + reload on success).
- **D4 — Grid thumbnails via `Image.network` with an error placeholder** so tests (where network images 400) and flaky URLs degrade to a neutral tile instead of crashing.
- **D5 — Per-image actions in a bottom sheet** (set primary / delete-confirm) — tap target friendly on a dense grid, same sheet grammar as the rest of the app.
- **D6 — No live E2E in this change**: the guard insertion is byte-identical to the two previously live-verified ones; upload multipart is proven by onboarding's registration flow. The standing verify scripts cover gallery next run (documented).

## Risks / Trade-offs

- **[Large uploads on mobile data]** picker uses `imageQuality: 80` (same as onboarding); backend enforces 10MB/file.
- **[Primary semantics server-side]** set-primary is a single PUT; the grid re-reads truth after each mutation rather than trusting local toggles.

## Migration Plan

1. Backend guards (done, builds) → 2. entity/api/repo (+tests) → cubit (+tests) → page/wiring (+widget tests) → 3. suites, commit, push.

## Open Questions

- None blocking. Queued: reorder UX, captions.
