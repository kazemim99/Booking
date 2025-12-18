<template>
  <div class="persian-date-picker-wrapper" dir="rtl">
    <date-picker
      :model-value="internalValue"
      type="date"
      format="YYYY-MM-DD"
      display-format="jYYYY/jMM/jDD"
      :min="min"
      :max="max"
      :editable="false"
      :clearable="clearable"
      :placeholder="placeholder"
      :disabled="disabled"
      color="#667eea"
      @update:model-value="handleDateChange"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import DatePicker from 'vue3-persian-datetime-picker'

interface Props {
  modelValue?: string | Date
  placeholder?: string
  clearable?: boolean
  disabled?: boolean
  min?: string | Date
  max?: string | Date
}

interface Emits {
  (e: 'update:modelValue', value: string): void
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  placeholder: 'انتخاب تاریخ',
  clearable: false,
  disabled: false
})

const emit = defineEmits<Emits>()

const internalValue = ref<string>('')

// Helper to ensure value is a string in YYYY-MM-DD format
const ensureStringValue = (value: any): string => {
  if (!value) return ''
  if (typeof value === 'string') return value
  if (value instanceof Date) {
    return value.toISOString().split('T')[0]
  }
  if (typeof value === 'object' && value._isAMomentObject) {
    // If it's a moment object, format it as YYYY-MM-DD
    return value.format('YYYY-MM-DD')
  }
  return String(value)
}

// Watch for external changes
watch(() => props.modelValue, (newValue) => {
  internalValue.value = ensureStringValue(newValue)
}, { immediate: true })

const handleDateChange = (value: any) => {
  // Ensure we always emit a string in YYYY-MM-DD format
  const stringValue = ensureStringValue(value)
  emit('update:modelValue', stringValue)
}
</script>

<style lang="scss">
/* Import the default styles from SCSS source */
@import 'vue3-persian-datetime-picker/src/picker/assets/scss/style.scss';

.persian-date-picker-wrapper {
  width: 100%;
}

.persian-date-picker-wrapper .vpd-input-group {
  width: 100%;
}

.persian-date-picker-wrapper .vpd-input-group input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 0.875rem;
  transition: all 0.2s;
  text-align: center;
  font-family: inherit;
  background: white;
}

.persian-date-picker-wrapper .vpd-input-group input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.persian-date-picker-wrapper .vpd-input-group input:disabled {
  background: #f3f4f6;
  cursor: not-allowed;
  opacity: 0.6;
}

.persian-date-picker-wrapper .vpd-input-group input::placeholder {
  color: #9ca3af;
}

/* Hide the icon button */
.persian-date-picker-wrapper .vpd-icon-btn {
  display: none;
}

/* Match our purple theme */
.persian-date-picker-wrapper .vpd-day.vpd-selected {
  background-color: #667eea !important;
  color: white !important;
}

.persian-date-picker-wrapper .vpd-day:hover:not(.vpd-selected):not(.vpd-disabled) {
  background-color: rgba(102, 126, 234, 0.1) !important;
}

.persian-date-picker-wrapper .vpd-day.vpd-today {
  color: #667eea;
  font-weight: 600;
}

/* Disabled dates */
.persian-date-picker-wrapper .vpd-day.vpd-disabled {
  color: #d1d5db;
  cursor: not-allowed;
  text-decoration: line-through;
}

/* Month/Year selectors */
.persian-date-picker-wrapper .vpd-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.persian-date-picker-wrapper .vpd-header button {
  color: white;
}

.persian-date-picker-wrapper .vpd-header button:hover {
  background-color: rgba(255, 255, 255, 0.2);
}

/* Weekday headers */
.persian-date-picker-wrapper .vpd-week {
  color: #6b7280;
  font-weight: 500;
}

/* RTL Support */
.persian-date-picker-wrapper .vpd-container {
  direction: rtl;
  font-family: inherit;
}

/* Calendar container styling */
.persian-date-picker-wrapper .vpd-body {
  padding: 1rem;
}

/* Actions (Today, Clear buttons) */
.persian-date-picker-wrapper .vpd-actions button {
  color: #667eea;
  font-weight: 500;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  transition: all 0.2s;
}

.persian-date-picker-wrapper .vpd-actions button:hover {
  background-color: rgba(102, 126, 234, 0.1);
}
</style>
