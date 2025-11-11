<template>
  <div class="day-schedule-editor">
    <div
      v-for="(day, index) in localSchedule"
      :key="index"
      :class="['day-card', { 'day-disabled': !day.isOpen }]"
    >
      <div class="day-header">
        <div class="day-info">
          <span class="day-name">{{ weekDays[index] }}</span>
          <span v-if="!day.isOpen" class="closed-badge">تعطیل</span>
          <span v-else class="time-info">
            {{ day.startTime }} - {{ day.endTime }}
            <span v-if="day.breaks && day.breaks.length > 0" class="breaks-count">
              ({{ day.breaks.length }} استراحت)
            </span>
          </span>
        </div>
        <div class="day-actions">
          <button
            v-if="showCopyButton && day.isOpen"
            type="button"
            class="action-btn copy-btn"
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
            <span class="btn-text">{{ copyButtonText }}</span>
          </button>
          <button
            type="button"
            class="action-btn edit-btn"
            @click="openEditModal(index)"
          >
            <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
              />
            </svg>
            <span class="btn-text">ویرایش</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Edit Modal -->
    <DayScheduleModal
      :is-open="modalOpen"
      :day-name="editingDayName"
      :day-data="editingDayData"
      :start-time-label="startTimeLabel"
      :end-time-label="endTimeLabel"
      :show-breaks="showBreaks"
      :breaks-label="breaksLabel"
      :break-start-label="breakStartLabel"
      :break-end-label="breakEndLabel"
      :add-break-text="addBreakText"
      :remove-break-label="removeBreakLabel"
      :no-breaks-text="noBreaksText"
      @close="closeEditModal"
      @save="handleDaySave"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import DayScheduleModal from './DayScheduleModal.vue'
import type { DayScheduleData } from './DayScheduleModal.vue'

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

// Modal state
const modalOpen = ref(false)
const editingDayIndex = ref<number | null>(null)
const editingDayName = ref('')
const editingDayData = ref<DayScheduleData>({
  isOpen: false,
  startTime: '',
  endTime: '',
  breaks: [],
})

const openEditModal = (index: number) => {
  editingDayIndex.value = index
  editingDayName.value = props.weekDays[index]
  editingDayData.value = JSON.parse(JSON.stringify(localSchedule.value[index]))
  modalOpen.value = true
}

const closeEditModal = () => {
  modalOpen.value = false
  editingDayIndex.value = null
}

const handleDaySave = (data: DayScheduleData) => {
  if (editingDayIndex.value !== null) {
    localSchedule.value[editingDayIndex.value] = JSON.parse(JSON.stringify(data))
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
  gap: 0.75rem;
}

.day-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.75rem;
  padding: 1rem 1.25rem;
  transition: all 0.2s;
}

.day-card:hover {
  border-color: #d1d5db;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.day-card.day-disabled {
  background: #f9fafb;
}

.day-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.day-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
  min-width: 0;
}

.day-name {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
  min-width: 4rem;
  flex-shrink: 0;
}

.closed-badge {
  font-size: 0.75rem;
  color: #6b7280;
  background: #f3f4f6;
  padding: 0.25rem 0.75rem;
  border-radius: 999px;
  flex-shrink: 0;
}

.time-info {
  font-size: 0.875rem;
  color: #6b7280;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.breaks-count {
  font-size: 0.75rem;
  color: #8b5cf6;
  background: rgba(139, 92, 246, 0.1);
  padding: 0.125rem 0.5rem;
  border-radius: 999px;
}

.day-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-shrink: 0;
}

.action-btn {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.5rem 0.875rem;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  color: #374151;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}

.action-btn:hover {
  background: #f9fafb;
  border-color: #8b5cf6;
  color: #8b5cf6;
}

.edit-btn:hover {
  border-color: #8b5cf6;
  color: #8b5cf6;
}

.copy-btn:hover {
  border-color: #10b981;
  color: #10b981;
}

.btn-icon {
  width: 1rem;
  height: 1rem;
  flex-shrink: 0;
}

.btn-text {
  display: none;
}

@media (min-width: 640px) {
  .btn-text {
    display: inline;
  }
}

@media (max-width: 640px) {
  .day-header {
    flex-direction: column;
    align-items: stretch;
  }

  .day-info {
    flex-direction: column;
    align-items: flex-start;
  }

  .day-actions {
    width: 100%;
  }

  .action-btn {
    flex: 1;
    justify-content: center;
  }
}
</style>
