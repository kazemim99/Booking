<template>
  <div class="provider-layout">
    <!-- Provider Header -->
    <ProviderHeader :provider="currentProvider" @toggle-sidebar="toggleSidebar" />

    <!-- Provider Sidebar -->
    <ProviderSidebar
      :is-collapsed="isSidebarCollapsed"
      :is-open="isSidebarOpen"
      :provider="currentProvider"
      @toggle="toggleSidebar"
      @close="closeSidebar"
    />

    <!-- Backdrop for mobile -->
    <transition name="fade">
      <div
        v-if="isSidebarOpen && isMobile"
        class="sidebar-backdrop"
        @click="closeSidebar"
      />
    </transition>

    <!-- Main Content -->
    <main class="provider-main" :class="mainClasses" role="main">
      <!-- Onboarding Alert (if profile incomplete) -->
      <transition name="slide-down">
        <Alert
          v-if="!isProfileComplete && !isAlertDismissed"
          type="warning"
          class="onboarding-alert"
          dismissible
          @dismiss="dismissAlert"
        >
          <div class="alert-content">
            <div class="alert-text">
              <strong>Complete your profile to start receiving bookings!</strong>
              <p>{{ completionPercentage }}% complete</p>
            </div>
            <Button size="sm" variant="primary" @click="goToOnboarding">
              Complete Profile
            </Button>
          </div>
        </Alert>
      </transition>

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
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '../stores/provider.store'
import ProviderHeader from '../components/navigation/ProviderHeader.vue'
import ProviderSidebar from '../components/navigation/ProviderSidebar.vue'
import ProviderFooter from '../components/navigation/ProviderFooter.vue'
import { Alert, Button } from '@/shared/components'

const router = useRouter()
const providerStore = useProviderStore()

const isSidebarCollapsed = ref(false)
const isSidebarOpen = ref(true)
const isAlertDismissed = ref(false)
const isMobile = ref(false)

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

const mainClasses = computed(() => ({
  'sidebar-collapsed': isSidebarCollapsed.value,
  'sidebar-open': isSidebarOpen.value && isMobile.value,
}))

const toggleSidebar = () => {
  if (isMobile.value) {
    isSidebarOpen.value = !isSidebarOpen.value
  } else {
    isSidebarCollapsed.value = !isSidebarCollapsed.value
    localStorage.setItem('providerSidebarCollapsed', JSON.stringify(isSidebarCollapsed.value))
  }
}

const closeSidebar = () => {
  if (isMobile.value) {
    isSidebarOpen.value = false
  }
}

const dismissAlert = () => {
  isAlertDismissed.value = true
  const dismissedUntil = Date.now() + 24 * 60 * 60 * 1000 // 24 hours
  localStorage.setItem('onboardingAlertDismissed', dismissedUntil.toString())
}

const goToOnboarding = () => {
  router.push({ name: 'ProviderRegistration' })
}

const checkMobile = () => {
  isMobile.value = window.innerWidth < 768
  if (!isMobile.value) {
    isSidebarOpen.value = true
  }
}

onMounted(async () => {
  // Check mobile
  checkMobile()
  window.addEventListener('resize', checkMobile)

  // Load sidebar state
  const saved = localStorage.getItem('providerSidebarCollapsed')
  if (saved && !isMobile.value) {
    isSidebarCollapsed.value = JSON.parse(saved)
  }

  // Check alert dismissal
  const dismissedUntil = localStorage.getItem('onboardingAlertDismissed')
  if (dismissedUntil && Date.now() < parseInt(dismissedUntil)) {
    isAlertDismissed.value = true
  }

  // Load current provider's data
  if (!currentProvider.value) {
    await providerStore.loadCurrentProvider()
  }
})

onUnmounted(() => {
  window.removeEventListener('resize', checkMobile)
})
</script>

<style scoped>
.provider-layout {
  display: flex;
  min-height: 100vh;
  background: var(--color-background-secondary);
}

.provider-main {
  flex: 1;
  margin-inline-start: var(--sidebar-width-expanded);
  margin-top: var(--header-height);
  transition: margin-inline-start var(--transition-slow);
  min-height: calc(100vh - var(--header-height));
  display: flex;
  flex-direction: column;
}

.provider-main.sidebar-collapsed {
  margin-inline-start: var(--sidebar-width-collapsed);
}

.provider-content {
  flex: 1;
  padding: var(--spacing-xl);
  max-width: var(--container-max-width);
  width: 100%;
  margin: 0 auto;
}

.onboarding-alert {
  margin: var(--spacing-lg) var(--spacing-xl);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-md);
}

.alert-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-md);
  flex-wrap: wrap;
}

.alert-text {
  flex: 1;
  min-width: 200px;
}

.alert-text strong {
  display: block;
  margin-bottom: var(--spacing-xs);
  color: var(--color-warning-700);
}

.alert-text p {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-warning-600);
}

/* Mobile backdrop */
.sidebar-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: calc(var(--z-index-fixed) - 1);
  backdrop-filter: blur(2px);
}

/* Mobile responsive */
@media (max-width: 767px) {
  .provider-main {
    margin-inline-start: 0;
  }

  .provider-content {
    padding: var(--spacing-md);
  }

  .onboarding-alert {
    margin: var(--spacing-md);
  }

  .alert-content {
    flex-direction: column;
    align-items: stretch;
  }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  .provider-content {
    padding: var(--spacing-lg);
  }
}

/* Transitions */
.fade-enter-active,
.fade-leave-active {
  transition: opacity var(--transition-base);
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.slide-down-enter-active,
.slide-down-leave-active {
  transition: all var(--transition-base);
}

.slide-down-enter-from,
.slide-down-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
