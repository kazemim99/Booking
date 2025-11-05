// dateHelpers.ts - Date utility functions for calendar operations
// Supports both Gregorian and Jalali (Persian) calendars

// ============================================
// Jalali Calendar Utilities
// ============================================

/**
 * Convert Gregorian date to Jalali (Persian) date
 * Algorithm from: https://github.com/jalaali/jalaali-js
 */
export function gregorianToJalali(gYear: number, gMonth: number, gDay: number): { jy: number; jm: number; jd: number } {
  const gy = gYear - 1600
  const gm = gMonth - 1
  const gd = gDay - 1

  let gDayNo = 365 * gy + Math.floor((gy + 3) / 4) - Math.floor((gy + 99) / 100) + Math.floor((gy + 399) / 400)

  for (let i = 0; i < gm; ++i) {
    gDayNo += [31, 28 + (((gy % 4 === 0) && (gy % 100 !== 0)) || (gy % 400 === 0) ? 1 : 0), 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][i]
  }
  gDayNo += gd

  let jDayNo = gDayNo - 79

  const jNp = Math.floor(jDayNo / 12053)
  jDayNo %= 12053

  let jy = 979 + 33 * jNp + 4 * Math.floor(jDayNo / 1461)

  jDayNo %= 1461

  if (jDayNo >= 366) {
    jy += Math.floor((jDayNo - 1) / 365)
    jDayNo = (jDayNo - 1) % 365
  }

  const jm = jDayNo < 186 ? 1 + Math.floor(jDayNo / 31) : 7 + Math.floor((jDayNo - 186) / 30)
  const jd = 1 + (jDayNo < 186 ? jDayNo % 31 : (jDayNo - 186) % 30)

  return { jy, jm, jd }
}

/**
 * Convert Jalali (Persian) date to Gregorian date
 */
export function jalaliToGregorian(jYear: number, jMonth: number, jDay: number): { gy: number; gm: number; gd: number } {
  const jy = jYear - 979
  const jm = jMonth - 1
  const jd = jDay - 1

  let jDayNo = 365 * jy + Math.floor(jy / 33) * 8 + Math.floor(((jy % 33) + 3) / 4)
  for (let i = 0; i < jm; ++i) {
    jDayNo += [31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 29][i]
  }

  jDayNo += jd

  let gDayNo = jDayNo + 79

  let gy = 1600 + 400 * Math.floor(gDayNo / 146097)
  gDayNo %= 146097

  let leap = true
  if (gDayNo >= 36525) {
    gDayNo--
    gy += 100 * Math.floor(gDayNo / 36524)
    gDayNo %= 36524

    if (gDayNo >= 365) {
      gDayNo++
    } else {
      leap = false
    }
  }

  gy += 4 * Math.floor(gDayNo / 1461)
  gDayNo %= 1461

  if (gDayNo >= 366) {
    leap = false

    gDayNo--
    gy += Math.floor(gDayNo / 365)
    gDayNo %= 365
  }

  const monthDays = [31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
  let gm = 0
  for (let i = 0; gDayNo >= monthDays[i] && i < 12; i++) {
    gDayNo -= monthDays[i]
    gm++
  }

  const gd = gDayNo + 1
  return { gy, gm: gm + 1, gd }
}

/**
 * Persian month names
 */
export const PERSIAN_MONTHS = [
  'فروردین', // Farvardin
  'اردیبهشت', // Ordibehesht
  'خرداد', // Khordad
  'تیر', // Tir
  'مرداد', // Mordad
  'شهریور', // Shahrivar
  'مهر', // Mehr
  'آبان', // Aban
  'آذر', // Azar
  'دی', // Dey
  'بهمن', // Bahman
  'اسفند', // Esfand
]

/**
 * Persian weekday names (Saturday is first day of week in Persian calendar)
 */
export const PERSIAN_WEEKDAYS = [
  'یکشنبه', // Sunday
  'دوشنبه', // Monday
  'سه‌شنبه', // Tuesday
  'چهارشنبه', // Wednesday
  'پنجشنبه', // Thursday
  'جمعه', // Friday
  'شنبه', // Saturday
]

export const PERSIAN_WEEKDAYS_SHORT = [
  'ی', // Y
  'د', // D
  'س', // S
  'چ', // Ch
  'پ', // P
  'ج', // J
  'ش', // Sh
]

// ============================================
// Core Date Functions
// ============================================

/**
 * Format a date as YYYY-MM-DD (always Gregorian for backend)
 */
export function formatDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

/**
 * Parse a date string (YYYY-MM-DD) to Date object
 */
export function parseDate(dateStr: string): Date {
  const [year, month, day] = dateStr.split('-').map(Number)
  return new Date(year, month - 1, day)
}

/**
 * Get the start of week (Sunday) for a given date
 */
export function startOfWeek(date: Date): Date {
  const result = new Date(date)
  const day = result.getDay()
  const diff = day // Sunday is 0
  result.setDate(result.getDate() - diff)
  result.setHours(0, 0, 0, 0)
  return result
}

/**
 * Get the end of week (Saturday) for a given date
 */
export function endOfWeek(date: Date): Date {
  const result = startOfWeek(date)
  result.setDate(result.getDate() + 6)
  result.setHours(23, 59, 59, 999)
  return result
}

/**
 * Get the start of month for a given date
 */
export function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1)
}

/**
 * Get the end of month for a given date
 */
export function endOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0, 23, 59, 59, 999)
}

/**
 * Add days to a date
 */
export function addDays(date: Date, days: number): Date {
  const result = new Date(date)
  result.setDate(result.getDate() + days)
  return result
}

/**
 * Add weeks to a date
 */
export function addWeeks(date: Date, weeks: number): Date {
  return addDays(date, weeks * 7)
}

/**
 * Add months to a date
 */
export function addMonths(date: Date, months: number): Date {
  const result = new Date(date)
  result.setMonth(result.getMonth() + months)
  return result
}

/**
 * Check if two dates are the same day
 */
export function isSameDay(date1: Date, date2: Date): boolean {
  return (
    date1.getFullYear() === date2.getFullYear() &&
    date1.getMonth() === date2.getMonth() &&
    date1.getDate() === date2.getDate()
  )
}

/**
 * Check if a date is in the same month as another date
 */
export function isSameMonth(date1: Date, date2: Date): boolean {
  return (
    date1.getFullYear() === date2.getFullYear() &&
    date1.getMonth() === date2.getMonth()
  )
}

/**
 * Get an array of 7 dates for the week starting from the given date
 */
export function getWeekDays(weekStart: Date): Date[] {
  const days: Date[] = []
  for (let i = 0; i < 7; i++) {
    days.push(addDays(weekStart, i))
  }
  return days
}

/**
 * Get an array of dates for the month view (including leading/trailing days)
 */
export function getMonthDays(monthStart: Date): Array<{ date: Date; isCurrentMonth: boolean }> {
  const days: Array<{ date: Date; isCurrentMonth: boolean }> = []

  // Get the first day to display (start of week containing first day of month)
  const firstDay = startOfWeek(monthStart)

  // Get the last day of the month
  const lastDay = endOfMonth(monthStart)

  // Get the last day to display (end of week containing last day of month)
  const lastDisplayDay = endOfWeek(lastDay)

  // Build array of all days to display
  let currentDay = new Date(firstDay)
  while (currentDay <= lastDisplayDay) {
    days.push({
      date: new Date(currentDay),
      isCurrentMonth: isSameMonth(currentDay, monthStart)
    })
    currentDay = addDays(currentDay, 1)
  }

  return days
}

/**
 * Format month and year (e.g., "January 2024" or "فروردین 1403")
 * Locale-aware: uses Jalali calendar for 'fa' locale
 */
export function formatMonthYear(date: Date, locale: string = 'en'): string {
  if (locale === 'fa') {
    const jalali = gregorianToJalali(date.getFullYear(), date.getMonth() + 1, date.getDate())
    return `${PERSIAN_MONTHS[jalali.jm - 1]} ${jalali.jy}`
  }
  return date.toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
}

/**
 * Format week range (e.g., "Jan 1 - Jan 7, 2024" or "فروردین ۱ - ۷, ۱۴۰۳")
 * Locale-aware: uses Jalali calendar for 'fa' locale
 */
export function formatWeekRange(date: Date, locale: string = 'en'): string {
  const weekStart = startOfWeek(date)
  const weekEnd = endOfWeek(date)

  if (locale === 'fa') {
    const jalaliStart = gregorianToJalali(weekStart.getFullYear(), weekStart.getMonth() + 1, weekStart.getDate())
    const jalaliEnd = gregorianToJalali(weekEnd.getFullYear(), weekEnd.getMonth() + 1, weekEnd.getDate())

    // Convert numbers to Persian numerals
    const persianStartDay = toPersianNumber(jalaliStart.jd)
    const persianEndDay = toPersianNumber(jalaliEnd.jd)
    const persianYear = toPersianNumber(jalaliEnd.jy)

    if (jalaliStart.jm === jalaliEnd.jm) {
      return `${PERSIAN_MONTHS[jalaliStart.jm - 1]} ${persianStartDay} - ${persianEndDay}، ${persianYear}`
    } else {
      return `${PERSIAN_MONTHS[jalaliStart.jm - 1]} ${persianStartDay} - ${PERSIAN_MONTHS[jalaliEnd.jm - 1]} ${persianEndDay}، ${persianYear}`
    }
  }

  const startMonth = weekStart.toLocaleDateString('en-US', { month: 'short' })
  const endMonth = weekEnd.toLocaleDateString('en-US', { month: 'short' })
  const startDay = weekStart.getDate()
  const endDay = weekEnd.getDate()
  const year = weekEnd.getFullYear()

  if (startMonth === endMonth) {
    return `${startMonth} ${startDay} - ${endDay}, ${year}`
  } else {
    return `${startMonth} ${startDay} - ${endMonth} ${endDay}, ${year}`
  }
}

/**
 * Get day name (e.g., "Monday" or "دوشنبه")
 * Locale-aware: uses Persian weekday names for 'fa' locale
 */
export function getDayName(date: Date, locale: string = 'en'): string {
  if (locale === 'fa') {
    return PERSIAN_WEEKDAYS[date.getDay()]
  }
  return date.toLocaleDateString('en-US', { weekday: 'long' })
}

/**
 * Get short day name (e.g., "Mon" or "د")
 * Locale-aware: uses Persian short weekday names for 'fa' locale
 */
export function getShortDayName(date: Date, locale: string = 'en'): string {
  if (locale === 'fa') {
    return PERSIAN_WEEKDAYS_SHORT[date.getDay()]
  }
  return date.toLocaleDateString('en-US', { weekday: 'short' })
}

/**
 * Convert English numbers to Persian numerals
 */
export function toPersianNumber(num: number): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return String(num).replace(/\d/g, (digit) => persianDigits[parseInt(digit)])
}

/**
 * Get day number formatted for locale (e.g., "15" or "۱۵")
 */
export function formatDayNumber(date: Date, locale: string = 'en'): string {
  if (locale === 'fa') {
    const jalali = gregorianToJalali(date.getFullYear(), date.getMonth() + 1, date.getDate())
    return toPersianNumber(jalali.jd)
  }
  return String(date.getDate())
}

/**
 * Check if a date is today
 */
export function isToday(date: Date): boolean {
  return isSameDay(date, new Date())
}

/**
 * Check if a date is in the past
 */
export function isPast(date: Date): boolean {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const compareDate = new Date(date)
  compareDate.setHours(0, 0, 0, 0)
  return compareDate < today
}

/**
 * Check if a date is in the future
 */
export function isFuture(date: Date): boolean {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const compareDate = new Date(date)
  compareDate.setHours(0, 0, 0, 0)
  return compareDate > today
}

/**
 * Get the number of days in a month
 */
export function getDaysInMonth(date: Date): number {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate()
}

/**
 * Format time for display
 * English: 12-hour format with AM/PM (e.g., "2:30 PM")
 * Persian: 24-hour format with Persian numerals (e.g., "۱۴:۳۰")
 */
export function formatTimeDisplay(time: string, locale: string = 'en'): string {
  const [hours, minutes] = time.split(':')
  const hour = parseInt(hours, 10)

  if (locale === 'fa') {
    // 24-hour format with Persian numerals
    return `${toPersianNumber(hour)}:${toPersianNumber(parseInt(minutes, 10))}`
  }

  // 12-hour format with AM/PM
  const ampm = hour >= 12 ? 'PM' : 'AM'
  const displayHour = hour % 12 || 12
  return `${displayHour}:${minutes} ${ampm}`
}

/**
 * Convert a string containing numbers to Persian numerals
 */
export function toPersianString(str: string): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return str.replace(/\d/g, (digit) => persianDigits[parseInt(digit)])
}

/**
 * Parse time string to minutes since midnight
 */
export function timeToMinutes(time: string): number {
  const [hours, minutes] = time.split(':').map(Number)
  return hours * 60 + minutes
}

/**
 * Convert minutes since midnight to time string
 */
export function minutesToTime(minutes: number): string {
  const hours = Math.floor(minutes / 60)
  const mins = minutes % 60
  return `${String(hours).padStart(2, '0')}:${String(mins).padStart(2, '0')}`
}

/**
 * Check if a time is within a time range
 */
export function isTimeInRange(time: string, startTime: string, endTime: string): boolean {
  const timeMinutes = timeToMinutes(time)
  const startMinutes = timeToMinutes(startTime)
  const endMinutes = timeToMinutes(endTime)
  return timeMinutes >= startMinutes && timeMinutes <= endMinutes
}
