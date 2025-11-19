/**
 * Auth Interceptor
 *
 * Handles JWT token injection and refresh logic
 */

import type { InternalAxiosRequestConfig } from 'axios'
import axios from 'axios'
import { microservices } from '../config/api-config'

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
 * Handles token refresh on 401 errors
 */
export async function authErrorInterceptor(error: unknown) {
  const errorObj = error as { config?: InternalAxiosRequestConfig & { _retry?: boolean }; response?: { status?: number } }
  const originalRequest = errorObj.config

  // If 401 and haven't retried yet
  if (errorObj.response?.status === 401 && originalRequest && !originalRequest._retry) {
    originalRequest._retry = true

    try {
      const refreshToken = localStorage.getItem('refresh_token')

      if (!refreshToken) {
        console.error('[Auth] No refresh token available')
        throw new Error('No refresh token available')
      }

      // Refresh token endpoint - use User Management API
      const userManagementBaseUrl = microservices.userManagement.baseURL
      console.log('[Auth] Refreshing token using:', `${userManagementBaseUrl}/v1/auth/refresh`)

      const response = await fetch(`${userManagementBaseUrl}/v1/auth/refresh`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify({ refreshToken })
      })

      console.log('[Auth] Refresh response status:', response.status)

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        console.error('[Auth] Token refresh failed:', errorData)
        throw new Error('Token refresh failed')
      }

      const data = await response.json()
      console.log('[Auth] Refresh response data:', data)

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
        console.error('[Auth] No access token in refresh response:', data)
        throw new Error('No access token in refresh response')
      }

      console.log('[Auth] Token refreshed successfully')

      // Update tokens
      localStorage.setItem('access_token', accessToken)
      if (newRefreshToken) {
        localStorage.setItem('refresh_token', newRefreshToken)
      }

      // Retry original request with new token
      originalRequest.headers.Authorization = `Bearer ${accessToken}`
      console.log('[Auth] Retrying original request with new token')
      return axios(originalRequest)

    } catch (refreshError) {
      // Clear tokens and redirect to login
      localStorage.removeItem('access_token')
      localStorage.removeItem('refresh_token')
      localStorage.removeItem('user')

      // Redirect to login (using correct route path)
      window.location.href = '/login'

      return Promise.reject(refreshError)
    }
  }

  return Promise.reject(error)
}
