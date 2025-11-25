<template>
  <div class="user-menu" v-click-outside="closeMenu">
    <!-- User Button -->
    <button class="user-button" :class="roleThemeClass" @click="toggleMenu" aria-label="User Menu">
      <img v-if="user?.avatarUrl" :src="user.avatarUrl" :alt="user.fullName" class="user-avatar" />
      <div v-else class="user-avatar-placeholder" :style="{ background: userColor }">
        {{ userInitials }}
      </div>
      <span class="user-name">{{ displayName }}</span>
      <span v-if="showRoleBadge" class="role-badge" :class="roleThemeClass">
        {{ roleLabel }}
      </span>
      <svg
        class="dropdown-arrow"
        :class="{ rotated: isMenuOpen }"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <!-- Dropdown Menu -->
    <transition name="dropdown">
      <div v-if="isMenuOpen" class="dropdown-menu" :class="roleThemeClass">
        <!-- User Info Section -->
        <div class="menu-header" :class="roleThemeClass">
          <div class="user-info">
            <div class="user-name-large">{{ user?.fullName || 'کاربر' }}</div>
            <div class="user-email">{{ user?.email || user?.phoneNumber }}</div>
            <div v-if="userRole === 'provider' && !isProviderOnboardingComplete" class="onboarding-notice">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <span>ثبت‌نام ناقص</span>
            </div>
          </div>
        </div>

        <div class="menu-divider"></div>

        <!-- Menu Items -->
        <ul class="menu-list">
          <li v-for="item in menuItems" :key="item.id" class="menu-item">
            <!-- Router Link Item -->
            <router-link
              v-if="item.path && !item.action"
              :to="item.path"
              class="menu-link"
              :class="{ 'has-badge': item.badge }"
              @click="closeMenu"
            >
              <span class="menu-icon" v-html="item.icon"></span>
              <span class="menu-label">{{ item.label }}</span>
              <span v-if="item.badge" class="menu-badge">{{ item.badge }}</span>
            </router-link>

            <!-- Action Button Item -->
            <button
              v-else-if="item.action"
              class="menu-link menu-button"
              :class="[{ 'has-badge': item.badge }, item.variant === 'danger' ? 'danger' : '']"
              @click="handleMenuItemClick(item)"
            >
              <span class="menu-icon" v-html="item.icon"></span>
              <span class="menu-label">{{ item.label }}</span>
              <span v-if="item.badge" class="menu-badge">{{ item.badge }}</span>
            </button>

            <!-- Divider -->
            <div v-if="item.divider" class="menu-divider"></div>
          </li>
        </ul>

        <!-- Logout Section -->
        <div class="menu-divider"></div>
        <button class="logout-button" @click="handleLogout">
          <span class="menu-icon">
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
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
              />
            </svg>
          </span>
          <span>خروج</span>
        </button>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useCustomerStore } from '@/modules/customer/stores/customer.store'
import { useRoleBasedNavigation, type MenuItem } from '@/shared/composables/useRoleBasedNavigation'

interface Props {
  showRoleBadge?: boolean
}

withDefaults(defineProps<Props>(), {
  showRoleBadge: false,
})

const router = useRouter()
const authStore = useAuthStore()
const customerStore = useCustomerStore()
const { userRole, roleLabel, roleThemeClass, isProviderOnboardingComplete } = useRoleBasedNavigation()

const isMenuOpen = ref(false)

// Get user from auth store
const user = computed(() => authStore.user)
const isAuthenticated = computed(() => authStore.isAuthenticated)

// Display name
const displayName = computed(() => {
  if (!user.value) return 'مهمان'
  return user.value.firstName || user.value.email?.split('@')[0] || 'کاربر'
})

// Get user initials
const userInitials = computed(() => {
  if (customerStore.profile) {
    return customerStore.userInitial
  }
  if (!user.value?.firstName) return 'ک'
  return user.value.firstName.charAt(0).toUpperCase()
})

// Get user color
const userColor = computed(() => {
  if (customerStore.profile) {
    return customerStore.userColor
  }
  // Role-based colors
  switch (userRole.value) {
    case 'admin':
      return '#334155'
    case 'provider':
      return '#1976d2'
    case 'customer':
      return '#667eea'
    default:
      return '#667eea'
  }
})

/**
 * Get menu items based on user role
 */
const menuItems = computed<MenuItem[]>(() => {
  switch (userRole.value) {
    case 'admin':
      return getAdminMenuItems()
    case 'provider':
      return getProviderMenuItems()
    case 'customer':
      return getCustomerMenuItems()
    default:
      return []
  }
})

/**
 * Customer menu items (uses modals)
 */
function getCustomerMenuItems(): MenuItem[] {
  return [
    {
      id: 'profile',
      label: 'پروفایل من',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>',
      action: () => {
        closeMenu()
        customerStore.openModal('profile')
      },
    },
    {
      id: 'bookings',
      label: 'نوبت‌های من',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>',
      action: () => {
        closeMenu()
        customerStore.openModal('bookings')
      },
      // badge: computed(() => customerStore.pendingBookingsCount || undefined),
    },
    {
      id: 'favorites',
      label: 'علاقه‌مندی‌ها',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" /></svg>',
      action: () => {
        closeMenu()
        customerStore.openModal('favorites')
      },
    },
    {
      id: 'reviews',
      label: 'نظرات من',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" /></svg>',
      action: () => {
        closeMenu()
        customerStore.openModal('reviews')
      },
    },
    {
      id: 'settings',
      label: 'تنظیمات',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /></svg>',
      action: () => {
        closeMenu()
        customerStore.openModal('settings')
      },
      divider: true,
    },
  ]
}

/**
 * Provider menu items (uses navigation)
 */
function getProviderMenuItems(): MenuItem[] {
  const items: MenuItem[] = [
    {
      id: 'dashboard',
      label: 'داشبورد',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" /></svg>',
      path: '/dashboard',
    },
    {
      id: 'profile',
      label: 'پروفایل کسب‌وکار',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" /></svg>',
      path: '/provider/profile',
    },
    {
      id: 'bookings',
      label: 'رزروها',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>',
      path: '/provider/bookings',
      // badge: computed(() => providerStore.newBookingsCount || undefined),
    },
    {
      id: 'services',
      label: 'خدمات',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>',
      path: '/provider/services',
    },
    {
      id: 'staff',
      label: 'کارکنان',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" /></svg>',
      path: '/provider/staff',
    },
    {
      id: 'hours',
      label: 'ساعت کاری',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>',
      path: '/provider/hours',
    },
    {
      id: 'settings',
      label: 'تنظیمات',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /></svg>',
      path: '/provider/settings',
      divider: true,
    },
  ]

  // Add "Complete Registration" item if onboarding not complete
  if (!isProviderOnboardingComplete.value) {
    items.unshift({
      id: 'complete-registration',
      label: 'تکمیل ثبت‌نام',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>',
      path: '/provider/registration',
      variant: 'danger',
      divider: true,
    })
  }

  return items
}

/**
 * Admin menu items
 */
function getAdminMenuItems(): MenuItem[] {
  return [
    {
      id: 'dashboard',
      label: 'Dashboard',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" /></svg>',
      path: '/admin/dashboard',
    },
    {
      id: 'users',
      label: 'Users',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" /></svg>',
      path: '/admin/users',
    },
    {
      id: 'providers',
      label: 'Providers',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" /></svg>',
      path: '/admin/providers',
      // badge: computed(() => adminStore.pendingProvidersCount || undefined),
    },
    {
      id: 'bookings',
      label: 'Bookings',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>',
      path: '/admin/bookings',
    },
    {
      id: 'settings',
      label: 'Settings',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /></svg>',
      path: '/admin/settings',
      divider: true,
    },
  ]
}

// Fetch customer profile on mount if authenticated and customer
onMounted(async () => {
  if (isAuthenticated.value && userRole.value === 'customer' && authStore.customerId && !customerStore.profile) {
    try {
      await customerStore.fetchProfile(authStore.customerId)
    } catch (error) {
      console.warn('[RoleBasedUserMenu] Failed to fetch customer profile:', error)
    }
  }
})

function toggleMenu(): void {
  isMenuOpen.value = !isMenuOpen.value
}

function closeMenu(): void {
  isMenuOpen.value = false
}

function handleMenuItemClick(item: MenuItem): void {
  if (item.action) {
    item.action()
  } else if (item.path) {
    closeMenu()
    router.push(item.path)
  }
}

async function handleLogout(): Promise<void> {
  closeMenu()
  if (userRole.value === 'customer') {
    customerStore.resetStore()
  }
  await authStore.logout()
}
</script>

<style scoped lang="scss">
.user-menu {
  position: relative;
}

.user-button {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: none;
  border: none;
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 8px;
  transition: background 0.2s;

  &:hover {
    background: #f3f4f6;
  }

  &.theme-business:hover {
    background: rgba(25, 118, 210, 0.08);
  }

  &.theme-admin:hover {
    background: rgba(51, 65, 85, 0.08);
  }
}

.user-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #e5e7eb;
}

.user-avatar-placeholder {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 0.875rem;
  border: 2px solid #e5e7eb;
}

.user-name {
  font-weight: 500;
  color: #374151;

  @media (max-width: 640px) {
    display: none;
  }
}

.role-badge {
  font-size: 0.75rem;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-weight: 600;

  &.theme-customer {
    background: #ede9fe;
    color: #667eea;
  }

  &.theme-business {
    background: #e3f2fd;
    color: #1976d2;
  }

  &.theme-admin {
    background: #f1f5f9;
    color: #334155;
  }
}

.dropdown-arrow {
  width: 20px;
  height: 20px;
  color: #9ca3af;
  transition: transform 0.2s;

  &.rotated {
    transform: rotate(180deg);
  }
}

.dropdown-menu {
  position: absolute;
  top: calc(100% + 0.5rem);
  inset-inline-end: 0;
  width: 280px;
  max-width: calc(100vw - 2rem);
  background: white;
  border-radius: 12px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
  overflow: hidden;
  z-index: 100;
}

.menu-header {
  padding: 1rem;
  color: white;

  &:not(.theme-business):not(.theme-admin) {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  }

  &.theme-business {
    background: linear-gradient(135deg, #1976d2 0%, #0d47a1 100%);
  }

  &.theme-admin {
    background: linear-gradient(135deg, #334155 0%, #0f172a 100%);
  }
}

.user-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.user-name-large {
  font-weight: 600;
  font-size: 1rem;
}

.user-email {
  font-size: 0.875rem;
  opacity: 0.9;
}

.onboarding-notice {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-top: 0.5rem;
  padding: 0.5rem;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 6px;
  font-size: 0.875rem;

  svg {
    width: 16px;
    height: 16px;
  }
}

.menu-divider {
  height: 1px;
  background: #e5e7eb;
}

.menu-list {
  list-style: none;
  margin: 0;
  padding: 0.5rem 0;
}

.menu-link {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  color: #374151;
  text-decoration: none;
  transition: background 0.2s;

  &:hover {
    background: #f3f4f6;
  }

  &.has-badge {
    justify-content: space-between;
  }

  &.danger {
    color: #ef4444;

    &:hover {
      background: #fef2f2;
    }
  }
}

.menu-button {
  width: 100%;
  border: none;
  background: none;
  cursor: pointer;
  text-align: start;
  font-size: inherit;
  font-family: inherit;
}

.menu-icon {
  display: flex;
  align-items: center;
  color: #6b7280;

  :deep(svg) {
    width: 20px;
    height: 20px;
  }
}

.menu-label {
  flex: 1;
}

.menu-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 20px;
  height: 20px;
  padding: 0 0.375rem;
  background: #ef4444;
  color: white;
  border-radius: 10px;
  font-size: 0.75rem;
  font-weight: 600;
}

.logout-button {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  background: none;
  border: none;
  color: #ef4444;
  font-weight: 500;
  cursor: pointer;
  text-align: left;
  transition: background 0.2s;

  &:hover {
    background: #fef2f2;
  }
}

// Dropdown transition
.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.2s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

@media (max-width: 640px) {
  .dropdown-menu {
    width: 260px;
  }

  .role-badge {
    display: none;
  }
}
</style>
