/**
 * TypeScript types and enums for locale and RTL support
 */

export const Language = {
  Persian: 'fa',
  English: 'en',
} as const

export type Language = typeof Language[keyof typeof Language]

export const Direction = {
  RTL: 'rtl',
  LTR: 'ltr',
} as const

export type Direction = typeof Direction[keyof typeof Direction]

export const DateFormat = {
  Gregorian: 'gregorian',
  Jalaali: 'jalaali',
} as const

export type DateFormat = typeof DateFormat[keyof typeof DateFormat]

export const NumberFormat = {
  Western: 'western',
  Persian: 'persian',
} as const

export type NumberFormat = typeof NumberFormat[keyof typeof NumberFormat]

export interface LocaleState {
  currentLocale: Language
  direction: Direction
  dateFormat: DateFormat
  numberFormat: NumberFormat
}

export interface LocaleConfig {
  name: string
  direction: Direction
  dateFormat: DateFormat
  numberFormat: NumberFormat
}

export type SupportedLocale = 'fa' | 'en'

export const LOCALE_CONFIG: Record<Language, LocaleConfig> = {
  [Language.Persian]: {
    name: 'فارسی',
    direction: Direction.RTL,
    dateFormat: DateFormat.Gregorian,
    numberFormat: NumberFormat.Western,
  },
  [Language.English]: {
    name: 'English',
    direction: Direction.LTR,
    dateFormat: DateFormat.Gregorian,
    numberFormat: NumberFormat.Western,
  },
}

export const RTL_LANGUAGES = new Set<string>([
  Language.Persian,
  'ar', // Arabic
  'he', // Hebrew
  'ur', // Urdu
])
