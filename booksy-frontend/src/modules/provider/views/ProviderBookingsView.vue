<template>
  <DashboardLayout>
    <!-- Toast Notifications -->
    <Toast ref="toastRef" />

    <div class="bookings-page">
      <!-- Page Header -->
      <div class="page-header">
        <div class="header-content">
          <div class="header-text">
            <h1 class="page-title">مدیریت رزروها</h1>
            <p class="page-subtitle">مشاهده و مدیریت تمام رزروهای خود</p>
          </div>
          <div class="header-actions">
            <button :class="['btn-secondary', { 'active': viewMode === 'calendar' }]" @click="toggleCalendarView">
              <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <span>{{ viewMode === 'calendar' ? 'نمای لیست' : 'نمای تقویم' }}</span>
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

        <!-- Calendar View -->
        <div v-if="viewMode === 'calendar'" class="calendar-view">
          <BookingCalendar
            :bookings="bookings"
            @booking-click="handleBookingClick"
          />
        </div>

        <!-- List View -->
        <div v-else>
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
        </div> <!-- End List View -->
      </div>
    </div>

    <!-- Create Booking Modal -->
    <CreateBookingModal
      v-model="showCreateBooking"
      :customers="customers"
      :services="services"
      @submit="handleNewBooking"
    />

    <!-- Reschedule Modal -->
    <Modal v-model="showRescheduleModal" title="زمان‌بندی مجدد رزرو" size="small">
      <div v-if="rescheduleBookingData" class="reschedule-modal">
        <div class="reschedule-info">
          <p><strong>مشتری:</strong> {{ rescheduleBookingData.customerName }}</p>
          <p><strong>خدمت:</strong> {{ rescheduleBookingData.service }}</p>
          <p><strong>تاریخ فعلی:</strong> {{ rescheduleBookingData.date }} - {{ rescheduleBookingData.time }}</p>
        </div>

        <div class="reschedule-picker">
          <label class="picker-label">تاریخ و زمان جدید</label>
          <VuePersianDatetimePicker
            v-model="newDateTime"
            placeholder="تاریخ و زمان جدید را انتخاب کنید"
            format="YYYY-MM-DD HH:mm"
            display-format="jYYYY/jMM/jDD - HH:mm"
            type="datetime"
            :min="new Date().toISOString()"
            auto-submit
            color="#1976d2"
            input-class="persian-datepicker-input"
          />
        </div>
      </div>

      <template #footer>
        <button class="btn-secondary" @click="cancelReschedule">انصراف</button>
        <button class="btn-primary" @click="confirmReschedule" :disabled="!newDateTime">
          ثبت تغییرات
        </button>
      </template>
    </Modal>

    <!-- Booking Details Modal -->
    <Modal v-model="showBookingDetails" title="جزئیات رزرو" size="medium">
      <div v-if="selectedBooking" class="booking-details-modal">
        <div class="detail-row">
          <div class="detail-label">نام مشتری:</div>
          <div class="detail-value">{{ selectedBooking.customerName }}</div>
        </div>
        <div class="detail-row">
          <div class="detail-label">شماره تماس:</div>
          <div class="detail-value">{{ selectedBooking.customerPhone }}</div>
        </div>
        <div class="detail-row">
          <div class="detail-label">خدمت:</div>
          <div class="detail-value">{{ selectedBooking.service }}</div>
        </div>
        <div class="detail-row">
          <div class="detail-label">تاریخ:</div>
          <div class="detail-value">{{ selectedBooking.date }}</div>
        </div>
        <div class="detail-row">
          <div class="detail-label">ساعت:</div>
          <div class="detail-value">{{ selectedBooking.time }}</div>
        </div>
        <div class="detail-row">
          <div class="detail-label">قیمت:</div>
          <div class="detail-value">{{ formatCurrency(selectedBooking.price) }} تومان</div>
        </div>
        <div class="detail-row">
          <div class="detail-label">وضعیت:</div>
          <div class="detail-value">
            <span :class="['status-badge', getStatusClass(selectedBooking.status)]">
              {{ getStatusLabel(selectedBooking.status) }}
            </span>
          </div>
        </div>
      </div>

      <template #footer>
        <button class="btn-secondary" @click="showBookingDetails = false">بستن</button>
        <button
          v-if="selectedBooking && canConfirm(selectedBooking)"
          class="btn-primary"
          @click="confirmBookingFromModal"
        >
          تایید رزرو
        </button>
      </template>
    </Modal>
  </DashboardLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useProviderStore } from '../stores/provider.store'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'
import { useToast, setToastInstance } from '@/shared/composables/useToast'
import DashboardLayout from '../components/dashboard/DashboardLayout.vue'
import Toast from '@/shared/components/Toast.vue'
import Modal from '@/shared/components/Modal.vue'
import VuePersianDatetimePicker from 'vue3-persian-datetime-picker'
import BookingCalendar from '../components/calendar/BookingCalendar.vue'
import CreateBookingModal from '../components/modals/CreateBookingModal.vue'

const toast = useToast()
const toastRef = ref()

const providerStore = useProviderStore()

const currentProvider = computed(() => providerStore.currentProvider)

// View mode: 'list' or 'calendar'
const viewMode = ref<'list' | 'calendar'>('list')

// Sample data - replace with actual API calls
const bookings = ref([
  {
    id: '1',
    customerName: 'علی احمدی',
    customerPhone: '۰۹۱۲۳۴۵۶۷۸۹',
    date: '2025-11-14',
    time: '۱۰:۰۰',
    service: 'کوتاهی مو',
    price: 150000,
    status: 'pending',
  },
  {
    id: '2',
    customerName: 'سارا محمدی',
    customerPhone: '۰۹۳۸۷۶۵۴۳۲۱',
    date: '2025-11-14',
    time: '۱۱:۳۰',
    service: 'رنگ مو',
    price: 350000,
    status: 'confirmed',
  },
  {
    id: '3',
    customerName: 'محمد رضایی',
    customerPhone: '۰۹۱۲۷۶۵۴۳۲۱',
    date: '2025-11-13',
    time: '۱۴:۰۰',
    service: 'اصلاح صورت',
    price: 80000,
    status: 'completed',
  },
  {
    id: '4',
    customerName: 'فاطمه کریمی',
    customerPhone: '۰۹۱۱۱۲۳۴۵۶۷',
    date: '2025-11-15',
    time: '۰۹:۰۰',
    service: 'مانیکور',
    price: 120000,
    status: 'confirmed',
  },
  {
    id: '5',
    customerName: 'حسین نوری',
    customerPhone: '۰۹۱۲۸۸۸۷۷۷۶',
    date: '2025-11-16',
    time: '۱۵:۰۰',
    service: 'کوتاهی مو',
    price: 150000,
    status: 'pending',
  },
])

// Sample customers
const customers = ref([
  { id: '1', name: 'علی احمدی', phone: '۰۹۱۲۳۴۵۶۷۸۹' },
  { id: '2', name: 'سارا محمدی', phone: '۰۹۳۸۷۶۵۴۳۲۱' },
  { id: '3', name: 'محمد رضایی', phone: '۰۹۱۲۷۶۵۴۳۲۱' },
  { id: '4', name: 'فاطمه کریمی', phone: '۰۹۱۱۱۲۳۴۵۶۷' },
  { id: '5', name: 'حسین نوری', phone: '۰۹۱۲۸۸۸۷۷۷۶' },
  { id: '6', name: 'زهرا حسینی', phone: '۰۹۱۳۱۱۱۲۲۲۳' },
  { id: '7', name: 'رضا مرادی', phone: '۰۹۱۴۲۲۲۳۳۳۴' },
])

// Sample services
const services = ref([
  { id: '1', name: 'کوتاهی مو', duration: 30, price: 150000 },
  { id: '2', name: 'رنگ مو', duration: 120, price: 350000 },
  { id: '3', name: 'اصلاح صورت', duration: 20, price: 80000 },
  { id: '4', name: 'مانیکور', duration: 45, price: 120000 },
  { id: '5', name: 'پدیکور', duration: 60, price: 150000 },
  { id: '6', name: 'ماساژ صورت', duration: 40, price: 200000 },
])

const loading = ref(false)
const activeTab = ref('all')
const searchQuery = ref('')
const filterDate = ref('all')
const filterService = ref('all')
const showCreateBooking = ref(false)
const showBookingDetails = ref(false)
const selectedBooking = ref<any>(null)
const showRescheduleModal = ref(false)
const rescheduleBookingData = ref<any>(null)
const newDateTime = ref('')

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
  const booking = bookings.value.find(b => b.id === id)
  if (booking) {
    booking.status = 'confirmed'
    toast.success(`رزرو ${booking.customerName} تایید شد`)
  }
}

const completeBooking = (id: string) => {
  const booking = bookings.value.find(b => b.id === id)
  if (booking) {
    booking.status = 'completed'
    toast.success(`رزرو ${booking.customerName} به عنوان انجام شده علامت گذاری شد`)
  }
}

const rescheduleBooking = (id: string) => {
  const booking = bookings.value.find(b => b.id === id)
  if (booking) {
    rescheduleBookingData.value = booking
    newDateTime.value = ''
    showRescheduleModal.value = true
  }
}

const confirmReschedule = () => {
  if (rescheduleBookingData.value && newDateTime.value) {
    // Parse the date/time from persian datepicker (format: YYYY-MM-DD HH:mm)
    const [datePart, timePart] = newDateTime.value.split(' ')
    const formattedDate = datePart.replace(/-/g, '/')
    const formattedTime = timePart

    // Update booking
    rescheduleBookingData.value.date = convertEnglishToPersianNumbers(formattedDate)
    rescheduleBookingData.value.time = convertEnglishToPersianNumbers(formattedTime)

    toast.success(`رزرو ${rescheduleBookingData.value.customerName} به ${convertEnglishToPersianNumbers(formattedDate)} ساعت ${convertEnglishToPersianNumbers(formattedTime)} منتقل شد`)

    // Close modal
    showRescheduleModal.value = false
    rescheduleBookingData.value = null
    newDateTime.value = ''
  }
}

const cancelReschedule = () => {
  showRescheduleModal.value = false
  rescheduleBookingData.value = null
  newDateTime.value = ''
}

const cancelBooking = (id: string) => {
  const booking = bookings.value.find(b => b.id === id)
  if (booking) {
    booking.status = 'cancelled'
    toast.warning(`رزرو ${booking.customerName} لغو شد`)
  }
}

const selectBooking = (booking: any) => {
  selectedBooking.value = booking
  showBookingDetails.value = true
}

const confirmBookingFromModal = () => {
  if (selectedBooking.value) {
    confirmBooking(selectedBooking.value.id)
    showBookingDetails.value = false
  }
}

const toggleCalendarView = () => {
  viewMode.value = viewMode.value === 'list' ? 'calendar' : 'list'
}

const handleNewBooking = (formData: any) => {
  // In production, this would make an API call
  const selectedService = services.value.find(s => s.id === formData.serviceId)
  const selectedCustomer = customers.value.find(c => c.id === formData.customerId)

  if (!selectedService || !selectedCustomer) return

  // Extract date and time from datetime string
  const dateTime = new Date(formData.dateTime)
  const date = dateTime.toISOString().split('T')[0]
  const hours = dateTime.getHours().toString().padStart(2, '0')
  const minutes = dateTime.getMinutes().toString().padStart(2, '0')
  const time = convertEnglishToPersianNumbers(`${hours}:${minutes}`)

  const newBooking = {
    id: (bookings.value.length + 1).toString(),
    customerName: selectedCustomer.name,
    customerPhone: selectedCustomer.phone,
    date,
    time,
    service: selectedService.name,
    price: selectedService.price,
    status: 'pending',
  }

  bookings.value.unshift(newBooking)
  toast.success('رزرو جدید با موفقیت ثبت شد')
}

const handleBookingClick = (booking: any) => {
  selectedBooking.value = booking
  showBookingDetails.value = true
}

const resetFilters = () => {
  filterDate.value = 'all'
  filterService.value = 'all'
}

onMounted(async () => {
  // Set toast instance
  if (toastRef.value) {
    setToastInstance(toastRef.value)
  }

  try {
    if (!currentProvider.value) {
      await providerStore.loadCurrentProvider()
    }
  } catch (error) {
    console.error('Failed to load provider data:', error)
    toast.error('خطا در بارگذاری اطلاعات ارائه‌دهنده')
  }
})
</script>

<style scoped>
.bookings-page {
  padding: 0;
  background: #fafafa;
  min-height: 100vh;
}

/* Page Header */
.page-header {
  background: white;
  border-radius: 4px;
  padding: 24px;
  margin-bottom: 24px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
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
  font-weight: 500;
  color: rgba(0, 0, 0, 0.87);
  margin: 0 0 4px;
  letter-spacing: 0.25px;
}

.page-subtitle {
  font-size: 14px;
  color: rgba(0, 0, 0, 0.6);
  margin: 0;
  letter-spacing: 0.25px;
}

.header-actions {
  display: flex;
  gap: 12px;
}

.btn-primary, .btn-secondary {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  border: none;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.btn-primary {
  background: #1976d2;
  color: white;
}

.btn-primary:hover {
  background: #1565c0;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.3);
}

.btn-primary:active {
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.btn-secondary {
  background: white;
  color: rgba(0, 0, 0, 0.87);
  border: 1px solid rgba(0, 0, 0, 0.23);
  box-shadow: none;
}

.btn-secondary:hover {
  background: rgba(0, 0, 0, 0.04);
  border-color: rgba(0, 0, 0, 0.23);
}

.btn-secondary.active {
  background: #1976d2;
  color: white;
  border-color: #1976d2;
}

.btn-secondary.active:hover {
  background: #1565c0;
  border-color: #1565c0;
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
  border-radius: 4px;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 16px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  transition: box-shadow 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.stat-card:hover {
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-icon svg {
  width: 24px;
  height: 24px;
  color: white;
}

.stat-today .stat-icon {
  background: #2196f3;
}

.stat-upcoming .stat-icon {
  background: #ff9800;
}

.stat-completed .stat-icon {
  background: #4caf50;
}

.stat-revenue .stat-icon {
  background: #9c27b0;
}

.stat-content {
  flex: 1;
}

.stat-value {
  font-size: 24px;
  font-weight: 400;
  color: rgba(0, 0, 0, 0.87);
  margin: 0 0 4px;
  letter-spacing: 0;
}

.stat-label {
  font-size: 14px;
  color: rgba(0, 0, 0, 0.6);
  font-weight: 400;
  letter-spacing: 0.25px;
}

/* Bookings Container */
.bookings-container {
  background: white;
  border-radius: 4px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  overflow: hidden;
}

/* Tabs */
.tabs-section {
  border-bottom: 1px solid rgba(0, 0, 0, 0.12);
  background: white;
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
  padding: 12px 16px;
  border: none;
  background: transparent;
  color: rgba(0, 0, 0, 0.6);
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  white-space: nowrap;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.tab-button:hover {
  color: rgba(0, 0, 0, 0.87);
  background: rgba(0, 0, 0, 0.04);
}

.tab-button.active {
  color: #1976d2;
  border-bottom-color: #1976d2;
}

.tab-badge {
  background: rgba(0, 0, 0, 0.12);
  color: rgba(0, 0, 0, 0.87);
  padding: 2px 8px;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 500;
  min-width: 20px;
  text-align: center;
}

.tab-button.active .tab-badge {
  background: #e3f2fd;
  color: #1976d2;
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
  border: 1px solid rgba(0, 0, 0, 0.23);
  border-radius: 4px;
  font-size: 14px;
  background: white;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: rgba(0, 0, 0, 0.87);
}

.search-input::placeholder {
  color: rgba(0, 0, 0, 0.38);
}

.search-input:hover {
  border-color: rgba(0, 0, 0, 0.87);
}

.search-input:focus {
  outline: none;
  border-color: #1976d2;
  border-width: 2px;
  padding: 9px 15px 9px 43px;
}

.filter-group {
  display: flex;
  gap: 12px;
}

.filter-select {
  padding: 10px 16px;
  border: 1px solid rgba(0, 0, 0, 0.23);
  border-radius: 4px;
  font-size: 14px;
  font-weight: 400;
  background: white;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: rgba(0, 0, 0, 0.87);
}

.filter-select:hover {
  border-color: rgba(0, 0, 0, 0.87);
}

.filter-select:focus {
  outline: none;
  border-color: #1976d2;
  border-width: 2px;
  padding: 9px 15px;
}

.btn-filter {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid rgba(0, 0, 0, 0.23);
  border-radius: 4px;
  background: white;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.btn-filter:hover {
  background: rgba(0, 0, 0, 0.04);
  border-color: rgba(0, 0, 0, 0.87);
}

.btn-filter.active {
  background: #e3f2fd;
  border-color: #1976d2;
}

.btn-filter svg {
  width: 20px;
  height: 20px;
  color: rgba(0, 0, 0, 0.6);
}

.btn-filter.active svg {
  color: #1976d2;
}

/* Bookings List */
.bookings-list {
  padding: 20px;
  display: grid;
  gap: 16px;
}

.booking-card {
  background: white;
  border-radius: 4px;
  padding: 16px;
  cursor: pointer;
  transition: box-shadow 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12);
}

.booking-card:hover {
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
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
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #1976d2;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 500;
  font-size: 16px;
}

.customer-info {
  flex: 1;
}

.customer-name {
  font-size: 16px;
  font-weight: 500;
  color: rgba(0, 0, 0, 0.87);
  margin: 0 0 4px;
  letter-spacing: 0.15px;
}

.customer-phone {
  font-size: 14px;
  color: rgba(0, 0, 0, 0.6);
  margin: 0;
  direction: ltr;
  text-align: right;
  letter-spacing: 0.25px;
}

.booking-status {
}

.status-badge {
  display: inline-flex;
  align-items: center;
  padding: 4px 12px;
  border-radius: 16px;
  font-size: 12px;
  font-weight: 500;
  letter-spacing: 0.25px;
}

.status-pending {
  background: #fff3e0;
  color: #e65100;
}

.status-confirmed {
  background: #e3f2fd;
  color: #0d47a1;
}

.status-completed {
  background: #e8f5e9;
  color: #1b5e20;
}

.status-cancelled {
  background: #ffebee;
  color: #b71c1c;
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
  color: rgba(0, 0, 0, 0.87);
  letter-spacing: 0.25px;
}

.detail-icon {
  width: 18px;
  height: 18px;
  color: rgba(0, 0, 0, 0.54);
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
  padding: 6px 16px;
  border: 1px solid;
  border-radius: 4px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  background: white;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.action-btn svg {
  width: 18px;
  height: 18px;
}

.btn-confirm {
  border-color: #4caf50;
  color: #2e7d32;
}

.btn-confirm:hover {
  background: #e8f5e9;
}

.btn-complete {
  border-color: #2196f3;
  color: #1565c0;
}

.btn-complete:hover {
  background: #e3f2fd;
}

.btn-reschedule {
  border-color: #ff9800;
  color: #e65100;
}

.btn-reschedule:hover {
  background: #fff3e0;
}

.btn-cancel {
  border-color: #f44336;
  color: #c62828;
}

.btn-cancel:hover {
  background: #ffebee;
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
  width: 40px;
  height: 40px;
  border: 3px solid rgba(0, 0, 0, 0.1);
  border-top-color: #1976d2;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 16px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.loading-state p {
  color: rgba(0, 0, 0, 0.6);
  font-size: 14px;
  font-weight: 400;
  letter-spacing: 0.25px;
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
  width: 72px;
  height: 72px;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.04);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 16px;
}

.empty-icon svg {
  width: 36px;
  height: 36px;
  color: rgba(0, 0, 0, 0.38);
}

.empty-state h3 {
  font-size: 20px;
  font-weight: 500;
  color: rgba(0, 0, 0, 0.87);
  margin: 0 0 8px;
  letter-spacing: 0.15px;
}

.empty-state p {
  font-size: 14px;
  color: rgba(0, 0, 0, 0.6);
  margin: 0 0 24px;
  max-width: 400px;
  letter-spacing: 0.25px;
}

/* Reschedule Modal */
.reschedule-modal {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.reschedule-info {
  background: rgba(0, 0, 0, 0.02);
  padding: 16px;
  border-radius: 4px;
}

.reschedule-info p {
  margin: 8px 0;
  font-size: 14px;
  color: rgba(0, 0, 0, 0.87);
  letter-spacing: 0.25px;
}

.reschedule-info strong {
  font-weight: 500;
  color: rgba(0, 0, 0, 0.6);
}

.reschedule-picker {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.picker-label {
  font-size: 14px;
  font-weight: 500;
  color: rgba(0, 0, 0, 0.6);
  letter-spacing: 0.25px;
}

/* Persian DatePicker Styling */
:deep(.persian-datepicker-input) {
  width: 100%;
  padding: 10px 16px;
  border: 1px solid rgba(0, 0, 0, 0.23);
  border-radius: 4px;
  font-size: 14px;
  background: white;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: rgba(0, 0, 0, 0.87);
  font-family: 'B Nazanin', Tahoma, Arial, sans-serif;
}

:deep(.persian-datepicker-input):hover {
  border-color: rgba(0, 0, 0, 0.87);
}

:deep(.persian-datepicker-input):focus {
  outline: none;
  border-color: #1976d2;
  border-width: 2px;
  padding: 9px 15px;
}

:deep(.vpd-input-group) {
  width: 100%;
}

/* Modal Booking Details */
.booking-details-modal {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.detail-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid rgba(0, 0, 0, 0.06);
}

.detail-row:last-child {
  border-bottom: none;
}

.detail-label {
  font-size: 14px;
  font-weight: 500;
  color: rgba(0, 0, 0, 0.6);
  letter-spacing: 0.25px;
}

.detail-value {
  font-size: 14px;
  font-weight: 400;
  color: rgba(0, 0, 0, 0.87);
  letter-spacing: 0.25px;
  text-align: left;
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

/* Calendar View */
.calendar-view {
  margin-top: 24px;
}
</style>
