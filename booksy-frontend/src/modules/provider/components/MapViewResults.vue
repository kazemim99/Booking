<template>
  <div class="map-view-container">
    <!-- Map View Header with View Toggle -->
    <div class="map-view-header">
      <div class="header-info">
        <h2 class="header-title">نمایش نقشه</h2>
        <p class="header-count">{{ visibleProviders.length }} ارائه‌دهنده</p>
      </div>

      <div class="view-toggle">
        <button
          class="view-btn"
          @click="$emit('viewModeChange', 'grid')"
          title="نمایش شبکه‌ای"
        >
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
          </svg>
        </button>
        <button
          class="view-btn"
          @click="$emit('viewModeChange', 'list')"
          title="نمایش لیستی"
        >
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
        <button
          class="view-btn active"
          title="نمایش نقشه"
        >
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Neshan Map -->
    <div class="map-wrapper">
      <div ref="mapContainer" class="map-canvas"></div>

      <!-- Map Controls -->
      <div class="map-controls">
        <button @click="recenterMap" class="control-btn" title="مرکز نقشه">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
          </svg>
        </button>
        <button @click="zoomIn" class="control-btn" title="بزرگنمایی">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
        </button>
        <button @click="zoomOut" class="control-btn" title="کوچکنمایی">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 12H4" />
          </svg>
        </button>
      </div>

      <!-- Loading Overlay -->
      <div v-if="loading" class="map-loading">
        <div class="loading-spinner"></div>
        <p>در حال بارگذاری نقشه...</p>
      </div>
    </div>

    <!-- Floating Provider Cards -->
    <transition-group name="slide-cards" tag="div" class="floating-cards">
      <div
        v-for="(provider, index) in visibleProviders"
        :key="provider.id"
        :class="['floating-card', { selected: selectedProviderId === provider.id }]"
        @click="selectProvider(provider)"
      >
        <div class="card-image">
          <img v-if="provider.logoUrl" :src="provider.logoUrl" :alt="provider.businessName" />
          <div v-else class="card-placeholder">
            {{ getInitials(provider.businessName) }}
          </div>
        </div>
        <div class="card-content">
          <h4>{{ provider.businessName }}</h4>
          <div class="card-rating">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path d="M10.788 3.21c.448-1.077 1.976-1.077 2.424 0l2.082 5.007 5.404.433c1.164.093 1.636 1.545.749 2.305l-4.117 3.527 1.257 5.273c.271 1.136-.964 2.033-1.96 1.425L12 18.354 7.373 21.18c-.996.608-2.231-.29-1.96-1.425l1.257-5.273-4.117-3.527c-.887-.76-.415-2.212.749-2.305l5.404-.433 2.082-5.006z" />
            </svg>
            <span>{{ provider.averageRating?.toFixed(1) || 'N/A' }}</span>
          </div>
          <p class="card-location">{{ provider.city }}، {{ provider.state }}</p>
        </div>
        <button class="card-book-btn" @click.stop="$emit('book', provider)">
          رزرو
        </button>
      </div>
    </transition-group>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, computed } from 'vue'
import { Map, View, Feature } from '@neshan-maps-platform/ol'
import { fromLonLat } from '@neshan-maps-platform/ol/proj'
import { Point } from '@neshan-maps-platform/ol/geom'
import { Vector as VectorSource } from '@neshan-maps-platform/ol/source'
import { Vector as VectorLayer } from '@neshan-maps-platform/ol/layer'
import { Style, Icon } from '@neshan-maps-platform/ol/style'
import type { ProviderSummary } from '../types/provider.types'

import '@neshan-maps-platform/ol/ol.css'

interface Props {
  providers: ProviderSummary[]
  loading?: boolean
  mapKey?: string
  serviceKey?: string
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
  mapKey: import.meta.env.VITE_NESHAN_MAP_KEY,
  serviceKey: import.meta.env.VITE_NESHAN_SERVICE_KEY,
})

const emit = defineEmits<{
  (e: 'providerClick', provider: ProviderSummary): void
  (e: 'book', provider: ProviderSummary): void
  (e: 'viewModeChange', mode: 'grid' | 'list' | 'map'): void
}>()

const mapContainer = ref<HTMLElement | null>(null)
const map = ref<any>(null)
const markersLayer = ref<any>(null)
const selectedProviderId = ref<string | null>(null)

// Filter providers with valid coordinates
const visibleProviders = computed(() => {
  return props.providers.filter(p => {
    // Check if coordinates exist in address object or directly on provider
    const hasCoords = (p.address?.latitude && p.address?.longitude) ||
                     ((p as any).latitude && (p as any).longitude)
    return hasCoords
  }).slice(0, 20) // Limit to 20 for performance
})

onMounted(() => {
  initializeMap()
})

onUnmounted(() => {
  // Clean up map instance to prevent memory leaks
  if (map.value) {
    console.log('[MapView] Cleaning up map instance')
    map.value.setTarget(null)
    map.value = null
  }
  markersLayer.value = null
})

watch(() => props.providers, () => {
  if (map.value && markersLayer.value) {
    updateMarkers()
  }
}, { deep: true })

const initializeMap = () => {
  if (!mapContainer.value) {
    console.warn('[MapView] Map container not found, skipping initialization')
    return
  }

  // Don't reinitialize if map already exists
  if (map.value) {
    console.log('[MapView] Map already initialized')
    return
  }

  console.log('[MapView] Initializing map with key:', props.mapKey)

  // Initialize map centered on Tehran
  const mapInstance = new Map({
    target: mapContainer.value,
    key: props.mapKey,
    maptype: 'neshan',
    poi: true,
    traffic: false,
    view: new View({
      center: fromLonLat([51.389, 35.6892]), // Tehran
      zoom: 12,
    }),
  })

  map.value = mapInstance

  // Create markers layer
  const markerSource = new VectorSource()
  const markerLayer = new VectorLayer({
    source: markerSource,
    zIndex: 1000,
  })

  markersLayer.value = markerLayer
  mapInstance.addLayer(markerLayer)

  // Add initial markers
  updateMarkers()

  // Center map if we have providers
  if (visibleProviders.value.length > 0) {
    centerMapOnProviders()
  }

  console.log('[MapView] Map initialized with', visibleProviders.value.length, 'providers')
}

const updateMarkers = () => {
  if (!map.value || !markersLayer.value) return

  const source = markersLayer.value.getSource()
  source.clear()

  console.log('[MapView] Updating markers for', visibleProviders.value.length, 'providers')

  visibleProviders.value.forEach((provider, index) => {
    const lat = provider.address?.latitude || (provider as any).latitude
    const lng = provider.address?.longitude || (provider as any).longitude

    if (!lat || !lng) {
      console.warn('[MapView] Provider missing coordinates:', provider.id, provider.businessName)
      return
    }

    console.log('[MapView] Adding marker for', provider.businessName, 'at', lat, lng)

    const marker = new Feature({
      geometry: new Point(fromLonLat([lng, lat])),
      provider: provider,
    })

    // Create custom marker style with number
    const isSelected = selectedProviderId.value === provider.id
    const markerColor = isSelected ? '%238B5CF6' : '%23EF4444'

    marker.setStyle(new Style({
      image: new Icon({
        anchor: [0.5, 1],
        src: `data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="40" height="52" viewBox="0 0 40 52"><path fill="${markerColor}" d="M20 0C8.9 0 0 8.9 0 20c0 11 20 32 20 32s20-21 20-32c0-11.1-8.9-20-20-20zm0 27c-3.9 0-7-3.1-7-7s3.1-7 7-7 7 3.1 7 7-3.1 7-7 7z"/><text x="20" y="22" font-family="Arial" font-size="14" font-weight="bold" fill="white" text-anchor="middle">${index + 1}</text></svg>`,
        scale: 1,
      }),
    }))

    source.addFeature(marker)
  })
}

const centerMapOnProviders = () => {
  if (!map.value || visibleProviders.value.length === 0) return

  // Calculate bounds
  let minLat = Infinity, maxLat = -Infinity
  let minLng = Infinity, maxLng = -Infinity

  visibleProviders.value.forEach(provider => {
    const lat = provider.address?.latitude || (provider as any).latitude
    const lng = provider.address?.longitude || (provider as any).longitude

    if (lat && lng) {
      minLat = Math.min(minLat, lat)
      maxLat = Math.max(maxLat, lat)
      minLng = Math.min(minLng, lng)
      maxLng = Math.max(maxLng, lng)
    }
  })

  // Center on average position
  const centerLat = (minLat + maxLat) / 2
  const centerLng = (minLng + maxLng) / 2

  console.log('[MapView] Centering map on', centerLat, centerLng)

  map.value.getView().setCenter(fromLonLat([centerLng, centerLat]))

  // Adjust zoom based on spread
  const latDiff = maxLat - minLat
  const lngDiff = maxLng - minLng
  const maxDiff = Math.max(latDiff, lngDiff)

  let zoom = 13
  if (maxDiff > 0.1) zoom = 11
  if (maxDiff > 0.5) zoom = 9
  if (maxDiff < 0.01) zoom = 15

  map.value.getView().setZoom(zoom)
}

const selectProvider = (provider: ProviderSummary) => {
  selectedProviderId.value = provider.id

  const lat = provider.address?.latitude || (provider as any).latitude
  const lng = provider.address?.longitude || (provider as any).longitude

  if (lat && lng) {
    map.value?.getView().animate({
      center: fromLonLat([lng, lat]),
      zoom: 16,
      duration: 500,
    })
  }

  updateMarkers() // Update marker colors
  emit('providerClick', provider)
}

const recenterMap = () => {
  centerMapOnProviders()
}

const zoomIn = () => {
  const view = map.value?.getView()
  if (view) {
    view.animate({
      zoom: view.getZoom() + 1,
      duration: 300,
    })
  }
}

const zoomOut = () => {
  const view = map.value?.getView()
  if (view) {
    view.animate({
      zoom: view.getZoom() - 1,
      duration: 300,
    })
  }
}

const getInitials = (name: string): string => {
  return name
    .split(' ')
    .map(word => word[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}
</script>

<style scoped>
.map-view-container {
  position: relative;
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

/* Map View Header */
.map-view-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  background: white;
  border-radius: 16px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.06);
}

.header-info {
  flex: 1;
}

.header-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: #111827;
  margin: 0 0 0.5rem 0;
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.header-count {
  font-size: 1rem;
  color: #6b7280;
  margin: 0;
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

/* View Toggle Buttons */
.view-toggle {
  display: flex;
  gap: 0.5rem;
  background: #f3f4f6;
  padding: 0.25rem;
  border-radius: 8px;
}

.view-btn {
  padding: 0.5rem;
  background: transparent;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.view-btn svg {
  width: 20px;
  height: 20px;
  color: #6b7280;
}

.view-btn:hover {
  background: white;
}

.view-btn.active {
  background: #8b5cf6;
}

.view-btn.active svg {
  color: white;
}

/* Map Wrapper */
.map-view-container {
  position: relative;
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.map-wrapper {
  position: relative;
  width: 100%;
  height: calc(100vh - 280px);
  min-height: 500px;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.1);
}

.map-canvas {
  width: 100%;
  height: 100%;
}

/* Map Controls */
.map-controls {
  position: absolute;
  top: 1rem;
  right: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  z-index: 1000;
}

.control-btn {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: white;
  border: none;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  cursor: pointer;
  transition: all 0.3s ease;
}

.control-btn svg {
  width: 20px;
  height: 20px;
  color: #374151;
}

.control-btn:hover {
  background: #8b5cf6;
  transform: scale(1.1);
}

.control-btn:hover svg {
  color: white;
}

/* Loading Overlay */
.map-loading {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(255, 255, 255, 0.9);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 2000;
}

.loading-spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #e5e7eb;
  border-top-color: #8b5cf6;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.map-loading p {
  margin-top: 1rem;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  color: #6b7280;
  font-size: 0.95rem;
}

/* Floating Cards */
.floating-cards {
  position: absolute;
  bottom: 1.5rem;
  left: 1.5rem;
  right: 1.5rem;
  display: flex;
  gap: 1rem;
  overflow-x: auto;
  padding: 0.5rem;
  z-index: 1000;
  scrollbar-width: thin;
}

.floating-card {
  flex-shrink: 0;
  width: 280px;
  background: white;
  border-radius: 16px;
  padding: 1rem;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  display: flex;
  gap: 1rem;
}

.floating-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.2);
}

.floating-card.selected {
  border: 2px solid #8b5cf6;
  transform: scale(1.05);
}

.card-image {
  width: 60px;
  height: 60px;
  border-radius: 12px;
  overflow: hidden;
  flex-shrink: 0;
}

.card-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.card-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
  color: white;
  font-weight: 700;
  font-size: 1.25rem;
}

.card-content {
  flex: 1;
  min-width: 0;
}

.card-content h4 {
  margin: 0 0 0.25rem 0;
  font-size: 0.95rem;
  font-weight: 700;
  color: #111827;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.card-rating {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  margin-bottom: 0.25rem;
}

.card-rating svg {
  width: 14px;
  height: 14px;
  color: #fbbf24;
}

.card-rating span {
  font-size: 0.875rem;
  font-weight: 600;
  color: #111827;
}

.card-location {
  font-size: 0.8rem;
  color: #6b7280;
  margin: 0;
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.card-book-btn {
  padding: 0.5rem 1rem;
  background: #8b5cf6;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  align-self: center;
}

.card-book-btn:hover {
  background: #7c3aed;
  transform: scale(1.05);
}

/* Slide Cards Animation */
.slide-cards-enter-active,
.slide-cards-leave-active {
  transition: all 0.3s ease;
}

.slide-cards-enter-from {
  opacity: 0;
  transform: translateY(20px);
}

.slide-cards-leave-to {
  opacity: 0;
  transform: translateY(-20px);
}

/* Mobile Optimizations */
@media (max-width: 768px) {
  .map-view-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
    padding: 1rem;
  }

  .view-toggle {
    width: 100%;
    justify-content: center;
  }

  .map-wrapper {
    height: calc(100vh - 200px);
    min-height: 400px;
    border-radius: 12px;
  }

  .floating-cards {
    bottom: 1rem;
    left: 1rem;
    right: 1rem;
  }

  .floating-card {
    width: 240px;
  }

  .map-controls {
    top: 0.75rem;
    right: 0.75rem;
  }

  .control-btn {
    width: 40px;
    height: 40px;
  }
}

/* Custom Scrollbar for Floating Cards */
.floating-cards::-webkit-scrollbar {
  height: 6px;
}

.floating-cards::-webkit-scrollbar-track {
  background: transparent;
}

.floating-cards::-webkit-scrollbar-thumb {
  background: rgba(0, 0, 0, 0.2);
  border-radius: 3px;
}

.floating-cards::-webkit-scrollbar-thumb:hover {
  background: rgba(0, 0, 0, 0.3);
}
</style>
