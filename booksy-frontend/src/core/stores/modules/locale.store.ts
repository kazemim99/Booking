/**
 * Locale Store
 * Manages internationalization, locale settings, and RTL/LTR direction
 */

import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import type { Language, DateFormat, NumberFormat } from '@/core/types/enums.types'

// ==================== Types ====================

interface LocaleState {
  currentLocale: Language
  fallbackLocale: Language
  availableLocales: Language[]
  direction: 'ltr' | 'rtl'
  dateFormat: DateFormat
  numberFormat: NumberFormat
  timezone: string
}

interface LocaleSettings {
  locale?: Language
  dateFormat?: DateFormat
  numberFormat?: NumberFormat
  timezone?: string
}

// ==================== Constants ====================

const STORAGE_KEY = 'booksy_locale_settings'

const DEFAULT_LOCALE: Language = 'fa' as Language
const DEFAULT_FALLBACK: Language = 'en' as Language
const DEFAULT_TIMEZONE = 'Asia/Tehran'

const LOCALE_CONFIG = {
  fa: {
    name: 'فارسی',
    direction: 'rtl' as const,
    dateFormat: 'jalaali' as DateFormat,
    numberFormat: 'persian' as NumberFormat,
  },
  en: {
    name: 'English',
    direction: 'ltr' as const,
    dateFormat: 'gregorian' as DateFormat,
    numberFormat: 'western' as NumberFormat,
  },
} as const

// ==================== Store ====================

export const useLocaleStore = defineStore('locale', () => {
  // ==================== State ====================

  const currentLocale = ref<Language>(DEFAULT_LOCALE)
  const fallbackLocale = ref<Language>(DEFAULT_FALLBACK)
  const availableLocales = ref<Language[]>(['fa' as Language, 'en' as Language])
  const direction = ref<'ltr' | 'rtl'>('rtl')
  const dateFormat = ref<DateFormat>('jalaali' as DateFormat)
  const numberFormat = ref<NumberFormat>('persian' as NumberFormat)
  const timezone = ref<string>(DEFAULT_TIMEZONE)

  // ==================== Computed ====================

  const isRTL = computed(() => direction.value === 'rtl')
  const isLTR = computed(() => direction.value === 'ltr')
  const isPersian = computed(() => currentLocale.value === 'fa')
  const isEnglish = computed(() => currentLocale.value === 'en')

  const localeConfig = computed(() => LOCALE_CONFIG[currentLocale.value])
  const localeName = computed(() => localeConfig.value.name)

  const state = computed<LocaleState>(() => ({
    currentLocale: currentLocale.value,
    fallbackLocale: fallbackLocale.value,
    availableLocales: availableLocales.value,
    direction: direction.value,
    dateFormat: dateFormat.value,
    numberFormat: numberFormat.value,
    timezone: timezone.value,
  }))

  // ==================== Actions ====================

  /**
   * Set current locale and update related settings
   */
  function setLocale(locale: Language) {
    if (!availableLocales.value.includes(locale)) {
      console.warn(`Locale ${locale} is not available. Using ${currentLocale.value}`)
      return
    }

    currentLocale.value = locale

    // Update direction based on locale
    const config = LOCALE_CONFIG[locale]
    direction.value = config.direction
    dateFormat.value = config.dateFormat
    numberFormat.value = config.numberFormat

    // Update HTML attributes
    updateHTMLAttributes()

    // Persist to localStorage
    persistToStorage()

    console.log(`[Locale] Switched to ${locale} (${config.name})`)
  }

  /**
   * Set date format preference
   */
  function setDateFormat(format: DateFormat) {
    dateFormat.value = format
    persistToStorage()
    console.log(`[Locale] Date format set to ${format}`)
  }

  /**
   * Set number format preference
   */
  function setNumberFormat(format: NumberFormat) {
    numberFormat.value = format
    persistToStorage()
    console.log(`[Locale] Number format set to ${format}`)
  }

  /**
   * Set timezone
   */
  function setTimezone(tz: string) {
    timezone.value = tz
    persistToStorage()
    console.log(`[Locale] Timezone set to ${tz}`)
  }

  /**
   * Toggle between Persian and English
   */
  function toggleLocale() {
    const newLocale = currentLocale.value === 'fa' ? ('en' as Language) : ('fa' as Language)
    setLocale(newLocale)
  }

  /**
   * Apply multiple settings at once
   */
  function applySettings(settings: LocaleSettings) {
    if (settings.locale) {
      setLocale(settings.locale)
    }
    if (settings.dateFormat) {
      setDateFormat(settings.dateFormat)
    }
    if (settings.numberFormat) {
      setNumberFormat(settings.numberFormat)
    }
    if (settings.timezone) {
      setTimezone(settings.timezone)
    }
  }

  /**
   * Update HTML document attributes
   */
  function updateHTMLAttributes() {
    if (typeof document !== 'undefined') {
      document.documentElement.setAttribute('lang', currentLocale.value)
      document.documentElement.setAttribute('dir', direction.value)
    }
  }

  /**
   * Persist locale settings to localStorage
   */
  function persistToStorage() {
    if (typeof localStorage === 'undefined') return

    const settings: LocaleState = {
      currentLocale: currentLocale.value,
      fallbackLocale: fallbackLocale.value,
      availableLocales: availableLocales.value,
      direction: direction.value,
      dateFormat: dateFormat.value,
      numberFormat: numberFormat.value,
      timezone: timezone.value,
    }

    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(settings))
    } catch (error) {
      console.error('[Locale] Failed to persist settings:', error)
    }
  }

  /**
   * Load locale settings from localStorage
   */
  function loadFromStorage() {
    if (typeof localStorage === 'undefined') return

    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (!stored) return

      const settings: LocaleState = JSON.parse(stored)

      if (settings.currentLocale && availableLocales.value.includes(settings.currentLocale)) {
        currentLocale.value = settings.currentLocale
      }

      if (settings.dateFormat) {
        dateFormat.value = settings.dateFormat
      }

      if (settings.numberFormat) {
        numberFormat.value = settings.numberFormat
      }

      if (settings.timezone) {
        timezone.value = settings.timezone
      }

      if (settings.direction) {
        direction.value = settings.direction
      }

      // Update HTML attributes after loading
      updateHTMLAttributes()

      console.log('[Locale] Settings loaded from storage')
    } catch (error) {
      console.error('[Locale] Failed to load settings:', error)
    }
  }

  /**
   * Reset to default settings
   */
  function resetToDefaults() {
    currentLocale.value = DEFAULT_LOCALE
    fallbackLocale.value = DEFAULT_FALLBACK
    direction.value = LOCALE_CONFIG[DEFAULT_LOCALE].direction
    dateFormat.value = LOCALE_CONFIG[DEFAULT_LOCALE].dateFormat
    numberFormat.value = LOCALE_CONFIG[DEFAULT_LOCALE].numberFormat
    timezone.value = DEFAULT_TIMEZONE

    updateHTMLAttributes()
    persistToStorage()

    console.log('[Locale] Reset to default settings')
  }

  /**
   * Initialize store
   */
  function initialize() {
    loadFromStorage()
    updateHTMLAttributes()
  }

  // ==================== Watchers ====================

  // Watch locale changes to update document
  watch(currentLocale, () => {
    updateHTMLAttributes()
  })

  // ==================== Return ====================

  return {
    // State
    currentLocale,
    fallbackLocale,
    availableLocales,
    direction,
    dateFormat,
    numberFormat,
    timezone,

    // Computed
    isRTL,
    isLTR,
    isPersian,
    isEnglish,
    localeConfig,
    localeName,
    state,

    // Actions
    setLocale,
    setDateFormat,
    setNumberFormat,
    setTimezone,
    toggleLocale,
    applySettings,
    updateHTMLAttributes,
    persistToStorage,
    loadFromStorage,
    resetToDefaults,
    initialize,
  }
})

// ==================== Helper Functions ====================

/**
 * Get Persian/Arabic numerals for a number
 */
export function toPersianNumber(num: number | string): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return String(num).replace(/\d/g, (digit) => persianDigits[parseInt(digit)])
}

/**
 * Get Western numerals from Persian/Arabic numerals
 */
export function toWesternNumber(num: string): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  const arabicDigits = ['٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩']

  let result = num

  persianDigits.forEach((persian, index) => {
    result = result.replace(new RegExp(persian, 'g'), String(index))
  })

  arabicDigits.forEach((arabic, index) => {
    result = result.replace(new RegExp(arabic, 'g'), String(index))
  })

  return result
}

/**
 * Format number according to current locale
 */
export function formatNumber(
  num: number,
  format?: NumberFormat
): string {
  const store = useLocaleStore()
  const numberFormat = format || store.numberFormat

  if (numberFormat === 'persian') {
    return toPersianNumber(num)
  }

  return String(num)
}
