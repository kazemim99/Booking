<template>
  <div class="gallery-view">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Photo Gallery</h1>
        <p class="page-subtitle">
          Showcase your business with high-quality photos of your work, team, and workspace
        </p>
      </div>
      <Button variant="secondary" @click="goBack">← Back to Dashboard</Button>
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
      <!-- Upload Section -->
      <Card class="section">
        <h2 class="section-title">Upload Photos</h2>
        <p class="section-description">
          Add images to your gallery. You can upload up to {{ maxImages }} images.
        </p>

        <GalleryUpload
          :max-images="maxImages"
          :is-uploading="galleryStore.isUploading"
          :upload-progress="uploadProgress"
          @upload="handleUpload"
        />

        <div v-if="galleryStore.isUploading" class="upload-status">
          <p>Uploading {{ uploadingFileCount }} image(s)...</p>
        </div>
      </Card>

      <!-- Gallery Section -->
      <Card class="section">
        <div class="section-header">
          <div>
            <h2 class="section-title">Your Gallery</h2>
            <p class="section-description">
              {{ galleryStore.galleryImages.length }} / {{ maxImages }} images
            </p>
          </div>

          <div v-if="galleryStore.galleryImages.length > 0" class="header-actions">
            <Button
              v-if="selectedImages.length > 0"
              variant="danger"
              size="small"
              @click="handleBulkDelete"
            >
              Delete Selected ({{ selectedImages.length }})
            </Button>
          </div>
        </div>

        <GalleryGrid
          ref="galleryGridRef"
          :images="galleryStore.galleryImages"
          :loading="galleryStore.isLoading"
          :selectable="true"
          :reorderable="true"
          :show-captions="true"
          :columns="3"
          empty-message="Upload your first photos to showcase your work and attract more customers"
          @edit="openEditModal"
          @delete="handleDelete"
          @reorder="handleReorder"
          @selection-change="handleSelectionChange"
        />
      </Card>
    </div>

    <!-- Edit Image Modal -->
    <Modal
      v-if="showEditModal"
      :modelValue="showEditModal"
      title="Edit Image Details"
      @update:modelValue="closeEditModal"
    >
      <form class="edit-form" @submit.prevent="saveImageEdit">
        <div class="form-group">
          <label for="imageCaption">Caption</label>
          <input
            id="imageCaption"
            v-model="editingMetadata.caption"
            type="text"
            class="form-input"
            placeholder="Enter a caption for this image"
            maxlength="500"
          />
          <small>A brief description of what's shown in the image (max 500 characters)</small>
        </div>

        <div class="form-group">
          <label for="imageAltText">Alt Text</label>
          <input
            id="imageAltText"
            v-model="editingMetadata.altText"
            type="text"
            class="form-input"
            placeholder="Describe the image for accessibility"
            maxlength="500"
          />
          <small>Helps screen readers and improves SEO (max 500 characters)</small>
        </div>

        <div class="modal-actions">
          <Button type="button" variant="secondary" @click="closeEditModal">
            Cancel
          </Button>
          <Button type="submit" variant="primary" :disabled="isSavingEdit">
            {{ isSavingEdit ? 'Saving...' : 'Save Changes' }}
          </Button>
        </div>
      </form>
    </Modal>

    <!-- Delete Confirmation Modal -->
    <Modal
      v-if="showDeleteModal"
      :modelValue="showDeleteModal"
      title="Delete Image"
      @update:modelValue="closeDeleteModal"
    >
      <p class="delete-message">
        Are you sure you want to delete this image? This action cannot be undone.
      </p>

      <div class="modal-actions">
        <Button type="button" variant="secondary" @click="closeDeleteModal">
          Cancel
        </Button>
        <Button type="button" variant="danger" :disabled="isDeleting" @click="confirmDelete">
          {{ isDeleting ? 'Deleting...' : 'Delete Image' }}
        </Button>
      </div>
    </Modal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { useGalleryStore } from '@/modules/provider/stores/gallery.store'
import { Button, Card, Modal, Alert, Spinner } from '@/shared/components'
import GalleryUpload from '@/modules/provider/components/gallery/GalleryUpload.vue'
import GalleryGrid from '@/modules/provider/components/gallery/GalleryGrid.vue'

const router = useRouter()
const providerStore = useProviderStore()
const galleryStore = useGalleryStore()

// Constants
const maxImages = 50

// State
const successMessage = ref<string | null>(null)
const showEditModal = ref(false)
const showDeleteModal = ref(false)
const editingImageId = ref<string | null>(null)
const deletingImageId = ref<string | null>(null)
const isSavingEdit = ref(false)
const isDeleting = ref(false)
const uploadingFileCount = ref(0)
const uploadProgress = ref(0)
const selectedImages = ref<string[]>([])
const galleryGridRef = ref<InstanceType<typeof GalleryGrid> | null>(null)

const editingMetadata = ref({
  caption: '',
  altText: ''
})

// Computed
const currentProviderId = computed(() => providerStore.currentProvider?.id)

// Lifecycle
onMounted(async () => {
  try {
    // Ensure provider is loaded
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const providerId = currentProviderId.value
    if (!providerId) {
      showError('Provider profile not found. Please register as a provider first.')
      return
    }

    // Load gallery images
    await galleryStore.fetchGalleryImages(providerId)
  } catch (error) {
    console.error('Error loading gallery:', error)
    showError('Failed to load gallery. Please try again.')
  }
})

// Methods
async function handleUpload(files: File[]) {
  const providerId = currentProviderId.value
  if (!providerId) {
    showError('Provider not found')
    return
  }

  uploadingFileCount.value = files.length
  uploadProgress.value = 0

  try {
    const uploadedImages = await galleryStore.uploadImages(providerId, files)

    showSuccess(`Successfully uploaded ${uploadedImages.length} image(s) to your gallery!`)

    // Reset upload state
    uploadingFileCount.value = 0
    uploadProgress.value = 0
  } catch (error: any) {
    console.error('Error uploading images:', error)
    showError(error.message || 'Failed to upload images. Please try again.')
  }
}

function openEditModal(imageId: string) {
  const image = galleryStore.galleryImages.find((img) => img.id === imageId)
  if (!image) return

  editingImageId.value = imageId
  editingMetadata.value = {
    caption: image.caption || '',
    altText: image.altText || ''
  }
  showEditModal.value = true
}

function closeEditModal() {
  showEditModal.value = false
  editingImageId.value = null
  editingMetadata.value = { caption: '', altText: '' }
}

async function saveImageEdit() {
  const providerId = currentProviderId.value
  const imageId = editingImageId.value

  if (!providerId || !imageId) return

  isSavingEdit.value = true

  try {
    await galleryStore.updateImageMetadata(providerId, imageId, editingMetadata.value)

    showSuccess('Image details updated successfully!')
    closeEditModal()
  } catch (error) {
    console.error('Error updating image:', error)
    showError('Failed to update image. Please try again.')
  } finally {
    isSavingEdit.value = false
  }
}

function handleDelete(imageId: string) {
  deletingImageId.value = imageId
  showDeleteModal.value = true
}

function closeDeleteModal() {
  showDeleteModal.value = false
  deletingImageId.value = null
}

async function confirmDelete() {
  const providerId = currentProviderId.value
  const imageId = deletingImageId.value

  if (!providerId || !imageId) return

  isDeleting.value = true

  try {
    await galleryStore.deleteImage(providerId, imageId)

    showSuccess('Image deleted successfully!')
    closeDeleteModal()
  } catch (error) {
    console.error('Error deleting image:', error)
    showError('Failed to delete image. Please try again.')
  } finally {
    isDeleting.value = false
  }
}

async function handleBulkDelete() {
  const providerId = currentProviderId.value
  if (!providerId || selectedImages.value.length === 0) return

  const confirmed = confirm(
    `Are you sure you want to delete ${selectedImages.value.length} selected image(s)? This action cannot be undone.`
  )

  if (!confirmed) return

  try {
    // Delete each selected image
    await Promise.all(
      selectedImages.value.map((imageId) => galleryStore.deleteImage(providerId, imageId))
    )

    showSuccess(`Successfully deleted ${selectedImages.value.length} image(s)!`)
    selectedImages.value = []
    galleryGridRef.value?.clearSelection()
  } catch (error) {
    console.error('Error deleting images:', error)
    showError('Failed to delete some images. Please try again.')
  }
}

async function handleReorder(imageOrders: Array<{ imageId: string; newOrder: number }>) {
  const providerId = currentProviderId.value
  if (!providerId) return

  try {
    await galleryStore.reorderImages(providerId, imageOrders)
    showSuccess('Gallery images reordered successfully!')
  } catch (error) {
    console.error('Error reordering images:', error)
    showError('Failed to reorder images. Please try again.')
  }
}

function handleSelectionChange(selectedIds: string[]) {
  selectedImages.value = selectedIds
}

function showSuccess(message: string) {
  successMessage.value = message
  setTimeout(() => {
    successMessage.value = null
  }, 3000)
}

function showError(message: string) {
  galleryStore.error = message
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function goBack() {
  router.push({ name: 'ProviderDashboard' })
}
</script>

<style scoped>
.gallery-view {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
  gap: 2rem;
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
  gap: 2rem;
  margin-bottom: 2rem;
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
}

.upload-status {
  margin-top: 1rem;
  padding: 1rem;
  background-color: #f3f4f6;
  border-radius: 0.5rem;
  text-align: center;
  color: #6b7280;
  font-size: 0.875rem;
}

.edit-form {
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
  outline: none;
  border-color: #8b5cf6;
  ring: 2px;
  ring-color: rgba(139, 92, 246, 0.2);
}

.form-group small {
  font-size: 0.75rem;
  color: #6b7280;
}

.delete-message {
  color: #6b7280;
  margin: 0 0 1.5rem 0;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 1rem;
}

/* Responsive */
@media (max-width: 768px) {
  .gallery-view {
    padding: 1rem;
  }

  .page-header {
    flex-direction: column;
  }

  .section-header {
    flex-direction: column;
  }

  .modal-actions {
    flex-direction: column;
  }

  .modal-actions button {
    width: 100%;
  }
}
</style>
