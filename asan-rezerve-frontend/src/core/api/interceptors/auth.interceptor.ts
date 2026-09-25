/**
 * Auth Interceptor
 *
 * Handles JWT token injection and refresh logic
 */

import type { InternalAxiosRequestConfig } from 'axios'
import axios from 'axios'
import { microservices } from '../config/api-config'

// Diagnostic logging is dev-only and never includes tokens or response bodies —
// access/refresh tokens are secrets and must not be written to the console.
const isDev = Boolean(import.meta.env?.DEV)

// Concurrent-refresh guard: a single in-flight refresh shared by all callers. Without
// this, parallel 401s each POST /refresh; because the refresh token rotates on every
// call, the second refresh would use an already-consumed token and log the user out.
let refreshPromise: Promise<string> | null = null

/**
 * Adds JWT token to request headers
 */
export function authInterceptor(config: InternalAxiosRequestConfig): InternalAxiosRequestConfig {
  const token = localStorage.getItem('access_token')

  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
}

/**
 * Performs a single token refresh and stores the rotated tokens.
 * Returns the new access token. Throws on any failure.
 */
async function performTokenRefresh(): Promise<string> {
  const refreshToken = localStorage.getItem('refresh_token')

  if (!refreshToken) {
    throw new Error('No refresh token available')
  }

  // Refresh token endpoint - use User Management API
  const userManagementBaseUrl = microservices.userManagement.baseURL

  const response = await fetch(`${userManagementBaseUrl}/v1/Auth/refresh`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    },
    body: JSON.stringify({ refreshToken })
  })

  if (!response.ok) {
    throw new Error(`Token refresh failed (HTTP ${response.status})`)
  }

  const data = await response.json()

  // Extract tokens - handle both nested data.data and flat data structure
  // Also handle PascalCase from .NET API (since we're using fetch, not axios)
  const accessToken =
    data.data?.accessToken ||
    data.data?.AccessToken ||
    data.accessToken ||
    data.AccessToken

  const newRefreshToken =
    data.data?.refreshToken ||
    data.data?.RefreshToken ||
    data.refreshToken ||
    data.RefreshToken

  if (!accessToken) {
    throw new Error('No access token in refresh response')
  }

  // Update tokens
  localStorage.setItem('access_token', accessToken)
  if (newRefreshToken) {
    localStorage.setItem('refresh_token', newRefreshToken)
  }

  return accessToken
}

/**
 * Handles token refresh on 401 errors
 */
export async function authErrorInterceptor(error: unknown) {
  const errorObj = error as { config?: InternalAxiosRequestConfig & { _retry?: boolean }; response?: { status?: number } }
  const originalRequest = errorObj.config

  // If 401 and haven't retried yet
  if (errorObj.response?.status === 401 && originalRequest && !originalRequest._retry) {
    originalRequest._retry = true

    try {
      // Coalesce concurrent 401s into one refresh; clear the slot when it settles.
      if (!refreshPromise) {
        refreshPromise = performTokenRefresh().finally(() => { refreshPromise = null })
      }

      const accessToken = await refreshPromise

      // Retry original request with new token
      originalRequest.headers.Authorization = `Bearer ${accessToken}`
      if (isDev) console.log('[Auth] Token refreshed; retrying original request')
      return axios(originalRequest)

    } catch (refreshError) {
      if (isDev) console.error('[Auth] Token refresh failed; signing out')
      // Determine user role before clearing tokens
      let userRole: 'provider' | 'customer' = 'customer'
      try {
        const token = localStorage.getItem('access_token')
        if (token) {
          const payload = JSON.parse(atob(token.split('.')[1]))
          const roles = Array.isArray(payload.role) ? payload.role : [payload.role]
          const isProvider = roles.includes('Provider') ||
                            roles.includes('ServiceProvider') ||
                            payload.user_type === 'Provider'
          if (isProvider) {
            userRole = 'provider'
          }
        }
      } catch (err) {
        if (isDev) console.error('[Auth] Error detecting user role:', err)
      }

      // Clear tokens and redirect to appropriate login page
      localStorage.removeItem('access_token')
      localStorage.removeItem('refresh_token')
      localStorage.removeItem('user')

      // Redirect to role-specific login page
      const loginPath = userRole === 'provider' ? '/provider/login' : '/customer/login'
      const currentPath = window.location.pathname

      // Add redirect query param if not already on a login page
      if (!currentPath.includes('/login')) {
        window.location.href = `${loginPath}?redirect=${encodeURIComponent(currentPath)}`
      } else {
        window.location.href = loginPath
      }

      return Promise.reject(refreshError)
    }
  }

  return Promise.reject(error)
}
