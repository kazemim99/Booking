import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createI18n } from 'vue-i18n'
import Antd from 'ant-design-vue'
import router from './router'
import App from './App.vue'

import 'ant-design-vue/dist/reset.css'
import '../../packages/design-tokens/tokens.css' // shared @booksy/tokens — brand source of truth
import './assets/styles/main.css'

// Import translation files
import fa from './locales/fa.json'
import en from './locales/en.json'

// Create i18n instance
const i18n = createI18n({
  legacy: false,
  locale: 'fa',
  fallbackLocale: 'en',
  messages: { fa, en },
  globalInjection: true,
})

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(i18n)
app.use(router)
app.use(Antd)

// Initialize locale from storage after pinia is ready
import { useLocaleStore } from './stores/locale.store'
const localeStore = useLocaleStore()
localeStore.initializeFromStorage()

app.mount('#app')
