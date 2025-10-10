<template>
  <div class="phone-input-wrapper">
    <label v-if="label" class="phone-input-label">
      {{ label }}
      <span v-if="required" class="required">*</span>
    </label>

    <div class="phone-input-container" :class="{ 'has-error': error, 'is-disabled': disabled }">
      <!-- Country Code Dropdown -->
      <div class="country-selector">
        <button
          type="button"
          class="country-button"
          :disabled="disabled"
          @click="toggleCountryDropdown"
          @blur="closeCountryDropdown"
        >
          <!-- <span class="country-flag">{{ selectedCountry.flag }}</span>
          <span class="country-code">{{ selectedCountry.dialCode }}</span>
          <svg
            class="chevron-icon"
            :class="{ 'is-open': isCountryDropdownOpen }"
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            <path
              fill-rule="evenodd"
              d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"
              clip-rule="evenodd"
            />
          </svg> -->
        </button>

        <!-- Country Dropdown -->
        <!-- <transition name="dropdown">
          <div v-if="isCountryDropdownOpen" class="country-dropdown">
            <div class="country-dropdown-inner">
              <button
                v-for="country in countryOptions"
                :key="country.code"
                type="button"
                class="country-option"
                :class="{ 'is-selected': country.code === modelValue.countryCode }"
                @mousedown.prevent="selectCountry(country)"
              >
                <span class="country-flag">{{ country.flag }}</span>
                <span class="country-name">{{ country.name }}</span>
                <span class="country-dial-code">{{ country.dialCode }}</span>
              </button>
            </div>
          </div>
        </transition> -->
      </div>

      <!-- Phone Number Input -->
      <input
        ref="phoneInput"
        type="tel"
        class="phone-input"
        :value="modelValue.phoneNumber"
        :placeholder="placeholder"
        :disabled="disabled"
        :autocomplete="autocomplete"
        @input="handlePhoneInput"
        @blur="handleBlur"
        @focus="handleFocus"
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
import { ref, computed } from 'vue'
import { COUNTRY_OPTIONS, type CountryOption } from '../types/phoneVerification.types'

interface PhoneInputValue {
  phoneNumber: string
  countryCode: string
}

interface Props {
  modelValue: PhoneInputValue
  label?: string
  placeholder?: string
  helperText?: string
  error?: string
  required?: boolean
  disabled?: boolean
  autocomplete?: string
}

interface Emits {
  (e: 'update:modelValue', value: PhoneInputValue): void
  (e: 'blur'): void
  (e: 'focus'): void
}

const props = withDefaults(defineProps<Props>(), {
  label: '',
  placeholder: 'Phone number',
  helperText: '',
  error: '',
  required: false,
  disabled: false,
  autocomplete: 'tel',
})

const emit = defineEmits<Emits>()

// Refs
const phoneInput = ref<HTMLInputElement>()
const isCountryDropdownOpen = ref(false)
const countryOptions = COUNTRY_OPTIONS

// Computed
// Even though the country selector UI is commented out,
// we still need this computed property for potential programmatic access
const selectedCountry = computed(() => {
  return countryOptions.find((c) => c.code === props.modelValue.countryCode) || countryOptions[0]
})

// Computed property is used in defineExpose below

// Methods
const handlePhoneInput = (event: Event) => {
  const target = event.target as HTMLInputElement
  let value = target.value

  // Remove non-numeric characters except + and spaces
  value = value.replace(/[^\d+\s]/g, '')

  emit('update:modelValue', {
    phoneNumber: value,
    countryCode: props.modelValue.countryCode,
  })
}

// This function will be used when the country selector UI is uncommented
// For now, we export it to ensure TypeScript doesn't complain about unused variables
const selectCountry = (country: CountryOption) => {
  emit('update:modelValue', {
    phoneNumber: props.modelValue.phoneNumber,
    countryCode: country.code,
  })
  isCountryDropdownOpen.value = false
  phoneInput.value?.focus()
}

// Public methods (exposed to parent)
const focus = () => {
  phoneInput.value?.focus()
}

// Make the function available to parent components
defineExpose({
  focus,
  selectCountry, // Expose the selectCountry function
  selectedCountry, // Expose the selected country computed
})

const toggleCountryDropdown = () => {
  if (!props.disabled) {
    isCountryDropdownOpen.value = !isCountryDropdownOpen.value
  }
}

const closeCountryDropdown = () => {
  setTimeout(() => {
    isCountryDropdownOpen.value = false
  }, 200)
}

const handleBlur = () => {
  emit('blur')
}

const handleFocus = () => {
  emit('focus')
}

// Remove this block as it's defined above now
</script>

<style scoped>
.phone-input-wrapper {
  width: 100%;
}

.phone-input-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.5rem;
}

.phone-input-label .required {
  color: #ef4444;
  margin-left: 0.25rem;
}

.phone-input-container {
  display: flex;
  align-items: center;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background-color: #ffffff;
  transition: all 0.2s ease;
}

.phone-input-container:focus-within {
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.phone-input-container.has-error {
  border-color: #ef4444;
}

.phone-input-container.has-error:focus-within {
  border-color: #ef4444;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
}

.phone-input-container.is-disabled {
  background-color: #f9fafb;
  cursor: not-allowed;
}

/* Country Selector */
.country-selector {
  position: relative;
  flex-shrink: 0;
}

.country-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 0.75rem;
  border: none;
  background: none;
  cursor: pointer;
  color: #374151;
  font-size: 0.875rem;
  transition: background-color 0.2s ease;
  border-right: 1px solid #e5e7eb;
}

.country-button:hover:not(:disabled) {
  background-color: #f9fafb;
}

.country-button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.country-flag {
  font-size: 1.25rem;
  line-height: 1;
}

.country-code {
  font-weight: 500;
  min-width: 2.5rem;
}

.chevron-icon {
  width: 1rem;
  height: 1rem;
  transition: transform 0.2s ease;
}

.chevron-icon.is-open {
  transform: rotate(180deg);
}

/* Country Dropdown */
.country-dropdown {
  position: absolute;
  top: calc(100% + 0.5rem);
  left: 0;
  z-index: 50;
  width: 280px;
  background-color: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  box-shadow:
    0 10px 15px -3px rgba(0, 0, 0, 0.1),
    0 4px 6px -2px rgba(0, 0, 0, 0.05);
}

.country-dropdown-inner {
  max-height: 300px;
  overflow-y: auto;
  padding: 0.25rem;
}

.country-option {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  padding: 0.75rem;
  border: none;
  background: none;
  cursor: pointer;
  border-radius: 0.375rem;
  transition: background-color 0.2s ease;
  text-align: left;
}

.country-option:hover {
  background-color: #f3f4f6;
}

.country-option.is-selected {
  background-color: #ede9fe;
  color: #8b5cf6;
}

.country-option .country-flag {
  font-size: 1.25rem;
}

.country-option .country-name {
  flex: 1;
  font-size: 0.875rem;
  font-weight: 500;
}

.country-option .country-dial-code {
  font-size: 0.875rem;
  color: #6b7280;
}

/* Phone Input */
.phone-input {
  flex: 1;
  padding: 0.75rem 1rem;
  border: none;
  background: none;
  font-size: 0.875rem;
  color: #111827;
  outline: none;
}

.phone-input::placeholder {
  color: #9ca3af;
}

.phone-input:disabled {
  cursor: not-allowed;
  color: #6b7280;
}

/* Error Message */
.error-message {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-top: 0.5rem;
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
  margin-top: 0.5rem;
  font-size: 0.875rem;
  color: #6b7280;
}

/* Transitions */
.dropdown-enter-active,
.dropdown-leave-active {
  transition:
    opacity 0.2s ease,
    transform 0.2s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-0.5rem);
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
