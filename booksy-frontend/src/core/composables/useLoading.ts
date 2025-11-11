/**
 * Loading Composable
 * Global and component-level loading state management
 */

import { ref, computed } from 'vue'

// ==================== Types ====================

export interface LoadingState {
  isLoading: boolean
  message?: string
  progress?: number
}

// ==================== Composable ====================

export function useLoading(initialState = false) {
  const isLoading = ref(initialState)
  const message = ref<string>()
  const progress = ref<number>()
  const loadingTasks = ref<Map<string, boolean>>(new Map())

  // ==================== Computed ====================

  const state = computed<LoadingState>(() => ({
    isLoading: isLoading.value,
    message: message.value,
    progress: progress.value,
  }))

  const hasActiveTasks = computed(() => loadingTasks.value.size > 0)

  // ==================== Actions ====================

  /**
   * Start loading
   */
  function start(msg?: string, progressValue?: number): void {
    isLoading.value = true
    message.value = msg
    progress.value = progressValue
  }

  /**
   * Stop loading
   */
  function stop(): void {
    isLoading.value = false
    message.value = undefined
    progress.value = undefined
  }

  /**
   * Toggle loading state
   */
  function toggle(): void {
    isLoading.value = !isLoading.value
  }

  /**
   * Set loading message
   */
  function setMessage(msg: string): void {
    message.value = msg
  }

  /**
   * Set loading progress (0-100)
   */
  function setProgress(value: number): void {
    progress.value = Math.min(100, Math.max(0, value))
  }

  /**
   * Increment progress
   */
  function incrementProgress(value: number = 1): void {
    if (progress.value !== undefined) {
      setProgress(progress.value + value)
    }
  }

  /**
   * Start a named loading task
   */
  function startTask(taskId: string): void {
    loadingTasks.value.set(taskId, true)
    if (!isLoading.value) {
      start()
    }
  }

  /**
   * Stop a named loading task
   */
  function stopTask(taskId: string): void {
    loadingTasks.value.delete(taskId)
    if (loadingTasks.value.size === 0) {
      stop()
    }
  }

  /**
   * Wrap async function with loading state
   */
  async function wrap<T>(
    fn: () => Promise<T>,
    msg?: string
  ): Promise<T> {
    start(msg)
    try {
      const result = await fn()
      return result
    } finally {
      stop()
    }
  }

  /**
   * Wrap async function with named task
   */
  async function wrapTask<T>(
    taskId: string,
    fn: () => Promise<T>
  ): Promise<T> {
    startTask(taskId)
    try {
      const result = await fn()
      return result
    } finally {
      stopTask(taskId)
    }
  }

  /**
   * Reset all loading state
   */
  function reset(): void {
    isLoading.value = false
    message.value = undefined
    progress.value = undefined
    loadingTasks.value.clear()
  }

  return {
    // State
    isLoading,
    message,
    progress,
    loadingTasks,

    // Computed
    state,
    hasActiveTasks,

    // Actions
    start,
    stop,
    toggle,
    setMessage,
    setProgress,
    incrementProgress,
    startTask,
    stopTask,
    wrap,
    wrapTask,
    reset,
  }
}

// ==================== Global Loading State ====================

const globalLoading = ref(false)
const globalMessage = ref<string>()

export function useGlobalLoading() {
  function start(msg?: string): void {
    globalLoading.value = true
    globalMessage.value = msg
  }

  function stop(): void {
    globalLoading.value = false
    globalMessage.value = undefined
  }

  async function wrap<T>(fn: () => Promise<T>, msg?: string): Promise<T> {
    start(msg)
    try {
      const result = await fn()
      return result
    } finally {
      stop()
    }
  }

  return {
    isLoading: globalLoading,
    message: globalMessage,
    start,
    stop,
    wrap,
  }
}
