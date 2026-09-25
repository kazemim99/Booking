export interface ApiResponse<T> {
  success: boolean
  data: T | null
  message?: string
  statusCode?: number
  error?: {
    code: string
    message: string
    errors?: ValidationErrors
  }
  errors?: ValidationErrors // Extracted from error.errors for easier access
  metadata?: {
    timestamp: string
    requestId: string
    path?: string
    method?: string
  }
}

export interface ErrorResponse {
  code: string
  message: string
  errors?: ValidationErrors
}

/**
 * Response metadata
 */
export interface ResponseMetadata {
  requestId: string
  timestamp: string
  duration: number
  path: string
  method: string
  version: string
}

export interface ValidationErrors {
  [field: string]: string[]
}

export interface PaginatedResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface ApiResponseWrapper<T> extends ApiResponse<T> {
  pagination?: {
    total: number
    page: number
    pageSize: number
    totalPages: number
  }
}
