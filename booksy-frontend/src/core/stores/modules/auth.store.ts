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

  // Customer state
  const customerId = ref<string | null>(null)

  // Getters
  const isAuthenticated = computed(() => !!token.value && !!user.value)
  const currentUser = computed(() => user.value)
  const userRoles = computed(() => user.value?.roles || [])
  const userName = computed(() => {
    if (!user.value) return 'Guest'
    return user.value.firstName || user.value.email || 'User'
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
   * Decode JWT token and extract user information
   */
  function decodeToken(jwtToken: string) {
    try {
      // JWT structure: header.payload.signature
      const parts = jwtToken.split('.')
      if (parts.length !== 3) {
        console.warn('[AuthStore] Invalid JWT token format')
        return null
      }

      // Decode payload (base64url)
      const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')))

      console.log('[AuthStore] Decoded token payload:', payload)

      return {
        userId: payload.sub || payload.nameid || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
        email: payload.email || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
        userType: payload.user_type,
        roles: payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || [],
        providerId: payload.providerId || payload.provider_id,
        providerStatus: payload.provider_status,
        customerId: payload.customerId || payload.customer_id,
      }
    } catch (error) {
      console.error('[AuthStore] Failed to decode JWT token:', error)
      return null
    }
  }

  /**
   * Decode JWT token and extract provider information (for Provider users only)
   */
  function decodeTokenAndExtractProviderInfo(jwtToken: string) {
    const tokenData = decodeToken(jwtToken)
    if (!tokenData) return null

    // Only extract provider info if user has Provider role or is Provider type
    const roles = Array.isArray(tokenData.roles) ? tokenData.roles : [tokenData.roles]
    const isProvider = roles.includes('Provider') ||
                      roles.includes('ServiceProvider') ||
                      tokenData.userType === 'Provider'

    if (isProvider && tokenData.providerId) {
      return {
        providerId: tokenData.providerId,
        providerStatus: tokenData.providerStatus || null
      }
    }

    return null
  }

  /**
   * Decode JWT token and extract customer information (for Customer users)
   */
  function decodeTokenAndExtractCustomerInfo(jwtToken: string) {
    const tokenData = decodeToken(jwtToken)
    if (!tokenData) return null

    // Only extract customer info if user has Customer role or is Customer type
    const roles = Array.isArray(tokenData.roles) ? tokenData.roles : [tokenData.roles]
    const isCustomer = roles.includes('Customer') ||
                      roles.includes('Client') ||
                      tokenData.userType === 'Customer'

    if (isCustomer) {
      return {
        userId: tokenData.userId,
        customerId: tokenData.customerId || tokenData.customer_id,
        email: tokenData.email,
        userType: tokenData.userType,
        roles: roles
      }
    }

    return null
  }

  /**
   * Set authentication token
   */
  function setToken(newToken: string | null) {
    token.value = newToken
    if (newToken) {
      localStorage.setItem('access_token', newToken)

      // Decode token to determine user type
      const tokenData = decodeToken(newToken)

      if (tokenData) {
        const roles = Array.isArray(tokenData.roles) ? tokenData.roles : [tokenData.roles]
        const isProvider = roles.includes('Provider') ||
                          roles.includes('ServiceProvider') ||
                          tokenData.userType === 'Provider'
        const isCustomer = roles.includes('Customer') ||
                          roles.includes('Client') ||
                          tokenData.userType === 'Customer'

        console.log('[AuthStore] Token user type:', {
          userType: tokenData.userType,
          roles: roles,
          isProvider,
          isCustomer
        })

        // Extract provider info ONLY for providers
        if (isProvider) {
          const providerInfo = decodeTokenAndExtractProviderInfo(newToken)
          if (providerInfo) {
            console.log('[AuthStore] ✅ Provider info extracted from token:', providerInfo)
            setProviderStatus(providerInfo.providerStatus as ProviderStatus, providerInfo.providerId)
          } else {
            console.log('[AuthStore] ℹ️ Provider user but no provider profile yet')
            setProviderStatus(null, null)
          }
        } else if (isCustomer) {
          const customerInfo = decodeTokenAndExtractCustomerInfo(newToken)
          console.log('[AuthStore] ✅ Customer info extracted from token:', customerInfo)
          // For customers, explicitly set provider status to null (they don't have providers)
          setProviderStatus(null, null)
          // Store customerId
          if (customerInfo?.customerId) {
            customerId.value = customerInfo.customerId
            console.log('[AuthStore] ✅ CustomerId set:', customerInfo.customerId)
          }
        }
      }
    } else {
      localStorage.removeItem('access_token')
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
   * Enhanced with role-based redirect validation
   * @param redirectPath - Optional path to redirect to (e.g., from query param)
   */
  async function redirectToDashboard(redirectPath?: string) {
    if (!user.value || !user.value.roles) {
      router.push({ name: 'CustomerLogin' })
      return
    }

    const roles = user.value.roles

    console.log('[AuthStore] redirectToDashboard called', {
      roles,
      redirectPath,
      providerStatus: providerStatus.value,
      providerId: providerId.value
    })

    // Determine primary role (priority: Admin > Provider > Customer)
    let primaryRole: 'admin' | 'provider' | 'customer' | null = null
    if (roles.includes('Admin') || roles.includes('Administrator')) {
      primaryRole = 'admin'
    } else if (roles.includes('Provider') || roles.includes('ServiceProvider')) {
      primaryRole = 'provider'
    } else if (roles.includes('Customer') || roles.includes('Client')) {
      primaryRole = 'customer'
    }

    // Helper: Check if redirect path is valid for user role
    const isValidRedirect = (path: string, role: 'admin' | 'provider' | 'customer'): boolean => {
      if (!path || path === '/') return false

      if (role === 'admin') {
        return path.startsWith('/admin')
      }

      if (role === 'provider') {
        // Providers can only redirect to provider routes if onboarding complete
        const isOnboardingComplete = providerStatus.value !== null &&
                                     providerStatus.value !== ProviderStatus.Drafted
        if (!isOnboardingComplete) return false

        return path.startsWith('/dashboard') ||
               path.startsWith('/provider') ||
               path.startsWith('/bookings')
      }

      if (role === 'customer') {
        const publicRoutes = ['/', '/providers', '/search', '/about', '/contact']
        const isPublic = publicRoutes.some(route => path.startsWith(route))
        const isCustomerRoute = path.startsWith('/booking')
        return isPublic || isCustomerRoute
      }

      return false
    }

    // Admin redirect
    if (primaryRole === 'admin') {
      if (redirectPath && isValidRedirect(redirectPath, 'admin')) {
        console.log('[AuthStore] Admin redirect to:', redirectPath)
        await router.push(redirectPath)
      } else {
        console.log('[AuthStore] Admin redirect to default dashboard')
        await router.push({ path: '/admin/dashboard' })
      }
      return
    }

    // Provider redirect - check status first
    if (primaryRole === 'provider') {
      // Fetch provider status if not already loaded
      if (providerStatus.value === null && providerId.value === null) {
        try {
          console.log('[AuthStore] Fetching provider status before redirect...')
          await fetchProviderStatus()
        } catch (error) {
          console.error('[AuthStore] Error fetching provider status, redirecting to registration', error)
          await router.push({ name: 'ProviderRegistration' })
          return
        }
      }

      // Check if onboarding is complete
      const isOnboardingComplete = providerStatus.value !== null &&
                                   providerStatus.value !== ProviderStatus.Drafted

      if (!isOnboardingComplete) {
        // Provider needs to complete registration - ignore redirect path
        console.log('[AuthStore] Provider onboarding incomplete, forcing registration')
        await router.push({ name: 'ProviderRegistration' })
        return
      }

      // Onboarding complete - check redirect path
      if (redirectPath && isValidRedirect(redirectPath, 'provider')) {
        console.log('[AuthStore] Provider redirect to:', redirectPath)
        await router.push(redirectPath)
      } else {
        console.log('[AuthStore] Provider redirect to dashboard')
        await router.push({ path: '/dashboard' })
      }
      return
    }

    // Customer redirect
    if (primaryRole === 'customer') {
      if (redirectPath && isValidRedirect(redirectPath, 'customer')) {
        console.log('[AuthStore] Customer redirect to:', redirectPath)
        await router.push(redirectPath)
      } else {
        console.log('[AuthStore] Customer redirect to homepage')
        await router.push({ path: '/' })
      }
      return
    }

    // Fallback - no recognized role
    console.log('[AuthStore] No recognized role, redirecting to homepage')
    await router.push({ path: '/' })
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

      // Stay on current page if it's a public route (like Home)
      // Only redirect to Login if on a protected route
      const currentPath = router.currentRoute.value.path
      const publicRoutes = ['/', '/providers', '/search', '/about', '/contact']
      const isPublicRoute = publicRoutes.includes(currentPath) || currentPath.startsWith('/provider/')

      if (!isPublicRoute) {
        router.push({ name: 'CustomerLogin' })
      }
    } catch (err: unknown) {
      console.error('Logout error:', err)
      setToken(null)
      setRefreshToken(null)
      setUser(null)
      clearProviderStatus()

      // Stay on current page if it's a public route
      const currentPath = router.currentRoute.value.path
      const publicRoutes = ['/', '/providers', '/search', '/about', '/contact']
      const isPublicRoute = publicRoutes.includes(currentPath) || currentPath.startsWith('/provider/')

      if (!isPublicRoute) {
        router.push({ name: 'CustomerLogin' })
      }
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
      const storedToken = localStorage.getItem('access_token')
      const storedRefreshToken = localStorage.getItem('refresh_token')
      const storedUser = localStorage.getItem('user')

      if (storedToken) {
        token.value = storedToken

        // Decode token to determine user type
        const tokenData = decodeToken(storedToken)

        if (tokenData) {
          const roles = Array.isArray(tokenData.roles) ? tokenData.roles : [tokenData.roles]
          const isProvider = roles.includes('Provider') ||
                            roles.includes('ServiceProvider') ||
                            tokenData.userType === 'Provider'
          const isCustomer = roles.includes('Customer') ||
                            roles.includes('Client') ||
                            tokenData.userType === 'Customer'

          console.log('[AuthStore] Loading from storage - user type:', {
            userType: tokenData.userType,
            roles: roles,
            isProvider,
            isCustomer
          })

          // Extract provider info ONLY for providers
          if (isProvider) {
            const providerInfo = decodeTokenAndExtractProviderInfo(storedToken)
            if (providerInfo) {
              console.log('[AuthStore] ✅ Provider info loaded from stored token:', providerInfo)
              setProviderStatus(providerInfo.providerStatus as ProviderStatus, providerInfo.providerId)
            } else {
              console.log('[AuthStore] ℹ️ Provider user but no provider profile in token')
              setProviderStatus(null, null)
            }
          } else if (isCustomer) {
            const customerInfo = decodeTokenAndExtractCustomerInfo(storedToken)
            console.log('[AuthStore] ✅ Customer info loaded from stored token:', customerInfo)
            // For customers, explicitly set provider status to null
            setProviderStatus(null, null)
            // Store customerId
            if (customerInfo?.customerId) {
              customerId.value = customerInfo.customerId
              console.log('[AuthStore] ✅ CustomerId loaded:', customerInfo.customerId)
            }
          }
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
      localStorage.removeItem('access_token')
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
    customerId,

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
