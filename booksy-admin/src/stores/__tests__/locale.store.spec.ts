import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useLocaleStore } from '../locale.store'
import { Language, Direction, DateFormat, NumberFormat } from '../../types/locale.types'

const STORAGE_KEY = 'booksy_admin_locale_settings'

describe('locale store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('gives the Persian locale the Jalali calendar and Persian digits', () => {
    const store = useLocaleStore()

    store.setLocale(Language.Persian)

    expect(store.dateFormat).toBe(DateFormat.Jalaali)
    expect(store.numberFormat).toBe(NumberFormat.Persian)
    expect(store.direction).toBe(Direction.RTL)
  })

  it('gives the English locale the Gregorian calendar and LTR', () => {
    const store = useLocaleStore()

    store.setLocale(Language.English)

    expect(store.dateFormat).toBe(DateFormat.Gregorian)
    expect(store.direction).toBe(Direction.LTR)
  })

  /**
   * The calendar used to be persisted alongside the language and restored verbatim. A session
   * that had saved the old Gregorian default therefore kept Gregorian dates forever, even
   * after Persian was switched to the Jalali calendar. Only the language is a real preference.
   */
  it('re-derives the calendar on load instead of restoring a stale one', () => {
    localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({
        currentLocale: Language.Persian,
        direction: Direction.RTL,
        dateFormat: DateFormat.Gregorian,
        numberFormat: NumberFormat.Western,
      }),
    )

    const store = useLocaleStore()
    store.initializeFromStorage()

    expect(store.currentLocale).toBe(Language.Persian)
    expect(store.dateFormat).toBe(DateFormat.Jalaali)
    expect(store.numberFormat).toBe(NumberFormat.Persian)
  })

  it('restores the stored language itself', () => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ currentLocale: Language.English }))

    const store = useLocaleStore()
    store.initializeFromStorage()

    expect(store.currentLocale).toBe(Language.English)
    expect(store.dateFormat).toBe(DateFormat.Gregorian)
  })

  it('defaults to Persian when nothing is stored', () => {
    const store = useLocaleStore()
    store.initializeFromStorage()

    expect(store.currentLocale).toBe(Language.Persian)
    expect(store.dateFormat).toBe(DateFormat.Jalaali)
  })

  it('falls back to Persian when the stored settings are corrupt', () => {
    localStorage.setItem(STORAGE_KEY, '{not json')

    const store = useLocaleStore()
    store.initializeFromStorage()

    expect(store.currentLocale).toBe(Language.Persian)
    expect(store.dateFormat).toBe(DateFormat.Jalaali)
  })
})
