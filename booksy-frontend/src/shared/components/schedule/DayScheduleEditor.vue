<template>
  <div class="day-schedule-editor">
    <div
      v-for="(day, index) in localSchedule"
      :key="index"
      :class="['day-card', { 'day-disabled': !day.isOpen }]"
    >
      <div class="day-row">
        <!-- Toggle + Day Name -->
        <div class="day-left">
          <label class="toggle-switch">
            <input
              type="checkbox"
              :checked="day.isOpen"
              @change="toggleDay(index, ($event.target as HTMLInputElement).checked)"
            />
            <span class="toggle-slider"></span>
          </label>
          <span class="day-name">{{ weekDays[index] }}</span>
          <span v-if="!day.isOpen" class="closed-badge">تعطیل</span>
        </div>

        <!-- Time Inputs (Inline) -->
        <div v-if="day.isOpen" class="time-fields">
          <div class="time-input-group">
            <label class="time-label">{{ startTimeLabel }}</label>
            <PersianTimePicker
              :model-value="day.startTime"
              :placeholder="startTimeLabel"
              @update:model-value="(value) => updateTime(index, 'startTime', value)"
            />
          </div>
          <div class="time-input-group">
            <label class="time-label">{{ endTimeLabel }}</label>
            <PersianTimePicker
              :model-value="day.endTime"
              :placeholder="endTimeLabel"
              @update:model-value="(value) => updateTime(index, 'endTime', value)"
            />
          </div>
          <div v-if="showBreakTime" class="time-input-group">
            <label class="time-label">{{ breakTimeLabel }}</label>
            <PersianTimePicker
              :model-value="day.breakTime || ''"
              :placeholder="breakTimeLabel"
              @update:model-value="(value) => updateTime(index, 'breakTime', value)"
            />
          </div>
          <button
            v-if="showCopyButton"
            type="button"
            class="copy-btn"
            @click="copyToAll(index)"
            :title="copyButtonLabel"
          >
            <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
              />
            </svg>
            <span class="copy-btn-text">{{ copyButtonText }}</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import PersianTimePicker from '@/shared/components/calendar/PersianTimePicker.vue'

export interface DayScheduleItem {
  isOpen: boolean
  startTime: string
  endTime: string
  breakTime?: string
}

interface Props {
  modelValue: DayScheduleItem[]
  weekDays?: string[]
  startTimeLabel?: string
  endTimeLabel?: string
  breakTimeLabel?: string
  showBreakTime?: boolean
  showCopyButton?: boolean
  copyButtonText?: string
  copyButtonLabel?: string
}

interface Emits {
  (e: 'update:modelValue', value: DayScheduleItem[]): void
}

const props = withDefaults(defineProps<Props>(), {
  weekDays: () => ['شنبه', 'یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنجشنبه', 'جمعه'],
  startTimeLabel: 'ساعت شروع',
  endTimeLabel: 'ساعت پایان',
  breakTimeLabel: 'استراحت',
  showBreakTime: false,
  showCopyButton: false,
  copyButtonText: 'کپی',
  copyButtonLabel: 'کپی به همه روزها',
})

const emit = defineEmits<Emits>()

// Local copy of schedule
const localSchedule = ref<DayScheduleItem[]>([...props.modelValue])

// Watch for external changes
watch(() => props.modelValue, (newValue) => {
  localSchedule.value = [...newValue]
}, { deep: true })

const toggleDay = (index: number, isOpen: boolean) => {
  localSchedule.value[index].isOpen = isOpen
  emitUpdate()
}

const updateTime = (index: number, field: 'startTime' | 'endTime' | 'breakTime', value: string) => {
  if (field === 'breakTime') {
    localSchedule.value[index].breakTime = value
  } else {
    localSchedule.value[index][field] = value
  }
  emitUpdate()
}

const copyToAll = (sourceIndex: number) => {
  const sourceDay = localSchedule.value[sourceIndex]
  localSchedule.value = localSchedule.value.map(() => ({
    isOpen: sourceDay.isOpen,
    startTime: sourceDay.startTime,
    endTime: sourceDay.endTime,
    breakTime: sourceDay.breakTime,
  }))
  emitUpdate()
}

const emitUpdate = () => {
  emit('update:modelValue', [...localSchedule.value])
}
</script>

<style scoped>
.day-schedule-editor {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.day-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.75rem;
  padding: 1.25rem;
  transition: all 0.2s;
}

.day-card:hover {
  border-color: #d1d5db;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.day-card.day-disabled {
  background: #f9fafb;
  opacity: 0.8;
}

.day-row {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.day-left {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.day-name {
  font-size: 1rem;
  font-weight: 600;
  color: #1a1a1a;
  min-width: 5rem;
}

.closed-badge {
  font-size: 0.75rem;
  color: #6b7280;
  background: #f3f4f6;
  padding: 0.25rem 0.75rem;
  border-radius: 999px;
}

/* Toggle Switch */
.toggle-switch {
  position: relative;
  display: inline-block;
  width: 3rem;
  height: 1.75rem;
  flex-shrink: 0;
}

.toggle-switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle-slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #e5e7eb;
  transition: 0.3s;
  border-radius: 1.75rem;
}

.toggle-slider:before {
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

.toggle-switch input:checked + .toggle-slider {
  background-color: #8b5cf6;
}

.toggle-switch input:checked + .toggle-slider:before {
  transform: translateX(1.25rem);
}

/* Time Fields */
.time-fields {
  display: grid;
  grid-template-columns: repeat(2, 1fr) auto;
  gap: 0.75rem;
  align-items: end;
}

/* When breakTime is shown, adjust grid */
.day-card:has(.time-input-group:nth-child(3)) .time-fields {
  grid-template-columns: repeat(3, 1fr) auto;
}

@media (max-width: 768px) {
  .time-fields {
    grid-template-columns: 1fr !important;
  }
}

.time-input-group {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.time-label {
  font-size: 0.75rem;
  color: #6b7280;
  font-weight: 500;
}

.copy-btn {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.625rem 1rem;
  background: #f3f4f6;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  color: #374151;
  cursor: pointer;
  transition: all 0.2s;
  height: fit-content;
  white-space: nowrap;
}

.copy-btn:hover {
  background: #e5e7eb;
  color: #8b5cf6;
  border-color: #8b5cf6;
}

.btn-icon {
  width: 1rem;
  height: 1rem;
  flex-shrink: 0;
}

@media (max-width: 640px) {
  .copy-btn-text {
    display: none;
  }
}
</style>
