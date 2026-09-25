<template>
  <router-link
    :to="to"
    class="nav-item"
    :class="{ active: isActive, collapsed }"
    @click="$emit('click')"
  >
    <div class="nav-icon">
      <svg v-if="icon === 'dashboard'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2H5a2 2 0 00-2-2z" />
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 5a2 2 0 012-2h4a2 2 0 012 2v6H8V5z" />
      </svg>
      <svg v-else-if="icon === 'calendar'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
      <svg v-else-if="icon === 'services'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
      </svg>
      <svg v-else-if="icon === 'users'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z" />
      </svg>
      <svg v-else-if="icon === 'building'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
      </svg>
      <svg v-else-if="icon === 'clock'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <svg v-else-if="icon === 'image'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
      <svg v-else-if="icon === 'chart'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
      </svg>
      <svg v-else-if="icon === 'star'" viewBox="0 0 24 24" fill="none" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
      </svg>
    </div>
    
    <span v-if="!collapsed" class="nav-label">{{ label }}</span>
    
    <div v-if="badge && !collapsed" class="nav-badge">{{ badge }}</div>
  </router-link>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

interface Props {
  to: string
  icon: string
  label: string
  badge?: string | number
  collapsed?: boolean
}

const props = defineProps<Props>()
defineEmits<{
  click: []
}>()

const route = useRoute()

// Check if current route is active
const isActive = computed(() => {
  return route.path.startsWith(String(props.to))
})
</script>

<style scoped>
.nav-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-lg);
  margin: var(--spacing-xs) var(--spacing-md);
  color: var(--color-text-secondary);
  text-decoration: none;
  border-radius: var(--radius-lg);
  transition: all var(--transition-fast);
  position: relative;
  cursor: pointer;
  border: 2px solid transparent;
}

.nav-item:hover {
  background: var(--color-gray-100);
  color: var(--color-text-primary);
  transform: translateX(4px);
  border-color: var(--color-gray-200);
}

.nav-item:active {
  transform: scale(0.98);
}

.nav-item.active {
  background: linear-gradient(135deg, var(--color-primary-50) 0%, var(--color-primary-100) 100%);
  color: var(--color-primary-600);
  font-weight: var(--font-weight-semibold);
  box-shadow: var(--shadow-primary);
  border-color: var(--color-primary-200);
}

.nav-item.active::before {
  content: '';
  position: absolute;
  inset-inline-start: 0;
  top: 50%;
  transform: translateY(-50%);
  width: 4px;
  height: 60%;
  background: var(--gradient-primary);
  border-radius: 0 var(--radius-md) var(--radius-md) 0;
}

.nav-item.collapsed {
  justify-content: center;
  padding: var(--spacing-sm);
  margin: var(--spacing-xs) var(--spacing-sm);
}

.nav-item.collapsed:hover {
  transform: scale(1.1);
}

.nav-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  flex-shrink: 0;
}

.nav-icon svg {
  width: 100%;
  height: 100%;
  stroke-width: 2.5;
}

.nav-item.active .nav-icon svg {
  stroke-width: 3;
}

.nav-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  white-space: nowrap;
  flex: 1;
}

.nav-badge {
  background: var(--gradient-danger);
  color: var(--color-text-inverse);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-bold);
  padding: 2px var(--spacing-xs);
  border-radius: var(--radius-full);
  min-width: 20px;
  text-align: center;
  margin-inline-start: auto;
  box-shadow: var(--shadow-sm);
}

.nav-item.collapsed .nav-badge {
  position: absolute;
  top: 4px;
  inset-inline-end: 4px;
  margin-inline-start: 0;
  min-width: 18px;
  height: 18px;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 10px;
}

/* Tooltip for collapsed state */
.nav-item.collapsed::after {
  content: attr(aria-label);
  position: absolute;
  inset-inline-start: 100%;
  top: 50%;
  transform: translateY(-50%);
  margin-inline-start: var(--spacing-sm);
  padding: var(--spacing-xs) var(--spacing-sm);
  background: var(--color-gray-900);
  color: var(--color-text-inverse);
  font-size: var(--font-size-xs);
  border-radius: var(--radius-md);
  white-space: nowrap;
  opacity: 0;
  pointer-events: none;
  transition: opacity var(--transition-fast);
  z-index: var(--z-index-tooltip);
}

.nav-item.collapsed:hover::after {
  opacity: 1;
}
</style>
