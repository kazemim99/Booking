<template>
  <div class="provider-layout">
    <!-- Provider Header -->
    <ProviderHeader :provider="currentProvider" @toggle-sidebar="toggleSidebar" />

    <!-- Provider Sidebar -->
    <ProviderSidebar
      :is-collapsed="isSidebarCollapsed"
      :provider="currentProvider"
      @toggle="toggleSidebar"
    />

    <!-- Main Content -->
    <main class="provider-main" :class="{ 'sidebar-collapsed': isSidebarCollapsed }">
      <!-- Onboarding Alert (if profile incomplete) -->
      <Alert
        v-if="!isProfileComplete"
        type="warning"
        class="onboarding-alert"
        dismissible
        message="Complete your profile to start receiving bookings!"
      >
        <div class="alert-content">
          <p>{{ completionPercentage }}% complete</p>
          <AppButton size="sm" variant="primary" @click="goToOnboarding">
            Complete Profile
          </AppButton>
        </div>
      </Alert>

      <!-- Page Content -->
      <div class="provider-content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </div>
      <ProviderFooter />
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '../stores/provider.store'
import ProviderHeader from '../components/navigation/ProviderHeader.vue'
import ProviderSidebar from '../components/navigation/ProviderSidebar.vue'
import ProviderFooter from '../components/navigation/ProviderFooter.vue'
import { Alert } from '@/shared/components'

const router = useRouter()
const providerStore = useProviderStore()

const isSidebarCollapsed = ref(false)

const currentProvider = computed(() => providerStore.currentProvider)

// Profile completion check
const isProfileComplete = computed(() => {
  const provider = currentProvider.value
  if (!provider) return false

  return !!(
    provider.profile.businessName &&
    provider.profile.description &&
    provider.contactInfo.email &&
    provider.contactInfo.primaryPhone &&
    provider.address.addressLine1 &&
    provider.businessHours &&
    provider.businessHours.length > 0
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

  const completed = checks.filter(Boolean).length
  return Math.round((completed / checks.length) * 100)
})

const toggleSidebar = () => {
  isSidebarCollapsed.value = !isSidebarCollapsed.value
  localStorage.setItem('providerSidebarCollapsed', JSON.stringify(isSidebarCollapsed.value))
}

const goToOnboarding = () => {
  router.push({ name: 'ProviderOnboarding' })
}

onMounted(async () => {
  const saved = localStorage.getItem('providerSidebarCollapsed')
  if (saved) {
    isSidebarCollapsed.value = JSON.parse(saved)
  }

  // ✅ Load current provider's data
  if (!currentProvider.value) {
    await providerStore.loadCurrentProvider()
  }
})
</script>

<style scoped>
.provider-layout {
  display: flex;
  min-height: 100vh;
  background: var(--color-bg-secondary);
}

.provider-main {
  flex: 1;
  margin-left: 260px;
  margin-top: 64px;
  transition: margin-left 0.3s ease;
}

.provider-main.sidebar-collapsed {
  margin-left: 80px;
}

.provider-content {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.onboarding-alert {
  margin: 1rem 2rem;
}

.alert-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.alert-content p {
  margin: 0;
  font-size: 0.875rem;
}

@media (max-width: 1024px) {
  .provider-main {
    margin-left: 0;
  }
}
</style>
