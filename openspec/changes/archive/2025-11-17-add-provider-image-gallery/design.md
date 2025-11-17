# Provider Image Gallery - Design Document

## Context
The system needs to support provider image galleries to showcase their work and business environment. Currently:
- BusinessProfile entity only has `LogoUrl` (string) property
- Frontend has UI (`GalleryView.vue`) that stores images in memory only
- No backend persistence or file upload capability exists
- The appsettings show `ImageService.Provider: "Local"` with optimization disabled

The gallery must align with DDD principles, support future CDN migration, and provide excellent UX with modern image formats and accessibility.

## Goals / Non-Goals

**Goals:**
- Enable providers to manage a gallery of images (upload, reorder, caption, delete)
- Support multiple image categories (portfolio work, workspace photos)
- Store image metadata in the database, files on disk (initially local, CDN-ready)
- Optimize images during upload (resize, WebP/AVIF conversion) for performance
- Provide responsive, accessible UI with drag-and-drop upload
- Maintain DDD architecture: BusinessProfile owns gallery images as entities/value objects
- Support future migration to cloud storage (Azure Blob, AWS S3) without API changes

**Non-Goals:**
- Advanced image editing (cropping, filters) - providers upload pre-edited images
- Video upload support (out of scope for this change)
- AI-based image moderation or tagging (future enhancement)
- Direct CDN integration (local storage first, architecture supports future CDN)
- Social media integration (Instagram import, etc.)

## Decisions

### 1. Domain Modeling: Gallery Image as Entity vs Value Object
**Decision:** Model `GalleryImage` as an **Entity** (not Value Object) within the Provider aggregate.

**Rationale:**
- Each image has identity (Guid Id) and lifecycle (uploaded, updated metadata, reordered, deleted)
- Images have mutable metadata (caption, display order, active status)
- Need to track upload timestamp and order independently
- Entity pattern fits better than Value Object for this use case

**Implementation:**
```csharp
public class GalleryImage : Entity<Guid>
{
    public ProviderId ProviderId { get; private set; }
    public string ImageUrl { get; private set; }
    public string ThumbnailUrl { get; private set; }
    public string MediumUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Caption { get; private set; }
    public string? AltText { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public bool IsActive { get; private set; }
}
```

**Alternative Considered:** Value Object with immutability - rejected because images need individual identity and mutable metadata.

### 2. File Storage Strategy
**Decision:** Use **local file system** initially with **CDN-ready URL structure**, abstract storage behind `IFileStorageService`.

**Storage Path Structure:**
```
/wwwroot/uploads/providers/{providerId}/gallery/{imageId}_{size}.{ext}
Example: /wwwroot/uploads/providers/abc123/gallery/img001_thumb.webp
```

**Rationale:**
- Simplicity first: local storage is easiest to implement and test
- CDN-ready: absolute URLs in database allow seamless migration to cloud storage
- Abstraction: `IFileStorageService` interface allows swapping storage providers
- Multiple sizes: Generate thumbnail (200x200), medium (800x800), original

**Implementation:**
```csharp
public interface IFileStorageService
{
    Task<StorageResult> UploadImageAsync(ProviderId providerId, Stream imageStream, string fileName, CancellationToken ct);
    Task<bool> DeleteImageAsync(string imageUrl, CancellationToken ct);
    Task<string> GetImageUrlAsync(string relativePath);
}

public class LocalFileStorageService : IFileStorageService
{
    // Stores files in wwwroot/uploads/providers/{providerId}/gallery/
    // Returns full URL: https://localhost:7002/uploads/providers/{providerId}/gallery/{imageId}.webp
}
```

**Alternative Considered:** Direct cloud storage (Azure Blob) - rejected for initial implementation complexity; will be easy to add later via interface.

### 3. Image Optimization Strategy
**Decision:** Server-side image processing during upload using **ImageSharp** library (already used in .NET ecosystem).

**Optimizations:**
- Validate file size (max 10MB), format (JPEG, PNG, WebP), dimensions
- Generate 3 versions: thumbnail (200x200), medium (800x800), original (max 2000x2000)
- Convert to WebP format for modern browsers (fallback to JPEG for older browsers)
- Preserve aspect ratio, use smart cropping for thumbnails

**Rationale:**
- Server-side processing ensures consistent quality across all clients
- ImageSharp is battle-tested, cross-platform, and integrates well with ASP.NET Core
- Multiple sizes improve performance (use thumbnail in grid, full in lightbox)
- WebP reduces file size by ~30% vs JPEG with similar quality

**Implementation:**
```csharp
public interface IImageOptimizationService
{
    Task<OptimizedImage> OptimizeAsync(Stream source, ImageOptimizationOptions options);
}

public class OptimizedImage
{
    public byte[] Thumbnail { get; set; }
    public byte[] Medium { get; set; }
    public byte[] Original { get; set; }
    public string Format { get; set; } // "webp"
}
```

**Alternative Considered:** Client-side compression - rejected because it's inconsistent across devices and browsers.

### 4. API Design
**Decision:** Add gallery endpoints to existing `ProvidersController` (RESTful, nested resource).

**Endpoints:**
```
POST   /api/v1/providers/{providerId}/gallery           - Upload images (multipart/form-data)
GET    /api/v1/providers/{providerId}/gallery           - Get all gallery images
PUT    /api/v1/providers/{providerId}/gallery/{imageId} - Update metadata (caption, order)
DELETE /api/v1/providers/{providerId}/gallery/{imageId} - Delete image
PUT    /api/v1/providers/{providerId}/gallery/reorder   - Reorder multiple images
```

**Rationale:**
- Gallery is a sub-resource of Provider, so nested routes are RESTful
- Keeps all provider-related endpoints in one controller
- Supports batch operations (upload multiple, reorder)

**Request/Response Models:**
```csharp
public class UploadGalleryImagesRequest
{
    public List<IFormFile> Images { get; set; }
}

public class GalleryImageResponse
{
    public Guid Id { get; set; }
    public string ThumbnailUrl { get; set; }
    public string MediumUrl { get; set; }
    public string OriginalUrl { get; set; }
    public int DisplayOrder { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public DateTime UploadedAt { get; set; }
}
```

**Alternative Considered:** Separate `GalleryController` - rejected to avoid controller proliferation; gallery is tightly coupled to provider.

### 5. Frontend Architecture
**Decision:** Component-based architecture with **drag-and-drop**, **progressive image loading**, and **optimistic UI updates**.

**Component Structure:**
```
booksy-frontend/src/modules/provider/
  components/gallery/
    GalleryUpload.vue        - Drag-and-drop upload with progress
    GalleryGrid.vue          - Image grid with lazy loading
    GalleryImageCard.vue     - Individual image with actions (edit, delete)
    ImageLightbox.vue        - Full-screen image viewer
  services/
    gallery.service.ts       - API calls
  stores/
    gallery.store.ts         - Gallery state management (Pinia)
  views/gallery/
    GalleryView.vue          - Main gallery page (refactor existing)
```

**Key Features:**
- **Drag-and-drop**: HTML5 drag-and-drop API for file upload
- **Progress feedback**: Show upload progress per image (0-100%)
- **Optimistic updates**: Add image to UI immediately, show error if upload fails
- **Lazy loading**: Intersection Observer API to load images as they scroll into view
- **Accessibility**: ARIA labels, keyboard navigation (Tab, Enter, Delete keys), alt text for all images

**Rationale:**
- Component reusability (gallery components can be used in customer-facing profile)
- Modern UX patterns align with user expectations
- Performance optimization (lazy loading) is critical for large galleries

**Alternative Considered:** Third-party library (vue-upload-component) - rejected to keep dependencies minimal and maintain full control over UX.

### 6. Database Schema
**Decision:** New `provider_gallery_images` table owned by `business_profiles` (EF Core owned entity).

**Schema:**
```sql
CREATE TABLE provider_gallery_images (
    id UUID PRIMARY KEY,
    business_profile_id UUID NOT NULL REFERENCES business_profiles(id) ON DELETE CASCADE,
    provider_id UUID NOT NULL REFERENCES providers(id) ON DELETE CASCADE,
    image_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500) NOT NULL,
    medium_url VARCHAR(500) NOT NULL,
    display_order INT NOT NULL DEFAULT 0,
    caption VARCHAR(500),
    alt_text VARCHAR(500),
    uploaded_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    INDEX idx_provider_order (provider_id, display_order),
    INDEX idx_display_order (business_profile_id, display_order)
);
```

**EF Core Configuration:**
```csharp
// In BusinessProfileConfiguration.cs
builder.OwnsMany(bp => bp.GalleryImages, gi =>
{
    gi.ToTable("provider_gallery_images");
    gi.WithOwner().HasForeignKey("BusinessProfileId");
    gi.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);
    gi.Property(x => x.ThumbnailUrl).IsRequired().HasMaxLength(500);
    gi.Property(x => x.MediumUrl).IsRequired().HasMaxLength(500);
    gi.Property(x => x.Caption).HasMaxLength(500);
    gi.Property(x => x.AltText).HasMaxLength(500);
    gi.Property(x => x.DisplayOrder).IsRequired();
    gi.HasIndex(x => new { x.ProviderId, x.DisplayOrder });
});
```

**Rationale:**
- Owned entities maintain aggregate boundary (GalleryImage is part of Provider aggregate)
- Cascade delete ensures cleanup when provider is removed
- Indexes optimize queries (get images by provider + category, sorted by order)
- URLs stored as strings allow easy migration to CDN

**Alternative Considered:** Separate `GalleryImages` aggregate - rejected because images have no meaning outside of a provider context.

## Risks / Trade-offs

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Local storage not scalable** | High load may fill disk or slow down app | Monitor disk usage; plan CDN migration within 3 months after launch |
| **Large image uploads block API** | Users may upload 10MB+ images, blocking request thread | Set max file size (10MB), use async file I/O, consider background job for optimization |
| **Image optimization CPU-intensive** | May slow down API response time | Implement background job processing (Hangfire) for optimization; return 202 Accepted immediately |
| **No content moderation** | Inappropriate images may be uploaded | Add manual review workflow for new providers; integrate AI moderation (Azure Content Moderator) in future |
| **Browser compatibility (WebP)** | Older browsers may not support WebP | Serve JPEG fallback via `<picture>` element with multiple sources |

## Migration Plan

### Phase 1: Backend Foundation (Week 1)
1. Add `GalleryImage` entity to Domain layer
2. Update `BusinessProfile` to own `GalleryImages` collection
3. Create EF Core migration for `provider_gallery_images` table
4. Implement `LocalFileStorageService` and `IImageOptimizationService` (ImageSharp)
5. Add gallery endpoints to `ProvidersController`
6. Write unit tests for domain logic and integration tests for API

### Phase 2: Frontend Implementation (Week 2)
1. Create gallery service (`gallery.service.ts`) and store (`gallery.store.ts`)
2. Build reusable components (GalleryUpload, GalleryGrid, GalleryImageCard)
3. Refactor `GalleryView.vue` to use new backend API
4. Add drag-and-drop upload with progress feedback
5. Implement image reordering (drag-to-reorder)
6. Add accessibility features (ARIA, keyboard nav)

### Phase 3: Optimization & Polish (Week 3)
1. Implement lazy loading for gallery grid
2. Add image lightbox for full-screen viewing
3. Optimize frontend bundle size (lazy load components)
4. Add error handling and retry logic for uploads
5. Write E2E tests (Cypress) for upload and delete flows

### Phase 4: Production Readiness (Week 4)
1. Load testing with large galleries (100+ images)
2. Security review (file upload validation, malicious file detection)
3. Add monitoring (file storage metrics, upload success rate)
4. Documentation (API docs, user guide)
5. Deploy to staging and perform UAT

**Rollback Plan:**
- Feature flagged: `FeatureFlags.EnableProviderGallery = false` to disable functionality
- Database: Migration is additive (new table), safe to rollback by setting flag to false
- File storage: Old code doesn't reference gallery files, safe to leave orphaned files (cleanup script provided)

## Open Questions

1. **Should we limit the number of gallery images per provider?**
   - Recommendation: Yes, set limit to 50 images initially (configurable via appsettings)
   - Rationale: Prevents abuse, ensures reasonable page load times

2. **Should we support image captions in multiple languages (i18n)?**
   - Recommendation: No, not in this change. Single caption field for MVP.
   - Future: Add `GalleryImageTranslation` table if multi-language support is needed

3. **Should customers be able to comment on or react to gallery images?**
   - Recommendation: Out of scope for this change. Focus on provider management first.
   - Future: Add engagement features (likes, comments) in a separate change

4. **Do we need video upload support?**
   - Recommendation: No, images only for this change. Videos require different infrastructure (streaming, transcoding).
   - Future: Evaluate based on user feedback after gallery launch

5. **Should we integrate with external image services (Instagram, Google Photos)?**
   - Recommendation: No, manual upload only for MVP.
   - Future: Consider import functionality based on demand
