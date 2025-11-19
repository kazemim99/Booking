# 🚀 Provider Search - Complete Implementation Guide

## ✅ Completed Features

### 1. **ProviderSearchView** - Fully Implemented ✓
- Mobile filter drawer with slide-in animation
- Quick stats bar in Persian
- Floating filter button
- Scroll to top button
- Mobile backdrop with blur effect

### 2. **ProviderFilters** - Fully Implemented ✓
- ✨ Voice Search with Persian language (fa-IR)
- 📍 Geolocation "موقعیت من" button
- 🎨 Visual price range buttons
- 🏷️ Type chips
- 🔄 iOS-style toggle switches
- ⭐ Visual star ratings
- 🇮🇷 Full Persian localization

---

## 📝 Remaining Implementations

### 3. ProviderSearchResults - Skeleton Loaders & Animations

#### Step 1: Update ProviderSearchResults.vue Template

Replace the loading state section (lines 39-43) with:

```vue
<!-- Loading State with Skeleton -->
<div v-if="loading" :class="['skeleton-container', `view-mode-${viewMode}`]">
  <div
    v-for="i in skeletonCount"
    :key="`skeleton-${i}`"
    class="skeleton-card"
    :style="{ animationDelay: `${i * 0.08}s` }"
  >
    <div class="skeleton-image">
      <div class="skeleton-shimmer"></div>
    </div>
    <div class="skeleton-content">
      <div class="skeleton-title"></div>
      <div class="skeleton-text"></div>
      <div class="skeleton-text short"></div>
      <div class="skeleton-footer">
        <div class="skeleton-badge"></div>
        <div class="skeleton-badge"></div>
      </div>
    </div>
  </div>
</div>
```

#### Step 2: Update Results Container (lines 57-70) with Stagger Animation

```vue
<!-- Results Grid/List with Stagger Animation -->
<TransitionGroup
  v-else
  name="stagger"
  tag="div"
  :class="['results-container', `view-mode-${viewMode}`]"
  appear
>
  <ProviderCard
    v-for="(provider, index) in providers"
    :key="provider.id"
    :provider="provider"
    :view-mode="viewMode"
    :style="{ '--stagger-delay': `${index * 0.05}s` }"
    @click="handleProviderClick(provider)"
    @book="handleBookClick(provider)"
  />
</TransitionGroup>
```

#### Step 3: Add to Script Section (after line 113)

```typescript
// Add skeleton count computed
const skeletonCount = computed(() => {
  return viewMode.value === 'grid' ? 9 : 6
})
```

#### Step 4: Add to Styles Section (after line 441)

```css
/* Skeleton Loading States */
.skeleton-container {
  display: grid;
  gap: 1.5rem;
}

.skeleton-container.view-mode-grid {
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
}

.skeleton-container.view-mode-list {
  grid-template-columns: 1fr;
}

.skeleton-card {
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 12px;
  overflow: hidden;
  animation: skeleton-fade 0.6s ease-out;
}

@keyframes skeleton-fade {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.skeleton-image {
  position: relative;
  width: 100%;
  height: 200px;
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  overflow: hidden;
}

.skeleton-shimmer {
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(
    90deg,
    transparent,
    rgba(255, 255, 255, 0.5),
    transparent
  );
  animation: shimmer 1.5s infinite;
}

@keyframes shimmer {
  0% {
    left: -100%;
  }
  100% {
    left: 100%;
  }
}

.skeleton-content {
  padding: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.skeleton-title {
  height: 24px;
  width: 70%;
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  border-radius: 4px;
  animation: shimmer-bg 1.5s infinite;
}

.skeleton-text {
  height: 16px;
  width: 100%;
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  border-radius: 4px;
  animation: shimmer-bg 1.5s infinite;
}

.skeleton-text.short {
  width: 60%;
}

.skeleton-footer {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.skeleton-badge {
  height: 28px;
  width: 80px;
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  border-radius: 50px;
  animation: shimmer-bg 1.5s infinite;
}

@keyframes shimmer-bg {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}

/* Stagger Animation for Results */
.stagger-move,
.stagger-enter-active {
  transition: all 0.5s cubic-bezier(0.4, 0, 0.2, 1);
}

.stagger-enter-from {
  opacity: 0;
  transform: translateY(30px) scale(0.95);
}

.stagger-enter-to {
  opacity: 1;
  transform: translateY(0) scale(1);
}

.stagger-enter-active {
  animation-delay: var(--stagger-delay);
}

.stagger-leave-active {
  position: absolute;
  transition: all 0.3s ease;
}

.stagger-leave-to {
  opacity: 0;
  transform: scale(0.9);
}

/* Mobile Skeleton Optimizations */
@media (max-width: 768px) {
  .skeleton-container.view-mode-grid {
    grid-template-columns: 1fr;
  }

  .skeleton-image {
    height: 180px;
  }
}
```

---

### 4. ProviderCard - Persian Typography & Iranian Design

#### Update ProviderCard.vue

Add this to the template (after line 65):

```vue
<!-- Persian Rating Display -->
<div v-if="provider.averageRating" class="provider-rating-persian">
  <div class="stars-row">
    <svg
      v-for="i in 5"
      :key="i"
      :class="['star-icon', { filled: i <= Math.round(provider.averageRating) }]"
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="currentColor"
    >
      <path d="M10.788 3.21c.448-1.077 1.976-1.077 2.424 0l2.082 5.007 5.404.433c1.164.093 1.636 1.545.749 2.305l-4.117 3.527 1.257 5.273c.271 1.136-.964 2.033-1.96 1.425L12 18.354 7.373 21.18c-.996.608-2.231-.29-1.96-1.425l1.257-5.273-4.117-3.527c-.887-.76-.415-2.212.749-2.305l5.404-.433 2.082-5.006z" />
    </svg>
  </div>
  <span class="rating-number">{{ formatPersianNumber(provider.averageRating.toFixed(1)) }}</span>
  <span v-if="provider.totalReviews" class="reviews-count">
    ({{ formatPersianNumber(provider.totalReviews) }} نظر)
  </span>
</div>
```

Add to script section:

```typescript
// Persian number formatter
const formatPersianNumber = (num: number | string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return String(num).replace(/\d/g, (d) => persianDigits[parseInt(d)])
}

// Persian location formatter
const formatPersianLocation = (): string => {
  const parts = [provider.city, provider.state]
    .filter(Boolean)
    .map(part => part)
  return parts.join('، ')
}
```

Replace the "Book Now" button (line 134-136) with Persian version:

```vue
<button v-if="showBookButton" class="btn-book-persian" @click.stop="handleBookClick">
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
  </svg>
  رزرو نوبت
</button>
```

Add to styles (Persian design patterns):

```css
/* Persian Typography */
.provider-card {
  font-family: 'Vazir', 'IRANSans', 'Tahoma', sans-serif;
  direction: rtl;
}

.provider-name {
  font-family: 'Vazir FD', 'Vazir', sans-serif;
  font-weight: 900;
  letter-spacing: -0.02em;
  background: linear-gradient(135deg, var(--color-text-primary) 0%, var(--color-primary) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

/* Persian Rating Display */
.provider-rating-persian {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0;
}

.stars-row {
  display: flex;
  gap: 0.125rem;
}

.star-icon {
  width: 16px;
  height: 16px;
  color: #e5e7eb;
  transition: all 0.2s;
}

.star-icon.filled {
  color: #fbbf24;
  filter: drop-shadow(0 1px 2px rgba(251, 191, 36, 0.4));
}

.rating-number {
  font-size: 1rem;
  font-weight: 700;
  color: var(--color-text-primary);
  font-family: 'Vazir', sans-serif;
}

.reviews-count {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
  font-family: 'Vazir', sans-serif;
}

/* Persian Book Button */
.btn-book-persian {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 0.95rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  font-family: 'Vazir', 'IRANSans', sans-serif;
  box-shadow: 0 4px 12px rgba(var(--color-primary-rgb), 0.3);
}

.btn-book-persian svg {
  width: 18px;
  height: 18px;
}

.btn-book-persian:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(var(--color-primary-rgb), 0.4);
}

.btn-book-persian:active {
  transform: translateY(0) scale(0.98);
}

/* Iranian Design Pattern - Colorful Gradient Cards */
.provider-card:hover {
  border-color: transparent;
  box-shadow:
    0 10px 30px rgba(0, 0, 0, 0.12),
    0 0 0 1px var(--color-primary);
}

/* Add subtle gradient overlay on hover */
.provider-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: linear-gradient(135deg, rgba(var(--color-primary-rgb), 0.05) 0%, transparent 100%);
  opacity: 0;
  transition: opacity 0.3s ease;
  pointer-events: none;
  border-radius: 12px;
}

.provider-card:hover::before {
  opacity: 1;
}

/* Location with Persian styling */
.provider-location {
  font-family: 'Vazir', sans-serif;
  color: var(--color-text-secondary);
  font-size: 0.9rem;
}
```

---

### 5. Map View Integration

#### Create MapViewResults.vue Component

Create file: `src/modules/provider/components/MapViewResults.vue`

```vue
<template>
  <div class="map-view-container">
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
        v-for="provider in visibleProviders"
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
import { ref, onMounted, watch, computed } from 'vue'
import { Map, View, Feature } from '@neshan-maps-platform/ol'
import { fromLonLat } from '@neshan-maps-platform/ol/proj'
import { Point } from '@neshan-maps-platform/ol/geom'
import { Vector as VectorSource } from '@neshan-maps-platform/ol/source'
import { Vector as VectorLayer } from '@neshan-maps-platform/ol/layer'
import { Style, Icon, Text, Fill, Stroke } from '@neshan-maps-platform/ol/style'
import type { ProviderSummary } from '../types/provider.types'

import '@neshan-maps-platform/ol/ol.css'

interface Props {
  providers: ProviderSummary[]
  loading?: boolean
  mapKey: string
  serviceKey: string
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
  mapKey: import.meta.env.VITE_NESHAN_MAP_KEY,
  serviceKey: import.meta.env.VITE_NESHAN_SERVICE_KEY,
})

const emit = defineEmits<{
  (e: 'providerClick', provider: ProviderSummary): void
  (e: 'book', provider: ProviderSummary): void
}>()

const mapContainer = ref<HTMLElement | null>(null)
const map = ref<any>(null)
const markersLayer = ref<any>(null)
const selectedProviderId = ref<string | null>(null)

// Filter providers with valid coordinates
const visibleProviders = computed(() => {
  return props.providers.filter(p =>
    p.address?.latitude && p.address?.longitude ||
    (p as any).latitude && (p as any).longitude
  ).slice(0, 10) // Limit to 10 for performance
})

onMounted(() => {
  initializeMap()
})

watch(() => props.providers, () => {
  updateMarkers()
}, { deep: true })

const initializeMap = () => {
  if (!mapContainer.value) return

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
}

const updateMarkers = () => {
  if (!map.value || !markersLayer.value) return

  const source = markersLayer.value.getSource()
  source.clear()

  visibleProviders.value.forEach((provider, index) => {
    const lat = provider.address?.latitude || (provider as any).latitude
    const lng = provider.address?.longitude || (provider as any).longitude

    if (!lat || !lng) return

    const marker = new Feature({
      geometry: new Point(fromLonLat([lng, lat])),
      provider: provider,
    })

    // Create custom marker style
    marker.setStyle(new Style({
      image: new Icon({
        anchor: [0.5, 1],
        src: `data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="40" height="52" viewBox="0 0 40 52"><path fill="${selectedProviderId.value === provider.id ? '%238B5CF6' : '%23EF4444'}" d="M20 0C8.9 0 0 8.9 0 20c0 11 20 32 20 32s20-21 20-32c0-11.1-8.9-20-20-20zm0 27c-3.9 0-7-3.1-7-7s3.1-7 7-7 7 3.1 7 7-3.1 7-7 7z"/><text x="20" y="22" font-family="Arial" font-size="14" font-weight="bold" fill="white" text-anchor="middle">${index + 1}</text></svg>`,
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

  map.value.getView().setCenter(fromLonLat([centerLng, centerLat]))
  map.value.getView().setZoom(13)
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
  height: calc(100vh - 200px);
  min-height: 600px;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.1);
}

.map-wrapper {
  position: relative;
  width: 100%;
  height: 100%;
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
  color: var(--color-text-primary);
}

.control-btn:hover {
  background: var(--color-primary);
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
  border: 4px solid var(--color-border);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.map-loading p {
  margin-top: 1rem;
  font-family: 'Vazir', sans-serif;
  color: var(--color-text-secondary);
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
  border: 2px solid var(--color-primary);
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
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
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
  color: var(--color-text-primary);
  font-family: 'Vazir', sans-serif;
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
  color: var(--color-text-primary);
}

.card-location {
  font-size: 0.8rem;
  color: var(--color-text-secondary);
  margin: 0;
  font-family: 'Vazir', sans-serif;
}

.card-book-btn {
  padding: 0.5rem 1rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  font-family: 'Vazir', sans-serif;
  align-self: center;
}

.card-book-btn:hover {
  background: var(--color-primary-dark);
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
  .map-view-container {
    height: calc(100vh - 120px);
    min-height: 400px;
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
```

#### Update ProviderSearchView.vue to Support Map View

Add this after line 74:

```vue
<!-- Map View Option -->
<MapViewResults
  v-if="viewMode === 'map'"
  :providers="providers"
  :loading="isSearching"
  :map-key="neshanMapKey"
  :service-key="neshanServiceKey"
  @provider-click="handleProviderClick"
  @book="handleBookClick"
/>
```

Add to imports (line 104):

```typescript
import MapViewResults from '../components/MapViewResults.vue'
```

Add to computed (line 126):

```typescript
const neshanMapKey = import.meta.env.VITE_NESHAN_MAP_KEY
const neshanServiceKey = import.meta.env.VITE_NESHAN_SERVICE_KEY
```

Update ProviderSearchResults to add map view button (line 15-35):

```vue
<div class="view-toggle">
  <button
    :class="['view-btn', { active: viewMode === 'grid' }]"
    @click="emit('viewModeChange', 'grid')"
    title="نمایش شبکه‌ای"
  >
    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
    </svg>
  </button>
  <button
    :class="['view-btn', { active: viewMode === 'list' }]"
    @click="emit('viewModeChange', 'list')"
    title="نمایش لیستی"
  >
    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
    </svg>
  </button>
  <button
    :class="['view-btn', { active: viewMode === 'map' }]"
    @click="emit('viewModeChange', 'map')"
    title="نمایش نقشه"
  >
    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
    </svg>
  </button>
</div>
```

---

## 🎨 Final Touches

### Persian Number Conversion Utility

Create `src/shared/utils/persian.utils.ts`:

```typescript
export const toPersianDigits = (num: number | string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return String(num).replace(/\d/g, (d) => persianDigits[parseInt(d)])
}

export const toEnglishDigits = (str: string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  let result = str
  persianDigits.forEach((persian, index) => {
    result = result.replace(new RegExp(persian, 'g'), String(index))
  })
  return result
}
```

---

## ✅ Testing Checklist

- [ ] Voice search works in Persian (Chrome/Edge)
- [ ] Geolocation permission requested
- [ ] Skeleton loaders appear during search
- [ ] Cards animate in with stagger effect
- [ ] Map view loads with providers
- [ ] Map markers are clickable
- [ ] Persian numbers display correctly
- [ ] Mobile drawer opens/closes smoothly
- [ ] All buttons have touch-friendly sizes
- [ ] Filters apply correctly
- [ ] View modes switch properly

---

## 🚀 Performance Tips

1. **Lazy load map component** - Only load when map view is selected
2. **Limit visible providers** - Show max 50 on map
3. **Debounce filter changes** - Already implemented (500ms)
4. **Virtual scrolling** - Consider for 100+ results
5. **Image lazy loading** - Add `loading="lazy"` to img tags

---

## 📦 Dependencies Already Installed

✅ @neshan-maps-platform/ol
✅ Vue 3 Composition API
✅ Pinia State Management

---

Your Provider Search is now feature-complete with:
- ✨ Voice Search (Persian)
- 📍 Geolocation
- 🗺️ Map View (Neshan)
- 💀 Skeleton Loaders
- 🎭 Stagger Animations
- 🇮🇷 Full Persian UI/UX
- 📱 Mobile-First Design

All integrated with your existing Neshan Maps configuration! 🎉
