<template>
  <div class="otp-input-wrapper">
    <label v-if="label" class="otp-input-label">
      {{ label }}
      <span v-if="required" class="required">*</span>
    </label>

    <div class="otp-input-container" :class="{ 'has-error': error }">
      <input
        v-for="(digit, index) in digits"
        :key="index"
        :ref="(el) => setInputRef(index, el as HTMLInputElement)"
        type="text"
        inputmode="numeric"
        maxlength="1"
        class="otp-digit-input"
        :class="{ 'has-value': digit, 'has-error': error }"
        :value="digit"
        :disabled="disabled"
        @input="(e) => handleInput(index, e)"
        @keydown="(e) => handleKeydown(index, e)"
        @paste="handlePaste"
      />
    </div>

    <!-- Error Message -->
    <transition name="fade">
      <div v-if="error" class="error-message">
        <svg class="error-icon" viewBox="0 0 20 20" fill="currentColor">
          <path
            fill-rule="evenodd"
            d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
            clip-rule="evenodd"
          />
        </svg>
        <span>{{ error }}</span>
      </div>
    </transition>

    <!-- Helper Text -->
    <div v-if="helperText && !error" class="helper-text">
      {{ helperText }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { watch, onMounted } from 'vue'
import { useOtpInput } from '../composables/useOtpInput'

interface Props {
  modelValue?: string
  label?: string
  helperText?: string
  error?: string
  required?: boolean
  disabled?: boolean
  length?: number
  autoFocus?: boolean
}

interface Emits {
  (e: 'update:modelValue', value: string): void
  (e: 'complete', value: string): void
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  label: '',
  helperText: '',
  error: '',
  required: false,
  disabled: false,
  length: 6,
  autoFocus: false,
})

const emit = defineEmits<Emits>()

// Use OTP input composable
const {
  digits,
  code,
  isComplete,
  setInputRef,
  handleInput,
  handleKeydown,
  handlePaste,
  focusFirst,
  clear,
  setError,
} = useOtpInput(props.length)

// Watch for code changes
watch(code, (newCode) => {
  emit('update:modelValue', newCode)

  // Emit complete event when all digits are filled
  if (isComplete.value) {
    emit('complete', newCode)
  }
})

// Watch for error prop changes
watch(
  () => props.error,
  (newError) => {
    if (newError) {
      setError()
    }
  }
)

// Auto-focus first input on mount
onMounted(() => {
  if (props.autoFocus) {
    focusFirst()
  }
})

// Public methods (exposed to parent)
defineExpose({
  focusFirst,
  clear,
  setError,
})
</script>

<style scoped>
.otp-input-wrapper {
  width: 100%;
}

.otp-input-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.5rem;
  text-align: center;
}

.otp-input-label .required {
  color: #ef4444;
  margin-left: 0.25rem;
}

.otp-input-container {
  display: flex;
  justify-content: center;
  gap: 0.75rem;
}

.otp-digit-input {
  width: 3rem;
  height: 3.5rem;
  text-align: center;
  font-size: 1.5rem;
  font-weight: 600;
  color: #111827;
  border: 2px solid #d1d5db;
  border-radius: 0.5rem;
  background-color: #ffffff;
  transition: all 0.2s ease;
  outline: none;
}

.otp-digit-input:focus {
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.otp-digit-input.has-value {
  border-color: #8b5cf6;
  background-color: #f5f3ff;
}

.otp-digit-input.has-error {
  border-color: #ef4444;
  animation: shake 0.5s ease;
}

.otp-digit-input:disabled {
  background-color: #f9fafb;
  cursor: not-allowed;
  opacity: 0.5;
}

/* Error Message */
.error-message {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  margin-top: 0.75rem;
  font-size: 0.875rem;
  color: #ef4444;
}

.error-icon {
  width: 1rem;
  height: 1rem;
  flex-shrink: 0;
}

/* Helper Text */
.helper-text {
  margin-top: 0.75rem;
  font-size: 0.875rem;
  color: #6b7280;
  text-align: center;
}

/* Shake Animation */
@keyframes shake {
  0%,
  100% {
    transform: translateX(0);
  }
  10%,
  30%,
  50%,
  70%,
  90% {
    transform: translateX(-8px);
  }
  20%,
  40%,
  60%,
  80% {
    transform: translateX(8px);
  }
}

.shake {
  animation: shake 0.5s ease;
}

/* Fade Transition */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* Responsive */
@media (max-width: 640px) {
  .otp-digit-input {
    width: 2.5rem;
    height: 3rem;
    font-size: 1.25rem;
  }

  .otp-input-container {
    gap: 0.5rem;
  }
}
</style>
