import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useToast } from '@/shared/composables/useToast'
import phoneVerificationApi from '../api/phoneVerification.api'
import type { PhoneVerificationState } from '../types/phoneVerification.types'
// import { UserType } from '@/modules/user-management/types/user-profile.types'

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
  // Persist in sessionStorage to survive navigation
  const VERIFICATION_ID_KEY = 'phone_verification_id'
  const PHONE_NUMBER_KEY = 'phone_verification_number'

  const verificationId = ref<string>(sessionStorage.getItem(VERIFICATION_ID_KEY) || '')

  // Restore phone number from sessionStorage if available
  if (sessionStorage.getItem(PHONE_NUMBER_KEY)) {
    state.value.phoneNumber = sessionStorage.getItem(PHONE_NUMBER_KEY) || ''
  }

  // Resend state
  const canResend = ref(false)
  const resendCountdown = ref(60)

  // Registration tracking
  const isNewUser = ref(false)

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

      const response = await phoneVerificationApi.sendVerificationCode({
        phoneNumber: fullPhoneNumber,
        countryCode: countryCode,
      })

      if (response.success && response.data) {
        // Store verification ID for the verify step
        verificationId.value = response.data.verificationId

        // Persist to sessionStorage for navigation
        sessionStorage.setItem(VERIFICATION_ID_KEY, response.data.verificationId)
        sessionStorage.setItem(PHONE_NUMBER_KEY, fullPhoneNumber)

        // Calculate expiresIn (seconds) from expiresAt
        const expiresAt = new Date(response.data.expiresAt)
        const now = new Date()
        const expiresInSeconds = Math.floor((expiresAt.getTime() - now.getTime()) / 1000)

        // Update state
        state.value.phoneNumber = fullPhoneNumber
        state.value.countryCode = countryCode
        state.value.maskedPhone = response.data.maskedPhoneNumber
        state.value.expiresIn = expiresInSeconds
        state.value.step = 'otp'
        state.value.remainingAttempts = response.data.maxAttempts

        // Start resend countdown
        startResendCountdown()

        return {
          success: true,
          maskedPhone: response.data.maskedPhoneNumber,
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
   * Complete customer authentication (unified verify + login/register)
   * This replaces the old 2-step flow (verifyCode → registerFromVerifiedPhone)
   */
  const completeCustomerAuthentication = async (code: string, userInfo?: { firstName?: string; lastName?: string; email?: string }) => {
    state.value.isLoading = true
    state.value.error = null

    if (!state.value.phoneNumber) {
      const errorMessage = 'Phone number is missing. Please request a new code.'
      state.value.error = errorMessage
      toast.error(errorMessage)
      state.value.isLoading = false
      return { success: false, error: errorMessage }
    }

    try {
      const response = await phoneVerificationApi.completeCustomerAuthentication({
        phoneNumber: state.value.phoneNumber,
        code,
        firstName: userInfo?.firstName,
        lastName: userInfo?.lastName,
        email: userInfo?.email,
      })

      if (response.success && response.data) {
        state.value.step = 'success'

        // Clear sessionStorage
        sessionStorage.removeItem(VERIFICATION_ID_KEY)
        sessionStorage.removeItem(PHONE_NUMBER_KEY)

        toast.success(response.data.isNewCustomer ? 'ثبت‌نام شما با موفقیت انجام شد!' : 'ورود موفقیت‌آمیز بود!')

        // Store authentication tokens
        authStore.setToken(response.data.accessToken)
        authStore.setRefreshToken(response.data.refreshToken)

        // Store user info
        const now = new Date().toISOString()
        authStore.setUser({
          id: response.data.userId,
          email: response.data.email || undefined,
          phoneNumber: response.data.phoneNumber,
          phoneVerified: true,
          emailVerified: !!response.data.email,
          userType: 'Customer' as any,
          roles: ['Customer'],
          status: 'Active' as any,
          createdAt: now,
          updatedAt: now,
          lastModifiedAt: now,
          profile: {
            firstName: response.data.fullName?.split(' ')[0] || '',
            lastName: response.data.fullName?.split(' ').slice(1).join(' ') || '',
            phoneNumber: response.data.phoneNumber,
          },
          preferences: {
            theme: 'light',
            language: 'fa',
            timezone: 'Asia/Tehran',
            currency: 'IRR',
            dateFormat: 'YYYY/MM/DD',
            timeFormat: '24h',
            notifications: {
              email: true,
              sms: true,
              push: true,
            },
            notificationSettings: {
              emailNotifications: true,
              smsNotifications: true,
              pushNotifications: true,
              appointmentReminders: true,
              promotionalEmails: false,
            },
            privacySettings: undefined,
          },
          metadata: {
            totalBookings: 0,
            completedBookings: 0,
            cancelledBookings: 0,
            noShows: 0,
            favoriteProviders: [],
            lastActivityAt: now,
          },
        } as any)

        isNewUser.value = response.data.isNewCustomer

        return {
          success: true,
          isNewCustomer: response.data.isNewCustomer,
          userId: response.data.userId,
          customerId: response.data.customerId,
          phoneNumber: response.data.phoneNumber,
        }
      } else {
        const errorMessage =
          typeof response.error === 'string' ? response.error : 'خطا در تأیید کد'
        state.value.error = errorMessage
        toast.error(errorMessage)

        return {
          success: false,
          error: errorMessage,
        }
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'خطا در تأیید کد'
      state.value.error = errorMessage
      toast.error(errorMessage)

      return { success: false, error: errorMessage }
    } finally {
      state.value.isLoading = false
    }
  }

  /**
   * Complete provider authentication (unified verify + login/register)
   */
  const completeProviderAuthentication = async (code: string, userInfo?: { firstName?: string; lastName?: string; email?: string }) => {
    state.value.isLoading = true
    state.value.error = null

    if (!state.value.phoneNumber) {
      const errorMessage = 'Phone number is missing. Please request a new code.'
      state.value.error = errorMessage
      toast.error(errorMessage)
      state.value.isLoading = false
      return { success: false, error: errorMessage }
    }

    try {
      const response = await phoneVerificationApi.completeProviderAuthentication({
        phoneNumber: state.value.phoneNumber,
        code,
        firstName: userInfo?.firstName,
        lastName: userInfo?.lastName,
        email: userInfo?.email,
      })

      if (response.success && response.data) {
        state.value.step = 'success'

        // Clear sessionStorage
        sessionStorage.removeItem(VERIFICATION_ID_KEY)
        sessionStorage.removeItem(PHONE_NUMBER_KEY)

        toast.success(response.data.isNewProvider ? 'ثبت‌نام شما با موفقیت انجام شد!' : 'ورود موفقیت‌آمیز بود!')

        // Store authentication tokens
        authStore.setToken(response.data.accessToken)
        authStore.setRefreshToken(response.data.refreshToken)

        // Store user info
        const now = new Date().toISOString()
        authStore.setUser({
          id: response.data.userId,
          email: response.data.email || undefined,
          phoneNumber: response.data.phoneNumber,
          phoneVerified: true,
          emailVerified: !!response.data.email,
          userType: 'Provider' as any,
          roles: ['Provider'],
          status: 'Active' as any,
          createdAt: now,
          updatedAt: now,
          lastModifiedAt: now,
          profile: {
            firstName: response.data.fullName?.split(' ')[0] || '',
            lastName: response.data.fullName?.split(' ').slice(1).join(' ') || '',
            phoneNumber: response.data.phoneNumber,
          },
          preferences: {
            theme: 'light',
            language: 'fa',
            timezone: 'Asia/Tehran',
            currency: 'IRR',
            dateFormat: 'YYYY/MM/DD',
            timeFormat: '24h',
            notifications: {
              email: true,
              sms: true,
              push: true,
            },
            notificationSettings: {
              emailNotifications: true,
              smsNotifications: true,
              pushNotifications: true,
              appointmentReminders: true,
              promotionalEmails: false,
            },
            privacySettings: undefined,
          },
          metadata: {
            totalBookings: 0,
            completedBookings: 0,
            cancelledBookings: 0,
            noShows: 0,
            favoriteProviders: [],
            lastActivityAt: now,
          },
        } as any)

        isNewUser.value = response.data.isNewProvider

        return {
          success: true,
          isNewProvider: response.data.isNewProvider,
          userId: response.data.userId,
          providerId: response.data.providerId,
          providerStatus: response.data.providerStatus,
          requiresOnboarding: response.data.requiresOnboarding,
        }
      } else {
        const errorMessage =
          typeof response.error === 'string' ? response.error : 'خطا در تأیید کد'
        state.value.error = errorMessage
        toast.error(errorMessage)

        return {
          success: false,
          error: errorMessage,
        }
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'خطا در تأیید کد'
      state.value.error = errorMessage
      toast.error(errorMessage)

      return { success: false, error: errorMessage }
    } finally {
      state.value.isLoading = false
    }
  }

  /**
   * @deprecated Use completeCustomerAuthentication or completeProviderAuthentication instead
   * Verify OTP code (old 2-step flow)
   */
  const verifyCode = async (code: string) => {
    console.warn('verifyCode() is deprecated. Use completeCustomerAuthentication() or completeProviderAuthentication() instead.')
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
      const response = await phoneVerificationApi.verifyCode({
        verificationId: verificationId.value,
        code,
      })

      if (response.success && response.data?.success) {
        state.value.step = 'success'

        toast.success('Phone verified successfully!')

        return {
          success: true,
          phoneNumber: response.data.phoneNumber,
          verifiedAt: response.data.verifiedAt,
        }
      } else {
        if (response.data?.remainingAttempts !== undefined) {
          state.value.remainingAttempts = response.data.remainingAttempts
        }

        const errorMessage =
          response.data?.message ||
          (typeof response.error === 'string' ? response.error : 'Invalid verification code')
        state.value.error = errorMessage
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

    // Clear sessionStorage
    sessionStorage.removeItem(VERIFICATION_ID_KEY)
    sessionStorage.removeItem(PHONE_NUMBER_KEY)
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
              router.push({ name: 'ProviderRegistration' })
            } else {
              router.push({ path: '/provider/dashboard' })
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
    completeCustomerAuthentication,
    completeProviderAuthentication,
    verifyCode, // Deprecated - kept for backwards compatibility
    resendCode,
    reset,
    changePhoneNumber,
    redirectAfterVerification,
  }
}
