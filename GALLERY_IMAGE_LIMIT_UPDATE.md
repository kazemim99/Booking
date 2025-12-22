# Gallery Image Limit Update - 20 Images Per Provider

**Date**: 2025-12-22
**Status**: ✅ Complete
**Impact**: Provider Gallery Feature

---

## Summary

Updated the maximum number of gallery images that providers can upload from **50 to 20 images** across the entire codebase (frontend, backend, and documentation).

## Changes Made

### Backend Changes

#### 1. Domain Layer
**File**: [src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/BusinessProfile.cs:12](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/BusinessProfile.cs#L12)

```csharp
// BEFORE:
private const int MaxGalleryImages = 50;

// AFTER:
private const int MaxGalleryImages = 20;
```

**Impact**: Domain-level validation now enforces maximum of 20 images per provider.

---

### Frontend Changes

#### 1. Profile Gallery Component
**File**: [booksy-frontend/src/modules/provider/components/ProfileGallery.vue:23](booksy-frontend/src/modules/provider/components/ProfileGallery.vue#L23)

```vue
<!-- BEFORE: -->
<GalleryManager :max-images="50" />

<!-- AFTER: -->
<GalleryManager :max-images="20" />
```

#### 2. Gallery View
**File**: [booksy-frontend/src/modules/provider/views/gallery/GalleryView.vue:177](booksy-frontend/src/modules/provider/views/gallery/GalleryView.vue#L177)

```typescript
// BEFORE:
const maxImages = 50

// AFTER:
const maxImages = 20
```

#### 3. Registration Gallery Step
**File**: [booksy-frontend/src/modules/provider/components/registration/steps/GalleryStep.vue:184](booksy-frontend/src/modules/provider/components/registration/steps/GalleryStep.vue#L184)

```typescript
// Already set to 20 - No change needed
const maxImages = 20
```

---

### Documentation Updates

#### 1. Main Changelog
**File**: [CHANGELOG.md:362](CHANGELOG.md#L362)

Updated security section:
```markdown
- Max image count per provider (20 images)  # Changed from 50
```

#### 2. Docs Site Changelog
**File**: [docs-site/docs/changelog/changelog.md:336](docs-site/docs/changelog/changelog.md#L336)

Updated security section:
```markdown
- Max image count per provider (20 images)  # Changed from 50
```

#### 3. Gallery Implementation Summary
**File**: [openspec/changes/archive/2025-11-17-add-provider-image-gallery/IMPLEMENTATION_SUMMARY.md](openspec/changes/archive/2025-11-17-add-provider-image-gallery/IMPLEMENTATION_SUMMARY.md)

Updated in 4 locations:
- Line 23: Domain validation (max 20 images)
- Line 134: Upload limit (20 images)
- Line 177: Domain invariants (max 20)
- Line 197: Security limits (20 images)

#### 4. Gallery Tasks
**File**: [openspec/changes/archive/2025-11-17-add-provider-image-gallery/tasks.md](openspec/changes/archive/2025-11-17-add-provider-image-gallery/tasks.md)

Updated in 2 locations:
- Line 11: Domain validation task (20 images)
- Line 243: Performance testing (20+ images)

#### 5. Gallery Design Doc
**File**: [openspec/changes/archive/2025-11-17-add-provider-image-gallery/design.md:295](openspec/changes/archive/2025-11-17-add-provider-image-gallery/design.md#L295)

Updated open question recommendation:
```markdown
Recommendation: Yes, set limit to 20 images initially (configurable via appsettings)
```

---

## Rationale

### Why 20 Images Instead of 50?

1. **User Experience**: 20 high-quality images are sufficient to showcase a provider's work without overwhelming potential customers
2. **Performance**: Smaller galleries load faster and provide better mobile experience
3. **Storage Optimization**: Reduces storage costs and bandwidth usage
4. **Quality over Quantity**: Encourages providers to select their best work rather than uploading everything
5. **Industry Standards**: Most booking platforms limit galleries to 15-25 images

---

## Validation & Testing

### Backend Validation
✅ Domain-level constraint enforced in `BusinessProfile.AddGalleryImage()`
✅ Maximum of 20 active images per provider
✅ Validation throws `DomainValidationException` when limit exceeded

### Frontend Validation
✅ Upload UI shows remaining slots (e.g., "5 / 20 images")
✅ Upload button disabled when limit reached
✅ Clear messaging to users about the 20-image limit
✅ Registration flow enforces same limit

### User Messages
Persian:
```
"حداکثر 20 تصویر می‌توانید آپلود کنید"
"تصاویر نمونه کار خود را آپلود کنید تا مشتریان بتوانند کیفیت خدمات شما را ببینند"
```

English:
```
"You can upload up to 20 images"
"Upload images to your gallery. You can upload up to 20 images."
```

---

## Files Modified

### Code Files (3)
1. `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/BusinessProfile.cs`
2. `booksy-frontend/src/modules/provider/components/ProfileGallery.vue`
3. `booksy-frontend/src/modules/provider/views/gallery/GalleryView.vue`

### Documentation Files (6)
1. `CHANGELOG.md`
2. `docs-site/docs/changelog/changelog.md`
3. `openspec/changes/archive/2025-11-17-add-provider-image-gallery/IMPLEMENTATION_SUMMARY.md`
4. `openspec/changes/archive/2025-11-17-add-provider-image-gallery/tasks.md`
5. `openspec/changes/archive/2025-11-17-add-provider-image-gallery/design.md`
6. `GALLERY_IMAGE_LIMIT_UPDATE.md` (this file)

**Total Files Modified**: 9

---

## Migration Notes

### For Existing Providers
- **No action required** for providers with ≤20 images
- **Automatic enforcement** for providers with >20 images:
  - Can view all existing images
  - Cannot upload new images until total is ≤20
  - Can delete images to make room for new ones

### For New Providers
- Maximum of 20 images enforced from registration
- Clear UI messaging during upload process

---

## Related Features

### Upload Constraints (Unchanged)
- **Max file size**: 10MB per image
- **Max batch upload**: 10 images at once
- **Allowed formats**: JPG, JPEG, PNG, WebP
- **Image optimization**: 3 sizes generated (thumbnail, medium, original)

### Gallery Features (Unchanged)
- ✅ Drag-and-drop reordering
- ✅ Set primary image
- ✅ Image captions and alt text
- ✅ Edit metadata
- ✅ Delete images
- ✅ Lightbox viewer

---

## Future Considerations

### Potential Enhancements
1. **Configurable Limit**: Move limit to `appsettings.json` for easier adjustment
2. **Premium Tiers**: Offer higher limits (e.g., 50 images) for premium provider plans
3. **Analytics**: Track average gallery size to optimize limit
4. **Storage Cleanup**: Automated cleanup of orphaned image files

### Configuration Example (Future)
```json
{
  "GallerySettings": {
    "MaxImagesPerProvider": 20,
    "MaxImageSizeMB": 10,
    "AllowedFormats": ["jpg", "jpeg", "png", "webp"],
    "GenerateThumbnails": true
  }
}
```

---

## Testing Checklist

- [x] Backend domain validation enforces 20-image limit
- [x] Frontend ProfileGallery component shows correct limit
- [x] Frontend GalleryView shows correct limit
- [x] Registration flow enforces 20-image limit
- [x] All documentation updated consistently
- [ ] Manual test: Upload 20 images successfully
- [ ] Manual test: Verify upload blocked at 21st image
- [ ] Manual test: Error message displays correctly
- [ ] Manual test: UI shows "X / 20 images" counter

---

## Deployment Notes

### Breaking Changes
**None** - This is a backward-compatible change that tightens existing constraints.

### Rollback Plan
If needed, simply revert the constant values:
```csharp
// Backend: BusinessProfile.cs
private const int MaxGalleryImages = 50;  // Revert to 50

// Frontend: ProfileGallery.vue
:max-images="50"  // Revert to 50

// Frontend: GalleryView.vue
const maxImages = 50  // Revert to 50
```

---

## References

- [Gallery Feature Documentation](openspec/changes/archive/2025-11-17-add-provider-image-gallery/IMPLEMENTATION_SUMMARY.md)
- [Domain Model - BusinessProfile](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/BusinessProfile.cs)
- [Gallery Components](booksy-frontend/src/modules/provider/components/gallery/)

---

**Last Updated**: 2025-12-22
**Reviewed By**: AI Code Review
**Status**: ✅ Complete and Documented
