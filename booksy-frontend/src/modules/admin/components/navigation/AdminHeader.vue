<template>
  <header class="admin-header">
    <!-- Left Section -->
    <div class="header-left">
      <button class="menu-toggle" @click="$emit('toggle-sidebar')">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M4 6h16M4 12h16M4 18h16"
          />
        </svg>
      </button>

      <h1 class="page-title">{{ pageTitle }}</h1>
    </div>

    <!-- Right Section -->
    <div class="header-right">
      <!-- Notifications -->
      <button class="header-icon-btn" title="Notifications">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
          />
        </svg>
        <span v-if="notificationCount" class="badge">{{ notificationCount }}</span>
      </button>

      <!-- User Menu -->
      <div class="user-menu">
        <button class="user-menu-trigger" @click="toggleUserMenu">
          <img
            v-if="user?.avatarUrl"
            :src="user.avatarUrl"
            :alt="user.fullName"
            class="user-avatar"
          />
          <div v-else class="user-avatar-placeholder">
            {{ userInitials }}
          </div>
          <span class="user-name">{{ user?.fullName || 'Admin' }}</span>
          <svg class="chevron" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M19 9l-7 7-7-7"
            />
          </svg>
        </button>

        <!-- Dropdown Menu -->
        <div v-if="showUserMenu" class="user-dropdown">
          <router-link to="/admin/profile" class="dropdown-item" @click="showUserMenu = false">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
              />
            </svg>
            My Profile
          </router-link>

          <router-link to="/admin/settings" class="dropdown-item" @click="showUserMenu = false">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"
              />
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
              />
            </svg>
            Settings
          </router-link>

          <div class="dropdown-divider"></div>

          <button class="dropdown-item" @click="handleLogout">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
              />
            </svg>
            Logout
          </button>
        </div>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'

defineEmits(['toggle-sidebar'])

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const showUserMenu = ref(false)
const notificationCount = ref(5) // TODO: Get from notification store

const user = computed(() => authStore.user)

const pageTitle = computed(() => {
  return route.meta.title || 'Admin Panel'
})

const userInitials = computed(() => {
  if (!user.value?.fullName) return 'AD'
  return user.value.fullName
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
})

const toggleUserMenu = () => {
  showUserMenu.value = !showUserMenu.value
}

const handleLogout = async () => {
  await authStore.logout()
  router.push({ name: 'Login' })
}
</script>

<style scoped>
.admin-header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 64px;
  background: white;
  border-bottom: 1px solid var(--color-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 2rem;
  z-index: 99;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.menu-toggle {
  display: none;
  width: 40px;
  height: 40px;
  border: none;
  background: transparent;
  cursor: pointer;
  color: var(--color-text-secondary);
}

.menu-toggle svg {
  width: 24px;
  height: 24px;
}

@media (max-width: 1024px) {
  .menu-toggle {
    display: flex;
    align-items: center;
    justify-content: center;
  }
}

.page-title {
  font-size: 1.5rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.header-icon-btn {
  position: relative;
  width: 40px;
  height: 40px;
  border: none;
  background: transparent;
  border-radius: 8px;
  cursor: pointer;
  color: var(--color-text-secondary);
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.header-icon-btn:hover {
  background: var(--color-bg-secondary);
  color: var(--color-text-primary);
}

.header-icon-btn svg {
  width: 20px;
  height: 20px;
}

.badge {
  position: absolute;
  top: 6px;
  right: 6px;
  background: var(--color-danger);
  color: white;
  font-size: 0.7rem;
  font-weight: 600;
  padding: 2px 6px;
  border-radius: 10px;
  min-width: 18px;
  text-align: center;
}

.user-menu {
  position: relative;
}

.user-menu-trigger {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 1rem;
  border: 1px solid var(--color-border);
  background: white;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.user-menu-trigger:hover {
  border-color: var(--color-primary);
}

.user-avatar,
.user-avatar-placeholder {
  width: 32px;
  height: 32px;
  border-radius: 50%;
}

.user-avatar {
  object-fit: cover;
}

.user-avatar-placeholder {
  background: var(--color-primary);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.875rem;
  font-weight: 600;
}

.user-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text-primary);
}

.chevron {
  width: 16px;
  height: 16px;
  color: var(--color-text-secondary);
}

.user-dropdown {
  position: absolute;
  top: calc(100% + 0.5rem);
  right: 0;
  min-width: 200px;
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  padding: 0.5rem 0;
  z-index: 1000;
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  padding: 0.75rem 1rem;
  border: none;
  background: transparent;
  color: var(--color-text-primary);
  text-decoration: none;
  font-size: 0.875rem;
  cursor: pointer;
  transition: background 0.2s;
}

.dropdown-item:hover {
  background: var(--color-bg-secondary);
}

.dropdown-item svg {
  width: 18px;
  height: 18px;
  color: var(--color-text-secondary);
}

.dropdown-divider {
  height: 1px;
  background: var(--color-border);
  margin: 0.5rem 0;
}
</style>
