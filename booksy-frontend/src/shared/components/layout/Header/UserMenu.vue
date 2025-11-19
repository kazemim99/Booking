<template>
  <div class="user-menu" v-click-outside="closeMenu">
    <!-- User Button -->
    <button class="user-button" @click="toggleMenu" aria-label="User Menu">
      <img v-if="user?.avatarUrl" :src="user.avatarUrl" :alt="user.fullName" class="user-avatar" />
      <div v-else class="user-avatar-placeholder" :style="{ background: userColor }">
        {{ userInitials }}
      </div>
      <span class="user-name">{{ user?.firstName || 'مهمان' }}</span>
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
      <div v-if="isMenuOpen" class="dropdown-menu">
        <!-- User Info Section -->
        <div class="menu-header">
          <div class="user-info">
            <div class="user-name-large">{{ user?.fullName }}</div>
            <div class="user-email">{{ user?.email }}</div>
          </div>
        </div>

        <div class="menu-divider"></div>

        <!-- Menu Items -->
        <ul class="menu-list">
          <li v-for="item in itemsToRender" :key="item.name" class="menu-item">
            <button
              v-if="item.action"
              class="menu-link menu-button"
              @click="handleMenuItemClick(item)"
            >
              <span class="menu-icon" v-html="item.icon"></span>
              <span>{{ item.label }}</span>
            </button>
            <router-link
              v-else
              :to="(item.to ?? item.path) as any"
              class="menu-link"
              @click="closeMenu"
            >
              <span class="menu-icon" v-html="item.icon"></span>
              <span>{{ item.label }}</span>
            </router-link>
          </li>
        </ul>

        <div class="menu-divider"></div>

        <!-- Logout Button -->
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

interface MenuItem {
  name: string
  label: string
  path?: string
  to?: unknown
  icon: string
  action?: () => void
}

const router = useRouter()
const authStore = useAuthStore()
const customerStore = useCustomerStore()
const isMenuOpen = ref(false)

// Get user from auth store
const user = computed(() => authStore.user)
const isAuthenticated = computed(() => authStore.isAuthenticated)

// Get user initials for avatar placeholder - using customer store's computed
const userInitials = computed(() => {
  if (customerStore.profile) {
    return customerStore.userInitial
  }
  if (!user.value?.firstName) return 'ک'
  const firstName = user.value.firstName
  return firstName.charAt(0).toUpperCase()
})

// Get user color from customer store
const userColor = computed(() => {
  if (customerStore.profile) {
    return customerStore.userColor
  }
  return '#667eea'
})

// Optional external menu (e.g., Provider/Admin)
const props = defineProps<{ menuItems?: MenuItem[] }>()

// Default menu items for customers - opens modals instead of navigation
const defaultMenuItems: MenuItem[] = [
  {
    name: 'profile',
    label: 'ویرایش پروفایل',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>',
    action: () => {
      closeMenu()
      customerStore.openModal('profile')
    },
  },
  {
    name: 'bookings',
    label: 'نوبت‌های من',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>',
    action: () => {
      closeMenu()
      customerStore.openModal('bookings')
    },
  },
  {
    name: 'favorites',
    label: 'علاقه‌مندی‌ها',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" /></svg>',
    action: () => {
      closeMenu()
      customerStore.openModal('favorites')
    },
  },
  {
    name: 'reviews',
    label: 'نظرات من',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" /></svg>',
    action: () => {
      closeMenu()
      customerStore.openModal('reviews')
    },
  },
  {
    name: 'settings',
    label: 'تنظیمات',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /></svg>',
    action: () => {
      closeMenu()
      customerStore.openModal('settings')
    },
  },
]

const itemsToRender = computed<MenuItem[]>(() => props.menuItems ?? defaultMenuItems)

// Fetch customer profile on mount if authenticated
onMounted(async () => {
  if (isAuthenticated.value && authStore.customerId && !customerStore.profile) {
    try {
      await customerStore.fetchProfile(authStore.customerId)
    } catch (error) {
      console.warn('[UserMenu] Failed to fetch customer profile:', error)
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
  } else if (item.to || item.path) {
    closeMenu()
    router.push((item.to ?? item.path) as any)
  }
}

async function handleLogout(): Promise<void> {
  closeMenu()
  customerStore.resetStore() // Clear customer data
  await authStore.logout()
  // Don't redirect here - let the auth store handle routing
}

// Just add a comment reminding where the directive is
// Note: v-click-outside directive is registered globally in main.ts
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
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
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

/* RTL Support */
[dir='rtl'] .dropdown-menu {
  inset-inline-end: 0;
  inset-inline-start: auto;
}

@media (max-width: 640px) {
  .dropdown-menu {
    width: 260px;
  }
}

.menu-header {
  padding: 1rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.user-info {
  display: flex;
  flex-direction: column;
}

.user-name-large {
  font-weight: 600;
  font-size: 1rem;
  margin-bottom: 0.25rem;
}

.user-email {
  font-size: 0.875rem;
  opacity: 0.9;
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
</style>
