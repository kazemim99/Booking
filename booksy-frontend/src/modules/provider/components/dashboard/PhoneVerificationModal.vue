<template>
  <Modal
    v-model="isOpen"
    title="تأیید شماره موبایل"
    size="small"
    :closable="!isVerifying"
    :close-on-overlay="!isVerifying"
  >
    <div class="phone-verification-content">
      <!-- Step 1: Show new phone number -->
      <div v-if="step === 'confirm'" class="verification-step">
        <p class="step-description">
          کد تأیید به شماره <strong>{{ formattedPhone }}</strong> ارسال می‌شود.
        </p>

        <div class="button-group">
          <button type="button" class="btn btn-secondary" @click="handleCancel">
            انصراف
          </button>
          <button
            type="button"
            class="btn btn-primary"
            :disabled="isSending"
            @click="handleSendCode"
          >
            <span v-if="isSending" class="loading-spinner"></span>
            <span>{{ isSending ? 'در حال ارسال...' : 'ارسال کد' }}</span>
          </button>
        </div>
      </div>

      <!-- Step 2: OTP Verification -->
      <div v-else-if="step === 'verify'" class="verification-step">
        <p class="step-description">
          کد ۶ رقمی ارسال شده به <strong>{{ formattedPhone }}</strong> را وارد کنید.
        </p>

        <OtpInput
          ref="otpInputRef"
          v-model="otpCode"
          label="کد تأیید"
          :error="verificationError"
          :disabled="isVerifying"
          :auto-focus="true"
          :length="6"
          @complete="handleVerifyCode"
        />

        <!-- Resend Code -->
        <div class="resend-container">
          <button
            v-if="canResend"
            type="button"
            class="btn-link"
            :disabled="isVerifying"
            @click="handleResendCode"
          >
            ارسال مجدد کد
          </button>
          <p v-else class="resend-countdown">
            ارسال مجدد کد در {{ resendCountdown }} ثانیه
          </p>
        </div>

        <!-- Change Phone -->
        <button
          type="button"
          class="btn-text"
          :disabled="isVerifying"
          @click="handleCancel"
        >
          تغییر شماره موبایل
        </button>
      </div>

      <!-- Step 3: Success -->
      <div v-else-if="step === 'success'" class="verification-step">
        <div class="success-container">
          <svg class="success-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          <h3 class="success-title">تأیید موفق</h3>
          <p class="success-description">
            شماره موبایل شما با موفقیت تأیید شد.
          </p>
        </div>
      </div>
    </div>
  </Modal>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import Modal from '@/shared/components/Modal.vue'
import OtpInput from '@/modules/auth/components/OtpInput.vue'
import { providerProfileService } from '../../services/provider-profile.service'
import { useAuthStore } from '@/core/stores/modules/auth.store'

interface Props {
  modelValue: boolean
  phoneNumber: string
  userId: string
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'verified', phoneNumber: string): void
  (e: 'cancel'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Local state
const step = ref<'confirm' | 'verify' | 'success'>('confirm')
const otpCode = ref('')
const otpInputRef = ref<InstanceType<typeof OtpInput>>()
const isSending = ref(false)
const isVerifying = ref(false)
const verificationError = ref('')
const resendCountdown = ref(0)
const resendTimer = ref<NodeJS.Timeout | null>(null)

const isOpen = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
})

const formattedPhone = computed(() => {
  // Format phone number for display (e.g., 0912 345 6789)
  const phone = props.phoneNumber
  if (phone.length === 11 && phone.startsWith('0')) {
    return `${phone.substring(0, 4)} ${phone.substring(4, 7)} ${phone.substring(7)}`
  }
  return phone
})

const canResend = computed(() => resendCountdown.value === 0)

// Methods
const handleSendCode = async () => {
  isSending.value = true
  verificationError.value = ''

  try {
    // Call API to send verification code
    const result = await providerProfileService.sendPhoneVerificationCode(
      props.userId,
      props.phoneNumber
    )

    console.log('Verification code sent:', result)

    // Move to verify step
    step.value = 'verify'
    startResendCountdown()
  } catch (error: any) {
    console.error('Error sending verification code:', error)
    verificationError.value = error.response?.data?.message || error.message || 'خطا در ارسال کد تأیید'
  } finally {
    isSending.value = false
  }
}

const handleVerifyCode = async () => {
  if (otpCode.value.length !== 6) return

  isVerifying.value = true
  verificationError.value = ''

  try {
    // Call API to verify code
    const result = await providerProfileService.verifyPhoneCode(
      props.userId,
      props.phoneNumber,
      otpCode.value
    )

    console.log('Phone verified:', result)

    if (!result.success) {
      verificationError.value = result.message || 'کد تأیید نامعتبر است'
      otpInputRef.value?.clear()
      otpCode.value = ''
      return
    }

    // Show success
    step.value = 'success'

    // Wait a moment then emit success
    setTimeout(() => {
      emit('verified', props.phoneNumber)
      handleClose()
    }, 1500)
  } catch (error: any) {
    console.error('Error verifying code:', error)
    verificationError.value = error.response?.data?.message || error.message || 'کد تأیید نامعتبر است'
    otpInputRef.value?.clear()
    otpCode.value = ''
  } finally {
    isVerifying.value = false
  }
}

const handleResendCode = async () => {
  if (!canResend.value) return

  isSending.value = true
  verificationError.value = ''

  try {
    // Call API to resend code
    await providerProfileService.sendPhoneVerificationCode(
      props.userId,
      props.phoneNumber
    )

    // Clear OTP input
    otpInputRef.value?.clear()
    otpCode.value = ''

    // Restart countdown
    startResendCountdown()
  } catch (error: any) {
    console.error('Error resending code:', error)
    verificationError.value = error.response?.data?.message || error.message || 'خطا در ارسال مجدد کد'
  } finally {
    isSending.value = false
  }
}

const handleCancel = () => {
  emit('cancel')
  handleClose()
}

const handleClose = () => {
  // Reset state
  step.value = 'confirm'
  otpCode.value = ''
  verificationError.value = ''
  stopResendCountdown()

  emit('update:modelValue', false)
}

const startResendCountdown = () => {
  resendCountdown.value = 60 // 60 seconds

  resendTimer.value = setInterval(() => {
    resendCountdown.value--

    if (resendCountdown.value <= 0) {
      stopResendCountdown()
    }
  }, 1000)
}

const stopResendCountdown = () => {
  if (resendTimer.value) {
    clearInterval(resendTimer.value)
    resendTimer.value = null
  }
  resendCountdown.value = 0
}

// Watch for modal close
watch(isOpen, (newValue) => {
  if (!newValue) {
    // Clean up timer when modal is closed
    stopResendCountdown()
  }
})
</script>

<style scoped>
.phone-verification-content {
  padding: 1rem 0;
}

.verification-step {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.step-description {
  font-size: 0.9375rem;
  color: #4b5563;
  text-align: center;
  line-height: 1.6;
  margin: 0;
}

.step-description strong {
  color: #111827;
  font-weight: 600;
  direction: ltr;
  display: inline-block;
}

/* Buttons */
.button-group {
  display: flex;
  gap: 0.75rem;
  justify-content: center;
  margin-top: 0.5rem;
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.625rem 1.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  border-radius: 0.5rem;
  border: none;
  cursor: pointer;
  transition: all 0.2s ease;
  min-width: 100px;
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

.btn-primary:disabled {
  background-color: #d1d5db;
  cursor: not-allowed;
  transform: none;
}

.btn-secondary {
  background-color: #e5e7eb;
  color: #374151;
}

.btn-secondary:hover:not(:disabled) {
  background-color: #d1d5db;
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
  width: 100%;
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
  margin-top: -0.5rem;
}

.resend-countdown {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

/* Success Container */
.success-container {
  text-align: center;
  padding: 1rem 0;
}

.success-icon {
  width: 4rem;
  height: 4rem;
  margin: 0 auto 1rem;
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
  font-size: 1.25rem;
  font-weight: 700;
  color: #111827;
  margin: 0 0 0.5rem 0;
}

.success-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
  line-height: 1.5;
}
</style>
