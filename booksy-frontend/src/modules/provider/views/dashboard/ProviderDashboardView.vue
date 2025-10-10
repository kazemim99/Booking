<template>
  <div class="provider-dashboard">
    <!-- Welcome Header -->
    <WelcomeCard :provider="currentProvider" :show-onboarding="!isProfileComplete" />

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
          <AppButton variant="text" size="sm" @click="goToBookings"> View All </AppButton>
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

      <!-- Profile Completion (if incomplete) -->
      <Card v-if="!isProfileComplete" class="dashboard-card completion-card">
        <ProfileCompletionCard
          :percentage="completionPercentage"
          :missing-items="missingProfileItems"
          @complete="goToOnboarding"
        />
      </Card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '../../stores/provider.store'
import { Card } from '@/shared/components'
import WelcomeCard from '../../components/dashboard/WelcomeCard.vue'
import QuickStatsCard from '../../components/dashboard/QuickStatsCard.vue'
import RecentBookingsCard from '../../components/dashboard/RecentBookingsCard.vue'
import QuickActionsCard from '../../components/dashboard/QuickActionsCard.vue'
import ProfileCompletionCard from '../../components/onboarding/ProfileCompletionCard.vue'

const router = useRouter()
const providerStore = useProviderStore()

const isLoading = ref(false)
const currentProvider = computed(() => providerStore.currentProvider)

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

const completionPercentage = computed(() => {
  const provider = currentProvider.value
  if (!provider) return 0

  const checks = [
    !!provider.profile.businessName,
    !!provider.profile.description,
    !!provider.profile.logoUrl,
    !!provider.contactInfo.email,
    !!provider.contactInfo.primaryPhone,
    !!provider.address.addressLine1,
    !!provider.businessHours && provider.businessHours.length > 0,
    !!provider.services && provider.services.length > 0,
  ]

  return Math.round((checks.filter(Boolean).length / checks.length) * 100)
})

const missingProfileItems = computed(() => {
  const provider = currentProvider.value
  const items = []

  if (!provider?.profile.logoUrl) items.push('Upload business logo')
  if (!provider?.profile.description) items.push('Add business description')
  if (!provider?.businessHours || provider.businessHours.length === 0)
    items.push('Set business hours')
  if (!provider?.services || provider.services.length === 0) items.push('Add services')

  return items
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

const goToOnboarding = () => {
  router.push({ name: 'ProviderOnboarding' })
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
</style>
