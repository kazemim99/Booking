import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, AxiosError } from 'axios'
import { apiConfig, microservices } from '../config/api-config'
import { ApiError } from './api-error'
import type { ApiResponse } from './api-response'

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
    // Request Interceptor
    this.axiosInstance.interceptors.request.use(
      (config) => {
        // Add auth token if exists
        const token = localStorage.getItem('auth_token')
        if (token && config.headers) {
          config.headers.Authorization = `Bearer ${token}`
        }

        // Add request timestamp
        config.metadata = { startTime: new Date() }

        if (import.meta.env.DEV) {
          console.log(`🌐 ${config.method?.toUpperCase()} ${config.url}`, config.params)
        }

        return config
      },
      (error: AxiosError) => {
        console.error('[HTTP] Request Error:', error)
        return Promise.reject(error)
      },
    )

    // Response Interceptor
    this.axiosInstance.interceptors.response.use(
      (response: AxiosResponse) => {
        this.logResponse(response)
        return response
      },
      async (error: AxiosError) => {
        return this.handleError(error)
      },
    )
  }

  private logResponse(response: AxiosResponse): void {
    if (!import.meta.env.DEV) return

    const metadata = response.config.metadata as { startTime: Date } | undefined

    if (metadata?.startTime) {
      const duration = new Date().getTime() - metadata.startTime.getTime()
      console.log(
        `[HTTP] ${response.config.method?.toUpperCase()} ${response.config.url} - ${response.status} (${duration}ms)`,
      )
    } else {
      console.log(
        `[HTTP] ${response.config.method?.toUpperCase()} ${response.config.url} - ${response.status}`,
      )
    }
  }

  private async handleError(error: AxiosError): Promise<any> {
    if (error.response) {
      // Server responded with error status
      const { status, data } = error.response
      const errorData = data as ErrorResponse

      // Check if this is a validation error (400 with error.errors)
      if (status === 400 && errorData?.error?.errors) {
        // Return normalized validation response instead of throwing
        const normalizedResponse: ApiResponse<any> = {
          success: false,
          data: null,
          message: errorData.message || 'Validation failed',
          statusCode: status,
          error: {
            code: errorData.error.code || 'VALIDATION_ERROR',
            message: errorData.error.message || 'Validation failed',
            errors: errorData.error.errors,
          },
          errors: errorData.error.errors, // Extract to top level
          metadata: errorData.metadata
            ? {
                timestamp: errorData.metadata.timestamp || new Date().toISOString(),
                requestId: errorData.metadata.requestId || '',
                path: errorData.metadata.path,
                method: errorData.metadata.method,
              }
            : undefined,
        }

        if (import.meta.env.DEV) {
          console.warn('[HTTP] Validation Error:', {
            status,
            message: errorData.message,
            errors: errorData.error.errors,
          })
        }

        // Return the normalized response (not throw)
        return Promise.resolve({ data: normalizedResponse } as AxiosResponse)
      }

      // Extract error message for non-validation errors
      let errorMessage = error.message
      if (errorData) {
        // Priority: message > error.message > title > default
        errorMessage =
          errorData.message || errorData.error?.message || errorData.title || error.message
      }

      // Create ApiError with all error information
      const apiError = new ApiError(errorMessage, status, errorData)

      // Log error details in development
      if (import.meta.env.DEV) {
        console.error('Response Error:', {
          status,
          message: errorMessage,
          errors: errorData?.errors,
          data: errorData,
        })
      }

      // Handle 401 Unauthorized - Token expired
      if (status === 401) {
        const originalRequest = error.config
        if (originalRequest && !originalRequest.metadata?.isRetry) {
          try {
            await this.handleUnauthorized()
            // Mark as retry to prevent infinite loop
            originalRequest.metadata = {
              ...originalRequest.metadata,
              isRetry: true,
              startTime: new Date(),
            }
            // Retry the original request with new token
            return this.axiosInstance.request(originalRequest)
          } catch (_refreshError) {
            console.error('[HTTP] Token refresh failed, redirecting to login')
            console.log(_refreshError)
            throw apiError
          }
        }
      }

      // Handle 403 Forbidden
      if (status === 403) {
        console.error('[HTTP] Access Forbidden')
      }

      throw apiError
    } else if (error.request) {
      // Request made but no response received
      const networkError = new ApiError(
        'No response from server. Please check your internet connection.',
        0,
      )
      console.error('[HTTP] Network Error:', error.request)
      throw networkError
    } else {
      // Error in request setup
      const setupError = new ApiError(error.message, 0)
      console.error('[HTTP] Request Setup Error:', error.message)
      throw setupError
    }
  }

  private async handleUnauthorized(): Promise<void> {
    // If already refreshing, wait for it to complete
    if (this.isRefreshing) {
      return new Promise((resolve) => {
        this.refreshSubscribers.push(() => {
          resolve()
        })
      })
    }

    const refreshToken = localStorage.getItem('refresh_token')

    if (!refreshToken) {
      this.clearAuthData()
      window.location.href = '/login'
      return Promise.reject(new Error('No refresh token available'))
    }

    this.isRefreshing = true

    try {
      // Try to refresh token
      const response = await axios.post<{ accessToken: string; refreshToken: string }>(
        `${this.baseURL}/auth/refresh`,
        { refreshToken },
      )

      const { accessToken, refreshToken: newRefreshToken } = response.data

      localStorage.setItem('auth_token', accessToken)
      localStorage.setItem('refresh_token', newRefreshToken)

      // Notify all subscribers
      this.refreshSubscribers.forEach((callback) => callback())
      this.refreshSubscribers = []

      return Promise.resolve()
    } catch (error) {
      // Refresh failed - logout user
      console.error('[HTTP] Token refresh failed:', error)
      this.clearAuthData()
      window.location.href = '/login'
      return Promise.reject(error)
    } finally {
      this.isRefreshing = false
    }
  }

  private isRefreshing = false
  private refreshSubscribers: Array<() => void> = []

  private clearAuthData(): void {
    localStorage.removeItem('auth_token')
    localStorage.removeItem('refresh_token')
    localStorage.removeItem('user')
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

// Extend AxiosRequestConfig to include metadata
declare module 'axios' {
  export interface AxiosRequestConfig {
    metadata?: {
      startTime: Date
      isRetry?: boolean
    }
  }
}
