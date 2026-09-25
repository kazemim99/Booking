<template>
  <Teleport to="body">
    <div class="toast-container">
      <TransitionGroup name="toast">
        <div
          v-for="notification in notifications"
          :key="notification.id"
          :class="['toast', `toast-${notification.type}`]"
          role="alert"
          aria-live="polite"
        >
          <div class="toast-icon">{{ getIcon(notification.type) }}</div>
          <div class="toast-content">
            <p class="toast-title">{{ notification.title }}</p>
            <p class="toast-message">{{ notification.message }}</p>
          </div>
          <button
            class="toast-close"
            @click="handleDismiss(notification.id)"
            aria-label="Close notification"
          >
            ✕
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  useNotificationStore,
  type NotificationType,
} from '@/core/stores/modules/notification.store'

const notificationStore = useNotificationStore()

const notifications = computed(() => notificationStore.notifications)

function getIcon(type: NotificationType): string {
  const icons = {
    success: '✓',
    error: '✕',
    warning: '⚠',
    info: 'ℹ',
  }
  return icons[type] || icons.info
}

function handleDismiss(id: string): void {
  notificationStore.removeNotification(id)
}
</script>

<style scoped lang="scss">
.toast-container {
  position: fixed;
  top: 1rem;
  right: 1rem;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  max-width: 420px;
  pointer-events: none;
}

.toast {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem;
  background: white;
  border-radius: 8px;
  box-shadow:
    0 4px 6px -1px rgba(0, 0, 0, 0.1),
    0 2px 4px -1px rgba(0, 0, 0, 0.06);
  border-left: 4px solid;
  min-width: 320px;
  pointer-events: auto;
  cursor: default;
}

.toast-success {
  border-left-color: var(--color-success-500);
  background: #f0fdf4;

  .toast-icon {
    color: var(--color-success-500);
  }
}

.toast-error {
  border-left-color: var(--color-danger-500);
  background: var(--color-danger-50);

  .toast-icon {
    color: var(--color-danger-500);
  }
}

.toast-warning {
  border-left-color: var(--color-warning-500);
  background: var(--color-warning-50);

  .toast-icon {
    color: var(--color-warning-500);
  }
}

.toast-info {
  border-left-color: var(--color-primary-500);
  background: var(--color-primary-50);

  .toast-icon {
    color: var(--color-primary-500);
  }
}

.toast-icon {
  font-size: 1.25rem;
  font-weight: bold;
  flex-shrink: 0;
  width: 1.5rem;
  height: 1.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
}

.toast-content {
  flex: 1;
  min-width: 0;
}

.toast-title {
  font-weight: 600;
  font-size: 0.875rem;
  margin: 0 0 0.25rem 0;
  color: var(--color-gray-900);
  line-height: 1.25;
}

.toast-message {
  font-size: 0.875rem;
  margin: 0;
  color: var(--color-gray-600);
  line-height: 1.5;
  word-wrap: break-word;
}

.toast-close {
  background: none;
  border: none;
  font-size: 1.25rem;
  cursor: pointer;
  color: var(--color-gray-500);
  padding: 0;
  width: 1.5rem;
  height: 1.5rem;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  transition: all 0.2s;

  &:hover {
    color: var(--color-gray-700);
    background: rgba(0, 0, 0, 0.05);
  }

  &:focus {
    outline: 2px solid var(--color-primary-500);
    outline-offset: 2px;
  }
}

// Toast animations
.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s ease;
}

.toast-enter-from {
  opacity: 0;
  transform: translateX(100%);
}

.toast-leave-to {
  opacity: 0;
  transform: translateX(100%) scale(0.95);
}

.toast-move {
  transition: transform 0.3s ease;
}

// Responsive adjustments
@media (max-width: 640px) {
  .toast-container {
    left: 1rem;
    right: 1rem;
    max-width: none;
  }

  .toast {
    min-width: auto;
    width: 100%;
  }
}

// Dark theme support
[data-theme='dark'] {
  .toast {
    background: var(--color-gray-900);
  }

  .toast-success {
    background: #064e3b;
  }

  .toast-error {
    background: #7f1d1d;
  }

  .toast-warning {
    background: #78350f;
  }

  .toast-info {
    background: #1e3a8a;
  }

  .toast-title {
    color: var(--color-gray-50);
  }

  .toast-message {
    color: var(--color-gray-400);
  }

  .toast-close {
    color: var(--color-gray-500);

    &:hover {
      color: var(--color-gray-50);
      background: rgba(255, 255, 255, 0.1);
    }
  }
}
</style>
