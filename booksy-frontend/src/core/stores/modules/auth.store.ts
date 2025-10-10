// src/core/stores/modules/auth.store.ts
import { User } from '@/modules/user-management/types/user.types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { authApi } from '@/modules/auth/api/auth.api'

interface ValidationErrors {
  [key: string]: string[]
}

export const useAuthStore = defineStore('auth', () => {
  const router = useRouter()

  // State
  const token = ref<string | null>(null)
  const refreshToken = ref<string | null>(null)
  const user = ref<User | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const validationErrors = ref<ValidationErrors>({})

  // Getters
  const isAuthenticated = computed(() => !!token.value && !!user.value)
  const currentUser = computed(() => user.value)
  const userRoles = computed(() => user.value?.roles || [])
  const userName = computed(() => {
    if (!user.value) return 'Guest'
    return user.value.fullName || user.value.email || 'User'
  })

  // Actions

  /**
   * Set authentication token
   */
  function setToken(newToken: string | null) {
    token.value = newToken
    if (newToken) {
      localStorage.setItem('auth_token', newToken)
    } else {
      localStorage.removeItem('auth_token')
    }
  }

  /**
   * Set refresh token
   */
  function setRefreshToken(newRefreshToken: string | null) {
    refreshToken.value = newRefreshToken
    if (newRefreshToken) {
      localStorage.setItem('refresh_token', newRefreshToken)
    } else {
      localStorage.removeItem('refresh_token')
    }
  }

  /**
   * Set user data
   */
  function setUser(userData: User | null) {
    user.value = userData
    if (userData) {
      localStorage.setItem('user', JSON.stringify(userData))
    } else {
      localStorage.removeItem('user')
    }
  }

  /**
   * Set error message
   */
  function setError(errorMessage: string | null) {
    error.value = errorMessage
  }

  /**
   * Clear error message
   */
  function clearError() {
    error.value = null
  }

  /**
   * Set validation errors
   */
  function setValidationErrors(errors: ValidationErrors) {
    validationErrors.value = errors
  }

  /**
   * Clear validation errors
   */
  function clearValidationErrors() {
    validationErrors.value = {}
  }

  /**
   * Set loading state
   */
  function setLoading(loading: boolean) {
    isLoading.value = loading
  }

  /**
   * Check if user has a specific role
   */
  function hasRole(role: string): boolean {
    return userRoles.value.includes(role)
  }

  /**
   * Check if user has any of the specified roles
   */
  function hasAnyRole(roles: string[]): boolean {
    return roles.some((role) => userRoles.value.includes(role))
  }

  /**
   * Check if user has all of the specified roles
   */
  function hasAllRoles(roles: string[]): boolean {
    return roles.every((role) => userRoles.value.includes(role))
  }

  /**
   * Check if user has a specific permission
   */
  function hasPermission(permission: string): boolean {
    return user.value?.permissions?.includes(permission) ?? false
  }

  /**
   * Redirect user to appropriate dashboard based on role
   */
  function redirectToDashboard() {
    if (!user.value || !user.value.roles) {
      router.push({ name: 'Login' })
      return
    }

    const roles = user.value.roles

    // Admin redirect
    if (roles.includes('Admin') || roles.includes('Administrator') || roles.includes('SysAdmin')) {
      router.push({ path: '/admin/dashboard' })
      return
    }

    // Provider redirect
    if (roles.includes('Provider') || roles.includes('ServiceProvider')) {
      router.push({ path: '/provider/dashboard' })
      return
    }

    // Customer/Client redirect
    if (roles.includes('Customer') || roles.includes('Client')) {
      router.push({ path: '/customer/dashboard' })
      return
    }

    // Default fallback
    router.push({ path: '/' })
  }




  /**
   * Logout user
   */
  async function logout() {
    try {
      setLoading(true)

      // Call logout endpoint
      await authApi.logout()

      setToken(null)
      setRefreshToken(null)
      setUser(null)
      clearError()
      clearValidationErrors()
      // Clear provider data
      localStorage.removeItem('provider_id')

      router.push({ name: 'Login' })
    } catch (err: unknown) {
      console.error('Logout error:', err)
      setToken(null)
      setRefreshToken(null)
      setUser(null)
      // Clear provider data
      localStorage.removeItem('provider_id')
      router.push({ name: 'Login' })
    } finally {
      setLoading(false)
    }
  }

  /**
   * Refresh authentication token
   */
  async function refresh() {
    try {
      if (!refreshToken.value) {
        throw new Error('No refresh token available')
      }

      const response = await authApi.refreshToken({
        refreshToken: refreshToken.value,
      })

      if (!response.success) {
        throw new Error('Token refresh failed')
      }

      const { tokens } = response.data!

      setToken(tokens.accessToken)
      setRefreshToken(tokens.refreshToken)

      return true
    } catch (err: unknown) {
      console.error('Token refresh failed:', err)
      await logout()
      return false
    }
  }

  /**
   * Load authentication state from storage
   */
  function loadFromStorage() {
    try {
      const storedToken = localStorage.getItem('auth_token')
      const storedRefreshToken = localStorage.getItem('refresh_token')
      const storedUser = localStorage.getItem('user')

      if (storedToken) {
        token.value = storedToken
      }

      if (storedRefreshToken) {
        refreshToken.value = storedRefreshToken
      }

      if (storedUser) {
        user.value = JSON.parse(storedUser)
      }
    } catch (err) {
      console.error('Failed to load auth state from storage:', err)
      localStorage.removeItem('auth_token')
      localStorage.removeItem('refresh_token')
      localStorage.removeItem('user')
    }
  }

  /**
   * Update user profile
   */
  function updateUser(updates: Partial<User>) {
    if (user.value) {
      user.value = { ...user.value, ...updates }
      localStorage.setItem('user', JSON.stringify(user.value))
    }
  }

  /**
   * Check if token is expired (basic check)
   */
  function isTokenExpired(): boolean {
    if (!token.value) return true

    try {
      const payload = JSON.parse(atob(token.value.split('.')[1]))
      const exp = payload.exp * 1000
      return Date.now() >= exp
    } catch {
      return true
    }
  }

  // Initialize store from storage on creation
  loadFromStorage()

  return {
    // State
    token,
    refreshToken,
    user,
    isLoading,
    error,
    validationErrors,

    // Getters
    isAuthenticated,
    currentUser,
    userRoles,
    userName,

    // Actions
    setToken,
    setRefreshToken,
    setUser,
    setError,
    clearError,
    setValidationErrors,
    clearValidationErrors,
    setLoading,
    hasRole,
    hasAnyRole,
    hasAllRoles,
    hasPermission,
    redirectToDashboard,
    logout,
    refresh,
    loadFromStorage,
    updateUser,
    isTokenExpired,
  }
})
