// src/core/composables/useRTL.ts
import { ref, computed, watch } from 'vue'

export type Direction = 'ltr' | 'rtl'
export type Language = 'en' | 'ar' | 'he' | 'fa' | 'ur'

interface RTLLanguages {
  [key: string]: boolean
}

const RTL_LANGUAGES: RTLLanguages = {
  ar: true, // Arabic
  he: true, // Hebrew
  fa: true, // Persian/Farsi
  ur: true, // Urdu
}

const currentLanguage = ref<Language>('en')
const isRTL = ref<boolean>(false)

export function useRTL() {
  const direction = computed<Direction>(() => (isRTL.value ? 'rtl' : 'ltr'))

  function setLanguage(lang: Language): void {
    currentLanguage.value = lang
    isRTL.value = RTL_LANGUAGES[lang] || false
    updateDocumentDirection()
    saveLanguagePreference(lang)
  }

  function toggleDirection(): void {
    isRTL.value = !isRTL.value
    updateDocumentDirection()
  }

  function updateDocumentDirection(): void {
    const html = document.documentElement
    html.setAttribute('dir', direction.value)
    html.setAttribute('lang', currentLanguage.value)

    document.body.classList.toggle('rtl', isRTL.value)
    document.body.classList.toggle('ltr', !isRTL.value)
  }

  function saveLanguagePreference(lang: Language): void {
    try {
      localStorage.setItem('booksy_language', lang)
      localStorage.setItem('booksy_direction', direction.value)
    } catch (error) {
      console.error('Failed to save language preference:', error)
    }
  }

  function loadLanguagePreference(): void {
    try {
      const savedLang = localStorage.getItem('booksy_language') as Language
      if (savedLang) {
        setLanguage(savedLang)
      }
    } catch (error) {
      console.error('Failed to load language preference:', error)
    }
  }

  function initializeRTL(): void {
    loadLanguagePreference()
    updateDocumentDirection()
  }

  function getTextAlign(align: 'left' | 'right' | 'center' = 'left'): string {
    if (align === 'center') return 'center'
    if (!isRTL.value) return align
    return align === 'left' ? 'right' : 'left'
  }

  function getFloat(float: 'left' | 'right'): string {
    if (!isRTL.value) return float
    return float === 'left' ? 'right' : 'left'
  }

  function getSpacing(spacing: string): string {
    if (!isRTL.value) return spacing

    const values = spacing.trim().split(/\s+/)
    if (values.length === 4) {
      return `${values[0]} ${values[3]} ${values[2]} ${values[1]}`
    }
    return spacing
  }

  watch(currentLanguage, () => {
    updateDocumentDirection()
  })

  return {
    currentLanguage: computed(() => currentLanguage.value),
    isRTL: computed(() => isRTL.value),
    direction,
    setLanguage,
    toggleDirection,
    initializeRTL,
    getTextAlign,
    getFloat,
    getSpacing,
  }
}

let instance: ReturnType<typeof useRTL> | null = null

export function useRTLInstance(): ReturnType<typeof useRTL> {
  if (!instance) {
    instance = useRTL()
  }
  return instance
}
