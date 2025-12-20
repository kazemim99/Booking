<template>
  <div class="quick-rebook-card" dir="rtl">
    <!-- Provider Header -->
    <div class="provider-header">
      <div class="provider-logo">
        <img
          v-if="suggestion.favorite.provider?.logoUrl"
          :src="suggestion.favorite.provider.logoUrl"
          :alt="suggestion.favorite.provider.businessName"
        />
        <div v-else class="placeholder">
          {{ getInitials(suggestion.favorite.provider?.businessName) }}
        </div>
      </div>

      <div class="provider-info">
        <h3>{{ suggestion.favorite.provider?.businessName || 'نامشخص' }}</h3>
        <div class="rating">
          <span class="stars">★ {{ suggestion.favorite.provider?.rating?.toFixed(1) || '0.0' }}</span>
          <span class="reviews">({{ suggestion.favorite.provider?.reviewCount || 0 }} نظر)</span>
        </div>
      </div>

      <FavoriteButton
        :provider-id="suggestion.favorite.providerId"
        @unfavorited="handleUnfavorited"
      />
    </div>

    <!-- Last Service -->
    <div v-if="suggestion.lastService" class="last-service">
      <div class="section-label">
        <svg viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd" />
        </svg>
        <span>آخرین سرویس رزرو شده</span>
      </div>
      <div class="service-details">
        <div class="service-name">{{ suggestion.lastService.name }}</div>
        <div class="service-meta">
          <span class="duration">{{ suggestion.lastService.duration }} دقیقه</span>
          <span class="separator">•</span>
          <span class="price">{{ formatCurrency(suggestion.lastService.price, 'IRR') }}</span>
        </div>
      </div>
    </div>

    <!-- Available Slots -->
    <div v-if="suggestion.suggestedSlots.length > 0" class="available-slots">
      <div class="section-label">
        <svg viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M6 2a1 1 0 00-1 1v1H4a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V6a2 2 0 00-2-2h-1V3a1 1 0 10-2 0v1H7V3a1 1 0 00-1-1zm0 5a1 1 0 000 2h8a1 1 0 100-2H6z" clip-rule="evenodd" />
        </svg>
        <span>زمان‌های پیشنهادی</span>
      </div>

      <div class="slots-grid">
        <button
          v-for="slot in displayedSlots"
          :key="`${slot.date}-${slot.startTime}`"
          @click="handleSlotSelect(slot)"
          class="slot-button"
          :class="{ selected: isSlotSelected(slot) }"
        >
          <div class="slot-date">{{ formatSlotDate(slot.date) }}</div>
          <div class="slot-time">{{ formatTime(slot.startTime) }}</div>
        </button>
      </div>

      <button
        v-if="suggestion.suggestedSlots.length > maxSlotsToShow"
        @click="showAllSlots = !showAllSlots"
        class="btn-show-more"
      >
        {{ showAllSlots ? 'نمایش کمتر' : `نمایش ${suggestion.suggestedSlots.length - maxSlotsToShow} زمان دیگر` }}
      </button>
    </div>

    <!-- Empty State -->
    <div v-else class="no-slots">
      <svg viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
      </svg>
      <p>در حال حاضر زمان خالی وجود ندارد</p>
    </div>

    <!-- Actions -->
    <div class="card-actions">
      <button
        @click="handleQuickRebook"
        :disabled="!selectedSlot || rebooking"
        class="btn-rebook"
        :class="{ loading: rebooking }"
      >
        <div v-if="rebooking" class="spinner"></div>
        <span v-else>{{ selectedSlot ? 'رزرو سریع' : 'انتخاب زمان' }}</span>
      </button>

      <button @click="handleViewProvider" class="btn-view">
        مشاهده جزئیات
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import FavoriteButton from './FavoriteButton.vue'
import type { QuickRebookSuggestion, TimeSlot } from '../../types/favorites.types'
import { formatTimeAgo } from '../../types/favorites.types'
import { formatCurrency } from '@/modules/provider/types/financial.types'

// ============================================================================
// Props & Emits
// ============================================================================

interface Props {
  suggestion: QuickRebookSuggestion
  maxSlotsToShow?: number
}

const props = withDefaults(defineProps<Props>(), {
  maxSlotsToShow: 3,
})

const emit = defineEmits<{
  (e: 'rebook', slot: TimeSlot, suggestion: QuickRebookSuggestion): void
  (e: 'view-provider', providerId: string): void
  (e: 'unfavorited', providerId: string): void
}>()

// ============================================================================
// State
// ============================================================================

const router = useRouter()
const selectedSlot = ref<TimeSlot | null>(null)
const showAllSlots = ref(false)
const rebooking = ref(false)

// ============================================================================
// Computed
// ============================================================================

const displayedSlots = computed(() => {
  if (showAllSlots.value) {
    return props.suggestion.suggestedSlots
  }
  return props.suggestion.suggestedSlots.slice(0, props.maxSlotsToShow)
})

// ============================================================================
// Methods
// ============================================================================

function getInitials(name?: string): string {
  if (!name) return '?'
  return name.charAt(0).toUpperCase()
}

function formatSlotDate(dateStr: string): string {
  const date = new Date(dateStr)
  const today = new Date()
  const tomorrow = new Date(today)
  tomorrow.setDate(tomorrow.getDate() + 1)

  // Reset time for comparison
  const dateOnly = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  const todayOnly = new Date(today.getFullYear(), today.getMonth(), today.getDate())
  const tomorrowOnly = new Date(tomorrow.getFullYear(), tomorrow.getMonth(), tomorrow.getDate())

  if (dateOnly.getTime() === todayOnly.getTime()) {
    return 'امروز'
  } else if (dateOnly.getTime() === tomorrowOnly.getTime()) {
    return 'فردا'
  }

  // Persian weekday names
  const weekdays = ['یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه', 'شنبه']
  const weekday = weekdays[date.getDay()]

  // Format as: "شنبه ۱۵ دی"
  const day = date.getDate()
  const monthNames = [
    'فروردین',
    'اردیبهشت',
    'خرداد',
    'تیر',
    'مرداد',
    'شهریور',
    'مهر',
    'آبان',
    'آذر',
    'دی',
    'بهمن',
    'اسفند',
  ]

  return `${weekday} ${toPersianNumber(day)} ${monthNames[date.getMonth()]}`
}

function formatTime(timeStr: string): string {
  const [hours, minutes] = timeStr.split(':')
  return `${toPersianNumber(hours)}:${toPersianNumber(minutes)}`
}

function toPersianNumber(num: number | string): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return String(num).replace(/\d/g, (digit) => persianDigits[parseInt(digit)])
}

function isSlotSelected(slot: TimeSlot): boolean {
  if (!selectedSlot.value) return false
  return (
    selectedSlot.value.date === slot.date &&
    selectedSlot.value.startTime === slot.startTime
  )
}

function handleSlotSelect(slot: TimeSlot): void {
  if (isSlotSelected(slot)) {
    selectedSlot.value = null
  } else {
    selectedSlot.value = slot
  }
}

async function handleQuickRebook(): Promise<void> {
  if (!selectedSlot.value) return

  rebooking.value = true

  try {
    // Emit the rebook event with selected slot
    emit('rebook', selectedSlot.value, props.suggestion)

    // In a real implementation, you would:
    // 1. Create a booking with the selected slot
    // 2. Navigate to booking confirmation
    // For now, we'll just navigate to the provider page with slot params

    await router.push({
      path: `/providers/${props.suggestion.favorite.providerId}`,
      query: {
        serviceId: props.suggestion.lastService?.id,
        date: selectedSlot.value.date,
        time: selectedSlot.value.startTime,
      },
    })
  } catch (error) {
    console.error('[QuickRebookCard] Error rebooking:', error)
    alert('خطا در رزرو سریع. لطفاً دوباره تلاش کنید.')
  } finally {
    rebooking.value = false
  }
}

function handleViewProvider(): void {
  emit('view-provider', props.suggestion.favorite.providerId)
  router.push(`/providers/${props.suggestion.favorite.providerId}`)
}

function handleUnfavorited(): void {
  emit('unfavorited', props.suggestion.favorite.providerId)
}
</script>

<style scoped>
.quick-rebook-card {
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 0.75rem;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  transition: box-shadow 0.2s;
}

.quick-rebook-card:hover {
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

/* Provider Header */
.provider-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.provider-logo {
  width: 3.5rem;
  height: 3.5rem;
  border-radius: 0.5rem;
  overflow: hidden;
  flex-shrink: 0;
  background: #f7fafc;
}

.provider-logo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
  font-weight: 600;
  color: white;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.provider-info {
  flex: 1;
  min-width: 0;
}

.provider-info h3 {
  font-size: 1.125rem;
  font-weight: 600;
  color: #2d3748;
  margin: 0 0 0.25rem 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.rating {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.stars {
  color: #f59e0b;
  font-weight: 600;
  font-size: 0.875rem;
}

.reviews {
  color: #718096;
  font-size: 0.75rem;
}

/* Sections */
.section-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #4a5568;
  font-weight: 500;
  font-size: 0.875rem;
  margin-bottom: 0.75rem;
}

.section-label svg {
  width: 1.125rem;
  height: 1.125rem;
  color: #718096;
}

/* Last Service */
.last-service {
  padding: 1rem;
  background: #f7fafc;
  border-radius: 0.5rem;
}

.service-details {
  padding-right: 1.625rem;
}

.service-name {
  font-size: 0.9375rem;
  font-weight: 600;
  color: #2d3748;
  margin-bottom: 0.375rem;
}

.service-meta {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #718096;
  font-size: 0.8125rem;
}

.separator {
  color: #cbd5e0;
}

/* Slots */
.available-slots {
  padding: 1rem;
  background: #f7fafc;
  border-radius: 0.5rem;
}

.slots-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(110px, 1fr));
  gap: 0.625rem;
  padding-right: 1.625rem;
}

.slot-button {
  padding: 0.625rem;
  background: white;
  border: 1.5px solid #e2e8f0;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s;
  text-align: center;
}

.slot-button:hover {
  border-color: #3182ce;
  background: #ebf8ff;
}

.slot-button.selected {
  border-color: #3182ce;
  background: #3182ce;
  color: white;
}

.slot-date {
  font-size: 0.75rem;
  font-weight: 500;
  margin-bottom: 0.25rem;
}

.slot-time {
  font-size: 0.875rem;
  font-weight: 600;
}

.btn-show-more {
  margin-top: 0.75rem;
  margin-right: 1.625rem;
  padding: 0.5rem 1rem;
  background: white;
  border: 1px solid #cbd5e0;
  border-radius: 0.375rem;
  color: #3182ce;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-show-more:hover {
  background: #ebf8ff;
  border-color: #3182ce;
}

/* No Slots */
.no-slots {
  text-align: center;
  padding: 2rem 1rem;
  color: #718096;
}

.no-slots svg {
  width: 3rem;
  height: 3rem;
  color: #cbd5e0;
  margin-bottom: 0.75rem;
}

.no-slots p {
  margin: 0;
  font-size: 0.875rem;
}

/* Actions */
.card-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: auto;
}

.btn-rebook,
.btn-view {
  flex: 1;
  padding: 0.75rem 1rem;
  border: none;
  border-radius: 0.5rem;
  font-weight: 600;
  font-size: 0.9375rem;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.btn-rebook {
  background: #3182ce;
  color: white;
}

.btn-rebook:hover:not(:disabled) {
  background: #2c5aa0;
}

.btn-rebook:disabled {
  background: #cbd5e0;
  cursor: not-allowed;
  opacity: 0.6;
}

.btn-rebook.loading {
  background: #cbd5e0;
}

.btn-view {
  background: white;
  color: #3182ce;
  border: 1.5px solid #3182ce;
}

.btn-view:hover {
  background: #ebf8ff;
}

.spinner {
  width: 1.125rem;
  height: 1.125rem;
  border: 2px solid #ffffff40;
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Responsive */
@media (max-width: 640px) {
  .quick-rebook-card {
    padding: 1rem;
  }

  .slots-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .card-actions {
    flex-direction: column;
  }
}
</style>
