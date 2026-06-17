<template>
  <DashboardLayout>
    <!-- Welcome Section -->
    <div class="dashboard-welcome">
      <h1>خوش آمدید، {{ displayName }}</h1>
      <p>مدیریت کسب و کار خود از اینجا شروع کنید</p>
    </div>

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

    <!-- Quick Actions Grid -->
    <div class="quick-actions">
      <div class="action-card" @click="navigateTo('/bookings')">
        <div class="action-icon bookings-icon">
          <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        </div>
        <h3>رزروها</h3>
        <p>مشاهده و مدیریت رزروها</p>
      </div>

      <div class="action-card" @click="navigateTo('/financial')">
        <div class="action-icon financial-icon">
          <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
        <h3>مالی</h3>
        <p>گزارشات مالی و تراکنش‌ها</p>
      </div>

      <div class="action-card" @click="navigateTo('/profile')">
        <div class="action-icon profile-icon">
          <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
          </svg>
        </div>
        <h3>پروفایل</h3>
        <p>مدیریت اطلاعات کسب و کار</p>
      </div>
    </div>

    <!-- Quick Stats -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon">📅</div>
        <div class="stat-info">
          <h4>رزروهای امروز</h4>
          <p class="stat-value">-</p>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon">💰</div>
        <div class="stat-info">
          <h4>درآمد این ماه</h4>
          <p class="stat-value">-</p>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon">⭐</div>
        <div class="stat-info">
          <h4>امتیاز</h4>
          <p class="stat-value">-</p>
        </div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '../../stores/provider.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { ProviderStatus } from '../../types/provider.types'
import DashboardLayout from '../../components/dashboard/DashboardLayout.vue'

const router = useRouter()
const providerStore = useProviderStore()
const authStore = useAuthStore()

const currentProvider = computed(() => providerStore.currentProvider)
const providerStatus = computed(() => authStore.providerStatus)

const displayName = computed(() => {
  if (!currentProvider.value) return 'مدیر'
  return currentProvider.value.profile?.businessName || 'مدیر'
})

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
      return 'در انتظار تایید حساب'
    case ProviderStatus.Inactive:
      return 'حساب غیرفعال'
    case ProviderStatus.Suspended:
      return 'حساب معلق شده'
    case ProviderStatus.Archived:
      return 'حساب بایگانی شده'
    default:
      return ''
  }
})

const statusMessage = computed(() => {
  switch (providerStatus.value) {
    case ProviderStatus.PendingVerification:
      return 'حساب شما در انتظار تایید مدیر است. می‌توانید داشبورد را مشاهده کنید اما برخی امکانات محدود هستند.'
    case ProviderStatus.Inactive:
      return 'حساب شما در حال حاضر غیرفعال است. لطفا با پشتیبانی تماس بگیرید.'
    case ProviderStatus.Suspended:
      return 'حساب شما به طور موقت معلق شده است. لطفا با پشتیبانی تماس بگیرید.'
    case ProviderStatus.Archived:
      return 'حساب شما بایگانی شده است. لطفا با پشتیبانی تماس بگیرید.'
    default:
      return ''
  }
})

const navigateTo = (path: string) => {
  router.push(path)
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
.dashboard-welcome {
  margin-bottom: 2rem;
}

.dashboard-welcome h1 {
  font-size: 2rem;
  font-weight: 700;
  color: var(--color-gray-900);
  margin: 0 0 0.5rem;
}

.dashboard-welcome p {
  font-size: 1rem;
  color: var(--color-gray-600);
  margin: 0;
}

/* Status Banner */
.status-banner {
  padding: 1rem 1.5rem;
  border-radius: 0.5rem;
  border-right: 4px solid;
  background: white;
  box-shadow: var(--shadow-sm);
  margin-bottom: 2rem;
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
  color: var(--color-gray-600);
  line-height: 1.5;
}

/* Status colors */
.status-pendingverification {
  border-right-color: var(--color-warning-500);
  background: #fffbeb;
}

.status-pendingverification .status-title {
  color: #b45309;
}

.status-inactive {
  border-right-color: var(--color-danger-500);
  background: var(--color-danger-50);
}

.status-inactive .status-title {
  color: var(--color-danger-600);
}

.status-suspended {
  border-right-color: var(--color-danger-600);
  background: var(--color-danger-50);
}

.status-suspended .status-title {
  color: var(--color-danger-600);
}

.status-archived {
  border-right-color: var(--color-gray-600);
  background: var(--color-gray-50);
}

.status-archived .status-title {
  color: var(--color-gray-800);
}

/* Quick Actions */
.quick-actions {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.action-card {
  background: white;
  border-radius: 0.75rem;
  padding: 2rem;
  text-align: center;
  cursor: pointer;
  transition: all 0.3s;
  box-shadow: var(--shadow-sm);
}

.action-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-md);
}

.action-icon {
  width: 64px;
  height: 64px;
  margin: 0 auto 1rem;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.action-icon svg {
  width: 32px;
  height: 32px;
  color: white;
}

.bookings-icon {
  background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-700) 100%);
}

.financial-icon {
  background: linear-gradient(135deg, #f093fb 0%, var(--color-primary-400) 100%);
}

.profile-icon {
  background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
}

.action-card h3 {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin: 0 0 0.5rem;
}

.action-card p {
  font-size: 0.875rem;
  color: var(--color-gray-600);
  margin: 0;
}

/* Stats Grid */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.stat-card {
  background: white;
  border-radius: 0.5rem;
  padding: 1.5rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: var(--shadow-sm);
}

.stat-icon {
  font-size: 2rem;
}

.stat-info {
  flex: 1;
}

.stat-info h4 {
  font-size: 0.875rem;
  color: var(--color-gray-600);
  margin: 0 0 0.25rem;
}

.stat-value {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-gray-900);
  margin: 0;
}

/* Responsive */
@media (max-width: 768px) {
  .dashboard-welcome h1 {
    font-size: 1.5rem;
  }

  .quick-actions {
    grid-template-columns: 1fr;
  }

  .stats-grid {
    grid-template-columns: 1fr;
  }
}
</style>
