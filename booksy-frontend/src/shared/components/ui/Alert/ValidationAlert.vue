<template>
  <transition name="slide-down">
    <div v-if="visible" :class="['validation-alert', `alert-${variant}`]">
      <div class="alert-icon">
        <!-- Error Icon -->
        <svg v-if="variant === 'error'" class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
          />
        </svg>
        <!-- Warning Icon -->
        <svg v-else-if="variant === 'warning'" class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
          />
        </svg>
        <!-- Success Icon -->
        <svg v-else-if="variant === 'success'" class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
          />
        </svg>
        <!-- Info Icon -->
        <svg v-else class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
          />
        </svg>
      </div>

      <div class="alert-content">
        <!-- Title -->
        <h4 v-if="title" class="alert-title">{{ title }}</h4>

        <!-- Single Message -->
        <p v-if="!isArray(message)" class="alert-message">{{ message }}</p>

        <!-- Multiple Messages (List) -->
        <ul v-else class="alert-list">
          <li v-for="(msg, index) in message" :key="index" class="alert-list-item">
            {{ msg }}
          </li>
        </ul>

        <!-- Validation Errors (Object) -->
        <div v-if="errors && Object.keys(errors).length > 0" class="alert-errors">
          <div v-for="(errorMessages, field) in errors" :key="field" class="error-group">
            <span class="error-field">{{ formatFieldName(field) }}:</span>
            <ul v-if="Array.isArray(errorMessages)" class="error-messages">
              <li v-for="(err, idx) in errorMessages" :key="idx">{{ err }}</li>
            </ul>
            <span v-else class="error-messages">{{ errorMessages }}</span>
          </div>
        </div>
      </div>

      <!-- Close Button -->
      <button v-if="dismissible" type="button" class="close-btn" @click="handleClose">
        <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>
  </transition>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'

interface Props {
  variant?: 'error' | 'warning' | 'success' | 'info'
  title?: string
  message?: string | string[]
  errors?: Record<string, string | string[]>
  dismissible?: boolean
  autoDismiss?: boolean
  autoDismissDelay?: number
  modelValue?: boolean
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'close'): void
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'error',
  dismissible: true,
  autoDismiss: false,
  autoDismissDelay: 5000,
  modelValue: true,
})

const emit = defineEmits<Emits>()

const visible = ref(props.modelValue)

// Watch for external changes
watch(() => props.modelValue, (newValue) => {
  visible.value = newValue
})

// Auto-dismiss timer
let dismissTimer: NodeJS.Timeout | null = null

watch(visible, (isVisible) => {
  if (isVisible && props.autoDismiss) {
    // Clear any existing timer
    if (dismissTimer) {
      clearTimeout(dismissTimer)
    }

    // Set new timer
    dismissTimer = setTimeout(() => {
      handleClose()
    }, props.autoDismissDelay)
  }
}, { immediate: true })

const isArray = (value: any): value is string[] => {
  return Array.isArray(value)
}

const formatFieldName = (field: string): string => {
  // Convert camelCase or PascalCase to readable format
  return field
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (str) => str.toUpperCase())
    .trim()
}

const handleClose = () => {
  visible.value = false
  emit('update:modelValue', false)
  emit('close')

  if (dismissTimer) {
    clearTimeout(dismissTimer)
    dismissTimer = null
  }
}
</script>

<style scoped>
.validation-alert {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem 1.25rem;
  border-radius: 0.75rem;
  border: 1px solid;
  margin-bottom: 1rem;
  direction: rtl;
  animation: shake 0.3s ease-in-out;
}

/* Variants */
.alert-error {
  background: #fef2f2;
  border-color: #fecaca;
  color: #991b1b;
}

.alert-warning {
  background: #fffbeb;
  border-color: #fde68a;
  color: #92400e;
}

.alert-success {
  background: #f0fdf4;
  border-color: #bbf7d0;
  color: #166534;
}

.alert-info {
  background: #eff6ff;
  border-color: #bfdbfe;
  color: #1e40af;
}

/* Icon */
.alert-icon {
  flex-shrink: 0;
  margin-top: 0.125rem;
}

.alert-error .icon {
  color: #ef4444;
}

.alert-warning .icon {
  color: #f59e0b;
}

.alert-success .icon {
  color: #10b981;
}

.alert-info .icon {
  color: #3b82f6;
}

.icon {
  width: 1.5rem;
  height: 1.5rem;
}

/* Content */
.alert-content {
  flex: 1;
  min-width: 0;
}

.alert-title {
  font-size: 1rem;
  font-weight: 700;
  margin-bottom: 0.25rem;
}

.alert-message {
  font-size: 0.875rem;
  line-height: 1.5;
  margin: 0;
}

/* List Messages */
.alert-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.alert-list-item {
  font-size: 0.875rem;
  line-height: 1.5;
  padding-right: 1rem;
  position: relative;
}

.alert-list-item::before {
  content: '•';
  position: absolute;
  right: 0;
  font-weight: bold;
}

/* Validation Errors */
.alert-errors {
  margin-top: 0.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.error-group {
  font-size: 0.875rem;
  line-height: 1.5;
}

.error-field {
  font-weight: 600;
  margin-left: 0.25rem;
}

.error-messages {
  display: inline;
  list-style: none;
  padding: 0;
  margin: 0;
}

.error-messages li {
  display: inline;
}

.error-messages li:not(:last-child)::after {
  content: '، ';
}

/* Close Button */
.close-btn {
  flex-shrink: 0;
  background: none;
  border: none;
  padding: 0.25rem;
  cursor: pointer;
  color: currentColor;
  opacity: 0.5;
  border-radius: 0.375rem;
  transition: all 0.2s;
  margin-top: -0.25rem;
  margin-left: -0.5rem;
}

.close-btn:hover {
  opacity: 1;
  background: rgba(0, 0, 0, 0.05);
}

.close-btn .icon {
  width: 1.25rem;
  height: 1.25rem;
}

/* Animations */
.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.3s ease;
}

.slide-down-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}

.slide-down-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

@keyframes shake {
  0%,
  100% {
    transform: translateX(0);
  }
  10%,
  30%,
  50%,
  70%,
  90% {
    transform: translateX(-5px);
  }
  20%,
  40%,
  60%,
  80% {
    transform: translateX(5px);
  }
}

@media (max-width: 640px) {
  .validation-alert {
    padding: 0.875rem 1rem;
  }

  .alert-title {
    font-size: 0.9375rem;
  }

  .alert-message,
  .alert-list-item,
  .error-group {
    font-size: 0.8125rem;
  }
}
</style>
