<template>
  <aside class="provider-sidebar" :class="{ collapsed: isCollapsed }">
    <!-- Logo/Brand -->
    <div class="sidebar-header">
      <router-link to="/provider/dashboard" class="brand">
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
        />

        <NavItem
          to="/provider/bookings"
          icon="calendar"
          label="Bookings"
          :badge="pendingBookingsCount"
          :collapsed="isCollapsed"
        />

        <NavItem
          to="/provider/services"
          icon="services"
          label="Services"
          :collapsed="isCollapsed"
        />

        <NavItem to="/provider/staff" icon="users" label="Staff" :collapsed="isCollapsed" />
      </div>

      <div class="nav-section">
        <span v-if="!isCollapsed" class="section-title">Business</span>

        <NavItem to="/provider/profile" icon="building" label="Profile" :collapsed="isCollapsed" />

        <NavItem
          to="/provider/hours"
          icon="clock"
          label="Business Hours"
          :collapsed="isCollapsed"
        />

        <NavItem to="/provider/gallery" icon="image" label="Gallery" :collapsed="isCollapsed" />
      </div>

      <div class="nav-section">
        <span v-if="!isCollapsed" class="section-title">Insights</span>

        <NavItem to="/provider/analytics" icon="chart" label="Analytics" :collapsed="isCollapsed" />

        <NavItem to="/provider/reviews" icon="star" label="Reviews" :collapsed="isCollapsed" />
      </div>
    </nav>

    <!-- Toggle Button -->
    <button class="toggle-btn" @click="$emit('toggle')">
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
import { computed } from 'vue'
import NavItem from './NavItem.vue'
import type { Provider } from '../../types/provider.types'

interface Props {
  isCollapsed: boolean
  provider: Provider | null
}

defineProps<Props>()
defineEmits(['toggle'])

const pendingBookingsCount = computed(() => {
  // TODO: Get from bookings store
  return 5
})
</script>

<style scoped>
.provider-sidebar {
  position: fixed;
  top: 0;
  left: 0;
  height: 100vh;
  width: 260px;
  background: white;
  border-right: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  transition: width 0.3s ease;
  z-index: 100;
}

.provider-sidebar.collapsed {
  width: 80px;
}

.sidebar-header {
  height: 64px;
  display: flex;
  align-items: center;
  padding: 0 1.5rem;
  border-bottom: 1px solid var(--color-border);
}

.brand {
  display: flex;
  align-items: center;
}

.logo {
  height: 32px;
}

.logo-icon {
  height: 32px;
  width: 32px;
}

.sidebar-nav {
  flex: 1;
  overflow-y: auto;
  padding: 1rem 0;
}

.nav-section {
  margin-bottom: 1.5rem;
}

.section-title {
  display: block;
  padding: 0.5rem 1.5rem;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  color: var(--color-text-tertiary);
  letter-spacing: 0.5px;
}

.toggle-btn {
  position: absolute;
  bottom: 1rem;
  right: 1rem;
  width: 40px;
  height: 40px;
  border: 1px solid var(--color-border);
  background: white;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}

.toggle-btn:hover {
  background: var(--color-bg-secondary);
}

.toggle-btn svg {
  width: 20px;
  height: 20px;
  color: var(--color-text-secondary);
}
</style>
