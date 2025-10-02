// src/core/stores/modules/auth.store.ts
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { User } from '@/core/types/auth.types'

interface ValidationErrors {
  [key: string]: string[]
}

export const useAuthStore = defineStore('auth', () => {
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
    validationErrors.value = {}
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
    if (!user.value || !user.value.roles) return false
    return user.value.roles.includes(role)
  }

  /**
   * Check if user has any of the specified roles
   */
  function hasAnyRole(roles: string[]): boolean {
    if (!user.value || !user.value.roles) return false
    return roles.some((role) => user.value!.roles!.includes(role))
  }

  /**
   * Check if user has all of the specified roles
   */
  function hasAllRoles(roles: string[]): boolean {
    if (!user.value || !user.value.roles) return false
    return roles.every((role) => user.value!.roles!.includes(role))
  }

  /**
   * Check if user has a specific permission
   */
  function hasPermission(permission: string): boolean {
    if (!user.value || !user.value.permissions) return false
    return user.value.permissions.includes(permission)
  }

  /**
   * Login user
   */
  async function login(credentials: { email: string; password: string; rememberMe?: boolean }) {
    try {
      setLoading(true)
      clearError()
      clearValidationErrors()

      // TODO: Replace with actual API call
      // const response = await authApi.login(credentials)

      // Mock response for demonstration
      const response = {
        token: 'mock-token-' + Date.now(),
        refreshToken: 'mock-refresh-token-' + Date.now(),
        user: {
          id: '1',
          email: credentials.email,
          fullName: 'John Doe',
          roles: ['User'],
          permissions: ['read:profile', 'update:profile'],
          avatarUrl: null,
          status: 'Active',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        } as User,
      }

      setToken(response.token)
      setRefreshToken(response.refreshToken)
      setUser(response.user)

      return true
    } catch (err: unknown) {
      let errorMessage = 'Login failed'
      if (typeof err === 'object' && err !== null) {
        // @ts-expect-error: dynamic error shape
        errorMessage = err.response?.data?.message || err.message || 'Login failed'
        // @ts-expect-error: dynamic error shape
        if (err.response?.data?.errors) {
          // @ts-expect-error: dynamic error shape
          setValidationErrors(err.response.data.errors)
        }
      }
      setError(errorMessage)
      return false
    } finally {
      setLoading(false)
    }
  }

  /**
   * Register new user
   */
  async function register(userData: {
    email: string
    password: string
    firstName: string
    lastName: string
    phoneNumber?: string
    userType: string
  }) {
    try {
      setLoading(true)
      clearError()
      clearValidationErrors()

      // TODO: Replace with actual API call
      // const response = await authApi.register(userData)

      // Mock response for demonstration
      const response = {
        token: 'mock-token-' + Date.now(),
        refreshToken: 'mock-refresh-token-' + Date.now(),
        user: {
          id: '1',
          email: userData.email,
          fullName: `${userData.firstName} ${userData.lastName}`,
          roles: [userData.userType === 'Provider' ? 'Provider' : 'Customer'],
          permissions: ['read:profile', 'update:profile'],
          avatarUrl: null,
          status: 'Active',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        } as User,
      }

      setToken(response.token)
      setRefreshToken(response.refreshToken)
      setUser(response.user)

      return true
    } catch (err: unknown) {
      let errorMessage = 'Registration failed'
      if (typeof err === 'object' && err !== null) {
        // @ts-expect-error: dynamic error shape
        errorMessage = err.response?.data?.message || err.message || 'Registration failed'
        // @ts-expect-error: dynamic error shape
        if (err.response?.data?.errors) {
          // @ts-expect-error: dynamic error shape
          setValidationErrors(err.response.data.errors)
        }
      }
      setError(errorMessage)

      return false
    } finally {
      setLoading(false)
    }
  }

  /**
   * Logout user
   */
  async function logout() {
    try {
      setLoading(true)

      // TODO: Call API to invalidate token if needed
      // await authApi.logout()

      setToken(null)
      setRefreshToken(null)
      setUser(null)
      clearError()
      clearValidationErrors()
    } catch (err: unknown) {
      console.error('Logout error:', err)
      // Clear local state anyway
      setToken(null)
      setRefreshToken(null)
      setUser(null)
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

      // TODO: Replace with actual API call
      // const response = await authApi.refresh(refreshToken.value)

      // Mock response
      const response = {
        token: 'new-mock-token-' + Date.now(),
        refreshToken: 'new-mock-refresh-token-' + Date.now(),
      }

      setToken(response.token)
      setRefreshToken(response.refreshToken)

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
      // Clear corrupted data
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
      // Decode JWT token (basic implementation)
      const payload = JSON.parse(atob(token.value.split('.')[1]))
      const exp = payload.exp * 1000 // Convert to milliseconds
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
    login,
    register,
    logout,
    refresh,
    loadFromStorage,
    updateUser,
    isTokenExpired,
  }
})
