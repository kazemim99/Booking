import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useToast } from '@/shared/composables/useToast'
import type { RegisterData } from '@/core/types/auth.types'
import type { ValidationErrors } from '@/core/types/auth.types'
import { UserType } from '@/modules/user-management/types/user.types'

/**
 * Composable for managing user registration state and functionality
 */
export function useRegister() {
  const router = useRouter()
  const authStore = useAuthStore()
  const toast = useToast()

  // State
  const isLoading = ref<boolean>(false)
  const error = ref<string | null>(null)
  const validationErrors = ref<ValidationErrors>({})
  const registrationStep = ref<number>(1)
  const registrationSuccess = ref<boolean>(false)

  // Computed
  const isAuthenticated = computed<boolean>(() => authStore.isAuthenticated)
  const currentUser = computed(() => authStore.user)

  /**
   * Attempts to register a new user with the provided data
   * @param data User registration data
   * @returns Promise<boolean> success state
   */
  async function register(data: RegisterData): Promise<boolean> {
    isLoading.value = true
    error.value = null
    validationErrors.value = {}

    try {
      const success = await authStore.register(data)

      if (success) {
        // On success, show toast notification and handle redirect
        registrationSuccess.value = true
        toast.success('Registration Successful! Welcome to Booksy!')

        // Redirect based on user type
        await handlePostRegistrationRedirect(data.userType as UserType)
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
          error.value = authStore.error || 'Registration failed. Please try again.'
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
   * Handles post-registration navigation based on user type
   * @param userType Type of user being registered
   */
  async function handlePostRegistrationRedirect(userType: UserType): Promise<void> {
    if (userType === UserType.Provider) {
      // Provider users go to provider onboarding
      await router.push({
        name: 'ProviderOnboarding',
        query: { welcome: 'true' },
      })
    } else if (userType === UserType.Client) {
      // Customer users go to home page
      await router.push('/')
    } else {
      // Default redirect
      await router.push('/')
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
   * Handles unexpected errors during registration
   * @param err Unknown error object
   */
  function handleUnexpectedError(err: unknown): void {
    const authError = err as { message?: string }
    error.value = authError.message || 'An unexpected error occurred'
    toast.error(error.value)
    console.error('Unexpected registration error:', err)
  }

  /**
   * Clears all errors and validation messages
   */
  function clearErrors(): void {
    error.value = null
    validationErrors.value = {}
  }

  /**
   * Advances to the next registration step
   */
  function nextStep(): void {
    registrationStep.value++
  }

  /**
   * Goes back to the previous registration step
   */
  function prevStep(): void {
    if (registrationStep.value > 1) {
      registrationStep.value--
    }
  }

  return {
    // State
    register,
    isLoading,
    error,
    validationErrors,
    isAuthenticated,
    currentUser,
    registrationStep,
    registrationSuccess,

    // Actions
    clearErrors,
    nextStep,
    prevStep,
  }
}
