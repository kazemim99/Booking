<template>
  <aside class="app-sidebar" :class="{ collapsed: isCollapsed }">
    <!-- Sidebar Content -->
    <div class="sidebar-content">
      <!-- Sidebar Menu -->
      <SidebarMenu :is-collapsed="isCollapsed" />
    </div>

    <!-- Collapse Toggle Button -->
    <button class="collapse-toggle" @click="$emit('toggle')" aria-label="Toggle Sidebar">
      <svg
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
        :class="{ rotated: isCollapsed }"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
      </svg>
    </button>
  </aside>
</template>

<script setup lang="ts">
import SidebarMenu from './SidebarMenu.vue'

interface Props {
  isCollapsed?: boolean
}

defineProps<Props>()

defineEmits<{
  toggle: []
}>()
</script>

<style scoped lang="scss">
.app-sidebar {
  position: fixed;
  top: 64px;
  left: 0;
  bottom: 0;
  width: 260px;
  background: white;
  border-right: 1px solid #e5e7eb;
  transition: all 0.3s ease;
  z-index: 900;
  overflow-x: hidden;
  overflow-y: auto;

  &.collapsed {
    width: 80px;
  }

  // Hide on mobile by default
  @media (max-width: 1024px) {
    transform: translateX(-100%);

    &:not(.collapsed) {
      transform: translateX(0);
      box-shadow: 4px 0 12px rgba(0, 0, 0, 0.1);
    }
  }
}

.sidebar-content {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 1rem 0;
}

.collapse-toggle {
  position: absolute;
  bottom: 1rem;
  right: -12px;
  width: 24px;
  height: 24px;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

  svg {
    width: 16px;
    height: 16px;
    color: #6b7280;
    transition: transform 0.3s ease;

    &.rotated {
      transform: rotate(180deg);
    }
  }

  &:hover {
    background: #f3f4f6;
    transform: scale(1.1);
  }

  @media (max-width: 1024px) {
    display: none;
  }
}

// Custom scrollbar
.app-sidebar::-webkit-scrollbar {
  width: 6px;
}

.app-sidebar::-webkit-scrollbar-track {
  background: transparent;
}

.app-sidebar::-webkit-scrollbar-thumb {
  background: #d1d5db;
  border-radius: 3px;

  &:hover {
    background: #9ca3af;
  }
}
</style>
