<template>
  <div class="image-upload-wrapper" dir="rtl">
    <div class="upload-container">
      <!-- Preview Image -->
      <div class="preview-section">
        <div v-if="imageUrl" class="image-preview">
          <img :src="imageUrl" :alt="label" />
          <button
            type="button"
            class="remove-btn"
            @click="handleRemove"
            title="حذف تصویر"
          >
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div v-else class="placeholder">
          <svg class="placeholder-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
          </svg>
          <span class="placeholder-text">{{ placeholder }}</span>
        </div>
      </div>

      <!-- Upload Button -->
      <div class="upload-actions">
        <label :for="inputId" :class="['upload-btn', { 'upload-btn-loading': loading }]">
          <svg v-if="loading" class="btn-icon spinner" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <circle cx="12" cy="12" r="10" stroke-width="4" stroke-opacity="0.25" />
            <path d="M12 2a10 10 0 0 1 10 10" stroke-width="4" stroke-linecap="round" />
          </svg>
          <svg v-else class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
          </svg>
          {{ loading ? 'در حال آپلود...' : (imageUrl ? 'تغییر تصویر' : 'انتخاب تصویر') }}
        </label>
        <input
          :id="inputId"
          ref="fileInput"
          type="file"
          accept="image/*"
          class="file-input"
          :disabled="loading"
          @change="handleFileSelect"
        />
        <p v-if="hint" class="upload-hint">{{ hint }}</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { getImageUrl } from '@/shared/utils/image.util'

interface Props {
  modelValue?: string | null
  label?: string
  placeholder?: string
  hint?: string
  maxSize?: number // in MB
  loading?: boolean
}

interface Emits {
  (e: 'update:modelValue', value: string | null): void
  (e: 'upload', file: File): void
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: null,
  label: 'تصویر',
  placeholder: 'هیچ تصویری انتخاب نشده',
  hint: 'فرمت‌های مجاز: JPG, PNG - حداکثر حجم: 5MB',
  maxSize: 5,
  loading: false
})

const emit = defineEmits<Emits>()

const fileInput = ref<HTMLInputElement | null>(null)
const rawImageUrl = ref<string | null>(props.modelValue)
const inputId = `image-upload-${Math.random().toString(36).substr(2, 9)}`

// Convert relative URLs from backend to full URLs for display
const imageUrl = computed(() => getImageUrl(rawImageUrl.value))

// Watch for external changes
watch(() => props.modelValue, (newValue) => {
  rawImageUrl.value = newValue
})

const handleFileSelect = (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]

  if (!file) return

  // Validate file type
  if (!file.type.startsWith('image/')) {
    alert('لطفاً فقط فایل تصویری انتخاب کنید')
    return
  }

  // Validate file size
  const fileSizeMB = file.size / (1024 * 1024)
  if (fileSizeMB > props.maxSize) {
    alert(`حجم فایل نباید بیشتر از ${props.maxSize} مگابایت باشد`)
    return
  }

  // Create preview URL (base64 for immediate preview)
  const reader = new FileReader()
  reader.onload = (e) => {
    const result = e.target?.result as string

    rawImageUrl.value = result
    emit('update:modelValue', result)
    emit('upload', file)
  }
  reader.readAsDataURL(file)
}

const handleRemove = () => {
  rawImageUrl.value = null
  emit('update:modelValue', null)
  if (fileInput.value) {
    fileInput.value.value = ''
  }
}
</script>

<style scoped lang="scss">
.image-upload-wrapper {
  width: 100%;
}

.upload-container {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.preview-section {
  width: 100%;
  max-width: 200px;
  aspect-ratio: 1;
}

.image-preview {
  position: relative;
  width: 100%;
  height: 100%;
  border-radius: 0.5rem;
  overflow: hidden;
  border: 2px solid #e5e7eb;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }
}

.remove-btn {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.6);
  border: none;
  border-radius: 50%;
  cursor: pointer;
  transition: all 0.2s;

  svg {
    width: 1rem;
    height: 1rem;
    stroke: white;
    stroke-width: 2;
  }

  &:hover {
    background: rgba(0, 0, 0, 0.8);
  }
}

.placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  background: #f9fafb;
  border: 2px dashed #d1d5db;
  border-radius: 0.5rem;
  padding: 1rem;
}

.placeholder-icon {
  width: 3rem;
  height: 3rem;
  stroke: #9ca3af;
  stroke-width: 1.5;
}

.placeholder-text {
  font-size: 0.75rem;
  color: #6b7280;
  text-align: center;
}

.upload-actions {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.upload-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  background: #8b5cf6;
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  align-self: flex-start;

  &:hover {
    background: #7c3aed;
  }

  &:active {
    transform: scale(0.98);
  }

  &.upload-btn-loading {
    opacity: 0.7;
    cursor: not-allowed;
    pointer-events: none;
  }
}

.btn-icon {
  width: 1rem;
  height: 1rem;
  stroke-width: 2;
}

.file-input {
  display: none;
}

.upload-hint {
  font-size: 0.75rem;
  color: #6b7280;
  margin: 0;
}

.spinner {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}
</style>
