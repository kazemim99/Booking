/**
 * Toast Composable
 * Enhanced toast notifications (alias for useNotification with additional features)
 */

import { useNotification } from './useNotification'

// ==================== Types ====================

export interface ToastOptions {
  title: string
  message: string
  duration?: number
  closable?: boolean
  action?: ToastAction
}

export interface ToastAction {
  label: string
  onClick: () => void
}

export type ToastType = 'success' | 'error' | 'warning' | 'info'

// ==================== Composable ====================

export function useToast() {
  const notification = useNotification()

  /**
   * Show success toast
   */
  function success(title: string, message: string, duration?: number): string {
    return notification.success(title, message, duration)
  }

  /**
   * Show error toast
   */
  function error(title: string, message: string, duration?: number): string {
    return notification.error(title, message, duration)
  }

  /**
   * Show warning toast
   */
  function warning(title: string, message: string, duration?: number): string {
    return notification.warning(title, message, duration)
  }

  /**
   * Show info toast
   */
  function info(title: string, message: string, duration?: number): string {
    return notification.info(title, message, duration)
  }

  /**
   * Show toast with options
   */
  function show(type: ToastType, options: ToastOptions): string {
    const { title, message, duration } = options

    switch (type) {
      case 'success':
        return success(title, message, duration)
      case 'error':
        return error(title, message, duration)
      case 'warning':
        return warning(title, message, duration)
      case 'info':
        return info(title, message, duration)
      default:
        return info(title, message, duration)
    }
  }

  /**
   * Show promise toast (loading -> success/error)
   */
  async function promise<T>(
    promise: Promise<T>,
    messages: {
      loading: string
      success: string
      error: string
    }
  ): Promise<T> {
    const loadingId = info('در حال انجام', messages.loading, 0)

    try {
      const result = await promise
      notification.remove(loadingId)
      success('موفقیت', messages.success)
      return result
    } catch (err) {
      notification.remove(loadingId)
      const errorMessage =
        err instanceof Error ? err.message : messages.error
      error('خطا', errorMessage)
      throw err
    }
  }

  /**
   * Remove toast by ID
   */
  function remove(id: string): void {
    notification.remove(id)
  }

  /**
   * Clear all toasts
   */
  function clearAll(): void {
    notification.clearAll()
  }

  return {
    success,
    error,
    warning,
    info,
    show,
    promise,
    remove,
    clearAll,
  }
}

// ==================== Pre-configured Helpers ====================

/**
 * Show loading toast
 */
export function showLoadingToast(message: string = 'در حال بارگذاری...'): string {
  const toast = useToast()
  return toast.info('لطفاً صبر کنید', message, 0)
}

/**
 * Show saved toast
 */
export function showSavedToast(): string {
  const toast = useToast()
  return toast.success('ذخیره شد', 'تغییرات با موفقیت ذخیره شد')
}

/**
 * Show deleted toast
 */
export function showDeletedToast(): string {
  const toast = useToast()
  return toast.success('حذف شد', 'مورد با موفقیت حذف شد')
}

/**
 * Show error toast
 */
export function showErrorToast(message?: string): string {
  const toast = useToast()
  return toast.error(
    'خطا',
    message || 'خطایی رخ داد. لطفاً دوباره تلاش کنید'
  )
}
