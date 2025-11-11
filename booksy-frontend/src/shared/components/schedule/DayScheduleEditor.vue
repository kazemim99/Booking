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
              :model-value="day.startTime || ''"
              :placeholder="startTimeLabel"
              @update:model-value="(value) => updateTime(index, 'startTime', value)"
            />
          </div>
          <div class="time-input-group">
            <label class="time-label">{{ endTimeLabel }}</label>
            <PersianTimePicker
              :model-value="day.endTime || ''"
              :placeholder="endTimeLabel"
              @update:model-value="(value) => updateTime(index, 'endTime', value)"
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

        <!-- Breaks Section -->
        <div v-if="day.isOpen && showBreaks" class="breaks-section">
          <div class="breaks-header">
            <label class="breaks-label">{{ breaksLabel }}</label>
            <button type="button" class="add-break-btn" @click="addBreak(index)">
              <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M12 4v16m8-8H4"
                />
              </svg>
              {{ addBreakText }}
            </button>
          </div>

          <div v-if="day.breaks && day.breaks.length > 0" class="breaks-list">
            <div
              v-for="(breakItem, breakIndex) in day.breaks"
              :key="breakItem.id"
              class="break-item"
            >
              <div class="break-times">
                <div class="break-time-input">
                  <label class="break-time-label">{{ breakStartLabel }}</label>
                  <PersianTimePicker
                    :model-value="breakItem.start"
                    :placeholder="breakStartLabel"
                    @update:model-value="(value) => updateBreak(index, breakIndex, 'start', value)"
                  />
                </div>
                <div class="break-time-input">
                  <label class="break-time-label">{{ breakEndLabel }}</label>
                  <PersianTimePicker
                    :model-value="breakItem.end"
                    :placeholder="breakEndLabel"
                    @update:model-value="(value) => updateBreak(index, breakIndex, 'end', value)"
                  />
                </div>
              </div>
              <button
                type="button"
                class="remove-break-btn"
                @click="removeBreak(index, breakIndex)"
                :title="removeBreakLabel"
              >
                <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>
          </div>
          <p v-else class="no-breaks-message">{{ noBreaksText }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import PersianTimePicker from '@/shared/components/calendar/PersianTimePicker.vue'

export interface BreakPeriod {
  id: string
  start: string
  end: string
}

export interface DayScheduleItem {
  isOpen: boolean
  startTime: string
  endTime: string
  breaks?: BreakPeriod[]
}

interface Props {
  modelValue: DayScheduleItem[]
  weekDays?: string[]
  startTimeLabel?: string
  endTimeLabel?: string
  showBreaks?: boolean
  breaksLabel?: string
  breakStartLabel?: string
  breakEndLabel?: string
  addBreakText?: string
  removeBreakLabel?: string
  noBreaksText?: string
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
  showBreaks: false,
  breaksLabel: 'استراحت‌ها',
  breakStartLabel: 'شروع استراحت',
  breakEndLabel: 'پایان استراحت',
  addBreakText: 'افزودن استراحت',
  removeBreakLabel: 'حذف استراحت',
  noBreaksText: 'استراحتی تعریف نشده است',
  showCopyButton: false,
  copyButtonText: 'کپی',
  copyButtonLabel: 'کپی به همه روزها',
})

const emit = defineEmits<Emits>()

// Local copy of schedule
const localSchedule = ref<DayScheduleItem[]>(JSON.parse(JSON.stringify(props.modelValue)))

// Watch for external changes
watch(() => props.modelValue, (newValue) => {
  localSchedule.value = JSON.parse(JSON.stringify(newValue))
}, { deep: true })

const toggleDay = (index: number, isOpen: boolean) => {
  localSchedule.value[index].isOpen = isOpen

  // Set default times when opening a day
  if (isOpen && !localSchedule.value[index].startTime) {
    localSchedule.value[index].startTime = '09:00'
  }
  if (isOpen && !localSchedule.value[index].endTime) {
    localSchedule.value[index].endTime = '18:00'
  }

  emitUpdate()
}

const updateTime = (index: number, field: 'startTime' | 'endTime', value: string | null) => {
  // Handle null/undefined values - keep the existing value if new value is falsy
  if (value !== null && value !== undefined && value !== '') {
    localSchedule.value[index][field] = value
    emitUpdate()
  }
}

const addBreak = (dayIndex: number) => {
  if (!localSchedule.value[dayIndex].breaks) {
    localSchedule.value[dayIndex].breaks = []
  }

  localSchedule.value[dayIndex].breaks!.push({
    id: `break-${Date.now()}-${Math.random()}`,
    start: '12:00',
    end: '13:00',
  })

  emitUpdate()
}

const removeBreak = (dayIndex: number, breakIndex: number) => {
  if (localSchedule.value[dayIndex].breaks) {
    localSchedule.value[dayIndex].breaks!.splice(breakIndex, 1)
    emitUpdate()
  }
}

const updateBreak = (dayIndex: number, breakIndex: number, field: 'start' | 'end', value: string | null) => {
  // Handle null/undefined values - keep the existing value if new value is falsy
  if (value !== null && value !== undefined && value !== '' && localSchedule.value[dayIndex].breaks) {
    localSchedule.value[dayIndex].breaks![breakIndex][field] = value
    emitUpdate()
  }
}

const copyToAll = (sourceIndex: number) => {
  const sourceDay = localSchedule.value[sourceIndex]
  localSchedule.value = localSchedule.value.map(() => ({
    isOpen: sourceDay.isOpen,
    startTime: sourceDay.startTime,
    endTime: sourceDay.endTime,
    breaks: sourceDay.breaks ? JSON.parse(JSON.stringify(sourceDay.breaks)) : [],
  }))
  emitUpdate()
}

const emitUpdate = () => {
  emit('update:modelValue', JSON.parse(JSON.stringify(localSchedule.value)))
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

/* Breaks Section */
.breaks-section {
  margin-top: 0.5rem;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb;
}

.breaks-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.75rem;
}

.breaks-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #374151;
}

.add-break-btn {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.375rem 0.75rem;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.8125rem;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
}

.add-break-btn:hover {
  background: #f9fafb;
  border-color: #8b5cf6;
  color: #8b5cf6;
}

.breaks-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.break-item {
  display: flex;
  gap: 0.75rem;
  align-items: flex-end;
  padding: 0.75rem;
  background: #f9fafb;
  border-radius: 0.5rem;
  border: 1px solid #e5e7eb;
}

.break-times {
  flex: 1;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

@media (max-width: 640px) {
  .break-times {
    grid-template-columns: 1fr;
  }
}

.break-time-input {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.break-time-label {
  font-size: 0.75rem;
  color: #6b7280;
  font-weight: 500;
}

.remove-break-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.625rem;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.375rem;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
  height: fit-content;
  flex-shrink: 0;
}

.remove-break-btn:hover {
  background: #fee2e2;
  border-color: #ef4444;
  color: #ef4444;
}

.no-breaks-message {
  font-size: 0.8125rem;
  color: #9ca3af;
  text-align: center;
  padding: 1rem;
  margin: 0;
}
</style>
