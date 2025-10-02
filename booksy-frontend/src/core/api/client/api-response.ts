export interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
  errors?: ValidationErrors
  metadata?: {
    timestamp: string
    requestId: string
  }
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
