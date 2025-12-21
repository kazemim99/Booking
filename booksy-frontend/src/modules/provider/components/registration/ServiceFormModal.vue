<!--
  ServiceFormModal Component
  Modal for adding and editing services during registration
-->

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="modelValue" class="modal-overlay" @click="handleOverlayClick" dir="rtl">
        <div class="modal-container" @click.stop>
          <!-- Header -->
          <div class="modal-header">
            <h2>{{ isEditing ? 'ویرایش خدمت' : 'افزودن خدمت جدید' }}</h2>
            <button class="close-btn" @click="close" :disabled="loading">
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          </div>

          <!-- Content -->
          <div class="modal-content">
            <!-- Service Name -->
            <div class="form-group">
              <label for="serviceName" class="form-label">نام خدمت</label>
              <input
                id="serviceName"
                v-model="formData.name"
                type="text"
                class="form-input"
                placeholder="مثال: اصلاح مو"
                :disabled="loading"
              />
              <span v-if="errors.name" class="error-text">{{ errors.name }}</span>
            </div>

            <!-- Price -->
            <div class="form-group">
              <label for="price" class="form-label">قیمت (تومان)</label>
              <input
                id="price"
                :value="formatPriceDisplay(formData.price)"
                @input="handlePriceInput"
                type="text"
                dir="ltr"
                class="form-input"
                placeholder="500,000"
                :disabled="loading"
              />
              <span v-if="errors.price" class="error-text">{{ errors.price }}</span>
            </div>

            <!-- Duration -->
            <div class="form-group">
              <label for="duration" class="form-label">مدت زمان</label>
              <select
                id="duration"
                v-model.number="formData.duration"
                class="form-input"
                :disabled="loading"
              >
                <option value="">انتخاب کنید...</option>
                <option
                  v-for="option in durationOptions"
                  :key="option.value"
                  :value="option.value"
                >
                  {{ option.label }}
                </option>
              </select>
              <span v-if="errors.duration" class="error-text">{{ errors.duration }}</span>
            </div>

            <!-- Error Message -->
            <div v-if="apiError" class="error-message">
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <span>{{ apiError }}</span>
            </div>
          </div>

          <!-- Footer -->
          <div class="modal-footer">
            <button class="btn btn-secondary" @click="close" :disabled="loading">
              لغو
            </button>
            <button
              class="btn btn-primary"
              @click="handleSubmit"
              :disabled="!isFormValid || loading"
            >
              <span v-if="loading" class="spinner"></span>
              <span v-else>{{ isEditing ? 'ویرایش' : 'افزودن' }}</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { Service } from '@/modules/provider/types/registration.types'
import { formatPriceDisplay, parsePriceInput as parsePrice } from '@/core/utils/price.service'

// ==================== Props & Emits ====================

interface Props {
  modelValue: boolean
  service?: Service | null
}

const props = defineProps<Props>()

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'submit', service: Service): void
}

const emit = defineEmits<Emits>()

// ==================== State ====================

const loading = ref(false)
const apiError = ref<string | null>(null)
const formData = ref({
  name: '',
  price: '',
  duration: '',
})

const errors = ref({
  name: '',
  price: '',
  duration: '',
})

// Duration options in minutes (15-minute increments up to 8 hours)
const durationOptions = computed(() => {
  const options = []
  for (let i = 15; i <= 480; i += 15) {
    const hours = Math.floor(i / 60)
    const mins = i % 60
    let label = ''
    if (hours > 0 && mins > 0) {
      label = `${hours} ساعت و ${mins} دقیقه`
    } else if (hours > 0) {
      label = `${hours} ساعت`
    } else {
      label = `${mins} دقیقه`
    }
    options.push({ value: i, label })
  }
  return options
})

// ==================== Computed ====================

const isEditing = computed(() => !!props.service)

const isFormValid = computed(() => {
  return formData.value.name.trim() &&
         formData.value.price &&
         formData.value.duration
})

// ==================== Methods ====================

function handlePriceInput(event: Event) {
  const input = event.target as HTMLInputElement
  formData.value.price = parsePrice(input.value)
}

function close() {
  if (loading.value) return
  emit('update:modelValue', false)
  resetForm()
}

function handleOverlayClick() {
  close()
}

function resetForm() {
  formData.value = { name: '', price: '', duration: '' }
  errors.value = { name: '', price: '', duration: '' }
  apiError.value = null
}

function validateForm(): boolean {
  errors.value = { name: '', price: '', duration: '' }

  if (!formData.value.name.trim()) {
    errors.value.name = 'نام خدمت الزامی است'
  }

  if (!formData.value.price) {
    errors.value.price = 'قیمت الزامی است'
  } else if (parseFloat(formData.value.price) <= 0) {
    errors.value.price = 'قیمت باید بزرگتر از صفر باشد'
  }

  if (!formData.value.duration) {
    errors.value.duration = 'مدت زمان الزامی است'
  }

  return !errors.value.name && !errors.value.price && !errors.value.duration
}

function handleSubmit() {
  if (!validateForm()) {
    return
  }

  apiError.value = null

  try {
    const totalMinutes = parseInt(formData.value.duration)
    const newService: Service = {
      id: props.service?.id || Date.now().toString(),
      name: formData.value.name,
      price: parseFloat(formData.value.price),
      durationHours: Math.floor(totalMinutes / 60),
      durationMinutes: totalMinutes % 60,
      priceType: 'fixed',
    }

    emit('submit', newService)
    close()
  } catch (err) {
    apiError.value = err instanceof Error ? err.message : 'خطای نامشخص'
  }
}

// ==================== Lifecycle ====================

watch(
  () => props.modelValue,
  (isOpen) => {
    if (isOpen) {
      if (props.service) {
        // Edit mode: populate form with existing service data
        const totalMinutes = props.service.durationHours * 60 + props.service.durationMinutes
        formData.value = {
          name: props.service.name,
          price: props.service.price.toString(),
          duration: totalMinutes.toString(),
        }
      } else {
        // Add mode: reset form
        resetForm()
      }
    }
  },
  { immediate: true }
)
</script>

<style scoped>
/* ==================== Modal Overlay ==================== */

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
  z-index: 9999;
  padding: 20px;
  overflow-y: auto;
}

.modal-container {
  background: white;
  border-radius: 16px;
  width: 100%;
  max-width: 500px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  direction: rtl;
}

/* ==================== Modal Header ==================== */

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 24px;
  border-bottom: 1px solid #e5e7eb;
}

.modal-header h2 {
  font-size: 20px;
  font-weight: 700;
  color: #111827;
  margin: 0;
}

.close-btn {
  background: transparent;
  border: none;
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  cursor: pointer;
  color: #6b7280;
  transition: all 0.2s ease;
  padding: 0;
}

.close-btn:hover:not(:disabled) {
  background: #f3f4f6;
  color: #111827;
}

.close-btn svg {
  width: 20px;
  height: 20px;
}

/* ==================== Modal Content ==================== */

.modal-content {
  flex: 1;
  overflow-y: auto;
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

/* Form Group */
.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.form-label {
  font-size: 14px;
  font-weight: 600;
  color: #374151;
}

.form-input {
  width: 100%;
  padding: 10px 12px;
  font-size: 14px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  background: white;
  transition: all 0.2s ease;
  font-family: inherit;
}

.form-input:focus {
  outline: none;
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.form-input:disabled {
  background: #f3f4f6;
  cursor: not-allowed;
  opacity: 0.6;
}

.error-text {
  font-size: 12px;
  color: #ef4444;
  margin-top: 2px;
}

/* Error Message */
.error-message {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 12px 16px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 8px;
  color: #dc2626;
  font-size: 14px;
}

.error-message svg {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
  margin-top: 2px;
}

/* ==================== Modal Footer ==================== */

.modal-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  padding: 20px 24px;
  border-top: 1px solid #e5e7eb;
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
  justify-content: center;
  min-width: 120px;
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-secondary {
  background: #f3f4f6;
  color: #374151;
}

.btn-secondary:hover:not(:disabled) {
  background: #e5e7eb;
}

.btn-primary {
  background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(139, 92, 246, 0.3);
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* ==================== Modal Transitions ==================== */

.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.3s ease;
}

.modal-enter-active .modal-container,
.modal-leave-active .modal-container {
  transition: transform 0.3s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-from .modal-container,
.modal-leave-to .modal-container {
  transform: scale(0.95);
}

/* ==================== Responsive ==================== */

@media (max-width: 768px) {
  .modal-container {
    max-width: 100%;
  }

  .modal-header {
    padding: 16px;
  }

  .modal-content {
    padding: 16px;
  }

  .modal-footer {
    padding: 16px;
  }
}
</style>
