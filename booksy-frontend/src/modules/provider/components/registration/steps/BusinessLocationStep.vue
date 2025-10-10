<template>
  <StepContainer
    :title="showConfirm ? $t('provider.registration.location.confirmTitle') : $t('provider.registration.location.title')"
    :subtitle="$t('provider.registration.location.subtitle')"
  >
    <!-- Step 1: Map Address Selection -->
    <div v-if="!showConfirm" class="location-map-container">
      <div class="address-search">
        <div class="search-input-wrapper">
          <svg class="search-icon" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd" />
          </svg>
          <input
            v-model="searchQuery"
            type="text"
            class="address-search-input"
            :placeholder="$t('provider.registration.location.searchPlaceholder')"
            @input="handleAddressSearch"
          />
        </div>
      </div>

      <!-- Map Placeholder -->
      <div class="map-placeholder">
        <div class="map-marker">
          <svg viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/>
          </svg>
        </div>
        <p class="map-placeholder-text">{{ $t('provider.registration.location.mapIntegrationNote') }}</p>
      </div>

      <p class="helper-text">{{ formattedAddress || $t('provider.registration.location.enterAddressHelp') }}</p>

      <!-- Navigation -->
      <NavigationButtons
        :show-back="true"
        :can-continue="!!formattedAddress"
        @back="$emit('back')"
        @next="handleNextToConfirm"
      />
    </div>

    <!-- Step 2: Address Confirmation Form -->
    <div v-else class="address-confirm-form">
      <form @submit.prevent="handleSubmit">
        <!-- Street Address Line 1 -->
        <div class="form-group">
          <label class="form-label-small">
            {{ $t('provider.registration.location.addressLine1') }}
          </label>
          <input
            v-model="addressData.addressLine1"
            type="text"
            class="form-input"
            required
          />
        </div>

        <!-- Street Address Line 2 -->
        <div class="form-group">
          <label class="form-label-small">
            {{ $t('provider.registration.location.addressLine2') }}
          </label>
          <input
            v-model="addressData.addressLine2"
            type="text"
            class="form-input"
            :placeholder="$t('provider.registration.location.optional')"
          />
        </div>

        <!-- City -->
        <div class="form-group">
          <label class="form-label-small">
            {{ $t('provider.registration.location.city') }}
          </label>
          <input
            v-model="addressData.city"
            type="text"
            class="form-input"
            required
          />
        </div>

        <!-- Zip Code -->
        <div class="form-group">
          <label class="form-label-small">
            {{ $t('provider.registration.location.zipCode') }}
          </label>
          <input
            v-model="addressData.zipCode"
            type="text"
            class="form-input"
            required
          />
        </div>

        <!-- Shared Location Checkbox -->
        <div class="checkbox-group">
          <input
            id="isShared"
            v-model="addressData.isShared"
            type="checkbox"
            class="checkbox-input"
          />
          <label for="isShared" class="checkbox-label">
            {{ $t('provider.registration.location.sharedLocation') }}
            <button type="button" class="info-button" :title="$t('provider.registration.location.sharedLocationHint')">
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
              </svg>
            </button>
          </label>
        </div>

        <!-- Navigation -->
        <div class="button-group">
          <button type="button" class="btn btn-secondary" @click="handleReset">
            {{ $t('common.reset') }}
          </button>
          <button type="submit" class="btn btn-primary">
            {{ $t('common.continue') }}
          </button>
        </div>
        <button type="button" class="btn-back-link" @click="showConfirm = false">
          {{ $t('common.back') }}
        </button>
      </form>
    </div>
  </StepContainer>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import StepContainer from '../shared/StepContainer.vue'
import NavigationButtons from '../shared/NavigationButtons.vue'
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

const showConfirm = ref(false)
const searchQuery = ref('')
const formattedAddress = ref('')

const addressData = ref<Partial<BusinessAddress>>({
  addressLine1: props.address?.addressLine1 || '',
  addressLine2: props.address?.addressLine2 || '',
  city: props.address?.city || '',
  zipCode: props.address?.zipCode || '',
  isShared: props.address?.isShared || false,
})

const locationData = ref<Partial<BusinessLocation>>({
  latitude: props.location?.latitude || 0,
  longitude: props.location?.longitude || 0,
  formattedAddress: props.location?.formattedAddress || '',
})

const handleAddressSearch = () => {
  // TODO: Implement actual map API integration (Google Maps/Mapbox)
  // For now, just show formatted address
  if (searchQuery.value.length > 3) {
    formattedAddress.value = searchQuery.value
    // Mock location data
    locationData.value = {
      latitude: 40.7128,
      longitude: -74.0060,
      formattedAddress: searchQuery.value,
    }
  }
}

const handleNextToConfirm = () => {
  // Parse search query into address components (simplified)
  const parts = searchQuery.value.split(',').map(s => s.trim())
  addressData.value.addressLine1 = parts[0] || ''
  addressData.value.city = parts[1] || ''
  addressData.value.zipCode = parts[2] || ''

  showConfirm.value = true
}

const handleReset = () => {
  addressData.value = {
    addressLine1: '',
    addressLine2: '',
    city: '',
    zipCode: '',
    isShared: false,
  }
}

const handleSubmit = () => {
  emit('update:address', addressData.value)
  emit('update:location', locationData.value)
  emit('next')
}

watch(() => props.address, (newVal) => {
  if (newVal) {
    addressData.value = { ...addressData.value, ...newVal }
  }
}, { deep: true })
</script>

<style scoped>
/* Map Container */
.location-map-container {
  width: 100%;
}

.address-search {
  margin-bottom: 1.5rem;
}

.search-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 1rem;
  width: 1.25rem;
  height: 1.25rem;
  color: #9ca3af;
}

.address-search-input {
  width: 100%;
  padding: 0.875rem 1rem 0.875rem 3rem;
  font-size: 0.9375rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  outline: none;
  transition: all 0.2s ease;
}

.address-search-input:focus {
  border-color: #10b981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}

.map-placeholder {
  position: relative;
  width: 100%;
  height: 20rem;
  background: linear-gradient(135deg, #f3f4f6 0%, #e5e7eb 100%);
  border: 2px dashed #d1d5db;
  border-radius: 0.75rem;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1rem;
}

.map-marker {
  width: 3rem;
  height: 3rem;
  color: #10b981;
}

.map-marker svg {
  width: 100%;
  height: 100%;
}

.map-placeholder-text {
  font-size: 0.875rem;
  color: #6b7280;
  text-align: center;
  max-width: 20rem;
}

.helper-text {
  font-size: 0.875rem;
  color: #ef4444;
  text-align: center;
  margin: 1rem 0;
}

/* Address Form */
.address-confirm-form {
  width: 100%;
  max-width: 32rem;
  margin: 0 auto;
}

.form-group {
  margin-bottom: 1.25rem;
}

.form-label-small {
  display: block;
  font-size: 0.75rem;
  font-weight: 600;
  color: #6b7280;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 0.5rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 0.9375rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  outline: none;
  transition: all 0.2s ease;
}

.form-input:focus {
  border-color: #10b981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}

.checkbox-group {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 1.5rem 0;
}

.checkbox-input {
  width: 1.125rem;
  height: 1.125rem;
  cursor: pointer;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.875rem;
  color: #6b7280;
  cursor: pointer;
}

.info-button {
  width: 1rem;
  height: 1rem;
  color: #9ca3af;
  background: none;
  border: none;
  cursor: help;
  padding: 0;
}

.button-group {
  display: flex;
  gap: 1rem;
  margin-top: 2rem;
}

.btn {
  flex: 1;
  padding: 0.75rem 1.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  border-radius: 0.5rem;
  border: none;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-primary {
  background-color: #111827;
  color: #ffffff;
}

.btn-primary:hover {
  background-color: #1f2937;
}

.btn-secondary {
  background-color: transparent;
  color: #ef4444;
  border: 1px solid #ef4444;
}

.btn-secondary:hover {
  background-color: #fef2f2;
}

.btn-back-link {
  display: block;
  width: 100%;
  margin-top: 1rem;
  padding: 0.5rem;
  background: none;
  border: none;
  color: #6b7280;
  font-size: 0.875rem;
  cursor: pointer;
  text-align: center;
}

.btn-back-link:hover {
  color: #111827;
  text-decoration: underline;
}

@media (max-width: 640px) {
  .map-placeholder {
    height: 16rem;
  }
}
</style>
