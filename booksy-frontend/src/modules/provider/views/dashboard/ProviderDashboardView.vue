<template>
  <DashboardLayout
    :current-page="currentPage"
    @navigate="handleNavigate"
    @logout="handleLogout"
  >
    <!-- Provider Status Banner -->
    <div v-if="showStatusBanner" class="status-banner mb-6" :class="`status-${providerStatus?.toLowerCase()}`">
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

    <!-- Bookings Tab -->
    <div v-if="currentPage === 'bookings'">
      <!-- Booking Statistics -->
      <BookingStatsCard :provider-id="currentProvider?.id" />

      <!-- Booking List -->
      <BookingListCard :provider-id="currentProvider?.id" />
    </div>

    <!-- Profile Tab -->
    <div v-if="currentPage === 'profile'">
      <ProfileManager />
    </div>
  </DashboardLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '../../stores/provider.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { ProviderStatus } from '../../types/provider.types'
import DashboardLayout from '../../components/dashboard/DashboardLayout.vue'
import BookingStatsCard from '../../components/dashboard/BookingStatsCard.vue'
import BookingListCard from '../../components/dashboard/BookingListCard.vue'
import ProfileManager from '../../components/dashboard/ProfileManager.vue'

const router = useRouter()
const providerStore = useProviderStore()
const authStore = useAuthStore()

const currentPage = ref('bookings')
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

const handleNavigate = (page: string) => {
  currentPage.value = page
}

const handleLogout = async () => {
  await authStore.logout()
  router.push({ name: 'Login' })
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
/* Status Banner Styles */
.status-banner {
  padding: 1rem 1.5rem;
  border-radius: 0.5rem;
  border-right: 4px solid;
  background: white;
  box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
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
  margin: 0 0 0.25rem 0;
}

.status-message {
  font-size: 0.875rem;
  margin: 0;
  color: #6b7280;
  line-height: 1.5;
}

/* Status-specific colors */
.status-pendingverification {
  border-right-color: #f59e0b;
  background: #fffbeb;
}

.status-pendingverification .status-title {
  color: #b45309;
}

.status-inactive {
  border-right-color: #ef4444;
  background: #fef2f2;
}

.status-inactive .status-title {
  color: #b91c1c;
}

.status-suspended {
  border-right-color: #dc2626;
  background: #fef2f2;
}

.status-suspended .status-title {
  color: #991b1b;
}

.status-archived {
  border-right-color: #6b7280;
  background: #f9fafb;
}

.status-archived .status-title {
  color: #374151;
}
</style>
