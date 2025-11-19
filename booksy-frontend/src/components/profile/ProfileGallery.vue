<template>
  <section class="profile-gallery" dir="rtl">
    <div class="gallery-header">
      <h2 class="section-title">گالری تصاویر</h2>
      <p class="section-subtitle">
        نمونه‌کارها و تصاویر از فضای کسب‌وکار
      </p>
    </div>

    <!-- Gallery Grid -->
    <div v-if="galleryImages.length > 0" class="gallery-grid">
      <div
        v-for="(image, index) in galleryImages"
        :key="image.id || index"
        class="gallery-item"
        :class="`span-${getSpanClass(index)}`"
        @click="openLightbox(index)"
      >
        <img :src="image.url" :alt="image.caption || 'تصویر گالری'" />
        <div class="image-overlay">
          <div class="overlay-content">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0zM10 7v3m0 0v3m0-3h3m-3 0H7" />
            </svg>
            <p v-if="image.caption">{{ image.caption }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state">
      <div class="empty-icon">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
      </div>
      <h3>هنوز تصویری ثبت نشده است</h3>
      <p>این ارائه‌دهنده هنوز تصاویری در گالری خود ثبت نکرده است.</p>
    </div>

    <!-- Lightbox -->
    <Teleport to="body">
      <div v-if="lightboxOpen" class="lightbox" @click="closeLightbox">
        <div class="lightbox-content" @click.stop>
          <button class="lightbox-close" @click="closeLightbox">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>

          <button class="lightbox-nav lightbox-prev" @click="prevImage">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </button>

          <div class="lightbox-image-container">
            <img
              :src="galleryImages[currentImageIndex]?.url"
              :alt="galleryImages[currentImageIndex]?.caption || 'تصویر گالری'"
            />
            <div v-if="galleryImages[currentImageIndex]?.caption" class="lightbox-caption">
              {{ galleryImages[currentImageIndex].caption }}
            </div>
          </div>

          <button class="lightbox-nav lightbox-next" @click="nextImage">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>

          <div class="lightbox-counter">
            {{ convertToPersianNumber(currentImageIndex + 1) }} / {{ convertToPersianNumber(galleryImages.length) }}
          </div>
        </div>
      </div>
    </Teleport>
  </section>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import type { Provider } from '@/modules/provider/types/provider.types'

interface GalleryImage {
  id?: string
  url: string
  caption?: string
  altText?: string
}

interface Props {
  provider: Provider
}

defineProps<Props>()

// State
const lightboxOpen = ref(false)
const currentImageIndex = ref(0)

// Mock gallery images (replace with real API data)
const mockGalleryImages: GalleryImage[] = [
  { id: '1', url: 'https://images.unsplash.com/photo-1560066984-138dadb4c035?w=800&q=80', caption: 'سالن آرایش حرفه‌ای' },
  { id: '2', url: 'https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&q=80', caption: 'فضای اسپا و ماساژ' },
  { id: '3', url: 'https://images.unsplash.com/photo-1487412947147-5cebf100ffc2?w=800&q=80', caption: 'خدمات مراقبت پوست' },
  { id: '4', url: 'https://images.unsplash.com/photo-1604902396830-aca29bb5b2e2?w=800&q=80', caption: 'مانیکور و پدیکور' },
  { id: '5', url: 'https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&q=80', caption: 'آرایش حرفه‌ای' },
  { id: '6', url: 'https://images.unsplash.com/photo-1519415510236-718bdfcd89c8?w=800&q=80', caption: 'آرایشگاه مردانه' },
  { id: '7', url: 'https://images.unsplash.com/photo-1562322140-8baeececf3df?w=800&q=80', caption: 'فضای داخلی سالن' },
  { id: '8', url: 'https://images.unsplash.com/photo-1633681926022-84c23e8cb2d6?w=800&q=80', caption: 'نمونه کار رنگ مو' },
]

// Computed
const galleryImages = computed((): GalleryImage[] => {
  // TODO: Get from provider.gallery when API is ready
  // For now, return mock images
  return mockGalleryImages
})

// Methods
const getSpanClass = (index: number): number => {
  // Create a varied masonry layout
  const pattern = [1, 2, 1, 2, 2, 1, 2, 1]
  return pattern[index % pattern.length]
}

const convertToPersianNumber = (num: number): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().split('').map(digit => {
    const parsedDigit = parseInt(digit)
    return !isNaN(parsedDigit) ? persianDigits[parsedDigit] : digit
  }).join('')
}

const openLightbox = (index: number) => {
  currentImageIndex.value = index
  lightboxOpen.value = true
  document.body.style.overflow = 'hidden'
}

const closeLightbox = () => {
  lightboxOpen.value = false
  document.body.style.overflow = ''
}

const nextImage = () => {
  currentImageIndex.value = (currentImageIndex.value + 1) % galleryImages.value.length
}

const prevImage = () => {
  currentImageIndex.value = (currentImageIndex.value - 1 + galleryImages.value.length) % galleryImages.value.length
}

// Keyboard navigation
const handleKeyPress = (event: KeyboardEvent) => {
  if (!lightboxOpen.value) return

  if (event.key === 'Escape') {
    closeLightbox()
  } else if (event.key === 'ArrowLeft') {
    prevImage()
  } else if (event.key === 'ArrowRight') {
    nextImage()
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleKeyPress)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeyPress)
  document.body.style.overflow = ''
})
</script>

<style scoped>
.profile-gallery {
  padding: 2rem 0;
}

.gallery-header {
  text-align: center;
  margin-bottom: 3rem;
}

.section-title {
  font-size: 2rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.section-subtitle {
  font-size: 1.05rem;
  color: #64748b;
  margin: 0;
}

.gallery-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  grid-auto-rows: 250px;
  gap: 1rem;
}

.gallery-item {
  position: relative;
  overflow: hidden;
  border-radius: 16px;
  cursor: pointer;
  transition: transform 0.3s;
}

.gallery-item.span-1 {
  grid-row: span 1;
}

.gallery-item.span-2 {
  grid-row: span 2;
}

.gallery-item:hover {
  transform: scale(1.02);
  z-index: 1;
}

.gallery-item img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.4s;
}

.gallery-item:hover img {
  transform: scale(1.1);
}

.image-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(to bottom, rgba(0, 0, 0, 0), rgba(0, 0, 0, 0.7));
  opacity: 0;
  transition: opacity 0.3s;
  display: flex;
  align-items: flex-end;
  padding: 1.5rem;
}

.gallery-item:hover .image-overlay {
  opacity: 1;
}

.overlay-content {
  color: white;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.overlay-content svg {
  width: 32px;
  height: 32px;
}

.overlay-content p {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 600;
}

.empty-state {
  text-align: center;
  padding: 5rem 2rem;
  background: white;
  border-radius: 24px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
}

.empty-icon {
  margin: 0 auto 1.5rem;
  width: 80px;
  height: 80px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  border-radius: 50%;
}

.empty-icon svg {
  width: 40px;
  height: 40px;
  color: #667eea;
}

.empty-state h3 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.empty-state p {
  font-size: 1rem;
  color: #64748b;
  margin: 0;
}

/* Lightbox */
.lightbox {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.95);
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  animation: fadeIn 0.3s;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

.lightbox-content {
  position: relative;
  width: 100%;
  max-width: 1200px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.lightbox-close {
  position: absolute;
  top: -3rem;
  left: 0;
  width: 48px;
  height: 48px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  border: 2px solid rgba(255, 255, 255, 0.2);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s;
}

.lightbox-close svg {
  width: 24px;
  height: 24px;
  color: white;
}

.lightbox-close:hover {
  background: rgba(255, 255, 255, 0.2);
  transform: rotate(90deg);
}

.lightbox-nav {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  width: 56px;
  height: 56px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  border: 2px solid rgba(255, 255, 255, 0.2);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s;
  z-index: 10;
}

.lightbox-nav svg {
  width: 28px;
  height: 28px;
  color: white;
}

.lightbox-nav:hover {
  background: rgba(255, 255, 255, 0.2);
  transform: translateY(-50%) scale(1.1);
}

.lightbox-prev {
  right: -80px;
}

.lightbox-next {
  left: -80px;
}

.lightbox-image-container {
  max-width: 100%;
  max-height: 80vh;
  position: relative;
}

.lightbox-image-container img {
  max-width: 100%;
  max-height: 80vh;
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5);
}

.lightbox-caption {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 1.5rem;
  background: linear-gradient(to top, rgba(0, 0, 0, 0.8), transparent);
  color: white;
  font-size: 1.125rem;
  font-weight: 600;
  border-radius: 0 0 12px 12px;
}

.lightbox-counter {
  position: absolute;
  bottom: -3rem;
  left: 50%;
  transform: translateX(-50%);
  padding: 0.75rem 1.5rem;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  border: 2px solid rgba(255, 255, 255, 0.2);
  border-radius: 24px;
  color: white;
  font-size: 1rem;
  font-weight: 600;
}

/* Responsive */
@media (max-width: 1024px) {
  .lightbox-prev {
    right: 1rem;
  }

  .lightbox-next {
    left: 1rem;
  }

  .lightbox-close {
    top: 1rem;
    right: 1rem;
    left: auto;
  }

  .lightbox-counter {
    bottom: 1rem;
  }
}

@media (max-width: 768px) {
  .gallery-grid {
    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
    grid-auto-rows: 150px;
    gap: 0.75rem;
  }

  .section-title {
    font-size: 1.75rem;
  }

  .lightbox {
    padding: 1rem;
  }

  .lightbox-nav {
    width: 44px;
    height: 44px;
  }

  .lightbox-nav svg {
    width: 22px;
    height: 22px;
  }

  .lightbox-prev {
    right: 0.5rem;
  }

  .lightbox-next {
    left: 0.5rem;
  }

  .lightbox-close {
    top: 0.5rem;
    right: 0.5rem;
  }

  .lightbox-counter {
    bottom: 0.5rem;
    font-size: 0.9rem;
  }
}
</style>
