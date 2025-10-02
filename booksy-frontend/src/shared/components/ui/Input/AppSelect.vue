<template>
  <div
    :class="[
      'select-wrapper',
      { 'select-wrapper-error': error, 'select-wrapper-disabled': disabled },
    ]"
  >
    <label v-if="label" :for="selectId" class="select-label">
      {{ label }}
      <span v-if="required" class="select-required">*</span>
    </label>

    <div class="select-container">
      <select
        :id="selectId"
        ref="selectRef"
        :value="modelValue"
        :disabled="disabled"
        :required="required"
        :class="selectClasses"
        @change="handleChange"
        @blur="handleBlur"
        @focus="handleFocus"
      >
        <option v-if="placeholder" value="" disabled>{{ placeholder }}</option>
        <option
          v-for="option in options"
          :key="getOptionValue(option)"
          :value="getOptionValue(option)"
          :disabled="option.disabled"
        >
          {{ getOptionLabel(option) }}
        </option>
      </select>
      <span class="select-arrow">▼</span>
    </div>

    <span v-if="error" class="select-error">{{ error }}</span>
    <span v-else-if="hint" class="select-hint">{{ hint }}</span>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

interface SelectOption {
  [key: string]: unknown
  disabled?: boolean
}

interface Props {
  modelValue?: string | number
  options: SelectOption[]
  label?: string
  placeholder?: string
  hint?: string
  error?: string
  disabled?: boolean
  required?: boolean
  labelKey?: string
  valueKey?: string
}

const props = withDefaults(defineProps<Props>(), {
  labelKey: 'label',
  valueKey: 'value',
  disabled: false,
  required: false,
})

interface Emits {
  (e: 'update:modelValue', value: string | number): void
  (e: 'change', value: string | number): void
  (e: 'blur', event: FocusEvent): void
  (e: 'focus', event: FocusEvent): void
}

const emit = defineEmits<Emits>()

const selectRef = ref<HTMLSelectElement | null>(null)
const selectId = computed(() => `select-${Math.random().toString(36).substr(2, 9)}`)

const selectClasses = computed(() => [
  'select',
  {
    'select-error': props.error,
    'select-placeholder': !props.modelValue,
  },
])

function getOptionValue(option: SelectOption): string | number {
  return option[props.valueKey] as string | number
}

function getOptionLabel(option: SelectOption): string {
  return option[props.labelKey] as string
}

function handleChange(event: Event) {
  const target = event.target as HTMLSelectElement
  const value = target.value
  emit('update:modelValue', value)
  emit('change', value)
}

function handleBlur(event: FocusEvent) {
  emit('blur', event)
}

function handleFocus(event: FocusEvent) {
  emit('focus', event)
}

function focus() {
  selectRef.value?.focus()
}

defineExpose({
  focus,
  selectRef,
})
</script>

<style scoped lang="scss">
.select-wrapper {
  margin-bottom: 1.5rem;

  &:last-child {
    margin-bottom: 0;
  }
}

.select-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.5rem;
}

.select-required {
  color: #ef4444;
  margin-left: 0.25rem;
}

.select-container {
  position: relative;
}

.select {
  width: 100%;
  padding: 0.75rem 2.5rem 0.75rem 1rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  background: white;
  color: #1f2937;
  font-size: 1rem;
  transition: all 0.2s;
  outline: none;
  appearance: none;
  cursor: pointer;

  &:hover:not(:disabled) {
    border-color: #9ca3af;
  }

  &:focus {
    border-color: #667eea;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
  }

  &:disabled {
    background-color: #f3f4f6;
    cursor: not-allowed;
    opacity: 0.6;
  }

  &-error {
    border-color: #ef4444;

    &:focus {
      border-color: #ef4444;
      box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
    }
  }

  &-placeholder {
    color: #9ca3af;
  }

  // Remove default arrow in IE
  &::-ms-expand {
    display: none;
  }
}

.select-arrow {
  position: absolute;
  right: 1rem;
  top: 50%;
  transform: translateY(-50%);
  pointer-events: none;
  font-size: 0.75rem;
  color: #6b7280;
  transition: transform 0.2s;
}

.select:focus ~ .select-arrow {
  transform: translateY(-50%) rotate(180deg);
}

.select-error {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.75rem;
  color: #ef4444;
}

.select-hint {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.75rem;
  color: #6b7280;
}

.select-wrapper-disabled {
  opacity: 0.6;
}
</style>
