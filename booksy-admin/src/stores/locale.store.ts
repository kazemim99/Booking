/**
 * Locale Store - Manages language, direction, and locale preferences
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { LocaleState } from '../types/locale.types'
import { Language, Direction, DateFormat, NumberFormat, LOCALE_CONFIG } from '../types/locale.types'

const STORAGE_KEY = 'booksy_admin_locale_settings'

export const useLocaleStore = defineStore('locale', () => {
  // State
  const currentLocale = ref<Language>(Language.Persian)
  const direction = ref<Direction>(Direction.RTL)
  const dateFormat = ref<DateFormat>(DateFormat.Gregorian)
  const numberFormat = ref<NumberFormat>(NumberFormat.Western)

  // Getters
  const isRTL = computed(() => direction.value === Direction.RTL)
  const isPersian = computed(() => currentLocale.value === Language.Persian)
  const isEnglish = computed(() => currentLocale.value === Language.English)
  const localeName = computed(() => LOCALE_CONFIG[currentLocale.value].name)

  // Actions
  function setLocale(locale: Language) {
    currentLocale.value = locale
    const config = LOCALE_CONFIG[locale]
    direction.value = config.direction
    dateFormat.value = config.dateFormat
    numberFormat.value = config.numberFormat

    // Update document attributes
    updateDocumentDirection()
    persistToStorage()
  }

  function toggleLocale() {
    const newLocale = currentLocale.value === Language.Persian
      ? Language.English
      : Language.Persian
    setLocale(newLocale)
  }

  function updateDocumentDirection() {
    if (typeof document !== 'undefined') {
      document.documentElement.dir = direction.value
      document.documentElement.lang = currentLocale.value

      // Update body classes
      document.body.classList.remove('rtl', 'ltr')
      document.body.classList.add(direction.value)
    }
  }

  function persistToStorage() {
    if (typeof localStorage !== 'undefined') {
      const state: LocaleState = {
        currentLocale: currentLocale.value,
        direction: direction.value,
        dateFormat: dateFormat.value,
        numberFormat: numberFormat.value,
      }
      localStorage.setItem(STORAGE_KEY, JSON.stringify(state))
    }
  }

  function initializeFromStorage() {
    if (typeof localStorage !== 'undefined') {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (stored) {
        try {
          const state: LocaleState = JSON.parse(stored)
          currentLocale.value = state.currentLocale || Language.Persian
          direction.value = state.direction || Direction.RTL
          dateFormat.value = state.dateFormat || DateFormat.Gregorian
          numberFormat.value = state.numberFormat || NumberFormat.Western
          updateDocumentDirection()
          return
        } catch (error) {
          console.error('Failed to parse stored locale settings:', error)
        }
      }
    }

    // If no stored settings, use defaults
    setLocale(Language.Persian)
  }

  return {
    // State
    currentLocale,
    direction,
    dateFormat,
    numberFormat,

    // Getters
    isRTL,
    isPersian,
    isEnglish,
    localeName,

    // Actions
    setLocale,
    toggleLocale,
    initializeFromStorage,
    updateDocumentDirection,
  }
})
