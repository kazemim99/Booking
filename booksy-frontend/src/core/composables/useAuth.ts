import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useNotificationStore } from '@/core/stores/modules/notification.store'
import type { LoginCredentials, RegisterData } from '@/core/types/auth.types'

interface AuthErrorWithMessage {
  message?: string
}

export function useAuth() {
  const authStore = useAuthStore()
  const notificationStore = useNotificationStore()
  const router = useRouter()

  const isAuthenticated = computed(() => authStore.isAuthenticated)
  const user = computed(() => authStore.user)
  const isLoading = computed(() => authStore.isLoading)

  async function login(credentials: LoginCredentials): Promise<boolean> {
    try {
      await authStore.login(credentials)
      notificationStore.success('Welcome back!', `Hello ${authStore.user?.firstName}`)
      return true
    } catch (error: unknown) {
      const authError = error as AuthErrorWithMessage
      notificationStore.error('Login Failed', authError.message || 'An error occurred')
      return false
    }
  }

  async function register(data: RegisterData): Promise<boolean> {
    try {
      await authStore.register(data)
      notificationStore.success('Registration Successful', 'Welcome to Booksy!')
      return true
    } catch (error: unknown) {
      const authError = error as AuthErrorWithMessage
      notificationStore.error('Registration Failed', authError.message || 'An error occurred')
      return false
    }
  }

  async function logout(): Promise<void> {
    await authStore.logout()
    notificationStore.info('Logged out', 'You have been logged out successfully')
    router.push({ name: 'Login' })
  }

  function hasRole(role: string): boolean {
    return authStore.user?.roles?.includes(role) ?? false
  }

  function hasAnyRole(roles: string[]): boolean {
    return authStore.user?.roles?.some((role) => roles.includes(role)) ?? false
  }

  return {
    isAuthenticated,
    user,
    isLoading,
    login,
    register,
    logout,
    hasRole,
    hasAnyRole,
  }
}
