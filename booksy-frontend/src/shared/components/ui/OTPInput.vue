<template>
  <div class="otp-input-wrapper">
    <div class="otp-input-container">
      <input
        v-for="(digit, index) in digits"
        :key="index"
        :ref="(el) => setInputRef(el, index)"
        v-model="digits[index]"
        type="text"
        inputmode="numeric"
        maxlength="1"
        class="otp-digit-input"
        :class="{ 'has-error': error, 'is-filled': digits[index] }"
        @input="handleInput(index, $event)"
        @keydown="handleKeyDown(index, $event)"
        @paste="handlePaste"
        @focus="handleFocus(index)"
      />
    </div>

    <div v-if="error" class="otp-error">
      {{ error }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'

interface Props {
  length?: number
  modelValue?: string
  error?: string
  autoFocus?: boolean
}

interface Emits {
  (e: 'update:modelValue', value: string): void
  (e: 'complete', value: string): void
}

const props = withDefaults(defineProps<Props>(), {
  length: 6,
  modelValue: '',
  error: '',
  autoFocus: true,
})

const emit = defineEmits<Emits>()

// Create array of digits
const digits = ref<string[]>(Array(props.length).fill(''))
const inputRefs = ref<(HTMLInputElement | null)[]>([])

// Watch modelValue to update digits
watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue.length <= props.length) {
      const chars = newValue.split('')
      digits.value = [...chars, ...Array(props.length - chars.length).fill('')]
    }
  },
  { immediate: true }
)

// Computed property for the complete OTP value
const otpValue = computed(() => digits.value.join(''))

// Set input ref
const setInputRef = (el: any, index: number) => {
  if (el) {
    inputRefs.value[index] = el as HTMLInputElement
  }
}

// Handle input
const handleInput = (index: number, event: Event) => {
  const target = event.target as HTMLInputElement
  let value = target.value

  // Only allow numeric input
  value = value.replace(/[^0-9]/g, '')

  // Update the digit
  digits.value[index] = value.slice(-1) // Take only the last character

  // Update model value
  const newValue = digits.value.join('')
  emit('update:modelValue', newValue)

  // Auto-focus next input if value is entered
  if (value && index < props.length - 1) {
    focusInput(index + 1)
  }

  // Emit complete event if all digits are filled
  if (newValue.length === props.length) {
    emit('complete', newValue)
  }
}

// Handle keydown
const handleKeyDown = (index: number, event: KeyboardEvent) => {
  // Handle backspace
  if (event.key === 'Backspace') {
    if (!digits.value[index] && index > 0) {
      // If current input is empty, move to previous and clear it
      digits.value[index - 1] = ''
      focusInput(index - 1)
      event.preventDefault()
    } else if (digits.value[index]) {
      // Clear current input
      digits.value[index] = ''
    }
    emit('update:modelValue', digits.value.join(''))
  }
  // Handle left arrow
  else if (event.key === 'ArrowLeft' && index > 0) {
    focusInput(index - 1)
    event.preventDefault()
  }
  // Handle right arrow
  else if (event.key === 'ArrowRight' && index < props.length - 1) {
    focusInput(index + 1)
    event.preventDefault()
  }
  // Handle delete
  else if (event.key === 'Delete') {
    digits.value[index] = ''
    emit('update:modelValue', digits.value.join(''))
  }
}

// Handle paste
const handlePaste = (event: ClipboardEvent) => {
  event.preventDefault()
  const pastedData = event.clipboardData?.getData('text') || ''
  const pastedDigits = pastedData.replace(/[^0-9]/g, '').slice(0, props.length)

  // Fill digits with pasted data
  for (let i = 0; i < props.length; i++) {
    digits.value[i] = pastedDigits[i] || ''
  }

  // Update model value
  const newValue = digits.value.join('')
  emit('update:modelValue', newValue)

  // Focus the next empty input or the last input
  const nextEmptyIndex = digits.value.findIndex((d) => !d)
  if (nextEmptyIndex !== -1) {
    focusInput(nextEmptyIndex)
  } else {
    focusInput(props.length - 1)
  }

  // Emit complete event if all digits are filled
  if (newValue.length === props.length) {
    emit('complete', newValue)
  }
}

// Handle focus
const handleFocus = (index: number) => {
  // Select the content when focused
  const input = inputRefs.value[index]
  if (input) {
    input.select()
  }
}

// Focus specific input
const focusInput = (index: number) => {
  if (index >= 0 && index < props.length) {
    inputRefs.value[index]?.focus()
  }
}

// Clear all inputs
const clear = () => {
  digits.value = Array(props.length).fill('')
  emit('update:modelValue', '')
  focusInput(0)
}

// Focus first input on mount if autoFocus is true
if (props.autoFocus) {
  setTimeout(() => focusInput(0), 100)
}

// Expose methods
defineExpose({
  clear,
  focus: () => focusInput(0),
})
</script>

<style scoped lang="scss">
.otp-input-wrapper {
  width: 100%;
}

.otp-input-container {
  display: flex;
  gap: 0.75rem;
  justify-content: center;
  direction: ltr;
}

.otp-digit-input {
  width: 3rem;
  height: 3.5rem;
  text-align: center;
  font-size: 1.5rem;
  font-weight: 600;
  border: 2px solid #cbd5e0;
  border-radius: 0.5rem;
  background-color: #ffffff;
  color: #1a202c;
  transition: all 0.2s ease;
  outline: none;

  &:focus {
    border-color: #667eea;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
  }

  &.is-filled {
    border-color: #667eea;
    background-color: #f7fafc;
  }

  &.has-error {
    border-color: #f56565;

    &:focus {
      box-shadow: 0 0 0 3px rgba(245, 101, 101, 0.1);
    }
  }

  &::placeholder {
    color: #a0aec0;
  }
}

.otp-error {
  text-align: center;
  color: #f56565;
  font-size: 0.875rem;
  margin-top: 0.75rem;
}

@media (max-width: 640px) {
  .otp-input-container {
    gap: 0.5rem;
  }

  .otp-digit-input {
    width: 2.5rem;
    height: 3rem;
    font-size: 1.25rem;
  }
}
</style>
