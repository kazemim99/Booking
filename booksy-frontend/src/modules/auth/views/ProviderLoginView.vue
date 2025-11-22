<template>
  <div class="auth-page">
    <div class="auth-container">
      <div class="auth-card">
        <!-- Logo/Icon -->
        <div class="icon-container">
          <div class="icon-wrapper">
            <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
              />
            </svg>
          </div>
        </div>

        <!-- Header -->
        <div class="auth-header">
          <h1 class="auth-title">ورود به پنل کسب و کار</h1>
          <p class="auth-description">
            برای ورود به پنل ارائه‌دهندگان، شماره موبایل خود را وارد کنید
          </p>
        </div>

        <!-- Form -->
        <form class="auth-form" @submit.prevent="handleSubmit">
          <div class="form-group">
            <label for="phone" class="form-label">شماره موبایل</label>
            <input
              id="phone"
              v-model="phoneNumber"
              type="tel"
              dir="ltr"
              class="form-input"
              :class="{ 'form-input-error': error }"
              placeholder="09123456789"
              :disabled="isLoading"
            />
            <p v-if="error" class="form-error">{{ error }}</p>
          </div>

          <AppButton type="submit" variant="primary" size="large" block :loading="isLoading">
            دریافت کد
          </AppButton>
        </form>

        <!-- Terms -->
        <p class="auth-terms">
          با ورود به سیستم، شما
          <a href="#" class="auth-link">قوانین و مقررات</a>
          را می‌پذیرید
        </p>

        <!-- Customer Login Link -->
        <div class="auth-footer">
          <p class="auth-footer-text">
            مشتری هستید؟
            <router-link to="/login" class="auth-link">ورود برای رزرو نوبت</router-link>
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { usePhoneVerification } from '../composables/usePhoneVerification'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

const router = useRouter()
const { sendVerificationCode } = usePhoneVerification()

// State
const phoneNumber = ref('')
const error = ref('')
const isLoading = ref(false)

// Methods
const handleSubmit = async () => {
  error.value = ''

  // Validation
  if (!phoneNumber.value) {
    error.value = 'لطفاً شماره موبایل خود را وارد کنید'
    return
  }

  // Iranian phone number validation
  const phoneRegex = /^09\d{9}$/
  if (!phoneRegex.test(phoneNumber.value)) {
    error.value = 'شماره موبایل وارد شده معتبر نیست'
    return
  }

  isLoading.value = true

  try {
    const result = await sendVerificationCode(phoneNumber.value, 'IR')

    if (result.success) {
      // Navigate to provider verification page
      router.push({
        name: 'ProviderPhoneVerification',
        query: {
          phone: phoneNumber.value
        },
      })
    } else {
      error.value = result.error || 'خطا در ارسال کد تأیید'
    }
  } catch (err: any) {
    error.value = err.message || 'خطا در ارسال کد تأیید'
  } finally {
    isLoading.value = false
  }
}
</script>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background: linear-gradient(135deg, rgba(139, 92, 246, 0.05) 0%, rgba(236, 72, 153, 0.1) 100%);
  direction: rtl;
}

.auth-container {
  width: 100%;
  max-width: 28rem;
}

.auth-card {
  background: white;
  border-radius: 1rem;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
  padding: 2rem;
}

/* Icon */
.icon-container {
  text-align: center;
  margin-bottom: 2rem;
}

.icon-wrapper {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 4rem;
  height: 4rem;
  border-radius: 9999px;
  background: rgba(139, 92, 246, 0.1);
  margin: 0 auto 1rem;
}

.icon {
  width: 2rem;
  height: 2rem;
  color: #8b5cf6;
}

/* Header */
.auth-header {
  text-align: center;
  margin-bottom: 2rem;
}

.auth-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.auth-description {
  font-size: 0.875rem;
  color: #6b7280;
  line-height: 1.5;
}

/* Form */
.auth-form {
  margin-bottom: 1.5rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.5rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background: #f9fafb;
  transition: all 0.2s ease;
}

.form-input:focus {
  outline: none;
  border-color: #8b5cf6;
  background: white;
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
  margin-top: 0.5rem;
  font-size: 0.875rem;
  color: #ef4444;
}

/* Terms */
.auth-terms {
  text-align: center;
  font-size: 0.875rem;
  color: #6b7280;
  margin-bottom: 1rem;
}

.auth-link {
  color: #8b5cf6;
  text-decoration: none;
  transition: color 0.2s ease;
}

.auth-link:hover {
  color: #7c3aed;
  text-decoration: underline;
}

/* Footer */
.auth-footer {
  text-align: center;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb;
}

.auth-footer-text {
  font-size: 0.875rem;
  color: #6b7280;
}

/* Responsive */
@media (max-width: 640px) {
  .auth-card {
    padding: 1.5rem;
  }

  .auth-title {
    font-size: 1.25rem;
  }
}
</style>
