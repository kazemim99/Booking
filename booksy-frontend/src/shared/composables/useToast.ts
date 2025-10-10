import { ref } from 'vue'

export interface Toast {
  id: string
  type: 'success' | 'error' | 'warning' | 'info'
  message: string
  duration: number
}

const toasts = ref<Toast[]>([])
let idCounter = 0

export function useToast() {
  const show = (type: Toast['type'], message: string, duration = 3000) => {
    const id = `toast-${++idCounter}`
    const toast: Toast = { id, type, message, duration }

    toasts.value.push(toast)

    if (duration > 0) {
      setTimeout(() => {
        remove(id)
      }, duration)
    }

    return id
  }

  const remove = (id: string) => {
    const index = toasts.value.findIndex((t) => t.id === id)
    if (index !== -1) {
      toasts.value.splice(index, 1)
    }
  }

  const success = (message: string, duration?: number) => show('success', message, duration)
  const error = (message: string, duration?: number) => show('error', message, duration)
  const warning = (message: string, duration?: number) => show('warning', message, duration)
  const info = (message: string, duration?: number) => show('info', message, duration)

  return {
    toasts,
    success,
    error,
    warning,
    info,
    remove,
  }
}
