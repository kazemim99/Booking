<template>
  <div class="persian-calendar-wrapper" dir="rtl">
    <date-picker
      v-model="internalValue"
      type="date"
      format="YYYY-MM-DD"
      display-format="jYYYY/jMM/jDD"
      :inline="true"
      :auto-submit="false"
      :editable="false"
      :clearable="false"
      :min="minDate"
      color="#8b5cf6"
      @change="handleDateChange"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import DatePicker from 'vue3-persian-datetime-picker'

interface Props {
  modelValue?: Date | null
  customDays?: Map<string, any>
}

interface Emits {
  (e: 'update:modelValue', value: Date | null): void
  (e: 'day-click', date: Date): void
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: null,
  customDays: () => new Map()
})

const emit = defineEmits<Emits>()

const internalValue = ref<string>('')

// Set minimum date to today (disable previous days)
const minDate = (() => {
  const today = new Date()
  const year = today.getFullYear()
  const month = String(today.getMonth() + 1).padStart(2, '0')
  const day = String(today.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
})()

// Watch for external changes
watch(() => props.modelValue, (newValue) => {
  if (newValue) {
    // Convert Date to string format YYYY-MM-DD
    const year = newValue.getFullYear()
    const month = String(newValue.getMonth() + 1).padStart(2, '0')
    const day = String(newValue.getDate()).padStart(2, '0')
    internalValue.value = `${year}-${month}-${day}`
  } else {
    internalValue.value = ''
  }
}, { immediate: true })

const handleDateChange = (value: string) => {
  if (!value) {
    emit('update:modelValue', null)
    return
  }

  // Parse the date string (format: YYYY-MM-DD)
  const date = new Date(value)
  emit('update:modelValue', date)
  emit('day-click', date)
}
</script>

<style lang="scss">
/* Import the default styles from SCSS source */
@import 'vue3-persian-datetime-picker/src/picker/assets/scss/style.scss';

.persian-calendar-wrapper {
  width: 100%;
}

/* Customize the calendar to match our design */
.persian-calendar-wrapper .vpd-input-group {
  display: none; /* Hide input since we're using inline mode */
}

.persian-calendar-wrapper .vpd-container {
  width: 100% !important;
  box-shadow: none !important;
  border: none !important;
}

.persian-calendar-wrapper .vpd-content {
  border-radius: 0.5rem;
}

/* Match our purple theme */
.persian-calendar-wrapper .vpd-day-selected {
  background-color: #8b5cf6 !important;
}

.persian-calendar-wrapper .vpd-day-selected span {
  color: white !important;
}

.persian-calendar-wrapper .vpd-day:hover {
  background-color: rgba(139, 92, 246, 0.1) !important;
}

/* RTL Support */
.persian-calendar-wrapper .vpd-container {
  direction: rtl;
}

/* Make Fridays red (Friday is the 7th column in Persian calendar - rightmost) */
.persian-calendar-wrapper .vpd-column-6 .vpd-day span {
  color: #ef4444 !important;
}

.persian-calendar-wrapper .vpd-column-6 .vpd-day-selected span {
  color: white !important; /* Keep white when selected */
}

.persian-calendar-wrapper .vpd-column-6 .vpd-day.vpd-disabled span {
  color: #fca5a5 !important; /* Lighter red for disabled Fridays */
}
</style>
