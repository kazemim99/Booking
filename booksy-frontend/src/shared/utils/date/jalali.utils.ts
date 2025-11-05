/**
 * Jalali (Persian) Calendar Utilities
 *
 * Provides conversion and formatting functions for Jalali/Gregorian dates
 * using @persian-tools/persian-tools library
 */

import { digitsEnToFa, digitsFaToEn } from '@persian-tools/persian-tools'
import jalaali from 'jalaali-js'

/**
 * Converts Gregorian date to Jalali using jalaali-js library
 */
export function gregorianToJalali(date: Date): { year: number; month: number; day: number } {
  const gy = date.getFullYear()
  const gm = date.getMonth() + 1
  const gd = date.getDate()

  const jalali = jalaali.toJalaali(gy, gm, gd)

  return { year: jalali.jy, month: jalali.jm, day: jalali.jd }
}

/**
 * Converts Jalali date to Gregorian
 */
export function jalaliToGregorian(jy: number, jm: number, jd: number): Date {
  jy += jm > 10 || (jm === 10 && jd > 10) ? 1595 : 1596
  const days = 365 * jy + Math.floor(jy / 33) * 8 + Math.floor(((jy % 33) + 3) / 4) + 78 + jd + (jm < 7 ? (jm - 1) * 31 : (jm - 7) * 30 + 186)
  let gy = 400 * Math.floor(days / 146097)
  let d = days % 146097
  if (d >= 36525) {
    gy += 100 * Math.floor(--d / 36524)
    d %= 36524
    if (d >= 365) d++
  }
  gy += 4 * Math.floor(d / 1461)
  d %= 1461
  gy += Math.floor((d - 1) / 365)
  if (d > 365) d = (d - 1) % 365
  const gm = [0, 31, gy % 4 === 0 && (gy % 100 !== 0 || gy % 400 === 0) ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
  let i = 0
  for (; i < 13 && d > gm[i]; i++) d -= gm[i]

  return new Date(gy, i - 1, d)
}

/**
 * Formats Jalali date to Persian string
 * @example formatJalaliDate({ year: 1403, month: 8, day: 11 }) => "۱۱ آبان ۱۴۰۳"
 */
export function formatJalaliDate(jalaliDate: { year: number; month: number; day: number }, format: 'short' | 'long' = 'long'): string {
  const monthNames = [
    'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
    'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
  ]

  const day = digitsEnToFa(jalaliDate.day.toString())
  const month = format === 'long' ? monthNames[jalaliDate.month - 1] : digitsEnToFa(jalaliDate.month.toString().padStart(2, '0'))
  const year = digitsEnToFa(jalaliDate.year.toString())

  return format === 'long' ? `${day} ${month} ${year}` : `${year}/${month}/${day}`
}

/**
 * Formats Date object to Jalali Persian string
 */
export function formatDateToJalali(date: Date, format: 'short' | 'long' = 'long'): string {
  const jalali = gregorianToJalali(date)
  return formatJalaliDate(jalali, format)
}

/**
 * Gets current Jalali date
 */
export function getCurrentJalaliDate(): { year: number; month: number; day: number } {
  return gregorianToJalali(new Date())
}

/**
 * Parses Jalali date string (YYYY/MM/DD) to Jalali object
 */
export function parseJalaliDate(dateString: string): { year: number; month: number; day: number } | null {
  // Convert Persian digits to English first
  const englishDate = digitsFaToEn(dateString)
  const parts = englishDate.split('/')

  if (parts.length !== 3) return null

  const year = parseInt(parts[0], 10)
  const month = parseInt(parts[1], 10)
  const day = parseInt(parts[2], 10)

  if (isNaN(year) || isNaN(month) || isNaN(day)) return null
  if (month < 1 || month > 12) return null
  if (day < 1 || day > 31) return null

  return { year, month, day }
}

/**
 * Converts Persian numbers to English numbers
 */
export function convertPersianToEnglishNumbers(str: string): string {
  return digitsFaToEn(str)
}

/**
 * Converts English numbers to Persian numbers
 */
export function convertEnglishToPersianNumbers(str: string): string {
  return digitsEnToFa(str)
}

/**
 * Validates if a Jalali date is valid
 */
export function isValidJalaliDate(year: number, month: number, day: number): boolean {
  if (month < 1 || month > 12) return false
  if (day < 1) return false

  // Months 1-6 have 31 days
  if (month <= 6 && day > 31) return false

  // Months 7-11 have 30 days
  if (month >= 7 && month <= 11 && day > 30) return false

  // Month 12 has 29 or 30 days (leap year check)
  if (month === 12) {
    const isLeap = ((year - 474) % 128) % 33 < 29
    return day <= (isLeap ? 30 : 29)
  }

  return true
}

/**
 * Gets Jalali month name
 */
export function getJalaliMonthName(month: number): string {
  const monthNames = [
    'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
    'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
  ]
  return monthNames[month - 1] || ''
}

/**
 * Gets Jalali weekday name
 */
export function getJalaliWeekdayName(date: Date): string {
  const weekdays = ['یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه', 'شنبه']
  return weekdays[date.getDay()]
}
