<script setup lang="ts">
import { computed } from 'vue'

// Rename component to SkeletonLoader to follow multi-word naming convention
defineOptions({
  name: 'SkeletonLoader'
})

interface Props {
  width?: string
  height?: string
  variant?: 'text' | 'circular' | 'rectangular'
  animation?: 'pulse' | 'wave' | 'none'
}

const props = withDefaults(defineProps<Props>(), {
  width: '100%',
  height: '1rem',
  variant: 'text',
  animation: 'wave',
})

const skeletonClasses = computed(() => {
  return [
    'skeleton',
    `skeleton--${props.variant}`,
    props.animation !== 'none' && `skeleton--${props.animation}`,
  ]
})

const skeletonStyles = computed(() => {
  return {
    width: props.width,
    height: props.height,
  }
})
</script>

<template>
  <div :class="skeletonClasses" :style="skeletonStyles">
    <slot />
  </div>
</template>

<style scoped>
.skeleton {
  display: block;
  background-color: var(--color-gray-200);
  border-radius: var(--radius-sm);
  position: relative;
  overflow: hidden;
}

.skeleton--text {
  border-radius: var(--radius-sm);
  transform: scale(1, 0.6);
}

.skeleton--circular {
  border-radius: var(--radius-full);
}

.skeleton--rectangular {
  border-radius: var(--radius-md);
}

/* Pulse animation */
.skeleton--pulse {
  animation: skeleton-pulse 1.5s ease-in-out infinite;
}

@keyframes skeleton-pulse {
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

/* Wave animation */
.skeleton--wave::after {
  content: '';
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  transform: translateX(-100%);
  background: linear-gradient(
    90deg,
    transparent,
    rgba(255, 255, 255, 0.5),
    transparent
  );
  animation: skeleton-wave 1.5s ease-in-out infinite;
}

@keyframes skeleton-wave {
  0% {
    transform: translateX(-100%);
  }
  100% {
    transform: translateX(100%);
  }
}

/* Respect reduced motion preferences */
@media (prefers-reduced-motion: reduce) {
  .skeleton--pulse,
  .skeleton--wave::after {
    animation: none;
  }

  .skeleton--pulse {
    opacity: 0.7;
  }
}
</style>
