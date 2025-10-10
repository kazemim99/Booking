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
      <Button variant="secondary" @click="goBack">← Back to Onboarding</Button>
    </div>

    <!-- Loading State -->
    <div v-if="providerStore.isLoading" class="loading-state">
      <Spinner />
      <p>Loading gallery...</p>
    </div>

    <!-- Error State -->
    <Alert
      v-if="errorMessage"
      type="error"
      :message="errorMessage"
      @dismiss="errorMessage = null"
    />

    <!-- Success Message -->
    <Alert
      v-if="successMessage"
      type="success"
      :message="successMessage"
      @dismiss="successMessage = null"
    />

    <div v-if="!providerStore.isLoading" class="gallery-content">
      <!-- Brand Assets Section -->
      <Card class="section">
        <h2 class="section-title">Brand Assets</h2>
        <p class="section-description">
          Upload your business logo and cover image to enhance your profile
        </p>

        <div class="brand-assets">
          <div class="brand-asset-item">
            <label class="asset-label">Business Logo</label>
            <div
              class="asset-upload-area"
              :class="{ 'has-image': logoUrl }"
              @click="triggerLogoUpload"
            >
              <div v-if="logoUrl" class="asset-preview">
                <img :src="logoUrl" alt="Business Logo" class="preview-image" />
                <div class="preview-overlay">
                  <Button variant="ghost" size="small" nativeType="button">Change Logo</Button>
                </div>
              </div>
              <div v-else class="upload-placeholder">
                <div class="upload-icon">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M12 6v6m0 0v6m0-6h6m-6 0H6"
                    />
                  </svg>
                </div>
                <div class="upload-text">
                  <p>Upload Logo</p>
                  <span>Square image recommended (JPG, PNG)</span>
                </div>
              </div>
              <input
                ref="logoFileInput"
                type="file"
                accept="image/*"
                class="file-input"
                @change="handleLogoUpload"
              />
            </div>
            <div class="asset-form-group">
              <label>Logo URL</label>
              <div class="input-with-button">
                <input
                  type="text"
                  v-model="logoUrl"
                  placeholder="https://example.com/logo.png"
                  class="form-input"
                />
                <Button
                  variant="secondary"
                  size="small"
                  nativeType="button"
                  @click="applyLogoUrl"
                  :disabled="!logoUrl"
                >
                  Apply
                </Button>
              </div>
              <small>You can paste an image URL or upload a file</small>
            </div>
          </div>

          <div class="brand-asset-item">
            <label class="asset-label">Cover Image</label>
            <div
              class="asset-upload-area cover-area"
              :class="{ 'has-image': coverImageUrl }"
              @click="triggerCoverUpload"
            >
              <div v-if="coverImageUrl" class="asset-preview">
                <img :src="coverImageUrl" alt="Cover Image" class="preview-image" />
                <div class="preview-overlay">
                  <Button variant="ghost" size="small" nativeType="button">Change Cover</Button>
                </div>
              </div>
              <div v-else class="upload-placeholder">
                <div class="upload-icon">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M12 6v6m0 0v6m0-6h6m-6 0H6"
                    />
                  </svg>
                </div>
                <div class="upload-text">
                  <p>Upload Cover Image</p>
                  <span>1280×400px recommended (JPG, PNG)</span>
                </div>
              </div>
              <input
                ref="coverFileInput"
                type="file"
                accept="image/*"
                class="file-input"
                @change="handleCoverUpload"
              />
            </div>
            <div class="asset-form-group">
              <label>Cover Image URL</label>
              <div class="input-with-button">
                <input
                  type="text"
                  v-model="coverImageUrl"
                  placeholder="https://example.com/cover.jpg"
                  class="form-input"
                />
                <Button
                  variant="secondary"
                  size="small"
                  nativeType="button"
                  @click="applyCoverUrl"
                  :disabled="!coverImageUrl"
                >
                  Apply
                </Button>
              </div>
              <small>You can paste an image URL or upload a file</small>
            </div>
          </div>
        </div>
      </Card>

      <!-- Portfolio Section -->
      <Card class="section">
        <div class="section-header">
          <div>
            <h2 class="section-title">Portfolio Gallery</h2>
            <p class="section-description">
              Showcase your work with high-quality photos to attract more customers
            </p>
          </div>
          <Button variant="primary" nativeType="button" @click="triggerGalleryUpload">
            + Add Photos
          </Button>
          <input
            ref="galleryFileInput"
            type="file"
            accept="image/*"
            multiple
            class="file-input"
            @change="handleGalleryUpload"
          />
        </div>

        <div v-if="galleryImages.length > 0" class="gallery-grid">
          <div
            v-for="(image, index) in galleryImages"
            :key="index"
            class="gallery-item"
          >
            <div class="gallery-image-container">
              <img :src="image.url" :alt="`Gallery image ${index + 1}`" class="gallery-image" />
              <div class="gallery-actions">
                <Button
                  variant="ghost"
                  size="small"
                  nativeType="button"
                  @click="editGalleryImage(index)"
                  title="Edit Image Details"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="currentColor"
                    width="16"
                    height="16"
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
                  @click="removeGalleryImage(index)"
                  title="Remove Image"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="currentColor"
                    width="16"
                    height="16"
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
            <div class="gallery-item-caption">
              {{ image.caption || `Image ${index + 1}` }}
            </div>
          </div>
        </div>

        <div v-else class="gallery-empty">
          <div class="empty-icon">🖼️</div>
          <h3>No Portfolio Photos Yet</h3>
          <p>Upload photos of your work to attract more customers</p>
          <Button variant="primary" nativeType="button" @click="triggerGalleryUpload">
            + Add Your First Photo
          </Button>
        </div>
      </Card>

      <!-- Workspace Photos Section -->
      <Card class="section">
        <div class="section-header">
          <div>
            <h2 class="section-title">Workspace Photos</h2>
            <p class="section-description">
              Show customers what your business space looks like
            </p>
          </div>
          <Button variant="primary" nativeType="button" @click="triggerWorkspaceUpload">
            + Add Photos
          </Button>
          <input
            ref="workspaceFileInput"
            type="file"
            accept="image/*"
            multiple
            class="file-input"
            @change="handleWorkspaceUpload"
          />
        </div>

        <div v-if="workspaceImages.length > 0" class="gallery-grid">
          <div
            v-for="(image, index) in workspaceImages"
            :key="index"
            class="gallery-item"
          >
            <div class="gallery-image-container">
              <img :src="image.url" :alt="`Workspace image ${index + 1}`" class="gallery-image" />
              <div class="gallery-actions">
                <Button
                  variant="ghost"
                  size="small"
                  nativeType="button"
                  @click="editWorkspaceImage(index)"
                  title="Edit Image Details"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="currentColor"
                    width="16"
                    height="16"
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
                  @click="removeWorkspaceImage(index)"
                  title="Remove Image"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="currentColor"
                    width="16"
                    height="16"
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
            <div class="gallery-item-caption">
              {{ image.caption || `Image ${index + 1}` }}
            </div>
          </div>
        </div>

        <div v-else class="gallery-empty">
          <div class="empty-icon">🏠</div>
          <h3>No Workspace Photos Yet</h3>
          <p>Upload photos of your business space to help customers find you</p>
          <Button variant="primary" nativeType="button" @click="triggerWorkspaceUpload">
            + Add Your First Photo
          </Button>
        </div>
      </Card>

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
            />
            <small>A brief description of what's shown in the image</small>
          </div>

          <div class="form-group">
            <label for="imageAltText">Alt Text</label>
            <input
              id="imageAltText"
              type="text"
              v-model="editingImage.altText"
              placeholder="Enter alternative text for this image"
              class="form-input"
            />
            <small>Describe the image for accessibility purposes</small>
          </div>

          <div class="form-group">
            <label>Tags</label>
            <input
              type="text"
              v-model="editingImage.tagsInput"
              placeholder="Enter tags, separated by commas"
              class="form-input"
            />
            <small>Add tags to help categorize your images</small>
          </div>

          <div class="modal-actions">
            <Button type="button" variant="secondary" @click="closeImageEditModal">
              Cancel
            </Button>
            <Button type="submit" variant="primary">
              Save Changes
            </Button>
          </div>
        </form>
      </Modal>
    </div>

    <!-- Form Actions -->
    <div class="form-actions">
      <Button type="button" variant="secondary" @click="goBack">Cancel</Button>
      <Button type="button" variant="primary" @click="saveGallery" :disabled="isSaving">
        {{ isSaving ? 'Saving...' : 'Save Gallery' }}
      </Button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { Button, Card, Modal, Alert, Spinner } from '@/shared/components'

const router = useRouter()
const providerStore = useProviderStore()

// State
const isSaving = ref(false)
const errorMessage = ref<string | null>(null)
const successMessage = ref<string | null>(null)

// File input refs
const logoFileInput = ref<HTMLInputElement | null>(null)
const coverFileInput = ref<HTMLInputElement | null>(null)
const galleryFileInput = ref<HTMLInputElement | null>(null)
const workspaceFileInput = ref<HTMLInputElement | null>(null)

// Image URLs
const logoUrl = ref('')
const coverImageUrl = ref('')

// Gallery images
interface GalleryImage {
  url: string
  caption?: string
  altText?: string
  tags?: string[]
  tagsInput?: string
  type?: 'portfolio' | 'workspace'
}

const galleryImages = ref<GalleryImage[]>([])
const workspaceImages = ref<GalleryImage[]>([])

// Image edit modal
const showImageEditModal = ref(false)
const editingImage = reactive<GalleryImage & { index: number; collection: 'gallery' | 'workspace' }>({
  url: '',
  caption: '',
  altText: '',
  tags: [],
  tagsInput: '',
  index: -1,
  collection: 'gallery'
})

// Load provider data
onMounted(async () => {
  try {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const provider = providerStore.currentProvider
    if (!provider) {
      errorMessage.value = 'Provider profile not found. Please register as a provider first.'
      return
    }

    // Load logo and cover image
    logoUrl.value = provider.profile.logoUrl || ''
    coverImageUrl.value = provider.profile.coverImageUrl || ''

    // In a real implementation, we would load gallery and workspace images from the provider
    // For demo purposes, we'll initialize with empty arrays
    // galleryImages.value = provider.galleryImages || []
    // workspaceImages.value = provider.workspaceImages || []
  } catch (error) {
    console.error('Error loading provider data:', error)
    errorMessage.value = 'Failed to load provider data. Please try again.'
  }
})

// File upload functions
function triggerLogoUpload() {
  logoFileInput.value?.click()
}

function triggerCoverUpload() {
  coverFileInput.value?.click()
}

function triggerGalleryUpload() {
  galleryFileInput.value?.click()
}

function triggerWorkspaceUpload() {
  workspaceFileInput.value?.click()
}

function handleLogoUpload(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    const file = input.files[0]

    // In a real implementation, we would upload the file to a server
    // For demo purposes, we'll create a temporary URL
    const tempUrl = URL.createObjectURL(file)
    logoUrl.value = tempUrl

    // Reset the input so the same file can be selected again if needed
    input.value = ''
  }
}

function handleCoverUpload(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    const file = input.files[0]

    // In a real implementation, we would upload the file to a server
    // For demo purposes, we'll create a temporary URL
    const tempUrl = URL.createObjectURL(file)
    coverImageUrl.value = tempUrl

    // Reset the input
    input.value = ''
  }
}

function handleGalleryUpload(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    const files = Array.from(input.files)

    // Add each file as a gallery image
    files.forEach(file => {
      const tempUrl = URL.createObjectURL(file)
      galleryImages.value.push({
        url: tempUrl,
        caption: file.name.replace(/\.[^/.]+$/, ""), // Remove file extension
        type: 'portfolio'
      })
    })

    // Reset the input
    input.value = ''

    // Show success message
    successMessage.value = `Added ${files.length} image${files.length > 1 ? 's' : ''} to your portfolio gallery.`
    setTimeout(() => {
      successMessage.value = null
    }, 3000)
  }
}

function handleWorkspaceUpload(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    const files = Array.from(input.files)

    // Add each file as a workspace image
    files.forEach(file => {
      const tempUrl = URL.createObjectURL(file)
      workspaceImages.value.push({
        url: tempUrl,
        caption: file.name.replace(/\.[^/.]+$/, ""), // Remove file extension
        type: 'workspace'
      })
    })

    // Reset the input
    input.value = ''

    // Show success message
    successMessage.value = `Added ${files.length} image${files.length > 1 ? 's' : ''} to your workspace gallery.`
    setTimeout(() => {
      successMessage.value = null
    }, 3000)
  }
}

// URL input functions
function applyLogoUrl() {
  // In a real implementation, we would validate the URL
  // For demo purposes, we'll just use the URL as is
  successMessage.value = 'Logo URL applied successfully.'
  setTimeout(() => {
    successMessage.value = null
  }, 3000)
}

function applyCoverUrl() {
  // In a real implementation, we would validate the URL
  // For demo purposes, we'll just use the URL as is
  successMessage.value = 'Cover image URL applied successfully.'
  setTimeout(() => {
    successMessage.value = null
  }, 3000)
}

// Gallery image functions
function editGalleryImage(index: number) {
  const image = galleryImages.value[index]

  editingImage.url = image.url
  editingImage.caption = image.caption || ''
  editingImage.altText = image.altText || ''
  editingImage.tags = image.tags || []
  editingImage.tagsInput = image.tags?.join(', ') || ''
  editingImage.index = index
  editingImage.collection = 'gallery'

  showImageEditModal.value = true
}

function editWorkspaceImage(index: number) {
  const image = workspaceImages.value[index]

  editingImage.url = image.url
  editingImage.caption = image.caption || ''
  editingImage.altText = image.altText || ''
  editingImage.tags = image.tags || []
  editingImage.tagsInput = image.tags?.join(', ') || ''
  editingImage.index = index
  editingImage.collection = 'workspace'

  showImageEditModal.value = true
}

function closeImageEditModal() {
  showImageEditModal.value = false
}

function saveImageEdit() {
  // Parse tags from comma-separated string
  const tags = editingImage.tagsInput
    ? editingImage.tagsInput
        .split(',')
        .map(tag => tag.trim())
        .filter(tag => tag !== '')
    : []

  const updatedImage: GalleryImage = {
    url: editingImage.url,
    caption: editingImage.caption,
    altText: editingImage.altText,
    tags
  }

  // Update the appropriate collection
  if (editingImage.collection === 'gallery') {
    galleryImages.value[editingImage.index] = updatedImage
  } else {
    workspaceImages.value[editingImage.index] = updatedImage
  }

  closeImageEditModal()

  successMessage.value = 'Image details updated successfully.'
  setTimeout(() => {
    successMessage.value = null
  }, 3000)
}

function removeGalleryImage(index: number) {
  // In a real implementation, we might want to confirm before removing
  galleryImages.value.splice(index, 1)

  successMessage.value = 'Image removed from portfolio gallery.'
  setTimeout(() => {
    successMessage.value = null
  }, 3000)
}

function removeWorkspaceImage(index: number) {
  // In a real implementation, we might want to confirm before removing
  workspaceImages.value.splice(index, 1)

  successMessage.value = 'Image removed from workspace gallery.'
  setTimeout(() => {
    successMessage.value = null
  }, 3000)
}

// Save all gallery changes
async function saveGallery() {
  isSaving.value = true

  try {
    const provider = providerStore.currentProvider
    if (!provider) {
      throw new Error('Provider not found. Please try refreshing the page.')
    }

    // Prepare update data
    const updateData = {
      logoUrl: logoUrl.value || undefined,
      coverImageUrl: coverImageUrl.value || undefined,
      // In a real implementation, we would also save the gallery and workspace images
      // galleryImages: galleryImages.value,
      // workspaceImages: workspaceImages.value
    }

    // Update provider
    const updatedProvider = await providerStore.updateProvider(provider.id, updateData)

    // Check if update was successful
    if (!updatedProvider) {
      // Check if there's an error in the store
      if (providerStore.error) {
        throw new Error(providerStore.error)
      }
      throw new Error('Failed to save gallery. Please try again.')
    }

    successMessage.value = 'Gallery saved successfully!'

    // Redirect after a short delay
    setTimeout(() => {
      router.push({ name: 'ProviderOnboarding' })
    }, 1500)
  } catch (error) {
    console.error('Error saving gallery:', error)
    errorMessage.value = error instanceof Error ? error.message : 'Failed to save gallery'
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    isSaving.value = false
  }
}

// Navigation
function goBack() {
  router.push({ name: 'ProviderOnboarding' })
}
</script>

<style scoped>
.gallery-view {
  max-width: 1000px;
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
  margin: 0 0 1.5rem 0;
}

/* Brand Assets */
.brand-assets {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 2rem;
}

.brand-asset-item {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.asset-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #111827;
}

.asset-upload-area {
  width: 100%;
  aspect-ratio: 1 / 1;
  border: 2px dashed #d1d5db;
  border-radius: 0.5rem;
  background-color: #f9fafb;
  cursor: pointer;
  overflow: hidden;
  position: relative;
  transition: all 0.2s ease;
}

.asset-upload-area:hover {
  border-color: #8b5cf6;
  background-color: #f5f3ff;
}

.asset-upload-area.has-image {
  border: none;
  background-color: transparent;
}

.cover-area {
  aspect-ratio: 3 / 1;
}

.asset-preview {
  width: 100%;
  height: 100%;
  position: relative;
}

.preview-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 0.5rem;
}

.preview-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.4);
  border-radius: 0.5rem;
  opacity: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: opacity 0.2s ease;
}

.asset-preview:hover .preview-overlay {
  opacity: 1;
}

.upload-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
}

.upload-icon {
  width: 3rem;
  height: 3rem;
  color: #8b5cf6;
}

.upload-text {
  text-align: center;
}

.upload-text p {
  font-weight: 600;
  color: #111827;
  margin: 0 0 0.25rem 0;
}

.upload-text span {
  font-size: 0.75rem;
  color: #6b7280;
}

.asset-form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.asset-form-group label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #6b7280;
}

.asset-form-group small {
  font-size: 0.75rem;
  color: #6b7280;
}

.input-with-button {
  display: flex;
  gap: 0.5rem;
}

.form-input {
  flex: 1;
  padding: 0.625rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  color: #111827;
}

.file-input {
  display: none;
}

/* Gallery Grid */
.gallery-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
}

.gallery-item {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.gallery-image-container {
  position: relative;
  aspect-ratio: 1 / 1;
  border-radius: 0.5rem;
  overflow: hidden;
}

.gallery-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.gallery-actions {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.gallery-image-container:hover .gallery-actions {
  opacity: 1;
}

.gallery-item-caption {
  font-size: 0.875rem;
  color: #4b5563;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
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

/* Modal Form */
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

/* Form Actions */
.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding-top: 2rem;
  border-top: 1px solid #e5e7eb;
  margin-top: 2rem;
}

/* Responsive Styles */
@media (max-width: 768px) {
  .gallery-view {
    padding: 1rem;
  }

  .page-header {
    flex-direction: column;
  }

  .brand-assets {
    grid-template-columns: 1fr;
  }

  .gallery-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .section-header {
    flex-direction: column;
  }

  .modal-actions, .form-actions {
    flex-direction: column;
  }

  .modal-actions button, .form-actions button {
    width: 100%;
  }
}