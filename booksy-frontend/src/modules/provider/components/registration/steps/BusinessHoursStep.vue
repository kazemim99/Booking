<template>
  <StepContainer
    :title="$t('provider.registration.hours.title')"
    :subtitle="$t('provider.registration.hours.subtitle')"
  >
    <div class="hours-list">
      <div
        v-for="day in businessHours"
        :key="day.dayOfWeek"
        class="day-row"
        :class="{ 'day-closed': !day.isOpen }"
        @click="day.isOpen && openDayModal(day)"
      >
        <label class="toggle-container" @click.stop>
          <input
            v-model="day.isOpen"
            type="checkbox"
            class="toggle-input"
          />
          <span class="toggle-slider"></span>
        </label>
        <span class="day-name">{{ getDayName(day.dayOfWeek) }}</span>
        <span class="day-hours">
          <template v-if="day.isOpen">
            {{ formatTime(day.openTime) }} - {{ formatTime(day.closeTime) }}
            <span v-if="day.breaks && day.breaks.length > 0" class="breaks-indicator">
              ({{ day.breaks.length }} break{{ day.breaks.length > 1 ? 's' : '' }})
            </span>
          </template>
          <template v-else>
            Closed
          </template>
        </span>
        <svg
          v-if="day.isOpen"
          class="edit-icon"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </div>
    </div>

    <p class="note">{{ $t('provider.registration.hours.note') }}</p>

    <NavigationButtons
      :show-back="true"
      :can-continue="hasAtLeastOneOpenDay"
      @back="$emit('back')"
      @next="handleNext"
    />

    <!-- Day Hours Modal -->
    <Transition name="modal">
      <div v-if="showModal" class="modal-overlay" @click="closeModal">
        <div class="modal-content" @click.stop>
          <div class="modal-header">
            <button class="back-button" @click="closeModal">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
              </svg>
            </button>
            <h3 class="modal-title">{{ getDayName(editingDay?.dayOfWeek || 0) }}</h3>
          </div>

          <p class="modal-description">
            Set your business hours here. Head to your calendar if you need to adjust hours for a single day.
          </p>

          <div class="hours-section">
            <label class="hours-label">Opening hours</label>
            <div class="time-inputs">
              <select v-model="tempOpenTime" class="time-select">
                <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                  {{ time.label }}
                </option>
              </select>
              <select v-model="tempCloseTime" class="time-select">
                <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                  {{ time.label }}
                </option>
              </select>
            </div>
          </div>

          <!-- Breaks Section -->
          <div v-if="tempBreaks.length > 0" class="breaks-section">
            <div v-for="(breakTime, index) in tempBreaks" :key="index" class="break-item">
              <label class="hours-label">Break {{ index + 1 }}</label>
              <div class="time-inputs">
                <select v-model="breakTime.start" class="time-select">
                  <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                    {{ time.label }}
                  </option>
                </select>
                <select v-model="breakTime.end" class="time-select">
                  <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                    {{ time.label }}
                  </option>
                </select>
                <button class="delete-break-btn" @click="removeBreak(index)">
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </div>
          </div>

          <button class="add-break-btn" @click="addBreak">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Add Break
          </button>

          <div class="modal-actions">
            <button class="btn-cancel" @click="closeModal">CANCEL</button>
            <button class="btn-save" @click="saveDayHours">SAVE</button>
          </div>
        </div>
      </div>
    </Transition>
  </StepContainer>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import StepContainer from '../shared/StepContainer.vue'
import NavigationButtons from '../shared/NavigationButtons.vue'
import type { DayHours, TimeSlot } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: DayHours[]
}

interface Emits {
  (e: 'update:modelValue', value: DayHours[]): void
  (e: 'next'): void
  (e: 'back'): void
}

interface TimeOption {
  value: string
  label: string
}

interface BreakTime {
  start: string
  end: string
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Initialize business hours with proper default values
const initializeHours = (): DayHours[] => {
  if (props.modelValue) {
    // If we have existing data, ensure all open days have times set
    return props.modelValue.map(day => ({
      ...day,
      openTime: day.isOpen && !day.openTime ? { hours: 10, minutes: 0 } : day.openTime,
      closeTime: day.isOpen && !day.closeTime ? { hours: 19, minutes: 0 } : day.closeTime,
      breaks: day.breaks || []
    }))
  }

  // Default initialization: Monday-Friday open
  return Array.from({ length: 7 }, (_, i) => ({
    dayOfWeek: i,
    isOpen: i >= 1 && i <= 5, // Monday-Friday open by default
    openTime: i >= 1 && i <= 5 ? { hours: 10, minutes: 0 } : null,
    closeTime: i >= 1 && i <= 5 ? { hours: 19, minutes: 0 } : null,
    breaks: [],
  }))
}

const businessHours = ref<DayHours[]>(initializeHours())

const showModal = ref(false)
const editingDay = ref<DayHours | null>(null)
const tempOpenTime = ref('10:00 AM')
const tempCloseTime = ref('7:00 PM')
const tempBreaks = ref<BreakTime[]>([])

// Generate time options (30-minute intervals)
const timeOptions = computed<TimeOption[]>(() => {
  const options: TimeOption[] = []
  for (let hour = 0; hour < 24; hour++) {
    for (let minute = 0; minute < 60; minute += 30) {
      const period = hour >= 12 ? 'PM' : 'AM'
      const displayHour = hour === 0 ? 12 : hour > 12 ? hour - 12 : hour
      const timeValue = `${displayHour}:${minute.toString().padStart(2, '0')} ${period}`
      options.push({
        value: timeValue,
        label: timeValue
      })
    }
  }
  return options
})

const getDayName = (dayOfWeek: number): string => {
  const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
  return days[dayOfWeek]
}

const formatTime = (time: TimeSlot | null): string => {
  if (!time) return '10:00 AM'
  const period = time.hours >= 12 ? 'PM' : 'AM'
  const displayHour = time.hours === 0 ? 12 : time.hours > 12 ? time.hours - 12 : time.hours
  return `${displayHour}:${time.minutes.toString().padStart(2, '0')} ${period}`
}

const parseTime = (timeStr: string): TimeSlot => {
  const [time, period] = timeStr.split(' ')
  const [hours, minutes] = time.split(':').map(Number)
  let actualHours = hours
  if (period === 'PM' && hours !== 12) {
    actualHours = hours + 12
  } else if (period === 'AM' && hours === 12) {
    actualHours = 0
  }
  return { hours: actualHours, minutes }
}

const generateId = (): string => {
  return `break_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
}

const openDayModal = (day: DayHours) => {
  editingDay.value = day
  tempOpenTime.value = formatTime(day.openTime)
  tempCloseTime.value = formatTime(day.closeTime)
  tempBreaks.value = day.breaks?.map(b => ({
    start: formatTime(b.start),
    end: formatTime(b.end)
  })) || []
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
  editingDay.value = null
}

const addBreak = () => {
  tempBreaks.value.push({
    start: '12:00 PM',
    end: '1:00 PM'
  })
}

const removeBreak = (index: number) => {
  tempBreaks.value.splice(index, 1)
}

const saveDayHours = () => {
  if (editingDay.value) {
    editingDay.value.openTime = parseTime(tempOpenTime.value)
    editingDay.value.closeTime = parseTime(tempCloseTime.value)
    editingDay.value.breaks = tempBreaks.value.map(b => ({
      id: generateId(),
      start: parseTime(b.start),
      end: parseTime(b.end)
    }))
  }
  closeModal()
}

const hasAtLeastOneOpenDay = computed(() => businessHours.value.some(day => day.isOpen))

// Watch for changes in day open/close state and initialize default times
watch(businessHours, (newHours) => {
  newHours.forEach((day) => {
    // If a day is toggled open but has no times set, initialize with defaults
    if (day.isOpen && (!day.openTime || !day.closeTime)) {
      day.openTime = { hours: 10, minutes: 0 }
      day.closeTime = { hours: 19, minutes: 0 }
      if (!day.breaks) {
        day.breaks = []
      }
    }
  })
}, { deep: true })

const handleNext = () => {
  emit('update:modelValue', businessHours.value)
  emit('next')
}
</script>

<style scoped>
.hours-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 2rem;
}

.day-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s;
}

.day-row:hover:not(.day-closed) {
  border-color: #10b981;
  box-shadow: 0 2px 4px rgba(16, 185, 129, 0.1);
}

.day-row.day-closed {
  opacity: 0.6;
  cursor: default;
}

.toggle-container {
  position: relative;
  display: inline-block;
  width: 3rem;
  height: 1.75rem;
}

.toggle-input {
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
  background-color: #d1d5db;
  transition: 0.3s;
  border-radius: 9999px;
}

.toggle-slider:before {
  position: absolute;
  content: "";
  height: 1.25rem;
  width: 1.25rem;
  left: 0.25rem;
  bottom: 0.25rem;
  background-color: white;
  transition: 0.3s;
  border-radius: 50%;
}

.toggle-input:checked + .toggle-slider {
  background-color: #10b981;
}

.toggle-input:checked + .toggle-slider:before {
  transform: translateX(1.25rem);
}

.day-name {
  flex: 0 0 7rem;
  font-weight: 600;
  color: #111827;
}

.day-hours {
  flex: 1;
  color: #6b7280;
  font-size: 0.875rem;
}

.breaks-indicator {
  font-size: 0.75rem;
  color: #10b981;
  margin-left: 0.5rem;
}

.edit-icon {
  width: 1.25rem;
  height: 1.25rem;
  color: #9ca3af;
}

.note {
  font-size: 0.875rem;
  color: #6b7280;
  text-align: center;
  font-style: italic;
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  background: white;
  border-radius: 1rem;
  width: 100%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
  padding: 1.5rem;
}

.modal-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.back-button {
  background: none;
  border: none;
  cursor: pointer;
  padding: 0.5rem;
  display: flex;
  align-items: center;
  color: #6b7280;
}

.back-button:hover {
  color: #111827;
}

.back-button svg {
  width: 1.5rem;
  height: 1.5rem;
}

.modal-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin: 0;
}

.modal-description {
  color: #6b7280;
  font-size: 0.875rem;
  margin-bottom: 1.5rem;
}

.hours-section,
.breaks-section {
  margin-bottom: 1.5rem;
}

.hours-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.5rem;
}

.time-inputs {
  display: grid;
  grid-template-columns: 1fr 1fr auto;
  gap: 0.75rem;
  align-items: center;
}

.time-select {
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  background: white;
  cursor: pointer;
  appearance: none;
  background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
  background-position: right 0.5rem center;
  background-repeat: no-repeat;
  background-size: 1.5em 1.5em;
  padding-right: 2.5rem;
}

.time-select:focus {
  outline: none;
  border-color: #10b981;
  ring: 2px;
  ring-color: rgba(16, 185, 129, 0.2);
}

.break-item {
  margin-bottom: 1rem;
}

.delete-break-btn {
  background: #fee2e2;
  border: none;
  border-radius: 0.5rem;
  padding: 0.75rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #dc2626;
  transition: all 0.2s;
}

.delete-break-btn:hover {
  background: #fecaca;
}

.delete-break-btn svg {
  width: 1.25rem;
  height: 1.25rem;
}

.add-break-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: none;
  border: 1px dashed #d1d5db;
  border-radius: 0.5rem;
  color: #6b7280;
  cursor: pointer;
  width: 100%;
  justify-content: center;
  font-size: 0.875rem;
  margin-bottom: 1.5rem;
  transition: all 0.2s;
}

.add-break-btn:hover {
  border-color: #10b981;
  color: #10b981;
}

.add-break-btn svg {
  width: 1.25rem;
  height: 1.25rem;
}

.modal-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
}

.btn-cancel,
.btn-save {
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  font-weight: 600;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
}

.btn-cancel {
  background: transparent;
  color: #6b7280;
}

.btn-cancel:hover {
  background: #f3f4f6;
}

.btn-save {
  background: #111827;
  color: white;
}

.btn-save:hover {
  background: #1f2937;
}

/* Modal Transition */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.3s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-active .modal-content,
.modal-leave-active .modal-content {
  transition: transform 0.3s ease;
}

.modal-enter-from .modal-content,
.modal-leave-to .modal-content {
  transform: scale(0.95);
}
</style>
