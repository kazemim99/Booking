<template>
  <div class="auth-page">
    <div class="auth-container">
      <div class="auth-card">
        <!-- Icon -->
        <div class="icon-container">
          <div class="icon-wrapper">
            <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
              />
            </svg>
          </div>
        </div>

        <!-- Header -->
        <div class="auth-header">
          <h1 class="auth-title">تایید کد</h1>
          <p class="auth-description">
            کد 6 رقمی ارسال شده به شماره
            <span class="phone-number" dir="ltr">{{ maskedPhone || phoneNumber }}</span>
            را وارد کنید
          </p>
        </div>

        <!-- OTP Form -->
        <form class="auth-form" @submit.prevent="handleSubmit">
          <OtpInput
            ref="otpInputRef"
            v-model="otpCode"
            :error="error"
            :disabled="isLoading"
            :auto-focus="true"
            @complete="handleOtpComplete"
          />

          <AppButton type="submit" variant="primary" size="large" block :loading="isLoading">
            تایید کد
          </AppButton>

          <!-- Resend and Back Links -->
          <div class="auth-actions">
            <button
              v-if="canResend"
              type="button"
              class="action-link"
              :disabled="isLoading"
              @click="handleResendCode"
            >
              ارسال مجدد کد
            </button>
            <p v-else class="resend-countdown">
              ارسال مجدد کد در {{ resendCountdown }} ثانیه
            </p>

            <button
              type="button"
              class="action-link secondary"
              :disabled="isLoading"
              @click="handleBackToLogin"
            >
              بازگشت به صفحه ورود
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { usePhoneVerification } from '../composables/usePhoneVerification'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import OtpInput from '../components/OtpInput.vue'

const router = useRouter()
const route = useRoute()
const {
  state,
  canResend,
  resendCountdown,
  completeCustomerAuthentication,
  completeProviderAuthentication,
  resendCode
} = usePhoneVerification()
const authStore = useAuthStore()

// State
const otpCode = ref('')
const error = ref('')
const isLoading = ref(false)
const otpInputRef = ref<InstanceType<typeof OtpInput>>()

// Computed
const phoneNumber = computed(() => (route.query.phone as string) || state.value.phoneNumber)
const maskedPhone = computed(() => state.value.maskedPhone)

// Methods
const handleSubmit = async () => {
  console.log('Submit clicked, OTP code:', otpCode.value, 'length:', otpCode.value.length)
  console.log('Phone number:', phoneNumber.value)

  if (otpCode.value.length !== 6) {
    error.value = 'لطفاً کد 6 رقمی را وارد کنید'
    console.error('OTP validation failed: length is', otpCode.value.length)
    return
  }

  await verifyOtp()
}

const handleOtpComplete = async (code: string) => {
  // Auto-submit when OTP is complete
  console.log('OTP complete event received:', code)
  otpCode.value = code
  await verifyOtp()
}

const verifyOtp = async () => {
  // Prevent duplicate calls (race condition between auto-submit and manual submit)
  if (isLoading.value) {
    console.log('[VerificationView] ⚠️ Already processing, skipping duplicate call')
    return
  }

  error.value = ''
  isLoading.value = true

  console.log('[VerificationView] Verifying OTP:', otpCode.value, 'with phone:', phoneNumber.value)

  try {
    // Get userType from route query params (explicitly passed by login page)
    const userTypeFromQuery = route.query.userType as string | undefined
    console.log('[VerificationView] 🔍 UserType from route query:', userTypeFromQuery)

    // Validate and use userType, default to Customer if not provided
    const userType = (userTypeFromQuery === 'Provider' || userTypeFromQuery === 'Customer'
      ? userTypeFromQuery
      : 'Customer') as 'Customer' | 'Provider'
    console.log('[VerificationView] 🔑 Final userType for authentication:', userType)

    // Use unified authentication endpoint based on user type
    let result
    if (userType === 'Customer') {
      result = await completeCustomerAuthentication(otpCode.value)
    } else {
      result = await completeProviderAuthentication(otpCode.value)
    }

    console.log('[VerificationView] Authentication result:', result)

    if (result.success) {
      console.log('[VerificationView] Authentication successful!')

      // The composable already stored tokens and user info in authStore
      // Now we just need to redirect based on user type and status

      if (userType === 'Provider' && 'requiresOnboarding' in result && result.requiresOnboarding) {
        console.log('[VerificationView] Provider requires onboarding, redirecting...')
        await router.push({ name: 'ProviderOnboarding' })
      } else {
        console.log('[VerificationView] Checking redirect path...')
        await redirectBasedOnProviderStatus()
      }
    } else {
      console.error('[VerificationView] Authentication failed:', result.error)
      error.value = result.error || 'کد وارد شده صحیح نیست'
      // Clear OTP input
      otpInputRef.value?.clear()
    }
  } catch (err: any) {
    console.error('[VerificationView] Error:', err)
    error.value = err.message || 'خطا در تأیید کد'
    otpInputRef.value?.clear()
  } finally {
    isLoading.value = false
  }
}

const redirectBasedOnProviderStatus = async () => {
  try {
    // Check sessionStorage for post-login redirect (more reliable than query params)
    const redirectPath = sessionStorage.getItem('post_login_redirect')

    console.log('[VerificationView] Current route query:', route.query)
    console.log('[VerificationView] Redirect path from sessionStorage:', redirectPath)

    if (redirectPath) {
      // Clear the redirect from sessionStorage
      sessionStorage.removeItem('post_login_redirect')

      // Honor the redirect parameter
      console.log('[VerificationView] Phone verification complete, redirecting to:', redirectPath)
      await router.push(redirectPath)
    } else {
      // No redirect path - use redirectToDashboard to handle role-based routing
      console.log('[VerificationView] Phone verification complete, redirecting to dashboard')
      await authStore.redirectToDashboard()
    }
  } catch (error) {
    console.error('[VerificationView] Error during redirect:', error)
  }
}

const handleResendCode = async () => {
  error.value = ''
  otpCode.value = ''

  const result = await resendCode()

  if (result && !result.success) {
    error.value = result.error || 'خطا در ارسال مجدد کد'
  }
}

const handleBackToLogin = () => {
  router.push({ name: 'Login' })
}

// Auto-focus OTP input on mount
onMounted(() => {
  otpInputRef.value?.focusFirst()
})
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

.phone-number {
  font-weight: 600;
  color: #111827;
}

/* Form */
.auth-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Actions */
.auth-actions {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
}

.action-link {
  background: none;
  border: none;
  color: #8b5cf6;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  padding: 0.25rem;
  transition: all 0.2s ease;
}

.action-link:hover:not(:disabled) {
  color: #7c3aed;
  text-decoration: underline;
}

.action-link:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.action-link.secondary {
  color: #6b7280;
}

.action-link.secondary:hover:not(:disabled) {
  color: #111827;
}

.resend-countdown {
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
