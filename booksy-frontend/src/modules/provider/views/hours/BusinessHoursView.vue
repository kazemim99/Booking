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
      <Button variant="secondary" @click="goBack">← Back to Onboarding</Button>
    </div>

    <!-- Loading State -->
    <div v-if="providerStore.isLoading" class="loading-state">
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
    <form v-if="!providerStore.isLoading" class="hours-form" @submit.prevent="handleSubmit">
      <Card class="form-section">
        <div class="form-section-header">
          <h2 class="section-title">Operating Hours</h2>
          <p class="section-description">
            Set your standard operating hours for each day of the week.
          </p>

          <div class="quick-actions">
            <Button nativeType="button" variant="secondary" size="small" @click="copyMondayScheduleToAllDays">
              Copy Monday to All Days
            </Button>
            <Button nativeType="button" variant="secondary" size="small" @click="setStandardBusinessHours">
              Set Standard Business Hours
            </Button>
            <Button nativeType="button" variant="secondary" size="small" @click="clearAllHours">
              Clear All Hours
            </Button>
          </div>
        </div>

        <!-- Days of Week -->
        <div class="days-container">
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
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { DayOfWeek, BusinessHours, TimeBreak } from '@/modules/provider/types/provider.types'
import { Button, Card, Alert, Spinner } from '@/shared/components'

const router = useRouter()
const providerStore = useProviderStore()

const isSaving = ref(false)
const errorMessage = ref<string | null>(null)
const successMessage = ref<string | null>(null)

// Special settings
const allowBookingsOutsideBusinessHours = ref(false)
const allowSameDayBookings = ref(true)
const advanceBookingHours = ref(2)
const maxBookingDays = ref(60)

// Days of the week
const weekDays = [
  { value: DayOfWeek.Monday, label: 'Monday' },
  { value: DayOfWeek.Tuesday, label: 'Tuesday' },
  { value: DayOfWeek.Wednesday, label: 'Wednesday' },
  { value: DayOfWeek.Thursday, label: 'Thursday' },
  { value: DayOfWeek.Friday, label: 'Friday' },
  { value: DayOfWeek.Saturday, label: 'Saturday' },
  { value: DayOfWeek.Sunday, label: 'Sunday' },
]

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
  return weekDays.map((day) => ({
    dayOfWeek: day.value,
    isOpen: false,
    openTime: '09:00',
    closeTime: '17:00',
    breaks: [] as { startTime: string; endTime: string }[],
  }))
}

// Schedule data
const scheduleData = reactive(createEmptyScheduleData())

// Set standard business hours (9 AM to 5 PM, Monday-Friday)
function setStandardBusinessHours() {
  weekDays.forEach((_, index) => {
    const isWeekday = index < 5 // Monday to Friday
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
  })
}

// Copy Monday's schedule to all days
function copyMondayScheduleToAllDays() {
  const mondaySchedule = scheduleData[0]

  for (let i = 1; i < scheduleData.length; i++) {
    scheduleData[i].isOpen = mondaySchedule.isOpen
    scheduleData[i].openTime = mondaySchedule.openTime
    scheduleData[i].closeTime = mondaySchedule.closeTime

    // Clear existing breaks
    scheduleData[i].breaks = []

    // Copy breaks from Monday
    if (mondaySchedule.isOpen) {
      mondaySchedule.breaks.forEach(breakTime => {
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
  scheduleData.forEach((day) => {
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

    errorMessage.value = `Closing time must be after opening time. Adjusted for ${weekDays[dayIndex].label}.`
  }

  // Validate all breaks within business hours
  day.breaks.forEach((_, breakIndex) => {
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

    // If provider has business hours, load them
    if (provider.businessHours && provider.businessHours.length > 0) {
      // Map existing business hours to our data structure
      provider.businessHours.forEach((hours) => {
        const dayIndex = hours.dayOfWeek

        scheduleData[dayIndex].isOpen = hours.isOpen
        scheduleData[dayIndex].openTime = hours.openTime
        scheduleData[dayIndex].closeTime = hours.closeTime

        // Clear existing breaks
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
    } else {
      // If no business hours set, use default business hours
      setStandardBusinessHours()
    }

    // Load provider preferences
    if (provider.allowOnlineBooking !== undefined) {
      allowBookingsOutsideBusinessHours.value = provider.allowOnlineBooking
    }

    // These would need to be added to the provider model in a real implementation
    // allowSameDayBookings.value = provider.allowSameDayBookings ?? true
    // advanceBookingHours.value = provider.advanceBookingHours ?? 2
    // maxBookingDays.value = provider.maxBookingDays ?? 60
  } catch (error) {
    console.error('Error loading provider business hours:', error)
    errorMessage.value = 'Failed to load business hours. Please try again.'
  }
})

// Prepare data for API
function prepareBusinessHours(): BusinessHours[] {
  return scheduleData.map((day) => ({
    id: '', // Will be assigned by the backend
    dayOfWeek: day.dayOfWeek,
    isOpen: day.isOpen,
    openTime: day.openTime,
    closeTime: day.closeTime,
    breaks: day.breaks.map((breakTime): TimeBreak => ({
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
  scheduleData.forEach((day, index) => {
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

    // Prepare business hours data
    const businessHours = prepareBusinessHours()

    // In a real implementation, these preferences would be included in the update
    const updateData = {
      businessHours: businessHours,
      allowOnlineBooking: allowBookingsOutsideBusinessHours.value,
      // These would need to be added to the model in a real implementation:
      // allowSameDayBookings: allowSameDayBookings.value,
      // advanceBookingHours: advanceBookingHours.value,
      // maxBookingDays: maxBookingDays.value,
    }

    // Update provider
    const updatedProvider = await providerStore.updateProvider(provider.id, updateData as any)

    // Check if update was successful
    if (!updatedProvider) {
      // Check if there's an error in the store
      if (providerStore.error) {
        throw new Error(providerStore.error)
      }
      throw new Error('Failed to update business hours. Please try again.')
    }

    successMessage.value = 'Business hours saved successfully!'
    window.scrollTo({ top: 0, behavior: 'smooth' })

    // Redirect after a short delay
    setTimeout(() => {
      router.push({ name: 'ProviderOnboarding' })
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

// Go back to onboarding
function goBack() {
  router.push({ name: 'ProviderOnboarding' })
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

@media (max-width: 768px) {
  .business-hours-view {
    padding: 1rem;
  }

  .page-header {
    flex-direction: column;
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