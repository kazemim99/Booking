## Why

«گالری» is the last coming-soon row in the app — providers cannot manage their public photos after onboarding, even though the backend gallery API (upload/list/metadata/reorder/set-primary/delete) has shipped since the Vue era. Recon also found the **same authorization defect a third time**: all five gallery write endpoints were `[Authorize]`-only — any authenticated user could upload to, reorder, or delete any provider's gallery.

## What Changes

- **Backend security fix**: the five gallery writes on `ProvidersController` (upload, metadata, reorder, set-primary, delete) now require `CanManageProvider` (the controller's existing helper with the first-session ownership fallback).
- **Flutter — gallery management** (`/more/gallery`): a thumbnail grid of the provider's photos with the primary image starred; upload via the device picker (multi-select, reusing the onboarding multipart pattern and `GalleryImageUpload`); per-image actions — set primary, delete behind confirmation. The hub row and the Setup checklist's «افزودن تصاویر گالری» item activate.
- **Deferred**: drag-reorder (endpoint exists; drag-grid UX is its own change), captions/alt-text editing.

## Capabilities

### New Capabilities
- `provider-gallery-management`: managing the public gallery from the app (upload, primary selection, deletion) and the ownership guard on gallery writes.

### Modified Capabilities
<!-- None. -->

## Impact

- Backend: `ProvidersController` gallery actions (guards only).
- Flutter: `GalleryImage` entity, api (`getGallery`/`uploadGalleryImages`/`deleteGalleryImage`/`setPrimaryGalleryImage`), repository, `GalleryCubit`, grid page with injectable picker (testability), hub/checklist wiring, `AppStrings`, tests.
- With this, **no coming-soon rows remain in the app**.
