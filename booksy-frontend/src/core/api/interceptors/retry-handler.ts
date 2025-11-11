/**
 * Retry Handler
 *
 * Automatically retries failed requests with exponential backoff
 * Useful for handling transient network errors
 */

import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import axios from 'axios'

interface RetryConfig {
  maxRetries: number
  retryDelay: number
  retryableStatuses: number[]
}

const DEFAULT_RETRY_CONFIG: RetryConfig = {
  maxRetries: 3,
  retryDelay: 1000, // 1 second
  retryableStatuses: [408, 429, 500, 502, 503, 504], // Request timeout, rate limit, server errors
}

/**
 * Enhanced config with retry metadata
 */
interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  retryCount?: number
  retryConfig?: Partial<RetryConfig>
}

/**
 * Determines if request should be retried
 */
function shouldRetry(error: AxiosError, config: RetryableRequestConfig): boolean {
  const retryConfig = { ...DEFAULT_RETRY_CONFIG, ...config.retryConfig }
  const currentRetryCount = config.retryCount || 0

  // Don't retry if max retries exceeded
  if (currentRetryCount >= retryConfig.maxRetries) {
    return false
  }

  // Don't retry if no response (network error on last attempt)
  if (!error.response) {
    return currentRetryCount < retryConfig.maxRetries
  }

  // Retry only for specific status codes
  return retryConfig.retryableStatuses.includes(error.response.status)
}

/**
 * Calculates delay before next retry using exponential backoff
 */
function getRetryDelay(retryCount: number, baseDelay: number): number {
  // Exponential backoff: delay * (2 ^ retryCount)
  // With jitter to prevent thundering herd
  const exponentialDelay = baseDelay * Math.pow(2, retryCount)
  const jitter = Math.random() * 0.3 * exponentialDelay // 0-30% jitter
  return exponentialDelay + jitter
}

/**
 * Retry interceptor for failed requests
 */
export async function retryInterceptor(error: AxiosError): Promise<any> {
  const config = error.config as RetryableRequestConfig

  if (!config || !shouldRetry(error, config)) {
    return Promise.reject(error)
  }

  // Increment retry count
  config.retryCount = (config.retryCount || 0) + 1

  const retryConfig = { ...DEFAULT_RETRY_CONFIG, ...config.retryConfig }
  const delay = getRetryDelay(config.retryCount - 1, retryConfig.retryDelay)

  if (import.meta.env.DEV) {
    console.log(
      `🔄 Retrying request (${config.retryCount}/${retryConfig.maxRetries}) after ${Math.round(delay)}ms: ${config.method?.toUpperCase()} ${config.url}`,
    )
  }

  // Wait before retrying
  await new Promise((resolve) => setTimeout(resolve, delay))

  // Retry the request
  return axios(config)
}

/**
 * Configure retry settings for a specific request
 */
export function withRetry(
  config: InternalAxiosRequestConfig,
  retryConfig?: Partial<RetryConfig>,
): RetryableRequestConfig {
  return {
    ...config,
    retryConfig,
  } as RetryableRequestConfig
}

/**
 * Disable retry for a specific request
 */
export function withoutRetry(config: InternalAxiosRequestConfig): RetryableRequestConfig {
  return {
    ...config,
    retryConfig: { maxRetries: 0 },
  } as RetryableRequestConfig
}
