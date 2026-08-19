import axios, { AxiosError, type AxiosInstance, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'
import { message } from 'ant-design-vue'

const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api/v1',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
})

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('admin_token')
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error: AxiosError) => {
    return Promise.reject(error)
  }
)

// The API wraps every 2xx body in { success, statusCode, message, data, metadata }
// (ApiResponseMiddleware). Unwrap it once here so callers always receive the payload
// itself — previously only the paginated endpoints unwrapped, and every single-item
// call silently handed the envelope object back to the UI as if it were the resource.
const isApiEnvelope = (body: unknown): body is { data: unknown } =>
  typeof body === 'object' &&
  body !== null &&
  'success' in body &&
  'statusCode' in body &&
  'data' in body

apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    if (isApiEnvelope(response.data)) {
      response.data = response.data.data
    }
    return response
  },
  (error: AxiosError) => {
    if (error.response) {
      switch (error.response.status) {
        case 401:
          message.error('Session expired. Please login again.')
          localStorage.removeItem('admin_token')
          window.location.href = '/login'
          break
        case 403:
          message.error('Access denied. Insufficient permissions.')
          break
        case 404:
          message.error('Resource not found.')
          break
        case 500:
          message.error('Server error. Please try again later.')
          break
        default:
          message.error((error.response.data as any)?.message || 'An error occurred')
      }
    } else if (error.request) {
      message.error('Network error. Please check your connection.')
    } else {
      message.error('Request failed. Please try again.')
    }
    return Promise.reject(error)
  }
)

export default apiClient
