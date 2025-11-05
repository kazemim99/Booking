<template>
  <div class="gallery-view">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Photo Gallery</h1>
        <p class="page-subtitle">
          Showcase your business with photos of your work and workspace
        </p>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="galleryStore.isLoading && !galleryStore.galleryImages.length" class="loading-state">
      <Spinner />
      <p>Loading gallery...</p>
    </div>

    <!-- Error State -->
    <Alert
      v-if="galleryStore.error"
      type="error"
      :message="galleryStore.error"
      @dismiss="galleryStore.clearError()"
    />

    <!-- Success Message -->
    <Alert
      v-if="successMessage"
      type="success"
      :message="successMessage"
      @dismiss="successMessage = null"
    />

    <div v-if="!galleryStore.isLoading || galleryStore.galleryImages.length" class="gallery-content">
      <!-- Portfolio Gallery Section -->
      <Card class="section">
        <div class="section-header">
          <div>
            <h2 class="section-title">Portfolio Gallery</h2>
            <p class="section-description">
              Showcase your work with high-quality photos to attract more customers
            </p>
          </div>
          <div class="header-actions">
            <Button
              v-if="selectedImages.length > 0"
              variant="danger"
              size="small"
              nativeType="button"
              @click="handleBulkDelete"
              :disabled="isDeletingBulk"
            >
              {{ isDeletingBulk ? 'Deleting...' : `Delete ${selectedImages.length} Selected` }}
            </Button>
            <Button
              variant="primary"
              nativeType="button"
              @click="triggerGalleryUpload"
              :disabled="galleryStore.isUploading"
            >
              {{ galleryStore.isUploading ? 'Uploading...' : '+ Add Photos' }}
            </Button>
          </div>
          <input
            ref="galleryFileInput"
            type="file"
            accept="image/jpeg,image/png,image/webp,image/jpg"
            multiple
            class="file-input"
            @change="handleGalleryUpload"
            :disabled="galleryStore.isUploading"
          />
        </div>

        <!-- Upload Progress -->
        <div v-if="galleryStore.uploadProgress.size > 0" class="upload-progress-container">
          <div
            v-for="[filename, progress] in galleryStore.uploadProgress"
            :key="filename"
            class="upload-progress-item"
          >
            <div class="progress-info">
              <span class="filename">{{ filename }}</span>
              <span class="progress-percent">{{ progress.progress }}%</span>
            </div>
            <div class="progress-bar">
              <div
                class="progress-fill"
                :style="{ width: `${progress.progress}%` }"
                :class="{
                  'progress-success': progress.status === 'success',
                  'progress-error': progress.status === 'error'
                }"
              ></div>
            </div>
            <p v-if="progress.error" class="progress-error-msg">{{ progress.error }}</p>
          </div>
        </div>

        <!-- Gallery Grid -->
        <div v-if="galleryStore.galleryImages.length > 0" class="gallery-grid">
          <!-- Select All Checkbox -->
          <div v-if="galleryStore.galleryImages.length > 1" class="select-all-container">
            <label class="checkbox-label">
              <input
                type="checkbox"
                :checked="isAllSelected"
                :indeterminate="isSomeSelected"
                @change="toggleSelectAll"
                class="checkbox-input"
              />
              <span>Select All ({{ selectedImages.length }} selected)</span>
            </label>
          </div>

          <div
            v-for="(image, index) in galleryStore.galleryImages"
            :key="image.id"
            class="gallery-item"
            :class="{ 'gallery-item-selected': selectedImages.includes(image.id) }"
          >
            <!-- Selection Checkbox -->
            <label class="gallery-checkbox">
              <input
                type="checkbox"
                :checked="selectedImages.includes(image.id)"
                @change="toggleImageSelection(image.id)"
                class="checkbox-input"
                :aria-label="`Select image ${index + 1}`"
              />
            </label>

            <!-- Image Container -->
            <div
              class="gallery-image-container"
              @click="openLightbox(index)"
              @keydown.enter="openLightbox(index)"
              @keydown.space.prevent="openLightbox(index)"
              role="button"
              tabindex="0"
              :aria-label="`View ${image.caption || `image ${index + 1}`} in lightbox`"
            >
              <img
                :src="image.thumbnailUrl"
                :alt="image.altText || image.caption || `Gallery image ${index + 1}`"
                class="gallery-image"
                loading="lazy"
              />
              <div class="gallery-actions">
                <Button
                  variant="ghost"
                  size="small"
                  nativeType="button"
                  @click.stop="editImage(image)"
                  title="Edit Image Details"
                  aria-label="Edit image metadata"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="currentColor"
                    width="16"
                    height="16"
                    aria-hidden="true"
                  >
                    <path
                      d="M21.731 2.269a2.625 2.625 0 00-3.712 0l-1.157 1.157 3.712 3.712 1.157-1.157a2.625 2.625 0 000-3.712zM19.513 8.199l-3.712-3.712-12.15 12.15a5.25 5.25 0 00-1.32 2.214l-.8 2.685a.75.75 0 00.933.933l2.685-.8a5.25 5.25 0 002.214-1.32L19.513 8.2z"
                    />
                  </svg>
                </Button>
                <Button
                  variant="danger"
                  size="small"
                  nativeType="button"
                  @click.stop="deleteImage(image.id)"
                  title="Remove Image"
                  aria-label="Delete image"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="currentColor"
                    width="16"
                    height="16"
                    aria-hidden="true"
                  >
                    <path
                      fill-rule="evenodd"
                      d="M16.5 4.478v.227a48.816 48.816 0 013.878.512.75.75 0 11-.256 1.478l-.209-.035-1.005 13.07a3 3 0 01-2.991 2.77H8.084a3 3 0 01-2.991-2.77L4.087 6.66l-.209.035a.75.75 0 01-.256-1.478A48.567 48.567 0 017.5 4.705v-.227c0-1.564 1.213-2.9 2.816-2.951a52.662 52.662 0 013.369 0c1.603.051 2.815 1.387 2.815 2.951zm-6.136-1.452a51.196 51.196 0 013.273 0C14.39 3.05 15 3.684 15 4.478v.113a49.488 49.488 0 00-6 0v-.113c0-.794.609-1.428 1.364-1.452zm-.355 5.945a.75.75 0 10-1.5.058l.347 9a.75.75 0 001.499-.058l-.346-9zm5.48.058a.75.75 0 10-1.498-.058l-.347 9a.75.75 0 001.5.058l.345-9z"
                      clip-rule="evenodd"
                    />
                  </svg>
                </Button>
              </div>
            </div>

            <!-- Image Caption -->
            <div class="gallery-item-caption" :title="image.caption">
              {{ image.caption || `Image ${index + 1}` }}
            </div>
          </div>
        </div>

        <!-- Empty State -->
        <div v-else class="gallery-empty">
          <div class="empty-icon" aria-hidden="true">🖼️</div>
          <h3>No Portfolio Photos Yet</h3>
          <p>Upload photos of your work to attract more customers</p>
          <Button
            variant="primary"
            nativeType="button"
            @click="triggerGalleryUpload"
            :disabled="galleryStore.isUploading"
          >
            + Add Your First Photo
          </Button>
        </div>
      </Card>
    </div>

    <!-- Image Edit Modal -->
    <Modal
      v-if="showImageEditModal"
      :modelValue="showImageEditModal"
      title="Edit Image Details"
      @update:modelValue="closeImageEditModal"
    >
      <form class="image-edit-form" @submit.prevent="saveImageEdit">
        <div class="form-group">
          <label for="imageCaption">Caption</label>
          <input
            id="imageCaption"
            type="text"
            v-model="editingImage.caption"
            placeholder="Enter a caption for this image"
            class="form-input"
            maxlength="500"
          />
          <small>A brief description of what's shown in the image (max 500 characters)</small>
        </div>

        <div class="form-group">
          <label for="imageAltText">Alt Text</label>
          <input
            id="imageAltText"
            type="text"
            v-model="editingImage.altText"
            placeholder="Enter alternative text for this image"
            class="form-input"
            maxlength="500"
          />
          <small>Describe the image for accessibility purposes (max 500 characters)</small>
        </div>

        <div class="modal-actions">
          <Button type="button" variant="secondary" @click="closeImageEditModal">
            Cancel
          </Button>
          <Button type="submit" variant="primary" :disabled="isSavingMetadata">
            {{ isSavingMetadata ? 'Saving...' : 'Save Changes' }}
          </Button>
        </div>
      </form>
    </Modal>

    <!-- Delete Confirmation Modal -->
    <Modal
      v-if="showDeleteConfirmModal"
      :modelValue="showDeleteConfirmModal"
      title="Confirm Deletion"
      @update:modelValue="closeDeleteConfirmModal"
    >
      <div class="delete-confirm-content">
        <p v-if="selectedImages.length > 1">
          Are you sure you want to delete {{ selectedImages.length }} images? This action cannot be undone.
        </p>
        <p v-else>
          Are you sure you want to delete this image? This action cannot be undone.
        </p>

        <div class="modal-actions">
          <Button type="button" variant="secondary" @click="closeDeleteConfirmModal">
            Cancel
          </Button>
          <Button
            type="button"
            variant="danger"
            @click="confirmDelete"
            :disabled="isDeletingBulk"
          >
            {{ isDeletingBulk ? 'Deleting...' : 'Delete' }}
          </Button>
        </div>
      </div>
    </Modal>

    <!-- Image Lightbox -->
    <ImageLightbox
      ref="lightboxRef"
      :images="galleryStore.galleryImages"
      :initial-index="lightboxInitialIndex"
      :show-thumbnails="true"
      @close="handleLightboxClose"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { useGalleryStore } from '@/modules/provider/stores/gallery.store'
import { Button, Card, Modal, Alert, Spinner } from '@/shared/components'
import ImageLightbox from '@/modules/provider/components/gallery/ImageLightbox.vue'
import type { GalleryImage } from '@/modules/provider/types/gallery.types'

const router = useRouter()
const providerStore = useProviderStore()
const galleryStore = useGalleryStore()

// State
const successMessage = ref<string | null>(null)
const galleryFileInput = ref<HTMLInputElement | null>(null)
const lightboxRef = ref<InstanceType<typeof ImageLightbox> | null>(null)
const lightboxInitialIndex = ref(0)

// Selection state
const selectedImages = ref<string[]>([])
const isAllSelected = computed(() =>
  galleryStore.galleryImages.length > 0 &&
  selectedImages.value.length === galleryStore.galleryImages.length
)
const isSomeSelected = computed(() =>
  selectedImages.value.length > 0 && selectedImages.value.length < galleryStore.galleryImages.length
)

// Edit modal state
const showImageEditModal = ref(false)
const editingImage = ref<{ id: string; caption?: string; altText?: string }>({
  id: '',
  caption: '',
  altText: ''
})
const isSavingMetadata = ref(false)

// Delete confirmation state
const showDeleteConfirmModal = ref(false)
const imageToDelete = ref<string | null>(null)
const isDeletingBulk = ref(false)

// Load gallery images
onMounted(async () => {
  try {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const provider = providerStore.currentProvider
    if (!provider) {
      galleryStore.error = 'Provider profile not found. Please register as a provider first.'
      return
    }

    await galleryStore.fetchGalleryImages(provider.id)
  } catch (error) {
    console.error('Error loading gallery:', error)
  }
})

// Cleanup
onUnmounted(() => {
  galleryStore.clearError()
})

// File upload handlers
function triggerGalleryUpload() {
  galleryFileInput.value?.click()
}

async function handleGalleryUpload(event: Event) {
  const input = event.target as HTMLInputElement
  if (!input.files || input.files.length === 0) return

  const provider = providerStore.currentProvider
  if (!provider) {
    galleryStore.error = 'Provider not found'
    return
  }

  const files = Array.from(input.files)

  // Validate file types
  const invalidFiles = files.filter(
    file => !['image/jpeg', 'image/png', 'image/webp', 'image/jpg'].includes(file.type)
  )
  if (invalidFiles.length > 0) {
    galleryStore.error = 'Only JPG, PNG, and WebP images are allowed'
    input.value = ''
    return
  }

  // Validate file sizes (10MB max)
  const oversizedFiles = files.filter(file => file.size > 10 * 1024 * 1024)
  if (oversizedFiles.length > 0) {
    galleryStore.error = 'Some files exceed the 10MB size limit'
    input.value = ''
    return
  }

  try {
    await galleryStore.uploadImages(provider.id, files)
    successMessage.value = `Successfully uploaded ${files.length} image${files.length > 1 ? 's' : ''}`
    setTimeout(() => {
      successMessage.value = null
    }, 3000)
  } catch (error: any) {
    console.error('Upload error:', error)
    // Error is already set in the store
  } finally {
    input.value = ''
  }
}

// Image selection handlers
function toggleImageSelection(imageId: string) {
  const index = selectedImages.value.indexOf(imageId)
  if (index > -1) {
    selectedImages.value.splice(index, 1)
  } else {
    selectedImages.value.push(imageId)
  }
}

function toggleSelectAll() {
  if (isAllSelected.value) {
    selectedImages.value = []
  } else {
    selectedImages.value = galleryStore.galleryImages.map(img => img.id)
  }
}

// Edit image handlers
function editImage(image: GalleryImage) {
  editingImage.value = {
    id: image.id,
    caption: image.caption || '',
    altText: image.altText || ''
  }
  showImageEditModal.value = true
}

function closeImageEditModal() {
  showImageEditModal.value = false
  editingImage.value = { id: '', caption: '', altText: '' }
}

async function saveImageEdit() {
  const provider = providerStore.currentProvider
  if (!provider) return

  isSavingMetadata.value = true

  try {
    await galleryStore.updateImageMetadata(provider.id, editingImage.value.id, {
      caption: editingImage.value.caption,
      altText: editingImage.value.altText
    })

    successMessage.value = 'Image details updated successfully'
    setTimeout(() => {
      successMessage.value = null
    }, 3000)

    closeImageEditModal()
  } catch (error) {
    console.error('Error updating image metadata:', error)
  } finally {
    isSavingMetadata.value = false
  }
}

// Delete handlers
function deleteImage(imageId: string) {
  imageToDelete.value = imageId
  selectedImages.value = [imageId]
  showDeleteConfirmModal.value = true
}

function handleBulkDelete() {
  if (selectedImages.value.length === 0) return
  showDeleteConfirmModal.value = true
}

function closeDeleteConfirmModal() {
  showDeleteConfirmModal.value = false
  if (!isDeletingBulk.value) {
    imageToDelete.value = null
  }
}

async function confirmDelete() {
  const provider = providerStore.currentProvider
  if (!provider || selectedImages.value.length === 0) return

  isDeletingBulk.value = true

  try {
    // Delete all selected images
    for (const imageId of selectedImages.value) {
      await galleryStore.deleteImage(provider.id, imageId)
    }

    successMessage.value = `Successfully deleted ${selectedImages.value.length} image${selectedImages.value.length > 1 ? 's' : ''}`
    setTimeout(() => {
      successMessage.value = null
    }, 3000)

    selectedImages.value = []
    closeDeleteConfirmModal()
  } catch (error) {
    console.error('Error deleting images:', error)
  } finally {
    isDeletingBulk.value = false
  }
}

// Lightbox handlers
function openLightbox(index: number) {
  lightboxInitialIndex.value = index
  lightboxRef.value?.open(index)
}

function handleLightboxClose() {
  // Optional: Add any cleanup or tracking
}
</script>

<style scoped>
.gallery-view {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.page-header {
  margin-bottom: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
  color: #111827;
}

.page-subtitle {
  font-size: 1rem;
  color: #6b7280;
  margin: 0;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
}

.gallery-content {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.section {
  padding: 2rem;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  margin-bottom: 2rem;
  flex-wrap: wrap;
}

.section-title {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
  color: #111827;
}

.section-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.header-actions {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.file-input {
  display: none;
}

/* Upload Progress */
.upload-progress-container {
  margin-bottom: 1.5rem;
  padding: 1rem;
  background-color: #f9fafb;
  border-radius: 0.5rem;
  border: 1px solid #e5e7eb;
}

.upload-progress-item {
  margin-bottom: 1rem;
}

.upload-progress-item:last-child {
  margin-bottom: 0;
}

.progress-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.filename {
  font-size: 0.875rem;
  color: #374151;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.progress-percent {
  font-size: 0.75rem;
  color: #6b7280;
  font-weight: 600;
  margin-left: 0.5rem;
}

.progress-bar {
  height: 0.5rem;
  background-color: #e5e7eb;
  border-radius: 0.25rem;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background-color: #8b5cf6;
  transition: width 0.3s ease;
}

.progress-fill.progress-success {
  background-color: #10b981;
}

.progress-fill.progress-error {
  background-color: #ef4444;
}

.progress-error-msg {
  font-size: 0.75rem;
  color: #ef4444;
  margin: 0.25rem 0 0 0;
}

/* Selection */
.select-all-container {
  grid-column: 1 / -1;
  padding: 0.75rem 1rem;
  background-color: #f9fafb;
  border-radius: 0.375rem;
  border: 1px solid #e5e7eb;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.checkbox-input {
  width: 1rem;
  height: 1rem;
  cursor: pointer;
}

/* Gallery Grid */
.gallery-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1.5rem;
}

.gallery-item {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  border-radius: 0.5rem;
  transition: all 0.2s;
}

.gallery-item-selected {
  outline: 3px solid #8b5cf6;
  outline-offset: 2px;
}

.gallery-checkbox {
  position: absolute;
  top: 0.5rem;
  left: 0.5rem;
  z-index: 2;
  background-color: white;
  border-radius: 0.25rem;
  padding: 0.25rem;
  box-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1);
  cursor: pointer;
}

.gallery-image-container {
  position: relative;
  aspect-ratio: 1 / 1;
  border-radius: 0.5rem;
  overflow: hidden;
  cursor: pointer;
  background-color: #f3f4f6;
}

.gallery-image-container:focus {
  outline: 2px solid #8b5cf6;
  outline-offset: 2px;
}

.gallery-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.2s;
}

.gallery-image-container:hover .gallery-image {
  transform: scale(1.05);
}

.gallery-actions {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  opacity: 0;
  transition: opacity 0.2s;
}

.gallery-image-container:hover .gallery-actions,
.gallery-image-container:focus-within .gallery-actions {
  opacity: 1;
}

.gallery-item-caption {
  font-size: 0.875rem;
  color: #4b5563;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  padding: 0 0.25rem;
}

/* Empty State */
.gallery-empty {
  text-align: center;
  padding: 4rem 2rem;
  background-color: #f9fafb;
  border-radius: 0.5rem;
  border: 1px dashed #d1d5db;
}

.empty-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}

.gallery-empty h3 {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
  color: #111827;
}

.gallery-empty p {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0 0 1.5rem 0;
}

/* Modal Forms */
.image-edit-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #111827;
}

.form-input {
  padding: 0.625rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  color: #111827;
}

.form-input:focus {
  outline: 2px solid #8b5cf6;
  outline-offset: 0;
  border-color: #8b5cf6;
}

.form-group small {
  font-size: 0.75rem;
  color: #6b7280;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 1rem;
}

.delete-confirm-content p {
  margin-bottom: 1.5rem;
  color: #374151;
}

/* Responsive */
@media (max-width: 768px) {
  .gallery-view {
    padding: 1rem;
  }

  .section {
    padding: 1rem;
  }

  .section-header {
    flex-direction: column;
  }

  .header-actions {
    width: 100%;
    flex-direction: column;
  }

  .header-actions button {
    width: 100%;
  }

  .gallery-grid {
    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
    gap: 1rem;
  }

  .modal-actions {
    flex-direction: column;
  }

  .modal-actions button {
    width: 100%;
  }
}

/* Accessibility */
@media (prefers-reduced-motion: reduce) {
  * {
    transition: none !important;
    animation: none !important;
  }
}
</style>
