<template>
  <DashboardLayout>
    <div class="bookings-page">
      <!-- Page Header -->
      <div class="page-header">
        <div class="header-content">
          <div class="header-text">
            <h1 class="page-title">مدیریت رزروها</h1>
            <p class="page-subtitle">مشاهده و مدیریت تمام رزروهای خود</p>
          </div>
          <div class="header-actions">
            <button class="btn-secondary" @click="toggleCalendarView">
              <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <span>نمای تقویم</span>
            </button>
            <button class="btn-primary" @click="showCreateBooking = true">
              <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              <span>رزرو جدید</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Quick Stats -->
      <div class="quick-stats">
        <div class="stat-card stat-today">
          <div class="stat-icon">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ formatNumber(todayBookings) }}</div>
            <div class="stat-label">رزروهای امروز</div>
          </div>
        </div>

        <div class="stat-card stat-upcoming">
          <div class="stat-icon">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ formatNumber(upcomingBookings) }}</div>
            <div class="stat-label">رزروهای آینده</div>
          </div>
        </div>

        <div class="stat-card stat-completed">
          <div class="stat-icon">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ formatNumber(completedBookings) }}</div>
            <div class="stat-label">انجام شده این ماه</div>
          </div>
        </div>

        <div class="stat-card stat-revenue">
          <div class="stat-icon">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ formatCurrency(monthlyRevenue) }}</div>
            <div class="stat-label">درآمد این ماه</div>
          </div>
        </div>
      </div>

      <!-- Tabs & Filters -->
      <div class="bookings-container">
        <div class="tabs-section">
          <div class="tabs-header">
            <button
              v-for="tab in tabs"
              :key="tab.id"
              @click="activeTab = tab.id"
              :class="['tab-button', { active: activeTab === tab.id }]"
            >
              <span class="tab-label">{{ tab.label }}</span>
              <span v-if="tab.count" class="tab-badge">{{ formatNumber(tab.count) }}</span>
            </button>
          </div>

          <div class="filters-section">
            <div class="search-box">
              <svg class="search-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <input
                v-model="searchQuery"
                type="text"
                placeholder="جستجو بر اساس نام، تلفن یا خدمت..."
                class="search-input"
              />
            </div>

            <div class="filter-group">
              <select v-model="filterDate" class="filter-select">
                <option value="all">همه تاریخ‌ها</option>
                <option value="today">امروز</option>
                <option value="tomorrow">فردا</option>
                <option value="week">این هفته</option>
                <option value="month">این ماه</option>
              </select>

              <select v-model="filterService" class="filter-select">
                <option value="all">همه خدمات</option>
                <option value="service1">خدمت ۱</option>
                <option value="service2">خدمت ۲</option>
              </select>

              <button class="btn-filter" :class="{ active: hasActiveFilters }" @click="resetFilters">
                <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
                </svg>
              </button>
            </div>
          </div>
        </div>

        <!-- Bookings List -->
        <div v-if="loading" class="loading-state">
          <div class="spinner"></div>
          <p>در حال بارگذاری رزروها...</p>
        </div>

        <div v-else-if="filteredBookings.length === 0" class="empty-state">
          <div class="empty-icon">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
          <h3>هیچ رزروی یافت نشد</h3>
          <p>در حال حاضر رزروی برای نمایش وجود ندارد</p>
          <button class="btn-primary" @click="showCreateBooking = true">
            ایجاد رزرو جدید
          </button>
        </div>

        <div v-else class="bookings-list">
          <div
            v-for="booking in filteredBookings"
            :key="booking.id"
            class="booking-card"
            @click="selectBooking(booking)"
          >
            <div class="booking-header">
              <div class="booking-customer">
                <div class="customer-avatar">{{ getInitials(booking.customerName) }}</div>
                <div class="customer-info">
                  <h4 class="customer-name">{{ booking.customerName }}</h4>
                  <p class="customer-phone">{{ booking.customerPhone || 'بدون شماره' }}</p>
                </div>
              </div>
              <div class="booking-status">
                <span :class="['status-badge', getStatusClass(booking.status)]">
                  {{ getStatusLabel(booking.status) }}
                </span>
              </div>
            </div>

            <div class="booking-details">
              <div class="detail-item">
                <svg class="detail-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                <span>{{ booking.date }}</span>
              </div>
              <div class="detail-item">
                <svg class="detail-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>{{ booking.time }}</span>
              </div>
              <div class="detail-item">
                <svg class="detail-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                <span>{{ booking.service }}</span>
              </div>
              <div v-if="booking.price" class="detail-item">
                <svg class="detail-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>{{ formatCurrency(booking.price) }} تومان</span>
              </div>
            </div>

            <div class="booking-actions">
              <button
                v-if="canConfirm(booking)"
                @click.stop="confirmBooking(booking.id)"
                class="action-btn btn-confirm"
              >
                <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
                تایید
              </button>
              <button
                v-if="canComplete(booking)"
                @click.stop="completeBooking(booking.id)"
                class="action-btn btn-complete"
              >
                <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                انجام شد
              </button>
              <button
                v-if="canReschedule(booking)"
                @click.stop="rescheduleBooking(booking.id)"
                class="action-btn btn-reschedule"
              >
                <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                زمان‌بندی مجدد
              </button>
              <button
                v-if="canCancel(booking)"
                @click.stop="cancelBooking(booking.id)"
                class="action-btn btn-cancel"
              >
                <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
                لغو
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useProviderStore } from '../stores/provider.store'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'
import DashboardLayout from '../components/dashboard/DashboardLayout.vue'

const providerStore = useProviderStore()

const currentProvider = computed(() => providerStore.currentProvider)

// Sample data - replace with actual API calls
const bookings = ref([
  {
    id: '1',
    customerName: 'علی احمدی',
    customerPhone: '۰۹۱۲۳۴۵۶۷۸۹',
    date: '۱۴۰۲/۱۲/۱۵',
    time: '۱۰:۰۰',
    service: 'کوتاهی مو',
    price: 150000,
    status: 'pending',
  },
  {
    id: '2',
    customerName: 'سارا محمدی',
    customerPhone: '۰۹۳۸۷۶۵۴۳۲۱',
    date: '۱۴۰۲/۱۲/۱۵',
    time: '۱۱:۳۰',
    service: 'رنگ مو',
    price: 350000,
    status: 'confirmed',
  },
  {
    id: '3',
    customerName: 'محمد رضایی',
    customerPhone: '۰۹۱۲۷۶۵۴۳۲۱',
    date: '۱۴۰۲/۱۲/۱۴',
    time: '۱۴:۰۰',
    service: 'اصلاح صورت',
    price: 80000,
    status: 'completed',
  },
])

const loading = ref(false)
const activeTab = ref('all')
const searchQuery = ref('')
const filterDate = ref('all')
const filterService = ref('all')
const showCreateBooking = ref(false)

// Computed stats
const todayBookings = computed(() => 8)
const upcomingBookings = computed(() => 15)
const completedBookings = computed(() => 142)
const monthlyRevenue = computed(() => 12500000)

const tabs = computed(() => [
  { id: 'all', label: 'همه', count: bookings.value.length },
  { id: 'pending', label: 'در انتظار تایید', count: bookings.value.filter(b => b.status === 'pending').length },
  { id: 'confirmed', label: 'تایید شده', count: bookings.value.filter(b => b.status === 'confirmed').length },
  { id: 'completed', label: 'انجام شده', count: bookings.value.filter(b => b.status === 'completed').length },
  { id: 'cancelled', label: 'لغو شده', count: bookings.value.filter(b => b.status === 'cancelled').length },
])

const filteredBookings = computed(() => {
  return bookings.value.filter(booking => {
    const matchesTab = activeTab.value === 'all' || booking.status === activeTab.value
    const matchesSearch =
      searchQuery.value === '' ||
      booking.customerName.includes(searchQuery.value) ||
      booking.service.includes(searchQuery.value)
    return matchesTab && matchesSearch
  })
})

const hasActiveFilters = computed(() => {
  return filterDate.value !== 'all' || filterService.value !== 'all'
})

// Methods
const formatNumber = (num: number) => {
  return convertEnglishToPersianNumbers(num.toString())
}

const formatCurrency = (amount: number) => {
  return convertEnglishToPersianNumbers(amount.toLocaleString('fa-IR'))
}

const getInitials = (name: string) => {
  const parts = name.split(' ')
  if (parts.length >= 2) {
    return parts[0][0] + parts[1][0]
  }
  return name.substring(0, 2)
}

const getStatusClass = (status: string) => {
  const classes: Record<string, string> = {
    pending: 'status-pending',
    confirmed: 'status-confirmed',
    completed: 'status-completed',
    cancelled: 'status-cancelled',
  }
  return classes[status] || 'status-pending'
}

const getStatusLabel = (status: string) => {
  const labels: Record<string, string> = {
    pending: 'در انتظار تایید',
    confirmed: 'تایید شده',
    completed: 'انجام شده',
    cancelled: 'لغو شده',
  }
  return labels[status] || status
}

const canConfirm = (booking: any) => booking.status === 'pending'
const canComplete = (booking: any) => booking.status === 'confirmed'
const canReschedule = (booking: any) => ['pending', 'confirmed'].includes(booking.status)
const canCancel = (booking: any) => ['pending', 'confirmed'].includes(booking.status)

const confirmBooking = (id: string) => {
  console.log('Confirm booking:', id)
}

const completeBooking = (id: string) => {
  console.log('Complete booking:', id)
}

const rescheduleBooking = (id: string) => {
  console.log('Reschedule booking:', id)
}

const cancelBooking = (id: string) => {
  console.log('Cancel booking:', id)
}

const selectBooking = (booking: any) => {
  console.log('Selected booking:', booking)
}

const toggleCalendarView = () => {
  console.log('Toggle calendar view')
}

const resetFilters = () => {
  filterDate.value = 'all'
  filterService.value = 'all'
}

onMounted(async () => {
  try {
    if (!currentProvider.value) {
      await providerStore.loadCurrentProvider()
    }
  } catch (error) {
    console.error('Failed to load provider data:', error)
  }
})
</script>

<style scoped>
.bookings-page {
  padding: 0;
  background: #f8f9fa;
  min-height: 100vh;
}

/* Page Header */
.page-header {
  background: white;
  border-radius: 16px;
  padding: 32px;
  margin-bottom: 24px;
  border: 1px solid #e9ecef;
}

.header-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  flex-wrap: wrap;
}

.header-text {
  flex: 1;
}

.page-title {
  font-size: 24px;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 4px;
}

.page-subtitle {
  font-size: 14px;
  color: #6b7280;
  margin: 0;
}

.header-actions {
  display: flex;
  gap: 12px;
}

.btn-primary, .btn-secondary {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 24px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
}

.btn-primary {
  background: #6366f1;
  color: white;
}

.btn-primary:hover {
  background: #4f46e5;
}

.btn-primary:active {
  background: #4338ca;
}

.btn-secondary {
  background: white;
  color: #374151;
  border: 1px solid #d1d5db;
}

.btn-secondary:hover {
  background: #f9fafb;
  border-color: #9ca3af;
}

.btn-icon {
  width: 18px;
  height: 18px;
}

/* Quick Stats */
.quick-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 24px;
  margin-bottom: 32px;
}

.stat-card {
  background: white;
  border-radius: 12px;
  padding: 24px;
  display: flex;
  align-items: center;
  gap: 16px;
  border: 1px solid #e5e7eb;
  transition: all 0.2s;
}

.stat-card:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.stat-icon {
  width: 56px;
  height: 56px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-icon svg {
  width: 28px;
  height: 28px;
  color: white;
}

.stat-today .stat-icon {
  background: #6366f1;
}

.stat-upcoming .stat-icon {
  background: #ec4899;
}

.stat-completed .stat-icon {
  background: #10b981;
}

.stat-revenue .stat-icon {
  background: #f59e0b;
}

.stat-content {
  flex: 1;
}

.stat-value {
  font-size: 28px;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 4px;
}

.stat-label {
  font-size: 14px;
  color: #6b7280;
  font-weight: 500;
}

/* Bookings Container */
.bookings-container {
  background: white;
  border-radius: 12px;
  border: 1px solid #e5e7eb;
  overflow: hidden;
}

/* Tabs */
.tabs-section {
  border-bottom: 1px solid #e5e7eb;
  background: #f9fafb;
}

.tabs-header {
  display: flex;
  gap: 4px;
  padding: 20px 20px 0;
  overflow-x: auto;
}

.tab-button {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 14px 20px;
  border: none;
  background: transparent;
  color: #6b7280;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  transition: all 0.2s;
  white-space: nowrap;
}

.tab-button:hover {
  color: #4b5563;
}

.tab-button.active {
  color: #6366f1;
  border-bottom-color: #6366f1;
}

.tab-badge {
  background: #e5e7eb;
  color: #374151;
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 600;
}

.tab-button.active .tab-badge {
  background: #eef2ff;
  color: #6366f1;
}

/* Filters */
.filters-section {
  display: flex;
  gap: 12px;
  padding: 20px;
  flex-wrap: wrap;
}

.search-box {
  position: relative;
  flex: 1;
  min-width: 250px;
}

.search-icon {
  position: absolute;
  right: 12px;
  top: 50%;
  transform: translateY(-50%);
  width: 18px;
  height: 18px;
  color: #9ca3af;
  pointer-events: none;
}

.search-input {
  width: 100%;
  padding: 10px 16px 10px 44px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 14px;
  background: white;
  transition: all 0.2s;
}

.search-input::placeholder {
  color: #9ca3af;
}

.search-input:focus {
  outline: none;
  border-color: #6366f1;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
}

.filter-group {
  display: flex;
  gap: 12px;
}

.filter-select {
  padding: 10px 16px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  background: white;
  cursor: pointer;
  transition: all 0.2s;
  color: #374151;
}

.filter-select:hover {
  border-color: #9ca3af;
}

.filter-select:focus {
  outline: none;
  border-color: #6366f1;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
}

.btn-filter {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: white;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-filter:hover {
  border-color: #6366f1;
  background: #f3f4f6;
}

.btn-filter.active {
  background: #eef2ff;
  border-color: #6366f1;
}

.btn-filter svg {
  width: 20px;
  height: 20px;
  color: #6b7280;
}

/* Bookings List */
.bookings-list {
  padding: 20px;
  display: grid;
  gap: 16px;
}

.booking-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 20px;
  cursor: pointer;
  transition: all 0.2s;
}

.booking-card:hover {
  border-color: #d1d5db;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.booking-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.booking-customer {
  display: flex;
  align-items: center;
  gap: 12px;
}

.customer-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: #6366f1;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 16px;
}

.customer-info {
  flex: 1;
}

.customer-name {
  font-size: 16px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 4px;
}

.customer-phone {
  font-size: 13px;
  color: #6b7280;
  margin: 0;
  direction: ltr;
  text-align: right;
}

.booking-status {
}

.status-badge {
  display: inline-flex;
  align-items: center;
  padding: 6px 12px;
  border-radius: 16px;
  font-size: 12px;
  font-weight: 600;
}

.status-pending {
  background: #fef3c7;
  color: #92400e;
}

.status-confirmed {
  background: #dbeafe;
  color: #1e40af;
}

.status-completed {
  background: #d1fae5;
  color: #065f46;
}

.status-cancelled {
  background: #fee2e2;
  color: #991b1b;
}

.booking-details {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
  margin-bottom: 16px;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  color: #374151;
}

.detail-icon {
  width: 16px;
  height: 16px;
  color: #9ca3af;
  flex-shrink: 0;
}

.booking-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.action-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  border: 1px solid;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  background: white;
}

.action-btn svg {
  width: 16px;
  height: 16px;
}

.btn-confirm {
  border-color: #22c55e;
  color: #16a34a;
}

.btn-confirm:hover {
  background: #f0fdf4;
}

.btn-complete {
  border-color: #3b82f6;
  color: #2563eb;
}

.btn-complete:hover {
  background: #eff6ff;
}

.btn-reschedule {
  border-color: #f59e0b;
  color: #d97706;
}

.btn-reschedule:hover {
  background: #fffbeb;
}

.btn-cancel {
  border-color: #ef4444;
  color: #dc2626;
}

.btn-cancel:hover {
  background: #fef2f2;
}

/* Loading State */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 80px 20px;
  text-align: center;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 3px solid #e5e7eb;
  border-top-color: #6366f1;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 16px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.loading-state p {
  color: #6b7280;
  font-size: 15px;
  font-weight: 500;
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 80px 20px;
  text-align: center;
}

.empty-icon {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  background: #f3f4f6;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 20px;
}

.empty-icon svg {
  width: 40px;
  height: 40px;
  color: #9ca3af;
}

.empty-state h3 {
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 8px;
}

.empty-state p {
  font-size: 14px;
  color: #6b7280;
  margin: 0 0 24px;
  max-width: 400px;
}

/* Responsive */
@media (max-width: 768px) {
  .page-header {
    padding: 16px;
  }

  .header-content {
    flex-direction: column;
    align-items: stretch;
  }

  .header-actions {
    flex-direction: column;
  }

  .quick-stats {
    grid-template-columns: 1fr;
  }

  .tabs-header {
    padding: 16px 16px 0;
  }

  .filters-section {
    padding: 16px;
    flex-direction: column;
  }

  .search-box {
    min-width: 100%;
  }

  .filter-group {
    flex-direction: column;
  }

  .filter-select, .btn-filter {
    width: 100%;
  }

  .bookings-list {
    padding: 16px;
  }

  .booking-details {
    grid-template-columns: 1fr;
  }
}
</style>
