import { useLocaleStore } from '../stores/locale.store'
import { DateFormat } from '../types/locale.types'

// Jalali rendering comes from the platform's own calendar support rather than a
// conversion library: 'fa-IR-u-ca-persian' selects the Persian calendar, and the
// fa-IR locale supplies Persian digits and month names.
const JALALI_LOCALE = 'fa-IR-u-ca-persian'
const GREGORIAN_LOCALE = 'en-US'

const DATE_ONLY: Intl.DateTimeFormatOptions = { year: 'numeric', month: 'long', day: 'numeric' }
const DATE_TIME: Intl.DateTimeFormatOptions = {
  year: 'numeric',
  month: 'long',
  day: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
}

const format = (date: string | Date | undefined | null, options: Intl.DateTimeFormatOptions): string => {
  if (!date) return ''

  const parsed = date instanceof Date ? date : new Date(date)
  if (Number.isNaN(parsed.getTime())) return ''

  const useJalali = useLocaleStore().dateFormat === DateFormat.Jalaali
  return new Intl.DateTimeFormat(useJalali ? JALALI_LOCALE : GREGORIAN_LOCALE, options).format(parsed)
}

/** Calendar date in the active locale — Jalali under Persian, Gregorian under English. */
export const formatDate = (date: string | Date | undefined | null): string => format(date, DATE_ONLY)

/** Date and time in the active locale. */
export const formatDateTime = (date: string | Date | undefined | null): string => format(date, DATE_TIME)
