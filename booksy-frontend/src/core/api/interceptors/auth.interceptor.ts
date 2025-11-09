/**
 * Auth Interceptor
 *
 * Handles JWT token injection and refresh logic
 */

import type { InternalAxiosRequestConfig } from 'axios'
import axios from 'axios'
import { localStorageService } from '@/core/services/storage/local-storage.service'

/**
 * Adds JWT token to request headers
 */
export function authInterceptor(config: InternalAxiosRequestConfig): InternalAxiosRequestConfig {
  const token = localStorageService.get<string>('access_token')

  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
}

/**
 * Handles token refresh on 401 errors
 */
export async function authErrorInterceptor(error: any) {
  const originalRequest = error.config

  // If 401 and haven't retried yet
  if (error.response?.status === 401 && !originalRequest._retry) {
    originalRequest._retry = true

    try {
      const refreshToken = localStorageService.get<string>('refresh_token')

      if (!refreshToken) {
        throw new Error('No refresh token available')
      }

      // Refresh token endpoint
      const response = await fetch(`${import.meta.env.VITE_API_BASE_URL}/api/v1/auth/refresh`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify({ refreshToken })
      })

      if (!response.ok) {
        throw new Error('Token refresh failed')
      }

      const data = await response.json()
      const { accessToken, refreshToken: newRefreshToken } = data.data || data

      // Update tokens
      localStorageService.set('access_token', accessToken)
      if (newRefreshToken) {
        localStorageService.set('refresh_token', newRefreshToken)
      }

      // Retry original request with new token
      originalRequest.headers.Authorization = `Bearer ${accessToken}`
      return axios(originalRequest)

    } catch (refreshError) {
      // Clear tokens and redirect to login
      localStorageService.remove('access_token')
      localStorageService.remove('refresh_token')
      localStorageService.remove('user')

      // Redirect to login
      window.location.href = '/auth/login'

      return Promise.reject(refreshError)
    }
  }

  return Promise.reject(error)
}
