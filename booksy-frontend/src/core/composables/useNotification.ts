import { useNotificationStore } from '@/core/stores/modules/notification.store'

export function useNotification() {
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
    clearAll
  }
}