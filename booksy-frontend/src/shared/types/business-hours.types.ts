/**
 * Shared Business Hours Types
 *
 * Centralized type definitions for business hours, breaks, and time slots
 * to avoid duplication across the codebase.
 */

// ==================== Time Representations ====================

/**
 * Time represented as hours and minutes (used in registration flow)
 * Example: { hours: 10, minutes: 30 } = 10:30 AM
 */
export interface TimeComponents {
  hours: number
  minutes: number
}

/**
 * Time represented as string in HH:mm format (used for display/API)
 * Example: "10:30"
 */
export type TimeString = string // HH:mm format

// ==================== Break Period Types ====================

/**
 * Break period with time components (registration flow)
 */
export interface BreakPeriodComponents {
  id: string
  start: TimeComponents
  end: TimeComponents
}

/**
 * Break period with time strings (API/display)
 */
export interface BreakPeriodString {
  id?: string
  start: TimeString
  end: TimeString
  label?: string
}

/**
 * Break period from backend (hours/minutes fields)
 */
export interface BreakPeriodBackend {
  startTimeHours: number
  startTimeMinutes: number
  endTimeHours: number
  endTimeMinutes: number
}

// ==================== Business Hours Types ====================

/**
 * Day hours with time components (registration flow)
 */
export interface DayHoursComponents {
  dayOfWeek: number // 0-6
  isOpen: boolean
  openTime: TimeComponents | null
  closeTime: TimeComponents | null
  breaks: BreakPeriodComponents[]
}

/**
 * Day hours with time strings (UI display)
 */
export interface DayHoursString {
  dayOfWeek?: number // Optional for array-indexed usage
  isOpen: boolean
  startTime: TimeString
  endTime: TimeString
  breaks: BreakPeriodString[]
}

/**
 * Business hours from backend API
 */
export interface BusinessHoursBackend {
  id?: string
  dayOfWeek: number
  isOpen: boolean
  openTimeHours?: number
  openTimeMinutes?: number
  closeTimeHours?: number
  closeTimeMinutes?: number
  breaks?: BreakPeriodBackend[]
}

/**
 * Business hours for provider entity (string format)
 */
export interface BusinessHoursEntity {
  id: string
  dayOfWeek: number
  openTime: TimeString
  closeTime: TimeString
  isOpen: boolean
  breaks?: BreakPeriodString[]
}

// ==================== Helper Types ====================

/**
 * Day of week enum (backend convention)
 * Monday = 0, Tuesday = 1, ..., Sunday = 6
 */
export enum DayOfWeek {
  Monday = 0,
  Tuesday = 1,
  Wednesday = 2,
  Thursday = 3,
  Friday = 4,
  Saturday = 5,
  Sunday = 6,
}

/**
 * Persian week day names (UI display)
 */
export const PERSIAN_WEEKDAYS = [
  'شنبه',    // Saturday
  'یکشنبه',  // Sunday
  'دوشنبه',  // Monday
  'سه‌شنبه', // Tuesday
  'چهارشنبه', // Wednesday
  'پنجشنبه', // Thursday
  'جمعه',    // Friday
] as const

/**
 * English week day names
 */
export const ENGLISH_WEEKDAYS = [
  'Saturday',
  'Sunday',
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
] as const

/**
 * Mapping between Persian week order and backend day enum
 */
export const PERSIAN_TO_BACKEND_DAY_MAP = {
  0: DayOfWeek.Saturday,  // شنبه
  1: DayOfWeek.Sunday,    // یکشنبه
  2: DayOfWeek.Monday,    // دوشنبه
  3: DayOfWeek.Tuesday,   // سه‌شنبه
  4: DayOfWeek.Wednesday, // چهارشنبه
  5: DayOfWeek.Thursday,  // پنجشنبه
  6: DayOfWeek.Friday,    // جمعه
} as const

// ==================== Utility Functions ====================

/**
 * Convert TimeComponents to TimeString
 */
export function timeComponentsToString(time: TimeComponents): TimeString {
  const h = time.hours.toString().padStart(2, '0')
  const m = time.minutes.toString().padStart(2, '0')
  return `${h}:${m}`
}

/**
 * Convert TimeString to TimeComponents
 */
export function timeStringToComponents(time: TimeString): TimeComponents {
  const [hours, minutes] = time.split(':').map(Number)
  return { hours: hours || 0, minutes: minutes || 0 }
}

/**
 * Convert backend format to TimeString
 */
export function backendTimeToString(hours?: number, minutes?: number): TimeString {
  if (hours === undefined || hours === null) return '10:00'
  const h = hours.toString().padStart(2, '0')
  const m = (minutes || 0).toString().padStart(2, '0')
  return `${h}:${m}`
}
