<template>
  <div class="profile-gallery">
    <!-- Success/Error Alerts -->
    <div v-if="error" class="alert alert-error">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="alert-icon">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd" />
      </svg>
      <span>{{ error }}</span>
      <button type="button" class="alert-close" @click="error = null">×</button>
    </div>

    <div v-if="successMessage" class="alert alert-success">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="alert-icon">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd" />
      </svg>
      <span>{{ successMessage }}</span>
      <button type="button" class="alert-close" @click="successMessage = null">×</button>
    </div>

    <!-- Gallery Manager - New Comprehensive Component -->
    <GalleryManager
      v-if="currentProviderId"
      :max-images="50"
      :max-size-m-b="5"
      @images-updated="handleImagesUpdated"
      @upload-success="handleUploadSuccess"
      @upload-error="handleUploadError"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import GalleryManager from '@/modules/provider/components/gallery/GalleryManager.vue'
import type { GalleryImage } from '@/modules/provider/types/gallery.types'

const providerStore = useProviderStore()

// State
const error = ref<string | null>(null)
const successMessage = ref<string | null>(null)

// Computed
const currentProviderId = computed(() => providerStore.currentProvider?.id)

// Event Handlers
function handleImagesUpdated(images: GalleryImage[]): void {
  console.log(`[ProfileGallery] Gallery updated: ${images.length} images`)
}

function handleUploadSuccess(images: GalleryImage[]): void {
  successMessage.value = `${images.length} تصویر با موفقیت آپلود شد!`
  setTimeout(() => {
    successMessage.value = null
  }, 3000)
}

function handleUploadError(err: Error): void {
  console.error('[ProfileGallery] Upload error:', err)
  error.value = err.message || 'خطا در آپلود تصاویر. لطفاً دوباره تلاش کنید.'
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
</style>
