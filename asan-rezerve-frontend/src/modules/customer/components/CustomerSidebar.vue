<template>
  <aside class="customer-sidebar" :class="{ collapsed }" dir="rtl">
    <div class="sidebar-header">
      <h2 v-if="!collapsed">Booksy</h2>
      <button @click="$emit('update:collapsed', !collapsed)" class="toggle-btn">
        <span>☰</span>
      </button>
    </div>
    
    <nav class="sidebar-nav">
      <router-link to="/customer/dashboard" class="nav-item">
        <span class="icon">🏠</span>
        <span v-if="!collapsed" class="label">داشبورد</span>
      </router-link>
      <router-link to="/customer/browse" class="nav-item">
        <span class="icon">🔍</span>
        <span v-if="!collapsed" class="label">جستجوی خدمات</span>
      </router-link>
      <router-link to="/customer/my-bookings" class="nav-item">
        <span class="icon">📅</span>
        <span v-if="!collapsed" class="label">رزروهای من</span>
      </router-link>
      <router-link to="/customer/favorites" class="nav-item">
        <span class="icon">❤️</span>
        <span v-if="!collapsed" class="label">علاقه‌مندی‌ها</span>
      </router-link>
      <router-link to="/customer/profile" class="nav-item">
        <span class="icon">👤</span>
        <span v-if="!collapsed" class="label">پروفایل</span>
      </router-link>
    </nav>
  </aside>
</template>

<script setup lang="ts">
defineProps<{
  collapsed: boolean
}>()

defineEmits<{
  'update:collapsed': [value: boolean]
}>()
</script>

<style scoped>
.customer-sidebar {
  position: fixed;
  right: 0;
  top: 0;
  width: 280px;
  height: 100vh;
  background: white;
  border-left: 1px solid var(--color-gray-200);
  transition: width 0.3s ease;
  z-index: 1000;
}

.customer-sidebar.collapsed {
  width: 80px;
}

.sidebar-header {
  padding: 1.5rem;
  border-bottom: 1px solid var(--color-gray-200);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.toggle-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
}

.sidebar-nav {
  padding: 1rem 0;
}

.nav-item {
  display: flex;
  align-items: center;
  padding: 1rem 1.5rem;
  color: var(--color-gray-700);
  text-decoration: none;
  transition: all 0.2s;
}

.nav-item:hover {
  background: var(--color-gray-100);
  color: var(--color-primary);
}

.nav-item.router-link-active {
  background: var(--color-primary-50);
  color: var(--color-primary);
  border-right: 3px solid var(--color-primary);
}

.icon {
  font-size: 1.5rem;
  min-width: 2rem;
}

.label {
  margin-right: 1rem;
}
</style>
