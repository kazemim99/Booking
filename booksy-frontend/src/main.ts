import { createApp, watch } from 'vue'
import { createPinia } from 'pinia'
import { createI18n } from 'vue-i18n'
import App from './App.vue'
import router from './core/router'
import { useRTLInstance } from './core/composables/useRTL'
import { vClickOutside } from './shared/directives/v-click-outside'
// Import global styles
import './assets/styles/main.scss'

// Import translations
import fa from './locales/fa.json'
import en from './locales/en.json'
import ar from './locales/ar.json'

// Create i18n instance
const i18n = createI18n({
  legacy: false,
  locale: 'fa',
  fallbackLocale: 'en',
  messages: {
    fa,
    en,
    ar,
  },
  globalInjection: true,
  messageCompiler: undefined,

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

// Sync i18n locale with RTL language
i18n.global.locale.value = rtl.currentLanguage.value as 'fa' | 'en' | 'ar'

// Watch for locale changes and update HTML lang attribute for font application
watch(() => i18n.global.locale.value, (newLocale) => {
  document.documentElement.setAttribute('lang', newLocale)
  console.log('🌐 Locale changed to:', newLocale)
})

// Set initial HTML lang attribute
document.documentElement.setAttribute('lang', i18n.global.locale.value)

// Register global directives
app.directive('click-outside', vClickOutside)

// Mount app
app.mount('#app')

console.log('🚀 Booksy Frontend Started')
console.log('Environment:', import.meta.env.MODE)
console.log('API URL:', import.meta.env.VITE_USER_MANAGEMENT_API_URL)
console.log('API URL:', import.meta.env.VITE_SERVICE_CATEGORY_API_URL)
console.log('Direction:', rtl.direction.value)
