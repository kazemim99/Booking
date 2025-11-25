<template>
  <div class="service-area-step">
    <div class="step-container">
      <div class="step-header">
        <h2 class="step-title">منطقه خدمات</h2>
        <p class="step-description">
          منطقه‌ای که در آن خدمات ارائه می‌دهید را مشخص کنید. این به مشتریان کمک می‌کند تا شما را پیدا کنند.
        </p>
      </div>

      <div class="step-content">
        <!-- Mobile Services Toggle -->
        <div class="info-box">
          <i class="icon-info-circle"></i>
          <div>
            <p><strong>خدمات سیار:</strong> آیا به محل مشتری می‌روید یا در محل ثابتی کار می‌کنید؟</p>
          </div>
        </div>

        <div class="form-group">
          <div class="checkbox-item" :class="{ selected: localOffersMobileServices }">
            <input
              id="mobile-services"
              v-model="localOffersMobileServices"
              type="checkbox"
            />
            <label for="mobile-services">
              <strong>ارائه خدمات سیار</strong>
              <span class="checkbox-description">به محل مشتریان می‌روم</span>
            </label>
          </div>
        </div>

        <!-- Service Radius (if mobile services) -->
        <div v-if="localOffersMobileServices" class="form-group">
          <label class="form-label">شعاع خدمات (کیلومتر)</label>
          <div class="radius-selector">
            <input
              v-model="localServiceArea.serviceRadius"
              type="range"
              min="5"
              max="50"
              step="5"
              class="radius-slider"
            />
            <span class="radius-value">{{ localServiceArea.serviceRadius }} کیلومتر</span>
          </div>
          <span class="form-hint">حداکثر فاصله‌ای که برای ارائه خدمات می‌روید</span>
        </div>

        <!-- City Selection -->
        <div class="form-section">
          <h3 class="section-title">محل اصلی فعالیت</h3>
          <div class="form-row">
            <div class="form-group">
              <label class="form-label required">استان</label>
              <input
                v-model="localServiceArea.state"
                type="text"
                class="form-input"
                placeholder="مثال: تهران"
                @blur="validateState"
              />
              <span v-if="errors.state" class="form-error">{{ errors.state }}</span>
            </div>

            <div class="form-group">
              <label class="form-label required">شهر</label>
              <input
                v-model="localServiceArea.city"
                type="text"
                class="form-input"
                placeholder="مثال: تهران"
                @blur="validateCity"
              />
              <span v-if="errors.city" class="form-error">{{ errors.city }}</span>
            </div>
          </div>
        </div>

        <!-- Map Location (Placeholder) -->
        <div class="form-group">
          <label class="form-label required">موقعیت روی نقشه</label>
          <div class="map-placeholder">
            <i class="icon-map-pin"></i>
            <p>موقعیت خود را روی نقشه مشخص کنید</p>
            <AppButton variant="secondary" @click="selectLocation">انتخاب موقعیت</AppButton>
          </div>
          <span v-if="hasLocation" class="form-hint success">
            <i class="icon-check-circle"></i>
            موقعیت انتخاب شد
          </span>
          <span v-else-if="errors.location" class="form-error">{{ errors.location }}</span>
        </div>
      </div>

      <!-- Actions -->
      <div class="step-actions">
        <AppButton variant="secondary" size="large" @click="$emit('back')">
          <i class="icon-arrow-right"></i>
          بازگشت
        </AppButton>

        <AppButton variant="primary" size="large" :disabled="!isValid" @click="handleNext">
          ادامه
          <i class="icon-arrow-left"></i>
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import AppButton from '@/shared/components/AppButton.vue'

// ============================================
// Props & Emits
// ============================================

interface ServiceArea {
  city: string
  state: string
  country: string
  serviceRadius: number
  latitude?: number
  longitude?: number
}

interface Props {
  serviceArea: ServiceArea
  offersMobileServices: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{
  (e: 'update:serviceArea', value: ServiceArea): void
  (e: 'update:offersMobileServices', value: boolean): void
  (e: 'next'): void
  (e: 'back'): void
}>()

// ============================================
// State
// ============================================

const localServiceArea = ref<ServiceArea>({ ...props.serviceArea })
const localOffersMobileServices = ref(props.offersMobileServices)

const errors = ref({
  state: '',
  city: '',
  location: '',
})

// ============================================
// Computed
// ============================================

const hasLocation = computed(() => {
  return !!(localServiceArea.value.latitude && localServiceArea.value.longitude)
})

const isValid = computed(() => {
  return (
    localServiceArea.value.state.trim() !== '' &&
    localServiceArea.value.city.trim() !== '' &&
    hasLocation.value &&
    !errors.value.state &&
    !errors.value.city &&
    !errors.value.location
  )
})

// ============================================
// Methods
// ============================================

function validateState() {
  if (!localServiceArea.value.state.trim()) {
    errors.value.state = 'استان الزامی است'
  } else {
    errors.value.state = ''
  }
}

function validateCity() {
  if (!localServiceArea.value.city.trim()) {
    errors.value.city = 'شهر الزامی است'
  } else {
    errors.value.city = ''
  }
}

function selectLocation() {
  // This would open a map picker modal
  // For now, just set dummy coordinates
  localServiceArea.value.latitude = 35.6892
  localServiceArea.value.longitude = 51.389
  errors.value.location = ''
}

function handleNext() {
  // Validate all fields
  validateState()
  validateCity()

  if (!hasLocation.value) {
    errors.value.location = 'لطفاً موقعیت خود را روی نقشه انتخاب کنید'
  }

  if (isValid.value) {
    emit('update:serviceArea', localServiceArea.value)
    emit('update:offersMobileServices', localOffersMobileServices.value)
    emit('next')
  }
}

// ============================================
// Watchers
// ============================================

watch(
  localServiceArea,
  (newValue) => {
    emit('update:serviceArea', newValue)
  },
  { deep: true }
)

watch(localOffersMobileServices, (newValue) => {
  emit('update:offersMobileServices', newValue)
})
</script>

<style scoped lang="scss">
@import './steps-common.scss';

.checkbox-description {
  display: block;
  font-size: 0.875rem;
  color: #6b7280;
  font-weight: normal;
  margin-top: 0.25rem;
}

.radius-selector {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.radius-slider {
  flex: 1;
  height: 6px;
  border-radius: 3px;
  background: #d1d5db;
  outline: none;
  cursor: pointer;

  &::-webkit-slider-thumb {
    appearance: none;
    width: 20px;
    height: 20px;
    border-radius: 50%;
    background: #7c3aed;
    cursor: pointer;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);

    &:hover {
      background: #6d28d9;
      transform: scale(1.1);
    }
  }

  &::-moz-range-thumb {
    width: 20px;
    height: 20px;
    border-radius: 50%;
    background: #7c3aed;
    cursor: pointer;
    border: none;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);

    &:hover {
      background: #6d28d9;
      transform: scale(1.1);
    }
  }
}

.radius-value {
  min-width: 120px;
  font-weight: 600;
  color: #7c3aed;
  text-align: center;
}

.map-placeholder {
  width: 100%;
  aspect-ratio: 16 / 9;
  background: #f3f4f6;
  border: 2px dashed #d1d5db;
  border-radius: 12px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  color: #6b7280;

  i {
    font-size: 3rem;
    color: #9ca3af;
  }

  p {
    margin: 0;
    font-size: 1rem;
    font-weight: 500;
  }
}

.form-hint.success {
  color: #10b981;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 500;

  i {
    font-size: 1.125rem;
  }
}
</style>
