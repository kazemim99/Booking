<template>
  <div class="single-image-upload">
    <div
      class="upload-area"
      :class="{
        'is-dragging': isDragging,
        'is-uploading': isUploading,
        'has-image': previewUrl
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
        class="file-input"
        @change="handleFileSelect"
      />

      <!-- Preview Image -->
      <div v-if="previewUrl" class="image-preview">
        <img :src="previewUrl" :alt="altText || 'تصویر'" />
        <div v-if="isUploading" class="upload-overlay">
          <div class="upload-spinner"></div>
          <p class="upload-text">در حال آپلود...</p>
        </div>
        <button
          v-else
          type="button"
          class="change-button"
          @click.stop="triggerFileInput"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            <path d="M2.695 14.763l-1.262 3.154a.5.5 0 00.65.65l3.155-1.262a4 4 0 001.343-.885L17.5 5.5a2.121 2.121 0 00-3-3L3.58 13.42a4 4 0 00-.885 1.343z" />
          </svg>
          تغییر تصویر
        </button>
      </div>

      <!-- Upload Placeholder -->
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
              d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
            />
          </svg>
        </div>
        <h3 class="upload-title">{{ uploadTitle }}</h3>
        <p class="upload-subtitle">تصویر را اینجا بکشید و رها کنید</p>
        <p class="upload-hint">یا برای انتخاب کلیک کنید</p>
        <p class="upload-format">
          فرمت‌های مجاز: JPG، PNG، WebP | حداکثر حجم: {{ maxFileSize }}MB
        </p>
      </div>
    </div>

    <!-- Validation Errors -->
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
  modelValue?: string | null // Preview URL
  uploadTitle?: string
  altText?: string
  maxFileSize?: number // in MB
  acceptedFormats?: string[]
  isUploading?: boolean
  aspectRatio?: 'square' | 'circle' | 'auto'
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: null,
  uploadTitle: 'آپلود تصویر',
  altText: 'تصویر',
  maxFileSize: 5,
  acceptedFormats: () => ['image/jpeg', 'image/png', 'image/webp'],
  isUploading: false,
  aspectRatio: 'square'
})

// Emits
const emit = defineEmits<{
  'update:modelValue': [value: string]
  upload: [file: File]
}>()

// State
const fileInput = ref<HTMLInputElement | null>(null)
const isDragging = ref(false)
const validationErrors = ref<string[]>([])

// Computed
const previewUrl = computed(() => props.modelValue)

// Methods
function triggerFileInput() {
  if (!props.isUploading) {
    fileInput.value?.click()
  }
}

function handleDragOver(event: DragEvent) {
  if (!props.isUploading) {
    isDragging.value = true
  }
}

function handleDragLeave() {
  isDragging.value = false
}

function handleDrop(event: DragEvent) {
  isDragging.value = false

  if (props.isUploading) {
    return
  }

  const files = Array.from(event.dataTransfer?.files || [])
  if (files.length > 0) {
    processFile(files[0])
  }
}

function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]

  if (file) {
    processFile(file)
  }

  // Reset input so the same file can be selected again
  input.value = ''
}

function processFile(file: File) {
  validationErrors.value = []

  const errors = validateFile(file)
  if (errors.length > 0) {
    validationErrors.value = errors
    return
  }

  // Create preview URL
  const previewUrl = URL.createObjectURL(file)
  emit('update:modelValue', previewUrl)
  emit('upload', file)
}

function validateFile(file: File): string[] {
  const errors: string[] = []

  // Check file type
  if (!props.acceptedFormats.includes(file.type)) {
    errors.push('فرمت فایل نامعتبر است. فرمت‌های مجاز: JPG، PNG، WebP')
  }

  // Check file size
  const fileSizeMB = file.size / (1024 * 1024)
  if (fileSizeMB > props.maxFileSize) {
    errors.push(`حجم فایل بیش از حد مجاز ${props.maxFileSize}MB است`)
  }

  return errors
}
</script>

<style scoped>
.single-image-upload {
  width: 100%;
}

.upload-area {
  border: 2px dashed #d1d5db;
  border-radius: 0.75rem;
  background-color: #f9fafb;
  padding: 2rem;
  text-align: center;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
  min-height: 200px;
  display: flex;
  align-items: center;
  justify-content: center;
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

.upload-area.has-image {
  padding: 0;
  border-style: solid;
  border-color: #e5e7eb;
}

.file-input {
  display: none;
}

.upload-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
}

.upload-icon {
  width: 3rem;
  height: 3rem;
  color: #8b5cf6;
}

.upload-title {
  font-size: 1rem;
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

.upload-format {
  font-size: 0.75rem;
  color: #9ca3af;
  margin: 0.5rem 0 0 0;
}

.image-preview {
  width: 100%;
  height: 100%;
  min-height: 200px;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.75rem;
  overflow: hidden;
}

.image-preview img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  max-height: 300px;
}

.upload-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.7);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
}

.upload-spinner {
  width: 3rem;
  height: 3rem;
  border: 4px solid rgba(255, 255, 255, 0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.upload-text {
  color: #fff;
  font-size: 0.875rem;
  margin: 0;
}

.change-button {
  position: absolute;
  bottom: 1rem;
  right: 50%;
  transform: translateX(50%);
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background-color: rgba(139, 92, 246, 0.95);
  color: #fff;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  opacity: 0;
}

.image-preview:hover .change-button {
  opacity: 1;
}

.change-button:hover {
  background-color: rgba(124, 58, 237, 1);
  transform: translateX(50%) translateY(-2px);
  box-shadow: 0 4px 12px rgba(139, 92, 246, 0.4);
}

.change-button svg {
  width: 1rem;
  height: 1rem;
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
