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

      <!-- Table -->
      <div class="table-wrapper">
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
import { ref, computed } from 'vue'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'

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
  bookings?: Booking[]
}

const props = withDefaults(defineProps<Props>(), {
  bookings: () => [
    {
      id: '1',
      customerName: 'علی احمدی',
      date: '۱۴۰۳/۰۸/۱۵',
      time: '۱۰:۳۰',
      service: 'کوتاهی مو',
      status: 'scheduled'
    },
    {
      id: '2',
      customerName: 'سارا محمدی',
      date: '۱۴۰۳/۰۸/۱۵',
      time: '۱۲:۰۰',
      service: 'رنگ مو',
      status: 'scheduled'
    },
    {
      id: '3',
      customerName: 'رضا کریمی',
      date: '۱۴۰۳/۰۸/۱۴',
      time: '۱۵:۳۰',
      service: 'اصلاح صورت',
      status: 'completed'
    },
    {
      id: '4',
      customerName: 'مریم رضایی',
      date: '۱۴۰۳/۰۸/۱۴',
      time: '۱۱:۰۰',
      service: 'مانیکور',
      status: 'completed'
    },
    {
      id: '5',
      customerName: 'حسین موسوی',
      date: '۱۴۰۳/۰۸/۱۳',
      time: '۱۶:۰۰',
      service: 'کوتاهی مو',
      status: 'cancelled'
    }
  ]
})

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

const statusColorClass = (status: BookingStatus): string => {
  const classes = {
    scheduled: 'status-scheduled',
    completed: 'status-completed',
    cancelled: 'status-cancelled'
  }
  return classes[status]
}

const filteredBookings = computed(() => {
  return props.bookings.filter(booking => {
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
</style>
