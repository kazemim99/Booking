<template>
  <nav v-if="isAuthenticated" class="bottom-navigation">
    <button
      v-for="item in navItems"
      :key="item.name"
      @click="handleNavClick(item)"
      class="nav-item"
      :class="{ active: activeItem === item.name }"
      :aria-label="item.label"
    >
      <div class="nav-icon" v-html="item.icon"></div>
      <span class="nav-label">{{ item.label }}</span>
      <span v-if="item.badge" class="nav-badge">{{ item.badge }}</span>
    </button>
  </nav>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useCustomerStore } from '@/modules/customer/stores/customer.store'

interface NavItem {
  name: string
  label: string
  icon: string
  action?: () => void
  route?: string
  badge?: number
}

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const customerStore = useCustomerStore()

const isAuthenticated = computed(() => authStore.isAuthenticated)
const activeItem = ref<string>('home')

const navItems: NavItem[] = [
  {
    name: 'home',
    label: 'خانه',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" /></svg>',
    route: '/'
  },
  {
    name: 'search',
    label: 'جستجو',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>',
    route: '/providers/search'
  },
  {
    name: 'bookings',
    label: 'نوبت‌ها',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>',
    action: () => customerStore.openModal('bookings'),
    badge: computed(() => customerStore.upcomingBookingsCount)
  },
  {
    name: 'favorites',
    label: 'علاقه‌مندی‌ها',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" /></svg>',
    action: () => customerStore.openModal('favorites'),
    badge: computed(() => customerStore.favoritesCount)
  },
  {
    name: 'profile',
    label: 'پروفایل',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>',
    action: () => customerStore.openModal('profile')
  }
]

function handleNavClick(item: NavItem) {
  activeItem.value = item.name

  if (item.action) {
    item.action()
  } else if (item.route) {
    router.push(item.route)
  }
}

// Update active item based on current route
router.afterEach((to) => {
  if (to.path === '/') {
    activeItem.value = 'home'
  } else if (to.path.includes('/providers/search')) {
    activeItem.value = 'search'
  }
})
</script>

<style scoped lang="scss">
.bottom-navigation {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  display: none;
  background: rgba(255, 255, 255, 0.98);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border-top: 1px solid rgba(229, 231, 235, 0.8);
  padding: 0.5rem 0 calc(0.5rem + env(safe-area-inset-bottom));
  z-index: 1000;
  box-shadow: 0 -4px 20px rgba(0, 0, 0, 0.08);

  @media (max-width: 768px) {
    display: flex;
    justify-content: space-around;
    align-items: center;
  }
}

.nav-item {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.25rem;
  flex: 1;
  padding: 0.5rem;
  background: none;
  border: none;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: #9ca3af;
  min-width: 60px;
  max-width: 80px;

  &:active {
    transform: scale(0.95);
  }

  &.active {
    color: #667eea;

    .nav-icon {
      transform: translateY(-2px);
    }

    .nav-label {
      font-weight: 600;
    }
  }
}

.nav-icon {
  width: 24px;
  height: 24px;
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);

  :deep(svg) {
    width: 100%;
    height: 100%;
    stroke-width: 2;
  }
}

.nav-label {
  font-size: 0.625rem;
  font-weight: 500;
  text-align: center;
  transition: all 0.3s;
  white-space: nowrap;
}

.nav-badge {
  position: absolute;
  top: 0.25rem;
  right: 50%;
  transform: translateX(8px);
  min-width: 18px;
  height: 18px;
  padding: 0 4px;
  background: #ef4444;
  color: white;
  border-radius: 9px;
  font-size: 0.625rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 4px rgba(239, 68, 68, 0.3);
}

// RTL Support
[dir='rtl'] {
  .nav-badge {
    right: auto;
    left: 50%;
    transform: translateX(-8px);
  }
}

// Animation for when navigation appears
@keyframes slideUp {
  from {
    transform: translateY(100%);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.bottom-navigation {
  animation: slideUp 0.3s ease-out;
}
</style>
