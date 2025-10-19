<template>
  <StepContainer
    :title="
      showConfirm
        ? $t('provider.registration.location.confirmTitle')
        : $t('provider.registration.location.title')
    "
    :subtitle="$t('provider.registration.location.subtitle')"
  >
    <!-- Step 1: Map Address Selection -->
    <div v-if="!showConfirm" class="location-map-container">
      <div class="address-search">
        <div class="search-input-wrapper">
          <svg class="search-icon" viewBox="0 0 20 20" fill="currentColor">
            <path
              fill-rule="evenodd"
              d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"
              clip-rule="evenodd"
            />
          </svg>
          <input
            ref="searchInput"
            v-model="searchQuery"
            type="text"
            class="address-search-input"
            :placeholder="$t('provider.registration.location.searchPlaceholder')"
            @input="handleAddressSearch"
            @keydown.enter.prevent="handleSearchEnter"
          />
        </div>
        <!-- Search Results Dropdown -->
        <div v-if="searchResults.length > 0" class="search-results">
          <div
            v-for="(result, index) in searchResults"
            :key="index"
            class="search-result-item"
            @click="selectSearchResult(result)"
          >
            <div class="result-title">{{ result.title }}</div>
            <div class="result-address">{{ result.address }}</div>
          </div>
        </div>
      </div>

      <!-- Neshan Map with Loading/Error States -->
      <div class="map-wrapper">
        <!-- Loading State -->
        <div v-if="isMapLoading" class="map-loading">
          <div class="spinner"></div>
          <p>{{ $t('provider.registration.location.loadingMap') || 'Loading map...' }}</p>
        </div>

        <!-- Error State -->
        <div v-else-if="mapError" class="map-error">
          <svg class="error-icon" viewBox="0 0 20 20" fill="currentColor">
            <path
              fill-rule="evenodd"
              d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
              clip-rule="evenodd"
            />
          </svg>
          <p>{{ mapError }}</p>
          <button @click="retryMapInit" class="btn-retry">
            {{ $t('common.retry') || 'Retry' }}
          </button>
        </div>

        <!-- Neshan Map -->
        <NeshanMap
          v-else
          defaultType="neshan"
          mapKey="web.741ff28152504624a0b3942d3621b56d"
          serviceKey="service.qBDJpu7hKVBEAzERghfm9JM7vqGKXoNNNTdtrGy7"
          :center="mapCenter"
          :zoom="mapZoom"
          :poi="true"
          :traffic="false"
          style="width: 100%; height: 100%;"
          @on-init="onMapInit"
        />
      </div>

      <p class="helper-text">
        {{ formattedAddress || $t('provider.registration.location.enterAddressHelp') }}
      </p>
      <p
        v-if="formattedAddress && locationData.latitude && locationData.longitude"
        class="coordinates-text"
      >
        {{ $t('provider.registration.location.coordinates') }}:
        {{ locationData.latitude.toFixed(6) }}, {{ locationData.longitude.toFixed(6) }}
      </p>

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
          <input v-model="addressData.addressLine1" type="text" class="form-input" required />
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
          <input v-model="addressData.city" type="text" class="form-input" required />
        </div>

        <!-- Zip Code -->
        <div class="form-group">
          <label class="form-label-small">
            {{ $t('provider.registration.location.zipCode') }}
          </label>
          <input v-model="addressData.zipCode" type="text" class="form-input" required />
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
            <button
              type="button"
              class="info-button"
              :title="$t('provider.registration.location.sharedLocationHint')"
            >
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path
                  fill-rule="evenodd"
                  d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
                  clip-rule="evenodd"
                />
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
import { ref, computed, watch, onUnmounted } from 'vue'
import NeshanMap from '@neshan-maps-platform/vue3-openlayers'
import { Map } from '@neshan-maps-platform/ol'
import { fromLonLat, toLonLat } from '@neshan-maps-platform/ol/proj'
import { Vector as VectorLayer } from '@neshan-maps-platform/ol/layer'
import { Vector as VectorSource } from '@neshan-maps-platform/ol/source'
import { Feature } from '@neshan-maps-platform/ol'
import { Point } from '@neshan-maps-platform/ol/geom'
import { Style, Icon } from '@neshan-maps-platform/ol/style'
import StepContainer from '../shared/StepContainer.vue'
import NavigationButtons from '../shared/NavigationButtons.vue'
import type { BusinessAddress, BusinessLocation } from '@/modules/provider/types/registration.types'
import type { PrimarySearchItem, PrimaryReverseResult } from '@/types/neshan-maps'

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
const searchResults = ref<PrimarySearchItem[]>([])
const searchTimeout = ref<NodeJS.Timeout | null>(null)

// Map state
const isMapLoading = ref(true)
const mapError = ref<string | null>(null)

let map: Map | null = null
let markerLayer: VectorLayer<VectorSource> | null = null
let markerSource: VectorSource | null = null

const addressData = ref<Partial<BusinessAddress>>({
  addressLine1: props.address?.addressLine1 || '',
  addressLine2: props.address?.addressLine2 || '',
  city: props.address?.city || '',
  zipCode: props.address?.zipCode || '',
  isShared: props.address?.isShared || false,
})

const locationData = ref<Partial<BusinessLocation>>({
  latitude: props.location?.latitude || 35.6892,
  longitude: props.location?.longitude || 51.389,
  formattedAddress: props.location?.formattedAddress || '',
})

const mapCenter = computed(() => ({
  latitude: locationData.value.latitude || 35.6892,
  longitude: locationData.value.longitude || 51.389,
}))

const mapZoom = ref(14)

// Initialize map with error handling
const onMapInit = (mapInstance: Map) => {
  try {
    map = mapInstance

    // Wait for map to be fully ready
    setTimeout(() => {
      if (!map) {
        throw new Error('Map instance not available')
      }

      // Create marker source and layer
      markerSource = new VectorSource()
      markerLayer = new VectorLayer({
        source: markerSource,
        style: new Style({
          image: new Icon({
            anchor: [0.5, 1],
            src: 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="%2310b981"><path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/></svg>',
            scale: 1.5,
          }),
        }),
      })

      map.addLayer(markerLayer)

      // Add initial marker
      updateMarker(locationData.value.longitude || 51.389, locationData.value.latitude || 35.6892)

      // Get initial address
      if (locationData.value.latitude && locationData.value.longitude) {
        reverseGeocode(locationData.value.latitude, locationData.value.longitude)
      }

      // Add click listener
      map.on('click', (event) => {
        const coords = toLonLat(event.coordinate)
        const [lng, lat] = coords
        updateMarker(lng, lat)
        locationData.value.latitude = lat
        locationData.value.longitude = lng
        reverseGeocode(lat, lng)
      })

      isMapLoading.value = false
      console.log('✅ Neshan Map initialized successfully')
    }, 100) // Small delay to ensure map canvas is ready
  } catch (error) {
    console.error('❌ Map initialization error:', error)
    isMapLoading.value = false
    mapError.value = 'Failed to initialize map. Please check your API keys.'
  }
}

// Retry map initialization
const retryMapInit = () => {
  isMapLoading.value = true
  mapError.value = null
  // Trigger re-render by updating map center
  locationData.value = { ...locationData.value }
}

// Update marker position
const updateMarker = (lng: number, lat: number) => {
  if (!markerSource) return

  markerSource.clear()
  const marker = new Feature({
    geometry: new Point(fromLonLat([lng, lat])),
  })
  markerSource.addFeature(marker)

  // Center map on marker
  if (map) {
    const view = map.getView()
    view?.animate({
      center: fromLonLat([lng, lat]),
      duration: 500,
    })
  }
}

// Handle address search with debounce
const handleAddressSearch = () => {
  if (searchTimeout.value) {
    clearTimeout(searchTimeout.value)
  }

  if (searchQuery.value.length < 3) {
    searchResults.value = []
    return
  }

  searchTimeout.value = setTimeout(() => {
    searchAddress(searchQuery.value)
  }, 500)
}

// Search for address using Neshan Search API
const searchAddress = async (query: string) => {
  try {
    // Using Neshan Search API
    const response = await fetch(
      `https://api.neshan.org/v1/search?term=${encodeURIComponent(query)}&lat=${mapCenter.value.latitude}&lng=${mapCenter.value.longitude}`,
      {
        headers: {
          'Api-Key': 'service.qBDJpu7hKVBEAzERghfm9JM7vqGKXoNNNTdtrGy7',
        },
      },
    )

    if (response.ok) {
      const data = await response.json()
      searchResults.value = data.items || []
    } else {
      console.error('Search failed:', response.statusText)
      searchResults.value = []
    }
  } catch (error) {
    console.error('Error searching address:', error)
    searchResults.value = []
  }
}

// Handle search enter key
const handleSearchEnter = () => {
  if (searchResults.value.length > 0) {
    selectSearchResult(searchResults.value[0])
  }
}

// Select search result
const selectSearchResult = (result: PrimarySearchItem) => {
  const { x: lng, y: lat } = result.location
  locationData.value.latitude = lat
  locationData.value.longitude = lng
  formattedAddress.value = result.address || result.title
  searchQuery.value = result.title
  searchResults.value = []

  // Update marker and map
  updateMarker(lng, lat)

  // Parse address
  parseAddressFromResult(result)
}

// Reverse geocode to get address from coordinates
const reverseGeocode = async (lat: number, lng: number) => {
  try {
    const response = await fetch(`https://api.neshan.org/v5/reverse?lat=${lat}&lng=${lng}`, {
      headers: {
        'Api-Key': 'service.qBDJpu7hKVBEAzERghfm9JM7vqGKXoNNNTdtrGy7',
      },
    })

    if (response.ok) {
      const data: PrimaryReverseResult = await response.json()
      formattedAddress.value = data.formatted_address || ''
      locationData.value.formattedAddress = data.formatted_address || ''

      // Parse address components
      parseAddressFromReverse(data)
    } else {
      console.error('Reverse geocoding failed:', response.statusText)
    }
  } catch (error) {
    console.error('Error reverse geocoding:', error)
  }
}

// Parse address from search result
const parseAddressFromResult = (result: PrimarySearchItem) => {
  addressData.value.addressLine1 = result.title || ''
  addressData.value.city = result.region || ''
  addressData.value.zipCode = ''
}

// Parse address from reverse geocoding result
const parseAddressFromReverse = (data: PrimaryReverseResult) => {
  addressData.value.addressLine1 = data.route_name || data.neighbourhood || ''
  addressData.value.city = data.city || ''
  addressData.value.zipCode = ''
}

const handleNextToConfirm = () => {
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

watch(
  () => props.address,
  (newVal) => {
    if (newVal) {
      addressData.value = { ...addressData.value, ...newVal }
    }
  },
  { deep: true },
)

// Cleanup
onUnmounted(() => {
  if (searchTimeout.value) {
    clearTimeout(searchTimeout.value)
  }
  searchResults.value = []
})
</script>

<!-- IMPORTANT: Import Neshan CSS globally (not scoped) -->
<style>
@import url('@neshan-maps-platform/vue3-openlayers/dist/style.css');
</style>

<style scoped>
/* Map Container */
.location-map-container {
  width: 100%;
}

.address-search {
  margin-bottom: 1.5rem;
  position: relative;
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
  z-index: 1;
}

[dir='rtl'] .search-icon {
  left: auto;
  right: 1rem;
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

[dir='rtl'] .address-search-input {
  padding: 0.875rem 3rem 0.875rem 1rem;
}

.address-search-input:focus {
  border-color: #10b981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}

/* Search Results Dropdown */
.search-results {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  margin-top: 0.5rem;
  background: #ffffff;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
  max-height: 20rem;
  overflow-y: auto;
  z-index: 10;
}

.search-result-item {
  padding: 0.75rem 1rem;
  cursor: pointer;
  transition: background-color 0.2s ease;
  border-bottom: 1px solid #f3f4f6;
}

.search-result-item:last-child {
  border-bottom: none;
}

.search-result-item:hover {
  background-color: #f9fafb;
}

.result-title {
  font-size: 0.9375rem;
  font-weight: 600;
  color: #111827;
  margin-bottom: 0.25rem;
}

.result-address {
  font-size: 0.8125rem;
  color: #6b7280;
}

/* Map Wrapper */
.map-wrapper {
  position: relative;
  width: 100%;
  height: 20rem;
  background: #f3f4f6;
  border-radius: 0.75rem;
  margin-bottom: 1rem;
  overflow: hidden;
  box-shadow:
    0 4px 6px -1px rgba(0, 0, 0, 0.1),
    0 2px 4px -1px rgba(0, 0, 0, 0.06);
}

/* Loading State */
.map-loading,
.map-error {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  background: #f9fafb;
  border-radius: 0.75rem;
  gap: 1rem;
}

.map-loading p,
.map-error p {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.spinner {
  width: 3rem;
  height: 3rem;
  border: 4px solid #e5e7eb;
  border-top-color: #10b981;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.error-icon {
  width: 3rem;
  height: 3rem;
  color: #ef4444;
}

.btn-retry {
  padding: 0.5rem 1.5rem;
  background-color: #10b981;
  color: white;
  border: none;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-retry:hover {
  background-color: #059669;
}

.helper-text {
  font-size: 0.875rem;
  color: #6b7280;
  text-align: center;
  margin: 1rem 0 0.5rem;
}

.coordinates-text {
  font-size: 0.75rem;
  color: #9ca3af;
  text-align: center;
  font-family: monospace;
  margin: 0 0 1rem;
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
  .map-wrapper {
    height: 16rem;
  }
}
</style>
