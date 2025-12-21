<template>
  <div class="my-bookings" dir="rtl">
    <div class="bookings-header">
      <h2>رزروهای من</h2>
      <button @click="router.push({ name: 'CustomerBooking' })" class="btn-new-booking">
        + رزرو جدید
      </button>
    </div>

    <div class="bookings-tabs">
      <button @click="activeTab = 'upcoming'" :class="{ active: activeTab === 'upcoming' }">
        آینده ({{ upcomingCount }})
      </button>
      <button @click="activeTab = 'past'" :class="{ active: activeTab === 'past' }">
        گذشته ({{ pastCount }})
      </button>
      <button @click="activeTab = 'cancelled'" :class="{ active: activeTab === 'cancelled' }">
        لغو شده ({{ cancelledCount }})
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-container">
      <div class="spinner"></div>
      <p>در حال بارگذاری...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-container">
      <div class="error-icon">⚠️</div>
      <p>{{ error }}</p>
      <button @click="loadBookings" class="btn-retry">تلاش مجدد</button>
    </div>

    <!-- Empty State -->
    <div v-else-if="filteredBookings.length === 0" class="empty-container">
      <div class="empty-icon">📅</div>
      <h3>رزروی یافت نشد</h3>
      <p v-if="activeTab === 'upcoming'">شما هیچ رزرو آینده‌ای ندارید</p>
      <p v-else-if="activeTab === 'past'">شما هیچ رزرو گذشته‌ای ندارید</p>
      <p v-else>شما هیچ رزرو لغو شده‌ای ندارید</p>
      <button @click="router.push({ name: 'CustomerBooking' })" class="btn-new-booking">
        رزرو جدید
      </button>
    </div>

    <!-- Bookings List -->
    <div v-else class="bookings-list">
      <div v-for="booking in filteredBookings" :key="booking.bookingId" class="booking-card">
        <div class="booking-status" :class="booking.statusColor">
          {{ booking.statusLabel }}
        </div>
        <div class="booking-content">
          <div class="booking-date-time">
            <div class="date">{{ booking.formattedDate }}</div>
            <div class="time">{{ booking.formattedTime }}</div>
          </div>
          <div class="booking-details">
            <h3>{{ booking.serviceName }}</h3>
            <p class="provider">🏢 {{ booking.providerName }}</p>
            <p v-if="booking.staffName" class="staff">👤 {{ booking.staffName }}</p>
            <p class="duration">⏱ {{ booking.formattedDuration }}</p>
            <p class="price">💰 {{ booking.formattedPrice }}</p>
          </div>
          <div class="booking-actions">
            <button @click="viewDetails(booking.bookingId)" class="btn-view">جزئیات</button>
            <button
              v-if="booking.canCancel"
              @click="cancelBooking(booking.bookingId)"
              class="btn-cancel"
              :disabled="cancellingBookingId === booking.bookingId"
            >
              {{ cancellingBookingId === booking.bookingId ? 'در حال لغو...' : 'لغو رزرو' }}
            </button>
            <button v-if="booking.isPast" @click="rebookService(booking)" class="btn-rebook">
              رزرو مجدد
            </button>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div v-if="pagination.totalPages > 1" class="pagination">
        <div class="pagination-info">
          نمایش {{ pagination.itemRange }} از {{ pagination.totalCount }} رزرو
        </div>
        <div class="pagination-controls">
          <button
            @click="changePage(pagination.previousPageNumber!)"
            :disabled="!pagination.hasPreviousPage"
            class="btn-page"
          >
            قبلی
          </button>
          <span class="page-numbers">
            صفحه {{ pagination.pageNumber }} از {{ pagination.totalPages }}
          </span>
          <button
            @click="changePage(pagination.nextPageNumber!)"
            :disabled="!pagination.hasNextPage"
            class="btn-page"
          >
            بعدی
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { bookingService } from '@/modules/booking/api/booking.service'
import type { CustomerBookingDto } from '@/modules/booking/types/booking-api.types'
import { mapToEnrichedBookingView, type EnrichedBookingView } from '@/modules/booking/mappers/booking-dto.mapper'

const router = useRouter()
const activeTab = ref<'upcoming' | 'past' | 'cancelled'>('upcoming')

// State management
const bookings = ref<EnrichedBookingView[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const cancellingBookingId = ref<string | null>(null)

// Pagination state
const pagination = ref({
  pageNumber: 1,
  pageSize: 20,
  totalPages: 0,
  totalCount: 0,
  hasPreviousPage: false,
  hasNextPage: false,
  previousPageNumber: null as number | null,
  nextPageNumber: null as number | null,
  itemRange: '',
})

// Computed counts (from all bookings, not just current page)
const upcomingCount = computed(() =>
  bookings.value.filter(b => b.isUpcoming && ['Requested', 'Confirmed', 'Pending'].includes(b.status)).length
)

const pastCount = computed(() =>
  bookings.value.filter(b => b.isPast).length
)

const cancelledCount = computed(() =>
  bookings.value.filter(b => b.status === 'Cancelled').length
)

// Filtered bookings based on active tab
const filteredBookings = computed(() => {
  if (activeTab.value === 'upcoming') {
    return bookings.value.filter(b => b.isUpcoming && ['Requested', 'Confirmed', 'Pending'].includes(b.status))
  } else if (activeTab.value === 'past') {
    return bookings.value.filter(b => b.isPast && b.status !== 'Cancelled')
  } else {
    return bookings.value.filter(b => b.status === 'Cancelled')
  }
})

// Load bookings from API
async function loadBookings() {
  try {
    loading.value = true
    error.value = null

    // Determine date filter based on active tab
    const filters: any = {
      page: pagination.value.pageNumber,
      size: pagination.value.pageSize,
      sort: 'StartTime',
      sortDesc: activeTab.value !== 'upcoming', // Descending for past, ascending for upcoming
    }

    // Add date filters based on tab
    if (activeTab.value === 'upcoming') {
      filters.from = new Date().toISOString()
    } else if (activeTab.value === 'past') {
      filters.to = new Date().toISOString()
    }

    // Add status filter for cancelled
    if (activeTab.value === 'cancelled') {
      filters.status = 'Cancelled'
    }

    const response = await bookingService.getMyBookings(filters)

    // Map to enriched view models with formatting
    bookings.value = response.items.map(mapToEnrichedBookingView)

    // Update pagination
    pagination.value = {
      pageNumber: response.pageNumber,
      pageSize: response.pageSize,
      totalPages: response.totalPages,
      totalCount: response.totalCount,
      hasPreviousPage: response.hasPreviousPage,
      hasNextPage: response.hasNextPage,
      previousPageNumber: response.previousPageNumber,
      nextPageNumber: response.nextPageNumber,
      itemRange: response.itemRange,
    }

    console.log('[MyBookingsView] Loaded bookings:', bookings.value.length)
  } catch (err) {
    console.error('[MyBookingsView] Error loading bookings:', err)
    error.value = 'خطا در بارگذاری رزروها. لطفا دوباره تلاش کنید.'
  } finally {
    loading.value = false
  }
}

// Change page
function changePage(page: number) {
  pagination.value.pageNumber = page
  loadBookings()
}

// Watch for tab changes and reload
watch(activeTab, () => {
  pagination.value.pageNumber = 1 // Reset to first page
  loadBookings()
})

// Action handlers
function viewDetails(id: string) {
  router.push(`/customer/booking/${id}`)
}

async function cancelBooking(id: string) {
  if (!confirm('آیا از لغو این رزرو مطمئن هستید؟')) {
    return
  }

  try {
    cancellingBookingId.value = id

    await bookingService.cancelBooking(id, {
      reason: 'درخواست مشتری',
      notes: 'لغو شده توسط مشتری از پنل کاربری'
    })

    // Refresh bookings list
    await loadBookings()

    // Show success message (you can integrate with toast here)
    alert('رزرو با موفقیت لغو شد')
  } catch (err) {
    console.error('[MyBookingsView] Error cancelling booking:', err)
    alert('خطا در لغو رزرو. لطفا دوباره تلاش کنید.')
  } finally {
    cancellingBookingId.value = null
  }
}

function rebookService(booking: EnrichedBookingView) {
  router.push({
    name: 'CustomerBooking',
    query: {
      serviceId: booking.serviceId,
      providerId: booking.providerId
    }
  })
}

// Load bookings on mount
onMounted(() => {
  loadBookings()
})
</script>

<style scoped>
.my-bookings {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.bookings-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.bookings-header h2 {
  margin: 0;
  font-size: 1.75rem;
}

.btn-new-booking {
  padding: 0.75rem 1.5rem;
  background: var(--color-primary, #1976d2);
  color: white;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 500;
  transition: background 0.2s;
}

.btn-new-booking:hover {
  background: var(--color-primary-dark, #1565c0);
}

.bookings-tabs {
  display: flex;
  gap: 1rem;
  margin-bottom: 2rem;
  border-bottom: 2px solid var(--color-gray-200, #e5e7eb);
}

.bookings-tabs button {
  padding: 1rem 1.5rem;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  cursor: pointer;
  font-size: 1rem;
  transition: all 0.2s;
}

.bookings-tabs button.active {
  border-bottom-color: var(--color-primary, #1976d2);
  color: var(--color-primary, #1976d2);
  font-weight: 600;
}

/* Loading State */
.loading-container {
  text-align: center;
  padding: 4rem 2rem;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 4px solid var(--color-gray-200, #e5e7eb);
  border-top-color: var(--color-primary, #1976d2);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Error State */
.error-container {
  text-align: center;
  padding: 4rem 2rem;
  background: var(--color-red-50, #fef2f2);
  border-radius: 0.75rem;
}

.error-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}

.error-container p {
  color: var(--color-red-700, #b91c1c);
  margin-bottom: 1.5rem;
  font-size: 1.125rem;
}

.btn-retry {
  padding: 0.75rem 1.5rem;
  background: var(--color-red-600, #dc2626);
  color: white;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 500;
}

/* Empty State */
.empty-container {
  text-align: center;
  padding: 4rem 2rem;
  background: var(--color-gray-50, #f9fafb);
  border-radius: 0.75rem;
}

.empty-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
}

.empty-container h3 {
  margin: 0 0 0.5rem;
  font-size: 1.5rem;
}

.empty-container p {
  color: var(--color-gray-600, #4b5563);
  margin-bottom: 2rem;
}

/* Booking Cards */
.booking-card {
  background: white;
  border-radius: 0.75rem;
  padding: 1.5rem;
  margin-bottom: 1rem;
  position: relative;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  transition: box-shadow 0.2s;
}

.booking-card:hover {
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.booking-status {
  position: absolute;
  top: 1rem;
  left: 1rem;
  padding: 0.375rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
}

.booking-status.success {
  background: var(--color-green-100, #d1fae5);
  color: var(--color-green-700, #047857);
}

.booking-status.warning {
  background: var(--color-yellow-100, #fef3c7);
  color: var(--color-yellow-700, #b45309);
}

.booking-status.info {
  background: var(--color-blue-100, #dbeafe);
  color: var(--color-blue-700, #1d4ed8);
}

.booking-status.error {
  background: var(--color-red-100, #fee2e2);
  color: var(--color-red-700, #b91c1c);
}

.booking-status.default {
  background: var(--color-gray-200, #e5e7eb);
  color: var(--color-gray-700, #374151);
}

.booking-content {
  display: grid;
  grid-template-columns: auto 1fr auto;
  gap: 1.5rem;
  align-items: center;
}

.booking-date-time {
  text-align: center;
  padding: 1rem;
  background: var(--color-primary-50, #e3f2fd);
  border-radius: 0.5rem;
  min-width: 120px;
}

.date {
  font-weight: 600;
  margin-bottom: 0.25rem;
  font-size: 0.9rem;
}

.time {
  font-size: 0.875rem;
  color: var(--color-gray-600, #4b5563);
}

.booking-details {
  flex: 1;
}

.booking-details h3 {
  margin: 0 0 0.5rem;
  font-size: 1.125rem;
  color: var(--color-gray-900, #111827);
}

.booking-details p {
  margin: 0.25rem 0;
  color: var(--color-gray-600, #4b5563);
  font-size: 0.875rem;
}

.booking-details p.provider {
  font-weight: 500;
  color: var(--color-gray-700, #374151);
}

.booking-actions {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.booking-actions button {
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  min-width: 100px;
  font-weight: 500;
  transition: all 0.2s;
}

.booking-actions button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-view {
  background: var(--color-primary, #1976d2);
  color: white;
}

.btn-view:hover:not(:disabled) {
  background: var(--color-primary-dark, #1565c0);
}

.btn-cancel {
  background: var(--color-red-100, #fee2e2);
  color: var(--color-red-700, #b91c1c);
}

.btn-cancel:hover:not(:disabled) {
  background: var(--color-red-200, #fecaca);
}

.btn-rebook {
  background: var(--color-gray-100, #f3f4f6);
  color: var(--color-gray-700, #374151);
}

.btn-rebook:hover:not(:disabled) {
  background: var(--color-gray-200, #e5e7eb);
}

/* Pagination */
.pagination {
  margin-top: 2rem;
  padding: 1.5rem;
  background: var(--color-gray-50, #f9fafb);
  border-radius: 0.75rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.pagination-info {
  color: var(--color-gray-600, #4b5563);
  font-size: 0.875rem;
}

.pagination-controls {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.page-numbers {
  color: var(--color-gray-700, #374151);
  font-weight: 500;
}

.btn-page {
  padding: 0.5rem 1rem;
  background: white;
  border: 1px solid var(--color-gray-300, #d1d5db);
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-page:hover:not(:disabled) {
  background: var(--color-gray-100, #f3f4f6);
}

.btn-page:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Responsive */
@media (max-width: 768px) {
  .booking-content {
    grid-template-columns: 1fr;
    gap: 1rem;
  }

  .booking-date-time {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .booking-actions {
    flex-direction: row;
  }

  .booking-status {
    position: relative;
    top: 0;
    left: 0;
    margin-bottom: 1rem;
    display: inline-block;
  }

  .pagination {
    flex-direction: column;
    gap: 1rem;
  }

  .pagination-controls {
    width: 100%;
    justify-content: space-between;
  }
}
</style>
