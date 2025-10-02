<template>
  <nav class="sidebar-menu">
    <ul class="menu-list">
      <li v-for="item in filteredMenuItems" :key="item.name" class="menu-item">
        <!-- Regular Menu Item -->
        <router-link
          v-if="!item.children"
          :to="item.path"
          class="menu-link"
          :class="{ active: isActive(item.path) }"
        >
          <span class="menu-icon" v-html="item.icon"></span>
          <span v-if="!isCollapsed" class="menu-text">{{ item.label }}</span>
          <span v-if="item.badge && !isCollapsed" class="menu-badge">
            {{ item.badge }}
          </span>
        </router-link>

        <!-- Menu Item with Children (Expandable) -->
        <div v-else>
          <button
            class="menu-link expandable"
            :class="{
              active: isActiveParent(item.path),
              expanded: expandedItems.includes(item.name),
            }"
            @click="toggleExpand(item.name)"
          >
            <span class="menu-icon" v-html="item.icon"></span>
            <span v-if="!isCollapsed" class="menu-text">{{ item.label }}</span>
            <svg
              v-if="!isCollapsed"
              class="expand-arrow"
              :class="{ rotated: expandedItems.includes(item.name) }"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M19 9l-7 7-7-7"
              />
            </svg>
          </button>

          <!-- Submenu -->
          <transition name="submenu">
            <ul v-if="!isCollapsed && expandedItems.includes(item.name)" class="submenu">
              <li v-for="child in item.children" :key="child.name" class="submenu-item">
                <router-link
                  :to="child.path"
                  class="submenu-link"
                  :class="{ active: isActive(child.path) }"
                >
                  <span class="submenu-text">{{ child.label }}</span>
                  <span v-if="child.badge" class="menu-badge">{{ child.badge }}</span>
                </router-link>
              </li>
            </ul>
          </transition>
        </div>
      </li>
    </ul>

    <!-- Sidebar Footer (Optional) -->
    <div v-if="!isCollapsed" class="sidebar-footer">
      <div class="footer-info">
        <p class="footer-version">Version 1.0.0</p>
        <p class="footer-copyright">© 2025 Booksy</p>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'

interface MenuChild {
  name: string
  label: string
  path: string
  badge?: string | number
}

interface MenuItem {
  name: string
  label: string
  path: string
  icon: string
  badge?: string | number
  requiredRole?: string[]
  children?: MenuChild[]
}

interface Props {
  isCollapsed?: boolean
}

defineProps<Props>()

const route = useRoute()
const authStore = useAuthStore()
const expandedItems = ref<string[]>([])

// Define menu items
const allMenuItems: MenuItem[] = [
  {
    name: 'dashboard',
    label: 'Dashboard',
    path: '/',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" /></svg>',
  },
  {
    name: 'bookings',
    label: 'Bookings',
    path: '/bookings',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>',
    badge: 3,
  },
  {
    name: 'services',
    label: 'Services',
    path: '/services',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>',
  },
  {
    name: 'providers',
    label: 'Providers',
    path: '/providers',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" /></svg>',
  },
  {
    name: 'reviews',
    label: 'Reviews',
    path: '/reviews',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" /></svg>',
  },
  {
    name: 'admin',
    label: 'Admin',
    path: '/admin',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /></svg>',
    requiredRole: ['Admin'],
    children: [
      { name: 'users', label: 'Users', path: '/admin/users' },
      { name: 'reports', label: 'Reports', path: '/admin/reports' },
      { name: 'settings', label: 'Settings', path: '/admin/settings' },
    ],
  },
]

// Filter menu items based on user role
const filteredMenuItems = computed(() => {
  return allMenuItems.filter((item) => {
    if (!item.requiredRole) return true
    return item.requiredRole.some((role) => authStore.hasRole(role))
  })
})

// Check if the current route matches the menu item
function isActive(path: string): boolean {
  return route.path === path
}

// Check if any child is active
function isActiveParent(path: string): boolean {
  return route.path.startsWith(path)
}

// Toggle expand/collapse for menu items with children
function toggleExpand(itemName: string): void {
  const index = expandedItems.value.indexOf(itemName)
  if (index > -1) {
    expandedItems.value.splice(index, 1)
  } else {
    expandedItems.value.push(itemName)
  }
}
</script>

<style scoped lang="scss">
.sidebar-menu {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.menu-list {
  list-style: none;
  margin: 0;
  padding: 0;
  flex: 1;
}

.menu-item {
  margin-bottom: 0.25rem;
}

.menu-link {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.875rem 1.25rem;
  color: #6b7280;
  text-decoration: none;
  transition: all 0.2s;
  cursor: pointer;
  border: none;
  background: none;
  width: 100%;
  text-align: left;
  font-size: 0.875rem;
  position: relative;

  &:hover {
    background: #f3f4f6;
    color: #667eea;
  }

  &.active {
    background: #ede9fe;
    color: #667eea;
    font-weight: 600;

    &::before {
      content: '';
      position: absolute;
      left: 0;
      top: 0;
      bottom: 0;
      width: 3px;
      background: #667eea;
    }
  }

  &.expandable {
    justify-content: space-between;
  }
}

.menu-icon {
  display: flex;
  align-items: center;
  flex-shrink: 0;

  :deep(svg) {
    width: 22px;
    height: 22px;
  }
}

.menu-text {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.menu-badge {
  background: #ef4444;
  color: white;
  border-radius: 9999px;
  font-size: 0.625rem;
  font-weight: 600;
  padding: 0.125rem 0.5rem;
  min-width: 20px;
  text-align: center;
}

.expand-arrow {
  width: 16px;
  height: 16px;
  transition: transform 0.2s;
  flex-shrink: 0;

  &.rotated {
    transform: rotate(180deg);
  }
}

// Submenu styles
.submenu {
  list-style: none;
  margin: 0;
  padding: 0;
  background: #f9fafb;
}

.submenu-item {
  margin: 0;
}

.submenu-link {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1.25rem 0.75rem 3.5rem;
  color: #6b7280;
  text-decoration: none;
  font-size: 0.875rem;
  transition: all 0.2s;

  &:hover {
    background: #f3f4f6;
    color: #667eea;
  }

  &.active {
    background: #ede9fe;
    color: #667eea;
    font-weight: 600;
  }
}

.submenu-text {
  flex: 1;
}

// Submenu transition
.submenu-enter-active,
.submenu-leave-active {
  transition: all 0.2s ease;
  max-height: 300px;
  overflow: hidden;
}

.submenu-enter-from,
.submenu-leave-to {
  max-height: 0;
  opacity: 0;
}

// Sidebar Footer
.sidebar-footer {
  padding: 1rem 1.25rem;
  border-top: 1px solid #e5e7eb;
  margin-top: auto;
}

.footer-info {
  text-align: center;
}

.footer-version,
.footer-copyright {
  margin: 0;
  font-size: 0.75rem;
  color: #9ca3af;
  line-height: 1.5;
}
</style>
