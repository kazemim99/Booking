<template>
  <div
    :class="['input-wrapper', { 'input-wrapper-error': error, 'input-wrapper-disabled': disabled }]"
  >
    <label v-if="label" :for="inputId" class="input-label">
      {{ label }}
      <span v-if="required" class="input-required">*</span>
    </label>

    <div class="input-container">
      <span v-if="prefixIcon || $slots.prefix" class="input-prefix">
        <slot name="prefix">
          <span v-if="prefixIcon" v-html="prefixIcon" />
        </slot>
      </span>

      <input
        :id="inputId"
        ref="inputRef"
        :type="type"
        :value="modelValue"
        :placeholder="placeholder"
        :disabled="disabled"
        :readonly="readonly"
        :required="required"
        :maxlength="maxlength"
        :minlength="minlength"
        :autocomplete="autocomplete"
        :class="inputClasses"
        @input="handleInput"
        @blur="handleBlur"
        @focus="handleFocus"
        @keypress="handleKeypress"
      />

      <span v-if="suffixIcon || $slots.suffix || clearable" class="input-suffix">
        <button
          v-if="clearable && modelValue"
          type="button"
          class="input-clear"
          @click="handleClear"
        >
          ✕
        </button>
        <slot name="suffix">
          <span v-if="suffixIcon" v-html="suffixIcon" />
        </slot>
      </span>
    </div>

    <span v-if="error" class="input-error">{{ error }}</span>
    <span v-else-if="hint" class="input-hint">{{ hint }}</span>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, useSlots } from 'vue'

interface Props {
  modelValue?: string | number
  type?: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search'
  label?: string
  placeholder?: string
  hint?: string
  error?: string | null | undefined
  disabled?: boolean
  readonly?: boolean
  required?: boolean
  clearable?: boolean
  prefixIcon?: string
  suffixIcon?: string
  maxlength?: number
  minlength?: number
  autocomplete?: string
  size?: 'small' | 'medium' | 'large'
  autofocus?: boolean
}

defineSlots<{
  prefix?: () => any
  suffix?: () => any
}>()

const props = withDefaults(defineProps<Props>(), {
  type: 'text',
  size: 'medium',
  clearable: false,
  disabled: false,
  readonly: false,
  required: false,
  autofocus: false,
})

interface Emits {
  (e: 'update:modelValue', value: string | number): void
  (e: 'blur', event: FocusEvent): void
  (e: 'focus', event: FocusEvent): void
  (e: 'keypress', event: KeyboardEvent): void
  (e: 'clear'): void
}

const emit = defineEmits<Emits>()

const inputRef = ref<HTMLInputElement | null>(null)
const inputId = computed(() => `input-${Math.random().toString(36).substr(2, 9)}`)

const slots = useSlots()

const inputClasses = computed(() => [
  'input',
  `input-${props.size}`,
  {
    'input-error': props.error,
    'input-with-prefix': props.prefixIcon || !!slots.prefix,
    'input-with-suffix': props.suffixIcon || !!slots.suffix || props.clearable,
  },
])

function handleInput(event: Event) {
  const target = event.target as HTMLInputElement
  emit('update:modelValue', target.value)
}

function handleBlur(event: FocusEvent) {
  emit('blur', event)
}

function handleFocus(event: FocusEvent) {
  emit('focus', event)
}

function handleKeypress(event: KeyboardEvent) {
  emit('keypress', event)
}

function handleClear() {
  emit('update:modelValue', '')
  emit('clear')
  inputRef.value?.focus()
}

function focus() {
  inputRef.value?.focus()
}

onMounted(() => {
  if (props.autofocus) {
    focus()
  }
})

defineExpose({
  focus,
  inputRef,
})
</script>

<style scoped lang="scss">
.input-wrapper {
  margin-bottom: 1rem;

  &:last-child {
    margin-bottom: 0;
  }
}

.input-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.5rem;
}

.input-required {
  color: #ef4444;
  margin-left: 0.25rem;
}

.input-container {
  position: relative;
  display: flex;
  align-items: center;
}

.input {
  width: 100%;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  background: white;
  color: #1f2937;
  transition: all 0.2s;
  outline: none;

  &::placeholder {
    color: #9ca3af;
  }

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

  &:readonly {
    background-color: #f9fafb;
  }

  // Sizes
  &-small {
    padding: 0.5rem 0.75rem;
    font-size: 0.875rem;
  }

  &-medium {
    padding: 0.75rem 1rem;
    font-size: 1rem;
  }

  &-large {
    padding: 1rem 1.25rem;
    font-size: 1.125rem;
  }

  // States
  &-error {
    border-color: #ef4444;

    &:focus {
      border-color: #ef4444;
      box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
    }
  }

  &-with-prefix {
    padding-left: 2.5rem;
  }

  &-with-suffix {
    padding-right: 2.5rem;
  }
}

.input-prefix,
.input-suffix {
  position: absolute;
  display: flex;
  align-items: center;
  color: #6b7280;
  font-size: 1.25rem;
  pointer-events: none;
}

.input-prefix {
  left: 0.75rem;
}

.input-suffix {
  right: 0.75rem;
  pointer-events: auto;
}

.input-clear {
  background: none;
  border: none;
  color: #9ca3af;
  cursor: pointer;
  padding: 0.25rem;
  font-size: 1rem;
  display: flex;
  align-items: center;
  transition: color 0.2s;

  &:hover {
    color: #6b7280;
  }
}

.input-error {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.75rem;
  color: #ef4444;
}

.input-hint {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.75rem;
  color: #6b7280;
}

.input-wrapper-disabled {
  opacity: 0.6;
}
</style>
