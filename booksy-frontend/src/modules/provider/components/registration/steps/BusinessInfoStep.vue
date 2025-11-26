<template>
  <div class="registration-step">

    <div class="step-card">
      <div class="step-header">
        <h2 class="step-title">اطلاعات کسب‌و‌کار</h2>
        <p class="step-description">لطفاً اطلاعات کسب‌و‌کار خود را وارد کنید</p>
      </div>

      <form class="step-form" @submit.prevent="handleSubmit" autocomplete="off">
        <!-- Business Name -->
        <div class="form-group">
          <label for="businessName" class="form-label">
            نام کسب‌و‌کار <span class="required">*</span>
          </label>
          <input
            id="businessName"
            v-model="formData.businessName"
            type="text"
            class="form-input"
            :class="{ 'form-input-error': errors.businessName }"
            placeholder="مثال: آرایشگاه زیبا"
            @blur="validateField('businessName')"
          />
          <span v-if="errors.businessName" class="form-error">{{ errors.businessName }}</span>
        </div>

        <!-- Owner Name -->
        <div class="form-group">
          <label for="ownerFirstName" class="form-label">
            نام مالک <span class="required">*</span>
          </label>
          <input
            id="ownerFirstName"
            v-model="formData.ownerFirstName"
            type="text"
            class="form-input"
            :class="{ 'form-input-error': errors.ownerFirstName }"
            placeholder="مثال: علی"
            @blur="validateField('ownerFirstName')"
          />
          <span v-if="errors.ownerFirstName" class="form-error">{{ errors.ownerFirstName }}</span>
        </div>

        <!-- Owner Last Name -->
        <div class="form-group">
          <label for="ownerLastName" class="form-label">
            نام خانوادگی مالک <span class="required">*</span>
          </label>
          <input
            id="ownerLastName"
            v-model="formData.ownerLastName"
            type="text"
            class="form-input"
            :class="{ 'form-input-error': errors.ownerLastName }"
            placeholder="مثال: احمدی"
            @blur="validateField('ownerLastName')"
          />
          <span v-if="errors.ownerLastName" class="form-error">{{ errors.ownerLastName }}</span>
        </div>

        <!-- Phone (Read-only) -->
        <div class="form-group">
          <label for="phoneNumber" class="form-label">شماره تماس</label>
          <input
            id="phoneNumber"
            v-model="formData.phoneNumber"
            type="tel"
            dir="ltr"
            class="form-input"
            readonly
            disabled
          />
        </div>

        <!-- Navigation -->
        <div class="step-actions">
          <AppButton type="submit" variant="primary" size="large" block :disabled="!isFormValid">
            بعدی
          </AppButton>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAuthStore } from '@/core/stores/modules/auth.store'

import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import type { BusinessInfo } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: Partial<BusinessInfo>
}

interface Emits {
  (e: 'update:modelValue', value: Partial<BusinessInfo>): void
  (e: 'next'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()
const authStore = useAuthStore()

// Get phone number from sessionStorage (saved during phone verification) or authStore
const PHONE_NUMBER_KEY = 'phone_verification_number'
const getPhoneNumber = () => {
  return sessionStorage.getItem(PHONE_NUMBER_KEY) || authStore.user?.phoneNumber || ''
}

// Form data
const formData = ref({
  businessName: props.modelValue?.businessName || '',
  ownerFirstName: props.modelValue?.ownerFirstName || '',
  ownerLastName: props.modelValue?.ownerLastName || '',
  phoneNumber: getPhoneNumber(),
})

const errors = ref<Record<string, string>>({})

// Validation
const validateField = (field: keyof typeof formData.value) => {
  errors.value = { ...errors.value }
  delete errors.value[field]

  if (field === 'businessName' && !formData.value.businessName.trim()) {
    errors.value.businessName = 'نام کسب‌و‌کار الزامی است'
  }
  if (field === 'ownerFirstName' && !formData.value.ownerFirstName.trim()) {
    errors.value.ownerFirstName = 'نام مالک الزامی است'
  }
  if (field === 'ownerLastName' && !formData.value.ownerLastName.trim()) {
    errors.value.ownerLastName = 'نام خانوادگی مالک الزامی است'
  }
}

const isFormValid = computed(() => {
  return (
    formData.value.businessName.trim() &&
    formData.value.ownerFirstName.trim() &&
    formData.value.ownerLastName.trim()
  )
})

const handleSubmit = () => {
  // Validate all fields
  validateField('businessName')
  validateField('ownerFirstName')
  validateField('ownerLastName')

  if (isFormValid.value && Object.keys(errors.value).length === 0) {
    emit('update:modelValue', formData.value)
    emit('next')
  }
}
</script>

<style scoped>
.registration-step {
  min-height: 100vh;
  padding: 2rem 1rem;
  background: #f9fafb;
  direction: rtl;
}

.step-card {
  max-width: 42rem;
  margin: 0 auto;
  background: white;
  border-radius: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  padding: 2rem;
}

.step-header {
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.step-description {
  font-size: 0.875rem;
  color: #6b7280;
}

.step-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.required {
  color: #ef4444;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
}

.form-input:focus {
  outline: none;
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.form-input:disabled {
  background: #f3f4f6;
  cursor: not-allowed;
  opacity: 0.6;
}

.form-input-error {
  border-color: #ef4444;
}

.form-input-error:focus {
  border-color: #ef4444;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
}

.form-error {
  font-size: 0.875rem;
  color: #ef4444;
}

.step-actions {
  margin-top: 1rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

@media (max-width: 640px) {
  .step-card {
    padding: 1.5rem;
  }

  .step-title {
    font-size: 1.25rem;
  }
}
</style>
