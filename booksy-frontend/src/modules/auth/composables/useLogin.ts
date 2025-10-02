import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useNotification } from '@/core/composables/useNotification'
import type { LoginCredentials } from '@/core/types/auth.types'

export function useLogin() {
  const router = useRouter()
  const authStore = useAuthStore()
  const notification = useNotification()
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function login(credentials: LoginCredentials): Promise<boolean> {
    isLoading.value = true
    error.value = null

    try {
      const success = await authStore.login(credentials)

      if (success) {
        notification.success('Welcome back!', `Hello ${authStore.user?.firstName || 'there'}!`)

        // Redirect to home or previous page
        const redirect = router.currentRoute.value.query.redirect as string
        await router.push(redirect || '/')

        return true
      } else {
        // Login failed - show error from store
        error.value = authStore.error || 'Login failed. Please try again.'
        notification.error('Login Failed', error.value)
        return false
      }
    } catch (err: unknown) {
      // Unexpected error
      const authError = err as { message?: string }
      error.value = authError.message || 'An unexpected error occurred'
      notification.error('Login Failed', error.value)
      console.error('Unexpected login error:', err)
      return false
    } finally {
      isLoading.value = false
    }
  }

  return {
    login,
    isLoading,
    error,
  }
}
