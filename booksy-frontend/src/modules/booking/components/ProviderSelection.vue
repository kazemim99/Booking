<template>
  <div class="provider-selection" dir="rtl">
    <div class="step-header">
      <h2 class="step-title">انتخاب کارشناس</h2>
      <p class="step-description">
        کارشناس مورد نظر خود را برای رزرو انتخاب کنید
      </p>
    </div>

    <!-- Loading State -->
    <div v-if="loadingProviders" class="loading-container">
      <div class="loading-spinner"></div>
      <p>در حال بارگذاری کارشناسان...</p>
    </div>

    <!-- Providers Grid -->
    <div v-else-if="providers.length > 0" class="providers-grid">
      <div
        v-for="provider in providers"
        :key="provider.id"
        class="provider-card"
        @click="selectProvider(provider)"
      >
        <!-- Provider Photo -->
        <div class="provider-photo">
          <img
            v-if="provider.photoUrl"
            :src="provider.photoUrl"
            :alt="provider.name"
          />
          <div v-else class="provider-photo-placeholder">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
          </div>
        </div>

        <!-- Provider Info -->
        <div class="provider-info">
          <h3 class="provider-name">{{ provider.name }}</h3>

          <!-- Rating -->
          <div v-if="provider.rating" class="provider-rating">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
            </svg>
            <span>{{ formatRating(provider.rating) }}</span>
            <span class="rating-count" v-if="provider.reviewCount">({{ provider.reviewCount }})</span>
          </div>

          <!-- Specialization -->
          <p v-if="provider.specialization" class="provider-specialization">
            {{ provider.specialization }}
          </p>

          <!-- Next Available -->
          <div v-if="provider.nextAvailable" class="next-available">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span>نزدیک‌ترین زمان: {{ formatNextAvailable(provider.nextAvailable) }}</span>
          </div>

          <!-- Price -->
          <div v-if="provider.basePrice" class="provider-price">
            <span class="price-label">قیمت:</span>
            <span class="price-value">{{ formatPrice(provider.basePrice) }}</span>
          </div>
        </div>

        <!-- Action Button -->
        <button class="select-button">
          <span>انتخاب زمان</span>
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>
      </div>
    </div>

    <!-- No Providers -->
    <div v-else class="no-providers">
      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
      </svg>
      <h3>کارشناسی موجود نیست</h3>
      <p>متأسفانه در حال حاضر کارشناسی برای این سرویس موجود نیست</p>
    </div>

    <!-- Time Slot Modal -->
    <TimeSlotModal
      v-if="selectedProvider"
      :provider="selectedProvider"
      :service-id="serviceId"
      :provider-id="providerId"
      @close="selectedProvider = null"
      @slot-selected="handleSlotSelected"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import TimeSlotModal from './TimeSlotModal.vue'
import { availabilityService } from '@/modules/booking/api/availability.service'

interface Provider {
  id: string
  name: string
  photoUrl?: string
  rating?: number
  reviewCount?: number
  specialization?: string
  basePrice?: {
    amount: number
    currency: string
  }
  nextAvailable?: string
}

interface Props {
  providerId: string
  serviceId: string | null
}

interface SlotSelectedEvent {
  date: string
  startTime: string
  endTime: string
  staffId: string
  staffName: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'slot-selected', slot: SlotSelectedEvent): void
}>()

// State
const loadingProviders = ref(false)
const providers = ref<Provider[]>([])
const selectedProvider = ref<Provider | null>(null)

// Load providers on mount
onMounted(async () => {
  await loadProviders()
})

const loadProviders = async () => {
  if (!props.serviceId) {
    console.warn('[ProviderSelection] No service selected')
    return
  }

  loadingProviders.value = true

  try {
    // Use the new endpoint to get qualified staff for this service
    const response = await availabilityService.getQualifiedStaff(
      props.providerId,
      props.serviceId
    )

    // Map qualified staff to provider format
    providers.value = response.qualifiedStaff.map(staff => ({
      id: staff.id,
      name: staff.name,
      photoUrl: staff.photoUrl,
      rating: staff.rating,
      reviewCount: staff.reviewCount,
      specialization: staff.specialization,
    }))

    console.log('[ProviderSelection] Providers loaded:', providers.value)
  } catch (error) {
    console.error('[ProviderSelection] Failed to load providers:', error)
    providers.value = []
  } finally {
    loadingProviders.value = false
  }
}

const selectProvider = (provider: Provider) => {
  selectedProvider.value = provider
}

const handleSlotSelected = (slot: SlotSelectedEvent) => {
  emit('slot-selected', slot)
  selectedProvider.value = null
}

const formatRating = (rating: number): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  const ratingStr = rating.toFixed(1)
  return ratingStr.split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

const formatPrice = (price: { amount: number; currency: string }): string => {
  const formatter = new Intl.NumberFormat('fa-IR')
  return `${formatter.format(price.amount)} ${price.currency === 'USD' ? 'دلار' : 'تومان'}`
}

const formatNextAvailable = (dateTime: string): string => {
  const date = new Date(dateTime)
  const today = new Date()
  const tomorrow = new Date(today)
  tomorrow.setDate(tomorrow.getDate() + 1)

  const isToday = date.toDateString() === today.toDateString()
  const isTomorrow = date.toDateString() === tomorrow.toDateString()

  const time = date.toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit', hour12: false })

  if (isToday) {
    return `امروز ${time}`
  } else if (isTomorrow) {
    return `فردا ${time}`
  } else {
    const dateStr = date.toLocaleDateString('fa-IR')
    return `${dateStr} ${time}`
  }
}
</script>

<style scoped>
.provider-selection {
  padding: 0;
}

.step-header {
  text-align: center;
  margin-bottom: 3rem;
}

.step-title {
  font-size: 2rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.step-description {
  font-size: 1.05rem;
  color: #64748b;
  margin: 0;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
}

.loading-spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #e2e8f0;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.loading-container p {
  font-size: 1rem;
  color: #64748b;
}

.providers-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
}

.provider-card {
  background: white;
  border-radius: 20px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  cursor: pointer;
  transition: all 0.3s ease;
  border: 2px solid transparent;
}

.provider-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  border-color: #667eea;
}

.provider-photo {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  overflow: hidden;
  margin: 0 auto 1rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.provider-photo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.provider-photo-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
}

.provider-photo-placeholder svg {
  width: 40px;
  height: 40px;
}

.provider-info {
  text-align: center;
}

.provider-name {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.provider-rating {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.25rem;
  color: #f59e0b;
  font-size: 0.95rem;
  margin-bottom: 0.5rem;
}

.provider-rating svg {
  width: 16px;
  height: 16px;
}

.rating-count {
  color: #94a3b8;
  font-size: 0.85rem;
}

.provider-specialization {
  font-size: 0.9rem;
  color: #64748b;
  margin: 0 0 1rem 0;
  min-height: 40px;
}

.next-available {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  font-size: 0.85rem;
  color: #10b981;
  background: #ecfdf5;
  padding: 0.5rem 0.75rem;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.next-available svg {
  width: 14px;
  height: 14px;
}

.provider-price {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.price-label {
  font-size: 0.9rem;
  color: #64748b;
}

.price-value {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1e293b;
}

.select-button {
  width: 100%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  padding: 0.875rem 1.5rem;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.select-button:hover {
  transform: scale(1.02);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.select-button svg {
  width: 18px;
  height: 18px;
}

.no-providers {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 4rem 2rem;
}

.no-providers svg {
  width: 80px;
  height: 80px;
  color: #cbd5e1;
  margin-bottom: 1.5rem;
}

.no-providers h3 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.no-providers p {
  font-size: 1rem;
  color: #64748b;
  margin: 0;
}

@media (max-width: 768px) {
  .providers-grid {
    grid-template-columns: 1fr;
  }

  .step-title {
    font-size: 1.75rem;
  }
}
</style>
