<template>
  <div class="admin-user-menu" v-click-outside="closeMenu">
    <!-- Menu Trigger -->
    <button
      class="user-menu-trigger"
      :class="{ active: isOpen }"
      @click="toggleMenu"
      aria-haspopup="true"
      :aria-expanded="isOpen"
    >
      <!-- Avatar -->
      <div class="avatar-wrapper">
        <img v-if="user?.avatarUrl" :src="user.avatarUrl" :alt="userFullName" class="user-avatar" />
        <div v-else class="user-avatar-placeholder">
          <span>{{ userInitials }}</span>
        </div>
        <div v-if="isOnline" class="online-indicator" title="Online"></div>
      </div>

      <!-- User Info -->
      <div class="user-info">
        <span class="user-name">{{ userFullName }}</span>
        <span class="user-role">{{ userRole }}</span>
      </div>

      <!-- Chevron Icon -->
      <svg
        class="chevron-icon"
        :class="{ rotated: isOpen }"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <!-- Dropdown Menu -->
    <transition name="dropdown">
      <div v-if="isOpen" class="user-dropdown" role="menu">
        <!-- User Info Header -->
        <div class="dropdown-header">
          <div class="header-avatar">
            <img v-if="user?.avatarUrl" :src="user.avatarUrl" :alt="userFullName" />
            <div v-else class="avatar-placeholder">
              {{ userInitials }}
            </div>
          </div>
          <div class="header-info">
            <div class="header-name">{{ userFullName }}</div>
            <div class="header-email">{{ user?.email }}</div>
          </div>
        </div>

        <div class="dropdown-divider"></div>

        <!-- Menu Items -->
        <nav class="dropdown-nav">
          <!-- Profile -->
          <router-link to="/profile" class="dropdown-item" @click="handleMenuClick" role="menuitem">
            <span class="item-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                />
              </svg>
            </span>
            <span class="item-label">My Profile</span>
            <span class="item-badge">View</span>
          </router-link>

          <!-- Account Settings -->
          <router-link
            to="/settings"
            class="dropdown-item"
            @click="handleMenuClick"
            role="menuitem"
          >
            <span class="item-icon">
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
            </span>
            <span class="item-label">Settings</span>
          </router-link>

          <!-- Notifications -->
          <router-link
            to="/notifications"
            class="dropdown-item"
            @click="handleMenuClick"
            role="menuitem"
          >
            <span class="item-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
                />
              </svg>
            </span>
            <span class="item-label">Notifications</span>
            <span v-if="notificationCount > 0" class="item-badge badge-danger">
              {{ notificationCount }}
            </span>
          </router-link>

          <!-- Help & Support -->
          <router-link to="/help" class="dropdown-item" @click="handleMenuClick" role="menuitem">
            <span class="item-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </span>
            <span class="item-label">Help & Support</span>
          </router-link>
        </nav>

        <div class="dropdown-divider"></div>

        <!-- Theme Toggle -->
        <div class="dropdown-section">
          <button class="dropdown-item" @click="toggleTheme" role="menuitem">
            <span class="item-icon">
              <svg v-if="isDarkMode" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"
                />
              </svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"
                />
              </svg>
            </span>
            <span class="item-label">{{ isDarkMode ? 'Light Mode' : 'Dark Mode' }}</span>
            <div class="theme-toggle">
              <div class="toggle-track" :class="{ active: isDarkMode }">
                <div class="toggle-thumb"></div>
              </div>
            </div>
          </button>
        </div>

        <div class="dropdown-divider"></div>

        <!-- Logout -->
        <div class="dropdown-section">
          <button class="dropdown-item logout-item" @click="handleLogout" role="menuitem">
            <span class="item-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
                />
              </svg>
            </span>
            <span class="item-label">Logout</span>
          </button>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useUiStore } from '@/core/stores/modules/ui.store'
import { vClickOutside } from '@/shared/directives/v-click-outside'

// Stores
const router = useRouter()
const authStore = useAuthStore()
const uiStore = useUiStore()

// State
const isOpen = ref(false)
const notificationCount = ref(3) // TODO: Get from notification store

// Computed
const user = computed(() => authStore.user)

const userFullName = computed(() => {
  if (!user.value) return 'Admin User'
  return user.value.fullName || `${user.value.firstName} ${user.value.lastName}`.trim() || 'User'
})

const userInitials = computed(() => {
  if (!user.value?.fullName) return 'AU'
  const names = user.value.fullName.split(' ')
  if (names.length >= 2) {
    return `${names[0][0]}${names[1][0]}`.toUpperCase()
  }
  return user.value.fullName.substring(0, 2).toUpperCase()
})

const userRole = computed(() => {
  if (!user.value?.roles || user.value.roles.length === 0) return 'User'
  // Get the primary role
  const roleMap: Record<string, string> = {
    Administrator: 'Admin',
    SysAdmin: 'System Admin',
    Provider: 'Provider',
    Client: 'Client',
  }
  return roleMap[user.value.roles[0]] || user.value.roles[0]
})

const isOnline = computed(() => true) // TODO: Implement online status

const isDarkMode = computed(() => uiStore.theme === 'dark')

// Methods
function toggleMenu() {
  isOpen.value = !isOpen.value
}

function closeMenu() {
  isOpen.value = false
}

function handleMenuClick() {
  closeMenu()
}

function toggleTheme() {
  uiStore.toggleTheme()
}

async function handleLogout() {
  closeMenu()

  // Show confirmation
  if (confirm('Are you sure you want to logout?')) {
    await authStore.logout()
    router.push({ name: 'Login' })
  }
}
</script>

<style scoped lang="scss">
.admin-user-menu {
  position: relative;
}

// Menu Trigger
.user-menu-trigger {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 1rem;
  border: 1px solid var(--color-border);
  background: white;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.2s ease;
  min-width: 200px;

  &:hover {
    border-color: var(--color-primary);
    box-shadow: 0 2px 8px rgba(99, 102, 241, 0.1);
  }

  &.active {
    border-color: var(--color-primary);
    box-shadow: 0 2px 8px rgba(99, 102, 241, 0.15);
  }

  &:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
  }
}

.avatar-wrapper {
  position: relative;
  flex-shrink: 0;
}

.user-avatar,
.user-avatar-placeholder {
  width: 36px;
  height: 36px;
  border-radius: 50%;
}

.user-avatar {
  object-fit: cover;
  border: 2px solid var(--color-border);
}

.user-avatar-placeholder {
  background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.875rem;
  font-weight: 600;
  border: 2px solid transparent;
}

.online-indicator {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 10px;
  height: 10px;
  background: #10b981;
  border: 2px solid white;
  border-radius: 50%;
}

.user-info {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  flex: 1;
  min-width: 0;
}

.user-name {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
}

.user-role {
  font-size: 0.75rem;
  color: var(--color-text-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
}

.chevron-icon {
  width: 18px;
  height: 18px;
  color: var(--color-text-secondary);
  transition: transform 0.2s ease;
  flex-shrink: 0;

  &.rotated {
    transform: rotate(180deg);
  }
}

// Dropdown Menu
.user-dropdown {
  position: absolute;
  top: calc(100% + 0.5rem);
  right: 0;
  min-width: 280px;
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 12px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.12);
  padding: 0.5rem;
  z-index: 1000;
  max-height: 600px;
  overflow-y: auto;

  /* Custom Scrollbar */
  &::-webkit-scrollbar {
    width: 6px;
  }

  &::-webkit-scrollbar-track {
    background: transparent;
  }

  &::-webkit-scrollbar-thumb {
    background: var(--color-border);
    border-radius: 3px;

    &:hover {
      background: var(--color-text-secondary);
    }
  }
}

// Dropdown Header
.dropdown-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.05) 0%, rgba(139, 92, 246, 0.05) 100%);
  border-radius: 8px;
  margin-bottom: 0.5rem;
}

.header-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  overflow: hidden;
  flex-shrink: 0;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .avatar-placeholder {
    width: 100%;
    height: 100%;
    background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.25rem;
    font-weight: 600;
  }
}

.header-info {
  flex: 1;
  min-width: 0;
}

.header-name {
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--color-text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-bottom: 0.25rem;
}

.header-email {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

// Dropdown Navigation
.dropdown-nav {
  padding: 0.25rem 0;
}

.dropdown-section {
  padding: 0.25rem 0;
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
  transition: all 0.2s ease;
  border-radius: 8px;
  position: relative;

  &:hover {
    background: var(--color-background);
    color: var(--color-primary);

    .item-icon {
      color: var(--color-primary);
    }
  }

  &:focus {
    outline: none;
    background: var(--color-background);
  }

  &.router-link-active {
    background: rgba(99, 102, 241, 0.08);
    color: var(--color-primary);

    .item-icon {
      color: var(--color-primary);
    }
  }

  &.logout-item {
    color: var(--color-danger);

    &:hover {
      background: rgba(239, 68, 68, 0.08);
      color: var(--color-danger);

      .item-icon {
        color: var(--color-danger);
      }
    }
  }
}

.item-icon {
  width: 20px;
  height: 20px;
  color: var(--color-text-secondary);
  transition: color 0.2s ease;
  flex-shrink: 0;

  svg {
    width: 100%;
    height: 100%;
    stroke-width: 2;
  }
}

.item-label {
  flex: 1;
  font-weight: 500;
  text-align: left;
}

.item-badge {
  padding: 0.25rem 0.5rem;
  background: var(--color-background);
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--color-text-secondary);

  &.badge-danger {
    background: var(--color-danger);
    color: white;
  }
}

// Theme Toggle
.theme-toggle {
  margin-left: auto;
}

.toggle-track {
  width: 42px;
  height: 24px;
  background: #e5e7eb;
  border-radius: 12px;
  position: relative;
  transition: background 0.3s ease;
  cursor: pointer;

  &.active {
    background: var(--color-primary);

    .toggle-thumb {
      transform: translateX(18px);
    }
  }
}

.toggle-thumb {
  position: absolute;
  top: 3px;
  left: 3px;
  width: 18px;
  height: 18px;
  background: white;
  border-radius: 50%;
  transition: transform 0.3s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

// Divider
.dropdown-divider {
  height: 1px;
  background: var(--color-border);
  margin: 0.5rem 0;
}

// Dropdown Animation
.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.2s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-10px) scale(0.95);
}

.dropdown-enter-to,
.dropdown-leave-from {
  opacity: 1;
  transform: translateY(0) scale(1);
}

// Responsive
@media (max-width: 768px) {
  .user-menu-trigger {
    min-width: auto;
    padding: 0.5rem;
  }

  .user-info {
    display: none;
  }

  .user-dropdown {
    right: 0;
    left: auto;
    min-width: 260px;
  }
}

@media (max-width: 480px) {
  .user-dropdown {
    position: fixed;
    top: 64px !important;
    right: 0.5rem;
    left: 0.5rem;
    width: auto;
    min-width: auto;
  }
}
</style>
