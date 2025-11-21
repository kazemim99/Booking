<template>
  <div class="gallery-upload">
    <div
      class="upload-area"
      :class="{
        'is-dragging': isDragging,
        'is-uploading': isUploading,
        'is-disabled': maxImages <= 0
      }"
      @click="triggerFileInput"
      @dragover.prevent="handleDragOver"
      @dragleave.prevent="handleDragLeave"
      @drop.prevent="handleDrop"
    >
      <input
        ref="fileInput"
        type="file"
        accept="image/jpeg,image/png,image/webp"
        multiple
        class="file-input"
        @change="handleFileSelect"
      />

      <div v-if="!isUploading" class="upload-placeholder">
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
              d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
            />
          </svg>
        </div>
        <h3 class="upload-title">تصاویر را اینجا بکشید و رها کنید</h3>
        <p class="upload-subtitle">یا برای انتخاب کلیک کنید</p>
        <p class="upload-hint">
          فرمت‌های مجاز: JPG، PNG، WebP | حداکثر حجم: {{ maxFileSize }}MB
        </p>
        <p v-if="currentCount > 0" class="upload-count">
          {{ currentCount }} از {{ totalLimit }} تصویر آپلود شده
          <span v-if="maxImages > 0" class="remaining">({{ maxImages }} جای خالی باقی مانده)</span>
          <span v-else class="limit-reached">حداکثر تعداد رسیده است</span>
        </p>
      </div>

      <div v-else class="upload-progress">
        <div class="progress-bar-container">
          <div class="progress-bar" :style="{ width: `${overallProgress}%` }"></div>
        </div>
        <p class="progress-text">در حال آپلود {{ uploadingFiles.length }} تصویر... {{ overallProgress }}%</p>
      </div>
    </div>

    <!-- File validation errors -->
    <div v-if="validationErrors.length > 0" class="validation-errors">
      <div v-for="(error, index) in validationErrors" :key="index" class="error-item">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 20 20"
          fill="currentColor"
          class="error-icon"
        >
          <path
            fill-rule="evenodd"
            d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z"
            clip-rule="evenodd"
          />
        </svg>
        <span>{{ error }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

// Props
interface Props {
  maxImages?: number // Remaining slots available
  currentCount?: number // Current number of images
  totalLimit?: number // Total limit (e.g., 20)
  maxFileSize?: number // in MB
  acceptedFormats?: string[]
  isUploading?: boolean
  uploadProgress?: number
}

const props = withDefaults(defineProps<Props>(), {
  maxImages: 20,
  currentCount: 0,
  totalLimit: 20,
  maxFileSize: 10,
  acceptedFormats: () => ['image/jpeg', 'image/png', 'image/webp'],
  isUploading: false,
  uploadProgress: 0
})

// Emits
const emit = defineEmits<{
  upload: [files: File[]]
}>()

// State
const fileInput = ref<HTMLInputElement | null>(null)
const isDragging = ref(false)
const validationErrors = ref<string[]>([])
const uploadingFiles = ref<File[]>([])

// Computed
const overallProgress = computed(() => Math.round(props.uploadProgress))

// Methods
function triggerFileInput() {
  if (!props.isUploading && props.maxImages > 0) {
    fileInput.value?.click()
  }
}

function handleDragOver(event: DragEvent) {
  if (!props.isUploading && props.maxImages > 0) {
    isDragging.value = true
  }
}

function handleDragLeave() {
  isDragging.value = false
}

function handleDrop(event: DragEvent) {
  isDragging.value = false

  if (props.isUploading || props.maxImages <= 0) {
    return
  }

  const files = Array.from(event.dataTransfer?.files || [])
  processFiles(files)
}

function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  const files = Array.from(input.files || [])
  processFiles(files)

  // Reset input so the same file can be selected again
  input.value = ''
}

function processFiles(files: File[]) {
  validationErrors.value = []
  const validFiles: File[] = []

  // Validate each file
  files.forEach((file) => {
    const errors = validateFile(file)
    if (errors.length > 0) {
      validationErrors.value.push(...errors)
    } else {
      validFiles.push(file)
    }
  })

  // Check total count
  if (validFiles.length > props.maxImages) {
    validationErrors.value.push(`شما تنها می‌توانید حداکثر ${props.maxImages} تصویر را همزمان آپلود کنید`)
    return
  }

  if (validFiles.length > 0) {
    uploadingFiles.value = validFiles
    emit('upload', validFiles)
  }
}

function validateFile(file: File): string[] {
  const errors: string[] = []

  // Check file type
  if (!props.acceptedFormats.includes(file.type)) {
    errors.push(`${file.name}: فرمت فایل نامعتبر است. فرمت‌های مجاز: JPG، PNG، WebP`)
  }

  // Check file size
  const fileSizeMB = file.size / (1024 * 1024)
  if (fileSizeMB > props.maxFileSize) {
    errors.push(`${file.name}: حجم فایل بیش از حد مجاز ${props.maxFileSize}MB است`)
  }

  return errors
}
</script>

<style scoped>
.gallery-upload {
  width: 100%;
}

.upload-area {
  border: 2px dashed #d1d5db;
  border-radius: 0.75rem;
  background-color: #f9fafb;
  padding: 3rem 2rem;
  text-align: center;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
}

.upload-area:hover:not(.is-uploading) {
  border-color: #8b5cf6;
  background-color: #f5f3ff;
}

.upload-area.is-dragging {
  border-color: #8b5cf6;
  background-color: #ede9fe;
  transform: scale(1.02);
}

.upload-area.is-uploading {
  cursor: not-allowed;
  opacity: 0.8;
}

.upload-area.is-disabled {
  cursor: not-allowed;
  opacity: 0.5;
  pointer-events: none;
}

.file-input {
  display: none;
}

.upload-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.upload-icon {
  width: 4rem;
  height: 4rem;
  color: #8b5cf6;
}

.upload-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
}

.upload-subtitle {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.upload-hint {
  font-size: 0.75rem;
  color: #9ca3af;
  margin: 0;
}

.upload-count {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0.5rem 0 0 0;
  font-weight: 500;
}

.upload-count .remaining {
  color: #10b981;
}

.upload-count .limit-reached {
  color: #ef4444;
  font-weight: 600;
}

.upload-progress {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.progress-bar-container {
  width: 100%;
  max-width: 400px;
  height: 8px;
  background-color: #e5e7eb;
  border-radius: 9999px;
  overflow: hidden;
}

.progress-bar {
  height: 100%;
  background-color: #8b5cf6;
  transition: width 0.3s ease;
}

.progress-text {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.validation-errors {
  margin-top: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.error-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem;
  background-color: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 0.375rem;
  color: #dc2626;
  font-size: 0.875rem;
}

.error-icon {
  width: 1.25rem;
  height: 1.25rem;
  flex-shrink: 0;
}
</style>
