import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useToast } from '@/shared/composables/useToast'
import type { LoginCredentials } from '@/core/types/auth.types'
import type { ValidationErrors } from '@/core/types/auth.types'

/**
 * Composable for managing login state and functionality
 */
export function useLogin() {
  const router = useRouter()
  const authStore = useAuthStore()
  const toast = useToast()

  // State
  const isLoading = ref<boolean>(false)
  const error = ref<string | null>(null)
  const validationErrors = ref<ValidationErrors>({})

  // Computed
  const isAuthenticated = computed<boolean>(() => authStore.isAuthenticated)
  const currentUser = computed(() => authStore.user)

  /**
   * Attempts to log in with the provided credentials
   * @param credentials User login credentials (email, password, rememberMe)
   * @returns Promise<boolean> success state
   */
  async function login(credentials: LoginCredentials): Promise<boolean> {
    isLoading.value = true
    error.value = null
    validationErrors.value = {}

    try {
      // Attempt login through auth store
      const success = await authStore.login(credentials)

      if (success) {
        // On success, show toast notification and redirect
        const userName = authStore.user?.firstName || 'there'
        toast.success(`Welcome back, ${userName}!`)

        // Redirect to home or previous page
        const redirect = router.currentRoute.value.query.redirect as string | undefined
        await router.push(redirect || '/')
        return true
      } else {
        // Handle validation errors
        if (authStore.validationErrors && Object.keys(authStore.validationErrors).length > 0) {
          validationErrors.value = { ...authStore.validationErrors }

          // Format error message for display
          const errorMessages = formatValidationErrors(authStore.validationErrors)
          error.value = errorMessages
          toast.error(errorMessages)
        } else {
          // General error
          error.value = authStore.error || 'Login failed. Please try again.'
          toast.error(error.value)
        }
        return false
      }
    } catch (err: unknown) {
      // Handle unexpected errors
      handleUnexpectedError(err)
      return false
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Formats validation errors into a human-readable string
   * @param errors Validation errors object
   * @returns Formatted error string
   */
  function formatValidationErrors(errors: ValidationErrors): string {
    return Object.entries(errors)
      .map(([field, messages]) => {
        const fieldName = field.charAt(0).toUpperCase() + field.slice(1)
        return `${fieldName}: ${Array.isArray(messages) ? messages.join(', ') : messages}`
      })
      .join('\n')
  }

  /**
   * Handles unexpected errors during login
   * @param err Unknown error object
   */
  function handleUnexpectedError(err: unknown): void {
    const authError = err as { message?: string }
    error.value = authError.message || 'An unexpected error occurred'
    toast.error(error.value)
    console.error('Unexpected login error:', err)
  }

  /**
   * Clears all errors and validation messages
   */
  function clearErrors(): void {
    error.value = null
    validationErrors.value = {}
  }

  return {
    // State
    login,
    isLoading,
    error,
    validationErrors,
    isAuthenticated,
    currentUser,

    // Actions
    clearErrors,
  }
}
