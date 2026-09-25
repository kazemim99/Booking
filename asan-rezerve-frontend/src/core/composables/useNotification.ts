import { useNotificationStore } from '@/core/stores/modules/notification.store'

interface NotificationComposable {
  success: (title: string, message: string, duration?: number) => string
  error: (title: string, message: string, duration?: number) => string
  warning: (title: string, message: string, duration?: number) => string
  info: (title: string, message: string, duration?: number) => string
  remove: (id: string) => void
  clearAll: () => void
  showSuccess: (title: string, message: string, duration?: number) => string
  showError: (title: string, message: string, duration?: number) => string
  showConfirm: (title: string, message: string, duration?: number) => string
}

export function useNotification(): NotificationComposable {
  const notificationStore = useNotificationStore()

  function success(title: string, message: string, duration?: number): string {
    return notificationStore.success(title, message, duration)
  }

  function error(title: string, message: string, duration?: number): string {
    return notificationStore.error(title, message, duration)
  }

  function warning(title: string, message: string, duration?: number): string {
    return notificationStore.warning(title, message, duration)
  }

  function info(title: string, message: string, duration?: number): string {
    return notificationStore.info(title, message, duration)
  }

  function remove(id: string): void {
    notificationStore.removeNotification(id)
  }

  function clearAll(): void {
    notificationStore.clearAll()
  }

  return {
    success,
    error,
    warning,
    info,
    remove,
    clearAll,
    // Aliases for backward compatibility
    showSuccess: success,
    showError: error,
    showConfirm: warning
  }
}