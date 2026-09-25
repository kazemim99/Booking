/**
 * Theme Store
 * Manages application theme, color scheme, and appearance settings
 */

import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'

// ==================== Types ====================

type ColorScheme = 'light' | 'dark' | 'auto'
type FontSize = 'small' | 'medium' | 'large' | 'xlarge'
type CompactMode = 'default' | 'compact' | 'comfortable'

interface ThemeState {
  colorScheme: ColorScheme
  fontSize: FontSize
  compactMode: CompactMode
  reducedMotion: boolean
  highContrast: boolean
  customColors?: ThemeColors
}

interface ThemeColors {
  primary?: string
  secondary?: string
  success?: string
  warning?: string
  danger?: string
  info?: string
}

// ==================== Constants ====================

const STORAGE_KEY = 'booksy_theme_settings'

const DEFAULT_COLOR_SCHEME: ColorScheme = 'light'
const DEFAULT_FONT_SIZE: FontSize = 'medium'
const DEFAULT_COMPACT_MODE: CompactMode = 'default'

const FONT_SIZE_VALUES = {
  small: '14px',
  medium: '16px',
  large: '18px',
  xlarge: '20px',
} as const

const CSS_VARIABLES = {
  fontSize: '--font-size-base',
  primary: '--color-primary',
  secondary: '--color-secondary',
  success: '--color-success',
  warning: '--color-warning',
  danger: '--color-danger',
  info: '--color-info',
} as const

// ==================== Store ====================

export const useThemeStore = defineStore('theme', () => {
  // ==================== State ====================

  const colorScheme = ref<ColorScheme>(DEFAULT_COLOR_SCHEME)
  const fontSize = ref<FontSize>(DEFAULT_FONT_SIZE)
  const compactMode = ref<CompactMode>(DEFAULT_COMPACT_MODE)
  const reducedMotion = ref(false)
  const highContrast = ref(false)
  const customColors = ref<ThemeColors | undefined>(undefined)

  // ==================== Computed ====================

  const isDark = computed(() => {
    if (colorScheme.value === 'auto') {
      return prefersDarkMode()
    }
    return colorScheme.value === 'dark'
  })

  const isLight = computed(() => !isDark.value)

  const currentFontSizeValue = computed(() => FONT_SIZE_VALUES[fontSize.value])

  const state = computed<ThemeState>(() => ({
    colorScheme: colorScheme.value,
    fontSize: fontSize.value,
    compactMode: compactMode.value,
    reducedMotion: reducedMotion.value,
    highContrast: highContrast.value,
    customColors: customColors.value,
  }))

  // ==================== Actions ====================

  /**
   * Set color scheme (light/dark/auto)
   */
  function setColorScheme(scheme: ColorScheme) {
    colorScheme.value = scheme
    applyColorScheme()
    persistToStorage()
    console.log(`[Theme] Color scheme set to ${scheme}`)
  }

  /**
   * Toggle between light and dark mode
   */
  function toggleColorScheme() {
    const newScheme = isDark.value ? 'light' : 'dark'
    setColorScheme(newScheme)
  }

  /**
   * Set font size
   */
  function setFontSize(size: FontSize) {
    fontSize.value = size
    applyFontSize()
    persistToStorage()
    console.log(`[Theme] Font size set to ${size} (${FONT_SIZE_VALUES[size]})`)
  }

  /**
   * Increase font size
   */
  function increaseFontSize() {
    const sizes: FontSize[] = ['small', 'medium', 'large', 'xlarge']
    const currentIndex = sizes.indexOf(fontSize.value)
    if (currentIndex < sizes.length - 1) {
      setFontSize(sizes[currentIndex + 1])
    }
  }

  /**
   * Decrease font size
   */
  function decreaseFontSize() {
    const sizes: FontSize[] = ['small', 'medium', 'large', 'xlarge']
    const currentIndex = sizes.indexOf(fontSize.value)
    if (currentIndex > 0) {
      setFontSize(sizes[currentIndex - 1])
    }
  }

  /**
   * Set compact mode
   */
  function setCompactMode(mode: CompactMode) {
    compactMode.value = mode
    applyCompactMode()
    persistToStorage()
    console.log(`[Theme] Compact mode set to ${mode}`)
  }

  /**
   * Toggle reduced motion
   */
  function setReducedMotion(enabled: boolean) {
    reducedMotion.value = enabled
    applyReducedMotion()
    persistToStorage()
    console.log(`[Theme] Reduced motion ${enabled ? 'enabled' : 'disabled'}`)
  }

  /**
   * Toggle high contrast
   */
  function setHighContrast(enabled: boolean) {
    highContrast.value = enabled
    applyHighContrast()
    persistToStorage()
    console.log(`[Theme] High contrast ${enabled ? 'enabled' : 'disabled'}`)
  }

  /**
   * Set custom theme colors
   */
  function setCustomColors(colors: ThemeColors) {
    customColors.value = colors
    applyCustomColors()
    persistToStorage()
    console.log('[Theme] Custom colors applied')
  }

  /**
   * Reset custom colors
   */
  function resetCustomColors() {
    customColors.value = undefined
    removeCustomColors()
    persistToStorage()
    console.log('[Theme] Custom colors reset')
  }

  /**
   * Apply color scheme to document
   */
  function applyColorScheme() {
    if (typeof document === 'undefined') return

    const root = document.documentElement

    if (isDark.value) {
      root.classList.add('dark')
      root.classList.remove('light')
    } else {
      root.classList.add('light')
      root.classList.remove('dark')
    }

    // Set color-scheme property for better browser support
    root.style.colorScheme = isDark.value ? 'dark' : 'light'
  }

  /**
   * Apply font size to document
   */
  function applyFontSize() {
    if (typeof document === 'undefined') return

    document.documentElement.style.setProperty(
      CSS_VARIABLES.fontSize,
      currentFontSizeValue.value
    )
  }

  /**
   * Apply compact mode to document
   */
  function applyCompactMode() {
    if (typeof document === 'undefined') return

    const root = document.documentElement
    root.classList.remove('compact-default', 'compact-compact', 'compact-comfortable')
    root.classList.add(`compact-${compactMode.value}`)
  }

  /**
   * Apply reduced motion preference
   */
  function applyReducedMotion() {
    if (typeof document === 'undefined') return

    if (reducedMotion.value) {
      document.documentElement.classList.add('reduce-motion')
    } else {
      document.documentElement.classList.remove('reduce-motion')
    }
  }

  /**
   * Apply high contrast mode
   */
  function applyHighContrast() {
    if (typeof document === 'undefined') return

    if (highContrast.value) {
      document.documentElement.classList.add('high-contrast')
    } else {
      document.documentElement.classList.remove('high-contrast')
    }
  }

  /**
   * Apply custom colors to CSS variables
   */
  function applyCustomColors() {
    if (typeof document === 'undefined' || !customColors.value) return

    const root = document.documentElement

    if (customColors.value.primary) {
      root.style.setProperty(CSS_VARIABLES.primary, customColors.value.primary)
    }
    if (customColors.value.secondary) {
      root.style.setProperty(CSS_VARIABLES.secondary, customColors.value.secondary)
    }
    if (customColors.value.success) {
      root.style.setProperty(CSS_VARIABLES.success, customColors.value.success)
    }
    if (customColors.value.warning) {
      root.style.setProperty(CSS_VARIABLES.warning, customColors.value.warning)
    }
    if (customColors.value.danger) {
      root.style.setProperty(CSS_VARIABLES.danger, customColors.value.danger)
    }
    if (customColors.value.info) {
      root.style.setProperty(CSS_VARIABLES.info, customColors.value.info)
    }
  }

  /**
   * Remove custom colors from CSS variables
   */
  function removeCustomColors() {
    if (typeof document === 'undefined') return

    const root = document.documentElement
    Object.values(CSS_VARIABLES).forEach((variable) => {
      root.style.removeProperty(variable)
    })
  }

  /**
   * Apply all theme settings
   */
  function applyAllSettings() {
    applyColorScheme()
    applyFontSize()
    applyCompactMode()
    applyReducedMotion()
    applyHighContrast()
    if (customColors.value) {
      applyCustomColors()
    }
  }

  /**
   * Persist theme settings to localStorage
   */
  function persistToStorage() {
    if (typeof localStorage === 'undefined') return

    const settings: ThemeState = {
      colorScheme: colorScheme.value,
      fontSize: fontSize.value,
      compactMode: compactMode.value,
      reducedMotion: reducedMotion.value,
      highContrast: highContrast.value,
      customColors: customColors.value,
    }

    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(settings))
    } catch (error) {
      console.error('[Theme] Failed to persist settings:', error)
    }
  }

  /**
   * Load theme settings from localStorage
   */
  function loadFromStorage() {
    if (typeof localStorage === 'undefined') return

    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (!stored) return

      const settings: ThemeState = JSON.parse(stored)

      if (settings.colorScheme) {
        colorScheme.value = settings.colorScheme
      }

      if (settings.fontSize) {
        fontSize.value = settings.fontSize
      }

      if (settings.compactMode) {
        compactMode.value = settings.compactMode
      }

      if (settings.reducedMotion !== undefined) {
        reducedMotion.value = settings.reducedMotion
      }

      if (settings.highContrast !== undefined) {
        highContrast.value = settings.highContrast
      }

      if (settings.customColors) {
        customColors.value = settings.customColors
      }

      console.log('[Theme] Settings loaded from storage')
    } catch (error) {
      console.error('[Theme] Failed to load settings:', error)
    }
  }

  /**
   * Reset to default settings
   */
  function resetToDefaults() {
    colorScheme.value = DEFAULT_COLOR_SCHEME
    fontSize.value = DEFAULT_FONT_SIZE
    compactMode.value = DEFAULT_COMPACT_MODE
    reducedMotion.value = false
    highContrast.value = false
    customColors.value = undefined

    applyAllSettings()
    persistToStorage()

    console.log('[Theme] Reset to default settings')
  }

  /**
   * Initialize store
   */
  function initialize() {
    loadFromStorage()
    detectSystemPreferences()
    applyAllSettings()
    setupMediaQueryListeners()
  }

  /**
   * Detect system preferences
   */
  function detectSystemPreferences() {
    if (typeof window === 'undefined') return

    // Detect dark mode preference
    if (colorScheme.value === 'auto') {
      applyColorScheme()
    }

    // Detect reduced motion preference
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      reducedMotion.value = true
      applyReducedMotion()
    }

    // Detect high contrast preference
    if (window.matchMedia('(prefers-contrast: high)').matches) {
      highContrast.value = true
      applyHighContrast()
    }
  }

  /**
   * Setup media query listeners for system preferences
   */
  function setupMediaQueryListeners() {
    if (typeof window === 'undefined') return

    // Listen for dark mode changes
    const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)')
    darkModeQuery.addEventListener('change', () => {
      if (colorScheme.value === 'auto') {
        applyColorScheme()
      }
    })

    // Listen for reduced motion changes
    const reducedMotionQuery = window.matchMedia('(prefers-reduced-motion: reduce)')
    reducedMotionQuery.addEventListener('change', (e) => {
      reducedMotion.value = e.matches
      applyReducedMotion()
    })

    // Listen for contrast changes
    const highContrastQuery = window.matchMedia('(prefers-contrast: high)')
    highContrastQuery.addEventListener('change', (e) => {
      highContrast.value = e.matches
      applyHighContrast()
    })
  }

  // ==================== Watchers ====================

  // Watch color scheme changes
  watch(colorScheme, () => {
    applyColorScheme()
  })

  // ==================== Return ====================

  return {
    // State
    colorScheme,
    fontSize,
    compactMode,
    reducedMotion,
    highContrast,
    customColors,

    // Computed
    isDark,
    isLight,
    currentFontSizeValue,
    state,

    // Actions
    setColorScheme,
    toggleColorScheme,
    setFontSize,
    increaseFontSize,
    decreaseFontSize,
    setCompactMode,
    setReducedMotion,
    setHighContrast,
    setCustomColors,
    resetCustomColors,
    applyAllSettings,
    persistToStorage,
    loadFromStorage,
    resetToDefaults,
    initialize,
  }
})

// ==================== Helper Functions ====================

/**
 * Check if user prefers dark mode
 */
function prefersDarkMode(): boolean {
  if (typeof window === 'undefined') return false
  return window.matchMedia('(prefers-color-scheme: dark)').matches
}

/**
 * Check if user prefers reduced motion
 */
export function prefersReducedMotion(): boolean {
  if (typeof window === 'undefined') return false
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches
}

/**
 * Check if user prefers high contrast
 */
export function prefersHighContrast(): boolean {
  if (typeof window === 'undefined') return false
  return window.matchMedia('(prefers-contrast: high)').matches
}
