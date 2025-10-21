<template>
  <div class="provider-dashboard">
    <!-- Welcome Header -->
    <WelcomeCard v-if="currentProvider" :provider="currentProvider" :show-onboarding="!isProfileComplete" />

    <!-- Provider Status Banner -->
    <div v-if="showStatusBanner" class="status-banner" :class="`status-${providerStatus?.toLowerCase()}`">
      <div class="status-banner-content">
        <div class="status-icon">
          <span v-if="providerStatus === ProviderStatus.PendingVerification">⏳</span>
          <span v-else-if="providerStatus === ProviderStatus.Inactive">⚠️</span>
          <span v-else-if="providerStatus === ProviderStatus.Suspended">🚫</span>
          <span v-else-if="providerStatus === ProviderStatus.Archived">📦</span>
        </div>
        <div class="status-text">
          <h3 class="status-title">{{ statusTitle }}</h3>
          <p class="status-message">{{ statusMessage }}</p>
        </div>
      </div>
    </div>

    <!-- Quick Stats Grid -->
    <div class="stats-grid">
      <QuickStatsCard
        title="Today's Bookings"
        :value="todayStats.bookings"
        icon="calendar"
        trend="+12%"
        variant="primary"
      />
      <QuickStatsCard
        title="This Week Revenue"
        :value="formatCurrency(todayStats.revenue)"
        icon="dollar"
        trend="+8%"
        variant="success"
      />
      <QuickStatsCard
        title="Pending Requests"
        :value="todayStats.pending"
        icon="clock"
        variant="warning"
      />
      <QuickStatsCard
        title="Average Rating"
        :value="todayStats.rating.toFixed(1)"
        icon="star"
        suffix="/ 5.0"
        variant="info"
      />
    </div>

    <!-- Charts Section -->
    <div class="charts-section">
      <div class="chart-card">
        <RevenueTrendChart
          title="Revenue Trend"
          subtitle="Last 30 days"
          :data="revenueData"
          :loading="isLoadingCharts"
          @range-change="handleRevenueRangeChange"
        />
      </div>
      <div class="chart-card">
        <BookingsTrendChart
          title="Bookings Overview"
          subtitle="Track your booking performance"
          :data="bookingsData"
          :loading="isLoadingCharts"
        />
      </div>
    </div>

    <!-- Main Content Grid -->
    <div class="dashboard-grid">
      <!-- Recent Bookings Card -->
      <div class="dashboard-card bookings-card">
        <div class="card-header">
          <h2 class="card-title">Recent Bookings</h2>
          <button class="view-all-button" @click="goToBookings">
            <span>View All</span>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </button>
        </div>
        <RecentBookingsCard :bookings="recentBookings" :loading="isLoadingBookings" @booking-click="handleBookingClick" />
      </div>

      <!-- Quick Actions Card -->
      <div class="dashboard-card actions-card">
        <div class="card-header">
          <h2 class="card-title">Quick Actions</h2>
        </div>
        <QuickActionsCard @action="handleQuickAction" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '../../stores/provider.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { ProviderStatus } from '../../types/provider.types'
import WelcomeCard from '../../components/dashboard/WelcomeCard.vue'
import QuickStatsCard from '../../components/dashboard/QuickStatsCard.vue'
import RecentBookingsCard from '../../components/dashboard/RecentBookingsCard.vue'
import QuickActionsCard from '../../components/dashboard/QuickActionsCard.vue'
import { RevenueTrendChart, BookingsTrendChart } from '../../components/charts'
import type { Appointment } from '@/modules/booking/types/booking.types'

const router = useRouter()
const providerStore = useProviderStore()
const authStore = useAuthStore()

const isLoadingBookings = ref(false)
const isLoadingCharts = ref(false)
const currentProvider = computed(() => providerStore.currentProvider)
const providerStatus = computed(() => authStore.providerStatus)

// Status banner computed properties
const showStatusBanner = computed(() => {
  const status = providerStatus.value
  return (
    status === ProviderStatus.PendingVerification ||
    status === ProviderStatus.Inactive ||
    status === ProviderStatus.Suspended ||
    status === ProviderStatus.Archived
  )
})

const statusTitle = computed(() => {
  switch (providerStatus.value) {
    case ProviderStatus.PendingVerification:
      return 'Account Pending Verification'
    case ProviderStatus.Inactive:
      return 'Account Inactive'
    case ProviderStatus.Suspended:
      return 'Account Suspended'
    case ProviderStatus.Archived:
      return 'Account Archived'
    default:
      return ''
  }
})

const statusMessage = computed(() => {
  switch (providerStatus.value) {
    case ProviderStatus.PendingVerification:
      return 'Your provider account is awaiting admin verification. You can view your dashboard but some features may be limited until verification is complete.'
    case ProviderStatus.Inactive:
      return 'Your provider account is currently inactive. Please contact support to reactivate your account.'
    case ProviderStatus.Suspended:
      return 'Your provider account has been temporarily suspended. Please contact support for more information.'
    case ProviderStatus.Archived:
      return 'Your provider account has been archived. Please contact support if you believe this is an error.'
    default:
      return ''
  }
})

// Mock stats data - Replace with real API calls
const todayStats = ref({
  bookings: 12,
  revenue: 1850,
  pending: 3,
  rating: 4.8,
})

// Mock revenue data - Replace with real API calls
const revenueData = ref([
  { date: '2024-01-01', amount: 850 },
  { date: '2024-01-02', amount: 920 },
  { date: '2024-01-03', amount: 1100 },
  { date: '2024-01-04', amount: 980 },
  { date: '2024-01-05', amount: 1250 },
  { date: '2024-01-06', amount: 1400 },
  { date: '2024-01-07', amount: 1320 },
  { date: '2024-01-08', amount: 1180 },
  { date: '2024-01-09', amount: 1450 },
  { date: '2024-01-10', amount: 1650 },
  { date: '2024-01-11', amount: 1520 },
  { date: '2024-01-12', amount: 1380 },
  { date: '2024-01-13', amount: 1280 },
  { date: '2024-01-14', amount: 1550 },
  { date: '2024-01-15', amount: 1720 },
])

// Mock bookings data - Replace with real API calls
const bookingsData = ref([
  { date: '2024-01-01', completed: 5, cancelled: 1, pending: 2 },
  { date: '2024-01-02', completed: 6, cancelled: 0, pending: 3 },
  { date: '2024-01-03', completed: 8, cancelled: 2, pending: 1 },
  { date: '2024-01-04', completed: 7, cancelled: 1, pending: 2 },
  { date: '2024-01-05', completed: 9, cancelled: 0, pending: 4 },
  { date: '2024-01-06', completed: 10, cancelled: 1, pending: 3 },
  { date: '2024-01-07', completed: 8, cancelled: 2, pending: 2 },
  { date: '2024-01-08', completed: 7, cancelled: 1, pending: 1 },
  { date: '2024-01-09', completed: 11, cancelled: 0, pending: 3 },
  { date: '2024-01-10', completed: 12, cancelled: 1, pending: 4 },
  { date: '2024-01-11', completed: 9, cancelled: 2, pending: 2 },
  { date: '2024-01-12', completed: 8, cancelled: 1, pending: 3 },
  { date: '2024-01-13', completed: 7, cancelled: 0, pending: 2 },
  { date: '2024-01-14', completed: 10, cancelled: 1, pending: 4 },
  { date: '2024-01-15', completed: 13, cancelled: 2, pending: 3 },
])

// Mock recent bookings - Replace with real API calls
const recentBookings = ref<Appointment[]>([])

const isProfileComplete = computed(() => {
  const provider = currentProvider.value
  if (!provider) return false

  return !!(
    provider.profile.businessName &&
    provider.profile.description &&
    provider.profile.logoUrl &&
    provider.businessHours &&
    provider.businessHours.length > 0 &&
    provider.services &&
    provider.services.length > 0
  )
})

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount)
}

const goToBookings = () => {
  router.push({ name: 'ProviderBookings' })
}

const handleQuickAction = (action: string) => {
  switch (action) {
    case 'add-service':
      router.push({ name: 'ProviderServices' })
      break
    case 'view-calendar':
      router.push({ name: 'ProviderBookings' })
      break
    case 'edit-hours':
      router.push({ name: 'ProviderBusinessHours' })
      break
    case 'view-profile':
      router.push({ name: 'ProviderProfile' })
      break
  }
}

const handleBookingClick = (booking: Appointment) => {
  // Navigate to booking details
  router.push({ name: 'ProviderBookingDetails', params: { id: booking.id } })
}

const handleRevenueRangeChange = (range: string) => {
  console.log('Revenue range changed to:', range)
  // TODO: Load data for selected range
  // isLoadingCharts.value = true
  // await loadRevenueData(range)
  // isLoadingCharts.value = false
}

onMounted(async () => {
  isLoadingBookings.value = true
  isLoadingCharts.value = true

  try {
    if (!currentProvider.value) {
      await providerStore.loadCurrentProvider()
    }

    // Load dashboard data for THIS provider only
    if (currentProvider.value) {
      // TODO: Load real data from API
      // todayStats.value = await loadProviderStats(currentProvider.value.id)
      // recentBookings.value = await loadProviderBookings(currentProvider.value.id)
      // revenueData.value = await loadRevenueData(currentProvider.value.id)
      // bookingsData.value = await loadBookingsData(currentProvider.value.id)
    }
  } finally {
    isLoadingBookings.value = false
    isLoadingCharts.value = false
  }
})
</script>

<style scoped>
.provider-dashboard {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xl);
  padding: var(--spacing-xl);
}

/* Status Banner Styles */
.status-banner {
  padding: var(--spacing-md) var(--spacing-xl);
  border-radius: var(--radius-lg);
  border-inline-start: 4px solid;
  background: white;
  box-shadow: var(--shadow-sm);
}

.status-banner-content {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-md);
}

.status-icon {
  font-size: var(--font-size-2xl);
  flex-shrink: 0;
}

.status-text {
  flex: 1;
}

.status-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  margin: 0 0 var(--spacing-xs) 0;
}

.status-message {
  font-size: var(--font-size-sm);
  margin: 0;
  color: var(--color-text-secondary);
  line-height: 1.5;
}

/* Status-specific colors */
.status-pendingverification {
  border-inline-start-color: var(--color-warning-500);
  background: var(--color-warning-50);
}

.status-pendingverification .status-title {
  color: var(--color-warning-700);
}

.status-inactive {
  border-inline-start-color: var(--color-danger-500);
  background: var(--color-danger-50);
}

.status-inactive .status-title {
  color: var(--color-danger-700);
}

.status-suspended {
  border-inline-start-color: var(--color-danger-600);
  background: var(--color-danger-50);
}

.status-suspended .status-title {
  color: var(--color-danger-800);
}

.status-archived {
  border-inline-start-color: var(--color-gray-500);
  background: var(--color-gray-50);
}

.status-archived .status-title {
  color: var(--color-gray-700);
}

/* Stats Grid */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: var(--spacing-lg);
}

/* Charts Section */
.charts-section {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: var(--spacing-lg);
}

.chart-card {
  min-width: 0;
}

/* Dashboard Grid */
.dashboard-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: var(--spacing-lg);
}

.dashboard-card {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg) var(--spacing-xl);
  background: var(--color-background);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-sm);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--spacing-md);
}

.card-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  margin: 0;
  color: var(--color-text-primary);
}

.view-all-button {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-xs) var(--spacing-sm);
  background: transparent;
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-primary-600);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.view-all-button:hover {
  background: var(--color-primary-50);
  border-color: var(--color-primary-300);
}

.view-all-button svg {
  width: 16px;
  height: 16px;
  stroke-width: 2;
  transition: transform var(--transition-fast);
}

.view-all-button:hover svg {
  transform: translateX(2px);
}

.bookings-card {
  min-height: 400px;
}

/* Mobile responsive */
@media (max-width: 767px) {
  .provider-dashboard {
    padding: var(--spacing-md);
    gap: var(--spacing-lg);
  }

  .stats-grid {
    grid-template-columns: 1fr;
    gap: var(--spacing-md);
  }

  .charts-section {
    grid-template-columns: 1fr;
    gap: var(--spacing-md);
  }

  .dashboard-grid {
    grid-template-columns: 1fr;
    gap: var(--spacing-md);
  }

  .dashboard-card {
    padding: var(--spacing-md) var(--spacing-lg);
    gap: var(--spacing-md);
  }

  .status-banner {
    padding: var(--spacing-sm) var(--spacing-md);
  }

  .bookings-card {
    min-height: auto;
  }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  .provider-dashboard {
    padding: var(--spacing-lg);
  }

  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .charts-section {
    grid-template-columns: 1fr;
  }

  .dashboard-grid {
    grid-template-columns: 1fr;
  }
}

/* Large desktop */
@media (min-width: 1440px) {
  .charts-section {
    grid-template-columns: 1fr 1fr;
  }
}

/* RTL Support - Already handled by logical properties */

/* Print styles */
@media print {
  .provider-dashboard {
    gap: var(--spacing-lg);
  }

  .status-banner,
  .dashboard-card {
    border: 1px solid #000;
    box-shadow: none;
    page-break-inside: avoid;
  }

  .view-all-button,
  .chart-controls {
    display: none;
  }
}
</style>
