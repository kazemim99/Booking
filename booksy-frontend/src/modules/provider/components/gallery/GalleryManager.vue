<template>
  <div class="gallery-manager" dir="rtl">
    <!-- Header -->
    <div class="gallery-header">
      <div>
        <h2 class="gallery-title">مدیریت گالری تصاویر</h2>
        <p class="gallery-subtitle">
          {{ images.length }} تصویر
          <span v-if="maxImages"> از {{ maxImages }}</span>
        </p>
      </div>
      <button
        v-if="!showUploader"
        @click="showUploader = true"
        :disabled="isMaxImagesReached"
        class="btn btn-primary"
      >
        <svg class="icon" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        افزودن تصویر
      </button>
    </div>

    <!-- Upload Section -->
    <div v-if="showUploader" class="upload-section">
      <div class="upload-header">
        <h3>آپلود تصاویر جدید</h3>
        <button @click="showUploader = false" class="btn-close" type="button">
          ×
        </button>
      </div>
      <ImageUploadWidget
        ref="uploadWidget"
        :max-files="remainingSlots"
        :max-size-m-b="maxSizeMB"
        @files-selected="handleFilesSelected"
        @upload-complete="handleUploadComplete"
      />
      <div class="upload-actions">
        <button
          @click="handleUpload"
          :disabled="isUploading || queuedFiles.length === 0"
          class="btn btn-primary"
        >
          <span v-if="isUploading">در حال آپلود...</span>
          <span v-else>آپلود {{ queuedFiles.length }} تصویر</span>
        </button>
        <button
          @click="cancelUpload"
          :disabled="isUploading"
          class="btn btn-secondary"
        >
          انصراف
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-state">
      <div class="spinner"></div>
      <p>در حال بارگذاری تصاویر...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-state">
      <svg class="icon-error" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
      </svg>
      <p>{{ error }}</p>
      <button @click="loadImages" class="btn btn-secondary">تلاش مجدد</button>
    </div>

    <!-- Empty State -->
    <div v-else-if="images.length === 0" class="empty-state">
      <svg class="icon-empty" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clip-rule="evenodd" />
      </svg>
      <h3>گالری خالی است</h3>
      <p>برای شروع، تصاویری از کارهای خود آپلود کنید</p>
      <button @click="showUploader = true" class="btn btn-primary">
        افزودن اولین تصویر
      </button>
    </div>

    <!-- Gallery Grid -->
    <div v-else class="gallery-grid">
      <div
        v-for="(image, index) in sortedImages"
        :key="image.id"
        class="gallery-item"
        :class="{ 'dragging': draggingId === image.id }"
        draggable="true"
        @dragstart="handleDragStart($event, image, index)"
        @dragover="handleDragOver($event, index)"
        @dragend="handleDragEnd"
        @drop="handleDrop($event, index)"
      >
        <!-- Image Card -->
        <div class="image-card">
          <!-- Thumbnail -->
          <div class="image-thumbnail">
            <img
              :src="image.thumbnailUrl"
              :alt="image.altText || image.caption || 'عکس گالری'"
              @click="handleImageClick(image)"
            />
            <div class="image-overlay">
              <button
                @click="handleViewImage(image)"
                class="overlay-btn"
                title="مشاهده"
              >
                <svg viewBox="0 0 20 20" fill="currentColor">
                  <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
                  <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" />
                </svg>
              </button>
              <button
                v-if="!image.isPrimary"
                @click="handleSetPrimary(image)"
                class="overlay-btn btn-primary"
                title="تنظیم به عنوان تصویر اصلی"
              >
                <svg viewBox="0 0 20 20" fill="currentColor">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                </svg>
              </button>
              <button
                @click="handleEditMetadata(image)"
                class="overlay-btn"
                title="ویرایش"
              >
                <svg viewBox="0 0 20 20" fill="currentColor">
                  <path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" />
                </svg>
              </button>
              <button
                @click="handleDeleteImage(image)"
                class="overlay-btn btn-danger"
                title="حذف"
              >
                <svg viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
                </svg>
              </button>
            </div>
            <!-- Primary Badge -->
            <div v-if="image.isPrimary" class="primary-badge" title="تصویر اصلی">
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
              </svg>
            </div>
            <!-- Drag Handle -->
            <div class="drag-handle" title="جابجایی">
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path d="M7 2a2 2 0 10-4 0 2 2 0 004 0zM7 10a2 2 0 10-4 0 2 2 0 004 0zM7 18a2 2 0 10-4 0 2 2 0 004 0zM17 2a2 2 0 10-4 0 2 2 0 004 0zM17 10a2 2 0 10-4 0 2 2 0 004 0zM17 18a2 2 0 10-4 0 2 2 0 004 0z" />
              </svg>
            </div>
          </div>

          <!-- Image Info -->
          <div class="image-info">
            <p v-if="image.caption" class="image-caption">{{ image.caption }}</p>
            <p v-else class="image-caption placeholder">بدون عنوان</p>
            <p class="image-date">{{ formatDate(image.uploadedAt) }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Edit Metadata Modal -->
    <Teleport to="body">
      <div v-if="editingImage" class="modal-overlay" @click="closeEditModal">
        <div class="modal-content" @click.stop>
          <div class="modal-header">
            <h3>ویرایش اطلاعات تصویر</h3>
            <button @click="closeEditModal" class="btn-close" type="button">×</button>
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
            <button @click="closeDeleteModal" class="btn-close" type="button">×</button>
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
              <span v-else>بله، حذف شود</span>
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
import ImageUploadWidget from './ImageUploadWidget.vue'

// ============================================================================
// Props & Emits
// ============================================================================

interface Props {
  maxImages?: number
  maxSizeMB?: number
}

const props = withDefaults(defineProps<Props>(), {
  maxImages: 50,
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
const error = ref<string | null>(null)

// Upload State
const showUploader = ref(false)
const uploadWidget = ref<InstanceType<typeof ImageUploadWidget> | null>(null)
const queuedFiles = ref<File[]>([])
const isUploading = ref(false)

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

// Drag & Drop State
const draggingId = ref<string | null>(null)
const dragOverIndex = ref<number | null>(null)

// ============================================================================
// Computed
// ============================================================================

const sortedImages = computed(() => {
  return [...images.value].sort((a, b) => a.displayOrder - b.displayOrder)
})

const remainingSlots = computed(() => {
  return props.maxImages - images.value.length
})

const isMaxImagesReached = computed(() => {
  return images.value.length >= props.maxImages
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
    error.value = 'شناسه ارائه‌دهنده یافت نشد'
    return
  }

  loading.value = true
  error.value = null

  try {
    images.value = await galleryService.getGalleryImages(providerId.value)
    console.log(`[GalleryManager] Loaded ${images.value.length} images`)
  } catch (err) {
    console.error('[GalleryManager] Error loading images:', err)
    error.value = 'خطا در بارگذاری تصاویر'
  } finally {
    loading.value = false
  }
}

// ============================================================================
// Upload Operations
// ============================================================================

function handleFilesSelected(files: File[]): void {
  queuedFiles.value = files
}

async function handleUpload(): Promise<void> {
  if (!providerId.value || queuedFiles.value.length === 0) return

  isUploading.value = true

  try {
    const uploadedImages = await galleryService.uploadImages(
      providerId.value,
      queuedFiles.value,
      (progress) => {
        // Update progress in widget
        progress.forEach(p => {
          uploadWidget.value?.setItemProgress(p.file, p.progress)
          if (p.status === 'success') {
            uploadWidget.value?.setItemStatus(p.file, 'success')
          } else if (p.status === 'error') {
            uploadWidget.value?.setItemStatus(p.file, 'error', p.error)
          }
        })
      }
    )

    console.log(`[GalleryManager] Uploaded ${uploadedImages.length} images`)

    // Reload gallery
    await loadImages()

    emit('upload-success', uploadedImages)
    emit('images-updated', images.value)

    // Reset upload state
    showUploader.value = false
    queuedFiles.value = []
  } catch (err) {
    console.error('[GalleryManager] Error uploading images:', err)
    error.value = 'خطا در آپلود تصاویر'
    emit('upload-error', err as Error)
  } finally {
    isUploading.value = false
  }
}

function handleUploadComplete(succeeded: File[], failed: File[]): void {
  console.log(`[GalleryManager] Upload complete: ${succeeded.length} succeeded, ${failed.length} failed`)
}

function cancelUpload(): void {
  showUploader.value = false
  queuedFiles.value = []
  uploadWidget.value?.clearSuccessful()
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

    emit('images-updated', images.value)
    closeEditModal()
  } catch (err) {
    console.error('[GalleryManager] Error updating metadata:', err)
    error.value = 'خطا در ذخیره تغییرات'
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

    emit('images-updated', images.value)
    closeDeleteModal()
  } catch (err) {
    console.error('[GalleryManager] Error deleting image:', err)
    error.value = 'خطا در حذف تصویر'
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

function handleImageClick(image: GalleryImage): void {
  viewingImage.value = image
}

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

    emit('images-updated', images.value)
  } catch (err) {
    console.error('[GalleryManager] Error setting primary image:', err)
    error.value = 'خطا در تنظیم تصویر اصلی'
  }
}

// ============================================================================
// Drag & Drop Reordering
// ============================================================================

function handleDragStart(event: DragEvent, image: GalleryImage, index: number): void {
  draggingId.value = image.id
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
    event.dataTransfer.setData('text/html', index.toString())
  }
}

function handleDragOver(event: DragEvent, index: number): void {
  event.preventDefault()
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'move'
  }
  dragOverIndex.value = index
}

function handleDragEnd(): void {
  draggingId.value = null
  dragOverIndex.value = null
}

async function handleDrop(event: DragEvent, dropIndex: number): Promise<void> {
  event.preventDefault()

  if (!providerId.value || !draggingId.value) return

  const dragIndex = sortedImages.value.findIndex(img => img.id === draggingId.value)

  if (dragIndex === dropIndex || dragIndex === -1) {
    handleDragEnd()
    return
  }

  // Reorder locally first for immediate feedback
  const reordered = [...sortedImages.value]
  const [draggedItem] = reordered.splice(dragIndex, 1)
  reordered.splice(dropIndex, 0, draggedItem)

  // Update display order
  const updatedImages = reordered.map((img, idx) => ({
    ...img,
    displayOrder: idx,
  }))

  images.value = updatedImages

  // Persist to backend
  try {
    // Build imageOrders dictionary: { imageId: displayOrder }
    const imageOrders: Record<string, number> = {}
    updatedImages.forEach(img => {
      imageOrders[img.id] = img.displayOrder
    })

    await galleryService.reorderImages(providerId.value, {
      imageOrders
    })

    console.log('[GalleryManager] Images reordered successfully')
    emit('images-updated', images.value)
  } catch (err) {
    console.error('[GalleryManager] Error reordering images:', err)
    // Rollback on error
    await loadImages()
    error.value = 'خطا در تغییر ترتیب تصاویر'
  } finally {
    handleDragEnd()
  }
}

// ============================================================================
// Utilities
// ============================================================================

function formatDate(date: Date | string): string {
  const d = typeof date === 'string' ? new Date(date) : date
  const now = new Date()
  const diffMs = now.getTime() - d.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) return 'امروز'
  if (diffDays === 1) return 'دیروز'
  if (diffDays < 7) return `${diffDays} روز پیش`
  if (diffDays < 30) return `${Math.floor(diffDays / 7)} هفته پیش`
  if (diffDays < 365) return `${Math.floor(diffDays / 30)} ماه پیش`
  return `${Math.floor(diffDays / 365)} سال پیش`
}
</script>

<style scoped>
.gallery-manager {
  padding: 1.5rem;
}

/* Header */
.gallery-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.gallery-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a202c;
  margin: 0 0 0.25rem 0;
}

.gallery-subtitle {
  color: #718096;
  font-size: 0.875rem;
  margin: 0;
}

/* Upload Section */
.upload-section {
  background: #f7fafc;
  border: 2px dashed #cbd5e0;
  border-radius: 0.5rem;
  padding: 1.5rem;
  margin-bottom: 2rem;
}

.upload-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.upload-header h3 {
  font-size: 1.125rem;
  font-weight: 600;
  color: #2d3748;
  margin: 0;
}

.upload-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1rem;
}

/* States */
.loading-state,
.error-state,
.empty-state {
  text-align: center;
  padding: 3rem 1rem;
}

.spinner {
  width: 3rem;
  height: 3rem;
  border: 4px solid #e2e8f0;
  border-top-color: #3182ce;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.icon-error,
.icon-empty {
  width: 4rem;
  height: 4rem;
  color: #cbd5e0;
  margin: 0 auto 1rem;
}

.icon-error {
  color: #f56565;
}

.error-state p,
.empty-state h3 {
  color: #2d3748;
  margin: 0.5rem 0;
}

.empty-state p {
  color: #718096;
  margin: 0.5rem 0 1.5rem;
}

/* Gallery Grid */
.gallery-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1.5rem;
}

.gallery-item {
  position: relative;
  transition: transform 0.2s, opacity 0.2s;
}

.gallery-item:hover {
  transform: translateY(-4px);
}

.gallery-item.dragging {
  opacity: 0.5;
}

/* Image Card */
.image-card {
  background: white;
  border-radius: 0.5rem;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  transition: box-shadow 0.2s;
}

.image-card:hover {
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.15);
}

/* Thumbnail */
.image-thumbnail {
  position: relative;
  width: 100%;
  aspect-ratio: 4 / 3;
  overflow: hidden;
  background: #f7fafc;
}

.image-thumbnail img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  cursor: pointer;
  transition: transform 0.3s;
}

.image-thumbnail:hover img {
  transform: scale(1.05);
}

/* Overlay */
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

.image-thumbnail:hover .image-overlay {
  opacity: 1;
}

.overlay-btn {
  width: 2.5rem;
  height: 2.5rem;
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
  background: #f7fafc;
}

.overlay-btn svg {
  width: 1.25rem;
  height: 1.25rem;
  color: #2d3748;
}

.overlay-btn.btn-danger:hover {
  background: #f56565;
}

.overlay-btn.btn-danger:hover svg {
  color: white;
}

/* Drag Handle */
.drag-handle {
  position: absolute;
  top: 0.5rem;
  left: 0.5rem;
  width: 2rem;
  height: 2rem;
  background: rgba(255, 255, 255, 0.9);
  border-radius: 0.25rem;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: move;
  opacity: 0;
  transition: opacity 0.2s;
}

.image-thumbnail:hover .drag-handle {
  opacity: 1;
}

.drag-handle svg {
  width: 1.25rem;
  height: 1.25rem;
  color: #718096;
}

/* Image Info */
.image-info {
  padding: 0.75rem 1rem;
}

.image-caption {
  font-size: 0.875rem;
  color: #2d3748;
  margin: 0 0 0.25rem 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.image-caption.placeholder {
  color: #a0aec0;
  font-style: italic;
}

.image-date {
  font-size: 0.75rem;
  color: #a0aec0;
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
  border-radius: 0.5rem;
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
  border-bottom: 1px solid #e2e8f0;
}

.modal-header h3 {
  font-size: 1.25rem;
  font-weight: 600;
  color: #2d3748;
  margin: 0;
}

.btn-close {
  width: 2rem;
  height: 2rem;
  border: none;
  background: transparent;
  font-size: 1.5rem;
  color: #718096;
  cursor: pointer;
  transition: color 0.2s;
}

.btn-close:hover {
  color: #2d3748;
}

.modal-body {
  padding: 1.5rem;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 1.5rem;
  border-top: 1px solid #e2e8f0;
}

/* Edit Preview */
.edit-preview {
  width: 100%;
  aspect-ratio: 16 / 9;
  border-radius: 0.375rem;
  overflow: hidden;
  background: #f7fafc;
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
  border-radius: 0.375rem;
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
  color: #2d3748;
  margin-bottom: 0.5rem;
}

.form-control {
  width: 100%;
  padding: 0.625rem 0.875rem;
  font-size: 0.875rem;
  border: 1px solid #cbd5e0;
  border-radius: 0.375rem;
  transition: all 0.2s;
}

.form-control:focus {
  outline: none;
  border-color: #3182ce;
  box-shadow: 0 0 0 3px rgba(49, 130, 206, 0.1);
}

.form-text {
  display: block;
  font-size: 0.75rem;
  color: #a0aec0;
  margin-top: 0.25rem;
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

/* Buttons */
.btn {
  padding: 0.625rem 1.25rem;
  font-size: 0.875rem;
  font-weight: 500;
  border: none;
  border-radius: 0.375rem;
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
  background: #3182ce;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #2c5aa0;
}

.btn-secondary {
  background: #e2e8f0;
  color: #2d3748;
}

.btn-secondary:hover:not(:disabled) {
  background: #cbd5e0;
}

.btn-danger {
  background: #f56565;
  color: white;
}

.btn-danger:hover:not(:disabled) {
  background: #e53e3e;
}

.btn .icon {
  width: 1.25rem;
  height: 1.25rem;
}

/* Utility */
.text-center {
  text-align: center;
}

.text-muted {
  color: #a0aec0;
  font-size: 0.875rem;
}

/* Primary Image Badge */
.primary-badge {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
  width: 2.5rem;
  height: 2.5rem;
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 8px rgba(251, 191, 36, 0.5);
  z-index: 10;
}

.primary-badge svg {
  width: 1.5rem;
  height: 1.5rem;
  color: white;
}

/* Primary Button Styling */
.overlay-btn.btn-primary {
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  border: none;
}

.overlay-btn.btn-primary svg {
  color: white;
}

.overlay-btn.btn-primary:hover {
  transform: scale(1.1);
  box-shadow: 0 4px 12px rgba(251, 191, 36, 0.4);
}
</style>
