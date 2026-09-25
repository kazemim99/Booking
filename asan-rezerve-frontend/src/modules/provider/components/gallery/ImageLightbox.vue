<template>
  <Teleport to="body">
    <Transition name="lightbox-fade">
      <div
        v-if="isOpen"
        class="lightbox-overlay"
        @click="handleOverlayClick"
        @keydown.esc="close"
        @keydown.left="previous"
        @keydown.right="next"
        role="dialog"
        aria-modal="true"
        aria-label="Image gallery lightbox"
        tabindex="0"
      >
        <!-- Close button -->
        <button
          class="lightbox-close"
          @click="close"
          aria-label="Close lightbox"
          type="button"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M6 18L18 6M6 6l12 12"
            />
          </svg>
        </button>

        <!-- Navigation arrows -->
        <button
          v-if="hasPrevious"
          class="lightbox-nav lightbox-nav-left"
          @click.stop="previous"
          aria-label="Previous image"
          type="button"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M15 19l-7-7 7-7"
            />
          </svg>
        </button>

        <button
          v-if="hasNext"
          class="lightbox-nav lightbox-nav-right"
          @click.stop="next"
          aria-label="Next image"
          type="button"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M9 5l7 7-7 7"
            />
          </svg>
        </button>

        <!-- Image container -->
        <div class="lightbox-content" @click.stop>
          <div class="lightbox-image-container">
            <img
              :src="currentImage?.originalUrl || currentImage?.mediumUrl"
              :alt="currentImage?.altText || currentImage?.caption || 'Gallery image'"
              class="lightbox-image"
              @load="handleImageLoad"
            />

            <!-- Loading spinner -->
            <div v-if="imageLoading" class="lightbox-loading">
              <div class="spinner"></div>
            </div>
          </div>

          <!-- Image metadata -->
          <div v-if="currentImage" class="lightbox-metadata">
            <div class="lightbox-caption-area">
              <h3 v-if="currentImage.caption" class="lightbox-caption">
                {{ currentImage.caption }}
              </h3>
              <p class="lightbox-counter">
                {{ currentIndex + 1 }} / {{ images.length }}
              </p>
            </div>

            <!-- Image uploaded date -->
            <p v-if="currentImage.uploadedAt" class="lightbox-date">
              Uploaded {{ formatDate(currentImage.uploadedAt) }}
            </p>
          </div>
        </div>

        <!-- Thumbnail strip -->
        <div v-if="showThumbnails && images.length > 1" class="lightbox-thumbnails">
          <button
            v-for="(image, index) in images"
            :key="image.id"
            class="lightbox-thumbnail"
            :class="{ active: index === currentIndex }"
            @click.stop="goToImage(index)"
            :aria-label="`Go to image ${index + 1}`"
            type="button"
          >
            <img
              :src="image.thumbnailUrl"
              :alt="image.altText || `Thumbnail ${index + 1}`"
            />
          </button>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import type { GalleryImage } from '../../types/gallery.types'

interface Props {
  images: GalleryImage[]
  initialIndex?: number
  showThumbnails?: boolean
}

interface Emits {
  (e: 'close'): void
  (e: 'change', index: number): void
}

const props = withDefaults(defineProps<Props>(), {
  initialIndex: 0,
  showThumbnails: true
})

const emit = defineEmits<Emits>()

// State
const isOpen = ref(false)
const currentIndex = ref(props.initialIndex)
const imageLoading = ref(true)

// Computed
const currentImage = computed(() => props.images[currentIndex.value])
const hasPrevious = computed(() => currentIndex.value > 0)
const hasNext = computed(() => currentIndex.value < props.images.length - 1)

// Methods
function open(index: number = props.initialIndex) {
  currentIndex.value = index
  isOpen.value = true
  imageLoading.value = true
  document.body.style.overflow = 'hidden'

  // Focus the overlay for keyboard navigation
  setTimeout(() => {
    const overlay = document.querySelector('.lightbox-overlay') as HTMLElement
    overlay?.focus()
  }, 100)
}

function close() {
  isOpen.value = false
  document.body.style.overflow = ''
  emit('close')
}

function previous() {
  if (hasPrevious.value) {
    currentIndex.value--
    imageLoading.value = true
    emit('change', currentIndex.value)
  }
}

function next() {
  if (hasNext.value) {
    currentIndex.value++
    imageLoading.value = true
    emit('change', currentIndex.value)
  }
}

function goToImage(index: number) {
  currentIndex.value = index
  imageLoading.value = true
  emit('change', index)
}

function handleOverlayClick(event: MouseEvent) {
  // Close if clicking directly on overlay (not on content)
  if ((event.target as HTMLElement).classList.contains('lightbox-overlay')) {
    close()
  }
}

function handleImageLoad() {
  imageLoading.value = false
}

function formatDate(date: Date): string {
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  }).format(date)
}

// Keyboard navigation
function handleKeydown(event: KeyboardEvent) {
  if (!isOpen.value) return

  switch (event.key) {
    case 'Escape':
      close()
      break
    case 'ArrowLeft':
      previous()
      break
    case 'ArrowRight':
      next()
      break
  }
}

// Watch for initial index changes
watch(() => props.initialIndex, (newIndex) => {
  if (isOpen.value) {
    currentIndex.value = newIndex
  }
})

// Cleanup on unmount
onUnmounted(() => {
  document.body.style.overflow = ''
})

// Expose methods for parent components
defineExpose({
  open,
  close,
  next,
  previous,
  goToImage
})
</script>

<style scoped>
.lightbox-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.95);
  z-index: 9999;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  outline: none;
}

.lightbox-close {
  position: absolute;
  top: 1rem;
  right: 1rem;
  background: rgba(255, 255, 255, 0.1);
  border: none;
  border-radius: 0.5rem;
  width: 3rem;
  height: 3rem;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: white;
  transition: background-color 0.2s;
  z-index: 10;
}

.lightbox-close:hover {
  background: rgba(255, 255, 255, 0.2);
}

.lightbox-close svg {
  width: 1.5rem;
  height: 1.5rem;
}

.lightbox-nav {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  background: rgba(255, 255, 255, 0.1);
  border: none;
  border-radius: 0.5rem;
  width: 3rem;
  height: 3rem;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: white;
  transition: background-color 0.2s;
  z-index: 10;
}

.lightbox-nav:hover {
  background: rgba(255, 255, 255, 0.2);
}

.lightbox-nav svg {
  width: 2rem;
  height: 2rem;
}

.lightbox-nav-left {
  left: 1rem;
}

.lightbox-nav-right {
  right: 1rem;
}

.lightbox-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
  max-width: 90vw;
  max-height: calc(100vh - 200px);
}

.lightbox-image-container {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  max-height: calc(100vh - 250px);
}

.lightbox-image {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  border-radius: 0.5rem;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);
}

.lightbox-loading {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.5);
  border-radius: 0.5rem;
}

.spinner {
  width: 3rem;
  height: 3rem;
  border: 3px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.lightbox-metadata {
  margin-top: 1.5rem;
  width: 100%;
  max-width: 800px;
  color: white;
}

.lightbox-caption-area {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0.5rem;
}

.lightbox-caption {
  font-size: 1.125rem;
  font-weight: 500;
  margin: 0;
}

.lightbox-counter {
  font-size: 0.875rem;
  color: rgba(255, 255, 255, 0.7);
  white-space: nowrap;
  margin: 0;
}

.lightbox-date {
  font-size: 0.875rem;
  color: rgba(255, 255, 255, 0.6);
  margin: 0;
}

.lightbox-thumbnails {
  display: flex;
  gap: 0.5rem;
  padding: 1rem;
  overflow-x: auto;
  max-width: 90vw;
  background: rgba(0, 0, 0, 0.3);
  border-radius: 0.5rem;
  margin-top: 1rem;
}

.lightbox-thumbnail {
  flex-shrink: 0;
  width: 4rem;
  height: 4rem;
  border: 2px solid transparent;
  border-radius: 0.25rem;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.2s;
  background: none;
  padding: 0;
}

.lightbox-thumbnail:hover {
  border-color: rgba(255, 255, 255, 0.5);
}

.lightbox-thumbnail.active {
  border-color: white;
  box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.3);
}

.lightbox-thumbnail img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* Transitions */
.lightbox-fade-enter-active,
.lightbox-fade-leave-active {
  transition: opacity 0.3s ease;
}

.lightbox-fade-enter-from,
.lightbox-fade-leave-to {
  opacity: 0;
}

/* Responsive */
@media (max-width: 768px) {
  .lightbox-overlay {
    padding: 0.5rem;
  }

  .lightbox-close,
  .lightbox-nav {
    width: 2.5rem;
    height: 2.5rem;
  }

  .lightbox-nav-left {
    left: 0.5rem;
  }

  .lightbox-nav-right {
    right: 0.5rem;
  }

  .lightbox-content {
    max-height: calc(100vh - 150px);
  }

  .lightbox-image-container {
    max-height: calc(100vh - 200px);
  }

  .lightbox-thumbnails {
    padding: 0.5rem;
  }

  .lightbox-thumbnail {
    width: 3rem;
    height: 3rem;
  }
}

/* Accessibility */
@media (prefers-reduced-motion: reduce) {
  .lightbox-fade-enter-active,
  .lightbox-fade-leave-active,
  .lightbox-close,
  .lightbox-nav,
  .lightbox-thumbnail {
    transition: none;
  }

  .spinner {
    animation: none;
  }
}
</style>
