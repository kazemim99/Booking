# Implementation Summary - Image URL Generation & UX Improvements

## Overview
Implemented proper image URL generation in the backend for GET operations and completed multiple UX improvements for business hours management and gallery features.

---

## 1. Backend: Dynamic Image URL Generation

### Problem
- Backend was storing relative paths (`uploads/providers/.../image.webp`)
- Frontend was receiving incorrect URLs from different ports
- Images weren't displaying after page reload

### Solution
Created a URL service that generates complete URLs based on HTTP context for **GET/fetch operations only**.

**IMPORTANT:** Upload operations return relative paths as-is. Only fetch/query operations convert to absolute URLs.

---

### Files Created

#### `Booksy.ServiceCatalog.Application/Abstractions/IUrlService.cs`
```csharp
namespace Booksy.ServiceCatalog.Application.Abstractions;

/// <summary>
/// Service for generating URLs based on the current HTTP context
/// </summary>
public interface IUrlService
{
    /// <summary>
    /// Gets the base URL of the current request (e.g., http://localhost:5010/api)
    /// </summary>
    string GetBaseUrl();

    /// <summary>
    /// Converts a relative path to an absolute URL using the current request's base URL
    /// </summary>
    /// <param name="relativePath">Relative path (e.g., uploads/providers/xxx/image.webp)</param>
    /// <returns>Absolute URL (e.g., http://localhost:5010/api/uploads/providers/xxx/image.webp)</returns>
    string ToAbsoluteUrl(string relativePath);
}
```

#### `Booksy.ServiceCatalog.Infrastructure/Services/UrlService.cs`
```csharp
using Booksy.ServiceCatalog.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Booksy.ServiceCatalog.Infrastructure.Services;

/// <summary>
/// Service for generating URLs based on the current HTTP context
/// </summary>
public sealed class UrlService : IUrlService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UrlService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return string.Empty;
        }

        // Build the base URL: {scheme}://{host}{pathBase}
        // Example: http://localhost:5010/api
        return $"{request.Scheme}://{request.Host}{request.PathBase}";
    }

    public string ToAbsoluteUrl(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            return relativePath;
        }

        // Ensure the path starts with a slash
        var path = relativePath.StartsWith('/') ? relativePath : $"/{relativePath}";

        return $"{baseUrl}{path}";
    }
}
```

---

### Files Modified

#### `ServiceCatalogInfrastructureExtensions.cs`
**Location:** `Booksy.ServiceCatalog.Infrastructure/DependencyInjection/ServiceCatalogInfrastructureExtensions.cs`

**Changes:**
```csharp
// Added at top of file
using Booksy.ServiceCatalog.Application.Abstractions;
using Booksy.ServiceCatalog.Infrastructure.Services;

// In AddServiceCatalogInfrastructure method:
public static IServiceCollection AddServiceCatalogInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // HTTP Context Accessor (needed for URL generation)
    services.AddHttpContextAccessor();

    // ... other services ...

    // Register URL service
    services.AddScoped<IUrlService, UrlService>();
```

---

#### `GetGalleryImagesQueryHandler.cs` ✅ USES URL SERVICE
**Location:** `Booksy.ServiceCatalog.Application/Queries/Provider/GetGalleryImages/GetGalleryImagesQueryHandler.cs`

**Changes:**
```csharp
using Booksy.ServiceCatalog.Application.Abstractions;

public sealed class GetGalleryImagesQueryHandler
    : IQueryHandler<GetGalleryImagesQuery, List<GalleryImageDto>>
{
    private readonly IProviderReadRepository _providerRepository;
    private readonly IUrlService _urlService;  // ✅ Added

    public GetGalleryImagesQueryHandler(
        IProviderReadRepository providerRepository,
        IUrlService urlService)  // ✅ Injected
    {
        _providerRepository = providerRepository;
        _urlService = urlService;
    }

    public async Task<List<GalleryImageDto>> Handle(
        GetGalleryImagesQuery request,
        CancellationToken cancellationToken)
    {
        // ... fetch provider ...

        return provider.Profile.GalleryImages
            .Where(img => img.IsActive)
            .OrderByDescending(img => img.IsPrimary)
            .ThenBy(img => img.DisplayOrder)
            .Select(img => new GalleryImageDto
            {
                Id = img.Id,
                // ✅ Convert to absolute URLs using URL service
                ThumbnailUrl = _urlService.ToAbsoluteUrl(img.ThumbnailUrl),
                MediumUrl = _urlService.ToAbsoluteUrl(img.MediumUrl),
                OriginalUrl = _urlService.ToAbsoluteUrl(img.ImageUrl),
                DisplayOrder = img.DisplayOrder,
                Caption = img.Caption,
                AltText = img.AltText,
                UploadedAt = img.UploadedAt,
                IsActive = img.IsActive,
                IsPrimary = img.IsPrimary
            })
            .ToList();
    }
}
```

---

#### `UploadGalleryImagesCommandHandler.cs` ❌ DOES NOT USE URL SERVICE
**Location:** `Booksy.ServiceCatalog.Application/Commands/Provider/UploadGalleryImages/UploadGalleryImagesCommandHandler.cs`

**IMPORTANT: NO URL SERVICE INJECTION HERE!**

**Final State:**
```csharp
// NO IUrlService import or injection!

public sealed class UploadGalleryImagesCommandHandler
    : ICommandHandler<UploadGalleryImagesCommand, List<GalleryImageDto>>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    // ❌ NO _urlService field!

    public UploadGalleryImagesCommandHandler(
        IProviderWriteRepository providerRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork)
        // ❌ NO urlService parameter!
    {
        _providerRepository = providerRepository;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<GalleryImageDto>> Handle(...)
    {
        // ... upload logic ...

        // ❌ Return relative paths as stored - NO URL conversion!
        uploadedImages.Add(new GalleryImageDto
        {
            Id = galleryImage.Id,
            ThumbnailUrl = galleryImage.ThumbnailUrl,  // Relative path
            MediumUrl = galleryImage.MediumUrl,         // Relative path
            OriginalUrl = galleryImage.ImageUrl,        // Relative path
            DisplayOrder = galleryImage.DisplayOrder,
            Caption = galleryImage.Caption,
            AltText = galleryImage.AltText,
            UploadedAt = galleryImage.UploadedAt,
            IsActive = galleryImage.IsActive,
            IsPrimary = galleryImage.IsPrimary
        });

        return uploadedImages;
    }
}
```

---

### Backend Result Summary

| Operation | Returns | URL Service Used? |
|-----------|---------|-------------------|
| **Upload** (POST) | Relative paths (`uploads/providers/.../image.webp`) | ❌ NO |
| **Fetch** (GET) | Absolute URLs (`http://localhost:5010/api/uploads/...`) | ✅ YES |

---

## 2. Frontend: Gallery Service Status

### Current State
**File:** `booksy-frontend/src/modules/provider/services/gallery.service.ts`

The frontend URL normalization has been **REMOVED** in the current implementation:

```typescript
private mapGalleryImageDates = (image: any): GalleryImage => {
  return {
    ...image,
    uploadedAt: new Date(image.uploadedAt),
    // Backend returns complete URLs, use them as-is
    originalUrl: image.originalUrl,  // No normalization
    largeUrl: image.largeUrl,
    mediumUrl: image.mediumUrl,
    smallUrl: image.smallUrl,
    thumbnailUrl: image.thumbnailUrl,
    isActive: image.isActive ?? true,
  }
}
```

### ⚠️ POTENTIAL ISSUE

Since **upload returns relative paths** but **frontend expects absolute URLs**, you may need to:

**Option A:** Restore frontend URL normalization for upload responses
```typescript
private normalizeImageUrl(url: string): string {
  if (!url) return url

  const baseUrl = 'http://localhost:5010/api'  // or from config
  const path = url.startsWith('/') ? url : `/${url}`

  return `${baseUrl}${path}`
}

// Use only for upload response
const newImages = uploadedImages.map((img) => ({
  ...img,
  url: this.normalizeImageUrl(img.mediumUrl || img.originalUrl),
  thumbnailUrl: this.normalizeImageUrl(img.thumbnailUrl)
}))
```

**Option B:** Refresh gallery data after upload (recommended)
```typescript
async function handleUpload(files: File[]) {
  // Upload images
  const uploadedImages = await galleryStore.uploadImages(providerId, files)

  // Immediately refresh to get absolute URLs from GET endpoint
  await galleryStore.fetchGalleryImages(providerId)
}
```

---

## 3. UX Improvements: Business Hours Management

### WorkingHoursStepNew.vue
**Location:** `booksy-frontend/src/modules/provider/components/registration/steps/WorkingHoursStepNew.vue`

**Changes:**
- Default hours: 10:00 - 22:00 (was: Closed)
- Default break: 13:00 - 15:00
- All days open by default (including Friday)
- Added data sorting to handle unsorted API responses

```typescript
const initializeSchedule = (): DayHours[] => {
  if (props.modelValue && props.modelValue.length === 7) {
    // Sort by dayOfWeek to ensure correct order (0=Saturday to 6=Friday)
    return [...props.modelValue].sort((a, b) => a.dayOfWeek - b.dayOfWeek)
  }

  // Default schedule (all days open)
  return weekDays.map((_, index) => ({
    dayOfWeek: index,
    isOpen: true,
    openTime: { hours: 10, minutes: 0 },
    closeTime: { hours: 22, minutes: 0 },
    breaks: [{
      id: '1',
      start: { hours: 13, minutes: 0 },
      end: { hours: 15, minutes: 0 },
    }],
  }))
}
```

---

### DayScheduleModal.vue
**Location:** `booksy-frontend/src/shared/components/schedule/DayScheduleModal.vue`

**Added Validations:**
1. End time > Start time for business hours
2. Break end time > Break start time
3. Breaks must be within business hours

```typescript
const handleSave = () => {
  const timeToMinutes = (time: string): number => {
    const [hours, minutes] = time.split(':').map(Number)
    return hours * 60 + minutes
  }

  // Validate business hours
  const startMinutes = timeToMinutes(localData.value.startTime)
  const endMinutes = timeToMinutes(localData.value.endTime)

  if (endMinutes <= startMinutes) {
    alert('ساعت پایان باید بعد از ساعت شروع باشد')
    return
  }

  // Validate breaks
  if (localData.value.breaks && localData.value.breaks.length > 0) {
    for (let i = 0; i < localData.value.breaks.length; i++) {
      const breakItem = localData.value.breaks[i]
      const breakStartMinutes = timeToMinutes(breakItem.start)
      const breakEndMinutes = timeToMinutes(breakItem.end)

      if (breakEndMinutes <= breakStartMinutes) {
        alert(`ساعت پایان استراحت ${i + 1} باید بعد از ساعت شروع آن باشد`)
        return
      }

      // Validate break is within business hours
      if (breakStartMinutes < startMinutes || breakEndMinutes > endMinutes) {
        alert(`استراحت ${i + 1} باید در بازه ساعات کاری باشد`)
        return
      }
    }
  }

  emit('save', JSON.parse(JSON.stringify(localData.value)))
  emit('close')
}
```

---

### DayScheduleEditor.vue
**Location:** `booksy-frontend/src/shared/components/schedule/DayScheduleEditor.vue`

**Major UX Changes:**
1. **Moved Toggle:** Day status toggle moved from modal to day card
2. **Copy Modal:** Changed from "copy to all" to selective day copying
   - Shows modal with checkboxes for each day
   - Pre-selects all open days (except source)
   - Disables closed days and source day

```typescript
// Toggle in day card
<div class="day-name-section">
  <span class="day-name">{{ weekDays[index] }}</span>
  <label class="toggle-switch">
    <input type="checkbox" v-model="day.isOpen" @change="handleToggleDay(index)" />
    <span class="toggle-slider"></span>
  </label>
</div>

// Copy modal logic
const copyToAll = (sourceIndex: number) => {
  copySourceIndex.value = sourceIndex
  copySourceDayName.value = props.weekDays[sourceIndex]
  // Pre-select all open days except the source day
  selectedDaysToCopy.value = props.weekDays
    .map((_, index) => index)
    .filter(index => index !== sourceIndex && localSchedule.value[index].isOpen)
  copyModalOpen.value = true
}
```

---

## 4. Gallery Features

### GalleryStep.vue
**Location:** `booksy-frontend/src/modules/provider/components/registration/steps/GalleryStep.vue`

**Improvements:**
- Maximum images: 20 (was: 50)
- Added upload limit validation
- Added progress bar with animation
- Shows remaining slots counter

```typescript
const maxImages = 20
const remainingSlots = computed(() => maxImages - localGalleryImages.value.length)

async function handleUpload(files: File[]) {
  const totalAfterUpload = localGalleryImages.value.length + files.length
  if (totalAfterUpload > maxImages) {
    error.value = `شما نمی‌توانید بیش از ${maxImages} تصویر آپلود کنید. در حال حاضر ${localGalleryImages.value.length} تصویر دارید و ${remainingSlots.value} جای خالی باقی مانده است.`
    return
  }

  // Upload with progress
  isUploading.value = true
  uploadProgress.value = 0

  const progressInterval = setInterval(() => {
    if (uploadProgress.value < 90) {
      uploadProgress.value += 10
    }
  }, 200)

  const uploadedImages = await galleryStore.uploadImages(providerId, files)

  clearInterval(progressInterval)
  uploadProgress.value = 100
}
```

---

### GalleryUpload.vue
**Location:** `booksy-frontend/src/modules/provider/components/gallery/GalleryUpload.vue`

**Features:**
- Displays current count and remaining slots
- Visual feedback when limit reached
- Disabled state when no slots available
- Upload progress indicator

```vue
<p v-if="currentCount > 0" class="upload-count">
  {{ currentCount }} از {{ totalLimit }} تصویر آپلود شده
  <span v-if="maxImages > 0" class="remaining">({{ maxImages }} جای خالی باقی مانده)</span>
  <span v-else class="limit-reached">حداکثر تعداد رسیده است</span>
</p>
```

---

## Testing Checklist

### Backend
- [ ] Build solution successfully
- [ ] Verify IUrlService is registered in DI
- [ ] Test gallery **fetch** returns absolute URLs (`http://localhost:5010/api/uploads/...`)
- [ ] Test gallery **upload** returns relative paths (`uploads/providers/.../image.webp`)
- [ ] Verify fetch URLs work in browser

### Frontend
- [ ] Upload images - may show relative paths initially
- [ ] **Refresh/reload page** - should display correctly with absolute URLs from fetch
- [ ] Check browser DevTools Network tab
  - Upload response: relative paths
  - Fetch response: absolute URLs
- [ ] Test business hours with validation
- [ ] Test selective day copying
- [ ] Test gallery upload limit (20 images)

### Integration Options
**Choose one:**
1. **Option A:** Add frontend URL normalization for upload responses
2. **Option B:** Refresh gallery after upload to get absolute URLs (recommended)

---

## Known Issue & Solution

### Issue
Upload returns relative paths, but frontend removed URL normalization.

### Solution Options

#### Option 1: Refresh After Upload (Recommended)
```typescript
// In GalleryStep.vue
async function handleUpload(files: File[]) {
  // ... upload logic ...
  const uploadedImages = await galleryStore.uploadImages(providerId, files)

  // Immediately refresh to get absolute URLs
  await galleryStore.fetchGalleryImages(providerId)

  // Update local images from store
  const storeImages = galleryStore.galleryImages.map(img => ({
    id: img.id,
    url: img.mediumUrl || img.originalUrl,  // Now absolute!
    thumbnailUrl: img.thumbnailUrl,
    displayOrder: img.displayOrder,
    caption: img.caption,
    altText: img.altText,
  }))

  localGalleryImages.value = storeImages
}
```

#### Option 2: Restore Frontend Normalization
Add back the `normalizeImageUrl` method in `gallery.service.ts` but only use it for mapping upload responses.

---

## Migration Notes

No database migration needed. The `UrlService.ToAbsoluteUrl()` method handles:
- Relative paths: `uploads/providers/.../image.webp`
- Paths with slash: `/uploads/providers/.../image.webp`

Both become: `http://localhost:5010/api/uploads/providers/.../image.webp`

---

## Architecture Summary

```
┌─────────────────────────────────────────────────────────┐
│                    BACKEND BEHAVIOR                      │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  POST /gallery (Upload)                                  │
│  └─> Returns: uploads/providers/.../image.webp          │
│       (Relative path, as stored in DB)                   │
│       ❌ NO URL conversion                               │
│                                                          │
│  GET /gallery (Fetch)                                    │
│  └─> Returns: http://localhost:5010/api/uploads/...     │
│       (Absolute URL via IUrlService)                     │
│       ✅ URL conversion applied                          │
│                                                          │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                   FRONTEND BEHAVIOR                      │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  After Upload:                                           │
│  └─> Receives relative paths                            │
│       Option A: Normalize URLs manually                  │
│       Option B: Refresh from GET endpoint ✅             │
│                                                          │
│  After Fetch/Reload:                                     │
│  └─> Receives absolute URLs from backend                │
│       Use as-is, no normalization needed                 │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## Files Changed Summary

### Backend Files Created
1. `Booksy.ServiceCatalog.Application/Abstractions/IUrlService.cs`
2. `Booksy.ServiceCatalog.Infrastructure/Services/UrlService.cs`

### Backend Files Modified
1. `ServiceCatalogInfrastructureExtensions.cs` - DI registration
2. `GetGalleryImagesQueryHandler.cs` - ✅ Uses IUrlService for GET
3. `UploadGalleryImagesCommandHandler.cs` - ❌ Does NOT use IUrlService

### Frontend Files Modified
1. `gallery.service.ts` - URL normalization removed
2. `WorkingHoursStepNew.vue` - Default hours & sorting
3. `DayScheduleModal.vue` - Time validation
4. `DayScheduleEditor.vue` - Toggle & copy modal
5. `GalleryStep.vue` - Upload limits & progress
6. `GalleryUpload.vue` - Visual feedback

### Documentation Created
1. `IMPLEMENTATION_SUMMARY.md` - This file

---

## Next Session Tasks

1. **Decide Frontend Strategy:**
   - [ ] Implement Option A (normalize upload response) OR
   - [ ] Implement Option B (refresh after upload) ✅ Recommended

2. **Testing:**
   - [ ] Build backend and verify no errors
   - [ ] Test upload returns relative paths
   - [ ] Test fetch returns absolute URLs
   - [ ] Test images display correctly after refresh

3. **Cleanup:**
   - [ ] Update IMPLEMENTATION_SUMMARY.md with final solution
   - [ ] Remove any unused code
   - [ ] Test all UX improvements (hours, gallery, validation)
