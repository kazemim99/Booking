<template>
  <div class="dashboard-container" dir="rtl">
    <!-- Header -->
    <header class="dashboard-header">
      <div class="header-content">
        <div class="header-left">
          <!-- Mobile Menu Toggle -->
          <button
            class="mobile-toggle"
            @click="toggleMobileMenu"
            aria-label="Toggle Menu"
          >
            <svg
              v-if="!mobileMenuOpen"
              class="icon"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
            <svg
              v-else
              class="icon"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>

          <!-- Logo/Title -->
          <div class="logo-section">
            <svg class="logo-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
            <h1 class="dashboard-title">داشبورد من</h1>
          </div>
        </div>

        <!-- User Info -->
        <div class="user-section">
          <!-- Role Badge -->
          <div v-if="roleLabel" class="role-badge" :class="roleBadgeClass">
            {{ roleLabel }}
          </div>
          <div class="user-name">
            <p>{{ displayName }}</p>
          </div>
          <div class="user-avatar">
            {{ avatarLetter }}
          </div>
        </div>
      </div>
    </header>

    <div class="dashboard-body">
      <!-- Sidebar - Desktop -->
      <aside class="dashboard-sidebar">
        <nav class="sidebar-nav">
          <!-- Loading State -->
          <div v-if="isLoadingHierarchy" class="loading-indicator">
            <div class="loading-spinner"></div>
            <span class="loading-text">در حال بارگذاری...</span>
          </div>

          <!-- Error State with Retry -->
          <div v-else-if="hierarchyLoadError" class="error-indicator">
            <span class="error-text">{{ hierarchyLoadError }}</span>
            <button @click="retryLoadHierarchy" class="retry-button">تلاش مجدد</button>
          </div>

          <!-- Menu Items -->
          <button
            v-for="item in menuItems"
            :key="item.id"
            @click="navigateTo(item.route)"
            :class="['nav-item', { 'active': isMenuItemActive(item.route) }]"
          >
            <component :is="item.icon" class="nav-icon" />
            <span>{{ item.label }}</span>
          </button>

          <!-- Logout Button -->
          <div class="nav-divider">
            <button
              @click="handleLogout"
              class="nav-item logout-item"
            >
              <LogoutIcon class="nav-icon" />
              <span>خروج</span>
            </button>
          </div>
        </nav>
      </aside>

      <!-- Mobile Menu -->
      <div v-if="mobileMenuOpen" class="mobile-menu-overlay">
        <!-- Backdrop -->
        <div class="mobile-backdrop" @click="toggleMobileMenu" />

        <!-- Menu Panel -->
        <aside class="mobile-menu-panel">
          <div class="mobile-menu-header">
            <h2>منوی اصلی</h2>
            <button class="mobile-close" @click="toggleMobileMenu">
              <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <nav class="mobile-nav">
            <!-- Loading State -->
            <div v-if="isLoadingHierarchy" class="loading-indicator">
              <div class="loading-spinner"></div>
              <span class="loading-text">در حال بارگذاری...</span>
            </div>

            <!-- Error State with Retry -->
            <div v-else-if="hierarchyLoadError" class="error-indicator">
              <span class="error-text">{{ hierarchyLoadError }}</span>
              <button @click="retryLoadHierarchy" class="retry-button">تلاش مجدد</button>
            </div>

            <!-- Menu Items -->
            <button
              v-for="item in menuItems"
              :key="item.id"
              @click="navigateToMobile(item.route)"
              :class="['nav-item', { 'active': isMenuItemActive(item.route) }]"
            >
              <component :is="item.icon" class="nav-icon" />
              <span>{{ item.label }}</span>
            </button>

            <!-- Logout Button -->
            <div class="nav-divider">
              <button @click="handleLogout" class="nav-item logout-item">
                <LogoutIcon class="nav-icon" />
                <span>خروج</span>
              </button>
            </div>
          </nav>
        </aside>
      </div>

      <!-- Main Content -->
      <main class="dashboard-main">
        <slot />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, h, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useProviderStore } from '../../stores/provider.store'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { HierarchyType } from '../../types/hierarchy.types'

// Icon Components (Simple SVG-based)
const CalendarIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z' })
])

const UserIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z' })
])

const CurrencyIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z' })
])

const LogoutIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1' })
])

const UsersIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z' })
])

const router = useRouter()
const route = useRoute()
const providerStore = useProviderStore()
const hierarchyStore = useHierarchyStore()
const authStore = useAuthStore()
const mobileMenuOpen = ref(false)

// Loading states
const isLoadingHierarchy = ref(false)
const hierarchyLoadError = ref<string | null>(null)

// Load provider data on mount
onMounted(async () => {
  await loadProviderData()
})

// Load provider data function (can be called to retry)
async function loadProviderData() {
  try {
    isLoadingHierarchy.value = true
    hierarchyLoadError.value = null

    // Load current provider if not already loaded
    if (!providerStore.currentProvider) {
      console.log('[DashboardLayout] Loading current provider...')
      await providerStore.loadCurrentProvider()
    }

    // Load hierarchy information using the provider ID
    if (providerStore.currentProvider?.id) {
      console.log('[DashboardLayout] Loading hierarchy for provider:', providerStore.currentProvider.id)
      await hierarchyStore.loadProviderHierarchy(providerStore.currentProvider.id)
      console.log('[DashboardLayout] Hierarchy loaded successfully:', hierarchyStore.currentHierarchy)
    } else {
      console.warn('[DashboardLayout] No provider ID found, cannot load hierarchy')
    }
  } catch (error) {
    console.error('[DashboardLayout] Failed to load provider data:', error)
    hierarchyLoadError.value = 'خطا در بارگذاری اطلاعات'
  } finally {
    isLoadingHierarchy.value = false
  }
}

// Retry loading hierarchy
function retryLoadHierarchy() {
  loadProviderData()
}

const currentProvider = computed(() => providerStore.currentProvider)
const currentHierarchy = computed(() => hierarchyStore.currentHierarchy)

const displayName = computed(() => {
  if (!currentProvider.value) return 'مدیر'
  return currentProvider.value.profile?.businessName || 'مدیر'
})

const avatarLetter = computed(() => {
  return displayName.value.charAt(0)
})

// Role-based badge label
const roleLabel = computed(() => {
  const provider = currentHierarchy.value?.provider
  const hierarchyType = provider?.hierarchyType
  const hasParentOrg = !!provider?.parentOrganizationId

  if (hierarchyType === HierarchyType.Organization) {
    return ''
  }

  if (hierarchyType === HierarchyType.Individual && hasParentOrg) {
    return 'کارمند'
  }

  if (hierarchyType === HierarchyType.Individual && !hasParentOrg) {
    return 'فردی'
  }

  return null
})

const roleBadgeClass = computed(() => {
  const provider = currentHierarchy.value?.provider
  const hierarchyType = provider?.hierarchyType
  const hasParentOrg = !!provider?.parentOrganizationId

  if (hierarchyType === HierarchyType.Organization) {
    return 'badge-organization'
  }

  if (hierarchyType === HierarchyType.Individual && hasParentOrg) {
    return 'badge-staff'
  }

  if (hierarchyType === HierarchyType.Individual && !hasParentOrg) {
    return 'badge-individual'
  }

  return ''
})

// Dashboard Icon
const DashboardIcon = () => h('svg', { class: 'w-5 h-5', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
  h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' })
])

const menuItems = computed(() => {
  const provider = currentHierarchy.value?.provider
  const hierarchyType = provider?.hierarchyType
  const hasParentOrg = !!provider?.parentOrganizationId

  // Base menu items for all providers
  const baseItems = [
    { id: 'dashboard', label: 'داشبورد', icon: DashboardIcon, route: '/provider/dashboard' }
  ]

  // If loading, show loading state with base items
  if (isLoadingHierarchy.value) {
    console.log('[DashboardLayout] Menu: Loading hierarchy...')
    return baseItems
  }

  // If error occurred, show base items with retry option
  if (hierarchyLoadError.value) {
    console.log('[DashboardLayout] Menu: Error loading hierarchy, showing base items')
    return baseItems
  }

  // If no hierarchy loaded yet, show enhanced fallback menu
  if (!currentHierarchy.value || !hierarchyType) {
    console.log('[DashboardLayout] Menu: No hierarchy data, showing enhanced fallback')
    // Enhanced fallback - show common items that most providers need
    return [
      ...baseItems,
      { id: 'profile', label: 'پروفایل', icon: UserIcon, route: '/provider/profile' },
      { id: 'bookings', label: 'رزروها', icon: CalendarIcon, route: '/provider/bookings' },
      { id: 'financial', label: 'مالی', icon: CurrencyIcon, route: '/provider/financial' }
    ]
  }

  // ORGANIZATION MENU - Full management capabilities
  if (hierarchyType === HierarchyType.Organization) {
    console.log('[DashboardLayout] Menu: Organization type')
    return [
      ...baseItems,
      { id: 'bookings', label: 'رزروها', icon: CalendarIcon, route: '/provider/bookings' },
      { id: 'financial', label: 'مالی', icon: CurrencyIcon, route: '/provider/financial' },
      { id: 'staff', label: 'پرسنل', icon: UsersIcon, route: '/provider/staff' }, // ← ORG ONLY
      { id: 'profile', label: 'پروفایل', icon: UserIcon, route: '/provider/profile' }
    ]
  }

  // STAFF MEMBER MENU - Limited to own data
  if (hierarchyType === HierarchyType.Individual && hasParentOrg) {
    console.log('[DashboardLayout] Menu: Staff member type')
    return [
      ...baseItems,
      { id: 'my-bookings', label: 'رزروهای من', icon: CalendarIcon, route: '/provider/my-bookings' },
      { id: 'my-earnings', label: 'درآمد من', icon: CurrencyIcon, route: '/provider/my-earnings' },
      { id: 'my-profile', label: 'پروفایل من', icon: UserIcon, route: '/provider/my-profile' },
      { id: 'organization', label: 'سازمان من', icon: UsersIcon, route: '/provider/my-organization' }
    ]
  }

  // INDEPENDENT INDIVIDUAL MENU - Full control, no staff management
  if (hierarchyType === HierarchyType.Individual && !hasParentOrg) {
    console.log('[DashboardLayout] Menu: Independent individual type')
    return [
      ...baseItems,
      { id: 'bookings', label: 'رزروها', icon: CalendarIcon, route: '/provider/bookings' },
      { id: 'financial', label: 'مالی', icon: CurrencyIcon, route: '/provider/financial' },
      { id: 'profile', label: 'پروفایل من', icon: UserIcon, route: '/provider/profile' }
    ]
  }

  // Final fallback
  console.log('[DashboardLayout] Menu: Unknown hierarchy type, using enhanced fallback')
  return [
    ...baseItems,
    { id: 'profile', label: 'پروفایل', icon: UserIcon, route: '/provider/profile' },
    { id: 'bookings', label: 'رزروها', icon: CalendarIcon, route: '/provider/bookings' },
    { id: 'financial', label: 'مالی', icon: CurrencyIcon, route: '/provider/financial' }
  ]
})

// Determine if a menu item is active based on current route
const isMenuItemActive = (itemRoute: string) => {
  return route.path === itemRoute
}

const toggleMobileMenu = () => {
  mobileMenuOpen.value = !mobileMenuOpen.value
}

const navigateTo = (itemRoute: string) => {
  router.push(itemRoute)
}

const navigateToMobile = (itemRoute: string) => {
  router.push(itemRoute)
  mobileMenuOpen.value = false
}

const handleLogout = async () => {
  await authStore.logout()
  router.push({ name: 'Login' })
}
</script>

<style scoped lang="scss">
/* Main Container */
.dashboard-container {
  min-height: 100vh;
  background: #f5f5f5;
  display: flex;
  flex-direction: column;
}

/* Header */
.dashboard-header {
  background: white;
  border-bottom: 1px solid #e0e0e0;
  position: sticky;
  top: 0;
  z-index: 40;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.header-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.mobile-toggle {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 8px;
  border: none;
  background: transparent;
  cursor: pointer;
  border-radius: 6px;
  transition: background-color 0.2s;

  &:hover {
    background: #f5f5f5;
  }

  .icon {
    width: 20px;
    height: 20px;
  }
}

.logo-section {
  display: flex;
  align-items: center;
  gap: 8px;
}

.logo-icon {
  width: 24px;
  height: 24px;
  color: #6366f1;
}

.dashboard-title {
  font-size: 18px;
  font-weight: 600;
  margin: 0;
  color: #1f2937;
}

.user-section {
  display: flex;
  align-items: center;
  gap: 12px;
}

.role-badge {
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;

  &.badge-organization {
    background: #dbeafe;
    color: #1e40af;
  }

  &.badge-staff {
    background: #fef3c7;
    color: #92400e;
  }

  &.badge-individual {
    background: #e0e7ff;
    color: #4338ca;
  }
}

.user-name {
  text-align: right;

  p {
    margin: 0;
    font-size: 14px;
    color: #4b5563;
  }
}

.user-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #e0e7ff;
  color: #4338ca;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 16px;
}

/* Dashboard Body */
.dashboard-body {
  display: flex;
  flex: 1;
}

/* Sidebar - Desktop */
.dashboard-sidebar {
  background: white;
  border-left: 1px solid #e0e0e0;
  width: 256px;
  position: sticky;
  top: 57px;
  height: calc(100vh - 57px);
  overflow-y: auto;
}

.sidebar-nav {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.nav-item {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  border-radius: 8px;
  border: none;
  background: transparent;
  cursor: pointer;
  transition: all 0.2s;
  text-align: right;
  font-size: 14px;
  color: #4b5563;

  &:hover {
    background: #f5f5f5;
  }

  &.active {
    background: #6366f1;
    color: white;
  }

  .nav-icon {
    width: 20px;
    height: 20px;
    flex-shrink: 0;
  }
}

.nav-divider {
  border-top: 1px solid #e0e0e0;
  padding-top: 16px;
  margin-top: 16px;
}

.logout-item {
  color: #dc2626;

  &:hover {
    background: #fee2e2;
  }
}

/* Loading Indicator */
.loading-indicator {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 24px 16px;
  margin-bottom: 16px;
}

.loading-spinner {
  width: 32px;
  height: 32px;
  border: 3px solid #e0e0e0;
  border-top-color: #6366f1;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.loading-text {
  font-size: 12px;
  color: #6b7280;
}

/* Error Indicator */
.error-indicator {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 16px;
  margin-bottom: 16px;
  background: #fef2f2;
  border-radius: 8px;
  border: 1px solid #fecaca;
}

.error-text {
  font-size: 12px;
  color: #dc2626;
  text-align: center;
}

.retry-button {
  padding: 6px 16px;
  background: #dc2626;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 12px;
  cursor: pointer;
  transition: background 0.2s;

  &:hover {
    background: #b91c1c;
  }

  &:active {
    background: #991b1b;
  }
}

/* Mobile Menu */
.mobile-menu-overlay {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  z-index: 50;
}

.mobile-backdrop {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
}

.mobile-menu-panel {
  position: absolute;
  right: 0;
  top: 0;
  bottom: 0;
  width: 256px;
  background: white;
  border-left: 1px solid #e0e0e0;
  box-shadow: -2px 0 8px rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
}

.mobile-menu-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px;
  border-bottom: 1px solid #e0e0e0;

  h2 {
    font-size: 18px;
    font-weight: 600;
    margin: 0;
  }
}

.mobile-close {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 8px;
  border: none;
  background: transparent;
  cursor: pointer;
  border-radius: 6px;
  transition: background-color 0.2s;

  &:hover {
    background: #f5f5f5;
  }

  .icon {
    width: 20px;
    height: 20px;
  }
}

.mobile-nav {
  flex: 1;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 4px;
  overflow-y: auto;
}

/* Main Content */
.dashboard-main {
  flex: 1;
  padding: 24px;
  overflow-y: auto;
}

/* Responsive */
@media (max-width: 1023px) {
  .dashboard-sidebar {
    display: none;
  }

  .mobile-toggle {
    display: flex;
  }
}

@media (min-width: 1024px) {
  .dashboard-sidebar {
    display: block;
  }

  .mobile-toggle {
    display: none;
  }

  .mobile-menu-overlay {
    display: none !important;
  }

  .user-name {
    display: block;
  }
}

@media (max-width: 639px) {
  .user-name {
    display: none;
  }

  .dashboard-main {
    padding: 16px;
  }
}

/* Icon helper */
.icon {
  width: 20px;
  height: 20px;
}
</style>
