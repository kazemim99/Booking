/**
 * Transform Interceptor
 *
 * Converts between frontend camelCase and backend PascalCase
 * Backend: .NET Core (uses PascalCase for JSON properties)
 * Frontend: TypeScript/JavaScript (uses camelCase convention)
 */

import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios'

/**
 * Converts request data from camelCase to PascalCase for .NET API
 * Example: { phoneNumber: "123" } → { PhoneNumber: "123" }
 */
export function requestTransformInterceptor(config: InternalAxiosRequestConfig) {
  if (config.data && typeof config.data === 'object' && !(config.data instanceof FormData)) {
    config.data = toPascalCase(config.data)
  }

  if (config.params && typeof config.params === 'object') {
    config.params = toPascalCase(config.params)
  }

  return config
}

/**
 * Converts response data from PascalCase to camelCase
 * Example: { PhoneNumber: "123" } → { phoneNumber: "123" }
 */
export function responseTransformInterceptor(response: AxiosResponse) {
  if (response.data && typeof response.data === 'object') {
    response.data = toCamelCase(response.data)
  }

  return response
}

/**
 * Convert object keys to PascalCase (C# convention)
 * Example: phoneNumber → PhoneNumber
 */
function toPascalCase(obj: any): any {
  if (obj === null || obj === undefined) {
    return obj
  }

  if (Array.isArray(obj)) {
    return obj.map(toPascalCase)
  }

  if (obj instanceof Date) {
    return obj
  }

  if (typeof obj === 'object') {
    return Object.keys(obj).reduce((result, key) => {
      // Skip if key is not a valid string or is empty
      if (!key || typeof key !== 'string' || key.length === 0) {
        result[key] = toPascalCase(obj[key])
        return result
      }

      // Convert first character to uppercase, keep rest as is
      const pascalKey = key.charAt(0).toUpperCase() + key.slice(1)
      result[pascalKey] = toPascalCase(obj[key])
      return result
    }, {} as any)
  }

  return obj
}

/**
 * Convert object keys to camelCase (JavaScript convention)
 * Example: PhoneNumber → phoneNumber
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
      // Skip if key is not a valid string or is empty
      if (!key || typeof key !== 'string' || key.length === 0) {
        result[key] = toCamelCase(obj[key])
        return result
      }

      // Convert first character to lowercase, keep rest as is
      const camelKey = key.charAt(0).toLowerCase() + key.slice(1)
      result[camelKey] = toCamelCase(obj[key])
      return result
    }, {} as any)
  }

  return obj
}
