<template>
  <div class="time-picker" :class="{ rtl: locale === 'fa' }">
    <div class="time-picker-display" @click="togglePicker">
      <i class="bi bi-clock"></i>
      <span class="time-text">{{ displayTime }}</span>
      <i class="bi bi-chevron-down"></i>
    </div>

    <Teleport to="body">
      <div v-if="isOpen" class="time-picker-overlay" @click="close">
        <div class="time-picker-popup" @click.stop :class="{ rtl: locale === 'fa' }">
          <div class="time-picker-header">
            <h3>{{ t('provider.businessHours.timePicker.selectTime') }}</h3>
            <button type="button" class="close-btn" @click="close">×</button>
          </div>

          <div class="time-picker-body">
            <div class="time-display-large">
              {{ formatDisplayNumber(selectedHour) }}:{{ formatDisplayNumber(selectedMinute) }}
            </div>

            <div class="time-picker-controls">
              <!-- Hour Selector -->
              <div class="time-column">
                <label class="time-label">{{ t('provider.businessHours.timePicker.hour') }}</label>
                <div class="time-scroll-container">
                  <div
                    v-for="hour in hours"
                    :key="hour"
                    :class="['time-option', { active: selectedHour === hour }]"
                    @click="selectHour(hour)"
                  >
                    {{ formatDisplayNumber(hour) }}
                  </div>
                </div>
              </div>

              <!-- Minute Selector -->
              <div class="time-column">
                <label class="time-label">{{ t('provider.businessHours.timePicker.minute') }}</label>
                <div class="time-scroll-container">
                  <div
                    v-for="minute in minutes"
                    :key="minute"
                    :class="['time-option', { active: selectedMinute === minute }]"
                    @click="selectMinute(minute)"
                  >
                    {{ formatDisplayNumber(minute) }}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="time-picker-footer">
            <button type="button" class="btn btn-secondary" @click="close">
              {{ t('common.cancel') }}
            </button>
            <button type="button" class="btn btn-primary" @click="confirm">
              {{ t('common.save') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { formatTimeDisplay, toPersianNumber } from '@/modules/provider/utils/dateHelpers'

const { t, locale } = useI18n()

// Props
interface Props {
  modelValue: string // HH:mm format
}

const props = defineProps<Props>()

// Emits
const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

// State
const isOpen = ref(false)
const selectedHour = ref(9)
const selectedMinute = ref(0)

// Computed
const hours = computed(() => {
  return Array.from({ length: 24 }, (_, i) => i)
})

const minutes = computed(() => {
  return [0, 15, 30, 45]
})

const displayTime = computed(() => {
  return formatTimeDisplay(props.modelValue, locale.value)
})

// Watch for changes in modelValue to update internal state
watch(() => props.modelValue, (newValue) => {
  parseTime(newValue)
}, { immediate: true })

// Methods
function parseTime(timeString: string) {
  const [hours, minutes] = timeString.split(':').map(Number)
  selectedHour.value = hours
  selectedMinute.value = minutes
}

function togglePicker() {
  isOpen.value = !isOpen.value
}

function close() {
  isOpen.value = false
}

function selectHour(hour: number) {
  selectedHour.value = hour
}

function selectMinute(minute: number) {
  selectedMinute.value = minute
}

function formatDisplayNumber(num: number): string {
  const formatted = String(num).padStart(2, '0')
  if (locale.value === 'fa') {
    return toPersianNumber(parseInt(formatted))
  }
  return formatted
}

function confirm() {
  const hourStr = String(selectedHour.value).padStart(2, '0')
  const minuteStr = String(selectedMinute.value).padStart(2, '0')
  emit('update:modelValue', `${hourStr}:${minuteStr}`)
  close()
}
</script>

<style scoped>
.time-picker {
  position: relative;
  width: 100%;
}

.time-picker-display {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 0.75rem;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s;
}

.time-picker-display:hover {
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.time-picker-display:focus-within {
  border-color: #3b82f6;
  outline: none;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.time-text {
  flex: 1;
  font-size: 0.875rem;
  font-weight: 500;
  color: #111827;
}

.time-picker-display i:first-child {
  color: #6b7280;
}

.time-picker-display i:last-child {
  color: #9ca3af;
  font-size: 0.75rem;
}

/* Overlay */
.time-picker-overlay {
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
  animation: fadeIn 0.2s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

/* Popup */
.time-picker-popup {
  background: white;
  border-radius: 0.75rem;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
  width: 90%;
  max-width: 400px;
  animation: slideUp 0.3s ease-out;
}

@keyframes slideUp {
  from {
    transform: translateY(20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.time-picker-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}

.time-picker-header h3 {
  margin: 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
}

.close-btn {
  background: none;
  border: none;
  font-size: 2rem;
  line-height: 1;
  color: #9ca3af;
  cursor: pointer;
  padding: 0;
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: color 0.2s;
}

.close-btn:hover {
  color: #111827;
}

.time-picker-body {
  padding: 1.5rem;
}

.time-display-large {
  text-align: center;
  font-size: 3rem;
  font-weight: 700;
  color: #3b82f6;
  margin-bottom: 1.5rem;
  font-variant-numeric: tabular-nums;
}

.time-picker-controls {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.time-column {
  display: flex;
  flex-direction: column;
}

.time-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #6b7280;
  text-align: center;
  margin-bottom: 0.5rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.time-scroll-container {
  height: 200px;
  overflow-y: auto;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  padding: 0.25rem;
  background: #f9fafb;
}

.time-scroll-container::-webkit-scrollbar {
  width: 8px;
}

.time-scroll-container::-webkit-scrollbar-track {
  background: #f1f5f9;
  border-radius: 4px;
}

.time-scroll-container::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 4px;
}

.time-scroll-container::-webkit-scrollbar-thumb:hover {
  background: #94a3b8;
}

.time-option {
  padding: 0.75rem;
  text-align: center;
  font-size: 1.125rem;
  font-weight: 500;
  color: #374151;
  cursor: pointer;
  border-radius: 0.375rem;
  transition: all 0.2s;
  margin-bottom: 0.25rem;
  font-variant-numeric: tabular-nums;
}

.time-option:hover {
  background: #e0f2fe;
  color: #0284c7;
}

.time-option.active {
  background: #3b82f6;
  color: white;
  font-weight: 700;
  box-shadow: 0 2px 4px rgba(59, 130, 246, 0.3);
}

.time-picker-footer {
  display: flex;
  gap: 0.75rem;
  padding: 1.25rem 1.5rem;
  border-top: 1px solid #e5e7eb;
  background: #f9fafb;
  border-radius: 0 0 0.75rem 0.75rem;
}

.btn {
  flex: 1;
  padding: 0.625rem 1rem;
  border: none;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-secondary {
  background: white;
  color: #374151;
  border: 1px solid #d1d5db;
}

.btn-secondary:hover {
  background: #f9fafb;
  border-color: #9ca3af;
}

.btn-primary {
  background: #3b82f6;
  color: white;
}

.btn-primary:hover {
  background: #2563eb;
  box-shadow: 0 2px 4px rgba(59, 130, 246, 0.3);
}

/* RTL Support */
.time-picker.rtl .time-picker-display {
  flex-direction: row-reverse;
}

.time-picker-popup.rtl {
  direction: rtl;
}

.time-picker-popup.rtl .time-picker-header {
  flex-direction: row-reverse;
}

/* Mobile Responsive */
@media (max-width: 640px) {
  .time-picker-popup {
    width: 95%;
    max-width: none;
  }

  .time-display-large {
    font-size: 2.5rem;
  }

  .time-scroll-container {
    height: 150px;
  }

  .time-option {
    padding: 0.625rem;
    font-size: 1rem;
  }
}
</style>
