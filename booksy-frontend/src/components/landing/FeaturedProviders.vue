<template>
  <section class="featured-section" dir="rtl">
    <div class="container">
      <div class="section-header">
        <div class="header-content">
          <h2 class="section-title">بهترین ارائه‌دهندگان</h2>
          <p class="section-subtitle">
            متخصصان برگزیده محبوب هزاران مشتری
          </p>
        </div>
        <button class="view-all-btn" @click="viewAll">
          مشاهده همه
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="loading-state">
        <div class="loading-spinner"></div>
        <p>در حال بارگذاری ارائه‌دهندگان برتر...</p>
      </div>

      <!-- Providers Grid -->
      <div v-else class="providers-grid">
        <div
          v-for="(provider, index) in featuredProviders"
          :key="provider.id"
          class="provider-card"
          :style="{ animationDelay: `${index * 0.1}s` }"
          @click="viewProvider(provider.id)"
        >
          <div class="provider-image">
            <img
              :src="buildProviderImageUrl(provider.profileImageUrl, provider.logoUrl) || getProviderMockImage(index)"
              :alt="provider.businessName"
              @error="(e) => handleImageError(e, index)"
            />
            <div class="provider-badge" v-if="provider.averageRating >= 4.5">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
              </svg>
              برتر
            </div>

            <!-- Favorite Button -->
            <FavoriteButton
              :provider-id="provider.id"
              @click.stop
            />
          </div>

          <div class="provider-content">
            <div class="provider-header">
              <h3 class="provider-name">{{ provider.businessName }}</h3>
              <div class="provider-rating">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.563.563 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
                </svg>
                <span class="rating-value">{{ convertToPersianNumber(provider.averageRating || 5.0) }}</span>
                <span class="rating-count">({{ convertToPersianNumber(provider.totalReviews || 0) }})</span>
              </div>
            </div>

            <p class="provider-description">{{ truncate(provider.description, 80) }}</p>

            <div class="provider-meta">
              <div class="meta-item">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                </svg>
                {{ provider.city }}, {{ provider.state }}
              </div>
              <div class="meta-item" v-if="provider.priceRange">
                <span class="price-indicator">{{ getPriceIndicator(provider.priceRange) }}</span>
              </div>
            </div>

            <div class="provider-features">
              <span v-if="provider.allowOnlineBooking" class="feature-badge">
                📅 رزرو آنلاین
              </span>
              <span v-if="provider.offersMobileServices" class="feature-badge">
                🚗 خدمات سیار
              </span>
            </div>

            <button class="book-btn" @click.stop="bookProvider(provider.id)">
              رزرو کنید
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { useCustomerStore } from '@/modules/customer/stores/customer.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { buildProviderImageUrl } from '@/core/utils/url.service'
import FavoriteButton from '@/modules/customer/components/favorites/FavoriteButton.vue'
import type { ProviderSummary, PriceRange } from '@/modules/provider/types/provider.types'

const router = useRouter()
const providerStore = useProviderStore()
const customerStore = useCustomerStore()
const authStore = useAuthStore()

const loading = ref(false)
const featuredProviders = ref<ProviderSummary[]>([])

const isAuthenticated = computed(() => authStore.isAuthenticated)

onMounted(async () => {
  loading.value = true
  try {
    // Fetch top-rated providers
    await providerStore.searchProviders({
      pageNumber: 1,
      pageSize: 6,
      sortBy: 'rating',
      sortDescending: true,
    })
    featuredProviders.value = providerStore.providers.slice(0, 6)

    // Fetch customer favorites if authenticated
    if (isAuthenticated.value && authStore.customerId) {
      try {
        await customerStore.fetchFavorites(authStore.customerId)
      } catch (error) {
        console.error('Failed to load favorites:', error)
      }
    }
  } catch (error) {
    console.error('Failed to load featured providers:', error)
  } finally {
    loading.value = false
  }
})

const truncate = (text: string, length: number) => {
  if (!text) return ''
  return text.length > length ? text.substring(0, length) + '...' : text
}

const getPriceIndicator = (priceRange: PriceRange) => {
  const indicators = {
    Budget: '$',
    Moderate: '$$',
    Premium: '$$$',
  }
  return indicators[priceRange] || '$$'
}

const viewProvider = (id: string) => {
  router.push(`/providers/${id}`)
}

const bookProvider = (id: string) => {
  router.push(`/book/${id}`)
}

const viewAll = () => {
  router.push('/providers/search')
}

const getProviderMockImage = (index: number): string => {
  // Beautiful mock images from Unsplash for different beauty/wellness services
  const mockImages = [
    'https://images.unsplash.com/photo-1560066984-138dadb4c035?w=600&q=80', // Hair salon
    'https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=600&q=80', // Spa/Massage
    'https://images.unsplash.com/photo-1487412947147-5cebf100ffc2?w=600&q=80', // Facial/Skincare
    'https://images.unsplash.com/photo-1604902396830-aca29bb5b2e2?w=600&q=80', // Nails
    'https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=600&q=80', // Makeup
    'https://images.unsplash.com/photo-1519415510236-718bdfcd89c8?w=600&q=80', // Barbershop
  ]
  return mockImages[index % mockImages.length]
}

const handleImageError = (event: Event, index: number) => {
  const img = event.target as HTMLImageElement
  // Fallback to a different mock image if the first one fails
  img.src = getProviderMockImage(index)
}

const convertToPersianNumber = (num: number): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().split('').map(digit => persianDigits[parseInt(digit)] || digit).join('')
}
</script>

<style scoped>
.featured-section {
  padding: 5rem 0;
  background: white;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 2rem;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 3rem;
  gap: 2rem;
}

.header-content {
  flex: 1;
}

.section-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.section-subtitle {
  font-size: 1.125rem;
  color: #64748b;
  margin: 0;
}

.view-all-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.875rem 1.5rem;
  background: white;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  color: #475569;
  cursor: pointer;
  transition: all 0.3s;
}

.view-all-btn svg {
  width: 16px;
  height: 16px;
}

.view-all-btn:hover {
  background: #667eea;
  color: white;
  border-color: #667eea;
  transform: translateX(-4px);
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
}

.loading-spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #e2e8f0;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.providers-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 2rem;
}

.provider-card {
  background: white;
  border-radius: 20px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  border: 2px solid #f1f5f9;
  animation: fadeInScale 0.6s ease-out;
  animation-fill-mode: both;
}

@keyframes fadeInScale {
  from {
    opacity: 0;
    transform: scale(0.95);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

.provider-card:hover {
  border-color: #667eea;
  transform: translateY(-8px);
  box-shadow: 0 20px 40px rgba(102, 126, 234, 0.2);
}

.provider-image {
  position: relative;
  height: 220px;
  overflow: hidden;
  background: #f1f5f9;
}

.provider-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.4s;
}

.provider-card:hover .provider-image img {
  transform: scale(1.1);
}

.provider-badge {
  position: absolute;
  top: 1rem;
  left: 1rem;
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.5rem 1rem;
  background: linear-gradient(135deg, #ffd700 0%, #ffed4e 100%);
  color: #1e293b;
  border-radius: 20px;
  font-size: 0.75rem;
  font-weight: 700;
  box-shadow: 0 4px 12px rgba(255, 215, 0, 0.4);
}

.provider-badge svg {
  width: 14px;
  height: 14px;
}

/* Favorite Button Positioning */
.provider-image :deep(.favorite-btn) {
  position: absolute;
  top: 1rem;
  right: 1rem;
  width: 40px;
  height: 40px;
  padding: 0.5rem;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(8px);
  border: none;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  z-index: 10;
}

.provider-image :deep(.favorite-btn:hover) {
  transform: scale(1.1);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.15);
  background: white;
}

.provider-image :deep(.favorite-btn.active) {
  background: rgba(254, 215, 215, 0.95);
  border-color: transparent;
}

.provider-image :deep(.favorite-btn svg) {
  width: 20px;
  height: 20px;
}

.provider-content {
  padding: 1.5rem;
}

.provider-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.provider-name {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
  flex: 1;
  line-height: 1.3;
}

.provider-rating {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  white-space: nowrap;
}

.provider-rating svg {
  width: 16px;
  height: 16px;
  color: #fbbf24;
}

.rating-value {
  font-weight: 700;
  color: #1e293b;
  font-size: 0.95rem;
}

.rating-count {
  color: #94a3b8;
  font-size: 0.875rem;
}

.provider-description {
  color: #64748b;
  font-size: 0.95rem;
  line-height: 1.5;
  margin: 0 0 1rem 0;
}

.provider-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 1rem;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.875rem;
  color: #64748b;
}

.meta-item svg {
  width: 14px;
  height: 14px;
  color: #94a3b8;
}

.price-indicator {
  font-weight: 700;
  color: #10b981;
}

.provider-features {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1.25rem;
}

.feature-badge {
  padding: 0.375rem 0.75rem;
  background: #f1f5f9;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 600;
  color: #475569;
}

.book-btn {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.875rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.book-btn svg {
  width: 16px;
  height: 16px;
}

.book-btn:hover {
  transform: scale(1.02);
  box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
}

/* Responsive */
@media (max-width: 768px) {
  .featured-section {
    padding: 3rem 0;
  }

  .section-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .section-title {
    font-size: 2rem;
  }

  .providers-grid {
    grid-template-columns: 1fr;
  }

  .view-all-btn {
    width: 100%;
    justify-content: center;
  }
}
</style>
