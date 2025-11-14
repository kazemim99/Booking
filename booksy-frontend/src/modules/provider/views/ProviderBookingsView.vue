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
  background: linear-gradient(135deg, #f6f8fb 0%, #fafbfd 100%);
  min-height: 100vh;
}

/* Page Header */
.page-header {
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.95) 0%, rgba(255, 255, 255, 0.9) 100%);
  backdrop-filter: blur(20px);
  border-radius: 20px;
  padding: 32px;
  margin-bottom: 24px;
  box-shadow:
    0 4px 20px rgba(0, 0, 0, 0.06),
    0 1px 3px rgba(0, 0, 0, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.8);
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
  border-radius: 12px;
  font-size: 14px;
  font-weight: 600;
  border: none;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  overflow: hidden;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);
}

.btn-primary::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.2) 0%, rgba(255, 255, 255, 0) 100%);
  opacity: 0;
  transition: opacity 0.3s;
}

.btn-primary:hover {
  transform: translateY(-3px);
  box-shadow: 0 8px 25px rgba(102, 126, 234, 0.45);
}

.btn-primary:hover::before {
  opacity: 1;
}

.btn-primary:active {
  transform: translateY(-1px);
  box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);
}

.btn-secondary {
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.9) 0%, rgba(249, 250, 251, 0.9) 100%);
  color: #374151;
  border: 1px solid rgba(229, 231, 235, 0.8);
  backdrop-filter: blur(10px);
}

.btn-secondary:hover {
  background: linear-gradient(135deg, rgba(255, 255, 255, 1) 0%, rgba(243, 244, 246, 1) 100%);
  border-color: #d1d5db;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
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
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.95) 0%, rgba(255, 255, 255, 0.85) 100%);
  backdrop-filter: blur(20px);
  border-radius: 20px;
  padding: 28px;
  display: flex;
  align-items: center;
  gap: 20px;
  box-shadow:
    0 4px 20px rgba(0, 0, 0, 0.06),
    0 1px 3px rgba(0, 0, 0, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.8);
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  overflow: hidden;
}

.stat-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.03) 0%, rgba(118, 75, 162, 0.03) 100%);
  opacity: 0;
  transition: opacity 0.4s;
}

.stat-card:hover {
  transform: translateY(-8px) scale(1.02);
  box-shadow:
    0 12px 40px rgba(0, 0, 0, 0.12),
    0 4px 12px rgba(0, 0, 0, 0.08);
  border-color: rgba(102, 126, 234, 0.3);
}

.stat-card:hover::before {
  opacity: 1;
}

.stat-icon {
  width: 64px;
  height: 64px;
  border-radius: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.stat-card:hover .stat-icon {
  transform: scale(1.1) rotate(5deg);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.2);
}

.stat-icon svg {
  width: 32px;
  height: 32px;
  color: white;
  position: relative;
  z-index: 1;
}

.stat-today .stat-icon {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.stat-upcoming .stat-icon {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
}

.stat-completed .stat-icon {
  background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
}

.stat-revenue .stat-icon {
  background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%);
}

.stat-content {
  flex: 1;
  position: relative;
  z-index: 1;
}

.stat-value {
  font-size: 32px;
  font-weight: 800;
  background: linear-gradient(135deg, #1f2937 0%, #374151 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin: 0 0 6px;
  transition: all 0.3s;
}

.stat-card:hover .stat-value {
  transform: scale(1.05);
}

.stat-label {
  font-size: 14px;
  color: #6b7280;
  font-weight: 500;
  letter-spacing: 0.3px;
}

/* Bookings Container */
.bookings-container {
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.95) 0%, rgba(255, 255, 255, 0.9) 100%);
  backdrop-filter: blur(20px);
  border-radius: 24px;
  box-shadow:
    0 8px 32px rgba(0, 0, 0, 0.08),
    0 2px 8px rgba(0, 0, 0, 0.06);
  border: 1px solid rgba(255, 255, 255, 0.8);
  overflow: hidden;
}

/* Tabs */
.tabs-section {
  border-bottom: 1px solid rgba(229, 231, 235, 0.5);
  background: linear-gradient(135deg, rgba(249, 250, 251, 0.6) 0%, rgba(255, 255, 255, 0.6) 100%);
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
  padding: 14px 24px;
  border: none;
  background: transparent;
  color: #6b7280;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  border-bottom: 3px solid transparent;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  white-space: nowrap;
  position: relative;
}

.tab-button::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.08) 0%, rgba(139, 92, 246, 0.08) 100%);
  opacity: 0;
  transition: opacity 0.3s;
  border-radius: 12px 12px 0 0;
}

.tab-button:hover::before {
  opacity: 1;
}

.tab-button:hover {
  color: #4b5563;
  transform: translateY(-2px);
}

.tab-button.active {
  color: #6366f1;
  border-bottom-color: #6366f1;
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.05) 0%, rgba(139, 92, 246, 0.05) 100%);
}

.tab-badge {
  background: linear-gradient(135deg, #e5e7eb 0%, #f3f4f6 100%);
  color: #374151;
  padding: 4px 10px;
  border-radius: 16px;
  font-size: 11px;
  font-weight: 700;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  transition: all 0.3s;
}

.tab-button.active .tab-badge {
  background: linear-gradient(135deg, #eef2ff 0%, #e0e7ff 100%);
  color: #6366f1;
  box-shadow: 0 2px 8px rgba(99, 102, 241, 0.2);
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
  padding: 12px 16px 12px 44px;
  border: 1px solid rgba(229, 231, 235, 0.8);
  border-radius: 12px;
  font-size: 14px;
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.9) 0%, rgba(249, 250, 251, 0.9) 100%);
  backdrop-filter: blur(10px);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.search-input::placeholder {
  color: #9ca3af;
}

.search-input:focus {
  outline: none;
  border-color: #6366f1;
  background: white;
  box-shadow: 0 4px 20px rgba(99, 102, 241, 0.15);
  transform: translateY(-2px);
}

.filter-group {
  display: flex;
  gap: 12px;
}

.filter-select {
  padding: 12px 16px;
  border: 1px solid rgba(229, 231, 235, 0.8);
  border-radius: 12px;
  font-size: 14px;
  font-weight: 500;
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.9) 0%, rgba(249, 250, 251, 0.9) 100%);
  backdrop-filter: blur(10px);
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: #374151;
}

.filter-select:hover {
  border-color: #d1d5db;
  transform: translateY(-2px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.filter-select:focus {
  outline: none;
  border-color: #6366f1;
  background: white;
  box-shadow: 0 4px 20px rgba(99, 102, 241, 0.15);
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
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.98) 0%, rgba(250, 251, 252, 0.98) 100%);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(229, 231, 235, 0.6);
  border-radius: 16px;
  padding: 24px;
  cursor: pointer;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  overflow: hidden;
}

.booking-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.03) 0%, rgba(118, 75, 162, 0.03) 100%);
  opacity: 0;
  transition: opacity 0.4s;
  pointer-events: none;
}

.booking-card:hover {
  border-color: rgba(99, 102, 241, 0.5);
  box-shadow:
    0 12px 40px rgba(99, 102, 241, 0.12),
    0 4px 12px rgba(0, 0, 0, 0.08);
  transform: translateY(-4px) scale(1.01);
}

.booking-card:hover::before {
  opacity: 1;
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
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 18px;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  z-index: 1;
}

.booking-card:hover .customer-avatar {
  transform: scale(1.1);
  box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
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
  padding: 8px 16px;
  border-radius: 24px;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.3px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  z-index: 1;
}

.booking-card:hover .status-badge {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
}

.status-pending {
  background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
  color: #92400e;
}

.status-confirmed {
  background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
  color: #1e40af;
}

.status-completed {
  background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
  color: #065f46;
}

.status-cancelled {
  background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);
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
  padding: 10px 18px;
  border: 2px solid;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  z-index: 1;
  overflow: hidden;
}

.action-btn::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  opacity: 0;
  transition: opacity 0.3s;
  z-index: -1;
}

.action-btn:hover::before {
  opacity: 1;
}

.action-btn:active {
  transform: scale(0.95);
}

.action-btn svg {
  width: 16px;
  height: 16px;
  transition: transform 0.3s;
}

.action-btn:hover svg {
  transform: scale(1.1);
}

.btn-confirm {
  background: white;
  border-color: #22c55e;
  color: #16a34a;
}

.btn-confirm::before {
  background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%);
}

.btn-confirm:hover {
  border-color: #16a34a;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(34, 197, 94, 0.2);
}

.btn-complete {
  background: white;
  border-color: #3b82f6;
  color: #2563eb;
}

.btn-complete::before {
  background: linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%);
}

.btn-complete:hover {
  border-color: #2563eb;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.2);
}

.btn-reschedule {
  background: white;
  border-color: #f59e0b;
  color: #d97706;
}

.btn-reschedule::before {
  background: linear-gradient(135deg, #fffbeb 0%, #fef3c7 100%);
}

.btn-reschedule:hover {
  border-color: #d97706;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(245, 158, 11, 0.2);
}

.btn-cancel {
  background: white;
  border-color: #ef4444;
  color: #dc2626;
}

.btn-cancel::before {
  background: linear-gradient(135deg, #fef2f2 0%, #fee2e2 100%);
}

.btn-cancel:hover {
  border-color: #dc2626;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.2);
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
  width: 56px;
  height: 56px;
  border: 4px solid rgba(229, 231, 235, 0.3);
  border-top-color: #6366f1;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 20px;
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.1);
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
  width: 96px;
  height: 96px;
  border-radius: 50%;
  background: linear-gradient(135deg, #f3f4f6 0%, #e5e7eb 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 24px;
  box-shadow:
    0 4px 20px rgba(0, 0, 0, 0.06),
    inset 0 2px 4px rgba(255, 255, 255, 0.5);
  transition: all 0.3s;
}

.empty-icon:hover {
  transform: scale(1.05);
}

.empty-icon svg {
  width: 48px;
  height: 48px;
  color: #9ca3af;
}

.empty-state h3 {
  font-size: 20px;
  font-weight: 700;
  background: linear-gradient(135deg, #1f2937 0%, #4b5563 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin: 0 0 10px;
}

.empty-state p {
  font-size: 15px;
  color: #6b7280;
  font-weight: 500;
  margin: 0 0 32px;
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
