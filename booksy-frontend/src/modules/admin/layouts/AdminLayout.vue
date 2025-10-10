<template>
  <div class="admin-layout">
    <!-- Admin Header -->
    <AdminHeader @toggle-sidebar="toggleSidebar" />

    <!-- Admin Sidebar -->
    <AdminSidebar :is-collapsed="isSidebarCollapsed" @toggle="toggleSidebar" />

    <!-- Main Content -->
    <main class="admin-main" :class="{ 'sidebar-collapsed': isSidebarCollapsed }">
      <!-- Page Content -->
      <div class="admin-content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </div>
      <AdminFooter />
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import AdminHeader from '../components/navigation/AdminHeader.vue'
import AdminSidebar from '../components/navigation/AdminSidebar.vue'
import AdminFooter from '../components/navigation/AdminFooter.vue'

const isSidebarCollapsed = ref(false)

const toggleSidebar = () => {
  isSidebarCollapsed.value = !isSidebarCollapsed.value
  localStorage.setItem('adminSidebarCollapsed', JSON.stringify(isSidebarCollapsed.value))
}

onMounted(() => {
  const saved = localStorage.getItem('adminSidebarCollapsed')
  if (saved) {
    isSidebarCollapsed.value = JSON.parse(saved)
  }
})
</script>

<style scoped>
.admin-layout {
  display: flex;
  min-height: 100vh;
  background: var(--color-bg-secondary);
}

.admin-main {
  flex: 1;
  margin-left: 260px;
  margin-top: 64px;
  transition: margin-left 0.3s ease;
}

.admin-main.sidebar-collapsed {
  margin-left: 80px;
}

.admin-content {
  padding: 2rem;
  max-width: 1600px;
  margin: 0 auto;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

@media (max-width: 1024px) {
  .admin-main {
    margin-left: 0;
  }
}
</style>
