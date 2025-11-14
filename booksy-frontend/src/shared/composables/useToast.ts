import { ref } from 'vue'

type ToastType = 'success' | 'error' | 'warning' | 'info'

interface ToastInstance {
  addToast: (message: string, type?: ToastType, duration?: number) => void
}

const toastInstance = ref<ToastInstance | null>(null)

export function setToastInstance(instance: ToastInstance) {
  toastInstance.value = instance
}

export function useToast() {
  const success = (message: string, duration = 3000) => {
    toastInstance.value?.addToast(message, 'success', duration)
  }

  const error = (message: string, duration = 4000) => {
    toastInstance.value?.addToast(message, 'error', duration)
  }

  const warning = (message: string, duration = 3500) => {
    toastInstance.value?.addToast(message, 'warning', duration)
  }

  const info = (message: string, duration = 3000) => {
    toastInstance.value?.addToast(message, 'info', duration)
  }

  return {
    success,
    error,
    warning,
    info,
  }
}
