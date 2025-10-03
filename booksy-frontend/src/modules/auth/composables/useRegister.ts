import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useNotification } from '@/core/composables/useNotification'
import type { RegisterData } from '@/core/types/auth.types'

export function useRegister() {
  const router = useRouter()
  const authStore = useAuthStore()
  const notification = useNotification()
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function register(data: RegisterData): Promise<boolean> {
    isLoading.value = true
    error.value = null

    try {
      const success = await authStore.register(data)

      if (success) {
        notification.success(
          'Registration Successful!',
          'Welcome to Booksy! Your account has been created.',
        )

        // ✅ Redirect based on user type
        if (data.userType === 'Provider') {
          // Provider users go to provider onboarding to complete their profile
          await router.push({
            name: 'ProviderOnboarding',
            query: { welcome: 'true' },
          })
        } else {
          // Customer users go to home page
          await router.push('/')
        }

        return true
      } else {
        // Registration failed - show error from store
        error.value = authStore.error || 'Registration failed. Please try again.'
        notification.error('Registration Failed', error.value)
        return false
      }
    } catch (err: unknown) {
      // Unexpected error
      const authError = err as { message?: string }
      error.value = authError.message || 'An unexpected error occurred'
      notification.error('Registration Failed', error.value)
      console.error('Unexpected registration error:', err)
      return false
    } finally {
      isLoading.value = false
    }
  }

  return {
    register,
    isLoading,
    error,
  }
}
