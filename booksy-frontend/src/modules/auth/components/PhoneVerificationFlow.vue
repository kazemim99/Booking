<template>
  <div class="phone-verification-flow">
    <!-- Step 1: Phone Number Entry -->
    <div v-if="state.step === 'phone'" class="verification-step">
      <div class="step-header">
        <h2 class="step-title">{{ $t('auth.phoneVerification.enterPhone') }}</h2>
        <p class="step-description">
          {{ $t('auth.phoneVerification.enterPhoneDescription') }}
        </p>
      </div>

      <form class="step-form" @submit.prevent="handleSendCode">
        <PhoneNumberInput
          v-model="phoneInput"
          :label="$t('auth.phoneVerification.phoneNumberLabel')"
          :placeholder="$t('auth.phoneVerification.phoneNumberPlaceholder')"
          :error="state.error || ''"
          :disabled="state.isLoading"
          required
        />

        <button type="submit" class="btn btn-primary" :disabled="!isPhoneValid || state.isLoading">
          <span v-if="state.isLoading" class="loading-spinner"></span>
          <span>{{ $t('auth.phoneVerification.sendCode') }}</span>
        </button>
      </form>
    </div>

    <!-- Step 2: OTP Verification -->
    <div v-else-if="state.step === 'otp'" class="verification-step">
      <div class="step-header">
        <h2 class="step-title">{{ $t('auth.phoneVerification.verifyCode') }}</h2>
        <p class="step-description">
          {{ $t('auth.phoneVerification.verifyCodeDescription', { phone: state.maskedPhone }) }}
        </p>
      </div>

      <form class="step-form" @submit.prevent="handleVerifyCode">
        <OtpInput
          ref="otpInputRef"
          v-model="otpCode"
          :label="$t('auth.phoneVerification.enterCodeLabel')"
          :helper-text="
            state.remainingAttempts < 3
              ? $t('auth.phoneVerification.attemptsRemaining', {
                  count: state.remainingAttempts,
                })
              : ''
          "
          :error="state.error || ''"
          :disabled="state.isLoading"
          :auto-focus="true"
          required
          @complete="handleOtpComplete"
        />

        <!-- Resend Code -->
        <div class="resend-container">
          <button
            v-if="canResend"
            type="button"
            class="btn-link"
            :disabled="state.isLoading"
            @click="handleResendCode"
          >
            {{ $t('auth.phoneVerification.resendCode') }}
          </button>
          <p v-else class="resend-countdown">
            {{ $t('auth.phoneVerification.resendIn', { seconds: resendCountdown }) }}
          </p>
        </div>

        <!-- Change Phone Number -->
        <button
          type="button"
          class="btn-text"
          :disabled="state.isLoading"
          @click="handleChangePhone"
        >
          {{ $t('auth.phoneVerification.changePhoneNumber') }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { usePhoneVerification } from '../composables/usePhoneVerification'
import PhoneNumberInput from './PhoneNumberInput.vue'
import OtpInput from './OtpInput.vue'
import { UserInfo } from '../types/phoneVerification.types'

interface Emits {
  (e: 'success', data: { user: UserInfo; token: string; isNewUser?: boolean }): void
  (e: 'error', error: string): void
}

const emit = defineEmits<Emits>()

// Composables
const {
  state,
  canResend,
  resendCountdown,
  sendVerificationCode,
  verifyCode,
  resendCode,
  changePhoneNumber,
} = usePhoneVerification()

// Local state
const phoneInput = ref({
  phoneNumber: '',
  countryCode: 'DE',
})
const otpCode = ref('')
const otpInputRef = ref<InstanceType<typeof OtpInput>>()

// Computed
const isPhoneValid = computed(() => {
  const phone = phoneInput.value.phoneNumber.trim()
  return phone.length >= 4 // Basic validation
})

// Methods
const handleSendCode = async () => {
  if (!isPhoneValid.value) return

  const result = await sendVerificationCode(
    phoneInput.value.phoneNumber,
    phoneInput.value.countryCode,
  )

  if (!result.success) {
    emit('error', result.error || 'Failed to send code')
  }
}

const handleVerifyCode = async () => {
  if (otpCode.value.length !== 6) return
  await verifyOtp()
}

const handleOtpComplete = async (code: string) => {
  // Auto-submit when OTP is complete
  otpCode.value = code
  await verifyOtp()
}

const verifyOtp = async () => {
  const result = await verifyCode(otpCode.value)
  if (!result) return

  if (result.success && result.user) {
    emit('success', {
      user: result.user,
      token: result.token || '',
      isNewUser: result.isNewUser,
    })
  } else {
    emit('error', result.error || 'Verification failed')
    // Clear OTP input on error
    otpInputRef.value?.clear()
    otpCode.value = ''
  }
}

const handleResendCode = async () => {
  const result = await resendCode()
  if (!result) return

  if (!result.success) {
    emit('error', result.error || 'Failed to resend code')
  }
  // Clear OTP input
  otpInputRef.value?.clear()
  otpCode.value = ''
}

const handleChangePhone = () => {
  changePhoneNumber()
  otpCode.value = ''
}

// Watch for step changes to reset local state
watch(
  () => state.value.step,
  (newStep) => {
    if (newStep === 'phone') {
      otpCode.value = ''
    }
  },
)
</script>

<style scoped>
.phone-verification-flow {
  width: 100%;
  max-width: 28rem;
  margin: 0 auto;
}

.verification-step {
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(0.5rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Step Header */
.step-header {
  text-align: center;
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.875rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.step-description {
  font-size: 0.875rem;
  color: #6b7280;
  line-height: 1.5;
}

/* Step Form */
.step-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Buttons */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  border-radius: 0.5rem;
  border: none;
  cursor: pointer;
  transition: all 0.2s ease;
  text-decoration: none;
}

.btn-primary {
  background-color: #8b5cf6;
  color: #ffffff;
}

.btn-primary:hover:not(:disabled) {
  background-color: #7c3aed;
  transform: translateY(-1px);
  box-shadow: 0 4px 6px -1px rgba(139, 92, 246, 0.3);
}

.btn-primary:active:not(:disabled) {
  transform: translateY(0);
}

.btn-primary:disabled {
  background-color: #d1d5db;
  cursor: not-allowed;
  transform: none;
}

.btn-link {
  background: none;
  border: none;
  color: #8b5cf6;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  text-decoration: underline;
  padding: 0;
}

.btn-link:hover:not(:disabled) {
  color: #7c3aed;
}

.btn-link:disabled {
  color: #9ca3af;
  cursor: not-allowed;
}

.btn-text {
  background: none;
  border: none;
  color: #6b7280;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  padding: 0;
  text-align: center;
}

.btn-text:hover:not(:disabled) {
  color: #111827;
  text-decoration: underline;
}

.btn-text:disabled {
  color: #9ca3af;
  cursor: not-allowed;
}

/* Loading Spinner */
.loading-spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Resend Container */
.resend-container {
  text-align: center;
}

.resend-countdown {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

/* Success Container */
.success-container {
  text-align: center;
  padding: 2rem 0;
}

.success-icon {
  width: 4rem;
  height: 4rem;
  margin: 0 auto 1.5rem;
  color: #10b981;
  animation: scaleIn 0.5s ease;
}

@keyframes scaleIn {
  from {
    opacity: 0;
    transform: scale(0.5);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

.success-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.success-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin-bottom: 2rem;
  line-height: 1.5;
}

/* Responsive */
@media (max-width: 640px) {
  .phone-verification-flow {
    max-width: 100%;
    padding: 0 1rem;
  }

  .step-title {
    font-size: 1.5rem;
  }
}
</style>
