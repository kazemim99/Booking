/**
 * Date Picker Composable
 * Persian (Jalaali) and Gregorian date picker support
 */

import { ref, computed, watch } from 'vue'
import type { Ref } from 'vue'
import type { DateFormat } from '@/core/types/enums.types'

// ==================== Types ====================

export interface UseDatePickerOptions {
  format?: DateFormat
  minDate?: Date | string
  maxDate?: Date | string
  disabledDates?: Array<Date | string>
  locale?: 'fa' | 'en'
}

export interface DateRange {
  start: Date | null
  end: Date | null
}

// ==================== Composable ====================

export function useDatePicker(
  initialDate?: Date | string | null,
  options: UseDatePickerOptions = {}
) {
  const {
    format = 'jalaali',
    minDate,
    maxDate,
    disabledDates = [],
    locale = 'fa',
  } = options

  // ==================== State ====================

  const selectedDate = ref<Date | null>(
    initialDate ? new Date(initialDate) : null
  )
  const displayFormat = ref<DateFormat>(format)
  const currentMonth = ref(new Date())
  const isOpen = ref(false)

  // ==================== Computed ====================

  const formattedDate = computed(() => {
    if (!selectedDate.value) return ''

    if (displayFormat.value === 'jalaali') {
      return formatJalaali(selectedDate.value)
    }

    return formatGregorian(selectedDate.value)
  })

  const isDateInRange = computed(() => {
    if (!selectedDate.value) return true

    const date = selectedDate.value.getTime()

    if (minDate) {
      const min = new Date(minDate).getTime()
      if (date < min) return false
    }

    if (maxDate) {
      const max = new Date(maxDate).getTime()
      if (date > max) return false
    }

    return true
  })

  // ==================== Actions ====================

  /**
   * Set selected date
   */
  function setDate(date: Date | string | null): void {
    selectedDate.value = date ? new Date(date) : null
  }

  /**
   * Clear selected date
   */
  function clearDate(): void {
    selectedDate.value = null
  }

  /**
   * Set display format
   */
  function setFormat(newFormat: DateFormat): void {
    displayFormat.value = newFormat
  }

  /**
   * Toggle date picker
   */
  function toggle(): void {
    isOpen.value = !isOpen.value
  }

  /**
   * Open date picker
   */
  function open(): void {
    isOpen.value = true
  }

  /**
   * Close date picker
   */
  function close(): void {
    isOpen.value = false
  }

  /**
   * Check if date is disabled
   */
  function isDateDisabled(date: Date): boolean {
    const dateTime = date.getTime()

    // Check min/max
    if (minDate && dateTime < new Date(minDate).getTime()) {
      return true
    }

    if (maxDate && dateTime > new Date(maxDate).getTime()) {
      return true
    }

    // Check disabled dates
    return disabledDates.some((disabledDate) => {
      const disabledTime = new Date(disabledDate).getTime()
      return isSameDay(new Date(disabledTime), date)
    })
  }

  /**
   * Navigate to previous month
   */
  function previousMonth(): void {
    currentMonth.value = new Date(
      currentMonth.value.getFullYear(),
      currentMonth.value.getMonth() - 1,
      1
    )
  }

  /**
   * Navigate to next month
   */
  function nextMonth(): void {
    currentMonth.value = new Date(
      currentMonth.value.getFullYear(),
      currentMonth.value.getMonth() + 1,
      1
    )
  }

  /**
   * Go to today
   */
  function goToToday(): void {
    currentMonth.value = new Date()
    selectedDate.value = new Date()
  }

  /**
   * Get formatted date string
   */
  function format(date: Date | null = selectedDate.value): string {
    if (!date) return ''

    if (displayFormat.value === 'jalaali') {
      return formatJalaali(date)
    }

    return formatGregorian(date)
  }

  return {
    // State
    selectedDate,
    displayFormat,
    currentMonth,
    isOpen,

    // Computed
    formattedDate,
    isDateInRange,

    // Actions
    setDate,
    clearDate,
    setFormat,
    toggle,
    open,
    close,
    isDateDisabled,
    previousMonth,
    nextMonth,
    goToToday,
    format,
  }
}

// ==================== Date Range Composable ====================

export function useDateRange(
  initialRange?: DateRange,
  options: UseDatePickerOptions = {}
) {
  const startDate = ref<Date | null>(initialRange?.start || null)
  const endDate = ref<Date | null>(initialRange?.end || null)

  const range = computed<DateRange>(() => ({
    start: startDate.value,
    end: endDate.value,
  }))

  const isRangeValid = computed(() => {
    if (!startDate.value || !endDate.value) return true
    return startDate.value <= endDate.value
  })

  const rangeDuration = computed(() => {
    if (!startDate.value || !endDate.value) return 0

    const diff = endDate.value.getTime() - startDate.value.getTime()
    return Math.ceil(diff / (1000 * 60 * 60 * 24))
  })

  function setRange(start: Date | null, end: Date | null): void {
    startDate.value = start
    endDate.value = end
  }

  function clearRange(): void {
    startDate.value = null
    endDate.value = null
  }

  return {
    startDate,
    endDate,
    range,
    isRangeValid,
    rangeDuration,
    setRange,
    clearRange,
  }
}

// ==================== Helper Functions ====================

/**
 * Format date as Jalaali (Persian calendar)
 * Note: This is a placeholder - you should use a proper Jalaali library like moment-jalaali
 */
function formatJalaali(date: Date): string {
  // Placeholder implementation
  // In production, use: moment(date).format('jYYYY/jMM/jDD')
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  // This is a simplified placeholder - actual conversion requires jalaali-js
  return `${year}/${month}/${day}`
}

/**
 * Format date as Gregorian
 */
function formatGregorian(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  return `${year}/${month}/${day}`
}

/**
 * Check if two dates are the same day
 */
function isSameDay(date1: Date, date2: Date): boolean {
  return (
    date1.getFullYear() === date2.getFullYear() &&
    date1.getMonth() === date2.getMonth() &&
    date1.getDate() === date2.getDate()
  )
}

/**
 * Check if date is today
 */
export function isToday(date: Date): boolean {
  return isSameDay(date, new Date())
}

/**
 * Check if date is in the past
 */
export function isPast(date: Date): boolean {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  return date < today
}

/**
 * Check if date is in the future
 */
export function isFuture(date: Date): boolean {
  const today = new Date()
  today.setHours(23, 59, 59, 999)
  return date > today
}
