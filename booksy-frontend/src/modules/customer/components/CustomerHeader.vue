<template>
  <header class="customer-header" dir="rtl">
    <div class="header-content">
      <h1 class="page-title">{{ pageTitle }}</h1>
      
      <div class="header-actions">
        <button @click="handleNewBooking" class="btn-primary">
          رزرو جدید
        </button>
        <div class="user-menu">
          <span>{{ userName }}</span>
        </div>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const pageTitle = computed(() => route.meta.title || 'داشبورد')
const userName = computed(() => authStore.user?.firstName || 'کاربر')

function handleNewBooking() {
  router.push({ name: 'CustomerBooking' })
}
</script>

<style scoped>
.customer-header {
  background: white;
  border-bottom: 1px solid var(--color-gray-200);
  padding: 1rem 2rem;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 600;
  margin: 0;
}

.header-actions {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.btn-primary {
  padding: 0.75rem 1.5rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 500;
}

.btn-primary:hover {
  background: var(--color-primary-600);
}

.user-menu {
  padding: 0.5rem 1rem;
  background: var(--color-gray-100);
  border-radius: 0.5rem;
}
</style>
