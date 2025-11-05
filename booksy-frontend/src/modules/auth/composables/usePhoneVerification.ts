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
      const response = await phoneVerificationApi.sendVerificationCode({
        phoneNumber,
        countryCode,
      })

      if (response.success && response.data) {
        // Update state
        state.value.phoneNumber = phoneNumber
        state.value.countryCode = countryCode
        state.value.maskedPhone = response.data.maskedPhoneNumber
        state.value.expiresIn = response.data.expiresIn
        state.value.step = 'otp'

        // Track if this is a new user (auto-registration)
        isNewUser.value = response.data.isNewUser || false

        // Start resend countdown
        startResendCountdown()

        return {
          success: true,
          maskedPhone: response.data.maskedPhoneNumber,
          isNewUser: isNewUser.value
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
   * Works for both login and registration flows
   * @param code - The OTP code to verify
   * @param phoneNumber - Optional phone number (if not provided, uses state.value.phoneNumber)
   */
  const verifyCode = async (code: string, phoneNumber?: string) => {
    state.value.isLoading = true
    state.value.error = null

    // Use provided phoneNumber or fall back to state
    const phoneToVerify = phoneNumber || state.value.phoneNumber

    if (!phoneToVerify) {
      const errorMessage = 'Phone number is required for verification'
      state.value.error = errorMessage
      toast.error(errorMessage)
      state.value.isLoading = false
      return { success: false, error: errorMessage }
    }

    try {
      const response = await phoneVerificationApi.verifyCode({
        phoneNumber: phoneToVerify,
        code,
        // For new users, set provider type by default (can be changed later)
        userType: 'Provider',
      })

      if (response.success && response.data?.success) {
        // Store auth token
        if (response.data.accessToken) {
          authStore.setToken(response.data.accessToken)
        }

        // Store user info
        if (response.data.user) {
          // Create a minimally required User object based on the UserInfo from phone verification
          authStore.setUser({
            id: response.data.user.id,
            email: response.data.user.phoneNumber + '@temp.booksy.com', // Temporary email for phone-only users
            phoneNumber: response.data.user.phoneNumber,
            phoneVerified: response.data.user.phoneVerified,
            userType: response.data.user.userType as UserType, // Using 'as any' to handle potential type mismatch
            roles: response.data.user.roles,
            // Required User properties
            lastModifiedAt: new Date().toISOString(),
            createdAt: new Date().toISOString(),
            status: response.data.user.status,
            // Empty required objects
            profile: {
              firstName: '',
              lastName: '',
            },
            preferences: {
              theme: 'light',
              notifications: {},
              language: 'en',
              timezone: 'UTC',
              currency: 'USD',
              dateFormat: 'MM/DD/YYYY',
              timeFormat: '12h',
              notificationSettings: {
                emailNotifications: true,
                smsNotifications: true,
                pushNotifications: false,
                appointmentReminders: true,
                promotionalEmails: false
              },
              privacySettings: undefined
            },
            metadata: {
              totalBookings: 0,
              completedBookings: 0,
              cancelledBookings: 0,
              noShows: 0,
              favoriteProviders: [],
              lastActivityAt: new Date().toISOString()
            }
          } as any)
        }

        state.value.step = 'success'

        // Show different toast message based on whether this was a login or registration
        if (isNewUser.value) {
          toast.success('Account created and verified successfully!')
        } else {
          toast.success('Phone verified successfully. Welcome back!')
        }

        return {
          success: true,
          user: response.data.user,
          token: response.data.accessToken || '',
          isNewUser: isNewUser.value
        }
      } else {
        // Update remaining attempts
        if (response.data?.remainingAttempts !== undefined) {
          state.value.remainingAttempts = response.data.remainingAttempts
        }

        const errorMessage =
          response.data?.errorMessage ||
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
