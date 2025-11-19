<template>
  <transition name="sidebar">
    <div v-if="isOpen" class="sidebar-overlay" @click="handleClose">
      <aside class="sidebar" @click.stop dir="rtl">
        <!-- Header -->
        <div class="sidebar-header">
          <h2 class="sidebar-title">نوبت‌های من</h2>
          <button @click="handleClose" class="close-button" aria-label="بستن">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <!-- Tabs -->
        <div class="tabs">
          <button
            class="tab"
            :class="{ active: activeTab === 'upcoming' }"
            @click="activeTab = 'upcoming'"
          >
            آینده
            <span v-if="upcomingBookings.length > 0" class="badge">{{ upcomingBookings.length }}</span>
          </button>
          <button
            class="tab"
            :class="{ active: activeTab === 'past' }"
            @click="activeTab = 'past'"
          >
            گذشته
            <span v-if="bookingHistory.length > 0" class="badge">{{ bookingHistory.length }}</span>
          </button>
        </div>

        <!-- Content -->
        <div class="sidebar-content">
          <!-- Upcoming Bookings Tab -->
          <div v-if="activeTab === 'upcoming'" class="tab-content">
            <div v-if="loading.upcomingBookings" class="loading-state">
              <div class="spinner"></div>
              <p>در حال بارگذاری...</p>
            </div>

            <div v-else-if="upcomingBookings.length === 0" class="empty-state">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="empty-icon">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <p class="empty-title">شما نوبت آینده‌ای ندارید</p>
              <p class="empty-description">هنوز هیچ نوبت آینده‌ای رزرو نکرده‌اید</p>
            </div>

            <div v-else class="bookings-list">
              <BookingCard
                v-for="booking in upcomingBookings"
                :key="booking.id"
                :booking="booking"
                type="upcoming"
                @cancel="handleCancelBooking"
                @reschedule="handleRescheduleBooking"
              />
            </div>
          </div>

          <!-- Past Bookings Tab -->
          <div v-if="activeTab === 'past'" class="tab-content">
            <div v-if="loading.bookingHistory" class="loading-state">
              <div class="spinner"></div>
              <p>در حال بارگذاری...</p>
            </div>

            <div v-else-if="bookingHistory.length === 0" class="empty-state">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="empty-icon">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p class="empty-title">تاریخچه نوبتی وجود ندارد</p>
              <p class="empty-description">هنوز هیچ نوبتی رزرو نکرده‌اید</p>
            </div>

            <div v-else class="bookings-list">
              <BookingCard
                v-for="booking in bookingHistory"
                :key="booking.id"
                :booking="booking"
                type="past"
                @rebook="handleRebookBooking"
              />

              <!-- Load More Button -->
              <button
                v-if="bookingHistoryHasMore"
                @click="handleLoadMore"
                class="load-more-button"
                :disabled="loading.bookingHistory"
              >
                <span v-if="loading.bookingHistory">در حال بارگذاری...</span>
                <span v-else>بارگذاری بیشتر</span>
              </button>
            </div>
          </div>
        </div>
      </aside>
    </div>
  </transition>

  <!-- Cancel Booking Confirmation Modal -->
  <CancelBookingModal
    ref="cancelModalRef"
    :is-open="showCancelModal"
    :booking="bookingToCancel"
    @close="closeCancelModal"
    @confirm="confirmCancelBooking"
  />
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useCustomerStore } from '../../stores/customer.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useToast } from '@/core/composables/useToast'
import { bookingService } from '@/modules/booking/api/booking.service'
import BookingCard from './BookingCard.vue'
import CancelBookingModal from './CancelBookingModal.vue'
import type { UpcomingBooking } from '../../types/customer.types'

interface Props {
  isOpen: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{
  close: []
}>()

const router = useRouter()
const customerStore = useCustomerStore()
const authStore = useAuthStore()
const { showSuccess, showError } = useToast()

const activeTab = ref<'upcoming' | 'past'>('upcoming')
const showCancelModal = ref(false)
const bookingToCancel = ref<UpcomingBooking | null>(null)
const cancelModalRef = ref<InstanceType<typeof CancelBookingModal> | null>(null)

const upcomingBookings = computed(() => customerStore.upcomingBookings)
const bookingHistory = computed(() => customerStore.bookingHistory)
const loading = computed(() => customerStore.loading)
const bookingHistoryHasMore = computed(() => customerStore.bookingHistoryHasMore)

// Fetch data when sidebar opens
watch(() => props.isOpen, async (isOpen) => {
  if (isOpen && authStore.customerId) {
    const customerId = authStore.customerId

    // Fetch upcoming bookings if not loaded
    if (upcomingBookings.value.length === 0) {
      await customerStore.fetchUpcomingBookings(customerId, 5)
    }

    // Fetch booking history if not loaded
    if (bookingHistory.value.length === 0) {
      await customerStore.fetchBookingHistory(customerId, 1, 20)
    }
  }
}, { immediate: true })

async function handleLoadMore(): Promise<void> {
  if (!authStore.customerId) return

  try {
    await customerStore.loadMoreBookingHistory(authStore.customerId)
  } catch (error) {
    console.error('[BookingsSidebar] Error loading more bookings:', error)
    showError('خطا در بارگذاری نوبت‌ها')
  }
}

function handleCancelBooking(bookingId: string): void {
  // Find the booking to cancel
  const booking = upcomingBookings.value.find(b => b.id === bookingId)
  if (!booking) {
    showError('نوبت یافت نشد')
    return
  }

  bookingToCancel.value = booking
  showCancelModal.value = true
}

async function confirmCancelBooking(reason: string, notes?: string): Promise<void> {
  if (!bookingToCancel.value || !authStore.customerId) return

  try {
    await bookingService.cancelBooking(bookingToCancel.value.id, {
      reason,
      notes
    })

    showSuccess('نوبت با موفقیت لغو شد')

    // Refresh upcoming bookings
    await customerStore.fetchUpcomingBookings(authStore.customerId, 5)

    // Close modal
    showCancelModal.value = false
    bookingToCancel.value = null
    cancelModalRef.value?.resetForm()
  } catch (error) {
    console.error('[BookingsSidebar] Error cancelling booking:', error)
    showError('خطا در لغو نوبت. لطفا دوباره تلاش کنید')
  }
}

function closeCancelModal(): void {
  showCancelModal.value = false
  bookingToCancel.value = null
  cancelModalRef.value?.resetForm()
}

function handleRescheduleBooking(bookingId: string): void {
  // Find the booking
  const booking = upcomingBookings.value.find(b => b.id === bookingId)
  if (!booking) {
    showError('نوبت یافت نشد')
    return
  }

  // Close sidebar and redirect to booking wizard with reschedule mode
  handleClose()
  router.push(`/booking/${booking.providerId}?reschedule=${bookingId}`)
  showSuccess('در حال انتقال به صفحه تغییر زمان...')
}

function handleRebookBooking(providerId: string, serviceName: string): void {
  // Redirect to booking wizard with pre-filled provider
  handleClose()
  router.push(`/booking/${providerId}?service=${encodeURIComponent(serviceName)}`)
  showSuccess('در حال انتقال به صفحه رزرو...')
}

function handleClose(): void {
  emit('close')
}
</script>

<style scoped lang="scss">
.sidebar-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  z-index: 1000;
  display: flex;
  justify-content: flex-start;
}

.sidebar {
  width: 100%;
  max-width: 480px;
  background: white;
  height: 100vh;
  display: flex;
  flex-direction: column;
  box-shadow: 2px 0 16px rgba(0, 0, 0, 0.1);
  overflow: hidden;
}

.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.sidebar-title {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0;
}

.close-button {
  background: none;
  border: none;
  cursor: pointer;
  color: white;
  padding: 0.5rem;
  border-radius: 8px;
  transition: background 0.2s;

  svg {
    width: 24px;
    height: 24px;
  }

  &:hover {
    background: rgba(255, 255, 255, 0.2);
  }
}

.tabs {
  display: flex;
  border-bottom: 1px solid #e5e7eb;
  background: white;
}

.tab {
  flex: 1;
  padding: 1rem;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  font-size: 1rem;
  font-weight: 500;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;

  &:hover {
    background: #f9fafb;
  }

  &.active {
    color: #667eea;
    border-bottom-color: #667eea;
  }
}

.badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 20px;
  height: 20px;
  padding: 0 6px;
  border-radius: 10px;
  background: #667eea;
  color: white;
  font-size: 0.75rem;
  font-weight: 600;
}

.sidebar-content {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem;
}

.tab-content {
  display: flex;
  flex-direction: column;
}

.bookings-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  color: #6b7280;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #e5e7eb;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  text-align: center;
}

.empty-icon {
  width: 64px;
  height: 64px;
  color: #d1d5db;
  margin-bottom: 1rem;
}

.empty-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: #374151;
  margin-bottom: 0.5rem;
}

.empty-description {
  font-size: 0.875rem;
  color: #6b7280;
}

.load-more-button {
  width: 100%;
  padding: 0.75rem;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-weight: 500;
  color: #667eea;
  cursor: pointer;
  transition: all 0.2s;
  margin-top: 0.5rem;

  &:hover:not(:disabled) {
    background: #f9fafb;
    border-color: #667eea;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

// Sidebar transition
.sidebar-enter-active,
.sidebar-leave-active {
  transition: all 0.3s ease;
}

.sidebar-enter-from .sidebar,
.sidebar-leave-to .sidebar {
  transform: translateX(-100%);
}

.sidebar-enter-from,
.sidebar-leave-to {
  opacity: 0;
}

// Mobile responsive
@media (max-width: 640px) {
  .sidebar {
    max-width: 100%;
  }
}
</style>
