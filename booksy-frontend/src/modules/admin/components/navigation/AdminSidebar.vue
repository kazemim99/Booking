<template>
  <aside class="admin-sidebar" :class="{ collapsed: isCollapsed }">
    <!-- Logo/Brand -->
    <div class="sidebar-header">
      <router-link to="/admin/dashboard" class="brand">
        <span v-if="!isCollapsed" class="brand-text">
          <span class="brand-icon">⚙️</span>
          Admin Panel
        </span>
        <span v-else class="brand-icon-only">⚙️</span>
      </router-link>
    </div>

    <!-- Navigation Menu -->
    <nav class="sidebar-nav">
      <!-- Main Section -->
      <div class="nav-section">
        <span v-if="!isCollapsed" class="section-title">Main</span>

        <NavItem to="/admin/dashboard" icon="dashboard" label="Dashboard" :collapsed="isCollapsed" />
      </div>

      <!-- Management Section -->
      <div class="nav-section">
        <span v-if="!isCollapsed" class="section-title">Management</span>

        <NavItem
          to="/admin/providers"
          icon="building"
          label="Providers"
          :badge="pendingProvidersCount"
          :collapsed="isCollapsed"
        />

        <NavItem to="/admin/users" icon="users" label="Users" :collapsed="isCollapsed" />

        <NavItem
          to="/admin/bookings"
          icon="calendar"
          label="Bookings"
          :collapsed="isCollapsed"
        />

        <NavItem to="/admin/services" icon="services" label="Services" :collapsed="isCollapsed" />
      </div>

      <!-- Analytics Section -->
      <div class="nav-section">
        <span v-if="!isCollapsed" class="section-title">Insights</span>

        <NavItem to="/admin/analytics" icon="chart" label="Analytics" :collapsed="isCollapsed" />

        <NavItem to="/admin/reports" icon="document" label="Reports" :collapsed="isCollapsed" />

        <NavItem to="/admin/reviews" icon="star" label="Reviews" :collapsed="isCollapsed" />
      </div>

      <!-- System Section -->
      <div class="nav-section">
        <span v-if="!isCollapsed" class="section-title">System</span>

        <NavItem to="/admin/settings" icon="settings" label="Settings" :collapsed="isCollapsed" />

        <NavItem to="/admin/logs" icon="list" label="Activity Logs" :collapsed="isCollapsed" />
      </div>
    </nav>

    <!-- Toggle Button -->
    <button class="toggle-btn" @click="$emit('toggle')" :title="isCollapsed ? 'Expand' : 'Collapse'">
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

interface Props {
  isCollapsed: boolean
}

defineProps<Props>()
defineEmits(['toggle'])

// TODO: Get from actual store
const pendingProvidersCount = computed(() => {
  return 3
})
</script>

<style scoped>
.admin-sidebar {
  position: fixed;
  top: 0;
  left: 0;
  height: 100vh;
  width: 260px;
  background: #1e293b;
  color: #e2e8f0;
  display: flex;
  flex-direction: column;
  transition: width 0.3s ease;
  z-index: 100;
  box-shadow: 2px 0 8px rgba(0, 0, 0, 0.1);
}

.admin-sidebar.collapsed {
  width: 80px;
}

.sidebar-header {
  height: 64px;
  display: flex;
  align-items: center;
  padding: 0 1.5rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.brand {
  display: flex;
  align-items: center;
  text-decoration: none;
  color: white;
  font-weight: 600;
  font-size: 1.1rem;
}

.brand-text {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.brand-icon {
  font-size: 1.5rem;
}

.brand-icon-only {
  font-size: 1.75rem;
}

.sidebar-nav {
  flex: 1;
  overflow-y: auto;
  padding: 1rem 0;
}

.sidebar-nav::-webkit-scrollbar {
  width: 6px;
}

.sidebar-nav::-webkit-scrollbar-track {
  background: transparent;
}

.sidebar-nav::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.2);
  border-radius: 3px;
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
  color: #94a3b8;
  letter-spacing: 0.5px;
}

.toggle-btn {
  position: absolute;
  bottom: 1rem;
  right: 1rem;
  width: 40px;
  height: 40px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  background: rgba(255, 255, 255, 0.05);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
  color: #e2e8f0;
}

.toggle-btn:hover {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.2);
}

.toggle-btn svg {
  width: 20px;
  height: 20px;
}
</style>