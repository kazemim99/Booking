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

    <!-- Quick Stats -->
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

    <!-- Main Content Grid -->
    <div class="dashboard-grid">
      <!-- Recent Bookings -->
      <Card class="dashboard-card bookings-card">
        <div class="card-header">
          <h2>Recent Bookings</h2>
          <AppButton variant="link" size="small" @click="goToBookings"> View All </AppButton>
        </div>
        <RecentBookingsCard :bookings="recentBookings" :loading="isLoading" />
      </Card>

      <!-- Quick Actions -->
      <Card class="dashboard-card actions-card">
        <div class="card-header">
          <h2>Quick Actions</h2>
        </div>
        <QuickActionsCard @action="handleQuickAction" />
      </Card>

    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '../../stores/provider.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { ProviderStatus } from '../../types/provider.types'
import { Card } from '@/shared/components'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import WelcomeCard from '../../components/dashboard/WelcomeCard.vue'
import QuickStatsCard from '../../components/dashboard/QuickStatsCard.vue'
import RecentBookingsCard from '../../components/dashboard/RecentBookingsCard.vue'

const router = useRouter()
const providerStore = useProviderStore()
const authStore = useAuthStore()

const isLoading = ref(false)
const currentProvider = computed(() => providerStore.currentProvider)
const providerStatus = computed(() => authStore.providerStatus)

// Status banner computed properties
const showStatusBanner = computed(() => {
  const status = providerStatus.value
  return status === ProviderStatus.PendingVerification ||
         status === ProviderStatus.Inactive ||
         status === ProviderStatus.Suspended ||
         status === ProviderStatus.Archived
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

// Mock data - Replace with real API calls
const todayStats = ref({
  bookings: 12,
  revenue: 1850,
  pending: 3,
  rating: 4.8,
})

const recentBookings = ref([
  // Mock bookings
])

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

onMounted(async () => {
  isLoading.value = true

  try {
    if (!currentProvider.value) {
      await providerStore.loadCurrentProvider()
    }

    // ✅ Load dashboard stats for THIS provider only
    if (currentProvider.value) {
      // TODO: Load bookings, revenue, etc. for currentProvider.value.id
      // todayStats.value = await loadProviderStats(currentProvider.value.id)
      // recentBookings.value = await loadProviderBookings(currentProvider.value.id)
    }
  } finally {
    isLoading.value = false
  }
})
</script>

<style scoped>
.provider-dashboard {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 1.5rem;
}

.dashboard-card {
  height: fit-content;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.card-header h2 {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0;
}

.bookings-card {
  grid-row: span 2;
}

@media (max-width: 1024px) {
  .dashboard-grid {
    grid-template-columns: 1fr;
  }

  .bookings-card {
    grid-row: span 1;
  }
}

/* Status Banner Styles */
.status-banner {
  padding: 1rem 1.5rem;
  border-radius: 0.5rem;
  border-left: 4px solid;
  background: white;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.status-banner-content {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
}

.status-icon {
  font-size: 1.5rem;
  flex-shrink: 0;
}

.status-text {
  flex: 1;
}

.status-title {
  font-size: 1rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
}

.status-message {
  font-size: 0.875rem;
  margin: 0;
  color: #6b7280;
  line-height: 1.5;
}

/* Status-specific colors */
.status-pendingverification {
  border-left-color: #f59e0b;
  background: #fffbeb;
}

.status-pendingverification .status-title {
  color: #d97706;
}

.status-inactive {
  border-left-color: #ef4444;
  background: #fef2f2;
}

.status-inactive .status-title {
  color: #dc2626;
}

.status-suspended {
  border-left-color: #dc2626;
  background: #fef2f2;
}

.status-suspended .status-title {
  color: #991b1b;
}

.status-archived {
  border-left-color: #6b7280;
  background: #f9fafb;
}

.status-archived .status-title {
  color: #4b5563;
}
</style>
