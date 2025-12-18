# Gallery Images Implementation

**Date**: 2025-11-26
**Status**: ✅ Implemented
**Components**: GalleryStep, OrganizationPreviewStep, OrganizationRegistrationFlow

---

## Overview

Implementation of gallery image upload, display, and restoration for the organization registration flow. Handles both local file storage during registration and server-side storage after provider creation.

---

## Architecture

### Data Flow

```
┌─────────────────────────────────────────────────────────┐
│                   Registration Flow                      │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  1. User uploads images                                  │
│     ├─> GalleryStep.vue                                 │
│     └─> Stored in composable: galleryImages[]           │
│                                                           │
│  2. On step completion                                   │
│     ├─> Images uploaded to /step-7/gallery              │
│     └─> API returns imageUrl, thumbnailUrl, mediumUrl   │
│                                                           │
│  3. On draft restoration                                 │
│     ├─> API returns galleryImages[]                     │
│     ├─> Map API format to component format              │
│     └─> Store in composable                              │
│                                                           │
│  4. Display in preview                                   │
│     └─> OrganizationPreviewStep reads from composable   │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

---

## API Response Format

### GET /progress Response

```json
{
  "galleryImages": [
    {
      "id": "aba30386-c728-4d60-a6e1-83203a00eb65",
      "imageUrl": "http://localhost:5010/uploads/providers/.../original.webp",
      "thumbnailUrl": "http://localhost:5010/uploads/providers/.../thumb.webp",
      "mediumUrl": "http://localhost:5010/uploads/providers/.../medium.webp",
      "displayOrder": 0,
      "isPrimary": false,
      "uploadedAt": "2025-11-26T11:09:36.599063+03:30"
    }
  ]
}
```

### Component Data Format

```typescript
interface GalleryImageData {
  id: string
  url: string              // Primary display URL (mapped from imageUrl)
  thumbnailUrl?: string    // Thumbnail version
  mediumUrl?: string       // Medium size version
  imageUrl?: string        // Original/large version
  altText?: string         // Alt text for accessibility
  displayOrder?: number    // Order in gallery
  isPrimary?: boolean      // Primary image flag
  uploadedAt?: string      // Upload timestamp
  file?: File             // File object (for new uploads only)
}
```

---

## Data Mapping

### API to Component Format

**Location**: `OrganizationRegistrationFlow.vue:540-550`

```typescript
registration.registrationData.value.galleryImages = draft.galleryImages.map((img: any) => ({
  id: img.id,
  url: img.imageUrl || img.mediumUrl || img.thumbnailUrl,  // ✅ Key mapping
  thumbnailUrl: img.thumbnailUrl,
  mediumUrl: img.mediumUrl,
  imageUrl: img.imageUrl,
  altText: `تصویر گالری ${img.displayOrder + 1}`,
  displayOrder: img.displayOrder,
  isPrimary: img.isPrimary,
  uploadedAt: img.uploadedAt,
}))
```

**Key Points**:
- API returns `imageUrl`, `thumbnailUrl`, `mediumUrl` as separate properties
- Component expects `url` property for display
- Fallback chain: `imageUrl` → `mediumUrl` → `thumbnailUrl`

---

## Component Implementation

### 1. GalleryStep.vue

**Purpose**: Upload and manage gallery images during registration

**Key Features**:
- Upload up to 20 images
- Local storage before provider creation
- Server upload after provider creation
- Delete images with confirmation
- Display uploaded images in grid

**Priority Loading Logic**:

```typescript
onMounted(async () => {
  // PRIORITY 1: Load from composable (during registration)
  const existingImages = registration.registrationData.value.galleryImages

  if (existingImages && existingImages.length > 0) {
    localGalleryImages.value = existingImages
    return  // ✅ Early return - don't fetch from store
  }

  // PRIORITY 2: Load from store (after provider creation)
  const providerId = currentProviderId.value

  if (providerId) {
    await galleryStore.fetchGalleryImages(providerId)
    // Map store format to component format...
  }
})
```

**Why Priority Matters**:
- During registration: Composable has the data, store might be empty
- After registration: Provider store has the data
- Early return prevents overwriting composable data with empty store data

---

### 2. OrganizationPreviewStep.vue

**Purpose**: Display all registration data for final review

**Gallery Display**:

```vue
<template>
  <div v-if="galleryImages.length > 0" class="gallery-preview">
    <div v-for="(image, index) in galleryImages" :key="index" class="gallery-thumbnail">
      <img :src="image.url" :alt="image.altText || 'تصویر گالری'" />
    </div>
  </div>
  <div v-else class="preview-empty">
    <i class="icon-alert-circle"></i>
    <span>هیچ تصویری اضافه نشده است</span>
  </div>
</template>

<script setup lang="ts">
const registration = useProviderRegistration()

const galleryImages = computed(() => {
  return registration.registrationData.value.galleryImages || []
})
</script>
```

**Styling**:
```scss
.gallery-preview {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 1rem;
}

.gallery-thumbnail {
  aspect-ratio: 1;
  border-radius: 8px;
  overflow: hidden;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }
}
```

---

### 3. OrganizationRegistrationFlow.vue

**Purpose**: Orchestrate the registration process

**Gallery Upload on Step 6**:

```typescript
// Step 6: Gallery upload
const galleryImages = registration.registrationData.value.galleryImages

if (galleryImages && galleryImages.length > 0) {
  // Filter for new File objects (not already uploaded)
  const filesToUpload = galleryImages
    .filter((img: any) => img.file instanceof File)
    .map((img: any) => img.file as File)

  if (filesToUpload.length > 0) {
    console.log(`📤 Uploading ${filesToUpload.length} image(s)...`)
    await providerRegistrationService.saveStep7Gallery(filesToUpload)
    toastService.success(`${filesToUpload.length} تصویر آپلود شد`)
  } else {
    console.log('✅ All images already uploaded')
    toastService.success('گالری ذخیره شد')
  }
}
```

**Gallery Restoration on Draft Load**:

```typescript
// Restore gallery images to composable
if (draft.galleryImages && draft.galleryImages.length > 0) {
  registration.registrationData.value.galleryImages = draft.galleryImages.map((img: any) => ({
    id: img.id,
    url: img.imageUrl || img.mediumUrl || img.thumbnailUrl,
    thumbnailUrl: img.thumbnailUrl,
    mediumUrl: img.mediumUrl,
    imageUrl: img.imageUrl,
    altText: `تصویر گالری ${img.displayOrder + 1}`,
    displayOrder: img.displayOrder,
    isPrimary: img.isPrimary,
    uploadedAt: img.uploadedAt,
  }))
  console.log('✅ Restored gallery images to composable:', draft.galleryImages.length)
}
```

---

## State Management

### Composable State

**File**: `useProviderRegistration.ts`

```typescript
interface RegistrationData {
  // ... other fields
  galleryImages: GalleryImageData[]
}

const registrationData = ref<RegistrationData>({
  // ... other fields
  galleryImages: [],
})

function setGalleryImages(images: GalleryImageData[]) {
  registrationData.value.galleryImages = images
}
```

**Why Composable**:
- ✅ Shared state across registration steps
- ✅ Persists during navigation between steps
- ✅ Single source of truth
- ✅ Easy to restore from API

---

## Upload Flow

### During Registration (No Provider ID)

```
1. User selects files in GalleryStep
   ↓
2. Files stored locally with temporary IDs
   ├─> Create object URLs for preview
   ├─> Store File objects in composable
   └─> Display thumbnails
   ↓
3. User proceeds to next step
   ↓
4. OrganizationRegistrationFlow uploads files
   ├─> Extract File objects from galleryImages
   ├─> Upload to /step-7/gallery
   └─> Server returns image URLs
   ↓
5. Continue to preview step
```

### After Provider Creation

```
1. User selects files in GalleryStep
   ↓
2. Immediately upload to server
   ├─> Provider ID available
   ├─> Upload to /providers/{id}/gallery
   └─> Server returns image data
   ↓
3. Update local state with server response
   ├─> Replace temporary IDs with server IDs
   ├─> Replace object URLs with server URLs
   └─> Save to composable
```

---

## Error Handling

### Upload Errors

```typescript
try {
  await providerRegistrationService.saveStep7Gallery(filesToUpload)
  toastService.success(`${filesToUpload.length} تصویر آپلود شد`)
} catch (error: any) {
  console.error('❌ Error uploading gallery:', error)
  error.value = error.message || 'خطا در آپلود تصاویر'
}
```

### Load Errors

```typescript
try {
  await galleryStore.fetchGalleryImages(providerId)
  // Process images...
} catch (err) {
  console.error('❌ GalleryStep: Error loading gallery:', err)
  // Don't show error - it's OK if provider doesn't have images yet
}
```

**Why Silent Failure**:
- Gallery is optional during registration
- Provider might not have uploaded images yet
- Don't block user with error messages

---

## Validation Rules

### File Validation

1. **Maximum Images**: 20 images per provider
2. **File Types**: JPG, PNG, WebP (handled by GalleryUpload component)
3. **File Size**: 10MB maximum per image (handled by server)

### Upload Validation

```typescript
const totalAfterUpload = localGalleryImages.value.length + files.length

if (totalAfterUpload > maxImages) {
  error.value = `شما نمی‌توانید بیش از ${maxImages} تصویر آپلود کنید`
  return
}
```

---

## Image Display

### Grid Layout

```scss
.gallery-preview {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 1rem;
}

@media (max-width: 640px) {
  .gallery-preview {
    grid-template-columns: repeat(auto-fill, minmax(100px, 1fr));
  }
}
```

### Thumbnail Sizing

- **Desktop**: 120px × 120px minimum
- **Mobile**: 100px × 100px minimum
- **Aspect Ratio**: 1:1 (square)
- **Object Fit**: Cover (fills container, may crop)

---

## Testing Scenarios

### Test 1: Upload During Registration

```
1. Start new registration
2. Complete steps 1-5
3. Navigate to Gallery step (step 6)
4. Upload 3 images
5. Click "Next"
6. Verify: Images show in preview step
7. Complete registration
8. Verify: Images uploaded to server
```

### Test 2: Resume Draft with Gallery

```
1. Start registration and upload 3 images
2. Leave page (draft saved)
3. Return to registration
4. Verify: Draft restored with 3 images
5. Navigate to Gallery step
6. Verify: 3 images displayed
7. Navigate to Preview step
8. Verify: 3 images displayed
```

### Test 3: Delete Image

```
1. Navigate to Gallery step with 3 images
2. Click delete button on first image
3. Confirm deletion
4. Verify: Image removed from list
5. Verify: Remaining images still displayed
6. Navigate to Preview
7. Verify: Only 2 images shown
```

### Test 4: Maximum Limit

```
1. Navigate to Gallery step
2. Upload 20 images (maximum)
3. Try to upload 1 more
4. Verify: Error message displayed
5. Verify: Upload prevented
```

---

## Common Issues & Solutions

### Issue 1: Images Not Displaying

**Symptoms**: Gallery step shows "No images uploaded"

**Causes**:
- Composable data not restored yet
- API response format mismatch
- Missing `url` property mapping

**Solution**:
```typescript
// Ensure mapping includes url property
url: img.imageUrl || img.mediumUrl || img.thumbnailUrl
```

---

### Issue 2: Images Uploaded Twice

**Symptoms**: Same images uploaded multiple times

**Causes**:
- Not filtering for File objects
- Including already-uploaded images

**Solution**:
```typescript
const filesToUpload = galleryImages
  .filter((img: any) => img.file instanceof File)
  .map((img: any) => img.file as File)
```

---

### Issue 3: Preview Shows Empty

**Symptoms**: Preview step shows "No images" despite upload

**Causes**:
- Preview reading from wrong data source
- Composable not updated

**Solution**:
```typescript
// Preview must read from composable
const galleryImages = computed(() => {
  return registration.registrationData.value.galleryImages || []
})
```

---

## Performance Considerations

### Image Loading

- **Lazy Loading**: Not implemented (all images loaded immediately)
- **Thumbnail Usage**: Use thumbnailUrl for list view when available
- **Caching**: Browser handles image caching automatically

### Upload Performance

- **Parallel Uploads**: Multiple files uploaded in single request
- **Progress Indication**: Visual progress bar during upload
- **Chunked Upload**: Not implemented (future enhancement for large files)

---

## Future Enhancements

### 1. Image Reordering

**Implementation**:
```typescript
function reorderImages(fromIndex: number, toIndex: number) {
  const images = [...localGalleryImages.value]
  const [removed] = images.splice(fromIndex, 1)
  images.splice(toIndex, 0, removed)

  // Update displayOrder
  images.forEach((img, index) => {
    img.displayOrder = index
  })

  localGalleryImages.value = images
  registration.setGalleryImages(images)
}
```

### 2. Image Cropping

**Implementation**:
- Add image cropper modal
- Allow user to crop before upload
- Save cropped version as primary image

### 3. Image Captions

**Current**: Caption field exists but not editable
**Enhancement**: Add caption input for each image

```vue
<template>
  <div class="image-card">
    <img :src="image.url" />
    <input
      v-model="image.caption"
      placeholder="توضیحات تصویر"
      @change="updateCaption(image.id, image.caption)"
    />
  </div>
</template>
```

### 4. Primary Image Selection

**Implementation**:
```typescript
function setPrimaryImage(imageId: string) {
  localGalleryImages.value.forEach(img => {
    img.isPrimary = img.id === imageId
  })
  registration.setGalleryImages(localGalleryImages.value)
}
```

---

## Related Documentation

- [FIX_REGISTRATION_STEP_DATA_SYNC.md](./FIX_REGISTRATION_STEP_DATA_SYNC.md) - Data sync patterns
- [VALIDATION_ERROR_HANDLING.md](./VALIDATION_ERROR_HANDLING.md) - Error handling
- [OWNER_NAMES_IMPLEMENTATION_SUMMARY.md](./OWNER_NAMES_IMPLEMENTATION_SUMMARY.md) - Other registration fields

---

## Debugging

### Console Logs

Gallery step logs use emojis for easy filtering:

```typescript
console.log('🖼️ GalleryStep onMounted: Checking for existing images')
console.log('✅ GalleryStep: Loading images from composable')
console.log('❌ GalleryStep: Error loading gallery:', err)
console.log('📤 Uploading 3 image(s) to backend...')
```

### Inspection Points

1. **Composable State**:
   ```javascript
   // In browser console
   $vm.registration.registrationData.value.galleryImages
   ```

2. **Local Component State**:
   ```javascript
   // In browser console
   $vm.localGalleryImages
   ```

3. **Network Requests**:
   - Check Network tab for `/step-7/gallery` uploads
   - Verify FormData contains File objects
   - Check response for imageUrl values

---

**Status**: ✅ Production Ready
**Last Updated**: 2025-11-26
**Maintained By**: Development Team
