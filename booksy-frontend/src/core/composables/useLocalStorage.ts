/**
 * Local Storage Composable
 * Reactive localStorage wrapper with TypeScript support
 */

import { ref, watch, type Ref } from 'vue'

// ==================== Types ====================

export interface UseLocalStorageOptions<T> {
  defaultValue?: T
  serializer?: {
    read: (raw: string) => T
    write: (value: T) => string
  }
  onError?: (error: Error) => void
  listenToStorageChanges?: boolean
}

// ==================== Composable ====================

export function useLocalStorage<T>(
  key: string,
  options: UseLocalStorageOptions<T> = {}
): Ref<T | undefined> {
  const {
    defaultValue,
    serializer = {
      read: JSON.parse,
      write: JSON.stringify,
    },
    onError = (e) => console.error('[useLocalStorage]', e),
    listenToStorageChanges = true,
  } = options

  // ==================== State ====================

  const data = ref<T | undefined>(defaultValue) as Ref<T | undefined>

  // ==================== Read from localStorage ====================

  function read(): void {
    if (typeof localStorage === 'undefined') {
      data.value = defaultValue
      return
    }

    try {
      const rawValue = localStorage.getItem(key)

      if (rawValue === null) {
        data.value = defaultValue
        return
      }

      data.value = serializer.read(rawValue)
    } catch (error) {
      onError(error as Error)
      data.value = defaultValue
    }
  }

  // ==================== Write to localStorage ====================

  function write(value: T | undefined): void {
    if (typeof localStorage === 'undefined') return

    try {
      if (value === undefined || value === null) {
        localStorage.removeItem(key)
        return
      }

      const rawValue = serializer.write(value)
      localStorage.setItem(key, rawValue)
    } catch (error) {
      onError(error as Error)
    }
  }

  // ==================== Initialize ====================

  read()

  // ==================== Watch for changes ====================

  watch(
    data,
    (newValue) => {
      write(newValue)
    },
    { deep: true }
  )

  // ==================== Listen to storage events ====================

  if (listenToStorageChanges && typeof window !== 'undefined') {
    window.addEventListener('storage', (event: StorageEvent) => {
      if (event.key === key && event.newValue !== null) {
        try {
          data.value = serializer.read(event.newValue)
        } catch (error) {
          onError(error as Error)
        }
      }
    })
  }

  return data
}

// ==================== Specialized Composables ====================

/**
 * Store string in localStorage
 */
export function useLocalStorageString(
  key: string,
  defaultValue?: string
): Ref<string | undefined> {
  return useLocalStorage<string>(key, {
    defaultValue,
    serializer: {
      read: (v) => v,
      write: (v) => v,
    },
  })
}

/**
 * Store number in localStorage
 */
export function useLocalStorageNumber(
  key: string,
  defaultValue?: number
): Ref<number | undefined> {
  return useLocalStorage<number>(key, {
    defaultValue,
    serializer: {
      read: (v) => Number(v),
      write: (v) => String(v),
    },
  })
}

/**
 * Store boolean in localStorage
 */
export function useLocalStorageBoolean(
  key: string,
  defaultValue?: boolean
): Ref<boolean | undefined> {
  return useLocalStorage<boolean>(key, {
    defaultValue,
    serializer: {
      read: (v) => v === 'true',
      write: (v) => String(v),
    },
  })
}

/**
 * Store array in localStorage
 */
export function useLocalStorageArray<T = unknown>(
  key: string,
  defaultValue?: T[]
): Ref<T[] | undefined> {
  return useLocalStorage<T[]>(key, {
    defaultValue,
  })
}

/**
 * Store object in localStorage
 */
export function useLocalStorageObject<T extends Record<string, unknown>>(
  key: string,
  defaultValue?: T
): Ref<T | undefined> {
  return useLocalStorage<T>(key, {
    defaultValue,
  })
}

// ==================== Helper Functions ====================

/**
 * Get item from localStorage
 */
export function getLocalStorageItem<T = string>(key: string): T | null {
  if (typeof localStorage === 'undefined') return null

  try {
    const item = localStorage.getItem(key)
    if (item === null) return null

    // Try to parse as JSON
    try {
      return JSON.parse(item) as T
    } catch {
      // Return as string if not valid JSON
      return item as unknown as T
    }
  } catch (error) {
    console.error('[getLocalStorageItem]', error)
    return null
  }
}

/**
 * Set item in localStorage
 */
export function setLocalStorageItem<T>(key: string, value: T): boolean {
  if (typeof localStorage === 'undefined') return false

  try {
    const serialized = typeof value === 'string' ? value : JSON.stringify(value)
    localStorage.setItem(key, serialized)
    return true
  } catch (error) {
    console.error('[setLocalStorageItem]', error)
    return false
  }
}

/**
 * Remove item from localStorage
 */
export function removeLocalStorageItem(key: string): boolean {
  if (typeof localStorage === 'undefined') return false

  try {
    localStorage.removeItem(key)
    return true
  } catch (error) {
    console.error('[removeLocalStorageItem]', error)
    return false
  }
}

/**
 * Clear all items from localStorage
 */
export function clearLocalStorage(): boolean {
  if (typeof localStorage === 'undefined') return false

  try {
    localStorage.clear()
    return true
  } catch (error) {
    console.error('[clearLocalStorage]', error)
    return false
  }
}

/**
 * Get all localStorage keys
 */
export function getLocalStorageKeys(): string[] {
  if (typeof localStorage === 'undefined') return []

  try {
    return Object.keys(localStorage)
  } catch (error) {
    console.error('[getLocalStorageKeys]', error)
    return []
  }
}

/**
 * Get localStorage size in bytes
 */
export function getLocalStorageSize(): number {
  if (typeof localStorage === 'undefined') return 0

  try {
    let size = 0
    for (const key in localStorage) {
      if (localStorage.hasOwnProperty(key)) {
        size += localStorage[key].length + key.length
      }
    }
    return size
  } catch (error) {
    console.error('[getLocalStorageSize]', error)
    return 0
  }
}

/**
 * Check if localStorage is available
 */
export function isLocalStorageAvailable(): boolean {
  try {
    const test = '__localStorage_test__'
    localStorage.setItem(test, test)
    localStorage.removeItem(test)
    return true
  } catch {
    return false
  }
}
