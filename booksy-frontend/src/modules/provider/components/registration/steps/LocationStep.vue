<template>
  <div class="registration-step">
    <ProgressIndicator :current-step="3" :total-steps="9" />

    <div class="step-card">
      <div class="step-header">
        <h2 class="step-title">موقعیت مکانی</h2>
        <p class="step-description">آدرس و موقعیت کسب‌و‌کار خود را مشخص کنید</p>
      </div>

      <form class="step-form" @submit.prevent="handleSubmit">
        <!-- Neshan Map Picker -->
        <div class="form-group">
          <label class="form-label">موقعیت روی نقشه</label>
          <p class="form-hint">روی نقشه کلیک کنید تا موقعیت کسب‌وکار خود را انتخاب کنید</p>
          <NeshanMapPicker
            v-model="formData.coordinates"
            :map-key="neshanMapKey"
            :service-key="neshanServiceKey"
            height="450px"
            @location-selected="handleLocationSelected"
          />
        </div>

        <!-- Province and City Selector -->
        <LocationSelector
          :province-id="formData.provinceId"
          :city-id="formData.cityId"
          province-label="استان"
          city-label="شهر"
          province-placeholder="استان را انتخاب کنید"
          city-placeholder="شهر را انتخاب کنید"
          :province-error="errors.province"
          :city-error="errors.city"
          :required="true"
          @update:province-id="handleProvinceChange"
          @update:city-id="handleCityChange"
        />

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
            maxlength="10"
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
import { ref, computed, onMounted, watch } from 'vue'
import ProgressIndicator from '../shared/ProgressIndicator.vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import NeshanMapPicker from '@/shared/components/map/NeshanMapPicker.vue'
import LocationSelector from '@/shared/components/forms/LocationSelector.vue'
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

// Neshan Map API keys - same as ProfileManager
const neshanMapKey = import.meta.env.VITE_NESHAN_MAP_KEY || 'web.92c704be7e37461ca51c38a3c0d94e92'
const neshanServiceKey =
  import.meta.env.VITE_NESHAN_SERVICE_KEY || 'service.6d4f76aefbce44e996ed56e3a49fe4c7'

// Form data
const formData = ref({
  provinceId: props.address?.provinceId || null as number | null,
  cityId: props.address?.cityId || null as number | null,
  address: props.address?.addressLine1 || '',
  postalCode: props.address?.zipCode || '',
  coordinates: props.location?.latitude && props.location?.longitude
    ? { lat: props.location.latitude, lng: props.location.longitude }
    : null as { lat: number; lng: number } | null,
  formattedAddress: props.address?.formattedAddress || '',
})

const errors = ref<Record<string, string>>({})

// Handle location selection from map
const handleLocationSelected = (data: {
  lat: number
  lng: number
  address?: string
  addressDetails?: {
    formattedAddress: string
    neighbourhood: string
    city: string
    state: string
    address: string
    route: string
    district: string
    village: string
    county: string
    postalCode: string
  } | null
}) => {
  console.log('Location selected:', data)
  formData.value.coordinates = { lat: data.lat, lng: data.lng }

  // Auto-fill address if available
  if (data.addressDetails) {
    formData.value.formattedAddress = data.addressDetails.formattedAddress || data.address || ''
    if (!formData.value.address) {
      formData.value.address = data.addressDetails.address || data.addressDetails.formattedAddress || ''
    }
    if (data.addressDetails.postalCode) {
      formData.value.postalCode = data.addressDetails.postalCode
    }
  } else if (data.address) {
    formData.value.formattedAddress = data.address
    if (!formData.value.address) {
      formData.value.address = data.address
    }
  }
}

// Handle province change
const handleProvinceChange = (provinceId: number | null) => {
  formData.value.provinceId = provinceId
  // Reset city when province changes
  if (formData.value.cityId) {
    formData.value.cityId = null
  }
  errors.value.province = ''
}

// Handle city change
const handleCityChange = (cityId: number | null) => {
  formData.value.cityId = cityId
  errors.value.city = ''
}

// Validation
const validateField = (field: keyof typeof formData.value) => {
  errors.value = { ...errors.value }

  switch (field) {
    case 'address':
      if (!formData.value.address?.trim()) {
        errors.value.address = 'آدرس الزامی است'
      } else {
        delete errors.value.address
      }
      break
  }
}

const validateForm = (): boolean => {
  errors.value = {}

  if (!formData.value.provinceId) {
    errors.value.province = 'استان الزامی است'
  }

  if (!formData.value.cityId) {
    errors.value.city = 'شهر الزامی است'
  }

  if (!formData.value.address?.trim()) {
    errors.value.address = 'آدرس الزامی است'
  }

  return Object.keys(errors.value).length === 0
}

const isFormValid = computed(() => {
  return (
    formData.value.provinceId !== null &&
    formData.value.cityId !== null &&
    formData.value.address?.trim() !== ''
  )
})

// Update parent when form data changes
watch(
  formData,
  (newValue) => {
    // Emit address update
    emit('update:address', {
      addressLine1: newValue.address,
      addressLine2: undefined,
      city: undefined, // City will be resolved by provinceId/cityId
      province: undefined, // Province will be resolved by provinceId/cityId
      zipCode: newValue.postalCode,
      formattedAddress: newValue.formattedAddress,
      provinceId: newValue.provinceId || undefined,
      cityId: newValue.cityId || undefined,
    })

    // Emit location update
    if (newValue.coordinates) {
      emit('update:location', {
        latitude: newValue.coordinates.lat,
        longitude: newValue.coordinates.lng,
      })
    }
  },
  { deep: true }
)

const handleSubmit = () => {
  if (validateForm()) {
    emit('next')
  }
}

// Initialize from props on mount
onMounted(() => {
  if (props.address?.provinceId) {
    formData.value.provinceId = props.address.provinceId
  }
  if (props.address?.cityId) {
    formData.value.cityId = props.address.cityId
  }
})
</script>

<style scoped>
.registration-step {
  width: 100%;
}

.step-card {
  background: #ffffff;
  border-radius: 1rem;
  padding: 2rem;
  box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
}

.step-header {
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin: 0 0 0.5rem 0;
}

.step-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
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

.form-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #374151;
}

.form-hint {
  font-size: 0.75rem;
  color: #6b7280;
  margin: 0;
}

.required {
  color: #ef4444;
  margin-right: 0.25rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  transition: all 0.2s;
  background: #ffffff;
}

.form-input:focus {
  outline: none;
  border-color: #10b981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}

.form-input-error {
  border-color: #ef4444;
}

.form-input-error:focus {
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
}

.form-error {
  font-size: 0.75rem;
  color: #ef4444;
}

.step-actions {
  display: flex;
  gap: 1rem;
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

  .step-actions {
    flex-direction: column-reverse;
  }
}
</style>
