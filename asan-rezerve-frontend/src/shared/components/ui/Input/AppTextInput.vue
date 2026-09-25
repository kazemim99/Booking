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
  font-weight: var(--font-weight-bold); /* Coliride form labels are bold navy */
  color: var(--color-navy);
  margin-bottom: 0.5rem;
}

.input-required {
  color: var(--color-danger-400);
  margin-left: 0.25rem;
}

.input-container {
  position: relative;
  display: flex;
  align-items: center;
}

.input {
  width: 100%;
  font-family: var(--font-family-base);
  border: 1.5px solid var(--color-border-light); /* #ebeef3 */
  border-radius: var(--radius-lg); /* 12px - Coliride field radius */
  background: var(--color-background);
  color: var(--color-text-primary);
  transition: border-color 0.2s, box-shadow 0.2s;
  outline: none;

  &::placeholder {
    color: var(--color-text-tertiary);
  }

  &:hover:not(:disabled) {
    border-color: var(--color-border-dark); /* #c3cad9 */
  }

  &:focus {
    border-color: var(--color-primary-500);
    box-shadow: 0 0 0 3px rgba(55, 119, 191, 0.12);
  }

  &:disabled {
    background-color: var(--color-background-secondary);
    cursor: not-allowed;
    opacity: 0.6;
  }

  &:read-only {
    background-color: var(--color-background-tertiary);
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
    border-color: var(--color-danger-600); /* #e74a3b - Coliride input error border */

    &:focus {
      border-color: var(--color-danger-600);
      box-shadow: 0 0 0 3px rgba(231, 74, 59, 0.12);
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
  color: var(--color-gray-400); /* #d2dbeb - Coliride field icon */
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
  color: var(--color-text-tertiary);
  cursor: pointer;
  padding: 0.25rem;
  font-size: 1rem;
  display: flex;
  align-items: center;
  transition: color 0.2s;

  &:hover {
    color: var(--color-text-secondary);
  }
}

.input-error {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.75rem;
  color: var(--color-danger-500);
}

.input-hint {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.75rem;
  color: var(--color-text-secondary);
}

.input-wrapper-disabled {
  opacity: 0.6;
}
</style>
