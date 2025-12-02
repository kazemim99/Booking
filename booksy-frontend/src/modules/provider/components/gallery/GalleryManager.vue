<template>
  <div class="gallery-manager" dir="rtl">
    <!-- Header -->
    <div class="gallery-header">
      <h2 class="gallery-title">گالری تصاویر</h2>
      <p class="gallery-subtitle">
        تصاویر نمونه کار خود را آپلود کنید تا مشتریان بتوانند کیفیت خدمات شما را ببینند
      </p>
    </div>

    <!-- Gallery Upload Area -->
    <div class="gallery-section">
      <GalleryUpload
        :max-images="remainingSlots"
        :current-count="images.length"
        :total-limit="props.maxImages"
        :is-uploading="isUploading"
        :upload-progress="uploadProgress"
        @upload="handleGalleryUpload"
      />

      <!-- Image Preview Grid -->
      <div v-if="images.length > 0" class="gallery-preview">
        <div class="gallery-preview-header">
          <h3 class="preview-title">تصاویر آپلود شده</h3>
          <p class="preview-count">{{ images.length }} تصویر</p>
        </div>

        <div class="image-grid">
          <div
            v-for="image in sortedImages"
            :key="image.id"
            class="image-card"
          >
            <div class="image-wrapper">
              <img
                :src="image.thumbnailUrl"
                :alt="image.altText || image.caption || 'تصویر گالری'"
                class="image"
                @click="handleViewImage(image)"
              />

              <!-- Primary Badge -->
              <div v-if="image.isPrimary" class="primary-badge" title="تصویر اصلی">
                <svg viewBox="0 0 20 20" fill="currentColor">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                </svg>
              </div>

              <!-- Action Buttons Overlay -->
              <div class="image-overlay">
                <button
                  v-if="!image.isPrimary"
                  @click.stop="handleSetPrimary(image)"
                  class="overlay-btn btn-primary"
                  title="تنظیم به عنوان تصویر اصلی"
                >
                  <svg viewBox="0 0 20 20" fill="currentColor">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                </button>
                <button
                  @click.stop="handleEditMetadata(image)"
                  class="overlay-btn"
                  title="ویرایش"
                >
                  <svg viewBox="0 0 20 20" fill="currentColor">
                    <path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" />
                  </svg>
                </button>
                <button
                  type="button"
                  class="overlay-btn btn-danger"
                  :disabled="isDeleting"
                  @click.stop="handleDeleteImage(image)"
                  title="حذف"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fill-rule="evenodd"
                      d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193V3.75A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z"
                      clip-rule="evenodd"
                    />
                  </svg>
                </button>
              </div>
            </div>
            <div v-if="image.caption" class="image-caption">{{ image.caption }}</div>
            <div v-else class="image-caption placeholder">بدون عنوان</div>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else-if="!loading" class="empty-state">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          class="empty-icon"
        >
          <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
          <circle cx="8.5" cy="8.5" r="1.5"></circle>
          <polyline points="21 15 16 10 5 21"></polyline>
        </svg>
        <p class="empty-text">هنوز تصویری آپلود نکرده‌اید</p>
        <p class="empty-hint">تصاویر خود را با کشیدن و رها کردن یا کلیک روی دکمه بالا آپلود کنید</p>
      </div>
    </div>

    <!-- Edit Metadata Modal -->
    <Teleport to="body">
      <div v-if="editingImage" class="modal-overlay" @click="closeEditModal">
        <div class="modal-content" @click.stop>
          <div class="modal-header">
            <h3>ویرایش اطلاعات تصویر</h3>
            <button @click="closeEditModal" class="modal-close" type="button">×</button>
          </div>

          <div class="modal-body">
            <!-- Image Preview -->
            <div class="edit-preview">
              <img :src="editingImage.mediumUrl" :alt="editingImage.altText || 'پیش‌نمایش'" />
            </div>

            <!-- Caption Field -->
            <div class="form-group">
              <label for="caption">عنوان تصویر</label>
              <input
                id="caption"
                v-model="editForm.caption"
                type="text"
                class="form-control"
                placeholder="توضیح کوتاهی درباره تصویر..."
                maxlength="200"
              />
              <small class="form-text">{{ editForm.caption?.length || 0 }} / 200</small>
            </div>

            <!-- Alt Text Field -->
            <div class="form-group">
              <label for="altText">متن جایگزین (Alt Text)</label>
              <input
                id="altText"
                v-model="editForm.altText"
                type="text"
                class="form-control"
                placeholder="توضیح تصویر برای موتورهای جستجو و نابینایان..."
                maxlength="150"
              />
              <small class="form-text">{{ editForm.altText?.length || 0 }} / 150</small>
            </div>
          </div>

          <div class="modal-footer">
            <button @click="saveMetadata" :disabled="savingMetadata" class="btn btn-primary">
              <span v-if="savingMetadata">در حال ذخیره...</span>
              <span v-else>ذخیره تغییرات</span>
            </button>
            <button @click="closeEditModal" :disabled="savingMetadata" class="btn btn-secondary">
              انصراف
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Delete Confirmation Modal -->
    <Teleport to="body">
      <div v-if="deletingImage" class="modal-overlay" @click="closeDeleteModal">
        <div class="modal-content modal-sm" @click.stop>
          <div class="modal-header">
            <h3>حذف تصویر</h3>
            <button @click="closeDeleteModal" class="modal-close" type="button">×</button>
          </div>

          <div class="modal-body">
            <div class="delete-preview">
              <img :src="deletingImage.thumbnailUrl" :alt="deletingImage.altText || 'تصویر'" />
            </div>
            <p class="text-center">آیا از حذف این تصویر اطمینان دارید؟</p>
            <p v-if="deletingImage.caption" class="text-muted text-center">
              "{{ deletingImage.caption }}"
            </p>
          </div>

          <div class="modal-footer">
            <button @click="confirmDelete" :disabled="isDeleting" class="btn btn-danger">
              <span v-if="isDeleting">در حال حذف...</span>
              <span v-else">بله، حذف شود</span>
            </button>
            <button @click="closeDeleteModal" :disabled="isDeleting" class="btn btn-secondary">
              انصراف
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Image Viewer Modal -->
    <Teleport to="body">
      <div v-if="viewingImage" class="modal-overlay modal-viewer" @click="closeViewModal">
        <div class="viewer-content" @click.stop>
          <button @click="closeViewModal" class="viewer-close" type="button">×</button>
          <img :src="viewingImage.originalUrl" :alt="viewingImage.altText || viewingImage.caption || 'تصویر'" />
          <div v-if="viewingImage.caption" class="viewer-caption">
            {{ viewingImage.caption }}
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { galleryService } from '@/modules/provider/services/gallery.service'
import type { GalleryImage, ImageMetadata } from '@/modules/provider/types/gallery.types'
import GalleryUpload from './GalleryUpload.vue'
import { toastService } from '@/core/services/toast.service'

// ============================================================================
// Props & Emits
// ============================================================================

interface Props {
  maxImages?: number
  maxSizeMB?: number
}

const props = withDefaults(defineProps<Props>(), {
  maxImages: 20,
  maxSizeMB: 5,
})

const emit = defineEmits<{
  (e: 'images-updated', images: GalleryImage[]): void
  (e: 'upload-success', images: GalleryImage[]): void
  (e: 'upload-error', error: Error): void
}>()

// ============================================================================
// Store & State
// ============================================================================

const providerStore = useProviderStore()
const providerId = computed(() => providerStore.currentProvider?.id || '')

// Gallery State
const images = ref<GalleryImage[]>([])
const loading = ref(false)

// Upload State
const isUploading = ref(false)
const uploadProgress = ref(0)

// Edit State
const editingImage = ref<GalleryImage | null>(null)
const editForm = ref<ImageMetadata>({
  caption: '',
  altText: '',
})
const savingMetadata = ref(false)

// Delete State
const deletingImage = ref<GalleryImage | null>(null)
const isDeleting = ref(false)

// View State
const viewingImage = ref<GalleryImage | null>(null)

// ============================================================================
// Computed
// ============================================================================

const sortedImages = computed(() => {
  return [...images.value].sort((a, b) => a.displayOrder - b.displayOrder)
})

const remainingSlots = computed(() => {
  return props.maxImages - images.value.length
})

// ============================================================================
// Lifecycle
// ============================================================================

onMounted(() => {
  loadImages()
})

watch(() => providerId.value, () => {
  if (providerId.value) {
    loadImages()
  }
})

// ============================================================================
// Gallery Operations
// ============================================================================

async function loadImages(): Promise<void> {
  if (!providerId.value) {
    toastService.error('شناسه ارائه‌دهنده یافت نشد')
    return
  }

  loading.value = true

  try {
    images.value = await galleryService.getGalleryImages(providerId.value)
    console.log(`[GalleryManager] Loaded ${images.value.length} images`)
  } catch (err) {
    console.error('[GalleryManager] Error loading images:', err)
    toastService.error('خطا در بارگذاری تصاویر')
  } finally {
    loading.value = false
  }
}

// ============================================================================
// Upload Operations
// ============================================================================

async function handleGalleryUpload(files: File[]): Promise<void> {
  if (!providerId.value || files.length === 0) return

  isUploading.value = true
  uploadProgress.value = 0

  try {
    const uploadedImages = await galleryService.uploadImages(
      providerId.value,
      files,
      (progressEvent) => {
        // Calculate progress from axios progress event
        if (progressEvent.total) {
          uploadProgress.value = Math.round((progressEvent.loaded / progressEvent.total) * 100)
        }
      }
    )

    console.log(`[GalleryManager] Uploaded ${uploadedImages.length} images`)

    // Reload gallery (cache is disabled for getGalleryImages)
    await loadImages()

    toastService.success(`${uploadedImages.length} تصویر با موفقیت آپلود شد`)
    emit('upload-success', uploadedImages)
    emit('images-updated', images.value)

    // Reset upload state
    uploadProgress.value = 0
  } catch (err) {
    console.error('[GalleryManager] Error uploading images:', err)
    toastService.error('خطا در آپلود تصاویر')
    emit('upload-error', err as Error)
  } finally {
    isUploading.value = false
  }
}

// ============================================================================
// Metadata Operations
// ============================================================================

function handleEditMetadata(image: GalleryImage): void {
  editingImage.value = image
  editForm.value = {
    caption: image.caption || '',
    altText: image.altText || '',
  }
}

async function saveMetadata(): Promise<void> {
  if (!editingImage.value || !providerId.value) return

  savingMetadata.value = true

  try {
    await galleryService.updateImageMetadata(
      providerId.value,
      editingImage.value.id,
      editForm.value
    )

    console.log(`[GalleryManager] Updated metadata for image ${editingImage.value.id}`)

    // Update local state
    const index = images.value.findIndex(img => img.id === editingImage.value!.id)
    if (index !== -1) {
      images.value[index] = {
        ...images.value[index],
        caption: editForm.value.caption,
        altText: editForm.value.altText,
      }
    }

    toastService.success('اطلاعات تصویر با موفقیت ذخیره شد')
    emit('images-updated', images.value)
    closeEditModal()
  } catch (err) {
    console.error('[GalleryManager] Error updating metadata:', err)
    toastService.error('خطا در ذخیره تغییرات')
  } finally {
    savingMetadata.value = false
  }
}

function closeEditModal(): void {
  editingImage.value = null
  editForm.value = { caption: '', altText: '' }
}

// ============================================================================
// Delete Operations
// ============================================================================

function handleDeleteImage(image: GalleryImage): void {
  deletingImage.value = image
}

async function confirmDelete(): Promise<void> {
  if (!deletingImage.value || !providerId.value) return

  isDeleting.value = true

  try {
    await galleryService.deleteImage(providerId.value, deletingImage.value.id)

    console.log(`[GalleryManager] Deleted image ${deletingImage.value.id}`)

    // Remove from local state
    images.value = images.value.filter(img => img.id !== deletingImage.value!.id)

    toastService.success('تصویر با موفقیت حذف شد')
    emit('images-updated', images.value)
    closeDeleteModal()
  } catch (err) {
    console.error('[GalleryManager] Error deleting image:', err)
    toastService.error('خطا در حذف تصویر')
  } finally {
    isDeleting.value = false
  }
}

function closeDeleteModal(): void {
  deletingImage.value = null
}

// ============================================================================
// View Operations
// ============================================================================

function handleViewImage(image: GalleryImage): void {
  viewingImage.value = image
}

function closeViewModal(): void {
  viewingImage.value = null
}

// ============================================================================
// Set Primary Image
// ============================================================================

async function handleSetPrimary(image: GalleryImage): Promise<void> {
  if (!providerId.value) return

  try {
    await galleryService.setPrimaryImage(providerId.value, image.id)

    console.log(`[GalleryManager] Set image ${image.id} as primary`)

    // Update local state - unset other images and set this one as primary
    images.value = images.value.map(img => ({
      ...img,
      isPrimary: img.id === image.id
    }))

    toastService.success('تصویر اصلی تنظیم شد')
    emit('images-updated', images.value)
  } catch (err) {
    console.error('[GalleryManager] Error setting primary image:', err)
    toastService.error('خطا در تنظیم تصویر اصلی')
  }
}
</script>

<style scoped>
.gallery-manager {
  padding: 1.5rem;
  direction: rtl;
}

/* Header */
.gallery-header {
  margin-bottom: 2rem;
}

.gallery-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin: 0 0 0.5rem 0;
}

.gallery-subtitle {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

/* Gallery Section */
.gallery-section {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

/* Gallery Preview */
.gallery-preview {
  margin-top: 2rem;
}

.gallery-preview-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.preview-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
}

.preview-count {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

/* Image Grid */
.image-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 1.5rem;
}

.image-card {
  background: white;
  border-radius: 0.5rem;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  transition: all 0.2s;
}

.image-card:hover {
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

/* Image Wrapper */
.image-wrapper {
  position: relative;
  width: 100%;
  aspect-ratio: 4 / 3;
  overflow: hidden;
  background-color: #f3f4f6;
}

.image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  cursor: pointer;
  transition: transform 0.3s;
}

.image-wrapper:hover .image {
  transform: scale(1.05);
}

/* Primary Badge */
.primary-badge {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
  width: 2rem;
  height: 2rem;
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 8px rgba(251, 191, 36, 0.5);
  z-index: 10;
}

.primary-badge svg {
  width: 1.25rem;
  height: 1.25rem;
  color: white;
}

/* Image Overlay */
.image-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  opacity: 0;
  transition: opacity 0.2s;
}

.image-wrapper:hover .image-overlay {
  opacity: 1;
}

.overlay-btn {
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 50%;
  background: white;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.overlay-btn:hover {
  transform: scale(1.1);
}

.overlay-btn svg {
  width: 1.125rem;
  height: 1.125rem;
  color: #374151;
}

.overlay-btn.btn-primary {
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
}

.overlay-btn.btn-primary svg {
  color: white;
}

.overlay-btn.btn-danger:hover {
  background: #ef4444;
}

.overlay-btn.btn-danger:hover svg {
  color: white;
}

/* Image Caption */
.image-caption {
  padding: 0.75rem;
  font-size: 0.875rem;
  color: #374151;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.image-caption.placeholder {
  color: #9ca3af;
  font-style: italic;
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 3rem 1rem;
}

.empty-icon {
  width: 4rem;
  height: 4rem;
  color: #d1d5db;
  margin: 0 auto 1rem;
}

.empty-text {
  font-size: 1rem;
  font-weight: 500;
  color: #374151;
  margin: 0 0 0.5rem 0;
}

.empty-hint {
  font-size: 0.875rem;
  color: #9ca3af;
  margin: 0;
}

/* Modals */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  background: white;
  border-radius: 0.75rem;
  max-width: 500px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 25px rgba(0, 0, 0, 0.15);
}

.modal-content.modal-sm {
  max-width: 400px;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-header h3 {
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
}

.modal-close {
  width: 2rem;
  height: 2rem;
  border: none;
  background: transparent;
  font-size: 1.5rem;
  color: #6b7280;
  cursor: pointer;
  transition: color 0.2s;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal-close:hover {
  color: #111827;
}

.modal-body {
  padding: 1.5rem;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

/* Edit Preview */
.edit-preview {
  width: 100%;
  aspect-ratio: 16 / 9;
  border-radius: 0.5rem;
  overflow: hidden;
  background: #f3f4f6;
  margin-bottom: 1.5rem;
}

.edit-preview img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

/* Delete Preview */
.delete-preview {
  width: 200px;
  aspect-ratio: 4 / 3;
  border-radius: 0.5rem;
  overflow: hidden;
  margin: 0 auto 1rem;
}

.delete-preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* Form */
.form-group {
  margin-bottom: 1.25rem;
}

.form-group label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.5rem;
}

.form-control {
  width: 100%;
  padding: 0.625rem 0.875rem;
  font-size: 0.875rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  transition: all 0.2s;
}

.form-control:focus {
  outline: none;
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.form-text {
  display: block;
  font-size: 0.75rem;
  color: #9ca3af;
  margin-top: 0.25rem;
}

/* Buttons */
.btn {
  padding: 0.625rem 1.25rem;
  font-size: 0.875rem;
  font-weight: 500;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background: #8b5cf6;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #7c3aed;
}

.btn-secondary {
  background: #e5e7eb;
  color: #374151;
}

.btn-secondary:hover:not(:disabled) {
  background: #d1d5db;
}

.btn-danger {
  background: #ef4444;
  color: white;
}

.btn-danger:hover:not(:disabled) {
  background: #dc2626;
}

/* Image Viewer */
.modal-viewer {
  background: rgba(0, 0, 0, 0.9);
}

.viewer-content {
  position: relative;
  max-width: 90vw;
  max-height: 90vh;
}

.viewer-content img {
  max-width: 100%;
  max-height: 85vh;
  object-fit: contain;
}

.viewer-close {
  position: absolute;
  top: -3rem;
  right: 0;
  width: 2.5rem;
  height: 2.5rem;
  border: none;
  background: rgba(255, 255, 255, 0.1);
  color: white;
  font-size: 2rem;
  cursor: pointer;
  border-radius: 0.25rem;
  transition: background 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
}

.viewer-close:hover {
  background: rgba(255, 255, 255, 0.2);
}

.viewer-caption {
  background: rgba(0, 0, 0, 0.8);
  color: white;
  padding: 1rem;
  text-align: center;
  margin-top: 0.5rem;
  border-radius: 0.25rem;
}

/* Utility */
.text-center {
  text-align: center;
}

.text-muted {
  color: #9ca3af;
  font-size: 0.875rem;
}
</style>
