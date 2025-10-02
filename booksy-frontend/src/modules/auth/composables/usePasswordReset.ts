import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useNotification } from '@/core/composables/useNotification'
import authApi from '../api/auth.api'
import type { ForgotPasswordRequest, ResetPasswordRequest } from '../types/auth-response.types'

export function usePasswordReset() {
  const router = useRouter()
  const notification = useNotification()
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const emailSent = ref(false)

  async function requestPasswordReset(data: ForgotPasswordRequest): Promise<boolean> {
    isLoading.value = true
    error.value = null
    emailSent.value = false

    try {
      const response = await authApi.forgotPassword(data)

      if (response.success) {
        emailSent.value = true
        notification.success(
          'Email Sent',
          'Password reset instructions have been sent to your email.',
        )
        return true
      }

      throw new Error(response.message || 'Failed to send reset email')
    } catch (err: unknown) {
      const authError = err as { message?: string }
      error.value = authError.message || 'Failed to send reset email. Please try again.'
      notification.error('Request Failed', error.value)
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function resetPassword(data: ResetPasswordRequest): Promise<boolean> {
    isLoading.value = true
    error.value = null

    try {
      const response = await authApi.resetPassword(data)

      if (response.success) {
        notification.success(
          'Password Reset Successful',
          'Your password has been reset. You can now login with your new password.',
        )

        // Redirect to login
        await router.push({ name: 'Login' })
        return true
      }

      throw new Error(response.message || 'Failed to reset password')
    } catch (err: unknown) {
      const authError = err as { message?: string }
      error.value = authError.message || 'Failed to reset password. Please try again.'
      notification.error('Reset Failed', error.value)
      return false
    } finally {
      isLoading.value = false
    }
  }

  return {
    requestPasswordReset,
    resetPassword,
    isLoading,
    error,
    emailSent,
  }
}
