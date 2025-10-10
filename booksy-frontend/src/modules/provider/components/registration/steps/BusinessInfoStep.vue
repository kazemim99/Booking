<template>
  <StepContainer
    :title="$t('provider.registration.businessInfo.title')"
    :subtitle="$t('provider.registration.businessInfo.subtitle')"
  >
    <form class="business-info-form" @submit.prevent="handleSubmit">
      <!-- Business Name -->
      <div class="form-group">
        <label for="businessName" class="form-label">
          {{ $t('provider.registration.businessInfo.businessName') }}
          <span class="required">*</span>
        </label>
        <input
          id="businessName"
          v-model="formData.businessName"
          type="text"
          class="form-input"
          :class="{ 'has-error': errors.businessName }"
          :placeholder="$t('provider.registration.businessInfo.businessNamePlaceholder')"
          required
          @blur="validateField('businessName')"
        />
        <span v-if="errors.businessName" class="error-message">{{ errors.businessName }}</span>
      </div>

      <!-- Owner First Name -->
      <div class="form-group">
        <label for="ownerFirstName" class="form-label">
          {{ $t('provider.registration.businessInfo.ownerFirstName') }}
          <span class="required">*</span>
        </label>
        <input
          id="ownerFirstName"
          v-model="formData.ownerFirstName"
          type="text"
          class="form-input"
          :class="{ 'has-error': errors.ownerFirstName }"
          :placeholder="$t('provider.registration.businessInfo.firstNamePlaceholder')"
          required
          @blur="validateField('ownerFirstName')"
        />
        <span v-if="errors.ownerFirstName" class="error-message">{{ errors.ownerFirstName }}</span>
      </div>

      <!-- Owner Last Name -->
      <div class="form-group">
        <label for="ownerLastName" class="form-label">
          {{ $t('provider.registration.businessInfo.ownerLastName') }}
          <span class="required">*</span>
        </label>
        <input
          id="ownerLastName"
          v-model="formData.ownerLastName"
          type="text"
          class="form-input"
          :class="{ 'has-error': errors.ownerLastName }"
          :placeholder="$t('provider.registration.businessInfo.lastNamePlaceholder')"
          required
          @blur="validateField('ownerLastName')"
        />
        <span v-if="errors.ownerLastName" class="error-message">{{ errors.ownerLastName }}</span>
      </div>

      <!-- Phone Number (Read-only from verification) -->
      <div class="form-group">
        <label for="phoneNumber" class="form-label">
          {{ $t('provider.registration.businessInfo.phoneNumber') }}
        </label>
        <input
          id="phoneNumber"
          v-model="formData.phoneNumber"
          type="tel"
          class="form-input"
          disabled
          readonly
        />
        <span class="form-hint">{{ $t('provider.registration.businessInfo.phoneHint') }}</span>
      </div>

      <!-- Navigation -->
      <NavigationButtons
        :show-back="true"
        :can-continue="!!isFormValid"
        @back="$emit('back')"
        @next="handleSubmit"
      />
    </form>
  </StepContainer>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import StepContainer from '../shared/StepContainer.vue'
import NavigationButtons from '../shared/NavigationButtons.vue'
import type { BusinessInfo } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: Partial<BusinessInfo>
}

interface Emits {
  (e: 'update:modelValue', value: Partial<BusinessInfo>): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Form data
const formData = ref<Partial<BusinessInfo>>({
  businessName: props.modelValue?.businessName || '',
  ownerFirstName: props.modelValue?.ownerFirstName || '',
  ownerLastName: props.modelValue?.ownerLastName || '',
  phoneNumber: props.modelValue?.phoneNumber || '',
})

// Validation errors
const errors = ref<Record<string, string>>({})

// Validate single field
const validateField = (field: keyof BusinessInfo) => {
  errors.value[field] = ''

  const value = formData.value[field]

  if (!value || (typeof value === 'string' && !value.trim())) {
    errors.value[field] = `${field} is required`
    return false
  }

  if (typeof value === 'string' && value.trim().length < 2) {
    errors.value[field] = 'Must be at least 2 characters'
    return false
  }

  return true
}

// Validate all fields
const validateForm = (): boolean => {
  let isValid = true

  isValid = validateField('businessName') && isValid
  isValid = validateField('ownerFirstName') && isValid
  isValid = validateField('ownerLastName') && isValid

  return isValid
}

// Check if form is valid
const isFormValid = computed(() => {
  return (
    formData.value.businessName?.trim() &&
    formData.value.ownerFirstName?.trim() &&
    formData.value.ownerLastName?.trim() &&
    Object.keys(errors.value).every((key) => !errors.value[key])
  )
})

// Handle form submission
const handleSubmit = () => {
  if (validateForm()) {
    emit('update:modelValue', formData.value)
    emit('next')
  }
}

// Watch for changes and emit
watch(
  formData,
  (newValue) => {
    emit('update:modelValue', newValue)
  },
  { deep: true },
)

// Watch props for external updates
watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue) {
      formData.value = { ...formData.value, ...newValue }
    }
  },
  { deep: true },
)
</script>

<style scoped>
.business-info-form {
  width: 100%;
  max-width: 32rem;
  margin: 0 auto;
}

/* Form Groups */
.form-group {
  margin-bottom: 1.5rem;
}

.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 600;
  color: #374151;
  margin-bottom: 0.5rem;
}

.required {
  color: #ef4444;
  margin-left: 0.25rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 0.9375rem;
  color: #111827;
  background-color: #ffffff;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  transition: all 0.2s ease;
  outline: none;
}

.form-input:focus {
  border-color: #10b981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}

.form-input:disabled {
  background-color: #f3f4f6;
  color: #9ca3af;
  cursor: not-allowed;
}

.form-input.has-error {
  border-color: #ef4444;
}

.form-input.has-error:focus {
  border-color: #ef4444;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
}

.error-message {
  display: block;
  margin-top: 0.375rem;
  font-size: 0.8125rem;
  color: #ef4444;
}

.form-hint {
  display: block;
  margin-top: 0.375rem;
  font-size: 0.8125rem;
  color: #6b7280;
}

/* Responsive */
@media (max-width: 640px) {
  .business-info-form {
    max-width: 100%;
  }

  .form-input {
    padding: 0.625rem 0.875rem;
    font-size: 0.875rem;
  }
}
</style>
