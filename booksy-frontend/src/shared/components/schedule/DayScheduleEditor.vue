<template>
  <div class="day-schedule-editor">
    <div
      v-for="(day, index) in localSchedule"
      :key="index"
      :class="['day-card', { 'day-disabled': !day.isOpen }]"
    >
      <div class="day-header">
        <div class="day-info">
          <div class="day-name-section">
            <span class="day-name">{{ weekDays[index] }}</span>
            <label class="toggle-switch">
              <input type="checkbox" v-model="day.isOpen" @change="handleToggleDay(index)" />
              <span class="toggle-slider"></span>
            </label>
          </div>
          <span v-if="!day.isOpen" class="closed-badge">تعطیل</span>
          <span v-else-if="day.startTime && day.endTime" class="time-info">
            {{ day.startTime }} - {{ day.endTime }}
            <span v-if="day.breaks && day.breaks.length > 0" class="breaks-count">
              ({{ day.breaks.length }} استراحت)
            </span>
          </span>
          <span v-else class="time-info text-muted">
            در حال بارگذاری...
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
            v-if="day.isOpen"
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

    <!-- Copy to Days Modal -->
    <div v-if="copyModalOpen" class="modal-overlay" @click.self="closeCopyModal">
      <div class="modal-container">
        <div class="modal-header">
          <h3 class="modal-title">کپی به روزهای دیگر</h3>
          <button type="button" class="close-btn" @click="closeCopyModal">
            <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div class="modal-body">
          <p class="copy-description">انتخاب کنید کدام روزها را می‌خواهید با {{ copySourceDayName }} یکسان کنید:</p>
          <div class="days-selection">
            <label
              v-for="(day, index) in weekDays"
              :key="index"
              :class="['day-checkbox', { disabled: index === copySourceIndex }]"
            >
              <input
                type="checkbox"
                v-model="selectedDaysToCopy"
                :value="index"
                :disabled="index === copySourceIndex"
              />
              <span class="checkbox-label">
                {{ day }}
                <span v-if="!localSchedule[index].isOpen" class="closed-tag">تعطیل</span>
              </span>
            </label>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn-cancel" @click="closeCopyModal">
            انصراف
          </button>
          <button type="button" class="btn-save" @click="handleCopyConfirm">
            اعمال تغییرات
          </button>
        </div>
      </div>
    </div>
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

const handleToggleDay = (index: number) => {
  const day = localSchedule.value[index]

  // If toggling from closed to open, set default values
  if (day.isOpen) {
    // Always set default values when opening a day if they're missing or invalid
    if (!day.startTime || !day.endTime || day.startTime === '' || day.endTime === '') {
      day.startTime = '10:00'
      day.endTime = '22:00'
    }

    // Add default break from 14:00 to 17:00 if no breaks exist
    if (!day.breaks || day.breaks.length === 0) {
      day.breaks = [
        {
          id: '1',
          start: '14:00',
          end: '17:00',
        }
      ]
    }
  } else {
    // When toggling to closed, clear the times and breaks (optional)
    // This prevents showing "NaN" or invalid times
    day.startTime = ''
    day.endTime = ''
    day.breaks = []
  }

  emitUpdate()
}

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

// Copy modal state
const copyModalOpen = ref(false)
const copySourceIndex = ref<number | null>(null)
const copySourceDayName = ref('')
const selectedDaysToCopy = ref<number[]>([])

const copyToAll = (sourceIndex: number) => {
  copySourceIndex.value = sourceIndex
  copySourceDayName.value = props.weekDays[sourceIndex]
  // Pre-select only open days except the source day (closed days are unchecked by default)
  selectedDaysToCopy.value = props.weekDays
    .map((_, index) => index)
    .filter(index => index !== sourceIndex && localSchedule.value[index].isOpen)
  copyModalOpen.value = true
}

const closeCopyModal = () => {
  copyModalOpen.value = false
  copySourceIndex.value = null
  selectedDaysToCopy.value = []
}

const handleCopyConfirm = () => {
  if (copySourceIndex.value !== null) {
    const sourceDay = localSchedule.value[copySourceIndex.value]
    selectedDaysToCopy.value.forEach(dayIndex => {
      localSchedule.value[dayIndex] = {
        isOpen: sourceDay.isOpen,
        startTime: sourceDay.startTime,
        endTime: sourceDay.endTime,
        breaks: sourceDay.breaks ? JSON.parse(JSON.stringify(sourceDay.breaks)) : [],
      }
    })
    emitUpdate()
  }
  closeCopyModal()
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

.day-name-section {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-shrink: 0;
}

.day-name {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
  min-width: 4rem;
  flex-shrink: 0;
}

/* Toggle Switch */
.toggle-switch {
  position: relative;
  display: inline-block;
  width: 2.5rem;
  height: 1.5rem;
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
  border-radius: 1.5rem;
}

.toggle-slider:before {
  position: absolute;
  content: '';
  height: 1.125rem;
  width: 1.125rem;
  left: 0.1875rem;
  bottom: 0.1875rem;
  background-color: white;
  transition: 0.3s;
  border-radius: 50%;
}

.toggle-switch input:checked + .toggle-slider {
  background-color: #8b5cf6;
}

.toggle-switch input:checked + .toggle-slider:before {
  transform: translateX(1rem);
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

.text-muted {
  opacity: 0.6;
  font-style: italic;
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

/* Copy Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 1rem;
}

.modal-container {
  background: white;
  border-radius: 1rem;
  width: 100%;
  max-width: 500px;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: #111827;
  margin: 0;
}

.close-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  border: none;
  background: transparent;
  color: #6b7280;
  cursor: pointer;
  border-radius: 0.375rem;
  transition: all 0.2s;
}

.close-btn:hover {
  background: #f3f4f6;
  color: #111827;
}

.modal-body {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem;
}

.copy-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin-bottom: 1.5rem;
}

.days-selection {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.day-checkbox {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s;
}

.day-checkbox:hover:not(.disabled) {
  background: #f3f4f6;
  border-color: #8b5cf6;
}

.day-checkbox.disabled {
  opacity: 0.5;
  cursor: not-allowed;
  background: #f9fafb;
}

.day-checkbox input[type="checkbox"] {
  width: 1.25rem;
  height: 1.25rem;
  cursor: pointer;
  accent-color: #8b5cf6;
}

.day-checkbox.disabled input[type="checkbox"] {
  cursor: not-allowed;
}

.checkbox-label {
  font-size: 1rem;
  font-weight: 500;
  color: #111827;
  flex: 1;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.closed-tag {
  font-size: 0.75rem;
  color: #6b7280;
  background: #f3f4f6;
  padding: 0.125rem 0.5rem;
  border-radius: 999px;
}

.modal-footer {
  display: flex;
  gap: 0.75rem;
  padding: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.btn-cancel,
.btn-save {
  flex: 1;
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
}

.btn-cancel {
  background: white;
  color: #374151;
  border: 1px solid #d1d5db;
}

.btn-cancel:hover {
  background: #f9fafb;
}

.btn-save {
  background: #8b5cf6;
  color: white;
}

.btn-save:hover {
  background: #7c3aed;
}
</style>
