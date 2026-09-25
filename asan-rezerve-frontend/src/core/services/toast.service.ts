/**
 * Toast Notification Service
 * Provides centralized toast notifications for success, error, warning, and info messages
 * This service acts as a bridge to the Pinia notification store
 */

import { getActivePinia } from 'pinia'
import { useNotificationStore } from '@/core/stores/modules/notification.store'

export interface ToastOptions {
  title?: string
  message: string
  duration?: number
  position?: 'top-right' | 'top-center' | 'top-left' | 'bottom-right' | 'bottom-center' | 'bottom-left'
}

export interface Toast {
  id: string
  type: 'success' | 'error' | 'warning' | 'info'
  title?: string
  message: string
  duration: number
  timestamp: number
}

class ToastService {
  private toasts: Toast[] = []
  private listeners: Set<(toasts: Toast[]) => void> = new Set()
  private idCounter = 0

  /**
   * Get the notification store if Pinia is available
   */
  private getNotificationStore() {
    try {
      if (getActivePinia()) {
        return useNotificationStore()
      }
    } catch {
      // Pinia not available, fallback to local state
    }
    return null
  }

  /**
   * Subscribe to toast changes
   */
  subscribe(callback: (toasts: Toast[]) => void): () => void {
    this.listeners.add(callback)
    // Return unsubscribe function
    return () => {
      this.listeners.delete(callback)
    }
  }

  /**
   * Notify all subscribers of toast changes
   */
  private notify() {
    this.listeners.forEach(callback => callback([...this.toasts]))
  }

  /**
   * Add a toast notification
   */
  private addToast(type: Toast['type'], options: ToastOptions): string {
    const store = this.getNotificationStore()

    if (store) {
      // Use Pinia store if available
      return store.addNotification(
        type,
        options.title || '',
        options.message,
        options.duration
      )
    }

    // Fallback to local state
    const id = `toast-${++this.idCounter}-${Date.now()}`
    const toast: Toast = {
      id,
      type,
      title: options.title,
      message: options.message,
      duration: options.duration ?? 5000,
      timestamp: Date.now()
    }

    this.toasts.push(toast)
    this.notify()

    // Auto-remove after duration
    if (toast.duration > 0) {
      setTimeout(() => {
        this.remove(id)
      }, toast.duration)
    }

    return id
  }

  /**
   * Show success toast
   */
  success(message: string, title?: string, duration?: number): string {
    return this.addToast('success', { message, title, duration })
  }

  /**
   * Show error toast
   */
  error(message: string, title?: string, duration?: number): string {
    return this.addToast('error', { message, title, duration: duration ?? 7000 })
  }

  /**
   * Show warning toast
   */
  warning(message: string, title?: string, duration?: number): string {
    return this.addToast('warning', { message, title, duration })
  }

  /**
   * Show info toast
   */
  info(message: string, title?: string, duration?: number): string {
    return this.addToast('info', { message, title, duration })
  }

  /**
   * Show multiple error toasts (for 5xx server errors)
   */
  errorBatch(messages: string[], title?: string): void {
    messages.forEach(message => {
      this.error(message, title)
    })
  }

  /**
   * Remove a specific toast
   */
  remove(id: string): void {
    const store = this.getNotificationStore()

    if (store) {
      store.removeNotification(id)
      return
    }

    // Fallback to local state
    const index = this.toasts.findIndex(t => t.id === id)
    if (index !== -1) {
      this.toasts.splice(index, 1)
      this.notify()
    }
  }

  /**
   * Clear all toasts
   */
  clearAll(): void {
    const store = this.getNotificationStore()

    if (store) {
      store.clearAll()
      return
    }

    // Fallback to local state
    this.toasts = []
    this.notify()
  }

  /**
   * Get all current toasts
   */
  getAll(): Toast[] {
    return [...this.toasts]
  }
}

export const toastService = new ToastService()
