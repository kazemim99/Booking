<template>
  <div class="provider-detail" dir="rtl">
    <div v-if="provider" class="provider-container">
      <div class="provider-header">
        <div class="provider-cover">
          <img :src="provider.coverImage || '/placeholder.jpg'" alt="Cover" />
        </div>
        <div class="provider-main-info">
          <div class="provider-logo">
            <img :src="provider.logo || '/placeholder.jpg'" :alt="provider.name" />
          </div>
          <div class="provider-details">
            <h1>{{ provider.name }}</h1>
            <p class="provider-type">{{ provider.type }}</p>
            <div class="provider-rating">
              ⭐ {{ provider.rating }} ({{ provider.reviewCount }} نظر)
            </div>
            <p class="provider-address">📍 {{ provider.address }}</p>
          </div>
          <div class="provider-actions">
            <FavoriteButton
              v-if="provider.id"
              :provider-id="String(provider.id)"
              :show-label="true"
              size="lg"
              @favorited="handleFavorited"
              @unfavorited="handleUnfavorited"
            />
            <button @click="bookNow" class="btn-book-now" data-testid="book-service-button">رزرو نوبت</button>
          </div>
        </div>
      </div>

      <div class="provider-content">
        <div class="content-tabs">
          <button @click="activeTab = 'services'" :class="{ active: activeTab === 'services' }">
            خدمات
          </button>
          <button @click="activeTab = 'about'" :class="{ active: activeTab === 'about' }">
            درباره
          </button>
          <button @click="activeTab = 'gallery'" :class="{ active: activeTab === 'gallery' }">
            گالری
          </button>
          <button @click="activeTab = 'reviews'" :class="{ active: activeTab === 'reviews' }">
            نظرات
          </button>
        </div>

        <div v-if="activeTab === 'services'" class="tab-content">
          <h3>خدمات ارائه شده</h3>
          <div class="services-list">
            <div v-for="service in provider.services" :key="service.id" class="service-item">
              <div class="service-info">
                <h4>{{ service.name }}</h4>
                <p>{{ service.description }}</p>
                <span class="duration">⏱️ {{ service.duration }} دقیقه</span>
              </div>
              <div class="service-price-book">
                <span class="price">{{ formatPrice(service.price) }} تومان</span>
                <button @click="bookService(service)" class="btn-book">رزرو</button>
              </div>
            </div>
          </div>
        </div>

        <div v-if="activeTab === 'about'" class="tab-content">
          <h3>درباره {{ provider.name }}</h3>
          <p>{{ provider.description }}</p>
        </div>

        <div v-if="activeTab === 'gallery'" class="tab-content">
          <h3>گالری تصاویر</h3>
          <div class="gallery-grid">
            <div v-for="(img, i) in provider.gallery" :key="i" class="gallery-item">
              <img :src="img" alt="Gallery" />
            </div>
          </div>
        </div>

        <div v-if="activeTab === 'reviews'" class="tab-content">
          <h3>نظرات مشتریان</h3>
          <div v-for="review in provider.reviews" :key="review.id" class="review-item">
            <div class="review-header">
              <strong>{{ review.customerName }}</strong>
              <span class="review-rating">⭐ {{ review.rating }}</span>
            </div>
            <p>{{ review.comment }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import FavoriteButton from '../components/favorites/FavoriteButton.vue'
import { providerService } from '@/modules/provider/services/provider.service'
import { serviceService } from '@/modules/provider/services/service.service'

const route = useRoute()
const router = useRouter()

const activeTab = ref('services')
const loading = ref(true)
const error = ref<string | null>(null)

interface DetailService {
  id: string
  name: string
  description: string
  duration: number
  price: number
}
interface ProviderDetail {
  id: string
  name: string
  type: string
  rating: number
  reviewCount: number
  address: string
  description: string
  logo: string | null
  coverImage: string | null
  services: DetailService[]
  gallery: string[]
  reviews: Array<{ id: string; customerName: string; rating: number; comment: string }>
}

const provider = ref<ProviderDetail>({
  id: String(route.params.id),
  name: '',
  type: '',
  rating: 0,
  reviewCount: 0,
  address: '',
  description: '',
  logo: null,
  coverImage: null,
  services: [],
  gallery: [],
  reviews: [],
})

async function loadProvider() {
  loading.value = true
  error.value = null
  try {
    const id = String(route.params.id)
    const [p, services] = await Promise.all([
      providerService.getProviderById(id, true, false, true),
      serviceService.getServicesByProvider(id).catch(() => []),
    ])
    provider.value = {
      id: p.id,
      name: p.profile?.businessName ?? '',
      type: '',
      rating: 0,
      reviewCount: 0,
      address: p.address?.formattedAddress || p.address?.addressLine1 || '',
      description: p.profile?.description ?? '',
      logo: p.profile?.logoUrl ?? null,
      coverImage: p.profile?.coverImageUrl ?? null,
      services: (services ?? []).map((s) => ({
        id: s.id,
        name: s.name,
        description: s.description ?? '',
        duration: s.duration,
        price: s.basePrice,
      })),
      gallery: [],
      reviews: [],
    }
  } catch (e) {
    console.error('[ProviderDetailView] failed to load provider', e)
    error.value = 'بارگذاری اطلاعات ارائه‌دهنده با خطا مواجه شد'
  } finally {
    loading.value = false
  }
}

onMounted(loadProvider)

function formatPrice(price: number) {
  return new Intl.NumberFormat('fa-IR').format(price)
}

function handleFavorited(providerId: string): void {
  console.log('[ProviderDetailView] Provider favorited:', providerId)
}

function handleUnfavorited(providerId: string): void {
  console.log('[ProviderDetailView] Provider unfavorited:', providerId)
}

function bookNow() {
  router.push({ name: 'NewBooking', query: { providerId: provider.value.id } })
}

function bookService(service: any) {
  router.push({ name: 'NewBooking', query: { providerId: provider.value.id, serviceId: service.id } })
}
</script>

<style scoped>
.provider-cover {
  height: 300px;
  background: var(--color-gray-200);
}

.provider-cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.provider-main-info {
  position: relative;
  background: white;
  padding: 1.5rem;
  border-radius: 0.75rem;
  margin-top: -100px;
  margin-bottom: 2rem;
  display: grid;
  grid-template-columns: auto 1fr auto;
  gap: 1.5rem;
}

.provider-logo {
  width: 150px;
  height: 150px;
  border-radius: 0.75rem;
  overflow: hidden;
  border: 4px solid white;
}

.provider-logo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.provider-actions {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  align-items: stretch;
}

.btn-book-now {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  background: var(--color-primary);
  color: white;
  font-weight: 600;
  transition: background 0.2s;
}

.btn-book-now:hover {
  background: var(--color-primary-dark, #2c5aa0);
}

.content-tabs {
  display: flex;
  gap: 1rem;
  border-bottom: 2px solid var(--color-gray-200);
  margin-bottom: 2rem;
}

.content-tabs button {
  padding: 1rem 1.5rem;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  cursor: pointer;
}

.content-tabs button.active {
  border-bottom-color: var(--color-primary);
  color: var(--color-primary);
}

.services-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.service-item {
  background: white;
  padding: 1.5rem;
  border-radius: 0.75rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.service-price-book {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.price {
  font-weight: 600;
  color: var(--color-primary);
}

.gallery-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1rem;
}

.gallery-item img {
  width: 100%;
  border-radius: 0.5rem;
}

.review-item {
  background: white;
  padding: 1rem;
  border-radius: 0.5rem;
  margin-bottom: 1rem;
}
</style>
