<template>
  <div class="registration-step">
    <ProgressIndicator :current-step="6" :total-steps="9" />

    <div class="step-card">
      <div class="step-header">
        <h2 class="step-title">ساعات کاری</h2>
        <p class="step-description">روزها و ساعات کاری خود را مشخص کنید</p>
      </div>

      <div class="step-content">
        <!-- Days Schedule Editor -->
        <DayScheduleEditor
          :model-value="scheduleForEditor"
          :week-days="weekDays"
          start-time-label="ساعت شروع"
          end-time-label="ساعت پایان"
          :show-breaks="true"
          breaks-label="استراحت‌ها"
          break-start-label="شروع"
          break-end-label="پایان"
          add-break-text="افزودن استراحت"
          remove-break-label="حذف"
          no-breaks-text="بدون استراحت"
          :show-copy-button="true"
          copy-button-text="کپی به سایر روزها"
          copy-button-label="کپی به سایر روزها"
          @update:model-value="handleScheduleUpdate"
        />

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
import { ref, computed, onMounted } from 'vue'
import ProgressIndicator from '../shared/ProgressIndicator.vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import DayScheduleEditor from '@/shared/components/schedule/DayScheduleEditor.vue'
import type { DayScheduleItem, BreakPeriod } from '@/shared/components/schedule/DayScheduleEditor.vue'
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

// Convert TimeSlot to string (HH:mm format)
const timeToString = (time: { hours: number; minutes: number } | null): string => {
  if (!time) return '09:00'
  return `${String(time.hours).padStart(2, '0')}:${String(time.minutes).padStart(2, '0')}`
}

// Convert string to TimeSlot
const stringToTime = (timeStr: string): { hours: number; minutes: number } => {
  const [hours, minutes] = timeStr.split(':').map(Number)
  return { hours, minutes }
}

// Convert DayHours[] to DayScheduleItem[] for the editor component
const scheduleForEditor = computed<DayScheduleItem[]>(() => {
  return schedule.value.map(day => ({
    isOpen: day.isOpen,
    startTime: timeToString(day.openTime),
    endTime: timeToString(day.closeTime),
    breaks: day.breaks?.map(brk => ({
      id: brk.id,
      start: timeToString(brk.start),
      end: timeToString(brk.end),
    })) || [],
  }))
})

// Handle updates from the editor component
const handleScheduleUpdate = (updatedSchedule: DayScheduleItem[]) => {
  schedule.value = updatedSchedule.map((day, index) => ({
    dayOfWeek: index,
    isOpen: day.isOpen,
    openTime: day.isOpen ? stringToTime(day.startTime) : null,
    closeTime: day.isOpen ? stringToTime(day.endTime) : null,
    breaks: day.breaks?.map(brk => ({
      id: brk.id,
      start: stringToTime(brk.start),
      end: stringToTime(brk.end),
    })) || [],
  }))

  emit('update:modelValue', schedule.value)
}

// Initialize and emit default schedule if not provided
onMounted(() => {
  // If no modelValue was provided, emit the default schedule
  if (!props.modelValue || props.modelValue.length === 0) {
    emit('update:modelValue', schedule.value)
  }
})

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
}
</style>
