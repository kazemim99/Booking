import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createI18n } from 'vue-i18n' // vue-i18n v10+ required for Vue 3
import App from './App.vue'
import router from './core/router'
import { useRTLInstance } from './core/composables/useRTL'
import { vClickOutside } from './shared/directives/v-click-outside'

// Import global styles
import './assets/styles/main.scss'

// Import translations
import en from './locales/en.json'
import ar from './locales/ar.json'

// Create i18n instance
const i18n = createI18n({
  legacy: false,
  locale: 'en',
  fallbackLocale: 'en',
  messages: {
    en,
    ar,
  },
  globalInjection: true,
})

// Create Vue app
const app = createApp(App)

// Setup Pinia
const pinia = createPinia()
app.use(pinia)

// Setup Router
app.use(router)

// Setup i18n
app.use(i18n)

// Initialize RTL support
const rtl = useRTLInstance()
rtl.initializeRTL()

// Register global directives
app.directive('click-outside', vClickOutside)

// Mount app
app.mount('#app')

console.log('🚀 Booksy Frontend Started')
console.log('Environment:', import.meta.env.MODE)
console.log('API URL:', import.meta.env.VITE_API_BASE_URL)
console.log('Direction:', rtl.direction.value)
