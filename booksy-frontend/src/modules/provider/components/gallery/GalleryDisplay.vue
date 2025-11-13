<template>
  <div class="gallery-display" dir="rtl">
    <!-- Header (optional) -->
    <div v-if="showHeader" class="gallery-display-header">
      <h2 v-if="title" class="gallery-title">{{ title }}</h2>
      <p v-if="subtitle" class="gallery-subtitle">{{ subtitle }}</p>

      <!-- View Mode Switcher -->
      <div v-if="allowViewSwitch" class="view-switcher">
        <button
          @click="currentView = 'grid'"
          :class="{ active: currentView === 'grid' }"
          class="view-btn"
          title="نمایش شبکه‌ای"
        >
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path d="M5 3a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2V5a2 2 0 00-2-2H5zM5 11a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2v-2a2 2 0 00-2-2H5zM11 5a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V5zM11 13a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
          </svg>
        </button>
        <button
          @click="currentView = 'masonry'"
          :class="{ active: currentView === 'masonry' }"
          class="view-btn"
          title="نمایش آبشاری"
        >
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path d="M3 4a1 1 0 011-1h12a1 1 0 011 1v2a1 1 0 01-1 1H4a1 1 0 01-1-1V4zM3 10a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H4a1 1 0 01-1-1v-6zM14 9a1 1 0 00-1 1v6a1 1 0 001 1h2a1 1 0 001-1v-6a1 1 0 00-1-1h-2z" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="gallery-loading">
      <div class="skeleton-grid">
        <div v-for="i in skeletonCount" :key="i" class="skeleton-item">
          <div class="skeleton-image"></div>
        </div>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="gallery-error">
      <svg class="icon-error" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
      </svg>
      <p>{{ error }}</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="displayImages.length === 0" class="gallery-empty">
      <svg class="icon-empty" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clip-rule="evenodd" />
      </svg>
      <p>{{ emptyMessage || 'هنوز تصویری اضافه نشده است' }}</p>
    </div>

    <!-- Gallery Grid View -->
    <div
      v-else-if="currentView === 'grid'"
      class="gallery-grid"
      :class="`columns-${columns}`"
    >
      <div
        v-for="(image, index) in displayImages"
        :key="image.id"
        class="gallery-item"
        @click="openLightbox(index)"
      >
        <div class="image-wrapper">
          <img
            :src="image.thumbnailUrl"
            :alt="image.altText || image.caption || 'تصویر گالری'"
            :loading="lazyLoad ? 'lazy' : 'eager'"
            class="gallery-image"
          />
          <div v-if="showCaptions && image.caption" class="image-caption-overlay">
            {{ image.caption }}
          </div>
        </div>
      </div>
    </div>

    <!-- Gallery Masonry View -->
    <div
      v-else-if="currentView === 'masonry'"
      class="gallery-masonry"
      :class="`columns-${columns}`"
    >
      <div
        v-for="(image, index) in displayImages"
        :key="image.id"
        class="masonry-item"
        @click="openLightbox(index)"
      >
        <img
          :src="image.mediumUrl"
          :alt="image.altText || image.caption || 'تصویر گالری'"
          :loading="lazyLoad ? 'lazy' : 'eager'"
          class="masonry-image"
        />
        <div v-if="showCaptions && image.caption" class="image-caption-overlay">
          {{ image.caption }}
        </div>
      </div>
    </div>

    <!-- Lightbox Modal -->
    <Teleport to="body">
      <div
        v-if="lightboxOpen && currentImage"
        class="lightbox-overlay"
        @click="closeLightbox"
      >
        <div class="lightbox-content" @click.stop>
          <!-- Close Button -->
          <button
            @click="closeLightbox"
            class="lightbox-close"
            type="button"
            title="بستن"
          >
            ×
          </button>

          <!-- Previous Button -->
          <button
            v-if="canGoPrevious"
            @click="previousImage"
            class="lightbox-nav lightbox-prev"
            type="button"
            title="قبلی"
          >
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
          </button>

          <!-- Next Button -->
          <button
            v-if="canGoNext"
            @click="nextImage"
            class="lightbox-nav lightbox-next"
            type="button"
            title="بعدی"
          >
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
            </svg>
          </button>

          <!-- Image -->
          <div class="lightbox-image-wrapper">
            <img
              :src="currentImage.originalUrl"
              :alt="currentImage.altText || currentImage.caption || 'تصویر'"
              class="lightbox-image"
            />
          </div>

          <!-- Caption -->
          <div v-if="currentImage.caption" class="lightbox-caption">
            {{ currentImage.caption }}
          </div>

          <!-- Counter -->
          <div class="lightbox-counter">
            {{ currentImageIndex + 1 }} از {{ displayImages.length }}
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { galleryService } from '@/modules/provider/services/gallery.service'
import type { GalleryImage } from '@/modules/provider/types/gallery.types'

// ============================================================================
// Props
// ============================================================================

interface Props {
  /** Provider ID to load gallery for */
  providerId?: string
  /** Pre-loaded images (if you don't want to fetch) */
  images?: GalleryImage[]
  /** Gallery title */
  title?: string
  /** Gallery subtitle/description */
  subtitle?: string
  /** Show header section */
  showHeader?: boolean
  /** Allow switching between view modes */
  allowViewSwitch?: boolean
  /** Default view mode */
  viewMode?: 'grid' | 'masonry'
  /** Number of columns (2, 3, 4) */
  columns?: 2 | 3 | 4
  /** Show captions on hover */
  showCaptions?: boolean
  /** Lazy load images */
  lazyLoad?: boolean
  /** Maximum images to show (0 = all) */
  maxImages?: number
  /** Empty state message */
  emptyMessage?: string
  /** Skeleton items count for loading */
  skeletonCount?: number
}

const props = withDefaults(defineProps<Props>(), {
  showHeader: true,
  allowViewSwitch: true,
  viewMode: 'grid',
  columns: 3,
  showCaptions: true,
  lazyLoad: true,
  maxImages: 0,
  skeletonCount: 6,
})

const emit = defineEmits<{
  (e: 'images-loaded', images: GalleryImage[]): void
  (e: 'image-click', image: GalleryImage, index: number): void
  (e: 'error', error: Error): void
}>()

// ============================================================================
// State
// ============================================================================

const loading = ref(false)
const error = ref<string | null>(null)
const loadedImages = ref<GalleryImage[]>([])
const currentView = ref<'grid' | 'masonry'>(props.viewMode)

// Lightbox
const lightboxOpen = ref(false)
const currentImageIndex = ref(0)

// ============================================================================
// Computed
// ============================================================================

const displayImages = computed(() => {
  // Use provided images or loaded images
  const source = props.images || loadedImages.value

  // Filter active images only
  const active = source.filter(img => img.isActive)

  // Sort by display order
  const sorted = [...active].sort((a, b) => a.displayOrder - b.displayOrder)

  // Limit if maxImages is set
  if (props.maxImages > 0) {
    return sorted.slice(0, props.maxImages)
  }

  return sorted
})

const currentImage = computed(() => {
  return displayImages.value[currentImageIndex.value] || null
})

const canGoPrevious = computed(() => {
  return currentImageIndex.value > 0
})

const canGoNext = computed(() => {
  return currentImageIndex.value < displayImages.value.length - 1
})

// ============================================================================
// Lifecycle
// ============================================================================

onMounted(() => {
  // Only load if providerId is given and no images provided
  if (props.providerId && !props.images) {
    loadImages()
  }
})

watch(() => props.providerId, (newId) => {
  if (newId && !props.images) {
    loadImages()
  }
})

// ============================================================================
// Methods
// ============================================================================

async function loadImages(): Promise<void> {
  if (!props.providerId) {
    error.value = 'شناسه ارائه‌دهنده مشخص نشده است'
    return
  }

  loading.value = true
  error.value = null

  try {
    loadedImages.value = await galleryService.getGalleryImages(props.providerId)
    console.log(`[GalleryDisplay] Loaded ${loadedImages.value.length} images`)
    emit('images-loaded', loadedImages.value)
  } catch (err) {
    console.error('[GalleryDisplay] Error loading images:', err)
    error.value = 'خطا در بارگذاری تصاویر'
    emit('error', err as Error)
  } finally {
    loading.value = false
  }
}

// ============================================================================
// Lightbox
// ============================================================================

function openLightbox(index: number): void {
  currentImageIndex.value = index
  lightboxOpen.value = true
  emit('image-click', displayImages.value[index], index)

  // Add keyboard listener
  document.addEventListener('keydown', handleKeyPress)
}

function closeLightbox(): void {
  lightboxOpen.value = false
  document.removeEventListener('keydown', handleKeyPress)
}

function previousImage(): void {
  if (canGoPrevious.value) {
    currentImageIndex.value--
  }
}

function nextImage(): void {
  if (canGoNext.value) {
    currentImageIndex.value++
  }
}

function handleKeyPress(event: KeyboardEvent): void {
  if (!lightboxOpen.value) return

  switch (event.key) {
    case 'Escape':
      closeLightbox()
      break
    case 'ArrowLeft':
      // In RTL, left = next
      nextImage()
      break
    case 'ArrowRight':
      // In RTL, right = previous
      previousImage()
      break
  }
}

// ============================================================================
// Expose
// ============================================================================

defineExpose({
  loadImages,
  openLightbox,
  closeLightbox,
})
</script>

<style scoped>
.gallery-display {
  width: 100%;
}

/* Header */
.gallery-display-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.gallery-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: #1a202c;
  margin: 0;
}

.gallery-subtitle {
  color: #718096;
  font-size: 0.95rem;
  margin: 0.5rem 0 0 0;
}

/* View Switcher */
.view-switcher {
  display: flex;
  gap: 0.5rem;
  background: #f7fafc;
  padding: 0.25rem;
  border-radius: 0.5rem;
}

.view-btn {
  width: 2.5rem;
  height: 2.5rem;
  border: none;
  background: transparent;
  border-radius: 0.375rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  color: #718096;
}

.view-btn:hover {
  background: #e2e8f0;
  color: #2d3748;
}

.view-btn.active {
  background: white;
  color: #3182ce;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.view-btn svg {
  width: 1.25rem;
  height: 1.25rem;
}

/* Loading State */
.gallery-loading {
  width: 100%;
}

.skeleton-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1.5rem;
}

.skeleton-item {
  border-radius: 0.5rem;
  overflow: hidden;
  background: #f7fafc;
}

.skeleton-image {
  width: 100%;
  aspect-ratio: 4 / 3;
  background: linear-gradient(
    90deg,
    #f7fafc 0%,
    #e2e8f0 50%,
    #f7fafc 100%
  );
  background-size: 200% 100%;
  animation: skeleton-loading 1.5s infinite;
}

@keyframes skeleton-loading {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

/* Error & Empty States */
.gallery-error,
.gallery-empty {
  text-align: center;
  padding: 4rem 2rem;
}

.icon-error,
.icon-empty {
  width: 5rem;
  height: 5rem;
  margin: 0 auto 1.5rem;
  color: #cbd5e0;
}

.icon-error {
  color: #f56565;
}

.gallery-error p,
.gallery-empty p {
  color: #718096;
  font-size: 1.1rem;
  margin: 0;
}

/* Grid View */
.gallery-grid {
  display: grid;
  gap: 1.5rem;
}

.gallery-grid.columns-2 {
  grid-template-columns: repeat(auto-fill, minmax(min(400px, 100%), 1fr));
}

.gallery-grid.columns-3 {
  grid-template-columns: repeat(auto-fill, minmax(min(280px, 100%), 1fr));
}

.gallery-grid.columns-4 {
  grid-template-columns: repeat(auto-fill, minmax(min(220px, 100%), 1fr));
}

.gallery-item {
  cursor: pointer;
  transition: transform 0.2s;
}

.gallery-item:hover {
  transform: translateY(-4px);
}

.image-wrapper {
  position: relative;
  width: 100%;
  aspect-ratio: 4 / 3;
  border-radius: 0.5rem;
  overflow: hidden;
  background: #f7fafc;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  transition: box-shadow 0.2s;
}

.image-wrapper:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.gallery-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s;
}

.image-wrapper:hover .gallery-image {
  transform: scale(1.05);
}

/* Masonry View */
.gallery-masonry {
  column-gap: 1.5rem;
  row-gap: 1.5rem;
}

.gallery-masonry.columns-2 {
  column-count: 2;
}

.gallery-masonry.columns-3 {
  column-count: 3;
}

.gallery-masonry.columns-4 {
  column-count: 4;
}

@media (max-width: 1024px) {
  .gallery-masonry.columns-4 {
    column-count: 3;
  }
}

@media (max-width: 768px) {
  .gallery-masonry.columns-3,
  .gallery-masonry.columns-4 {
    column-count: 2;
  }
}

@media (max-width: 480px) {
  .gallery-masonry {
    column-count: 1 !important;
  }
}

.masonry-item {
  break-inside: avoid;
  margin-bottom: 1.5rem;
  cursor: pointer;
  position: relative;
  border-radius: 0.5rem;
  overflow: hidden;
  background: #f7fafc;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  transition: all 0.2s;
}

.masonry-item:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transform: translateY(-2px);
}

.masonry-image {
  width: 100%;
  height: auto;
  display: block;
  transition: transform 0.3s;
}

.masonry-item:hover .masonry-image {
  transform: scale(1.03);
}

/* Caption Overlay */
.image-caption-overlay {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background: linear-gradient(to top, rgba(0, 0, 0, 0.7), transparent);
  color: white;
  padding: 2rem 1rem 1rem 1rem;
  font-size: 0.875rem;
  opacity: 0;
  transition: opacity 0.2s;
}

.image-wrapper:hover .image-caption-overlay,
.masonry-item:hover .image-caption-overlay {
  opacity: 1;
}

/* Lightbox */
.lightbox-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.95);
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  animation: fadeIn 0.2s;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

.lightbox-content {
  position: relative;
  width: 100%;
  max-width: 1200px;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  align-items: center;
}

/* Lightbox Controls */
.lightbox-close {
  position: absolute;
  top: -3.5rem;
  left: 0;
  width: 3rem;
  height: 3rem;
  border: none;
  background: rgba(255, 255, 255, 0.1);
  color: white;
  font-size: 2.5rem;
  line-height: 1;
  cursor: pointer;
  border-radius: 0.375rem;
  transition: background 0.2s;
  z-index: 10;
}

.lightbox-close:hover {
  background: rgba(255, 255, 255, 0.2);
}

.lightbox-nav {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  width: 3rem;
  height: 3rem;
  border: none;
  background: rgba(255, 255, 255, 0.1);
  color: white;
  cursor: pointer;
  border-radius: 0.375rem;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.lightbox-nav:hover {
  background: rgba(255, 255, 255, 0.2);
  transform: translateY(-50%) scale(1.1);
}

.lightbox-nav svg {
  width: 1.5rem;
  height: 1.5rem;
}

.lightbox-prev {
  right: -4rem;
}

.lightbox-next {
  left: -4rem;
}

@media (max-width: 768px) {
  .lightbox-prev {
    right: 0.5rem;
  }

  .lightbox-next {
    left: 0.5rem;
  }
}

/* Lightbox Image */
.lightbox-image-wrapper {
  max-width: 100%;
  max-height: 75vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.lightbox-image {
  max-width: 100%;
  max-height: 75vh;
  object-fit: contain;
  border-radius: 0.5rem;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);
}

/* Lightbox Caption */
.lightbox-caption {
  background: rgba(0, 0, 0, 0.7);
  color: white;
  padding: 1rem 1.5rem;
  margin-top: 1rem;
  border-radius: 0.375rem;
  text-align: center;
  max-width: 600px;
}

/* Lightbox Counter */
.lightbox-counter {
  position: absolute;
  top: -3.5rem;
  right: 0;
  background: rgba(0, 0, 0, 0.5);
  color: white;
  padding: 0.5rem 1rem;
  border-radius: 0.375rem;
  font-size: 0.875rem;
}

/* Responsive */
@media (max-width: 768px) {
  .gallery-grid.columns-3,
  .gallery-grid.columns-4 {
    grid-template-columns: repeat(auto-fill, minmax(min(200px, 100%), 1fr));
  }

  .gallery-display-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .lightbox-overlay {
    padding: 1rem;
  }

  .lightbox-close,
  .lightbox-counter {
    top: 0.5rem;
  }
}

@media (max-width: 480px) {
  .gallery-grid {
    grid-template-columns: 1fr !important;
  }

  .gallery-title {
    font-size: 1.5rem;
  }
}
</style>
