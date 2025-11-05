<template>
  <div class="registration-step">
    <ProgressIndicator :current-step="3" :total-steps="9" />

    <div class="step-card">
      <div class="step-header">
        <h2 class="step-title">موقعیت مکانی</h2>
        <p class="step-description">آدرس و موقعیت کسب‌و‌کار خود را مشخص کنید</p>
      </div>

      <form class="step-form" @submit.prevent="handleSubmit">
        <!-- Mock Map Placeholder -->
        <div class="form-group">
          <label class="form-label">انتخاب روی نقشه</label>
          <div class="map-placeholder">
            <div class="map-placeholder-content">
              <svg class="map-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
                />
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
                />
              </svg>
              <p class="map-placeholder-text">کلیک کنید تا موقعیت را روی نقشه انتخاب کنید</p>
            </div>
            <!-- Grid pattern to simulate map -->
            <div class="map-grid"></div>
          </div>
        </div>

        <!-- Province and City -->
        <div class="form-row">
          <div class="form-group">
            <label for="province" class="form-label">
              استان <span class="required">*</span>
            </label>
            <input
              id="province"
              v-model="formData.province"
              type="text"
              class="form-input"
              :class="{ 'form-input-error': errors.province }"
              placeholder="مثال: تهران"
              @blur="validateField('province')"
            />
            <span v-if="errors.province" class="form-error">{{ errors.province }}</span>
          </div>

          <div class="form-group">
            <label for="city" class="form-label">
              شهر <span class="required">*</span>
            </label>
            <input
              id="city"
              v-model="formData.city"
              type="text"
              class="form-input"
              :class="{ 'form-input-error': errors.city }"
              placeholder="مثال: تهران"
              @blur="validateField('city')"
            />
            <span v-if="errors.city" class="form-error">{{ errors.city }}</span>
          </div>
        </div>

        <!-- Address -->
        <div class="form-group">
          <label for="address" class="form-label">
            آدرس دقیق <span class="required">*</span>
          </label>
          <input
            id="address"
            v-model="formData.address"
            type="text"
            class="form-input"
            :class="{ 'form-input-error': errors.address }"
            placeholder="مثال: خیابان ولیعصر، کوچه پنجم، پلاک ۱۲"
            @blur="validateField('address')"
          />
          <span v-if="errors.address" class="form-error">{{ errors.address }}</span>
        </div>

        <!-- Postal Code -->
        <div class="form-group">
          <label for="postalCode" class="form-label">کد پستی (اختیاری)</label>
          <input
            id="postalCode"
            v-model="formData.postalCode"
            type="text"
            dir="ltr"
            class="form-input"
            placeholder="1234567890"
          />
        </div>

        <!-- Navigation -->
        <div class="step-actions">
          <AppButton type="button" variant="outline" size="large" @click="$emit('back')">
            قبلی
          </AppButton>
          <AppButton type="submit" variant="primary" size="large" :disabled="!isFormValid">
            بعدی
          </AppButton>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import ProgressIndicator from '../shared/ProgressIndicator.vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import type { BusinessAddress, BusinessLocation } from '@/modules/provider/types/registration.types'

interface Props {
  address?: Partial<BusinessAddress>
  location?: Partial<BusinessLocation>
}

interface Emits {
  (e: 'update:address', value: Partial<BusinessAddress>): void
  (e: 'update:location', value: Partial<BusinessLocation>): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Form data
const formData = ref({
  province: props.address?.province || '',
  city: props.address?.city || '',
  address: props.address?.addressLine1 || '',
  postalCode: props.address?.zipCode || '',
})

const errors = ref<Record<string, string>>({})

// Validation
const validateField = (field: keyof typeof formData.value) => {
  errors.value = { ...errors.value }
  delete errors.value[field]

  if (field === 'province' && !formData.value.province.trim()) {
    errors.value.province = 'استان الزامی است'
  }
  if (field === 'city' && !formData.value.city.trim()) {
    errors.value.city = 'شهر الزامی است'
  }
  if (field === 'address' && !formData.value.address.trim()) {
    errors.value.address = 'آدرس الزامی است'
  }
}

const isFormValid = computed(() => {
  return (
    formData.value.province.trim() &&
    formData.value.city.trim() &&
    formData.value.address.trim()
  )
})

const handleSubmit = () => {
  // Validate all fields
  validateField('province')
  validateField('city')
  validateField('address')

  if (isFormValid.value && Object.keys(errors.value).length === 0) {
    // Emit address data
    emit('update:address', {
      addressLine1: formData.value.address,
      city: formData.value.city,
      province: formData.value.province,
      zipCode: formData.value.postalCode,
      country: 'Iran',
    })

    // Emit location data (mock for now since we don't have real map)
    emit('update:location', {
      latitude: 35.6892, // Default Tehran coordinates
      longitude: 51.389,
    })

    emit('next')
  }
}
</script>

<style scoped>
.registration-step {
  min-height: 100vh;
  padding: 2rem 1rem;
  background: #f9fafb;
  direction: rtl;
}

.step-card {
  max-width: 42rem;
  margin: 0 auto;
  background: white;
  border-radius: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  padding: 2rem;
}

.step-header {
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.step-description {
  font-size: 0.875rem;
  color: #6b7280;
}

.step-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.required {
  color: #ef4444;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
}

.form-input:focus {
  outline: none;
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.form-input-error {
  border-color: #ef4444;
}

.form-input-error:focus {
  border-color: #ef4444;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
}

.form-error {
  font-size: 0.875rem;
  color: #ef4444;
}

/* Map Placeholder */
.map-placeholder {
  position: relative;
  height: 16rem;
  background: #f3f4f6;
  border-radius: 0.75rem;
  overflow: hidden;
  border: 1px solid #e5e7eb;
}

.map-placeholder-content {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 1;
}

.map-icon {
  width: 3rem;
  height: 3rem;
  color: #8b5cf6;
  margin-bottom: 0.5rem;
}

.map-placeholder-text {
  font-size: 0.875rem;
  color: #6b7280;
  text-align: center;
}

.map-grid {
  position: absolute;
  inset: 0;
  opacity: 0.1;
  background-image: linear-gradient(0deg, #000 1px, transparent 1px),
    linear-gradient(90deg, #000 1px, transparent 1px);
  background-size: 20px 20px;
}

/* Navigation */
.step-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.step-actions > * {
  flex: 1;
}

@media (max-width: 640px) {
  .step-card {
    padding: 1.5rem;
  }

  .step-title {
    font-size: 1.25rem;
  }

  .form-row {
    grid-template-columns: 1fr;
  }

  .map-placeholder {
    height: 12rem;
  }
}
</style>
