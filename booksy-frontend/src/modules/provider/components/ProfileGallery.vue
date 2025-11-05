<template>
  <div class="profile-gallery">
    <!-- Error Alert -->
    <div v-if="error" class="alert alert-error">
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 20 20"
        fill="currentColor"
        class="alert-icon"
      >
        <path
          fill-rule="evenodd"
          d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z"
          clip-rule="evenodd"
        />
      </svg>
      <span>{{ error }}</span>
      <button type="button" class="alert-close" @click="error = null">×</button>
    </div>

    <!-- Success Alert -->
    <div v-if="successMessage" class="alert alert-success">
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 20 20"
        fill="currentColor"
        class="alert-icon"
      >
        <path
          fill-rule="evenodd"
          d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z"
          clip-rule="evenodd"
        />
      </svg>
      <span>{{ successMessage }}</span>
      <button type="button" class="alert-close" @click="successMessage = null">×</button>
    </div>

    <!-- Gallery Upload Area -->
    <div class="gallery-section">
      <GalleryUpload
        :max-images="maxImages"
        :is-uploading="isUploading"
        :upload-progress="uploadProgress"
        @upload="handleUpload"
      />

      <!-- Image Preview Grid -->
      <div v-if="localGalleryImages.length > 0" class="gallery-preview">
        <div class="gallery-header">
          <h3 class="gallery-title">تصاویر آپلود شده</h3>
          <p class="gallery-count">{{ localGalleryImages.length }} تصویر</p>
        </div>

        <div class="image-grid">
          <div
            v-for="image in localGalleryImages"
            :key="image.id"
            class="image-card"
          >
            <div class="image-wrapper">
              <img :src="image.mediumUrl || image.originalUrl" :alt="image.altText || 'تصویر گالری'" class="image" />
              <button
                type="button"
                class="delete-btn"
                :disabled="isDeleting"
                @click="handleDeleteImage(image.id)"
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
            <div v-if="image.caption" class="image-caption">{{ image.caption }}</div>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else class="empty-state">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          class="empty-icon"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
          />
        </svg>
        <p class="empty-text">هنوز تصویری آپلود نشده است</p>
        <p class="empty-hint">تصاویر نمونه کار خود را آپلود کنید تا مشتریان بتوانند کیفیت خدمات شما را ببینند</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { useGalleryStore } from '@/modules/provider/stores/gallery.store'
import GalleryUpload from '@/modules/provider/components/gallery/GalleryUpload.vue'
import type { GalleryImage } from '@/modules/provider/types/gallery.types'

const providerStore = useProviderStore()
const galleryStore = useGalleryStore()

// State
const maxImages = 50
const error = ref<string | null>(null)
const successMessage = ref<string | null>(null)
const isUploading = ref(false)
const isDeleting = ref(false)
const uploadProgress = ref(0)
const localGalleryImages = ref<GalleryImage[]>([])

// Computed
const currentProviderId = computed(() => providerStore.currentProvider?.id)

// Lifecycle
onMounted(async () => {
  const providerId = currentProviderId.value
  if (providerId) {
    try {
      await galleryStore.fetchGalleryImages(providerId)
      localGalleryImages.value = [...galleryStore.galleryImages]
    } catch (err) {
      console.error('Error loading gallery:', err)
      // Don't show error - it's OK if provider doesn't have images yet
    }
  }
})

// Methods
async function handleUpload(files: File[]) {
  const providerId = currentProviderId.value

  if (!providerId) {
    error.value = 'خطا: ارائه‌دهنده یافت نشد'
    return
  }

  isUploading.value = true
  error.value = null
  uploadProgress.value = 0

  try {
    const uploadedImages = await galleryStore.uploadImages(providerId, files)

    localGalleryImages.value.push(...uploadedImages)

    successMessage.value = `${uploadedImages.length} تصویر با موفقیت آپلود شد!`
    setTimeout(() => {
      successMessage.value = null
    }, 3000)
  } catch (err: any) {
    console.error('Error uploading images:', err)
    error.value = err.message || 'خطا در آپلود تصاویر. لطفاً دوباره تلاش کنید.'
  } finally {
    isUploading.value = false
    uploadProgress.value = 0
  }
}

async function handleDeleteImage(imageId: string) {
  const providerId = currentProviderId.value

  if (!providerId) {
    error.value = 'خطا در حذف تصویر'
    return
  }

  isDeleting.value = true
  error.value = null

  try {
    await galleryStore.deleteImage(providerId, imageId)
    localGalleryImages.value = localGalleryImages.value.filter((img) => img.id !== imageId)

    successMessage.value = 'تصویر با موفقیت حذف شد'
    setTimeout(() => {
      successMessage.value = null
    }, 2000)
  } catch (err: any) {
    console.error('Error deleting image:', err)
    error.value = err.message || 'خطا در حذف تصویر. لطفاً دوباره تلاش کنید.'
  } finally {
    isDeleting.value = false
  }
}
</script>

<style scoped>
.profile-gallery {
  padding: 0;
}

/* Alerts */
.alert {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
  border-radius: 0.5rem;
  margin-bottom: 1.5rem;
  font-size: 0.875rem;
}

.alert-error {
  background-color: #fef2f2;
  border: 1px solid #fecaca;
  color: #dc2626;
}

.alert-success {
  background-color: #f0fdf4;
  border: 1px solid #bbf7d0;
  color: #16a34a;
}

.alert-icon {
  width: 1.25rem;
  height: 1.25rem;
  flex-shrink: 0;
}

.alert-close {
  margin-right: auto;
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: inherit;
  opacity: 0.6;
  transition: opacity 0.2s;
}

.alert-close:hover {
  opacity: 1;
}

/* Gallery Section */
.gallery-section {
  margin-bottom: 0;
}

/* Gallery Preview */
.gallery-preview {
  margin-top: 2rem;
}

.gallery-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.gallery-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
}

.gallery-count {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

/* Image Grid */
.image-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 1rem;
}

.image-card {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.image-wrapper {
  position: relative;
  aspect-ratio: 1;
  border-radius: 0.5rem;
  overflow: hidden;
  background-color: #f3f4f6;
}

.image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.delete-btn {
  position: absolute;
  top: 0.5rem;
  left: 0.5rem;
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: rgba(239, 68, 68, 0.9);
  color: white;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: background-color 0.2s;
}

.delete-btn:hover:not(:disabled) {
  background-color: rgba(220, 38, 38, 1);
}

.delete-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.delete-btn svg {
  width: 1.25rem;
  height: 1.25rem;
}

.image-caption {
  font-size: 0.75rem;
  color: #6b7280;
  text-align: center;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 2rem;
  background-color: #f9fafb;
  border: 2px dashed #d1d5db;
  border-radius: 0.75rem;
  margin-top: 2rem;
}

.empty-icon {
  width: 3rem;
  height: 3rem;
  color: #9ca3af;
  margin-bottom: 1rem;
}

.empty-text {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
  margin: 0 0 0.5rem 0;
}

.empty-hint {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
  text-align: center;
  max-width: 400px;
}

/* Responsive */
@media (max-width: 768px) {
  .image-grid {
    grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
    gap: 0.75rem;
  }
}
</style>
