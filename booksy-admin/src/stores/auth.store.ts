import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type LoginRequest } from '../api/auth.api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('admin_token'))
  const refreshToken = ref<string | null>(localStorage.getItem('admin_refresh_token'))
  const storedUser = localStorage.getItem('admin_user')
  const user = ref<any>(storedUser ? JSON.parse(storedUser) : null)
  const loading = ref(false)

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() =>
    user.value?.roles?.includes('Admin') ||
    user.value?.roles?.includes('SuperAdmin') ||
    user.value?.roles?.includes('Administrator')
  )

  const login = async (credentials: LoginRequest) => {
    loading.value = true
    try {
      const response = await authApi.login(credentials)
      token.value = response.accessToken
      refreshToken.value = response.refreshToken
      user.value = response.userInfo
      localStorage.setItem('admin_token', response.accessToken)
      localStorage.setItem('admin_refresh_token', response.refreshToken)
      localStorage.setItem('admin_user', JSON.stringify(response.userInfo))

      console.log('Login successful, user data:', {
        user: response.userInfo,
        roles: response.userInfo.roles,
        isAdmin: response.userInfo.roles?.includes('Admin') ||
                 response.userInfo.roles?.includes('SuperAdmin') ||
                 response.userInfo.roles?.includes('Administrator')
      })

      return true
    } catch (error) {
      console.error('Login error:', error)
      return false
    } finally {
      loading.value = false
    }
  }

  const logout = async () => {
    try {
      await authApi.logout()
    } finally {
      token.value = null
      refreshToken.value = null
      user.value = null
      localStorage.removeItem('admin_token')
      localStorage.removeItem('admin_refresh_token')
      localStorage.removeItem('admin_user')
    }
  }

  const fetchCurrentUser = async () => {
    if (!token.value) return

    // Note: /Users/me endpoint doesn't exist yet in the API
    // User data is already populated from login response
    // This is a placeholder for when the endpoint is implemented
    if (user.value) {
      console.log('User data already loaded from login:', user.value)
      return
    }

    try {
      const userData = await authApi.getCurrentUser()
      user.value = userData
    } catch (error) {
      console.warn('Failed to fetch current user (endpoint may not exist):', error)
      // Don't clear tokens on error - user data from login is still valid
    }
  }

  const refreshAccessToken = async () => {
    if (!refreshToken.value) return false

    try {
      const response = await authApi.refreshToken(refreshToken.value)
      token.value = response.accessToken
      refreshToken.value = response.refreshToken
      localStorage.setItem('admin_token', response.accessToken)
      localStorage.setItem('admin_refresh_token', response.refreshToken)
      return true
    } catch (error) {
      console.error('Token refresh failed:', error)
      await logout()
      return false
    }
  }

  const initialize = async () => {
    if (token.value) {
      await fetchCurrentUser()
    }
  }

  return {
    token,
    refreshToken,
    user,
    loading,
    isAuthenticated,
    isAdmin,
    login,
    logout,
    fetchCurrentUser,
    refreshAccessToken,
    initialize,
  }
})
