<template>
  <button :type="type" :class="buttonClasses" :disabled="disabled || loading" @click="handleClick">
    <span v-if="loading" class="btn-loading">
      <svg class="btn-spinner" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle
          class="spinner-track"
          cx="12"
          cy="12"
          r="10"
          stroke="currentColor"
          stroke-width="4"
        />
        <path
          class="spinner-path"
          d="M12 2a10 10 0 0 1 10 10"
          stroke="currentColor"
          stroke-width="4"
          stroke-linecap="round"
        />
      </svg>
      <span v-if="loadingText">{{ loadingText }}</span>
    </span>
    <span v-else class="btn-content">
      <span v-if="icon || $slots.icon" class="btn-icon">
        <slot name="icon">
          <span v-if="icon" v-html="icon" />
        </slot>
      </span>
      <slot />
    </span>
  </button>
</template>

<script setup lang="ts">
import { computed } from 'vue'

type ButtonVariant = 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'ghost' | 'link'
type ButtonSize = 'small' | 'medium' | 'large'
type ButtonType = 'button' | 'submit' | 'reset'

interface Props {
  variant?: ButtonVariant
  size?: ButtonSize
  type?: ButtonType
  disabled?: boolean
  loading?: boolean
  loadingText?: string
  block?: boolean
  rounded?: boolean
  icon?: string
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'primary',
  size: 'medium',
  type: 'button',
  disabled: false,
  loading: false,
  block: false,
  rounded: false,
})

interface Emits {
  (e: 'click', event: MouseEvent): void
}

const emit = defineEmits<Emits>()

const buttonClasses = computed(() => [
  'btn',
  `btn-${props.variant}`,
  `btn-${props.size}`,
  {
    'btn-block': props.block,
    'btn-rounded': props.rounded,
    'btn-loading': props.loading,
    'btn-disabled': props.disabled,
  },
])

function handleClick(event: MouseEvent): void {
  if (!props.disabled && !props.loading) {
    emit('click', event)
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/rtl';

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  font-weight: 600;
  border: none;
  cursor: pointer;
  transition: all 0.3s ease;
  border-radius: 8px;
  position: relative;
  overflow: hidden;
  white-space: nowrap;

  &:focus {
    outline: none;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.2);
  }

  &:disabled,
  &.btn-disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  &.btn-loading {
    cursor: wait;
  }

  // Sizes
  &-small {
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
  }

  &-medium {
    padding: 0.75rem 1.5rem;
    font-size: 1rem;
  }

  &-large {
    padding: 1rem 2rem;
    font-size: 1.125rem;
  }

  // Variants
  &-primary {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;

    &:hover:not(:disabled):not(.btn-loading) {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    }

    &:active:not(:disabled):not(.btn-loading) {
      transform: translateY(0);
    }
  }

  &-secondary {
    background: #e5e7eb;
    color: #374151;

    &:hover:not(:disabled):not(.btn-loading) {
      background: #d1d5db;
    }
  }

  &-success {
    background: #10b981;
    color: white;

    &:hover:not(:disabled):not(.btn-loading) {
      background: #059669;
    }
  }

  &-danger {
    background: #ef4444;
    color: white;

    &:hover:not(:disabled):not(.btn-loading) {
      background: #dc2626;
    }
  }

  &-warning {
    background: #f59e0b;
    color: white;

    &:hover:not(:disabled):not(.btn-loading) {
      background: #d97706;
    }
  }

  &-ghost {
    background: transparent;
    color: #667eea;
    border: 1px solid #667eea;

    &:hover:not(:disabled):not(.btn-loading) {
      background: rgba(102, 126, 234, 0.1);
    }
  }

  &-link {
    background: transparent;
    color: #667eea;
    padding: 0;

    &:hover:not(:disabled):not(.btn-loading) {
      text-decoration: underline;
    }
  }

  // Modifiers
  &-block {
    width: 100%;
  }

  &-rounded {
    border-radius: 9999px;
  }
}

.btn-content {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.btn-icon {
  display: flex;
  align-items: center;

  :deep(svg) {
    width: 1.25rem;
    height: 1.25rem;
  }
}

.btn-loading {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.btn-spinner {
  width: 1.25rem;
  height: 1.25rem;
  animation: spin 1s linear infinite;
}

.spinner-track {
  opacity: 0.25;
}

.spinner-path {
  opacity: 0.75;
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

// RTL Support
@include rtl {
  .btn-content {
    flex-direction: row-reverse;
  }

  .btn-icon {
    @include margin-end(0.5rem);
    @include margin-start(0);
  }
}
</style>
