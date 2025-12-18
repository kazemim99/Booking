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
            <span v-if="pastBookings.length > 0" class="badge">{{ pastBookings.length }}</span>
          </button>
        </div>

        <!-- Content -->
        <div class="sidebar-content">
          <!-- Upcoming Bookings Tab -->
          <div v-if="activeTab === 'upcoming'" class="tab-content">
            <div v-if="loading.upcoming" class="loading-state">
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
              <div
                v-for="booking in upcomingBookings"
                :key="booking.bookingId"
                class="booking-card"
              >
                <div class="booking-header">
                  <div class="booking-status" :class="booking.statusColor">
                    {{ booking.statusLabel }}
                  </div>
                  <div class="booking-date">
                    {{ booking.formattedDate }}
                  </div>
                </div>

                <div class="booking-body">
                  <h3 class="service-name">{{ booking.serviceName }}</h3>
                  <p class="provider-name">🏢 {{ booking.providerName }}</p>
                  <p v-if="booking.staffName" class="staff-name">👤 {{ booking.staffName }}</p>
                  <p class="booking-time">🕐 {{ booking.formattedTime }}</p>
                  <p class="booking-duration">⏱ {{ booking.formattedDuration }}</p>
                  <p class="booking-price">💰 {{ booking.formattedPrice }}</p>
                </div>

                <div class="booking-actions">
                  <button
                    v-if="booking.canCancel"
                    @click="handleCancelBooking(booking.bookingId)"
                    class="btn-cancel"
                  >
                    لغو رزرو
                  </button>
                  <button
                    v-if="booking.canReschedule"
                    @click="handleRescheduleBooking(booking)"
                    class="btn-reschedule"
                  >
                    تغییر زمان
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Past Bookings Tab -->
          <div v-if="activeTab === 'past'" class="tab-content">
            <div v-if="loading.past" class="loading-state">
              <div class="spinner"></div>
              <p>در حال بارگذاری...</p>
            </div>

            <div v-else-if="pastBookings.length === 0" class="empty-state">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="empty-icon">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p class="empty-title">تاریخچه نوبتی وجود ندارد</p>
              <p class="empty-description">هنوز هیچ نوبتی رزرو نکرده‌اید</p>
            </div>

            <div v-else class="bookings-list">
              <div
                v-for="booking in pastBookings"
                :key="booking.bookingId"
                class="booking-card past"
              >
                <div class="booking-header">
                  <div class="booking-status" :class="booking.statusColor">
                    {{ booking.statusLabel }}
                  </div>
                  <div class="booking-date">
                    {{ booking.formattedDate }}
                  </div>
                </div>

                <div class="booking-body">
                  <h3 class="service-name">{{ booking.serviceName }}</h3>
                  <p class="provider-name">🏢 {{ booking.providerName }}</p>
                  <p v-if="booking.staffName" class="staff-name">👤 {{ booking.staffName }}</p>
                  <p class="booking-time">🕐 {{ booking.formattedTime }}</p>
                  <p class="booking-price">💰 {{ booking.formattedPrice }}</p>
                </div>

                <div class="booking-actions">
                  <button
                    @click="handleRebookBooking(booking)"
                    class="btn-rebook"
                  >
                    رزرو مجدد
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </aside>
    </div>
  </transition>

  <!-- Cancel Booking Confirmation Modal -->
  <CancelBookingModal
    v-if="showCancelModal"
    :is-open="showCancelModal"
    :booking="bookingToCancel"
    @close="closeCancelModal"
    @confirm="confirmCancelBooking"
  />

  <!-- Reschedule Booking Modal -->
  <RescheduleBookingModal
    v-if="showRescheduleModal"
    :is-open="showRescheduleModal"
    :booking="bookingToReschedule"
    @close="closeRescheduleModal"
    @confirm="confirmRescheduleBooking"
  />

  <!-- Rebook Modal (reuses RescheduleBookingModal) -->
  <RescheduleBookingModal
    v-if="showRebookModal"
    :is-open="showRebookModal"
    :booking="bookingToRebook"
    @close="closeRebookModal"
    @confirm="confirmRebookBooking"
  />
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { bookingService } from '@/modules/booking/api/booking.service'
import { mapToEnrichedBookingView, type EnrichedBookingView } from '@/modules/booking/mappers/booking-dto.mapper'
import CancelBookingModal from './CancelBookingModal.vue'
import RescheduleBookingModal from './RescheduleBookingModal.vue'
import type { CustomerBookingDto } from '@/modules/booking/types/booking-api.types'

interface Props {
  isOpen: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{
  close: []
}>()

// State
const activeTab = ref<'upcoming' | 'past'>('upcoming')

// Cancel modal state
const showCancelModal = ref(false)
const bookingToCancel = ref<EnrichedBookingView | null>(null)

// Reschedule modal state
const showRescheduleModal = ref(false)
const bookingToReschedule = ref<EnrichedBookingView | null>(null)

// Rebook modal state
const showRebookModal = ref(false)
const bookingToRebook = ref<EnrichedBookingView | null>(null)

// Bookings data
const upcomingBookings = ref<EnrichedBookingView[]>([])
const pastBookings = ref<EnrichedBookingView[]>([])

const loading = ref({
  upcoming: false,
  past: false
})

// Fetch upcoming bookings
async function fetchUpcomingBookings(): Promise<void> {
  try {
    loading.value.upcoming = true

    // Use the helper method that filters for upcoming bookings with correct statuses
    const bookings = await bookingService.getUpcomingBookings(10)

    // Map to enriched view models
    upcomingBookings.value = bookings.map(mapToEnrichedBookingView)

    console.log('[BookingsSidebar] Loaded upcoming bookings:', upcomingBookings.value.length)
  } catch (error) {
    console.error('[BookingsSidebar] Error fetching upcoming bookings:', error)
    showErrorMessage('خطا در بارگذاری نوبت‌های آینده')
  } finally {
    loading.value.upcoming = false
  }
}

// Fetch past bookings
async function fetchPastBookings(): Promise<void> {
  try {
    loading.value.past = true

    // Use the helper method that filters for past bookings
    const bookings = await bookingService.getPastBookings(10)

    // Map to enriched view models
    pastBookings.value = bookings.map(mapToEnrichedBookingView)

    console.log('[BookingsSidebar] Loaded past bookings:', pastBookings.value.length)
  } catch (error) {
    console.error('[BookingsSidebar] Error fetching past bookings:', error)
    showErrorMessage('خطا در بارگذاری تاریخچه نوبت‌ها')
  } finally {
    loading.value.past = false
  }
}

// Watch for sidebar open/close
watch(() => props.isOpen, async (isOpen) => {
  if (isOpen) {
    // Fetch bookings when sidebar opens
    await fetchUpcomingBookings()
    await fetchPastBookings()
  }
}, { immediate: true })

// Watch for tab changes
watch(activeTab, async (newTab) => {
  if (newTab === 'upcoming' && upcomingBookings.value.length === 0) {
    await fetchUpcomingBookings()
  } else if (newTab === 'past' && pastBookings.value.length === 0) {
    await fetchPastBookings()
  }
})

// Cancel booking handler
function handleCancelBooking(bookingId: string): void {
  const booking = upcomingBookings.value.find(b => b.bookingId === bookingId)
  if (!booking) {
    showErrorMessage('نوبت یافت نشد')
    return
  }

  bookingToCancel.value = booking
  showCancelModal.value = true
}

// Confirm cancel booking
async function confirmCancelBooking(reason: string, notes?: string): Promise<void> {
  if (!bookingToCancel.value) return

  try {
    await bookingService.cancelBooking(bookingToCancel.value.bookingId, {
      reason,
      notes
    })

    showSuccessMessage('نوبت با موفقیت لغو شد')

    // Refresh bookings
    await fetchUpcomingBookings()
    await fetchPastBookings()

    // Close modal
    showCancelModal.value = false
    bookingToCancel.value = null
  } catch (error) {
    console.error('[BookingsSidebar] Error cancelling booking:', error)
    showErrorMessage('خطا در لغو نوبت. لطفا دوباره تلاش کنید')
  }
}

// Close cancel modal
function closeCancelModal(): void {
  showCancelModal.value = false
  bookingToCancel.value = null
}

// Reschedule booking handler
function handleRescheduleBooking(booking: EnrichedBookingView): void {
  bookingToReschedule.value = booking
  showRescheduleModal.value = true
}

// Confirm reschedule booking
async function confirmRescheduleBooking(newStartTime: string, reason?: string): Promise<void> {
  if (!bookingToReschedule.value) return

  try {
    await bookingService.rescheduleBooking({
      appointmentId: bookingToReschedule.value.bookingId,
      newStartTime,
      reason: reason || 'درخواست تغییر زمان'
    })

    showSuccessMessage('نوبت با موفقیت تغییر زمان یافت')

    // Refresh bookings
    await fetchUpcomingBookings()
    await fetchPastBookings()

    // Close modal
    showRescheduleModal.value = false
    bookingToReschedule.value = null
  } catch (error) {
    console.error('[BookingsSidebar] Error rescheduling booking:', error)
    showErrorMessage('خطا در تغییر زمان نوبت. لطفا دوباره تلاش کنید')
  }
}

// Close reschedule modal
function closeRescheduleModal(): void {
  showRescheduleModal.value = false
  bookingToReschedule.value = null
}

// Rebook handler
function handleRebookBooking(booking: EnrichedBookingView): void {
  bookingToRebook.value = booking
  showRebookModal.value = true
}

// Confirm rebook (create new booking)
async function confirmRebookBooking(newStartTime: string): Promise<void> {
  if (!bookingToRebook.value) return

  try {
    // Create new booking with same service and provider
    await bookingService.createBooking({
      customerId: '', // Will be set from auth context
      providerId: bookingToRebook.value.providerId,
      serviceId: bookingToRebook.value.serviceId,
      staffProviderId: bookingToRebook.value.staffId || '',
      startTime: newStartTime,
      customerNotes: 'رزرو مجدد'
    })

    showSuccessMessage('نوبت جدید با موفقیت رزرو شد')

    // Refresh bookings
    await fetchUpcomingBookings()
    await fetchPastBookings()

    // Close modal
    showRebookModal.value = false
    bookingToRebook.value = null
  } catch (error) {
    console.error('[BookingsSidebar] Error rebooking:', error)
    showErrorMessage('خطا در رزرو مجدد. لطفا دوباره تلاش کنید')
  }
}

// Close rebook modal
function closeRebookModal(): void {
  showRebookModal.value = false
  bookingToRebook.value = null
}

// Close sidebar
function handleClose(): void {
  emit('close')
}

// Toast notification helpers (fallback to alert if useToast not available)
function showSuccessMessage(message: string): void {
  try {
    const { showSuccess } = require('@/core/composables/useToast')
    showSuccess(message)
  } catch {
    // Fallback to console if toast not available
    console.log('[Success]', message)
  }
}

function showErrorMessage(message: string): void {
  try {
    const { showError } = require('@/core/composables/useToast')
    showError(message)
  } catch {
    // Fallback to console if toast not available
    console.error('[Error]', message)
  }
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

.booking-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 1rem;
  transition: all 0.2s;

  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    border-color: #667eea;
  }

  &.past {
    opacity: 0.8;
  }
}

.booking-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
}

.booking-status {
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;

  &.success {
    background: #d1fae5;
    color: #047857;
  }

  &.warning {
    background: #fef3c7;
    color: #b45309;
  }

  &.info {
    background: #dbeafe;
    color: #1d4ed8;
  }

  &.error {
    background: #fee2e2;
    color: #b91c1c;
  }

  &.default {
    background: #e5e7eb;
    color: #374151;
  }
}

.booking-date {
  font-size: 0.875rem;
  color: #6b7280;
  font-weight: 500;
}

.booking-body {
  margin-bottom: 0.75rem;
}

.service-name {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
  margin: 0 0 0.5rem;
}

.provider-name,
.staff-name,
.booking-time,
.booking-duration,
.booking-price {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0.25rem 0;
}

.provider-name {
  font-weight: 500;
  color: #374151;
}

.booking-actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.75rem;
}

.booking-actions button {
  flex: 1;
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-cancel {
  background: #fee2e2;
  color: #b91c1c;

  &:hover {
    background: #fecaca;
  }
}

.btn-reschedule {
  background: #e0e7ff;
  color: #4338ca;

  &:hover {
    background: #c7d2fe;
  }
}

.btn-rebook {
  background: #f3f4f6;
  color: #374151;

  &:hover {
    background: #e5e7eb;
  }
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
