<template>
  <header class="app-header">
    <div class="header-left">
      <!-- Sidebar Toggle Button -->
      <button class="sidebar-toggle" @click="$emit('toggle-sidebar')" aria-label="Toggle Sidebar">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M4 6h16M4 12h16M4 18h16"
          />
        </svg>
      </button>

      <!-- Logo -->
      <router-link to="/" class="logo">
        <span class="logo-text">Booksy</span>
      </router-link>

      <!-- Main Navigation -->
      <HeaderNav class="header-nav" />
    </div>

    <div class="header-right">
      <!-- Search Bar (Optional) -->
      <div class="search-bar" v-if="showSearch">
        <input type="text" placeholder="Search..." v-model="searchQuery" @input="handleSearch" />
        <svg
          class="search-icon"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
          />
        </svg>
      </div>

      <!-- Notification Bell -->
      <button class="notification-btn" @click="toggleNotifications" aria-label="Notifications">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
          />
        </svg>
        <span v-if="unreadNotifications > 0" class="notification-badge">
          {{ unreadNotifications }}
        </span>
      </button>

      <!-- User Menu -->
      <UserMenu />
    </div>
  </header>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import HeaderNav from './HeaderNav.vue'
import UserMenu from './UserMenu.vue'

interface Props {
  isSidebarCollapsed?: boolean
  showSearch?: boolean
}

defineProps<Props>()

defineEmits<{
  'toggle-sidebar': []
}>()

const router = useRouter()
const searchQuery = ref('')
const unreadNotifications = ref(3) // This should come from a store

function handleSearch(): void {
  if (searchQuery.value.trim()) {
    router.push({ name: 'Search', query: { q: searchQuery.value } })
  }
}

function toggleNotifications(): void {
  // Navigate to notifications or toggle dropdown
  router.push({ name: 'Notifications' })
}
</script>

<style scoped lang="scss">
.app-header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 64px;
  background: white;
  border-bottom: 1px solid #e5e7eb;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 1.5rem;
  z-index: 1000;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex: 1;
}

.sidebar-toggle {
  background: none;
  border: none;
  cursor: pointer;
  color: #6b7280;
  padding: 0.5rem;
  display: flex;
  align-items: center;
  transition: color 0.2s;

  svg {
    width: 24px;
    height: 24px;
  }

  &:hover {
    color: #667eea;
  }
}

.logo {
  display: flex;
  align-items: center;
  text-decoration: none;
  font-size: 1.5rem;
  font-weight: 700;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  transition: opacity 0.2s;

  &:hover {
    opacity: 0.8;
  }
}

.header-nav {
  @media (max-width: 768px) {
    display: none;
  }
}

.header-right {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.search-bar {
  position: relative;
  display: flex;
  align-items: center;

  input {
    padding: 0.5rem 2.5rem 0.5rem 1rem;
    border: 1px solid #d1d5db;
    border-radius: 8px;
    outline: none;
    width: 250px;
    transition: all 0.2s;

    &:focus {
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }
  }

  .search-icon {
    position: absolute;
    right: 0.75rem;
    width: 20px;
    height: 20px;
    color: #9ca3af;
    pointer-events: none;
  }

  @media (max-width: 1024px) {
    display: none;
  }
}

.notification-btn {
  position: relative;
  background: none;
  border: none;
  cursor: pointer;
  color: #6b7280;
  padding: 0.5rem;
  display: flex;
  align-items: center;
  transition: color 0.2s;

  svg {
    width: 24px;
    height: 24px;
  }

  &:hover {
    color: #667eea;
  }
}

.notification-badge {
  position: absolute;
  top: 0;
  right: 0;
  background: #ef4444;
  color: white;
  border-radius: 9999px;
  font-size: 0.625rem;
  font-weight: 600;
  padding: 0.125rem 0.375rem;
  min-width: 18px;
  text-align: center;
}
</style>
