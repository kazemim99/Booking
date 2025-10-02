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
  border-left-color: #10b981;
  background: #f0fdf4;

  .toast-icon {
    color: #10b981;
  }
}

.toast-error {
  border-left-color: #ef4444;
  background: #fef2f2;

  .toast-icon {
    color: #ef4444;
  }
}

.toast-warning {
  border-left-color: #f59e0b;
  background: #fffbeb;

  .toast-icon {
    color: #f59e0b;
  }
}

.toast-info {
  border-left-color: #3b82f6;
  background: #eff6ff;

  .toast-icon {
    color: #3b82f6;
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
  color: #111827;
  line-height: 1.25;
}

.toast-message {
  font-size: 0.875rem;
  margin: 0;
  color: #6b7280;
  line-height: 1.5;
  word-wrap: break-word;
}

.toast-close {
  background: none;
  border: none;
  font-size: 1.25rem;
  cursor: pointer;
  color: #9ca3af;
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
    color: #4b5563;
    background: rgba(0, 0, 0, 0.05);
  }

  &:focus {
    outline: 2px solid #3b82f6;
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
    background: #1f2937;
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
    color: #f9fafb;
  }

  .toast-message {
    color: #d1d5db;
  }

  .toast-close {
    color: #9ca3af;

    &:hover {
      color: #f9fafb;
      background: rgba(255, 255, 255, 0.1);
    }
  }
}
</style>
