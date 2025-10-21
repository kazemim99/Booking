<template>
  <aside
    class="provider-sidebar"
    :class="sidebarClasses"
    role="navigation"
    aria-label="Provider navigation"
  >
    <!-- Logo/Brand -->
    <div class="sidebar-header">
      <router-link to="/provider/dashboard" class="brand" @click="handleNavigation">
        <img v-if="!isCollapsed" src="@/assets/logo.svg" alt="Booksy" class="logo" />
        <img v-else src="@/assets/logo.svg" alt="B" class="logo-icon" />
      </router-link>
    </div>

    <!-- Navigation Menu -->
    <nav class="sidebar-nav">
      <div class="nav-section">
        <span v-if="!isCollapsed" class="section-title">Main</span>

        <NavItem
          to="/provider/dashboard"
          icon="dashboard"
          label="Dashboard"
          :collapsed="isCollapsed"
          @click="handleNavigation"
        />

        <NavItem
          to="/provider/bookings"
          icon="calendar"
          label="Bookings"
          :badge="pendingBookingsCount"
          :collapsed="isCollapsed"
          @click="handleNavigation"
        />

        <NavItem
          to="/provider/services"
          icon="services"
          label="Services"
          :collapsed="isCollapsed"
          @click="handleNavigation"
        />

        <NavItem
          to="/provider/staff"
          icon="users"
          label="Staff"
          :collapsed="isCollapsed"
          @click="handleNavigation"
        />
      </div>

      <div class="nav-section">
        <span v-if="!isCollapsed" class="section-title">Business</span>

        <NavItem
          to="/provider/profile"
          icon="building"
          label="Profile"
          :collapsed="isCollapsed"
          @click="handleNavigation"
        />

        <NavItem
          to="/provider/hours"
          icon="clock"
          label="Business Hours"
          :collapsed="isCollapsed"
          @click="handleNavigation"
        />

        <NavItem
          to="/provider/gallery"
          icon="image"
          label="Gallery"
          :collapsed="isCollapsed"
          @click="handleNavigation"
        />
      </div>

      <div class="nav-section">
        <span v-if="!isCollapsed" class="section-title">Insights</span>

        <NavItem
          to="/provider/analytics"
          icon="chart"
          label="Analytics"
          :collapsed="isCollapsed"
          @click="handleNavigation"
        />

        <NavItem
          to="/provider/reviews"
          icon="star"
          label="Reviews"
          :collapsed="isCollapsed"
          @click="handleNavigation"
        />
      </div>
    </nav>

    <!-- Toggle Button (desktop only) -->
    <button
      v-if="!isMobile"
      class="toggle-btn"
      @click="$emit('toggle')"
      aria-label="Toggle sidebar"
    >
      <svg v-if="!isCollapsed" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M11 19l-7-7 7-7m8 14l-7-7 7-7"
        />
      </svg>
      <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M13 5l7 7-7 7M5 5l7 7-7 7"
        />
      </svg>
    </button>
  </aside>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import NavItem from './NavItem.vue'
import type { Provider } from '../../types/provider.types'

interface Props {
  isCollapsed: boolean
  isOpen: boolean
  provider: Provider | null
}

const props = defineProps<Props>()
const emit = defineEmits(['toggle', 'close'])

const isMobile = ref(false)

const sidebarClasses = computed(() => ({
  collapsed: props.isCollapsed,
  open: props.isOpen,
  mobile: isMobile.value,
}))

const pendingBookingsCount = computed(() => {
  // TODO: Get from bookings store
  return 5
})

const handleNavigation = () => {
  if (isMobile.value) {
    emit('close')
  }
}

const checkMobile = () => {
  isMobile.value = window.innerWidth < 768
}

onMounted(() => {
  checkMobile()
  window.addEventListener('resize', checkMobile)
})

onUnmounted(() => {
  window.removeEventListener('resize', checkMobile)
})
</script>

<style scoped>
.provider-sidebar {
  position: fixed;
  top: 0;
  inset-inline-start: 0;
  height: 100vh;
  width: var(--sidebar-width-expanded);
  background: var(--color-background);
  border-inline-end: 1px solid var(--color-border);
  box-shadow: var(--shadow-sm);
  display: flex;
  flex-direction: column;
  transition: all var(--transition-slow);
  z-index: var(--z-index-fixed);
}

.provider-sidebar.collapsed {
  width: var(--sidebar-width-collapsed);
}

.sidebar-header {
  height: var(--header-height);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
  background: linear-gradient(135deg, var(--color-primary-50) 0%, var(--color-background) 100%);
}

.brand {
  display: flex;
  align-items: center;
  transition: transform var(--transition-fast);
}

.brand:hover {
  transform: scale(1.05);
}

.logo {
  height: 36px;
  transition: all var(--transition-fast);
}

.logo-icon {
  height: 36px;
  width: 36px;
}

.sidebar-nav {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: var(--spacing-lg) 0;
}

/* Custom scrollbar */
.sidebar-nav::-webkit-scrollbar {
  width: 6px;
}

.sidebar-nav::-webkit-scrollbar-track {
  background: var(--color-gray-50);
}

.sidebar-nav::-webkit-scrollbar-thumb {
  background: var(--color-gray-300);
  border-radius: var(--radius-full);
}

.sidebar-nav::-webkit-scrollbar-thumb:hover {
  background: var(--color-gray-400);
}

.nav-section {
  margin-bottom: var(--spacing-xl);
}

.nav-section:last-child {
  margin-bottom: 0;
}

.section-title {
  display: block;
  padding: var(--spacing-sm) var(--spacing-lg);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  color: var(--color-text-tertiary);
  letter-spacing: 0.05em;
  transition: opacity var(--transition-fast);
}

.provider-sidebar.collapsed .section-title {
  opacity: 0;
  height: 0;
  padding: 0;
  overflow: hidden;
}

.toggle-btn {
  position: absolute;
  bottom: var(--spacing-lg);
  inset-inline-end: var(--spacing-lg);
  width: 40px;
  height: 40px;
  border: 1px solid var(--color-border);
  background: var(--color-background);
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all var(--transition-fast);
  box-shadow: var(--shadow-sm);
}

.toggle-btn:hover {
  background: var(--color-primary-50);
  border-color: var(--color-primary-300);
  transform: scale(1.1);
  box-shadow: var(--shadow-md);
}

.toggle-btn:active {
  transform: scale(0.95);
}

.toggle-btn svg {
  width: 20px;
  height: 20px;
  color: var(--color-text-secondary);
  transition: color var(--transition-fast);
}

.toggle-btn:hover svg {
  color: var(--color-primary-600);
}

/* Mobile styles */
@media (max-width: 767px) {
  .provider-sidebar {
    transform: translateX(-100%);
    box-shadow: var(--shadow-xl);
  }

  .provider-sidebar.open {
    transform: translateX(0);
  }

  /* RTL support for mobile */
  [dir='rtl'] .provider-sidebar {
    transform: translateX(100%);
  }

  [dir='rtl'] .provider-sidebar.open {
    transform: translateX(0);
  }

  .toggle-btn {
    display: none;
  }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  .sidebar-header {
    padding: 0 var(--spacing-md);
  }
}
</style>
