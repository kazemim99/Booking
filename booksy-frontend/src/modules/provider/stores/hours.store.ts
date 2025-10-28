// hours.store.ts - Working hours management store
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  BusinessHoursWithBreaks,
  HolidaySchedule,
  ExceptionSchedule,
  DayAvailability,
  HolidaysResponse,
  ExceptionsResponse,
  UpdateBusinessHoursRequest,
  AddHolidayRequest,
  AddExceptionRequest,
} from '../types/hours.types'
import { DayStatus } from '../types/hours.types'
import { hoursService } from '../services/hours.service'

interface HoursState {
  baseHours: BusinessHoursWithBreaks[]
  holidays: HolidaySchedule[]
  exceptions: ExceptionSchedule[]
  isLoading: boolean
  error: string | null
}

export const useHoursStore = defineStore('hours', () => {
  // ============================================
  // State
  // ============================================

  const state = ref<HoursState>({
    baseHours: [],
    holidays: [],
    exceptions: [],
    isLoading: false,
    error: null,
  })

  // ============================================
  // Getters
  // ============================================

  /**
   * Get availability for a specific date considering all schedule layers
   * Priority: Holiday > Exception > Break > Base Hours
   */
  const getAvailabilityForDate = computed(() => {
    return (date: string): DayAvailability => {
      const dayOfWeek = new Date(date).getDay()

      // Check if date is a holiday (highest priority)
      const holiday = state.value.holidays.find((h) => {
        if (h.isRecurring) {
          // Simple recurring check (same month-day)
          const holidayDate = new Date(h.date)
          const checkDate = new Date(date)
          return (
            holidayDate.getMonth() === checkDate.getMonth() &&
            holidayDate.getDate() === checkDate.getDate()
          )
        }
        return h.date === date
      })

      if (holiday) {
        return {
          date,
          isAvailable: false,
          reason: `Holiday: ${holiday.reason}`,
          slots: [],
        }
      }

      // Check for exception schedule
      const exception = state.value.exceptions.find((e) => e.date === date)
      if (exception) {
        if (exception.isClosed) {
          return {
            date,
            isAvailable: false,
            reason: `Closed: ${exception.reason}`,
            slots: [],
          }
        }
        // Use exception hours
        return {
          date,
          isAvailable: true,
          slots: [
            {
              startTime: exception.openTime!,
              endTime: exception.closeTime!,
            },
          ],
        }
      }

      // Use base hours for this day of week
      const baseHour = state.value.baseHours.find((h) => h.dayOfWeek === dayOfWeek)
      if (!baseHour || !baseHour.isOpen) {
        return {
          date,
          isAvailable: false,
          reason: 'Closed',
          slots: [],
        }
      }

      // Calculate slots considering breaks
      const slots = calculateSlotsWithBreaks(
        baseHour.openTime!,
        baseHour.closeTime!,
        baseHour.breaks || []
      )

      return {
        date,
        isAvailable: slots.length > 0,
        slots,
      }
    }
  })

  /**
   * Get upcoming holidays (future only)
   */
  const upcomingHolidays = computed(() => {
    const today = new Date().toISOString().split('T')[0]
    return state.value.holidays
      .filter((h) => h.date >= today)
      .sort((a, b) => a.date.localeCompare(b.date))
  })

  /**
   * Get active exceptions (future and near past)
   */
  const activeExceptions = computed(() => {
    const today = new Date()
    const thirtyDaysAgo = new Date(today)
    thirtyDaysAgo.setDate(today.getDate() - 30)
    const cutoffDate = thirtyDaysAgo.toISOString().split('T')[0]

    return state.value.exceptions
      .filter((e) => e.date >= cutoffDate)
      .sort((a, b) => a.date.localeCompare(b.date))
  })

  /**
   * Check if there are unsaved changes
   */
  const hasUnsavedChanges = computed(() => {
    // This would compare current state with persisted state
    // For now, always return false
    return false
  })

  // ============================================
  // Actions
  // ============================================

  /**
   * Load complete schedule for a provider
   */
  async function loadSchedule(providerId: string): Promise<void> {
    state.value.isLoading = true
    state.value.error = null

    try {
      // Load all schedule data in parallel
      const [hoursResponse, holidaysResponse, exceptionsResponse] = await Promise.all([
        hoursService.getBusinessHours(providerId),
        hoursService.getHolidays(providerId),
        hoursService.getExceptions(providerId),
      ])

      state.value.baseHours = hoursResponse.businessHours
      state.value.holidays = holidaysResponse.holidays
      state.value.exceptions = exceptionsResponse.exceptions
    } catch (error) {
      state.value.error = error instanceof Error ? error.message : 'Failed to load schedule'
      throw error
    } finally {
      state.value.isLoading = false
    }
  }

  /**
   * Update base business hours
   */
  async function updateHours(request: UpdateBusinessHoursRequest): Promise<void> {
    // Optimistic update
    const previousHours = [...state.value.baseHours]
    state.value.baseHours = request.businessHours
    state.value.error = null

    try {
      await hoursService.updateBusinessHours(request)
    } catch (error) {
      // Rollback on error
      state.value.baseHours = previousHours
      state.value.error = error instanceof Error ? error.message : 'Failed to update hours'
      throw error
    }
  }

  /**
   * Add a holiday
   */
  async function addHoliday(request: AddHolidayRequest): Promise<HolidaySchedule> {
    state.value.error = null

    try {
      const holiday = await hoursService.addHoliday(request)

      // Optimistically add to state
      state.value.holidays.push(holiday)

      return holiday
    } catch (error) {
      state.value.error = error instanceof Error ? error.message : 'Failed to add holiday'
      throw error
    }
  }

  /**
   * Remove a holiday
   */
  async function removeHoliday(providerId: string, holidayId: string): Promise<void> {
    // Optimistic update
    const previousHolidays = [...state.value.holidays]
    state.value.holidays = state.value.holidays.filter((h) => h.id !== holidayId)
    state.value.error = null

    try {
      await hoursService.deleteHoliday(providerId, holidayId)
    } catch (error) {
      // Rollback on error
      state.value.holidays = previousHolidays
      state.value.error = error instanceof Error ? error.message : 'Failed to remove holiday'
      throw error
    }
  }

  /**
   * Add an exception schedule
   */
  async function addException(request: AddExceptionRequest): Promise<ExceptionSchedule> {
    state.value.error = null

    try {
      const exception = await hoursService.addException(request)

      // Optimistically add to state
      state.value.exceptions.push(exception)

      return exception
    } catch (error) {
      state.value.error = error instanceof Error ? error.message : 'Failed to add exception'
      throw error
    }
  }

  /**
   * Remove an exception schedule
   */
  async function removeException(providerId: string, exceptionId: string): Promise<void> {
    // Optimistic update
    const previousExceptions = [...state.value.exceptions]
    state.value.exceptions = state.value.exceptions.filter((e) => e.id !== exceptionId)
    state.value.error = null

    try {
      await hoursService.deleteException(providerId, exceptionId)
    } catch (error) {
      // Rollback on error
      state.value.exceptions = previousExceptions
      state.value.error = error instanceof Error ? error.message : 'Failed to remove exception'
      throw error
    }
  }

  /**
   * Clear error state
   */
  function clearError(): void {
    state.value.error = null
  }

  /**
   * Reset store to initial state
   */
  function $reset(): void {
    state.value = {
      baseHours: [],
      holidays: [],
      exceptions: [],
      isLoading: false,
      error: null,
    }
  }

  // ============================================
  // Helper Functions
  // ============================================

  /**
   * Calculate available slots by subtracting breaks from base hours
   */
  function calculateSlotsWithBreaks(
    openTime: string,
    closeTime: string,
    breaks: Array<{ startTime: string; endTime: string }>
  ) {
    if (breaks.length === 0) {
      return [{ startTime: openTime, endTime: closeTime }]
    }

    const slots = []
    let currentStart = openTime

    // Sort breaks by start time
    const sortedBreaks = [...breaks].sort((a, b) => a.startTime.localeCompare(b.startTime))

    for (const breakPeriod of sortedBreaks) {
      // Add slot before break if there's time
      if (currentStart < breakPeriod.startTime) {
        slots.push({
          startTime: currentStart,
          endTime: breakPeriod.startTime,
        })
      }
      currentStart = breakPeriod.endTime
    }

    // Add final slot after last break
    if (currentStart < closeTime) {
      slots.push({
        startTime: currentStart,
        endTime: closeTime,
      })
    }

    return slots
  }

  return {
    // State
    state,

    // Getters
    getAvailabilityForDate,
    upcomingHolidays,
    activeExceptions,
    hasUnsavedChanges,

    // Actions
    loadSchedule,
    updateHours,
    addHoliday,
    removeHoliday,
    addException,
    removeException,
    clearError,
    $reset,
  }
})
