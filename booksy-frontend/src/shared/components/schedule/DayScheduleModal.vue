<template>
  <div v-if="isOpen" class="modal-overlay" @click.self="handleClose">
    <div class="modal-container">
      <div class="modal-header">
        <h3 class="modal-title">{{ dayName }}</h3>
        <button type="button" class="close-btn" @click="handleClose">
          <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <div class="modal-body">
        <!-- Day Status Toggle -->
        <div class="form-group">
          <label class="toggle-label">
            <span class="toggle-text">روز کاری</span>
            <label class="toggle-switch">
              <input type="checkbox" v-model="localData.isOpen" />
              <span class="toggle-slider"></span>
            </label>
          </label>
        </div>

        <!-- Time Inputs (shown when day is open) -->
        <div v-if="localData.isOpen" class="time-section">
          <div class="time-row">
            <div class="form-group">
              <label class="form-label">{{ startTimeLabel }}</label>
              <PersianTimePicker
                v-model="localData.startTime"
                :placeholder="startTimeLabel"
              />
            </div>
            <div class="form-group">
              <label class="form-label">{{ endTimeLabel }}</label>
              <PersianTimePicker
                v-model="localData.endTime"
                :placeholder="endTimeLabel"
              />
            </div>
          </div>

          <!-- Breaks Section -->
          <div v-if="showBreaks" class="breaks-section">
            <div class="breaks-header">
              <label class="section-label">{{ breaksLabel }}</label>
              <button type="button" class="add-btn" @click="addBreak">
                <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                {{ addBreakText }}
              </button>
            </div>

            <div v-if="localData.breaks && localData.breaks.length > 0" class="breaks-list">
              <div
                v-for="(breakItem, index) in localData.breaks"
                :key="breakItem.id"
                class="break-item"
              >
                <div class="break-number">{{ index + 1 }}</div>
                <div class="break-inputs">
                  <div class="form-group">
                    <label class="form-label-small">{{ breakStartLabel }}</label>
                    <PersianTimePicker
                      v-model="breakItem.start"
                      :placeholder="breakStartLabel"
                    />
                  </div>
                  <div class="form-group">
                    <label class="form-label-small">{{ breakEndLabel }}</label>
                    <PersianTimePicker
                      v-model="breakItem.end"
                      :placeholder="breakEndLabel"
                    />
                  </div>
                </div>
                <button
                  type="button"
                  class="remove-btn"
                  @click="removeBreak(index)"
                  :title="removeBreakLabel"
                >
                  <svg class="btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </div>
            <p v-else class="no-breaks-text">{{ noBreaksText }}</p>
          </div>
        </div>

        <!-- Closed Message -->
        <div v-else class="closed-message">
          <svg class="closed-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p>این روز تعطیل است</p>
        </div>
      </div>

      <div class="modal-footer">
        <button type="button" class="btn-cancel" @click="handleClose">
          انصراف
        </button>
        <button type="button" class="btn-save" @click="handleSave">
          ذخیره تغییرات
        </button>
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

export interface DayScheduleData {
  isOpen: boolean
  startTime: string
  endTime: string
  breaks?: BreakPeriod[]
}

interface Props {
  isOpen: boolean
  dayName: string
  dayData: DayScheduleData
  startTimeLabel?: string
  endTimeLabel?: string
  showBreaks?: boolean
  breaksLabel?: string
  breakStartLabel?: string
  breakEndLabel?: string
  addBreakText?: string
  removeBreakLabel?: string
  noBreaksText?: string
}

interface Emits {
  (e: 'close'): void
  (e: 'save', data: DayScheduleData): void
}

const props = withDefaults(defineProps<Props>(), {
  startTimeLabel: 'ساعت شروع',
  endTimeLabel: 'ساعت پایان',
  showBreaks: false,
  breaksLabel: 'استراحت‌ها',
  breakStartLabel: 'شروع',
  breakEndLabel: 'پایان',
  addBreakText: 'افزودن استراحت',
  removeBreakLabel: 'حذف استراحت',
  noBreaksText: 'استراحتی تعریف نشده است',
})

const emit = defineEmits<Emits>()

// Local copy of data
const localData = ref<DayScheduleData>(JSON.parse(JSON.stringify(props.dayData)))

// Watch for prop changes
watch(() => props.dayData, (newData) => {
  localData.value = JSON.parse(JSON.stringify(newData))
}, { deep: true })

// Watch for modal open/close to reset data
watch(() => props.isOpen, (isOpen) => {
  if (isOpen) {
    localData.value = JSON.parse(JSON.stringify(props.dayData))
  }
})

const addBreak = () => {
  if (!localData.value.breaks) {
    localData.value.breaks = []
  }
  localData.value.breaks.push({
    id: `break-${Date.now()}-${Math.random()}`,
    start: '12:00',
    end: '13:00',
  })
}

const removeBreak = (index: number) => {
  if (localData.value.breaks) {
    localData.value.breaks.splice(index, 1)
  }
}

const handleClose = () => {
  emit('close')
}

const handleSave = () => {
  emit('save', JSON.parse(JSON.stringify(localData.value)))
  emit('close')
}
</script>

<style scoped>
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
  max-width: 600px;
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

.close-btn .icon {
  width: 1.25rem;
  height: 1.25rem;
}

.modal-body {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.form-label-small {
  font-size: 0.75rem;
  font-weight: 500;
  color: #6b7280;
}

.toggle-label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 0.5rem;
  border: 1px solid #e5e7eb;
}

.toggle-text {
  font-size: 1rem;
  font-weight: 500;
  color: #111827;
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

.time-section {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  margin-top: 1.5rem;
}

.time-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

@media (max-width: 640px) {
  .time-row {
    grid-template-columns: 1fr;
  }
}

.breaks-section {
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.breaks-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.section-label {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
}

.add-btn {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.5rem 1rem;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  color: #374151;
  cursor: pointer;
  transition: all 0.2s;
}

.add-btn:hover {
  background: #f9fafb;
  border-color: #8b5cf6;
  color: #8b5cf6;
}

.breaks-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.break-item {
  display: flex;
  gap: 0.75rem;
  align-items: flex-start;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 0.5rem;
  border: 1px solid #e5e7eb;
}

.break-number {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  background: #8b5cf6;
  color: white;
  border-radius: 50%;
  font-size: 0.875rem;
  font-weight: 600;
  flex-shrink: 0;
}

.break-inputs {
  flex: 1;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

@media (max-width: 640px) {
  .break-inputs {
    grid-template-columns: 1fr;
  }
}

.remove-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.375rem;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;
}

.remove-btn:hover {
  background: #fee2e2;
  border-color: #ef4444;
  color: #ef4444;
}

.btn-icon {
  width: 1rem;
  height: 1rem;
}

.no-breaks-text {
  text-align: center;
  color: #9ca3af;
  font-size: 0.875rem;
  padding: 2rem 1rem;
  margin: 0;
}

.closed-message {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  text-align: center;
  color: #6b7280;
}

.closed-icon {
  width: 4rem;
  height: 4rem;
  margin-bottom: 1rem;
  opacity: 0.5;
}

.closed-message p {
  font-size: 1rem;
  margin: 0;
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

.icon {
  width: 1.25rem;
  height: 1.25rem;
}
</style>
