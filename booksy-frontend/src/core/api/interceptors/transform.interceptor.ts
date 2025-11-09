/**
 * Transform Interceptor
 *
 * Converts request data to snake_case and response data to camelCase
 */

import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios'

/**
 * Converts request data to snake_case for API
 */
export function requestTransformInterceptor(config: InternalAxiosRequestConfig) {
  if (config.data && typeof config.data === 'object' && !(config.data instanceof FormData)) {
    config.data = toSnakeCase(config.data)
  }

  if (config.params && typeof config.params === 'object') {
    config.params = toSnakeCase(config.params)
  }

  return config
}

/**
 * Converts response data from snake_case to camelCase
 */
export function responseTransformInterceptor(response: AxiosResponse) {
  if (response.data && typeof response.data === 'object') {
    response.data = toCamelCase(response.data)
  }

  return response
}

/**
 * Convert object keys to snake_case
 */
function toSnakeCase(obj: any): any {
  if (obj === null || obj === undefined) {
    return obj
  }

  if (Array.isArray(obj)) {
    return obj.map(toSnakeCase)
  }

  if (obj instanceof Date) {
    return obj
  }

  if (typeof obj === 'object') {
    return Object.keys(obj).reduce((result, key) => {
      const snakeKey = key.replace(/[A-Z]/g, (letter) => `_${letter.toLowerCase()}`)
      result[snakeKey] = toSnakeCase(obj[key])
      return result
    }, {} as any)
  }

  return obj
}

/**
 * Convert object keys to camelCase
 */
function toCamelCase(obj: any): any {
  if (obj === null || obj === undefined) {
    return obj
  }

  if (Array.isArray(obj)) {
    return obj.map(toCamelCase)
  }

  if (obj instanceof Date) {
    return obj
  }

  if (typeof obj === 'object') {
    return Object.keys(obj).reduce((result, key) => {
      const camelKey = key.replace(/_([a-z])/g, (_, letter) => letter.toUpperCase())
      result[camelKey] = toCamelCase(obj[key])
      return result
    }, {} as any)
  }

  return obj
}
