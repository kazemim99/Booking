<template>
  <div class="app-layout" :class="{ 'sidebar-collapsed': isSidebarCollapsed }">
    <!-- Header -->
    <AppHeader @toggle-sidebar="toggleSidebar" :is-sidebar-collapsed="isSidebarCollapsed" />

    <!-- Sidebar -->
    <AppSidebar v-if="showSidebar" :is-collapsed="isSidebarCollapsed" @toggle="toggleSidebar" />

    <!-- Main Content Area -->
    <main class="app-main" :class="{ 'no-sidebar': !showSidebar }">
      <!-- Breadcrumb -->
      <Breadcrumb v-if="showBreadcrumb" class="app-breadcrumb" />

      <!-- Page Content -->
      <div class="app-content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </div>

      <!-- Footer -->
      <AppFooter v-if="showFooter" />
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import AppHeader from './Header/AppHeader.vue'
import AppSidebar from './Sidebar/AppSidebar.vue'
import AppFooter from './Footer/AppFooter.vue'
import Breadcrumb from './Breadcrumb/Breadcrumb.vue'

const route = useRoute()
const isSidebarCollapsed = ref(false)

// Check if sidebar should be shown based on route meta
const showSidebar = computed(() => {
  return route.meta.showSidebar !== false
})

// Check if breadcrumb should be shown
const showBreadcrumb = computed(() => {
  return route.meta.showBreadcrumb !== false
})

// Check if footer should be shown
const showFooter = computed(() => {
  return route.meta.showFooter !== false
})

// Toggle sidebar collapse state
function toggleSidebar(): void {
  isSidebarCollapsed.value = !isSidebarCollapsed.value
  localStorage.setItem('sidebarCollapsed', JSON.stringify(isSidebarCollapsed.value))
}

// Handle responsive sidebar on mobile
function handleResize(): void {
  if (window.innerWidth < 1024) {
    isSidebarCollapsed.value = true
  }
}

// Initialize sidebar state from localStorage
onMounted(() => {
  const savedState = localStorage.getItem('sidebarCollapsed')
  if (savedState !== null) {
    isSidebarCollapsed.value = JSON.parse(savedState)
  }

  // Handle initial resize
  handleResize()
  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  window.removeEventListener('resize', handleResize)
})
</script>

<style scoped lang="scss">
.app-layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: #f3f4f6;
}

.app-main {
  display: flex;
  flex-direction: column;
  flex: 1;
  margin-left: 260px;
  margin-top: 64px;
  transition: margin-left 0.3s ease;

  &.no-sidebar {
    margin-left: 0;
  }

  @media (max-width: 1024px) {
    margin-left: 0;
  }
}

.sidebar-collapsed .app-main {
  margin-left: 80px;

  @media (max-width: 1024px) {
    margin-left: 0;
  }
}

.app-breadcrumb {
  padding: 1rem 2rem;
  background: white;
  border-bottom: 1px solid #e5e7eb;
}

.app-content {
  flex: 1;
  padding: 2rem;
  overflow-y: auto;

  @media (max-width: 768px) {
    padding: 1rem;
  }
}

// Transition animations
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
