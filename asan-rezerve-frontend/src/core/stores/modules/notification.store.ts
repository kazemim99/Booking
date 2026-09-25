import { defineStore } from 'pinia'
import { ref } from 'vue'

export type NotificationType = 'success' | 'error' | 'warning' | 'info'

export interface Notification {
  id: string
  type: NotificationType
  title: string
  message: string
  duration?: number
  timestamp: Date
}

export const useNotificationStore = defineStore('notification', () => {
  // State
  const notifications = ref<Notification[]>([])
  const maxNotifications = 5

  // Actions
  function addNotification(
    type: NotificationType,
    title: string,
    message: string,
    duration = 5000
  ): string {
    const id = `notification-${Date.now()}-${Math.random()}`
    const notification: Notification = {
      id,
      type,
      title,
      message,
      duration,
      timestamp: new Date()
    }

    notifications.value.unshift(notification)

    // Limit number of notifications
    if (notifications.value.length > maxNotifications) {
      notifications.value = notifications.value.slice(0, maxNotifications)
    }

    // Auto-remove after duration
    if (duration > 0) {
      setTimeout(() => {
        removeNotification(id)
      }, duration)
    }

    return id
  }

  function removeNotification(id: string): void {
    const index = notifications.value.findIndex((n) => n.id === id)
    if (index !== -1) {
      notifications.value.splice(index, 1)
    }
  }

  function clearAll(): void {
    notifications.value = []
  }

  // Convenience methods
  function success(title: string, message: string, duration?: number): string {
    return addNotification('success', title, message, duration)
  }

  function error(title: string, message: string, duration?: number): string {
    return addNotification('error', title, message, duration)
  }

  function warning(title: string, message: string, duration?: number): string {
    return addNotification('warning', title, message, duration)
  }

  function info(title: string, message: string, duration?: number): string {
    return addNotification('info', title, message, duration)
  }

  return {
    // State
    notifications,
    // Actions
    addNotification,
    removeNotification,
    clearAll,
    success,
    error,
    warning,
    info
  }
})