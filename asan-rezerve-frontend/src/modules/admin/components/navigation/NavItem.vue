<template>
    <router-link
      :to="to"
      class="nav-item"
      :class="{ collapsed, active: isActive }"
      active-class="active"
    >
      <span class="nav-icon">
        <component :is="iconComponent" />
      </span>
      <span v-if="!collapsed" class="nav-label">{{ label }}</span>
      <span v-if="badge && !collapsed" class="nav-badge">{{ badge }}</span>
    </router-link>
  </template>
  
  <script setup lang="ts">
  import { computed } from 'vue'
  import { useRoute } from 'vue-router'
  
  interface Props {
    to: string
    icon: string
    label: string
    badge?: number
    collapsed?: boolean
  }
  
  const props = defineProps<Props>()
  const route = useRoute()
  
  const isActive = computed(() => route.path.startsWith(props.to))
  
  // Simple icon mapping (you can replace with actual icon components)
  const iconComponent = computed(() => {
    const icons: Record<string, string> = {
      dashboard: '📊',
      building: '🏢',
      users: '👥',
      calendar: '📅',
      services: '🛎️',
      chart: '📈',
      document: '📄',
      star: '⭐',
      settings: '⚙️',
      list: '📋',
    }
    
    return () => icons[props.icon] || '📌'
  })
  </script>
  
  <style scoped>
  .nav-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.75rem 1.5rem;
    color: var(--color-gray-500);
    text-decoration: none;
    transition: all 0.2s;
    position: relative;
    font-size: 0.875rem;
  }
  
  .nav-item:hover {
    background: rgba(255, 255, 255, 0.05);
    color: white;
  }
  
  .nav-item.active {
    background: rgba(59, 130, 246, 0.1);
    color: var(--color-primary-500);
    border-right: 3px solid var(--color-primary-500);
  }
  
  .nav-item.collapsed {
    justify-content: center;
    padding: 0.75rem;
  }
  
  .nav-icon {
    font-size: 1.25rem;
    display: flex;
    align-items: center;
    justify-content: center;
    min-width: 24px;
  }
  
  .nav-label {
    flex: 1;
    white-space: nowrap;
  }
  
  .nav-badge {
    background: var(--color-danger-500);
    color: white;
    font-size: 0.7rem;
    font-weight: 600;
    padding: 2px 6px;
    border-radius: 10px;
    min-width: 20px;
    text-align: center;
  }
  </style>