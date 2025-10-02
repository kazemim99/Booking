<template>
  <span :class="badgeClasses">
    <slot />
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  variant?: 'primary' | 'success' | 'danger' | 'warning' | 'info' | 'secondary' | 'default'
  size?: 'small' | 'medium' | 'large'
  rounded?: boolean
  dot?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'primary',
  size: 'medium',
  rounded: false,
  dot: false,
})

const badgeClasses = computed(() => [
  'badge',
  `badge-${props.variant}`,
  `badge-${props.size}`,
  {
    'badge-rounded': props.rounded,
    'badge-dot': props.dot,
  },
])
</script>

<style scoped lang="scss">
.badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  white-space: nowrap;
  border-radius: 4px;
  transition: all 0.2s;

  // Sizes
  &-small {
    padding: 0.125rem 0.5rem;
    font-size: 0.75rem;
  }

  &-medium {
    padding: 0.25rem 0.75rem;
    font-size: 0.875rem;
  }

  &-large {
    padding: 0.5rem 1rem;
    font-size: 1rem;
  }

  // Variants
  &-primary {
    background: #667eea;
    color: white;
  }

  &-success {
    background: #10b981;
    color: white;
  }

  &-danger {
    background: #ef4444;
    color: white;
  }

  &-warning {
    background: #f59e0b;
    color: white;
  }

  &-info {
    background: #3b82f6;
    color: white;
  }

  &-secondary {
    background: #6b7280;
    color: white;
  }

  // Modifiers
  &-rounded {
    border-radius: 9999px;
  }

  &-dot {
    width: 8px;
    height: 8px;
    padding: 0;
    border-radius: 50%;
  }
}
</style>
