/**
 * Date and time formatting utilities
 * Centralized date operations for consistent formatting across components
 */

/**
 * Format date string or Date object to display format
 * @param date - Date string (ISO or similar) or Date object
 * @param locale - Locale for formatting (default: 'fa' for Persian)
 * @returns Formatted date string
 */
export function formatDate(date: Date | string | null | undefined, locale: string = 'fa'): string {
  if (!date) return ''

  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date

    if (isNaN(dateObj.getTime())) {
      return ''
    }

    if (locale === 'fa') {
      // Return Gregorian date, user can convert to Jalali using jalali.utils if needed
      const year = dateObj.getFullYear()
      const month = String(dateObj.getMonth() + 1).padStart(2, '0')
      const day = String(dateObj.getDate()).padStart(2, '0')
      return `${year}/${month}/${day}`
    }

    // English format
    return dateObj.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    })
  } catch (error) {
    return ''
  }
}

/**
 * Format date and time to display format
 * @param dateTime - Date string or Date object
 * @param locale - Locale for formatting (default: 'fa' for Persian)
 * @returns Formatted date and time string
 */
export function formatDateTime(dateTime: Date | string | null | undefined, locale: string = 'fa'): string {
  if (!dateTime) return ''

  try {
    const dateObj = typeof dateTime === 'string' ? new Date(dateTime) : dateTime

    if (isNaN(dateObj.getTime())) {
      return ''
    }

    const date = formatDate(dateObj, locale)
    const time = formatTime(dateObj, locale)

    return `${date} ${time}`
  } catch (error) {
    return ''
  }
}

/**
 * Format time to display format
 * @param date - Date object or time string (HH:MM or HH:MM:SS)
 * @param locale - Locale for formatting (default: 'fa' for Persian/24-hour)
 * @returns Formatted time string
 */
export function formatTime(date: Date | string, locale: string = 'fa'): string {
  try {
    let hours: number
    let minutes: number

    if (typeof date === 'string') {
      // Parse time string format (HH:MM or HH:MM:SS)
      const [h, m] = date.split(':').map(Number)
      hours = h
      minutes = m || 0
    } else {
      hours = date.getHours()
      minutes = date.getMinutes()
    }

    if (locale === 'fa') {
      // 24-hour format
      return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`
    }

    // 12-hour format with AM/PM (English)
    const ampm = hours >= 12 ? 'PM' : 'AM'
    const displayHours = hours % 12 || 12
    return `${displayHours}:${String(minutes).padStart(2, '0')} ${ampm}`
  } catch (error) {
    return ''
  }
}

/**
 * Format date with full date and time details
 * @param date - Date string or Date object
 * @returns Full formatted string (e.g., "Friday, January 15, 2024 at 2:30 PM")
 */
export function formatFullDate(date: Date | string | null | undefined): string {
  if (!date) return ''

  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date

    if (isNaN(dateObj.getTime())) {
      return ''
    }

    return dateObj.toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  } catch (error) {
    return ''
  }
}

/**
 * Format date relative to now (e.g., "2 days ago", "in 3 hours")
 * @param date - Date string or Date object
 * @returns Relative time string
 */
export function formatRelativeTime(date: Date | string | null | undefined): string {
  if (!date) return ''

  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date

    if (isNaN(dateObj.getTime())) {
      return ''
    }

    const now = new Date()
    const diffMs = now.getTime() - dateObj.getTime()
    const diffSecs = Math.floor(diffMs / 1000)
    const diffMins = Math.floor(diffSecs / 60)
    const diffHours = Math.floor(diffMins / 60)
    const diffDays = Math.floor(diffHours / 24)
    const diffWeeks = Math.floor(diffDays / 7)
    const diffMonths = Math.floor(diffDays / 30)
    const diffYears = Math.floor(diffDays / 365)

    if (diffSecs < 60) return 'just now'
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`
    if (diffWeeks < 4) return `${diffWeeks} week${diffWeeks > 1 ? 's' : ''} ago`
    if (diffMonths < 12) return `${diffMonths} month${diffMonths > 1 ? 's' : ''} ago`
    return `${diffYears} year${diffYears > 1 ? 's' : ''} ago`
  } catch (error) {
    return ''
  }
}

/**
 * Format day name (e.g., "Monday", "Friday")
 * @param date - Date object
 * @param locale - Locale ('en' or 'fa')
 * @returns Day name
 */
export function formatDayName(date: Date, locale: string = 'en'): string {
  const englishDays = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
  const persianDays = ['یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه', 'شنبه']

  const dayIndex = date.getDay()
  return locale === 'fa' ? persianDays[dayIndex] : englishDays[dayIndex]
}

/**
 * Format month name (e.g., "January", "فروردین")
 * @param monthIndex - Month index (0-11)
 * @param locale - Locale ('en' or 'fa')
 * @returns Month name
 */
export function formatMonthName(monthIndex: number, locale: string = 'en'): string {
  const englishMonths = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ]
  const persianMonths = [
    'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
    'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
  ]

  if (monthIndex < 0 || monthIndex > 11) return ''
  return locale === 'fa' ? persianMonths[monthIndex] : englishMonths[monthIndex]
}

/**
 * Format date as YYYY-MM-DD (ISO format)
 * @param date - Date object or string
 * @returns ISO format date string
 */
export function toISODate(date: Date | string | null | undefined): string {
  if (!date) return ''

  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date

    if (isNaN(dateObj.getTime())) {
      return ''
    }

    const year = dateObj.getFullYear()
    const month = String(dateObj.getMonth() + 1).padStart(2, '0')
    const day = String(dateObj.getDate()).padStart(2, '0')

    return `${year}-${month}-${day}`
  } catch (error) {
    return ''
  }
}

/**
 * Parse date string in various formats
 * @param dateStr - Date string to parse
 * @returns Date object or null if parsing fails
 */
export function parseDate(dateStr: string): Date | null {
  if (!dateStr) return null

  try {
    // Try direct Date constructor first
    let date = new Date(dateStr)
    if (!isNaN(date.getTime())) {
      return date
    }

    // Try YYYY-MM-DD format
    const match = dateStr.match(/^(\d{4})-(\d{2})-(\d{2})$/)
    if (match) {
      return new Date(parseInt(match[1]), parseInt(match[2]) - 1, parseInt(match[3]))
    }

    return null
  } catch (error) {
    return null
  }
}

/**
 * Get number of days between two dates
 * @param date1 - First date
 * @param date2 - Second date
 * @returns Number of days between dates
 */
export function daysBetween(date1: Date, date2: Date): number {
  const diffMs = Math.abs(date2.getTime() - date1.getTime())
  return Math.floor(diffMs / (1000 * 60 * 60 * 24))
}

/**
 * Check if date is today
 * @param date - Date to check
 * @returns True if date is today
 */
export function isToday(date: Date | string): boolean {
  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date
    const today = new Date()

    return (
      dateObj.getFullYear() === today.getFullYear() &&
      dateObj.getMonth() === today.getMonth() &&
      dateObj.getDate() === today.getDate()
    )
  } catch (error) {
    return false
  }
}

/**
 * Check if date is in the past
 * @param date - Date to check
 * @returns True if date is in the past
 */
export function isPast(date: Date | string): boolean {
  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    dateObj.setHours(0, 0, 0, 0)
    return dateObj < today
  } catch (error) {
    return false
  }
}

/**
 * Check if date is in the future
 * @param date - Date to check
 * @returns True if date is in the future
 */
export function isFuture(date: Date | string): boolean {
  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    dateObj.setHours(0, 0, 0, 0)
    return dateObj > today
  } catch (error) {
    return false
  }
}
