<template>
  <div class="customer-layout" dir="rtl">
    <CustomerSidebar v-model:collapsed="sidebarCollapsed" />
    
    <div class="main-content" :class="{ 'sidebar-collapsed': sidebarCollapsed }">
      <CustomerHeader />
      
      <main class="page-content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import CustomerSidebar from '../components/CustomerSidebar.vue'
import CustomerHeader from '../components/CustomerHeader.vue'

const sidebarCollapsed = ref(false)
</script>

<style scoped>
.customer-layout {
  display: flex;
  min-height: 100vh;
  background: var(--color-gray-50);
}

.main-content {
  flex: 1;
  margin-right: 280px;
  transition: margin 0.3s ease;
}

.main-content.sidebar-collapsed {
  margin-right: 80px;
}

.page-content {
  padding: 2rem;
  min-height: calc(100vh - 64px);
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
