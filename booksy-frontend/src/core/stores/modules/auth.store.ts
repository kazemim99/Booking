// src/core/stores/modules/auth.store.ts
import { User } from '@/modules/user-management/types/user.types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { authApi } from '@/modules/auth/api/auth.api'
import { providerService } from '@/modules/provider/services/provider.service'
import { ProviderStatus } from '@/modules/provider/types/provider.types'

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

  // Provider status state
  const providerStatus = ref<ProviderStatus | null>(null)
  const providerId = ref<string | null>(null)

  // Getters
  const isAuthenticated = computed(() => !!token.value && !!user.value)
  const currentUser = computed(() => user.value)
  const userRoles = computed(() => user.value?.roles || [])
  const userName = computed(() => {
    if (!user.value) return 'Guest'
    return user.value.fullName || user.value.email || 'User'
  })

  // Provider status getters
  const needsProfileCompletion = computed(() => {
    return providerStatus.value === ProviderStatus.Drafted || providerStatus.value === null
  })

  const isPendingVerification = computed(() => {
    return providerStatus.value === ProviderStatus.PendingVerification
  })

  const isProviderActive = computed(() => {
    return providerStatus.value === ProviderStatus.Active || providerStatus.value === ProviderStatus.Verified
  })

  // Actions

  /**
   * Decode JWT token and extract provider information
   */
  function decodeTokenAndExtractProviderInfo(jwtToken: string) {
    try {
      // JWT structure: header.payload.signature
      const parts = jwtToken.split('.')
      if (parts.length !== 3) {
        console.warn('[AuthStore] Invalid JWT token format')
        return null
      }

      // Decode payload (base64url)
      const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')))

      // Extract providerId from token claims
      const providerId = payload.providerId || payload.provider_id
      const providerStatus = payload.provider_status

      console.log('[AuthStore] Decoded token payload:', { providerId, providerStatus })

      if (providerId) {
        return {
          providerId: providerId,
          providerStatus: providerStatus || null
        }
      }

      return null
    } catch (error) {
      console.error('[AuthStore] Failed to decode JWT token:', error)
      return null
    }
  }

  /**
   * Set authentication token
   */
  function setToken(newToken: string | null) {
    token.value = newToken
    if (newToken) {
      localStorage.setItem('auth_token', newToken)

      // Extract provider info from token
      const providerInfo = decodeTokenAndExtractProviderInfo(newToken)
      if (providerInfo) {
        console.log('[AuthStore] Provider info extracted from token:', providerInfo)
        setProviderStatus(providerInfo.providerStatus as ProviderStatus, providerInfo.providerId)
      }
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
   * Set provider status
   */
  function setProviderStatus(status: ProviderStatus | null, id: string | null) {
    providerStatus.value = status
    providerId.value = id
  }

  /**
   * Clear provider status
   */
  function clearProviderStatus() {
    providerStatus.value = null
    providerId.value = null
  }

  /**
   * Fetch provider status for current user
   * Called after login for Provider users
   */
  async function fetchProviderStatus(): Promise<void> {
    try {
      console.log('[AuthStore] Fetching provider status')
      const statusData = await providerService.getCurrentProviderStatus()

      if (statusData) {
        setProviderStatus(statusData.status, statusData.providerId)
        console.log('[AuthStore] Provider status set:', statusData.status)
      } else {
        // No provider record found - user needs to complete registration
        setProviderStatus(null, null)
        console.log('[AuthStore] No provider record found')
      }
    } catch (err) {
      console.error('[AuthStore] Failed to fetch provider status:', err)
      // Don't throw - we'll handle this gracefully in the UI
      setProviderStatus(null, null)
      throw err
    }
  }

  /**
   * Login user
   */
  async function login(credentials: { email: string; password: string; rememberMe?: boolean }): Promise<boolean> {
    try {
      setLoading(true)
      clearError()
      clearValidationErrors()

      const response = await authApi.login(credentials)

      if (!response.success || !response.data) {
        setError(response.error?.message || 'Login failed')
        if (response.error?.errors) {
          setValidationErrors(response.error.errors)
        }
        return false
      }

      const { tokens, user: userData } = response.data

      setToken(tokens.accessToken)
      setRefreshToken(tokens.refreshToken)

      // Transform API response to User type
      const user: User = {
        ...userData,
        email: userData.email || undefined,
        fullName: `${userData.profile.firstName} ${userData.profile.lastName}`,
        firstName: userData.profile.firstName,
        lastName: userData.profile.lastName,
        avatarUrl: userData.profile.avatarUrl,
        userType: 'Customer' as any, // Default, will be updated from profile
        preferences: {} as any, // Will be loaded separately
        metadata: {} as any, // Will be loaded separately
        updatedAt: userData.lastModifiedAt,
        status: userData.status as any,
        lastModifiedAt: userData.lastModifiedAt || new Date().toISOString(),
      }

      setUser(user)

      // Fetch provider status if user is a Provider
      if (userData.roles && (userData.roles.includes('Provider') || userData.roles.includes('ServiceProvider'))) {
        try {
          await fetchProviderStatus()
        } catch (err) {
          console.error('[AuthStore] Failed to fetch provider status after login:', err)
          // Don't fail login if provider status fetch fails
        }
      }

      return true
    } catch (err: unknown) {
      const error = err as { message?: string }
      setError(error.message || 'An unexpected error occurred during login')
      return false
    } finally {
      setLoading(false)
    }
  }

  /**
   * Register new user
   */
  async function register(data: { email: string; password: string; firstName: string; lastName: string; userType?: string }): Promise<boolean> {
    try {
      setLoading(true)
      clearError()
      clearValidationErrors()

      const registerRequest = {
        ...data,
        userType: data.userType || 'Customer'
      }

      const response = await authApi.register(registerRequest)

      if (!response.success || !response.data) {
        setError(response.error?.message || 'Registration failed')
        if (response.error?.errors) {
          setValidationErrors(response.error.errors)
        }
        return false
      }

      // Registration successful - user needs to login
      return true
    } catch (err: unknown) {
      const error = err as { message?: string }
      setError(error.message || 'An unexpected error occurred during registration')
      return false
    } finally {
      setLoading(false)
    }
  }

  /**
   * Redirect user to appropriate dashboard based on role and provider status
   */
  async function redirectToDashboard() {
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

    // Provider redirect - check status first
    if (roles.includes('Provider') || roles.includes('ServiceProvider')) {
      // Fetch provider status if not already loaded
      if (providerStatus.value === null && providerId.value === null) {
        try {
          await fetchProviderStatus()
        } catch (err) {
          console.error('[AuthStore] Error fetching provider status, redirecting to registration')
          router.push({ name: 'ProviderRegistration' })
          return
        }
      }

      // Redirect based on provider status
      if (providerStatus.value === ProviderStatus.Drafted || providerStatus.value === null) {
        // Provider needs to complete registration
        router.push({ name: 'ProviderRegistration' })
        return
      }

      // All other statuses (PendingVerification, Verified, Active, etc.) go to dashboard
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
      clearProviderStatus()
      // Clear provider data
      localStorage.removeItem('provider_id')

      router.push({ name: 'Login' })
    } catch (err: unknown) {
      console.error('Logout error:', err)
      setToken(null)
      setRefreshToken(null)
      setUser(null)
      clearProviderStatus()
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

        // Extract provider info from token
        const providerInfo = decodeTokenAndExtractProviderInfo(storedToken)
        if (providerInfo) {
          console.log('[AuthStore] Provider info loaded from stored token:', providerInfo)
          setProviderStatus(providerInfo.providerStatus as ProviderStatus, providerInfo.providerId)
        }
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
    providerStatus,
    providerId,

    // Getters
    isAuthenticated,
    currentUser,
    userRoles,
    userName,
    needsProfileCompletion,
    isPendingVerification,
    isProviderActive,

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
    setProviderStatus,
    clearProviderStatus,
    fetchProviderStatus,
    login,
    register,
    redirectToDashboard,
    logout,
    refresh,
    loadFromStorage,
    updateUser,
    isTokenExpired,
  }
})
