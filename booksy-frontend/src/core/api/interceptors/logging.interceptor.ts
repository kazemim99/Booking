/**
 * Logging Interceptor
 *
 * Logs all API requests and responses (development only)
 */

import type { InternalAxiosRequestConfig, AxiosResponse } from 'axios'
import { loggerService } from '@/core/services/logger/logger.service'

/**
 * Logs all API requests (only in development)
 */
export function requestLoggingInterceptor(config: InternalAxiosRequestConfig) {
  if (import.meta.env.DEV) {
    const { method, url, params, data } = config

    loggerService.info('🚀 API Request', {
      method: method?.toUpperCase(),
      url,
      params,
      data: data ? (typeof data === 'string' ? data.substring(0, 100) : data) : undefined
    })
  }

  // Add request timestamp for performance tracking
  ;(config as any).metadata = { startTime: new Date() }

  return config
}

/**
 * Logs all API responses (only in development)
 */
export function responseLoggingInterceptor(response: AxiosResponse) {
  if (import.meta.env.DEV) {
    const { status, config, data } = response
    const metadata = (config as any).metadata
    const duration = metadata ? new Date().getTime() - metadata.startTime.getTime() : 0

    loggerService.info('✅ API Response', {
      status,
      url: config.url,
      duration: `${duration}ms`,
      data: data ? (typeof data === 'string' ? data.substring(0, 100) : data) : undefined
    })
  }

  return response
}

/**
 * Logs API errors
 */
export function errorLoggingInterceptor(error: any) {
  const { config, response } = error
  const metadata = config?.metadata
  const duration = metadata ? new Date().getTime() - metadata.startTime.getTime() : 0

  loggerService.error('❌ API Error', {
    message: error.message,
    status: response?.status,
    url: config?.url,
    duration: `${duration}ms`,
    data: response?.data
  })

  return Promise.reject(error)
}
