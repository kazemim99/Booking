<template>
  <div class="persian-time-picker-wrapper" dir="rtl">
    <date-picker
      v-model="internalValue"
      type="time"
      format="HH:mm"
      display-format="HH:mm"
      :editable="false"
      :clearable="clearable"
      :placeholder="placeholder"
      color="#8b5cf6"
      @change="handleTimeChange"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import DatePicker from 'vue3-persian-datetime-picker'

interface Props {
  modelValue?: string
  placeholder?: string
  clearable?: boolean
}

interface Emits {
  (e: 'update:modelValue', value: string): void
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  placeholder: 'انتخاب زمان',
  clearable: true
})

const emit = defineEmits<Emits>()

const internalValue = ref<string>('')

// Helper to ensure value is a string (handles moment objects)
const ensureStringValue = (value: any): string => {
  if (!value) return ''
  if (typeof value === 'string') return value
  if (typeof value === 'object' && value._isAMomentObject) {
    // If it's a moment object, format it as HH:mm
    return value.format('HH:mm')
  }
  return String(value)
}

// Watch for external changes
watch(() => props.modelValue, (newValue) => {
  internalValue.value = ensureStringValue(newValue)
}, { immediate: true })

const handleTimeChange = (value: any) => {
  // Ensure we always emit a string
  const stringValue = ensureStringValue(value)
  emit('update:modelValue', stringValue)
}
</script>

<style lang="scss">
/* Import the default styles from SCSS source */
@import 'vue3-persian-datetime-picker/src/picker/assets/scss/style.scss';

.persian-time-picker-wrapper {
  width: 100%;
}

.persian-time-picker-wrapper .vpd-input-group {
  width: 100%;
}

.persian-time-picker-wrapper .vpd-input-group input {
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  transition: all 0.2s;
  text-align: center;
  font-family: inherit;
}

.persian-time-picker-wrapper .vpd-input-group input:focus {
  outline: none;
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.persian-time-picker-wrapper .vpd-input-group input::placeholder {
  color: #9ca3af;
}

/* Hide the icon button */
.persian-time-picker-wrapper .vpd-icon-btn {
  display: none;
}

/* Match our purple theme */
.persian-time-picker-wrapper .vpd-time.vpd-selected {
  background-color: #8b5cf6 !important;
  color: white !important;
}

.persian-time-picker-wrapper .vpd-time:hover {
  background-color: rgba(139, 92, 246, 0.1) !important;
}

/* RTL Support */
.persian-time-picker-wrapper .vpd-container {
  direction: rtl;
}
</style>
