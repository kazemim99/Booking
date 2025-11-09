import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, AxiosError } from 'axios'
import { apiConfig, microservices } from '../config/api-config'
import { ApiError } from './api-error'
import type { ApiResponse } from './api-response'
import {
  authInterceptor,
  authErrorInterceptor,
} from '../interceptors/auth.interceptor'
import { errorInterceptor } from '../interceptors/error.interceptor'
import {
  requestLoggingInterceptor,
  responseLoggingInterceptor,
  errorLoggingInterceptor,
} from '../interceptors/logging.interceptor'
import {
  requestTransformInterceptor,
  responseTransformInterceptor,
} from '../interceptors/transform.interceptor'
import { retryInterceptor } from '../interceptors/retry-handler'
import {
  cacheRequestInterceptor,
  cacheResponseInterceptor,
} from '../interceptors/request-cache'

interface ErrorResponse {
  success?: boolean
  statusCode?: number
  message?: string
  data?: unknown
  error?: {
    code?: string
    message?: string
    errors?: Record<string, string[]>
  }
  errors?: Record<string, string[]>
  title?: string
  status?: number
  metadata?: {
    requestId?: string
    timestamp?: string
    path?: string
    method?: string
  }
}

interface HttpClientConfig {
  baseURL: string
  timeout: number
  withCredentials: boolean
}

class HttpClient {
  private axiosInstance: AxiosInstance
  private readonly baseURL: string

  constructor(config: HttpClientConfig = apiConfig) {
    this.baseURL = config.baseURL
    this.axiosInstance = axios.create({
      baseURL: config.baseURL,
      timeout: config.timeout,
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
      },
      withCredentials: config.withCredentials,
    })

    this.initializeInterceptors()
  }

  private initializeInterceptors(): void {
    // ============================================
    // Request Interceptors (applied in order)
    // ============================================

    // 1. Cache - Check cache before making request (GET only)
    this.axiosInstance.interceptors.request.use(
      cacheRequestInterceptor as any,
      (error: AxiosError) => Promise.reject(error),
    )

    // 2. Logging - Track request start time and log request details
    this.axiosInstance.interceptors.request.use(
      requestLoggingInterceptor,
      (error: AxiosError) => {
        console.error('[HTTP] Request Error:', error)
        return Promise.reject(error)
      },
    )

    // 3. Auth - Add JWT token to request headers
    this.axiosInstance.interceptors.request.use(
      authInterceptor,
      (error: AxiosError) => Promise.reject(error),
    )

    // 4. Transform - Convert camelCase to PascalCase for .NET API
    this.axiosInstance.interceptors.request.use(
      requestTransformInterceptor,
      (error: AxiosError) => Promise.reject(error),
    )

    // ============================================
    // Response Interceptors (applied in reverse order)
    // ============================================

    // 1. Transform - Convert PascalCase to camelCase from .NET API
    this.axiosInstance.interceptors.response.use(
      responseTransformInterceptor,
      (error: AxiosError) => Promise.reject(error),
    )

    // 2. Logging - Log successful responses
    this.axiosInstance.interceptors.response.use(
      responseLoggingInterceptor,
      (error: AxiosError) => Promise.reject(error),
    )

    // 3. Cache - Store successful GET responses
    this.axiosInstance.interceptors.response.use(
      cacheResponseInterceptor,
      (error: AxiosError) => Promise.reject(error),
    )

    // 4. Auth Error - Handle 401 errors and token refresh
    this.axiosInstance.interceptors.response.use(
      (response: AxiosResponse) => response,
      authErrorInterceptor,
    )

    // 5. Error - Handle all other errors with Persian messages
    this.axiosInstance.interceptors.response.use(
      (response: AxiosResponse) => response,
      errorInterceptor,
    )

    // 6. Retry - Retry failed requests with exponential backoff
    this.axiosInstance.interceptors.response.use(
      (response: AxiosResponse) => response,
      retryInterceptor,
    )

    // 7. Error Logging - Log error responses in development
    this.axiosInstance.interceptors.response.use(
      (response: AxiosResponse) => response,
      errorLoggingInterceptor,
    )
  }


  /**
   * Normalize API response to extract validation errors from nested error.errors
   */
  private normalizeResponse<T>(responseData: ApiResponse<T>): ApiResponse<T> {
    // If error.errors exists, extract it to top level for easier access
    if (responseData.error?.errors) {
      return {
        ...responseData,
        errors: responseData.error.errors,
      } as ApiResponse<T> & { errors: Record<string, string[]> }
    }
    return responseData
  }

  // HTTP Methods
  public async get<T>(url: string, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
    const response = await this.axiosInstance.get<ApiResponse<T>>(url, config)
    return this.normalizeResponse(response.data)
  }

  public async post<T>(
    url: string,
    data?: unknown,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    const response = await this.axiosInstance.post<ApiResponse<T>>(url, data, config)
    return this.normalizeResponse(response.data)
  }

  public async put<T>(
    url: string,
    data?: unknown,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    const response = await this.axiosInstance.put<ApiResponse<T>>(url, data, config)
    return this.normalizeResponse(response.data)
  }

  public async patch<T>(
    url: string,
    data?: unknown,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    const response = await this.axiosInstance.patch<ApiResponse<T>>(url, data, config)
    return this.normalizeResponse(response.data)
  }

  public async delete<T>(url: string, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
    const response = await this.axiosInstance.delete<ApiResponse<T>>(url, config)
    return this.normalizeResponse(response.data)
  }

  // Get raw axios instance for special cases
  public getInstance(): AxiosInstance {
    return this.axiosInstance
  }
}

// Export singleton instances for each microservice
export const serviceCategoryClient = new HttpClient(microservices.serviceCategory)
export const userManagementClient = new HttpClient(microservices.userManagement)

// Legacy default client (backward compatibility)
export const httpClient = serviceCategoryClient
export default httpClient

// Re-export cache utilities
export {
  withCache,
  withoutCache,
  clearCache,
  getCacheStats,
  requestCache,
} from '../interceptors/request-cache'

// Re-export retry utilities
export { withRetry, withoutRetry } from '../interceptors/retry-handler'

// Extend AxiosRequestConfig to include metadata
declare module 'axios' {
  export interface AxiosRequestConfig {
    metadata?: {
      startTime: Date
      isRetry?: boolean
    }
  }
}
