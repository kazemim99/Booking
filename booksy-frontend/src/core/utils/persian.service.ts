/**
 * Persian language utilities
 * Centralized Persian number conversion and localization utilities
 */

/**
 * Convert English/Arabic digits to Persian digits
 * @param value - Number or string containing digits
 * @returns String with Persian digits (۰-۹)
 * @example
 * toPersianDigits(123) // "۱۲۳"
 * toPersianDigits("2024/01/15") // "۲۰۲۴/۰۱/۱۵"
 */
export function toPersianDigits(value: number | string): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return String(value).replace(/\d/g, (digit) => persianDigits[parseInt(digit)])
}

/**
 * Convert Persian digits to English digits
 * @param text - String containing Persian digits
 * @returns String with English digits (0-9)
 * @example
 * fromPersianDigits("۱۲۳") // "123"
 * fromPersianDigits("۲۰۲۴/۰۱/۱۵") // "2024/01/15"
 */
export function fromPersianDigits(text: string): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  let result = text
  persianDigits.forEach((persian, index) => {
    result = result.replace(new RegExp(persian, 'g'), String(index))
  })
  return result
}

/**
 * Format number with Persian digits and thousand separators
 * @param num - Number to format
 * @returns Formatted string with Persian digits and commas
 * @example
 * formatPersianNumber(1234567) // "۱,۲۳۴,۵۶۷"
 */
export function formatPersianNumber(num: number): string {
  const formatted = num.toLocaleString('en-US') // Use comma separator
  return toPersianDigits(formatted)
}

/**
 * Persian weekday names (Saturday is first day of week in Persian calendar)
 */
export const PERSIAN_WEEKDAYS = [
  'یکشنبه',    // Sunday
  'دوشنبه',    // Monday
  'سه‌شنبه',   // Tuesday
  'چهارشنبه',  // Wednesday
  'پنج‌شنبه',  // Thursday
  'جمعه',      // Friday
  'شنبه'       // Saturday
]

/**
 * Persian month names (Jalali/Solar Hijri calendar)
 */
export const PERSIAN_MONTHS = [
  'فروردین',
  'اردیبهشت',
  'خرداد',
  'تیر',
  'مرداد',
  'شهریور',
  'مهر',
  'آبان',
  'آذر',
  'دی',
  'بهمن',
  'اسفند'
]

/**
 * Get Persian weekday name for a date
 * @param date - Date object
 * @returns Persian weekday name
 * @example
 * getPersianWeekday(new Date('2024-01-15')) // "دوشنبه"
 */
export function getPersianWeekday(date: Date): string {
  return PERSIAN_WEEKDAYS[date.getDay()]
}

/**
 * Get Persian month name by index (0-11)
 * @param monthIndex - Month index (0-11)
 * @returns Persian month name
 * @example
 * getPersianMonth(0) // "فروردین"
 */
export function getPersianMonth(monthIndex: number): string {
  if (monthIndex < 0 || monthIndex > 11) return ''
  return PERSIAN_MONTHS[monthIndex]
}
