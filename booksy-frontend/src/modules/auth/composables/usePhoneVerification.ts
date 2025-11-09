import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useToast } from '@/shared/composables/useToast'
import phoneVerificationApi from '../api/phoneVerification.api'
import type { PhoneVerificationState } from '../types/phoneVerification.types'
import { UserType } from '@/modules/user-management/types/user-profile.types'

/**
 * Composable for handling phone verification flow (both login and registration)
 */
export function usePhoneVerification() {
  const router = useRouter()
  const authStore = useAuthStore()
  const toast = useToast()

  // State
  const state = ref<PhoneVerificationState>({
    phoneNumber: '',
    countryCode: 'DE',
    maskedPhone: '',
    expiresIn: 300,
    isLoading: false,
    error: null,
    step: 'phone',
    remainingAttempts: 3,
  })

  // Verification ID from backend (required for verify step)
  const verificationId = ref<string>('')

  // Resend state
  const canResend = ref(false)
  const resendCountdown = ref(60)

  // Registration tracking
  const isNewUser = ref(false)

  // Development mode settings
  const isDevMode = import.meta.env.VITE_DEV_MODE === 'true'
  const devOtpCode = import.meta.env.VITE_DEV_OTP_CODE || '123456'
  const skipRealSms = import.meta.env.VITE_SKIP_REAL_SMS === 'true'

  /**
   * Send verification code to phone number
   * This function works for both login and registration
   */
  const sendVerificationCode = async (phoneNumber: string, countryCode: string) => {
    state.value.isLoading = true
    state.value.error = null

    try {
      // Format phone number with country code
      const fullPhoneNumber = phoneNumber.startsWith('+') ? phoneNumber : `${countryCode}${phoneNumber}`

      // 🔧 DEVELOPMENT MODE: Skip real SMS
      if (isDevMode && skipRealSms) {
        console.log(`🔧 DEV MODE: Skipping real SMS. Use code: ${devOtpCode}`)

        // Simulate backend response
        await new Promise(resolve => setTimeout(resolve, 500)) // Simulate network delay

        verificationId.value = 'dev-verification-id-' + Date.now()

        state.value.phoneNumber = fullPhoneNumber
        state.value.countryCode = countryCode
        state.value.maskedPhone = fullPhoneNumber.slice(0, -4).replace(/./g, '*') + fullPhoneNumber.slice(-4)
        state.value.expiresIn = 300
        state.value.step = 'otp'
        state.value.remainingAttempts = 3

        startResendCountdown()

        toast.success(`DEV MODE: Code is ${devOtpCode}`)

        return {
          success: true,
          maskedPhone: state.value.maskedPhone,
          isNewUser: false
        }
      }

      // 📱 PRODUCTION: Real SMS via backend
      const response = await phoneVerificationApi.sendVerificationCode({
        phoneNumber: fullPhoneNumber,
        method: 'SMS',
        purpose: 'Registration',
      })

      if (response.success && response.data) {
        // Store verification ID for the verify step
        verificationId.value = response.data.verificationId

        // Calculate expiresIn (seconds) from expiresAt
        const expiresAt = new Date(response.data.expiresAt)
        const now = new Date()
        const expiresInSeconds = Math.floor((expiresAt.getTime() - now.getTime()) / 1000)

        // Update state
        state.value.phoneNumber = fullPhoneNumber
        state.value.countryCode = countryCode
        state.value.maskedPhone = response.data.phoneNumber // Already masked by backend
        state.value.expiresIn = expiresInSeconds
        state.value.step = 'otp'
        state.value.remainingAttempts = response.data.maxAttempts

        // Start resend countdown
        startResendCountdown()

        return {
          success: true,
          maskedPhone: response.data.phoneNumber,
          isNewUser: false // Backend doesn't tell us this yet, will be determined during verification
        }
      } else {
        throw new Error(typeof response.error === 'string' ? response.error : 'Failed to send verification code')
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'Failed to send code'
      state.value.error = errorMessage
      return { success: false, error: errorMessage }
    } finally {
      state.value.isLoading = false
    }
  }

  /**
   * Verify OTP code
   * NOTE: Backend phone verification only confirms the phone number is valid.
   * After successful verification, user must complete registration or login separately.
   * @param code - The 6-digit OTP code to verify
   */
  const verifyCode = async (code: string) => {
    state.value.isLoading = true
    state.value.error = null

    if (!verificationId.value) {
      const errorMessage = 'Verification ID is missing. Please request a new code.'
      state.value.error = errorMessage
      toast.error(errorMessage)
      state.value.isLoading = false
      return { success: false, error: errorMessage }
    }

    try {
      // 🔧 DEVELOPMENT MODE: Accept dev OTP code
      if (isDevMode && skipRealSms) {
        // Simulate network delay
        await new Promise(resolve => setTimeout(resolve, 300))

        if (code === devOtpCode) {
          state.value.step = 'success'
          toast.success('Phone verified successfully! (DEV MODE)')

          return {
            success: true,
            phoneNumber: state.value.phoneNumber,
            verifiedAt: new Date().toISOString(),
          }
        } else {
          state.value.remainingAttempts--
          const errorMessage = `Invalid code. Use ${devOtpCode} in dev mode. Remaining attempts: ${state.value.remainingAttempts}`
          state.value.error = errorMessage
          toast.error(errorMessage)

          return {
            success: false,
            error: errorMessage,
            remainingAttempts: state.value.remainingAttempts,
          }
        }
      }

      // 📱 PRODUCTION: Real verification via backend
      const response = await phoneVerificationApi.verifyCode({
        verificationId: verificationId.value,
        code,
      })

      if (response.success && response.data?.success) {
        state.value.step = 'success'

        toast.success('Phone verified successfully!')

        // TODO: After phone verification, implement actual login/registration flow
        // The backend separates phone verification from authentication
        // Next steps would be to either:
        // 1. Call a separate registration endpoint if this is a new user
        // 2. Call a login endpoint if this is an existing user
        // For now, we just confirm the phone is verified

        return {
          success: true,
          phoneNumber: response.data.phoneNumber,
          verifiedAt: response.data.verifiedAt,
        }
      } else {
        // Update remaining attempts
        if (response.data?.remainingAttempts !== undefined) {
          state.value.remainingAttempts = response.data.remainingAttempts
        }

        const errorMessage =
          response.data?.message ||
          (typeof response.error === 'string' ? response.error : 'Invalid verification code')
        state.value.error = errorMessage

        // Show toast for error
        toast.error(errorMessage)

        return {
          success: false,
          error: errorMessage,
          remainingAttempts: state.value.remainingAttempts,
        }
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'Verification failed'
      state.value.error = errorMessage

      // Show toast for error
      toast.error(errorMessage)

      return { success: false, error: errorMessage }
    } finally {
      state.value.isLoading = false
    }
  }

  /**
   * Resend verification code
   */
  const resendCode = async () => {
    if (!canResend.value) return

    const result = await sendVerificationCode(state.value.phoneNumber, state.value.countryCode)

    if (result.success) {
      // Reset attempts
      state.value.remainingAttempts = 3
      state.value.error = null
      toast.success('Verification code resent')
    }

    return result
  }

  /**
   * Start countdown for resend button
   */
  const startResendCountdown = () => {
    canResend.value = false
    resendCountdown.value = 60

    const interval = setInterval(() => {
      resendCountdown.value--

      if (resendCountdown.value <= 0) {
        canResend.value = true
        clearInterval(interval)
      }
    }, 1000)
  }

  /**
   * Reset verification state
   */
  const reset = () => {
    state.value = {
      phoneNumber: '',
      countryCode: 'DE',
      maskedPhone: '',
      expiresIn: 300,
      isLoading: false,
      error: null,
      step: 'phone',
      remainingAttempts: 3,
    }
    verificationId.value = ''
    canResend.value = false
    resendCountdown.value = 60
    isNewUser.value = false
  }

  /**
   * Change phone number (go back to phone input step)
   */
  const changePhoneNumber = () => {
    state.value.step = 'phone'
    state.value.error = null
    state.value.remainingAttempts = 3
  }

  /**
   * Redirect after successful verification
   * For new users: go to provider registration
   * For existing users: check if they have a provider profile
   */
  const redirectAfterVerification = async () => {
    if (isNewUser.value) {
      // New users must complete provider registration first
      router.push({
        name: 'ProviderRegistration'
      })
    } else {
      // Existing users: check if they have a provider profile
      const userType = authStore.user?.userType
      if (userType === 'Provider') {
        // For providers, check if they have completed provider registration
        try {
          const providerStore = await import('@/modules/provider/stores/provider.store')
          await providerStore.useProviderStore().loadCurrentProvider()
          const hasProvider = !!providerStore.useProviderStore().currentProvider

          if (hasProvider) {
            // Provider profile exists, check if onboarding is complete
            const provider = providerStore.useProviderStore().currentProvider
            const needsOnboarding = !provider?.profile?.businessName ||
              !provider?.businessHours?.length ||
              !provider?.services?.length

            if (needsOnboarding) {
              router.push({ name: 'ProviderOnboarding' })
            } else {
              router.push({ path: '/dashboard' })
            }
          } else {
            // No provider profile, redirect to registration
            router.push({ name: 'ProviderRegistration' })
          }
        } catch (error) {
          console.error('Error checking provider status:', error)
          // On error, default to registration
          router.push({ name: 'ProviderRegistration' })
        }
      } else if (userType === 'Admin') {
        router.push({ path: '/admin/dashboard' })
      } else {
        // Default for customers or unknown types
        router.push({ path: '/' })
      }
    }
  }

  return {
    // State
    state: computed(() => state.value),
    canResend: computed(() => canResend.value),
    resendCountdown: computed(() => resendCountdown.value),
    isNewUser: computed(() => isNewUser.value),

    // Actions
    sendVerificationCode,
    verifyCode,
    resendCode,
    reset,
    changePhoneNumber,
    redirectAfterVerification,
  }
}
