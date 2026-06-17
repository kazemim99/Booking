<!--
  ImageUploadWidget Component
  Drag-and-drop image upload with progress tracking
-->

<template>
  <div class="image-upload-widget" dir="rtl">
    <!-- Upload Zone -->
    <div
      class="upload-zone"
      :class="{
        'drag-over': isDragging,
        'has-files': uploadQueue.length > 0,
      }"
      @dragover.prevent="handleDragOver"
      @dragleave.prevent="handleDragLeave"
      @drop.prevent="handleDrop"
      @click="triggerFileInput"
    >
      <!-- Icon & Text -->
      <div v-if="uploadQueue.length === 0" class="upload-prompt">
        <i class="icon-upload"></i>
        <h3>تصاویر خود را بکشید و اینجا رها کنید</h3>
        <p>یا کلیک کنید تا فایل‌ها را انتخاب نمایید</p>
        <span class="supported-formats">فرمت‌های پشتیبانی شده: JPG, PNG, WEBP (حداکثر {{ maxSizeMB }}MB)</span>
      </div>

      <!-- Upload Queue -->
      <div v-else class="upload-queue">
        <div
          v-for="item in uploadQueue"
          :key="item.file.name + item.file.lastModified"
          class="upload-item"
          :class="`status-${item.status}`"
        >
          <!-- Preview -->
          <div class="preview">
            <img v-if="(item as any).preview" :src="(item as any).preview" :alt="item.file.name" />
            <div v-else class="preview-placeholder">
              <i class="icon-image"></i>
            </div>
          </div>

          <!-- Info -->
          <div class="info">
            <div class="file-name">{{ item.file.name }}</div>
            <div class="file-size">{{ formatFileSize(item.file.size) }}</div>

            <!-- Progress Bar -->
            <div v-if="item.status === 'uploading'" class="progress-bar">
              <div class="progress-fill" :style="{ width: `${item.progress}%` }"></div>
            </div>

            <!-- Status Messages -->
            <div v-if="item.status === 'success'" class="status-message success">
              <i class="icon-check"></i>
              <span>آپلود موفق</span>
            </div>
            <div v-if="item.status === 'error'" class="status-message error">
              <i class="icon-alert"></i>
              <span>{{ item.error || 'خطا در آپلود' }}</span>
            </div>
          </div>

          <!-- Actions -->
          <div class="actions">
            <button
              v-if="item.status === 'pending' || item.status === 'error'"
              class="action-btn remove"
              @click.stop="removeFromQueue(item)"
              title="حذف"
            >
              <i class="icon-x"></i>
            </button>
            <span v-else-if="item.status === 'uploading'" class="uploading-spinner">
              <i class="icon-spinner"></i>
            </span>
            <span v-else-if="item.status === 'success'" class="success-icon">
              <i class="icon-check-circle"></i>
            </span>
          </div>
        </div>

        <!-- Add More Button -->
        <button class="add-more-btn" @click.stop="triggerFileInput">
          <i class="icon-plus"></i>
          <span>افزودن تصویر بیشتر</span>
        </button>
      </div>

      <!-- Hidden File Input -->
      <input
        ref="fileInput"
        type="file"
        accept="image/jpeg,image/png,image/webp"
        multiple
        style="display: none"
        @change="handleFileSelect"
      />
    </div>

    <!-- Upload Button -->
    <div v-if="hasQueuedFiles" class="upload-controls">
      <button class="btn btn-secondary" @click="clearQueue" :disabled="isUploading">
        پاک کردن همه
      </button>
      <button class="btn btn-primary" @click="startUpload" :disabled="isUploading || !hasQueuedFiles">
        <span v-if="isUploading" class="spinner"></span>
        <span v-else>آپلود {{ pendingCount }} تصویر</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { UploadProgress } from '@/modules/provider/types/gallery.types'

// ==================== Props & Emits ====================

interface Props {
  maxFiles?: number
  maxSizeMB?: number
  autoUpload?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  maxFiles: 10,
  maxSizeMB: 5,
  autoUpload: false,
})

interface Emits {
  (e: 'files-selected', files: File[]): void
  (e: 'upload-start'): void
  (e: 'upload-progress', progress: UploadProgress[]): void
  (e: 'upload-complete', succeeded: File[], failed: File[]): void
}

const emit = defineEmits<Emits>()

// ==================== State ====================

const fileInput = ref<HTMLInputElement | null>(null)
const isDragging = ref(false)
const uploadQueue = ref<UploadProgress[]>([])

// ==================== Computed ====================

const isUploading = computed(() => uploadQueue.value.some(item => item.status === 'uploading'))
const hasQueuedFiles = computed(() => uploadQueue.value.some(item => item.status === 'pending'))
const pendingCount = computed(() => uploadQueue.value.filter(item => item.status === 'pending').length)

// ==================== Methods ====================

function triggerFileInput() {
  fileInput.value?.click()
}

function handleFileSelect(event: Event) {
  const target = event.target as HTMLInputElement
  if (target.files) {
    addFiles(Array.from(target.files))
  }
  // Reset input to allow selecting the same file again
  target.value = ''
}

function handleDragOver() {
  isDragging.value = true
}

function handleDragLeave() {
  isDragging.value = false
}

function handleDrop(event: DragEvent) {
  isDragging.value = false
  const files = event.dataTransfer?.files
  if (files) {
    addFiles(Array.from(files))
  }
}

function addFiles(files: File[]) {
  // Filter valid image files
  const validFiles = files.filter(file => {
    // Check file type
    if (!file.type.startsWith('image/')) {
      console.warn(`File ${file.name} is not an image`)
      return false
    }

    // Check file size
    const sizeMB = file.size / (1024 * 1024)
    if (sizeMB > props.maxSizeMB) {
      console.warn(`File ${file.name} is too large (${sizeMB.toFixed(2)}MB)`)
      return false
    }

    return true
  })

  // Check max files limit
  const remainingSlots = props.maxFiles - uploadQueue.value.length
  if (validFiles.length > remainingSlots) {
    console.warn(`Can only add ${remainingSlots} more files (max ${props.maxFiles})`)
    validFiles.splice(remainingSlots)
  }

  // Add to queue
  validFiles.forEach(file => {
    const uploadItem: UploadProgress = {
      file,
      progress: 0,
      status: 'pending',
    }

    // Create preview
    const reader = new FileReader()
    reader.onload = (e) => {
      const item = uploadQueue.value.find(i => i.file === file)
      if (item) {
        ;(item as any).preview = e.target?.result
      }
    }
    reader.readAsDataURL(file)

    uploadQueue.value.push(uploadItem)
  })

  emit('files-selected', validFiles)

  // Auto upload if enabled
  if (props.autoUpload && validFiles.length > 0) {
    startUpload()
  }
}

function removeFromQueue(item: UploadProgress) {
  const index = uploadQueue.value.indexOf(item)
  if (index > -1) {
    uploadQueue.value.splice(index, 1)
  }
}

function clearQueue() {
  uploadQueue.value = uploadQueue.value.filter(item => item.status === 'uploading')
}

async function startUpload() {
  const pendingFiles = uploadQueue.value.filter(item => item.status === 'pending')
  if (pendingFiles.length === 0) return

  emit('upload-start')

  // Set all pending items to uploading
  pendingFiles.forEach(item => {
    item.status = 'uploading'
    item.progress = 0
  })

  // Simulate upload progress (in real app, this would be from actual upload)
  // The parent component should handle actual upload using gallery service
  const uploadPromises = pendingFiles.map(item => simulateUpload(item))

  await Promise.allSettled(uploadPromises)

  // Emit results
  const succeeded = uploadQueue.value.filter(i => i.status === 'success').map(i => i.file)
  const failed = uploadQueue.value.filter(i => i.status === 'error').map(i => i.file)

  emit('upload-complete', succeeded, failed)
}

function simulateUpload(item: UploadProgress): Promise<void> {
  return new Promise((resolve) => {
    const duration = 2000 // 2 seconds
    const interval = 50
    const steps = duration / interval
    const increment = 100 / steps

    const timer = setInterval(() => {
      item.progress = Math.min(100, item.progress + increment)

      if (item.progress >= 100) {
        clearInterval(timer)
        item.status = 'success'
        resolve()
      }
    }, interval)
  })
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

// ==================== Public Methods (exposed to parent) ====================

defineExpose({
  getQueuedFiles: () => uploadQueue.value.map(item => item.file),
  clearSuccessful: () => {
    uploadQueue.value = uploadQueue.value.filter(item => item.status !== 'success')
  },
  setItemStatus: (file: File, status: UploadProgress['status'], error?: string) => {
    const item = uploadQueue.value.find(i => i.file === file)
    if (item) {
      item.status = status
      if (error) item.error = error
    }
  },
  setItemProgress: (file: File, progress: number) => {
    const item = uploadQueue.value.find(i => i.file === file)
    if (item) item.progress = progress
  },
})
</script>

<style scoped>
.image-upload-widget {
  width: 100%;
  direction: rtl;
}

/* ==================== Upload Zone ==================== */

.upload-zone {
  border: 2px dashed var(--color-gray-400);
  border-radius: 12px;
  padding: 40px 20px;
  text-align: center;
  cursor: pointer;
  transition: all 0.3s ease;
  background: var(--color-gray-50);
}

.upload-zone:hover {
  border-color: var(--color-gray-500);
  background: var(--color-gray-100);
}

.upload-zone.drag-over {
  border-color: var(--color-primary-500);
  background: var(--color-primary-50);
  transform: scale(1.02);
}

.upload-zone.has-files {
  padding: 20px;
  cursor: default;
}

.upload-zone.has-files:hover {
  border-color: var(--color-gray-400);
  background: var(--color-gray-50);
}

/* Upload Prompt */
.upload-prompt {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.upload-prompt i {
  font-size: 64px;
  color: var(--color-gray-500);
}

.upload-prompt h3 {
  font-size: 18px;
  font-weight: 600;
  color: var(--color-gray-800);
  margin: 0;
}

.upload-prompt p {
  font-size: 14px;
  color: var(--color-gray-600);
  margin: 0;
}

.supported-formats {
  font-size: 12px;
  color: var(--color-gray-500);
}

/* ==================== Upload Queue ==================== */

.upload-queue {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.upload-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 12px;
  background: white;
  border: 1px solid var(--color-gray-300);
  border-radius: 8px;
  transition: all 0.2s ease;
}

.upload-item.status-success {
  border-color: var(--color-success-500);
  background: #f0fdf4;
}

.upload-item.status-error {
  border-color: var(--color-danger-500);
  background: var(--color-danger-50);
}

/* Preview */
.preview {
  flex-shrink: 0;
  width: 60px;
  height: 60px;
  border-radius: 6px;
  overflow: hidden;
  background: var(--color-gray-100);
}

.preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.preview-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-gray-500);
  font-size: 24px;
}

/* Info */
.info {
  flex: 1;
  min-width: 0;
}

.file-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--color-gray-900);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.file-size {
  font-size: 12px;
  color: var(--color-gray-600);
  margin-top: 4px;
}

/* Progress Bar */
.progress-bar {
  width: 100%;
  height: 4px;
  background: var(--color-gray-300);
  border-radius: 2px;
  margin-top: 8px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--color-primary-500), var(--color-primary-600));
  transition: width 0.3s ease;
}

/* Status Messages */
.status-message {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 6px;
  font-size: 13px;
  font-weight: 500;
}

.status-message.success {
  color: var(--color-success-600);
}

.status-message.error {
  color: var(--color-danger-600);
}

.status-message i {
  font-size: 14px;
}

/* Actions */
.actions {
  flex-shrink: 0;
}

.action-btn {
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 50%;
  background: var(--color-gray-100);
  color: var(--color-gray-600);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.action-btn:hover {
  background: var(--color-danger-600);
  color: white;
}

.uploading-spinner i,
.success-icon i {
  font-size: 20px;
}

.uploading-spinner i {
  color: var(--color-primary-500);
  animation: spin 1s linear infinite;
}

.success-icon i {
  color: var(--color-success-500);
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Add More Button */
.add-more-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
  padding: 12px;
  border: 2px dashed var(--color-gray-400);
  border-radius: 8px;
  background: white;
  color: var(--color-gray-600);
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.add-more-btn:hover {
  border-color: var(--color-primary-500);
  color: var(--color-primary-500);
  background: var(--color-primary-50);
}

.add-more-btn i {
  font-size: 16px;
}

/* ==================== Upload Controls ==================== */

.upload-controls {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 20px;
}

.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-secondary {
  background: var(--color-gray-100);
  color: var(--color-gray-800);
}

.btn-secondary:hover:not(:disabled) {
  background: var(--color-gray-300);
}

.btn-primary {
  background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-600) 100%);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

/* ==================== Icon Fallbacks ==================== */

.icon-upload::before {
  content: '📤';
}

.icon-image::before {
  content: '🖼';
}

.icon-check::before {
  content: '✓';
}

.icon-alert::before {
  content: '⚠';
}

.icon-x::before {
  content: '✕';
}

.icon-spinner::before {
  content: '⟳';
}

.icon-check-circle::before {
  content: '✓';
}

.icon-plus::before {
  content: '+';
}
</style>
