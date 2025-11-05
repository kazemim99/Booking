<template>
  <div class="registration-step">
    <ProgressIndicator :current-step="6" :total-steps="9" />

    <div class="step-card">
      <div class="step-header">
        <h2 class="step-title">ساعات کاری</h2>
        <p class="step-description">روزها و ساعات کاری خود را مشخص کنید</p>
      </div>

      <div class="step-content">
        <!-- Days Schedule -->
        <div class="days-list">
          <div
            v-for="(day, index) in weekDays"
            :key="index"
            class="day-item"
            :class="{ 'day-closed': !schedule[index].isOpen }"
          >
            <div class="day-header">
              <div class="day-toggle">
                <label class="switch">
                  <input
                    type="checkbox"
                    :checked="schedule[index].isOpen"
                    @change="handleToggleDay(index)"
                  />
                  <span class="slider"></span>
                </label>
                <label
                  class="day-name"
                  @click="handleToggleDay(index)"
                >
                  {{ day }}
                </label>
              </div>

              <button
                v-if="schedule[index].isOpen"
                type="button"
                class="btn-copy"
                @click="handleCopySchedule(index)"
                title="کپی به سایر روزها"
              >
                <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                  />
                </svg>
                کپی به سایر روزها
              </button>
            </div>

            <div v-if="schedule[index].isOpen" class="day-times">
              <div class="time-group">
                <label class="time-label">ساعت شروع</label>
                <input
                  type="time"
                  :value="timeToString(schedule[index].openTime)"
                  dir="ltr"
                  class="time-input"
                  @change="(e) => handleTimeChange(index, 'openTime', (e.target as HTMLInputElement).value)"
                />
              </div>
              <div class="time-group">
                <label class="time-label">ساعت پایان</label>
                <input
                  type="time"
                  :value="timeToString(schedule[index].closeTime)"
                  dir="ltr"
                  class="time-input"
                  @change="(e) => handleTimeChange(index, 'closeTime', (e.target as HTMLInputElement).value)"
                />
              </div>
            </div>
          </div>
        </div>

        <!-- Error Message -->
        <p v-if="error" class="error-message">{{ error }}</p>

        <!-- Navigation -->
        <div class="step-actions">
          <AppButton type="button" variant="outline" size="large" @click="$emit('back')">
            قبلی
          </AppButton>
          <AppButton type="button" variant="primary" size="large" @click="handleNext">
            بعدی
          </AppButton>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import ProgressIndicator from '../shared/ProgressIndicator.vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import type { DayHours } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: DayHours[]
}

interface Emits {
  (e: 'update:modelValue', value: DayHours[]): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Persian week days (starting from Saturday = 0)
const weekDays = ['شنبه', 'یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنجشنبه', 'جمعه']

// Initialize schedule from props or default
const initializeSchedule = (): DayHours[] => {
  if (props.modelValue && props.modelValue.length === 7) {
    return props.modelValue
  }

  // Default schedule (Saturday-Thursday open, Friday closed)
  return weekDays.map((_, index) => ({
    dayOfWeek: index,
    isOpen: index !== 6, // Friday (index 6) is closed by default
    openTime: { hours: 9, minutes: 0 },
    closeTime: { hours: 18, minutes: 0 },
    breaks: [],
  }))
}

const schedule = ref<DayHours[]>(initializeSchedule())
const error = ref('')

// Initialize and emit default schedule if not provided
onMounted(() => {
  // If no modelValue was provided, emit the default schedule
  if (!props.modelValue || props.modelValue.length === 0) {
    emit('update:modelValue', schedule.value)
  }
})

// Convert time object to string for input
const timeToString = (time: { hours: number; minutes: number } | null): string => {
  if (!time) return '09:00'
  return `${String(time.hours).padStart(2, '0')}:${String(time.minutes).padStart(2, '0')}`
}

// Convert string to time object
const stringToTime = (timeStr: string): { hours: number; minutes: number } => {
  const [hours, minutes] = timeStr.split(':').map(Number)
  return { hours, minutes }
}

// Methods
const handleToggleDay = (dayIndex: number) => {
  const isOpen = !schedule.value[dayIndex].isOpen
  schedule.value[dayIndex].isOpen = isOpen

  // If closing the day, set times to null
  if (!isOpen) {
    schedule.value[dayIndex].openTime = null
    schedule.value[dayIndex].closeTime = null
  } else {
    // If opening the day, set default times if not already set
    if (!schedule.value[dayIndex].openTime) {
      schedule.value[dayIndex].openTime = { hours: 9, minutes: 0 }
    }
    if (!schedule.value[dayIndex].closeTime) {
      schedule.value[dayIndex].closeTime = { hours: 18, minutes: 0 }
    }
  }

  emit('update:modelValue', schedule.value)
}

const handleTimeChange = (dayIndex: number, field: 'openTime' | 'closeTime', value: string) => {
  schedule.value[dayIndex][field] = stringToTime(value)
  emit('update:modelValue', schedule.value)
}

const handleCopySchedule = (fromDayIndex: number) => {
  const fromSchedule = schedule.value[fromDayIndex]

  schedule.value = schedule.value.map((day, index) => {
    if (index === fromDayIndex) return day

    return {
      ...day,
      isOpen: fromSchedule.isOpen,
      openTime: { ...fromSchedule.openTime! },
      closeTime: { ...fromSchedule.closeTime! },
    }
  })

  emit('update:modelValue', schedule.value)
}

const handleNext = () => {
  error.value = ''

  // Validate at least one day is open
  const hasOpenDay = schedule.value.some((day) => day.isOpen)

  if (!hasOpenDay) {
    error.value = 'لطفاً حداقل یک روز کاری را انتخاب کنید'
    return
  }

  // Validate that open days have valid times
  const invalidDays: string[] = []
  schedule.value.forEach((day, index) => {
    if (day.isOpen) {
      if (!day.openTime || !day.closeTime) {
        invalidDays.push(weekDays[index])
      }
    }
  })

  if (invalidDays.length > 0) {
    error.value = `لطفاً ساعات کاری را برای روزهای زیر تعیین کنید: ${invalidDays.join('، ')}`
    return
  }

  // Validate that close time is after open time
  const invalidTimeRanges: string[] = []
  schedule.value.forEach((day, index) => {
    if (day.isOpen && day.openTime && day.closeTime) {
      const openMinutes = day.openTime.hours * 60 + day.openTime.minutes
      const closeMinutes = day.closeTime.hours * 60 + day.closeTime.minutes

      if (closeMinutes <= openMinutes) {
        invalidTimeRanges.push(weekDays[index])
      }
    }
  })

  if (invalidTimeRanges.length > 0) {
    error.value = `ساعت پایان باید بعد از ساعت شروع باشد برای: ${invalidTimeRanges.join('، ')}`
    return
  }

  emit('next')
}
</script>

<style scoped>
.registration-step {
  min-height: 100vh;
  padding: 2rem 1rem;
  background: #f9fafb;
  direction: rtl;
}

.step-card {
  max-width: 42rem;
  margin: 0 auto;
  background: white;
  border-radius: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  padding: 2rem;
}

.step-header {
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.step-description {
  font-size: 0.875rem;
  color: #6b7280;
}

.step-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Days List */
.days-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.day-item {
  padding: 1rem;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
}

.day-item.day-closed {
  background: rgba(0, 0, 0, 0.02);
}

.day-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.75rem;
}

.day-toggle {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.day-name {
  font-size: 1rem;
  font-weight: 500;
  color: #111827;
  cursor: pointer;
  user-select: none;
}

/* Toggle Switch */
.switch {
  position: relative;
  display: inline-block;
  width: 3rem;
  height: 1.75rem;
}

.switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #d1d5db;
  transition: 0.3s;
  border-radius: 2rem;
}

.slider:before {
  position: absolute;
  content: '';
  height: 1.25rem;
  width: 1.25rem;
  left: 0.25rem;
  bottom: 0.25rem;
  background-color: white;
  transition: 0.3s;
  border-radius: 50%;
}

input:checked + .slider {
  background-color: #8b5cf6;
}

input:checked + .slider:before {
  transform: translateX(1.25rem);
}

/* Copy Button */
.btn-copy {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.5rem 0.75rem;
  font-size: 0.875rem;
  color: #6b7280;
  background: none;
  border: 1px solid #e5e7eb;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-copy:hover {
  background: #f9fafb;
  border-color: #8b5cf6;
  color: #8b5cf6;
}

.btn-copy .icon {
  width: 1rem;
  height: 1rem;
}

/* Day Times */
.day-times {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  padding-right: 3.75rem;
}

.time-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.time-label {
  font-size: 0.875rem;
  color: #6b7280;
}

.time-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
  text-align: left;
}

.time-input:focus {
  outline: none;
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

/* Error Message */
.error-message {
  font-size: 0.875rem;
  color: #ef4444;
  text-align: center;
}

/* Navigation */
.step-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.step-actions > * {
  flex: 1;
}

@media (max-width: 640px) {
  .step-card {
    padding: 1.5rem;
  }

  .step-title {
    font-size: 1.25rem;
  }

  .day-times {
    padding-right: 0;
    grid-template-columns: 1fr;
  }

  .btn-copy {
    font-size: 0.75rem;
    padding: 0.375rem 0.5rem;
  }

  .btn-copy span {
    display: none;
  }
}
</style>
