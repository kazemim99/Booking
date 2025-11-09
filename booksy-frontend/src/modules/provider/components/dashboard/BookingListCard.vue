<template>
  <div class="booking-list-card">
    <div class="card-content">
      <h3 class="card-title">لیست رزروها</h3>

      <!-- Filters -->
      <div class="filters">
        <!-- Search -->
        <div class="search-box">
          <svg
            class="search-icon"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="جستجو بر اساس نام مشتری یا خدمت..."
            class="search-input"
          />
        </div>

        <!-- Period Filter -->
        <select v-model="filterPeriod" class="filter-select">
          <option value="all">همه</option>
          <option value="today">امروز</option>
          <option value="week">این هفته</option>
          <option value="month">این ماه</option>
        </select>

        <!-- Status Filter -->
        <select v-model="filterStatus" class="filter-select">
          <option value="all">همه</option>
          <option value="scheduled">رزروشده</option>
          <option value="completed">انجام‌شده</option>
          <option value="cancelled">لغوشده</option>
        </select>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="loading-container">
        <div class="spinner"></div>
        <p>در حال بارگذاری...</p>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="error-container">
        <p>{{ error }}</p>
        <button @click="fetchBookings" class="retry-btn">تلاش مجدد</button>
      </div>

      <!-- Table -->
      <div v-else class="table-wrapper">
        <table class="booking-table">
          <thead>
            <tr>
              <th>نام مشتری</th>
              <th>تاریخ</th>
              <th>ساعت</th>
              <th>خدمت</th>
              <th>وضعیت</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="paginatedBookings.length === 0" class="empty-row">
              <td colspan="5">
                رزروی یافت نشد
              </td>
            </tr>
            <tr
              v-for="booking in paginatedBookings"
              :key="booking.id"
              class="data-row"
            >
              <td>{{ booking.customerName }}</td>
              <td>{{ booking.date }}</td>
              <td>{{ booking.time }}</td>
              <td>{{ booking.service }}</td>
              <td>
                <span :class="['status-badge', statusColorClass(booking.status)]">
                  {{ statusLabels[booking.status] }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="pagination">
        <p class="pagination-info">
          صفحه {{ formatNumber(currentPage) }} از {{ formatNumber(totalPages) }}
        </p>
        <div class="pagination-buttons">
          <button
            @click="previousPage"
            :disabled="currentPage === 1"
            class="pagination-btn"
          >
            <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <button
            @click="nextPage"
            :disabled="currentPage === totalPages"
            class="pagination-btn"
          >
            <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'
import { bookingService } from '@/modules/booking/api/booking.service'
import type { Appointment } from '@/modules/booking/types/booking.types'

type BookingStatus = 'scheduled' | 'completed' | 'cancelled'

interface Booking {
  id: string
  customerName: string
  date: string
  time: string
  service: string
  status: BookingStatus
}

interface Props {
  providerId?: string
}

const props = defineProps<Props>()

const bookings = ref<Booking[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

const searchQuery = ref('')
const filterPeriod = ref('all')
const filterStatus = ref('all')
const currentPage = ref(1)
const itemsPerPage = 5

const statusLabels: Record<BookingStatus, string> = {
  scheduled: 'رزروشده',
  completed: 'انجام‌شده',
  cancelled: 'لغوشده'
}

// Fetch bookings from API
const fetchBookings = async () => {
  if (!props.providerId) return

  loading.value = true
  error.value = null

  try {
    const response = await bookingService.getProviderBookings(
      props.providerId,
      undefined, // status filter
      1, // page
      100 // get more bookings for local filtering
    )

    // Map API response to component format
    bookings.value = response.items.map(mapAppointmentToBooking)
  } catch (err) {
    console.error('Error fetching bookings:', err)
    error.value = 'خطا در بارگذاری لیست رزروها'
    bookings.value = []
  } finally {
    loading.value = false
  }
}

// Map Appointment from API to Booking for display
const mapAppointmentToBooking = (appointment: Appointment): Booking => {
  return {
    id: appointment.id,
    customerName: appointment.clientId, // TODO: Fetch customer name
    date: formatDate(appointment.scheduledStartTime),
    time: formatTime(appointment.scheduledStartTime),
    service: appointment.serviceId, // TODO: Fetch service name
    status: mapStatus(appointment.status),
  }
}

// Map API status to component status
const mapStatus = (apiStatus: string): BookingStatus => {
  const statusMap: Record<string, BookingStatus> = {
    Pending: 'scheduled',
    Confirmed: 'scheduled',
    InProgress: 'scheduled',
    Completed: 'completed',
    Cancelled: 'cancelled',
    NoShow: 'cancelled',
  }
  return statusMap[apiStatus] || 'scheduled'
}

// Format date to Persian
const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  // TODO: Use Jalaali date formatting
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return convertEnglishToPersianNumbers(`${year}/${month}/${day}`)
}

// Format time
const formatTime = (dateString: string): string => {
  const date = new Date(dateString)
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return convertEnglishToPersianNumbers(`${hours}:${minutes}`)
}

// Watch for providerId changes
watch(() => props.providerId, () => {
  if (props.providerId) {
    fetchBookings()
  }
})

// Load data on mount
onMounted(() => {
  if (props.providerId) {
    fetchBookings()
  }
})

const statusColorClass = (status: BookingStatus): string => {
  const classes = {
    scheduled: 'status-scheduled',
    completed: 'status-completed',
    cancelled: 'status-cancelled'
  }
  return classes[status]
}

const filteredBookings = computed(() => {
  return bookings.value.filter(booking => {
    const matchesSearch =
      booking.customerName.includes(searchQuery.value) ||
      booking.service.includes(searchQuery.value)
    const matchesStatus =
      filterStatus.value === 'all' || booking.status === filterStatus.value
    return matchesSearch && matchesStatus
  })
})

const totalPages = computed(() => {
  return Math.ceil(filteredBookings.value.length / itemsPerPage)
})

const paginatedBookings = computed(() => {
  const startIndex = (currentPage.value - 1) * itemsPerPage
  return filteredBookings.value.slice(startIndex, startIndex + itemsPerPage)
})

const formatNumber = (num: number) => {
  return convertEnglishToPersianNumbers(num.toString())
}

const previousPage = () => {
  if (currentPage.value > 1) {
    currentPage.value--
  }
}

const nextPage = () => {
  if (currentPage.value < totalPages.value) {
    currentPage.value++
  }
}
</script>

<style scoped lang="scss">
.booking-list-card {
  background: white;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.card-content {
  padding: 24px;
}

.card-title {
  font-size: 18px;
  font-weight: 600;
  margin: 0 0 24px 0;
  color: #1f2937;
}

/* Filters */
.filters {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 24px;
}

@media (min-width: 768px) {
  .filters {
    flex-direction: row;
  }
}

.search-box {
  position: relative;
  flex: 1;
}

.search-icon {
  position: absolute;
  right: 12px;
  top: 50%;
  transform: translateY(-50%);
  width: 16px;
  height: 16px;
  color: #9ca3af;
}

.search-input {
  width: 100%;
  padding: 8px 16px 8px 40px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 14px;

  &:focus {
    outline: none;
    ring: 2px;
    ring-color: #6366f1;
    border-color: transparent;
  }
}

.filter-select {
  width: 100%;
  padding: 8px 16px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 14px;
  background: white;
  cursor: pointer;

  &:focus {
    outline: none;
    ring: 2px;
    ring-color: #6366f1;
    border-color: transparent;
  }
}

@media (min-width: 768px) {
  .filter-select {
    width: 180px;
  }
}

/* Table */
.table-wrapper {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  overflow: hidden;
}

.booking-table {
  width: 100%;
  border-collapse: collapse;

  thead {
    background: #f9fafb;

    tr th {
      text-align: right;
      padding: 12px 16px;
      font-size: 14px;
      font-weight: 500;
      color: #374151;
    }
  }

  tbody {
    tr {
      border-top: 1px solid #e5e7eb;
      transition: background-color 0.2s;

      &.data-row:hover {
        background: #f9fafb;
      }

      &.empty-row td {
        text-align: center;
        padding: 32px;
        color: #6b7280;
      }

      td {
        padding: 12px 16px;
        font-size: 14px;
        color: #1f2937;
      }
    }
  }
}

/* Status Badge */
.status-badge {
  display: inline-flex;
  padding: 4px 8px;
  font-size: 12px;
  font-weight: 500;
  border-radius: 4px;
  border: 1px solid;
}

.status-scheduled {
  background: #fef3c7;
  color: #92400e;
  border-color: #fde68a;
}

.status-completed {
  background: #d1fae5;
  color: #065f46;
  border-color: #a7f3d0;
}

.status-cancelled {
  background: #fee2e2;
  color: #991b1b;
  border-color: #fecaca;
}

/* Pagination */
.pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 16px;
}

.pagination-info {
  font-size: 14px;
  color: #4b5563;
  margin: 0;
}

.pagination-buttons {
  display: flex;
  gap: 8px;
}

.pagination-btn {
  padding: 4px 12px;
  border: 1px solid #d1d5db;
  border-radius: 4px;
  background: white;
  cursor: pointer;
  transition: background-color 0.2s;

  &:hover:not(:disabled) {
    background: #f9fafb;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .btn-icon {
    width: 16px;
    height: 16px;
  }
}

/* Loading & Error States */
.loading-container,
.error-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  text-align: center;
  color: #6b7280;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #e5e7eb;
  border-top-color: #6366f1;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 16px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.retry-btn {
  margin-top: 16px;
  padding: 8px 16px;
  background: #6366f1;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.2s;

  &:hover {
    background: #4f46e5;
  }
}
</style>
