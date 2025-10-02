<template>
  <div v-if="show" :class="['alert', `alert-${type}`]">
    <span class="alert-icon">{{ icon }}</span>
    <div class="alert-content">
      <p v-if="title" class="alert-title">{{ title }}</p>
      <p class="alert-message">{{ message }}</p>
    </div>
    <button v-if="dismissible" @click="dismiss" class="alert-close">✕</button>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

type AlertType = 'success' | 'error' | 'warning' | 'info'

interface Props {
  type?: AlertType
  title?: string
  message: string
  dismissible?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  type: 'info',
  dismissible: true,
})

const emit = defineEmits<{
  dismiss: []
}>()

const show = ref(true)

const icon = computed(() => {
  switch (props.type) {
    case 'success':
      return '✓'
    case 'error':
      return '✕'
    case 'warning':
      return '⚠️'
    case 'info':
    default:
      return 'ℹ️'
  }
})

function dismiss() {
  show.value = false
  emit('dismiss')
}
</script>

<style scoped lang="scss">
.alert {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.875rem 1rem;
  border-radius: 8px;
  border: 1px solid;
  margin-bottom: 1.5rem;
  font-size: 0.875rem;
}

.alert-success {
  background-color: #f0fdf4;
  border-color: #bbf7d0;
  color: #166534;
}

.alert-error {
  background-color: #fef2f2;
  border-color: #fecaca;
  color: #991b1b;
}

.alert-warning {
  background-color: #fffbeb;
  border-color: #fde68a;
  color: #92400e;
}

.alert-info {
  background-color: #eff6ff;
  border-color: #bfdbfe;
  color: #1e40af;
}

.alert-icon {
  font-size: 1.25rem;
  flex-shrink: 0;
  margin-top: 0.0625rem;
}

.alert-content {
  flex: 1;
}

.alert-title {
  font-weight: 600;
  margin: 0 0 0.25rem 0;
}

.alert-message {
  margin: 0;
  line-height: 1.5;
}

.alert-close {
  background: none;
  border: none;
  font-size: 1.125rem;
  cursor: pointer;
  color: currentColor;
  opacity: 0.5;
  transition: opacity 0.2s;
  padding: 0;
  flex-shrink: 0;

  &:hover {
    opacity: 1;
  }
}
</style>
