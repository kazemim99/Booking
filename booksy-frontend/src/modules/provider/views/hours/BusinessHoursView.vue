<template>
  <div class="business-hours-view">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Business Hours</h1>
        <p class="page-subtitle">
          Set your regular business hours and availability for bookings
        </p>
      </div>
      <div class="header-actions">
        <div class="view-mode-toggle">
          <button
            type="button"
            :class="['view-mode-btn', { active: viewMode === 'calendar' }]"
            @click="viewMode = 'calendar'"
          >
            📅 Calendar
          </button>
          <button
            type="button"
            :class="['view-mode-btn', { active: viewMode === 'list' }]"
            @click="viewMode = 'list'"
          >
            📋 List
          </button>
        </div>
        <Button variant="secondary" @click="goBack">{{ backButtonText }}</Button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="providerStore.isLoading || isLoadingHours" class="loading-state">
      <Spinner />
      <p>Loading business hours...</p>
    </div>

    <!-- Error State -->
    <Alert
      v-if="errorMessage"
      type="error"
      :message="errorMessage"
      @dismiss="errorMessage = null"
    />

    <!-- Success Message -->
    <Alert
      v-if="successMessage"
      type="success"
      :message="successMessage"
      @dismiss="successMessage = null"
    />

    <!-- Form -->
    <form v-if="!providerStore.isLoading && !isLoadingHours" class="hours-form" @submit.prevent="handleSubmit">
      <Card class="form-section">
        <!-- Calendar View -->
        <div v-if="viewMode === 'calendar'" class="calendar-container">
          <HoursCalendarView
            :business-hours="scheduleData"
            :holidays="hoursStore.state.holidays"
            :exceptions="hoursStore.state.exceptions"
            @day-click="handleDayClick"
          />
        </div>

        <!-- List View -->
        <div v-else class="list-container">
          <HoursListView
            :schedule-data="scheduleData"
            @toggle-day="onToggleDay"
            @time-change="handleTimeChange"
            @add-break="addBreak"
            @remove-break="removeBreak"
            @break-change="handleBreakChange"
            @copy-monday="copyMondayScheduleToAllDays"
            @set-standard="setStandardBusinessHours"
            @clear-all="clearAllHours"
          />
        </div>

        <!-- Legacy Days List (hidden, keeping for compatibility) -->
        <div v-show="false" class="days-container">
          <div
            v-for="(day, index) in weekDays"
            :key="day.value"
            class="day-schedule"
            :class="{ 'day-closed': !scheduleData[index].isOpen }"
          >
            <div class="day-header">
              <h3 class="day-name">{{ day.label }}</h3>
              <div class="day-toggle">
                <label class="toggle-label">
                  <span>{{ scheduleData[index].isOpen ? 'Open' : 'Closed' }}</span>
                  <input
                    type="checkbox"
                    v-model="scheduleData[index].isOpen"
                    @change="onToggleDay(index)"
                  />
                  <span class="toggle-switch"></span>
                </label>
              </div>
            </div>

            <div v-if="scheduleData[index].isOpen" class="day-hours">
              <div class="time-range">
                <div class="time-input">
                  <label>Opening Time</label>
                  <select v-model="scheduleData[index].openTime" @change="validateTimeRange(index)">
                    <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                      {{ time.label }}
                    </option>
                  </select>
                </div>

                <div class="time-separator">to</div>

                <div class="time-input">
                  <label>Closing Time</label>
                  <select v-model="scheduleData[index].closeTime" @change="validateTimeRange(index)">
                    <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                      {{ time.label }}
                    </option>
                  </select>
                </div>
              </div>

              <!-- Break Time -->
              <div class="break-section">
                <div class="break-header">
                  <h4 class="break-title">Breaks</h4>
                  <Button
                    nativeType="button"
                    variant="secondary"
                    size="small"
                    @click="addBreak(index)"
                    :disabled="scheduleData[index].breaks.length >= 3"
                  >
                    + Add Break
                  </Button>
                </div>

                <div v-if="scheduleData[index].breaks.length === 0" class="no-breaks">
                  No breaks scheduled
                </div>

                <div
                  v-for="(breakTime, breakIndex) in scheduleData[index].breaks"
                  :key="`${index}-${breakIndex}`"
                  class="break-time-container"
                >
                  <div class="time-range">
                    <div class="time-input">
                      <label>From</label>
                      <select
                        v-model="breakTime.startTime"
                        @change="validateBreakTime(index, breakIndex)"
                      >
                        <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                          {{ time.label }}
                        </option>
                      </select>
                    </div>

                    <div class="time-separator">to</div>

                    <div class="time-input">
                      <label>To</label>
                      <select
                        v-model="breakTime.endTime"
                        @change="validateBreakTime(index, breakIndex)"
                      >
                        <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                          {{ time.label }}
                        </option>
                      </select>
                    </div>

                    <Button
                      nativeType="button"
                      variant="danger"
                      size="small"
                      class="remove-break"
                      @click="removeBreak(index, breakIndex)"
                    >
                      Remove
                    </Button>
                  </div>
                </div>
              </div>
            </div>

            <div v-else class="day-closed-message">
              <p>Closed on this day</p>
            </div>
          </div>
        </div>
      </Card>

      <Card class="form-section">
        <h2 class="section-title">Special Schedule Settings</h2>

        <div class="special-settings">
          <div class="checkbox-group">
            <label class="checkbox-label">
              <input type="checkbox" v-model="allowBookingsOutsideBusinessHours" class="checkbox" />
              <span class="checkbox-text">
                <strong>Allow bookings outside business hours</strong>
                <small>Customers can book appointments outside your regular business hours</small>
              </span>
            </label>

            <label class="checkbox-label">
              <input type="checkbox" v-model="allowSameDayBookings" class="checkbox" />
              <span class="checkbox-text">
                <strong>Accept same-day bookings</strong>
                <small>Allow customers to book appointments for the current day</small>
              </span>
            </label>
          </div>

          <div class="form-group">
            <label>Minimum notice for bookings</label>
            <div class="input-with-suffix">
              <input
                type="number"
                v-model="advanceBookingHours"
                min="0"
                max="72"
                class="number-input"
              />
              <span class="input-suffix">hours</span>
            </div>
            <small>Minimum time required before a booking (0 = no advance notice required)</small>
          </div>

          <div class="form-group">
            <label>Maximum booking window</label>
            <div class="input-with-suffix">
              <input
                type="number"
                v-model="maxBookingDays"
                min="1"
                max="365"
                class="number-input"
              />
              <span class="input-suffix">days</span>
            </div>
            <small>How far in advance customers can book (e.g., 30 days, 60 days, etc.)</small>
          </div>
        </div>
      </Card>

      <!-- Form Actions -->
      <div class="form-actions">
        <Button type="button" variant="secondary" @click="goBack">Cancel</Button>
        <Button type="submit" variant="primary" :disabled="isSaving">
          {{ isSaving ? 'Saving...' : 'Save Business Hours' }}
        </Button>
      </div>
    </form>

    <!-- Day Schedule Modal -->
    <DayScheduleModal
      :is-open="isModalOpen"
      :day-schedule="selectedDaySchedule"
      :day-label="selectedDayLabel"
      :selected-date="selectedDate"
      :provider-id="providerId"
      :is-saving="isSaving"
      @close="closeModal"
      @save="saveDaySchedule"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { useHoursStore } from '@/modules/provider/stores/hours.store'
import { DayOfWeek, BusinessHours, TimeBreak } from '@/modules/provider/types/provider.types'
import { CalendarDay, BusinessHoursWithBreaks } from '@/modules/provider/types/hours.types'
import { Button, Card, Alert, Spinner } from '@/shared/components'
import HoursCalendarView from '@/modules/provider/components/hours/HoursCalendarView.vue'
import HoursListView from '@/modules/provider/components/hours/HoursListView.vue'
import DayScheduleModal from '@/modules/provider/components/hours/DayScheduleModal.vue'

const { locale, t } = useI18n()

const router = useRouter()
const providerStore = useProviderStore()
const hoursStore = useHoursStore()

const isSaving = ref(false)
const errorMessage = ref<string | null>(null)
const successMessage = ref<string | null>(null)
const viewMode = ref<'calendar' | 'list'>('list')
const isLoadingHours = ref(false)

// Modal state
const isModalOpen = ref(false)
const selectedDaySchedule = ref<BusinessHoursWithBreaks | null>(null)
const selectedDayLabel = ref('')
const selectedDate = ref<string | undefined>(undefined)
const providerId = computed(() => providerStore.currentProvider?.id)

// Check if we're in onboarding context
const fromOnboarding = computed(() => router.currentRoute.value.query.from === 'onboarding')
const backButtonText = computed(() => fromOnboarding.value ? '← Back to Onboarding' : '← Back')

// Special settings
const allowBookingsOutsideBusinessHours = ref(false)
const allowSameDayBookings = ref(true)
const advanceBookingHours = ref(2)
const maxBookingDays = ref(60)

// Days of the week - ordered based on locale (Persian week starts Saturday, Western starts Monday)
const weekDays = computed(() => {
  const isPersian = locale.value === 'fa'

  if (isPersian) {
    // Persian week: Saturday to Friday
    return [
      { value: DayOfWeek.Saturday, label: 'Saturday' },
      { value: DayOfWeek.Sunday, label: 'Sunday' },
      { value: DayOfWeek.Monday, label: 'Monday' },
      { value: DayOfWeek.Tuesday, label: 'Tuesday' },
      { value: DayOfWeek.Wednesday, label: 'Wednesday' },
      { value: DayOfWeek.Thursday, label: 'Thursday' },
      { value: DayOfWeek.Friday, label: 'Friday' },
    ]
  } else {
    // Western week: Monday to Sunday
    return [
      { value: DayOfWeek.Monday, label: 'Monday' },
      { value: DayOfWeek.Tuesday, label: 'Tuesday' },
      { value: DayOfWeek.Wednesday, label: 'Wednesday' },
      { value: DayOfWeek.Thursday, label: 'Thursday' },
      { value: DayOfWeek.Friday, label: 'Friday' },
      { value: DayOfWeek.Saturday, label: 'Saturday' },
      { value: DayOfWeek.Sunday, label: 'Sunday' },
    ]
  }
})

// Time options (30-minute intervals)
const timeOptions = computed(() => {
  const options = []
  for (let i = 0; i < 24; i++) {
    const hour = i.toString().padStart(2, '0')
    options.push({ value: `${hour}:00`, label: formatTimeForDisplay(`${hour}:00`) })
    options.push({ value: `${hour}:30`, label: formatTimeForDisplay(`${hour}:30`) })
  }
  return options
})

// Format time for display (e.g., "09:00" -> "9:00 AM")
function formatTimeForDisplay(time: string): string {
  const [hours, minutes] = time.split(':')
  const hour = parseInt(hours, 10)
  const ampm = hour >= 12 ? 'PM' : 'AM'
  const displayHour = hour % 12 || 12
  return `${displayHour}:${minutes} ${ampm}`
}

// Create empty schedule data structure
const createEmptyScheduleData = () => {
  const isPersian = locale.value === 'fa'

  return weekDays.value.map((day) => ({
    dayOfWeek: day.value,
    isOpen: isPersian, // Persian: all days open by default, Western: closed by default
    openTime: '09:00',
    closeTime: '17:00',
    breaks: [] as { startTime: string; endTime: string }[],
  }))
}

// Schedule data
const scheduleData = reactive(createEmptyScheduleData())

// Set standard business hours
function setStandardBusinessHours() {
  const isPersian = locale.value === 'fa'

  weekDays.value.forEach((_, index) => {
    if (isPersian) {
      // Persian: All days open (Saturday to Friday), Friday is half-day
      const isFriday = scheduleData[index].dayOfWeek === DayOfWeek.Friday
      scheduleData[index].isOpen = true
      scheduleData[index].openTime = '09:00'
      scheduleData[index].closeTime = isFriday ? '13:00' : '17:00' // Friday half-day
      scheduleData[index].breaks = []
    } else {
      // Western: Monday to Friday
      const isWeekday = index < 5
      scheduleData[index].isOpen = isWeekday
      scheduleData[index].openTime = '09:00'
      scheduleData[index].closeTime = '17:00'
      scheduleData[index].breaks = []

      // Add a lunch break for weekdays
      if (isWeekday) {
        scheduleData[index].breaks.push({
          startTime: '12:00',
          endTime: '13:00',
        })
      }
    }
  })
}

// Copy first day's schedule to all days (Monday for Western, Saturday for Persian)
function copyMondayScheduleToAllDays() {
  const firstDaySchedule = scheduleData[0]

  for (let i = 1; i < scheduleData.length; i++) {
    scheduleData[i].isOpen = firstDaySchedule.isOpen
    scheduleData[i].openTime = firstDaySchedule.openTime
    scheduleData[i].closeTime = firstDaySchedule.closeTime

    // Clear existing breaks
    scheduleData[i].breaks = []

    // Copy breaks from first day
    if (firstDaySchedule.isOpen) {
      firstDaySchedule.breaks.forEach((breakTime: { startTime: string; endTime: string }) => {
        scheduleData[i].breaks.push({
          startTime: breakTime.startTime,
          endTime: breakTime.endTime
        })
      })
    }
  }
}

// Clear all hours
function clearAllHours() {
  scheduleData.forEach((day: BusinessHoursWithBreaks) => {
    day.isOpen = false
    day.breaks = []
  })
}

// Add a break
function addBreak(dayIndex: number) {
  if (scheduleData[dayIndex].breaks.length < 3) {
    scheduleData[dayIndex].breaks.push({
      startTime: '12:00',
      endTime: '13:00',
    })
  }
}

// Remove a break
function removeBreak(dayIndex: number, breakIndex: number) {
  scheduleData[dayIndex].breaks.splice(breakIndex, 1)
}

// Toggle day open/closed
function onToggleDay(dayIndex: number) {
  // If day is closed, clear breaks
  if (!scheduleData[dayIndex].isOpen) {
    scheduleData[dayIndex].breaks = []
  }
}

// Validate time range (closing time must be after opening time)
function validateTimeRange(dayIndex: number) {
  const day = scheduleData[dayIndex]

  if (day.openTime >= day.closeTime) {
    // Auto-correct by setting closing time to 8 hours after opening time
    const [openHours, openMinutes] = day.openTime.split(':').map(Number)
    let closeHours = openHours + 8

    if (closeHours >= 24) {
      closeHours = 23
      day.closeTime = `${closeHours.toString().padStart(2, '0')}:30`
    } else {
      day.closeTime = `${closeHours.toString().padStart(2, '0')}:${openMinutes.toString().padStart(2, '0')}`
    }

    errorMessage.value = `Closing time must be after opening time. Adjusted for ${weekDays.value[dayIndex].label}.`
  }

  // Validate all breaks within business hours
  day.breaks.forEach((_: { startTime: string; endTime: string }, breakIndex: number) => {
    validateBreakTime(dayIndex, breakIndex)
  })
}

// Validate break time
function validateBreakTime(dayIndex: number, breakIndex: number) {
  const day = scheduleData[dayIndex]
  const breakTime = day.breaks[breakIndex]

  const dayStart = day.openTime
  const dayEnd = day.closeTime

  // Break must be within business hours
  if (breakTime.startTime < dayStart) {
    breakTime.startTime = dayStart
  }

  if (breakTime.endTime > dayEnd) {
    breakTime.endTime = dayEnd
  }

  // Break end must be after break start
  if (breakTime.startTime >= breakTime.endTime) {
    // Add 1 hour to start time for end time
    const [startHours, startMinutes] = breakTime.startTime.split(':').map(Number)
    let endHours = startHours + 1

    if (endHours >= 24) {
      endHours = 23
      breakTime.endTime = `${endHours.toString().padStart(2, '0')}:30`
    } else {
      breakTime.endTime = `${endHours.toString().padStart(2, '0')}:${startMinutes.toString().padStart(2, '0')}`
    }

    // Make sure it's still within business hours
    if (breakTime.endTime > dayEnd) {
      breakTime.endTime = dayEnd
    }
  }
}

// Load existing business hours
onMounted(async () => {
  try {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const provider = providerStore.currentProvider
    if (!provider) {
      errorMessage.value = 'Provider profile not found. Please register as a provider first.'
      return
    }

    // Load hours from the hours store
    isLoadingHours.value = true
    try {
      await hoursStore.loadSchedule(provider.id)

      // Map hours store data to scheduleData format
      if (hoursStore.state.baseHours && hoursStore.state.baseHours.length > 0) {
        hoursStore.state.baseHours.forEach((hours) => {
          const dayIndex = hours.dayOfWeek

          scheduleData[dayIndex].isOpen = hours.isOpen
          scheduleData[dayIndex].openTime = hours.openTime || '09:00'
          scheduleData[dayIndex].closeTime = hours.closeTime || '17:00'
          scheduleData[dayIndex].breaks = []

          // Add breaks if they exist
          if (hours.breaks && hours.breaks.length > 0) {
            hours.breaks.forEach((breakTime) => {
              scheduleData[dayIndex].breaks.push({
                startTime: breakTime.startTime,
                endTime: breakTime.endTime,
              })
            })
          }
        })
      } else if (provider.businessHours && provider.businessHours.length > 0) {
        // Fallback to provider data if hours store is empty
        provider.businessHours.forEach((hours) => {
          const dayIndex = hours.dayOfWeek

          scheduleData[dayIndex].isOpen = hours.isOpen
          scheduleData[dayIndex].openTime = hours.openTime
          scheduleData[dayIndex].closeTime = hours.closeTime
          scheduleData[dayIndex].breaks = []

          if (hours.breaks && hours.breaks.length > 0) {
            hours.breaks.forEach((breakTime) => {
              scheduleData[dayIndex].breaks.push({
                startTime: breakTime.startTime,
                endTime: breakTime.endTime,
              })
            })
          }
        })
      } else {
        // If no business hours set, use default business hours
        setStandardBusinessHours()
      }
    } catch (error) {
      console.error('Error loading hours from store:', error)
      // Fallback to default hours on error
      setStandardBusinessHours()
    } finally {
      isLoadingHours.value = false
    }

    // Load provider preferences
    if (provider.allowOnlineBooking !== undefined) {
      allowBookingsOutsideBusinessHours.value = provider.allowOnlineBooking
    }
  } catch (error) {
    console.error('Error loading provider business hours:', error)
    errorMessage.value = 'Failed to load business hours. Please try again.'
  }
})

// Prepare data for API
function prepareBusinessHours(): BusinessHours[] {
  return scheduleData.map((day: BusinessHoursWithBreaks) => ({
    id: '', // Will be assigned by the backend
    dayOfWeek: day.dayOfWeek,
    isOpen: day.isOpen,
    openTime: day.openTime || '',
    closeTime: day.closeTime || '',
    breaks: (day.breaks || []).map((breakTime: { startTime: string; endTime: string }): TimeBreak => ({
      startTime: breakTime.startTime,
      endTime: breakTime.endTime,
    })),
  }))
}

// Handle form submission
async function handleSubmit() {
  errorMessage.value = null
  successMessage.value = null

  // Validate all time ranges
  scheduleData.forEach((day: BusinessHoursWithBreaks, index: number) => {
    if (day.isOpen) {
      validateTimeRange(index)
    }
  })

  isSaving.value = true

  try {
    const provider = providerStore.currentProvider
    if (!provider) {
      throw new Error('Provider not found. Please try refreshing the page.')
    }

    // Prepare business hours data for hours store
    const businessHoursData: BusinessHoursWithBreaks[] = scheduleData.map((day: BusinessHoursWithBreaks) => ({
      dayOfWeek: day.dayOfWeek,
      isOpen: day.isOpen,
      openTime: day.isOpen ? day.openTime : undefined,
      closeTime: day.isOpen ? day.closeTime : undefined,
      breaks: day.isOpen ? (day.breaks || []) : [],
    }))

    // Update hours using the dedicated business-hours endpoint
    await hoursStore.updateHours({
      providerId: provider.id,
      businessHours: businessHoursData,
    })

    // Also update provider settings (for backward compatibility and other fields)
    const businessHours = prepareBusinessHours()
    const updateData: Partial<typeof provider> = {
      businessHours: businessHours,
      allowOnlineBooking: allowBookingsOutsideBusinessHours.value,
    }

    // Update provider (non-blocking - just sync other settings)
    providerStore.updateProvider(provider.id, updateData).catch(err => {
      console.warn('Provider update failed but business hours saved:', err)
      // Don't throw - business hours are already saved via hours endpoint
    })

    successMessage.value = 'Business hours saved successfully!'
    window.scrollTo({ top: 0, behavior: 'smooth' })

    // Check if we came from onboarding (query param or referrer)
    const fromOnboarding = router.currentRoute.value.query.from === 'onboarding'

    // Redirect after a short delay
    setTimeout(() => {
      if (fromOnboarding) {
        router.push({ name: 'ProviderOnboarding' })
      } else {
        router.push({ name: 'ProviderDashboard' })
      }
    }, 1500)
  } catch (error) {
    console.error('Error saving business hours:', error)
    errorMessage.value =
      error instanceof Error ? error.message : 'Failed to save business hours'
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    isSaving.value = false
  }
}

// Handle day click from calendar
function handleDayClick(day: CalendarDay) {
  // Find the schedule data for this day
  const daySchedule = scheduleData.find((d: BusinessHoursWithBreaks) => d.dayOfWeek === day.dayOfWeek)

  if (daySchedule) {
    selectedDaySchedule.value = { ...daySchedule }
    selectedDayLabel.value = getDayLabel(day.dayOfWeek)
    selectedDate.value = day.date // Set the selected date for holiday/exception management
    isModalOpen.value = true
  }
}

// Get day label by day of week value
function getDayLabel(dayOfWeek: number): string {
  const dayKey = Object.keys(DayOfWeek).find(key => DayOfWeek[key as keyof typeof DayOfWeek] === dayOfWeek)
  if (dayKey) {
    return t(`provider.businessHours.weekDays.${dayKey.toLowerCase()}`)
  }
  return ''
}

// Close modal
function closeModal() {
  isModalOpen.value = false
  selectedDaySchedule.value = null
  selectedDayLabel.value = ''
}

// Save day schedule from modal
async function saveDaySchedule(schedule: BusinessHoursWithBreaks) {
  const dayIndex = scheduleData.findIndex((d: BusinessHoursWithBreaks) => d.dayOfWeek === schedule.dayOfWeek)

  if (dayIndex !== -1) {
    // Update local state
    scheduleData[dayIndex].isOpen = schedule.isOpen
    scheduleData[dayIndex].openTime = schedule.openTime || '09:00'
    scheduleData[dayIndex].closeTime = schedule.closeTime || '17:00'
    scheduleData[dayIndex].breaks = schedule.breaks ? [...schedule.breaks] : []
  }

  // Save to API
  isSaving.value = true
  errorMessage.value = null

  try {
    const provider = providerStore.currentProvider
    if (!provider) {
      throw new Error('Provider not found. Please try refreshing the page.')
    }

    // Prepare business hours data for hours store
    const businessHoursData: BusinessHoursWithBreaks[] = scheduleData.map((day: BusinessHoursWithBreaks) => ({
      dayOfWeek: day.dayOfWeek,
      isOpen: day.isOpen,
      openTime: day.isOpen ? day.openTime : undefined,
      closeTime: day.isOpen ? day.closeTime : undefined,
      breaks: day.isOpen ? (day.breaks || []) : [],
    }))

    // Update hours using the dedicated business-hours endpoint
    await hoursStore.updateHours({
      providerId: provider.id,
      businessHours: businessHoursData,
    })

    successMessage.value = 'Business hours saved successfully!'
    window.scrollTo({ top: 0, behavior: 'smooth' })

    closeModal()
  } catch (error) {
    console.error('Error saving business hours:', error)
    errorMessage.value = error instanceof Error ? error.message : 'Failed to save business hours'
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    isSaving.value = false
  }
}

// Handle time change from list view
function handleTimeChange(index: number, field: 'openTime' | 'closeTime', value: string) {
  scheduleData[index][field] = value
  validateTimeRange(index)
}

// Handle break change from list view
function handleBreakChange(dayIndex: number, breakIndex: number, field: 'startTime' | 'endTime', value: string) {
  scheduleData[dayIndex].breaks[breakIndex][field] = value
  validateBreakTime(dayIndex, breakIndex)
}

// Go back to previous page or dashboard
function goBack() {
  const fromOnboarding = router.currentRoute.value.query.from === 'onboarding'

  if (fromOnboarding) {
    router.push({ name: 'ProviderOnboarding' })
  } else {
    // Go back to previous page or dashboard
    router.back()
  }
}
</script>

<style scoped>
.business-hours-view {
  max-width: 1000px;
  margin: 0 auto;
  padding: 2rem;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
  gap: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
  color: #111827;
}

.page-subtitle {
  font-size: 1rem;
  color: #6b7280;
  margin: 0;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
}

.hours-form {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.form-section {
  padding: 2rem;
}

.form-section-header {
  margin-bottom: 2rem;
}

.section-title {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
  color: #111827;
}

.section-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0 0 1rem 0;
}

.quick-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1rem;
}

.days-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.day-schedule {
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  overflow: hidden;
  transition: all 0.2s ease;
}

.day-schedule:hover {
  border-color: #d1d5db;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.day-closed {
  background-color: #f9fafb;
}

.day-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  background-color: #f3f4f6;
  border-bottom: 1px solid #e5e7eb;
}

.day-name {
  font-size: 1rem;
  font-weight: 600;
  margin: 0;
  color: #111827;
}

.day-toggle {
  display: flex;
  align-items: center;
}

.toggle-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
}

.toggle-label span {
  font-size: 0.875rem;
  font-weight: 500;
}

.toggle-switch {
  position: relative;
  display: inline-block;
  width: 3rem;
  height: 1.5rem;
  background-color: #e5e7eb;
  border-radius: 1.5rem;
  transition: all 0.2s ease;
}

.toggle-label input {
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle-label input:checked + .toggle-switch {
  background-color: #10b981;
}

.toggle-switch:before {
  position: absolute;
  content: "";
  height: 1.25rem;
  width: 1.25rem;
  left: 0.125rem;
  bottom: 0.125rem;
  background-color: white;
  border-radius: 50%;
  transition: all 0.2s ease;
}

.toggle-label input:checked + .toggle-switch:before {
  transform: translateX(1.5rem);
}

.day-hours {
  padding: 1.5rem;
}

.day-closed-message {
  padding: 1.5rem;
  text-align: center;
  color: #6b7280;
}

.time-range {
  display: flex;
  align-items: flex-end;
  gap: 1rem;
}

.time-input {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.time-input label {
  font-size: 0.875rem;
  color: #6b7280;
  font-weight: 500;
}

.time-input select {
  padding: 0.625rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  color: #111827;
  background-color: white;
  cursor: pointer;
}

.time-separator {
  display: flex;
  align-items: center;
  padding-bottom: 0.625rem;
  color: #6b7280;
  font-size: 0.875rem;
}

.break-section {
  margin-top: 1.5rem;
  border-top: 1px dashed #e5e7eb;
  padding-top: 1.5rem;
}

.break-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.break-title {
  font-size: 0.875rem;
  font-weight: 600;
  margin: 0;
  color: #4b5563;
}

.no-breaks {
  text-align: center;
  padding: 1rem;
  color: #6b7280;
  font-style: italic;
  background-color: #f9fafb;
  border-radius: 0.375rem;
  font-size: 0.875rem;
}

.break-time-container {
  margin-bottom: 1rem;
  padding: 0.75rem;
  background-color: #f9fafb;
  border-radius: 0.375rem;
}

.break-time-container:last-child {
  margin-bottom: 0;
}

.remove-break {
  margin-left: auto;
}

.special-settings {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.checkbox-group {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.checkbox-label {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  cursor: pointer;
  padding: 1rem;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  transition: all 0.2s;
}

.checkbox-label:hover {
  border-color: #3b82f6;
  background: #eff6ff;
}

.checkbox {
  width: 1.25rem;
  height: 1.25rem;
  margin-top: 0.125rem;
  cursor: pointer;
  flex-shrink: 0;
}

.checkbox-text {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.checkbox-text strong {
  font-weight: 600;
  color: #111827;
}

.checkbox-text small {
  font-size: 0.875rem;
  color: #6b7280;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #111827;
}

.form-group small {
  font-size: 0.75rem;
  color: #6b7280;
}

.input-with-suffix {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.number-input {
  padding: 0.625rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  color: #111827;
  width: 5rem;
}

.input-suffix {
  font-size: 0.875rem;
  color: #6b7280;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.view-mode-toggle {
  display: flex;
  gap: 0.25rem;
  background: #e5e7eb;
  border-radius: 0.375rem;
  padding: 0.25rem;
}

.view-mode-btn {
  padding: 0.5rem 1rem;
  border: none;
  background: transparent;
  border-radius: 0.25rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
}

.view-mode-btn.active {
  background: white;
  color: #111827;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
}

.calendar-container,
.list-container {
  margin-bottom: 1rem;
}

@media (max-width: 768px) {
  .business-hours-view {
    padding: 1rem;
  }

  .page-header {
    flex-direction: column;
  }

  .header-actions {
    flex-direction: column;
    width: 100%;
  }

  .view-mode-toggle {
    width: 100%;
  }

  .view-mode-btn {
    flex: 1;
  }

  .quick-actions {
    flex-direction: column;
    gap: 0.5rem;
  }

  .time-range {
    flex-direction: column;
    gap: 1rem;
  }

  .time-separator {
    padding: 0;
    margin: 0 auto;
  }

  .form-actions {
    flex-direction: column-reverse;
  }

  .form-actions button {
    width: 100%;
  }
}
</style>
