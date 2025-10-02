export interface SelectOption<T = string | number> {
  label: string
  value: T
  disabled?: boolean
}

export interface ValidationError {
  field: string
  message: string
}

export interface ValidationErrors {
  [field: string]: string[]
}

export interface ApiErrorResponse {
  message?: string
  error?: string
  title?: string
  errors?: ValidationErrors
  status?: number
  type?: string
}

export type SortOrder = 'asc' | 'desc'

export interface SortOptions {
  field: string
  order: SortOrder
}

export interface PagedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface PaginationParams {
  pageNumber: number
  pageSize: number
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

export interface DateRange {
  start: Date
  end: Date
}

export type RecordType = Record<string, unknown>
