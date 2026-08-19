import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { formatDate, formatDateTime } from '../date'
import { useLocaleStore } from '../../stores/locale.store'
import { Language, DateFormat } from '../../types/locale.types'

/**
 * The panel is Persian-first but rendered every date through dayjs's Gregorian
 * 'MMM DD, YYYY', so an admin working in Persian saw "Aug 11, 2026".
 */
describe('date formatting', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  const registeredAt = '2026-08-11T16:45:55.458241+03:30'

  it('renders Jalali dates under the Persian locale', () => {
    useLocaleStore().setLocale(Language.Persian)

    const formatted = formatDate(registeredAt)

    expect(formatted).toContain('۱۴۰۵')
    expect(formatted).toContain('مرداد')
    expect(formatted).not.toMatch(/2026|Aug/)
  })

  it('renders Gregorian dates under the English locale', () => {
    useLocaleStore().setLocale(Language.English)

    expect(formatDate(registeredAt)).toContain('2026')
  })

  it('follows a locale switch rather than caching the first calendar used', () => {
    const store = useLocaleStore()

    store.setLocale(Language.English)
    expect(formatDate(registeredAt)).toContain('2026')

    store.setLocale(Language.Persian)
    expect(formatDate(registeredAt)).toContain('۱۴۰۵')
  })

  it('includes the time of day when asked', () => {
    useLocaleStore().setLocale(Language.Persian)

    expect(formatDateTime(registeredAt)).toMatch(/[۰-۹]{2}:[۰-۹]{2}/)
  })

  it('renders nothing for a missing date instead of "Invalid Date"', () => {
    useLocaleStore().setLocale(Language.Persian)

    expect(formatDate(undefined)).toBe('')
    expect(formatDate(null)).toBe('')
    expect(formatDate('')).toBe('')
  })

  it('renders nothing for an unparseable date', () => {
    useLocaleStore().setLocale(Language.Persian)

    expect(formatDate('not-a-date')).toBe('')
  })

  it('selects the Persian calendar from the locale config, not a stored preference', () => {
    // dateFormat is derived from the language; it is not independently persisted.
    useLocaleStore().setLocale(Language.Persian)

    expect(useLocaleStore().dateFormat).toBe(DateFormat.Jalaali)
  })
})
