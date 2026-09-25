class SessionStorageService {
  set<T>(key: string, value: T): void {
    try {
      const serialized = JSON.stringify(value)
      sessionStorage.setItem(key, serialized)
    } catch (error) {
      console.error(`Error saving to sessionStorage: ${key}`, error)
    }
  }

  get<T>(key: string): T | null {
    try {
      const item = sessionStorage.getItem(key)
      return item ? JSON.parse(item) : null
    } catch (error) {
      console.error(`Error reading from sessionStorage: ${key}`, error)
      return null
    }
  }

  remove(key: string): void {
    try {
      sessionStorage.removeItem(key)
    } catch (error) {
      console.error(`Error removing from sessionStorage: ${key}`, error)
    }
  }

  clear(): void {
    try {
      sessionStorage.clear()
    } catch (error) {
      console.error('Error clearing sessionStorage', error)
    }
  }

  has(key: string): boolean {
    return sessionStorage.getItem(key) !== null
  }
}

export const sessionStorageService = new SessionStorageService()
export default sessionStorageService