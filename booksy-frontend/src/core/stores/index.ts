import { createPinia } from 'pinia'
import type { App } from 'vue'

const pinia = createPinia()

export function setupStore(app: App) {
  app.use(pinia)
}

export { pinia }
export default pinia

// Export all stores
export * from './modules/auth.store'
export * from './modules/notification.store'
export * from './modules/ui.store'
export * from './modules/locale.store'
export * from './modules/theme.store'
export * from './modules/cache.store'