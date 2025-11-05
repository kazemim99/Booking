# Provider Image Gallery Proposal

## Why
Providers currently can only upload a logo and cover image (as noted in the provider-management spec). They need the ability to showcase their work through a comprehensive image gallery that displays their portfolio, workspace, and business ambiance. This feature is essential for service providers (salons, spas, clinics, etc.) who rely heavily on visual presentation to attract customers. Currently, the frontend has placeholder UI (GalleryView.vue) that stores images only in browser memory without backend persistence.

## What Changes
- Add gallery image management to the Provider aggregate as a single unified gallery
- Implement file upload infrastructure with local storage and CDN-ready architecture
- Add backend API endpoints for gallery CRUD operations (upload, reorder, update metadata, delete)
- Complete the frontend gallery component with drag-and-drop upload, image optimization (WebP/AVIF), and reordering
- Implement image validation, optimization, and storage strategy aligned with DDD principles
- Add accessibility features (ARIA labels, keyboard navigation) and responsive design

## Impact
- **Affected specs**: `provider-management` (MODIFIED - add gallery visibility controls), new spec `provider-gallery-management` (ADDED)
- **Affected code**:
  - **Backend**:
    - Domain: `BusinessProfile` entity (add gallery images collection)
    - Infrastructure: New file storage service, image upload configuration
    - API: New `GalleryController` or extend `ProvidersController` with gallery endpoints
    - Database: New migration for gallery images table
  - **Frontend**:
    - Complete `booksy-frontend/src/modules/provider/views/gallery/GalleryView.vue`
    - Add gallery service and store
    - New reusable gallery components (GalleryUpload, GalleryGrid, GalleryImageCard)
- **Database**: New `provider_gallery_images` table owned by `BusinessProfile`
- **Storage**: Local file system initially (`wwwroot/uploads/providers/{providerId}/gallery/`), CDN-ready URLs
- **Performance**: Image optimization during upload (resize, WebP conversion), lazy loading on frontend
- **Breaking changes**: None - this is additive functionality
