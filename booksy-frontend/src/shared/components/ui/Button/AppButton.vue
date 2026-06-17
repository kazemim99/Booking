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
  font-family: var(--font-family-base);
  font-weight: var(--font-weight-bold); /* Coliride labels are bold */
  border: none;
  cursor: pointer;
  transition: background-color 0.2s ease, border-color 0.2s ease, color 0.2s ease;
  border-radius: var(--radius-md); /* 10px - Coliride button radius */
  position: relative;
  overflow: hidden;
  white-space: nowrap;

  &:focus-visible {
    outline: none;
    box-shadow: 0 0 0 3px rgba(55, 119, 191, 0.25);
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

  // Variants — Coliride: flat, no shadow, no transform on hover; hover just darkens
  &-primary {
    background: var(--color-primary-500);
    color: var(--color-text-inverse);

    &:hover:not(:disabled):not(.btn-loading) {
      background: var(--color-primary-600);
    }
  }

  // Coliride secondary = white fill with a 2px blue outline and blue label
  &-secondary {
    background: var(--color-background);
    color: var(--color-primary-500);
    border: 2px solid var(--color-primary-500);

    &:hover:not(:disabled):not(.btn-loading) {
      background: var(--color-primary-50);
    }
  }

  &-success {
    background: var(--color-success-500);
    color: var(--color-text-inverse);

    &:hover:not(:disabled):not(.btn-loading) {
      background: var(--color-success-600);
    }
  }

  &-danger {
    background: var(--color-danger-400);
    color: var(--color-text-inverse);

    &:hover:not(:disabled):not(.btn-loading) {
      background: var(--color-danger-500);
    }
  }

  &-warning {
    background: var(--color-warning-500);
    color: var(--color-navy); /* gold needs dark ink for contrast */

    &:hover:not(:disabled):not(.btn-loading) {
      background: var(--color-warning-400);
    }
  }

  &-ghost {
    background: transparent;
    color: var(--color-primary-500);
    border: 1px solid var(--color-primary-500);

    &:hover:not(:disabled):not(.btn-loading) {
      background: var(--color-primary-50);
    }
  }

  &-link {
    background: transparent;
    color: var(--color-primary-500);
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
