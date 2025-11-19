<template>
  <div v-if="isOpen" class="modal-overlay" @click.self="close">
    <div class="modal-container">
      <div class="modal-header">
        <h2 class="modal-title">{{ modalTitle }}</h2>
        <button type="button" class="close-btn" @click="close" :aria-label="t('provider.businessHours.modal.close')">
          ×
        </button>
      </div>

      <div class="modal-body">
        <!-- Schedule Configuration -->
        <div class="mode-content">
          <!-- Day Status Toggle -->
          <div class="status-toggle">
            <label class="toggle-label">
              <span class="toggle-text">
                {{ localSchedule.isOpen ? t('provider.businessHours.dayStatus.open') : t('provider.businessHours.dayStatus.closed') }}
              </span>
              <input
                type="checkbox"
                v-model="localSchedule.isOpen"
                class="toggle-input"
              />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <!-- Hours Section -->
          <div v-if="localSchedule.isOpen" class="hours-section">
            <h3 class="section-title">{{ t('provider.businessHours.modal.workingHours') }}</h3>
            <div class="time-input-group">
              <div class="time-field">
                <label>{{ t('provider.businessHours.listView.openingLabel') }}</label>
                <TimePicker v-model="localSchedule.openTime" />
              </div>
              <span class="time-separator">—</span>
              <div class="time-field">
                <label>{{ t('provider.businessHours.listView.closingLabel') }}</label>
                <TimePicker v-model="localSchedule.closeTime" />
              </div>
            </div>
          </div>

          <!-- Breaks Section -->
          <div v-if="localSchedule.isOpen" class="breaks-section">
            <div class="breaks-header">
              <h3 class="section-title">{{ t('provider.businessHours.listView.breaksLabel') }}</h3>
              <button
                type="button"
                class="add-break-btn"
                @click="addBreak"
                :disabled="localSchedule.breaks.length >= 3"
              >
                {{ t('provider.businessHours.listView.addBreak') }}
              </button>
            </div>

            <div v-if="localSchedule.breaks.length === 0" class="no-breaks">
              {{ t('provider.businessHours.listView.noBreaks') }}
            </div>

            <div v-else class="breaks-list">
              <div
                v-for="(breakItem, index) in localSchedule.breaks"
                :key="index"
                class="break-item"
              >
                <div class="time-input-group">
                  <div class="time-field">
                    <label>{{ t('provider.businessHours.breaks.from') }}</label>
                    <TimePicker v-model="breakItem.startTime" />
                  </div>
                  <span class="time-separator">—</span>
                  <div class="time-field">
                    <label>{{ t('provider.businessHours.breaks.to') }}</label>
                    <TimePicker v-model="breakItem.endTime" />
                  </div>
                  <button
                    type="button"
                    class="remove-btn"
                    @click="removeBreak(index)"
                    :title="t('provider.businessHours.breaks.remove')"
                  >
                    ×
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Closed Day Options (Holiday/Closure Details) -->
          <div v-if="!localSchedule.isOpen && selectedDate" class="holiday-section">
            <h3 class="section-title">{{ t('provider.hours.holidays.title') }}</h3>

            <div class="form-group">
              <label class="form-label required">{{ t('provider.hours.holidays.reason') }}</label>
              <input
                v-model="holidayData.reason"
                type="text"
                class="form-control"
                :placeholder="t('provider.hours.holidays.reasonPlaceholder')"
                maxlength="200"
              />
              <small class="form-text">{{ formatNumber(holidayData.reason.length) }}/{{ formatNumber(200) }}</small>
            </div>

            <div class="form-group">
              <div class="form-check">
                <input
                  id="recurring-check"
                  v-model="holidayData.isRecurring"
                  type="checkbox"
                  class="form-check-input"
                />
                <label for="recurring-check" class="form-check-label">
                  {{ t('provider.hours.holidays.makeRecurring') }}
                </label>
              </div>
              <small class="form-text">{{ t('provider.hours.holidays.recurringHelp') }}</small>
            </div>

            <div v-if="holidayData.isRecurring" class="form-group">
              <label class="form-label required">{{ t('provider.hours.holidays.recurrencePattern') }}</label>
              <select v-model="holidayData.recurrencePattern" class="form-select">
                <option value="Yearly">{{ t('provider.hours.holidays.patterns.yearly') }}</option>
                <option value="Monthly">{{ t('provider.hours.holidays.patterns.monthly') }}</option>
                <option value="Weekly">{{ t('provider.hours.holidays.patterns.weekly') }}</option>
              </select>
            </div>

            <div class="info-box holiday-info">
              <i class="bi bi-info-circle"></i>
              <span>{{ t('provider.hours.holidays.infoText') }}</span>
            </div>
          </div>
        </div>

        <!-- Error Message -->
        <div v-if="errorMessage" class="alert alert-danger">
          <i class="bi bi-exclamation-triangle"></i>
          {{ errorMessage }}
        </div>
      </div>

      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" @click="close" :disabled="isSaving">
          {{ t('provider.businessHours.modal.cancel') }}
        </button>
        <button type="button" class="btn btn-primary" @click="save" :disabled="isSaving || !isValid">
          <span v-if="isSaving" class="spinner"></span>
          {{ isSaving ? t('provider.businessHours.messages.saving') : t('provider.businessHours.modal.save') }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useHoursStore } from '@/modules/provider/stores/hours.store'
import type { BusinessHoursWithBreaks } from '@/modules/provider/types/hours.types'
import { RecurrencePattern } from '@/modules/provider/types/hours.types'
import { formatTimeDisplay, toPersianNumber, formatMonthYear, getDayName, formatDayNumber } from '@/modules/provider/utils/dateHelpers'
import TimePicker from './TimePicker.vue'

const { t, locale } = useI18n()
const hoursStore = useHoursStore()

// Props
interface Props {
  isOpen: boolean
  daySchedule: BusinessHoursWithBreaks | null
  dayLabel?: string
  selectedDate?: string // YYYY-MM-DD format
  providerId?: string
  isSaving?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  isSaving: false
})

// Emits
const emit = defineEmits<{
  close: []
  save: [schedule: BusinessHoursWithBreaks]
}>()

// State
const errorMessage = ref('')

const localSchedule = ref<BusinessHoursWithBreaks>({
  dayOfWeek: 0,
  isOpen: false,
  openTime: '09:00',
  closeTime: '17:00',
  breaks: []
})

const holidayData = ref({
  reason: '',
  isRecurring: false,
  recurrencePattern: RecurrencePattern.Yearly
})

// Computed
const modalTitle = computed(() => {
  if (props.selectedDate) {
    const date = new Date(props.selectedDate)

    if (locale.value === 'fa') {
      // Use Jalali calendar for Persian locale
      const dayName = getDayName(date, 'fa')
      const dayNumber = formatDayNumber(date, 'fa')
      const monthYear = formatMonthYear(date, 'fa')
      return `${dayName}، ${dayNumber} ${monthYear}`
    }

    return date.toLocaleDateString(undefined, {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric'
    })
  }
  return t('provider.businessHours.modal.title', { day: props.dayLabel })
})

const existingHoliday = computed(() => {
  if (!props.selectedDate) return null
  return hoursStore.state.holidays.find(h => h.date === props.selectedDate)
})

const existingException = computed(() => {
  if (!props.selectedDate) return null
  return hoursStore.state.exceptions.find(e => e.date === props.selectedDate)
})

const isValid = computed(() => {
  // For specific dates in Open mode, validate time ranges
  if (props.selectedDate && localSchedule.value.isOpen) {
    return localSchedule.value.openTime < localSchedule.value.closeTime
  }
  // For closed days with specific date, require reason
  if (props.selectedDate && !localSchedule.value.isOpen) {
    return holidayData.value.reason.trim().length > 0
  }
  // For weekly schedule, always valid
  return true
})

// Watch for changes
watch(() => props.isOpen, (isOpen) => {
  if (isOpen) {
    initializeModal()
  }
})

watch(() => props.daySchedule, (newSchedule) => {
  // Only apply base schedule if there's no existing exception/holiday
  // This prevents overwriting the state set by initializeModal()
  if (newSchedule && !existingHoliday.value && !existingException.value) {
    localSchedule.value = {
      dayOfWeek: newSchedule.dayOfWeek,
      isOpen: newSchedule.isOpen,
      openTime: newSchedule.openTime || '09:00',
      closeTime: newSchedule.closeTime || '17:00',
      breaks: newSchedule.breaks ? [...newSchedule.breaks] : []
    }
  }
}, { immediate: true })

// Time options
const timeOptions = computed(() => {
  const options = []
  for (let i = 0; i < 24; i++) {
    const hour = i.toString().padStart(2, '0')
    options.push({
      value: `${hour}:00`,
      label: formatTimeDisplay(`${hour}:00`, locale.value)
    })
    options.push({
      value: `${hour}:30`,
      label: formatTimeDisplay(`${hour}:30`, locale.value)
    })
  }
  return options
})

// Methods
function initializeModal() {
  errorMessage.value = ''

  // Reset holiday data first
  holidayData.value = { reason: '', isRecurring: false, recurrencePattern: RecurrencePattern.Yearly }

  // Check if there's an existing holiday
  if (existingHoliday.value) {
    // Holiday exists - set to closed and load holiday data
    localSchedule.value.isOpen = false
    holidayData.value = {
      reason: existingHoliday.value.reason,
      isRecurring: existingHoliday.value.isRecurring,
      recurrencePattern: existingHoliday.value.recurrencePattern || RecurrencePattern.Yearly
    }
  } else if (existingException.value) {
    // Exception exists - check if it's closed or open
    if (existingException.value.isClosed) {
      // Closed exception - set toggle to closed and load reason
      localSchedule.value.isOpen = false
      holidayData.value.reason = existingException.value.reason || ''
      holidayData.value.isRecurring = false
    } else {
      // Open exception - set toggle to open and load hours
      localSchedule.value.isOpen = true
      localSchedule.value.openTime = existingException.value.openTime || '09:00'
      localSchedule.value.closeTime = existingException.value.closeTime || '17:00'
    }
  } else {
    // No existing data - use base hours from daySchedule prop
    // This will be set by the watch on props.daySchedule
  }
}


function formatNumber(num: number): string {
  if (locale.value === 'fa') {
    return toPersianNumber(num)
  }
  return String(num)
}

function addBreak() {
  if (localSchedule.value.breaks.length < 3) {
    localSchedule.value.breaks.push({
      startTime: '12:00',
      endTime: '13:00'
    })
  }
}

function removeBreak(index: number) {
  localSchedule.value.breaks.splice(index, 1)
}

function close() {
  emit('close')
}

async function save() {
  errorMessage.value = ''

  try {
    // If we have a specific date, save as holiday or exception
    if (props.selectedDate && props.providerId) {
      // Always delete existing holiday/exception first
      if (existingException.value?.id) {
        await hoursStore.removeException(props.providerId, existingException.value.id)
      }
      if (existingHoliday.value?.id) {
        await hoursStore.removeHoliday(props.providerId, existingHoliday.value.id)
      }

      if (!localSchedule.value.isOpen) {
        // Toggle is CLOSED - Add holiday or closed exception
        if (holidayData.value.isRecurring) {
          // Recurring closed day - save as holiday
          await hoursStore.addHoliday({
            providerId: props.providerId,
            holiday: {
              date: props.selectedDate,
              reason: holidayData.value.reason,
              isRecurring: true,
              recurrencePattern: holidayData.value.recurrencePattern
            }
          })
        } else {
          // Non-recurring closed day - save as exception with isClosed flag
          await hoursStore.addException({
            providerId: props.providerId,
            exception: {
              date: props.selectedDate,
              openTime: undefined,
              closeTime: undefined,
              reason: holidayData.value.reason,
              isClosed: true
            }
          })
        }
      } else {
        // Toggle is OPEN - Add exception with custom hours (or delete if matches base hours)
        // For now, always add exception when explicitly setting hours for a specific date
        await hoursStore.addException({
          providerId: props.providerId,
          exception: {
            date: props.selectedDate,
            openTime: localSchedule.value.openTime,
            closeTime: localSchedule.value.closeTime,
            reason: `Custom hours for ${modalTitle.value}`,
            isClosed: false
          }
        })
      }

      close()
    } else {
      // No specific date - save as recurring weekly schedule
      emit('save', { ...localSchedule.value })
    }
  } catch (error) {
    console.error('Save error:', error)
    errorMessage.value = error instanceof Error ? error.message : 'Failed to save'
  }
}

</script>

<style scoped>
/* ... (keeping all existing styles from original DayScheduleModal) ... */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-container {
  background: white;
  border-radius: 0.75rem;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
  max-width: 700px;
  width: 100%;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-title {
  font-size: 1.5rem;
  font-weight: 700;
  margin: 0;
  color: #111827;
}

.close-btn {
  width: 2.5rem;
  height: 2.5rem;
  border: none;
  background: transparent;
  font-size: 2rem;
  color: #6b7280;
  cursor: pointer;
  border-radius: 0.375rem;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
}

.close-btn:hover {
  background: #f3f4f6;
  color: #111827;
}

.modal-body {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Mode Selector */
.mode-selector {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.5rem;
  padding: 0.5rem;
  background: #f9fafb;
  border-radius: 0.5rem;
}

.mode-tab {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem;
  border: 2px solid transparent;
  background: white;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 0.875rem;
  font-weight: 500;
  color: #6b7280;
}

.mode-tab i {
  font-size: 1.5rem;
}

.mode-tab:hover {
  border-color: #d1d5db;
  color: #111827;
}

.mode-tab.active {
  border-color: #3b82f6;
  background: #eff6ff;
  color: #3b82f6;
}

.mode-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Form Groups */
.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #374151;
}

.form-label.required::after {
  content: ' *';
  color: #ef4444;
}

.form-control,
.form-select {
  padding: 0.625rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.875rem;
}

.form-control:focus,
.form-select:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.form-text {
  font-size: 0.75rem;
  color: #6b7280;
}

.form-check {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.form-check-input {
  width: 1.25rem;
  height: 1.25rem;
  cursor: pointer;
}

.form-check-label {
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
}

/* Info Boxes */
.info-box {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.875rem;
}

.info-box i {
  font-size: 1.25rem;
  flex-shrink: 0;
}

.holiday-info {
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #991b1b;
}

.exception-info {
  background: #fef3c7;
  border: 1px solid #fde68a;
  color: #92400e;
}

/* Status Toggle */
.status-toggle {
  padding: 1rem;
  background: #f9fafb;
  border-radius: 0.5rem;
  border: 1px solid #e5e7eb;
}

.toggle-label {
  display: flex;
  justify-content: space-between;
  align-items: center;
  cursor: pointer;
}

.toggle-text {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
}

.toggle-input {
  display: none;
}

.toggle-slider {
  position: relative;
  display: inline-block;
  width: 3.5rem;
  height: 2rem;
  background-color: #e5e7eb;
  border-radius: 2rem;
  transition: all 0.2s;
}

.toggle-slider:before {
  position: absolute;
  content: "";
  height: 1.5rem;
  width: 1.5rem;
  left: 0.25rem;
  bottom: 0.25rem;
  background-color: white;
  border-radius: 50%;
  transition: all 0.2s;
}

.toggle-input:checked + .toggle-slider {
  background-color: #10b981;
}

.toggle-input:checked + .toggle-slider:before {
  transform: translateX(1.5rem);
}

.hours-section,
.breaks-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.section-title {
  font-size: 1.125rem;
  font-weight: 600;
  margin: 0;
  color: #111827;
}

.time-input-group {
  display: flex;
  align-items: flex-end;
  gap: 0.75rem;
}

.time-field {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.time-field label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #6b7280;
}

.time-select {
  padding: 0.625rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  color: #111827;
  background-color: white;
  cursor: pointer;
  transition: all 0.2s;
}

.time-select:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.time-separator {
  padding-bottom: 0.625rem;
  color: #9ca3af;
  font-weight: 500;
  font-size: 1.125rem;
}

.breaks-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.add-break-btn {
  padding: 0.5rem 1rem;
  border: 1px solid #d1d5db;
  background: white;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: #111827;
  cursor: pointer;
  transition: all 0.2s;
}

.add-break-btn:hover:not(:disabled) {
  border-color: #3b82f6;
  background: #eff6ff;
  color: #3b82f6;
}

.add-break-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.no-breaks {
  text-align: center;
  padding: 1.5rem;
  color: #9ca3af;
  font-style: italic;
  background-color: #f9fafb;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  border: 1px dashed #e5e7eb;
}

.breaks-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.break-item {
  padding: 1rem;
  background-color: #f9fafb;
  border-radius: 0.5rem;
  border: 1px solid #e5e7eb;
}

.remove-btn {
  width: 2.5rem;
  height: 2.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #fca5a5;
  background: #fef2f2;
  border-radius: 0.375rem;
  font-size: 1.75rem;
  color: #ef4444;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;
  align-self: flex-end;
  margin-bottom: 0.125rem;
  line-height: 1;
}

.remove-btn:hover {
  background: #ef4444;
  color: white;
}

.alert {
  padding: 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.alert-danger {
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #991b1b;
}

.modal-footer {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1.5rem;
  border-top: 1px solid #e5e7eb;
  background: #f9fafb;
}

.flex-spacer {
  flex: 1;
}

.btn {
  padding: 0.625rem 1.25rem;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  border: 1px solid transparent;
}

.btn-secondary {
  background: white;
  color: #374151;
  border-color: #d1d5db;
}

.btn-secondary:hover {
  background: #f9fafb;
  border-color: #9ca3af;
}

.btn-primary {
  background: #3b82f6;
  color: white;
  border-color: #3b82f6;
}

.btn-primary:hover {
  background: #2563eb;
  border-color: #2563eb;
}

.btn-danger {
  background: #ef4444;
  color: white;
  border-color: #ef4444;
}

.btn-danger:hover {
  background: #dc2626;
  border-color: #dc2626;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.spinner {
  display: inline-block;
  width: 1rem;
  height: 1rem;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
  margin-right: 0.5rem;
  vertical-align: middle;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Mobile Responsive */
@media (max-width: 768px) {
  .modal-container {
    max-height: 95vh;
  }

  .modal-header,
  .modal-body,
  .modal-footer {
    padding: 1rem;
  }

  .mode-selector {
    grid-template-columns: 1fr;
  }

  .time-input-group {
    flex-direction: column;
    align-items: stretch;
  }

  .time-separator {
    text-align: center;
    padding: 0.25rem 0;
  }

  .remove-btn {
    width: 100%;
    margin-top: 0.5rem;
  }

  .modal-footer {
    flex-wrap: wrap;
  }

  .btn {
    flex: 1;
  }
}
</style>
