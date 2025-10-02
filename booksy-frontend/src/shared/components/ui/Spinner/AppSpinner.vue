<template>
  <div :class="spinnerClasses" :style="spinnerStyle">
    <div class="spinner-circle"></div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  size?: 'small' | 'medium' | 'large' | number
  color?: string
  thickness?: number
}

const props = withDefaults(defineProps<Props>(), {
  size: 'medium',
  color: 'currentColor',
  thickness: 3,
})

const spinnerClasses = computed(() => [
  'spinner',
  typeof props.size === 'string' ? `spinner-${props.size}` : '',
])

const spinnerSize = computed(() => {
  if (typeof props.size === 'number') return `${props.size}px`

  const sizeMap = {
    small: '20px',
    medium: '32px',
    large: '48px',
  }
  return sizeMap[props.size]
})

const spinnerStyle = computed(() => ({
  width: spinnerSize.value,
  height: spinnerSize.value,
  '--spinner-color': props.color,
  '--spinner-thickness': `${props.thickness}px`,
}))
</script>

<style scoped lang="scss">
.spinner {
  display: inline-block;
  position: relative;

  &-small {
    width: 20px;
    height: 20px;
  }

  &-medium {
    width: 32px;
    height: 32px;
  }

  &-large {
    width: 48px;
    height: 48px;
  }
}

.spinner-circle {
  width: 100%;
  height: 100%;
  border: var(--spinner-thickness, 3px) solid rgba(102, 126, 234, 0.2);
  border-top-color: var(--spinner-color, #667eea);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
