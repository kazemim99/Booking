/**
 * useAvailability Composable
 * Provides reactive availability checking functionality
 */

import { ref, computed } from 'vue'
import { availabilityService, type TimeSlot, type AvailableDate } from '../api/availability.service'

export interface UseAvailabilityOptions {
  providerId: string
  serviceId: string
  staffMemberId?: string
  autoLoad?: boolean
}

export function useAvailability(options: UseAvailabilityOptions) {
  // State
  const loading = ref(false)
  const error = ref<string | null>(null)
  const selectedDate = ref<string>('')
  const selectedSlot = ref<TimeSlot | null>(null)
  const availableSlots = ref<TimeSlot[]>([])
  const availableDates = ref<AvailableDate[]>([])

  // Computed
  const hasSlots = computed(() => availableSlots.value.length > 0)

  const availableSlotsOnly = computed(() =>
    availableSlots.value.filter(slot => slot.available)
  )

  const groupedSlots = computed(() =>
    availabilityService.groupSlotsByTimeOfDay(availableSlots.value)
  )

  const hasAvailability = computed(() => availableSlotsOnly.value.length > 0)

  const nextAvailableSlot = computed(() =>
    availableSlotsOnly.value[0] || null
  )

  // Methods
  async function loadSlots(date: string) {
    loading.value = true
    error.value = null

    try {
      const response = await availabilityService.getAvailableSlots({
        providerId: options.providerId,
        serviceId: options.serviceId,
        date,
        staffMemberId: options.staffMemberId,
      })

      availableSlots.value = response.slots
      selectedDate.value = date
      return response.slots
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'خطا در بارگذاری زمان‌های خالی'
      availableSlots.value = []
      throw err
    } finally {
      loading.value = false
    }
  }

  async function loadAvailableDates(fromDate: string, toDate: string) {
    loading.value = true
    error.value = null

    try {
      const response = await availabilityService.getAvailableDates({
        providerId: options.providerId,
        serviceId: options.serviceId,
        fromDate,
        toDate,
        staffMemberId: options.staffMemberId,
      })

      availableDates.value = response.dates
      return response.dates
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'خطا در بارگذاری تاریخ‌های خالی'
      availableDates.value = []
      throw err
    } finally {
      loading.value = false
    }
  }

  async function checkSlotAvailability(startTime: string) {
    loading.value = true
    error.value = null

    try {
      const result = await availabilityService.checkAvailability({
        providerId: options.providerId,
        serviceId: options.serviceId,
        startTime,
        staffMemberId: options.staffMemberId,
      })

      return result
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'خطا در بررسی زمان'
      throw err
    } finally {
      loading.value = false
    }
  }

  function selectSlot(slot: TimeSlot) {
    if (slot.available) {
      selectedSlot.value = slot
    }
  }

  function clearSelection() {
    selectedSlot.value = null
  }

  function formatSlotTime(slot: TimeSlot): string {
    return availabilityService.formatTimeSlot(slot)
  }

  function formatSlotTimePersian(slot: TimeSlot): string {
    const formatted = availabilityService.formatTimeSlot(slot)
    return availabilityService.toPersianTime(formatted)
  }

  // Auto-load today's slots if enabled
  if (options.autoLoad) {
    const today = new Date().toISOString().split('T')[0]
    loadSlots(today)
  }

  return {
    // State
    loading,
    error,
    selectedDate,
    selectedSlot,
    availableSlots,
    availableDates,

    // Computed
    hasSlots,
    availableSlotsOnly,
    groupedSlots,
    hasAvailability,
    nextAvailableSlot,

    // Methods
    loadSlots,
    loadAvailableDates,
    checkSlotAvailability,
    selectSlot,
    clearSelection,
    formatSlotTime,
    formatSlotTimePersian,
  }
}
