<template>
  <div class="customer-info" dir="rtl">
    <div class="step-header">
      <h2 class="step-title">اطلاعات تماس</h2>
      <p class="step-description">
        لطفاً اطلاعات خود را برای تکمیل رزرو وارد کنید
      </p>
    </div>

    <form class="info-form" @submit.prevent="handleSubmit">
      <div class="form-grid">
        <!-- First Name -->
        <div class="form-group">
          <label class="form-label">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
            نام
            <span class="required">*</span>
          </label>
          <input
            v-model="formData.firstName"
            type="text"
            class="form-input"
            :class="{ error: errors.firstName }"
            placeholder="مثال: سارا"
            @input="clearError('firstName')"
          />
          <span v-if="errors.firstName" class="error-message">{{ errors.firstName }}</span>
        </div>

        <!-- Last Name -->
        <div class="form-group">
          <label class="form-label">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
            نام خانوادگی
            <span class="required">*</span>
          </label>
          <input
            v-model="formData.lastName"
            type="text"
            class="form-input"
            :class="{ error: errors.lastName }"
            placeholder="مثال: احمدی"
            @input="clearError('lastName')"
          />
          <span v-if="errors.lastName" class="error-message">{{ errors.lastName }}</span>
        </div>

        <!-- Phone Number -->
        <div class="form-group">
          <label class="form-label">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
            </svg>
            شماره تماس
            <span class="required">*</span>
          </label>
          <input
            v-model="formData.phoneNumber"
            type="tel"
            class="form-input"
            :class="{ error: errors.phoneNumber }"
            placeholder="۰۹۱۲۳۴۵۶۷۸۹"
            dir="ltr"
            @input="clearError('phoneNumber')"
          />
          <span v-if="errors.phoneNumber" class="error-message">{{ errors.phoneNumber }}</span>
        </div>

        <!-- Email -->
        <div class="form-group">
          <label class="form-label">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
            ایمیل
            <span class="optional">(اختیاری)</span>
          </label>
          <input
            v-model="formData.email"
            type="email"
            class="form-input"
            :class="{ error: errors.email }"
            placeholder="example@email.com"
            dir="ltr"
            @input="clearError('email')"
          />
          <span v-if="errors.email" class="error-message">{{ errors.email }}</span>
        </div>

        <!-- Notes -->
        <div class="form-group full-width">
          <label class="form-label">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" />
            </svg>
            یادداشت
            <span class="optional">(اختیاری)</span>
          </label>
          <textarea
            v-model="formData.notes"
            class="form-textarea"
            placeholder="توضیحات یا درخواست‌های خاص خود را بنویسید..."
            rows="4"
          ></textarea>
          <div class="char-count">{{ formData.notes.length }}/۵۰۰</div>
        </div>
      </div>

      <!-- Info Cards -->
      <div class="info-cards">
        <div class="info-card">
          <div class="card-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M12 1.5a5.25 5.25 0 00-5.25 5.25v3a3 3 0 00-3 3v6.75a3 3 0 003 3h10.5a3 3 0 003-3v-6.75a3 3 0 00-3-3v-3c0-2.9-2.35-5.25-5.25-5.25zm3.75 8.25v-3a3.75 3.75 0 10-7.5 0v3h7.5z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="card-content">
            <h4>امنیت اطلاعات</h4>
            <p>اطلاعات شما کاملاً محرمانه و امن نگهداری می‌شود.</p>
          </div>
        </div>

        <div class="info-card">
          <div class="card-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path d="M1.5 8.67v8.58a3 3 0 003 3h15a3 3 0 003-3V8.67l-8.928 5.493a3 3 0 01-3.144 0L1.5 8.67z" />
              <path d="M22.5 6.908V6.75a3 3 0 00-3-3h-15a3 3 0 00-3 3v.158l9.714 5.978a1.5 1.5 0 001.572 0L22.5 6.908z" />
            </svg>
          </div>
          <div class="card-content">
            <h4>تایید رزرو</h4>
            <p>پس از ثبت، اطلاعات رزرو به ایمیل و پیامک ارسال می‌شود.</p>
          </div>
        </div>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useAuthStore } from '@/core/stores/modules/auth.store'

interface CustomerData {
  firstName: string
  lastName: string
  phoneNumber: string
  email: string
  notes: string
}

interface Props {
  customerData: CustomerData
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'info-updated', data: CustomerData): void
}>()

const authStore = useAuthStore()

// Get user data from authenticated user profile
const getUserPhoneNumber = (): string => {
  // User already authenticated with phone, use it
  return authStore.user?.phoneNumber || props.customerData.phoneNumber || ''
}

const getUserFirstName = (): string => {
  return authStore.user?.firstName || props.customerData.firstName || ''
}

const getUserLastName = (): string => {
  return authStore.user?.lastName || props.customerData.lastName || ''
}

const getUserEmail = (): string => {
  return authStore.user?.email || props.customerData.email || ''
}

// State - Auto-populate from user profile for faster booking
const formData = ref<CustomerData>({
  firstName: props.customerData.firstName || getUserFirstName(),
  lastName: props.customerData.lastName || getUserLastName(),
  phoneNumber: getUserPhoneNumber(),
  email: props.customerData.email || getUserEmail(),
  notes: props.customerData.notes || '',
})

const errors = ref({
  firstName: '',
  lastName: '',
  phoneNumber: '',
  email: '',
})

// Watch form data and emit changes
watch(formData, (newData) => {
  emit('info-updated', { ...newData })
}, { deep: true })

// Methods
const clearError = (field: keyof typeof errors.value) => {
  errors.value[field] = ''
}

const validateForm = (): boolean => {
  let isValid = true

  // Validate first name
  if (!formData.value.firstName.trim()) {
    errors.value.firstName = 'نام الزامی است'
    isValid = false
  } else if (formData.value.firstName.trim().length < 2) {
    errors.value.firstName = 'نام باید حداقل ۲ کاراکتر باشد'
    isValid = false
  }

  // Validate last name
  if (!formData.value.lastName.trim()) {
    errors.value.lastName = 'نام خانوادگی الزامی است'
    isValid = false
  } else if (formData.value.lastName.trim().length < 2) {
    errors.value.lastName = 'نام خانوادگی باید حداقل ۲ کاراکتر باشد'
    isValid = false
  }

  // Validate phone number
  const phoneRegex = /^09\d{9}$/
  if (!formData.value.phoneNumber.trim()) {
    errors.value.phoneNumber = 'شماره تماس الزامی است'
    isValid = false
  } else if (!phoneRegex.test(formData.value.phoneNumber.replace(/[۰-۹]/g, (d) => '۰۱۲۳۴۵۶۷۸۹'.indexOf(d).toString()))) {
    errors.value.phoneNumber = 'شماره تماس معتبر نیست'
    isValid = false
  }

  // Validate email (optional but must be valid if provided)
  if (formData.value.email.trim()) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(formData.value.email)) {
      errors.value.email = 'ایمیل معتبر نیست'
      isValid = false
    }
  }

  return isValid
}

const handleSubmit = () => {
  if (validateForm()) {
    emit('info-updated', { ...formData.value })
  }
}

// Expose validate method for parent component
defineExpose({
  validateForm,
})
</script>

<style scoped>
.customer-info {
  padding: 0;
}

.step-header {
  text-align: center;
  margin-bottom: 3rem;
}

.step-title {
  font-size: 2rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.step-description {
  font-size: 1.05rem;
  color: #64748b;
  margin: 0;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.form-group {
  display: flex;
  flex-direction: column;
}

.form-group.full-width {
  grid-column: 1 / -1;
}

.form-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1rem;
  font-weight: 600;
  color: #1e293b;
  margin-bottom: 0.5rem;
}

.form-label svg {
  width: 20px;
  height: 20px;
  color: #667eea;
}

.required {
  color: #ef4444;
}

.optional {
  font-size: 0.875rem;
  font-weight: 400;
  color: #94a3b8;
}

.form-input,
.form-textarea {
  padding: 1rem;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  font-size: 1rem;
  font-family: 'Vazir', sans-serif;
  color: #1e293b;
  transition: all 0.3s;
  background: white;
}

.form-input:focus,
.form-textarea:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.form-input.error,
.form-textarea.error {
  border-color: #ef4444;
}

.form-textarea {
  resize: vertical;
  min-height: 120px;
}

.error-message {
  font-size: 0.875rem;
  color: #ef4444;
  margin-top: 0.375rem;
}

.char-count {
  font-size: 0.875rem;
  color: #94a3b8;
  text-align: left;
  margin-top: 0.375rem;
}

.info-cards {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.5rem;
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 2px solid #f1f5f9;
}

.info-card {
  display: flex;
  gap: 1rem;
  padding: 1.5rem;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%);
  border-radius: 16px;
  border: 1px solid rgba(102, 126, 234, 0.1);
}

.card-icon {
  width: 48px;
  height: 48px;
  flex-shrink: 0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.card-icon svg {
  width: 24px;
  height: 24px;
  color: white;
}

.card-content h4 {
  font-size: 1rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.375rem 0;
}

.card-content p {
  font-size: 0.875rem;
  color: #64748b;
  margin: 0;
  line-height: 1.5;
}

/* Responsive */
@media (max-width: 768px) {
  .step-title {
    font-size: 1.75rem;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }

  .info-cards {
    grid-template-columns: 1fr;
  }
}
</style>
