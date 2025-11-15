<template>
  <div class="provider-card" dir="rtl" @click="handleClick">
    <!-- Cover Image or Logo -->
    <div class="provider-image">
      <img
        :src="provider.logoUrl || provider.coverImageUrl || '/placeholder-provider.jpg'"
        :alt="provider.businessName"
        @error="onImageError"
      />
      <div v-if="provider.distanceKm !== undefined" class="distance-badge">
        {{ formatDistance(provider.distanceKm) }}
      </div>
      <div class="status-badge" :class="statusClass">
        {{ provider.currentStatus }}
      </div>
    </div>

    <!-- Provider Info -->
    <div class="provider-info">
      <!-- Header -->
      <div class="provider-header">
        <h3 class="provider-name">{{ provider.businessName }}</h3>
        <div class="favorite-btn" @click.stop="handleFavoriteClick">
          <FavoriteButton :provider-id="provider.id" />
        </div>
      </div>

      <!-- Type -->
      <p class="provider-type">{{ formatProviderType(provider.type) }}</p>

      <!-- Rating -->
      <div class="provider-rating">
        <span class="rating-value">
          <span class="star-icon">⭐</span>
          {{ provider.averageRating.toFixed(1) }}
        </span>
        <span class="review-count">({{ provider.totalReviews }} نظر)</span>
      </div>

      <!-- Address -->
      <div class="provider-address">
        <span class="location-icon">📍</span>
        <span class="address-text">{{ formatAddress(provider.address) }}</span>
      </div>

      <!-- Tags -->
      <div v-if="provider.tags && provider.tags.length > 0" class="provider-tags">
        <span v-for="tag in provider.tags.slice(0, 3)" :key="tag" class="tag">
          {{ tag }}
        </span>
      </div>

      <!-- Price Range (if available) -->
      <div v-if="provider.priceRange" class="price-range">
        <span class="price-label">قیمت:</span>
        <span class="price-value">
          {{ formatPriceRange(provider.priceRange) }}
        </span>
      </div>

      <!-- Next Available Slot -->
      <div v-if="provider.nextAvailableSlot" class="next-slot">
        <span class="slot-icon">🕐</span>
        <span class="slot-text">{{ formatNextSlot(provider.nextAvailableSlot) }}</span>
      </div>

      <!-- Features -->
      <div class="provider-features">
        <span v-if="provider.allowOnlineBooking" class="feature">
          <span class="feature-icon">✓</span>
          رزرو آنلاین
        </span>
        <span v-if="provider.offersMobileServices" class="feature">
          <span class="feature-icon">✓</span>
          خدمات سیار
        </span>
      </div>

      <!-- Action Button -->
      <button class="book-now-btn" @click.stop="handleBookNow">
        <span>رزرو نوبت</span>
        <span class="arrow">←</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import type { ProviderSearchResult } from '../../types/search.types'
import FavoriteButton from '../favorites/FavoriteButton.vue'
import { ProviderType } from '@/modules/provider/types/provider.types'

// ============================================
// Props
// ============================================

interface Props {
  provider: ProviderSearchResult
}

const props = defineProps<Props>()

// ============================================
// Emits
// ============================================

const emit = defineEmits<{
  click: [providerId: string]
  bookNow: [providerId: string]
}>()

// ============================================
// Router
// ============================================

const router = useRouter()

// ============================================
// Computed
// ============================================

const statusClass = computed(() => {
  return props.provider.isOpen ? 'status-open' : 'status-closed'
})

// ============================================
// Methods
// ============================================

function handleClick() {
  router.push(`/customer/provider/${props.provider.id}`)
  emit('click', props.provider.id)
}

function handleBookNow() {
  router.push(`/customer/provider/${props.provider.id}`)
  emit('bookNow', props.provider.id)
}

function handleFavoriteClick() {
  // The FavoriteButton component handles the favorite logic
}

function formatDistance(km: number): string {
  if (km < 1) {
    return `${Math.round(km * 1000)} متر`
  }
  return `${km.toFixed(1)} کیلومتر`
}

function formatProviderType(type: ProviderType): string {
  const typeLabels: Record<ProviderType, string> = {
    [ProviderType.Individual]: 'فردی',
    [ProviderType.Salon]: 'آرایشگاه',
    [ProviderType.Clinic]: 'کلینیک',
    [ProviderType.Spa]: 'اسپا',
    [ProviderType.Studio]: 'استودیو',
    [ProviderType.Professional]: 'حرفه‌ای',
  }
  return typeLabels[type] || type
}

function formatAddress(address: any): string {
  return address.formattedAddress || `${address.city}, ${address.province}`
}

function formatPriceRange(priceRange: { min: number; max: number; currency: string }): string {
  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('fa-IR').format(price)
  }

  return `${formatPrice(priceRange.min)} - ${formatPrice(priceRange.max)} تومان`
}

function formatNextSlot(slot: { startTime: string; endTime: string }): string {
  const startDate = new Date(slot.startTime)
  const now = new Date()
  const isToday = startDate.toDateString() === now.toDateString()

  const timeStr = startDate.toLocaleTimeString('fa-IR', {
    hour: '2-digit',
    minute: '2-digit',
  })

  return isToday ? `امروز ${timeStr}` : timeStr
}

function onImageError(event: Event) {
  const target = event.target as HTMLImageElement
  target.src = '/placeholder-provider.jpg'
}
</script>

<style scoped>
.provider-card {
  background: white;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  transition: all 0.3s ease;
  cursor: pointer;
  display: flex;
  flex-direction: column;
}

.provider-card:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  transform: translateY(-2px);
}

/* Image */
.provider-image {
  position: relative;
  width: 100%;
  height: 200px;
  overflow: hidden;
  background: #1e96fc;
}

.provider-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s ease;
}

.provider-card:hover .provider-image img {
  transform: scale(1.05);
}

.distance-badge {
  position: absolute;
  top: 12px;
  right: 12px;
  background: rgba(0, 0, 0, 0.7);
  color: white;
  padding: 4px 12px;
  border-radius: 16px;
  font-size: 0.875rem;
  font-weight: 500;
}

.status-badge {
  position: absolute;
  bottom: 12px;
  right: 12px;
  padding: 6px 16px;
  border-radius: 20px;
  font-size: 0.875rem;
  font-weight: 600;
  backdrop-filter: blur(8px);
}

.status-open {
  background: rgba(16, 185, 129, 0.9);
  color: white;
}

.status-closed {
  background: rgba(239, 68, 68, 0.9);
  color: white;
}

/* Provider Info */
.provider-info {
  padding: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
  flex: 1;
}

.provider-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.5rem;
}

.provider-name {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0;
  line-height: 1.3;
  flex: 1;
}

.favorite-btn {
  flex-shrink: 0;
}

.provider-type {
  color: #6b7280;
  font-size: 0.875rem;
  margin: 0;
  font-weight: 500;
}

.provider-rating {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.rating-value {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  color: #f59e0b;
  font-weight: 600;
  font-size: 0.9375rem;
}

.star-icon {
  font-size: 1rem;
}

.review-count {
  color: #9ca3af;
  font-size: 0.875rem;
}

.provider-address {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #6b7280;
  font-size: 0.875rem;
}

.location-icon {
  font-size: 1rem;
  flex-shrink: 0;
}

.address-text {
  line-height: 1.4;
}

.provider-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.tag {
  background: #f3f4f6;
  color: #4b5563;
  padding: 4px 12px;
  border-radius: 16px;
  font-size: 0.8125rem;
  font-weight: 500;
}

.price-range {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
}

.price-label {
  color: #6b7280;
}

.price-value {
  color: #1f2937;
  font-weight: 600;
}

.next-slot {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: #ecfdf5;
  color: #059669;
  padding: 8px 12px;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
}

.slot-icon {
  font-size: 1rem;
}

.provider-features {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  padding-top: 0.5rem;
  border-top: 1px solid #f3f4f6;
}

.feature {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  color: #059669;
  font-size: 0.8125rem;
  font-weight: 500;
}

.feature-icon {
  font-size: 0.875rem;
  font-weight: 700;
}

.book-now-btn {
  background: #1e96fc;
  color: white;
  border: none;
  padding: 12px 20px;
  border-radius: 10px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  margin-top: auto;
  transition: all 0.3s ease;
}

.book-now-btn:hover {
  background: #0d7de3;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(30, 150, 252, 0.3);
}

.book-now-btn:active {
  transform: scale(0.98);
}

.arrow {
  transition: transform 0.3s ease;
}

.book-now-btn:hover .arrow {
  transform: translateX(-4px);
}

/* Responsive */
@media (max-width: 640px) {
  .provider-image {
    height: 160px;
  }

  .provider-info {
    padding: 1rem;
    gap: 0.75rem;
  }

  .provider-name {
    font-size: 1rem;
  }
}
</style>
