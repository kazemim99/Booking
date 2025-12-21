<template>
  <div class="neshan-map-picker" dir="rtl">
    <div ref="mapContainer" class="map-container"></div>

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { Map, View, Feature } from '@neshan-maps-platform/ol'
import { fromLonLat, toLonLat } from '@neshan-maps-platform/ol/proj'
import { Point } from '@neshan-maps-platform/ol/geom'
import { Vector as VectorSource } from '@neshan-maps-platform/ol/source'
import { Vector as VectorLayer } from '@neshan-maps-platform/ol/layer'
import { Style, Icon } from '@neshan-maps-platform/ol/style'

// Import Neshan Maps CSS
import '@neshan-maps-platform/ol/ol.css'

interface Props {
  modelValue?: { lat: number; lng: number } | null
  zoom?: number
  mapKey: string
  serviceKey: string
  defaultType?: 'neshan' | 'standard-day' | 'standard-night' | 'satellite-day'
  height?: string
  showSearchBox?: boolean
}

interface AddressDetails {
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
}

interface Emits {
  (e: 'update:modelValue', value: { lat: number; lng: number }): void
  (e: 'locationSelected', value: { lat: number; lng: number; address?: string; addressDetails?: AddressDetails | null }): void
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: null,
  zoom: 15,
  defaultType: 'neshan',
  height: '400px',
  showSearchBox: true,
})

const emit = defineEmits<Emits>()

const mapContainer = ref<HTMLElement | null>(null)
const searchQuery = ref('')
const currentPosition = ref<{ lat: number; lng: number } | null>(props.modelValue)
const map = ref<any>(null)
const marker = ref<any>(null)

const displayCoordinates = computed(() => {
  if (!currentPosition.value) return 'انتخاب نشده'
  return `${currentPosition.value.lat.toFixed(6)}, ${currentPosition.value.lng.toFixed(6)}`
})

onMounted(() => {
  initializeMap()
})

watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue && map.value) {
      currentPosition.value = newValue
      updateMarker(newValue.lat, newValue.lng)
      map.value.getView().setCenter(fromLonLat([newValue.lng, newValue.lat]))
    }
  }
)

const initializeMap = () => {
  if (!mapContainer.value) return

  // Default to Tehran if no position is provided
  const initialLat = currentPosition.value?.lat || 35.6892
  const initialLng = currentPosition.value?.lng || 51.389

  // Initialize the map
  const mapInstance = new Map({
    target: mapContainer.value,
    key: props.mapKey,
    mapType: props.defaultType,
    poi: true,
    traffic: false,
    view: new View({
      center: fromLonLat([initialLng, initialLat]),
      zoom: props.zoom,
    }),
  })

  map.value = mapInstance

  // Add click handler to set location
  mapInstance.on('click', async (event: any) => {
    const coordinates = toLonLat(event.coordinate)
    const lng = coordinates[0]
    const lat = coordinates[1]

    currentPosition.value = { lat, lng }
    updateMarker(lat, lng)

    // Get address details from coordinates
    const addressData = await reverseGeocode(lat, lng)

    emit('update:modelValue', { lat, lng })
    emit('locationSelected', {
      lat,
      lng,
      addressDetails: addressData
    })
  })

  // Add initial marker if position exists
  if (currentPosition.value) {
    updateMarker(currentPosition.value.lat, currentPosition.value.lng)
  }
}

const updateMarker = (lat: number, lng: number) => {
  if (!map.value) return

  // Remove existing marker
  if (marker.value) {
    map.value.removeLayer(marker.value)
  }

  // Create new marker
  const markerSource = new VectorSource({
    features: [
      new Feature({
        geometry: new Point(fromLonLat([lng, lat])),
      }),
    ],
  })

  const markerLayer = new VectorLayer({
    source: markerSource,
    style: new Style({
      image: new Icon({
        anchor: [0.5, 1],
        src: 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="32" height="42" viewBox="0 0 32 42"><path fill="%23FF0000" d="M16 0C7.2 0 0 7.2 0 16c0 8.8 16 26 16 26s16-17.2 16-26c0-8.8-7.2-16-16-16zm0 22c-3.3 0-6-2.7-6-6s2.7-6 6-6 6 2.7 6 6-2.7 6-6 6z"/></svg>',
        scale: 1,
      }),
    }),
  })

  marker.value = markerLayer
  map.value.addLayer(markerLayer)
}

const reverseGeocode = async (lat: number, lng: number) => {
  if (!props.serviceKey) return null

  try {
    // Use Neshan Reverse Geocoding API
    const response = await fetch(
      `https://api.neshan.org/v5/reverse?lat=${lat}&lng=${lng}`,
      {
        headers: {
          'Api-Key': props.serviceKey,
        },
      }
    )

    if (!response.ok) {
      console.error('Reverse geocoding failed:', response.statusText)
      return null
    }

    const data = await response.json()
    console.log('Reverse geocoding response:', data)

    return {
      formattedAddress: data.formatted_address || '',
      neighbourhood: data.neighbourhood || '',
      city: data.city || '',
      state: data.state || '',
      address: data.address || '',
      route: data.route_name || data.route || '',
      district: data.district || '',
      village: data.village || '',
      county: data.county || '',
      postalCode: data.postal_code || ''
    }
  } catch (error) {
    console.error('Reverse geocoding error:', error)
    return null
  }
}

const handleSearch = async () => {
  if (!searchQuery.value.trim() || !props.serviceKey) return

  try {
    // Use Neshan Search API
    const response = await fetch(
      `https://api.neshan.org/v1/search?term=${encodeURIComponent(searchQuery.value)}&lat=${currentPosition.value?.lat || 35.6892}&lng=${currentPosition.value?.lng || 51.389}`,
      {
        headers: {
          'Api-Key': props.serviceKey,
        },
      }
    )

    const data = await response.json()

    if (data.items && data.items.length > 0) {
      const firstResult = data.items[0]
      const lat = firstResult.location.y
      const lng = firstResult.location.x

      currentPosition.value = { lat, lng }
      updateMarker(lat, lng)
      map.value.getView().setCenter(fromLonLat([lng, lat]))
      map.value.getView().setZoom(16)

      // Get detailed address info via reverse geocoding
      const addressData = await reverseGeocode(lat, lng)

      emit('update:modelValue', { lat, lng })
      emit('locationSelected', {
        lat,
        lng,
        address: firstResult.title,
        addressDetails: addressData
      })
    }
  } catch (error) {
    console.error('Search error:', error)
  }
}
</script>

<style scoped lang="scss">
.neshan-map-picker {
  position: relative;
  width: 100%;
}

.map-container {
  width: 100%;
  height: v-bind(height);
  border-radius: 0.5rem;
  overflow: hidden;
  border: 2px solid #e5e7eb;
}

.search-box {
  position: absolute;
  top: 1rem;
  right: 1rem;
  left: 1rem;
  display: flex;
  gap: 0.5rem;
  z-index: 1000;
}

.search-input {
  flex: 1;
  padding: 0.75rem 1rem;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-family: inherit;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

  &:focus {
    outline: none;
    border-color: #8b5cf6;
    box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
  }
}

.search-btn {
  width: 2.75rem;
  height: 2.75rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #8b5cf6;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: background 0.2s;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

  svg {
    width: 1.25rem;
    height: 1.25rem;
    stroke: white;
    stroke-width: 2;
  }

  &:hover {
    background: #7c3aed;
  }

  &:active {
    transform: scale(0.95);
  }
}

.coordinates-display {
  margin-top: 0.75rem;
  padding: 0.75rem 1rem;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  display: flex;
  gap: 0.5rem;
  font-size: 0.875rem;
}

.coord-label {
  font-weight: 500;
  color: #6b7280;
}

.coord-value {
  color: #111827;
  font-family: monospace;
  direction: ltr;
}
</style>
