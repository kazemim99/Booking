/**
 * Common Type Definitions
 * Shared types used across the Booksy application
 */

// ==================== Utility Types ====================

/**
 * Unique identifier type (string or number)
 */
export type ID = string | number

/**
 * Timestamp type (ISO 8601 string or Date object)
 */
export type Timestamp = string | Date

/**
 * Makes all properties of T nullable
 */
export type Nullable<T> = T | null

/**
 * Makes all properties of T optional and nullable
 */
export type Optional<T> = T | null | undefined

/**
 * Makes specific keys K in T nullable
 */
export type NullableKeys<T, K extends keyof T> = Omit<T, K> & {
  [P in K]: T[P] | null
}

/**
 * Makes specific keys K in T optional
 */
export type PartialKeys<T, K extends keyof T> = Omit<T, K> & {
  [P in K]?: T[P]
}

/**
 * Deep partial type - makes all properties optional recursively
 */
export type DeepPartial<T> = {
  [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P]
}

/**
 * Extract keys of T that have value type V
 */
export type KeysOfType<T, V> = {
  [K in keyof T]: T[K] extends V ? K : never
}[keyof T]

/**
 * Generic record type with unknown values
 */
export type RecordType = Record<string, unknown>

/**
 * Generic dictionary type
 */
export type Dictionary<T = unknown> = Record<string, T>

// ==================== Response Types ====================

/**
 * Standard list/collection response
 */
export interface ListResponse<T> {
  items: T[]
  totalCount: number
}

/**
 * Paginated response wrapper
 */
export interface PaginatedResponse<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

/**
 * Alias for backwards compatibility
 */
export type PagedResult<T> = PaginatedResponse<T>

/**
 * Pagination request parameters
 */
export interface PaginationParams {
  pageNumber: number
  pageSize: number
  sortBy?: string
  sortOrder?: SortOrder
}

// ==================== Selection & Options ====================

/**
 * Generic select option for dropdowns and pickers
 */
export interface SelectOption<T = string | number> {
  label: string
  value: T
  disabled?: boolean
  icon?: string
  description?: string
}

/**
 * Grouped select options
 */
export interface GroupedSelectOption<T = string | number> {
  groupLabel: string
  options: SelectOption<T>[]
}

// ==================== Sorting ====================

/**
 * Sort order direction
 */
export type SortOrder = 'asc' | 'desc'

/**
 * Sorting options
 */
export interface SortOptions {
  field: string
  order: SortOrder
}

// ==================== Date & Time ====================

/**
 * Date range with start and end dates
 */
export interface DateRange {
  start: Date | string
  end: Date | string
}

/**
 * Time range with start and end times (HH:mm format)
 */
export interface TimeRange {
  start: string
  end: string
}

/**
 * Date and time range combined
 */
export interface DateTimeRange {
  startDate: Date | string
  endDate: Date | string
  startTime?: string
  endTime?: string
}

// ==================== Validation & Errors ====================

/**
 * Single validation error
 */
export interface ValidationError {
  field: string
  message: string
  code?: string
}

/**
 * Validation errors collection (field -> error messages array)
 */
export interface ValidationErrors {
  [field: string]: string[]
}

/**
 * API error response structure
 */
export interface ApiErrorResponse {
  message?: string
  error?: string
  title?: string
  errors?: ValidationErrors
  status?: number
  type?: string
  traceId?: string
}

// ==================== Status & State ====================

/**
 * Generic status type
 */
export type Status = 'active' | 'inactive' | 'pending' | 'archived' | 'deleted'

/**
 * Loading state
 */
export type LoadingState = 'idle' | 'loading' | 'success' | 'error'

/**
 * Generic async state wrapper
 */
export interface AsyncState<T> {
  data: T | null
  loading: boolean
  error: string | null
}

// ==================== Location & Geography ====================

/**
 * Geographic coordinates
 */
export interface Coordinates {
  latitude: number
  longitude: number
}

/**
 * Address structure
 */
export interface Address {
  street: string
  city: string
  province: string
  postalCode?: string
  country: string
  coordinates?: Coordinates
}

// ==================== File & Media ====================

/**
 * File upload information
 */
export interface FileInfo {
  id?: ID
  name: string
  size: number
  type: string
  url?: string
  uploadedAt?: Timestamp
}

/**
 * Image information
 */
export interface ImageInfo extends FileInfo {
  width?: number
  height?: number
  thumbnail?: string
  alt?: string
}

// ==================== Audit & Tracking ====================

/**
 * Audit fields for tracking creation/updates
 */
export interface AuditFields {
  createdAt: Timestamp
  updatedAt: Timestamp
  createdBy?: ID
  updatedBy?: ID
}

/**
 * Soft delete support
 */
export interface SoftDeletable {
  deletedAt?: Timestamp | null
  deletedBy?: ID | null
  isDeleted: boolean
}

/**
 * Entity with audit trail
 */
export interface AuditedEntity extends AuditFields {
  id: ID
}

/**
 * Full audited entity with soft delete
 */
export interface FullAuditedEntity extends AuditedEntity, SoftDeletable {}

// ==================== Currency & Money ====================

/**
 * Money/currency representation
 */
export interface Money {
  amount: number
  currency: string // ISO 4217 currency code (e.g., 'IRR', 'USD')
}

/**
 * Price with optional discount
 */
export interface Price extends Money {
  originalAmount?: number
  discountPercentage?: number
}

// ==================== Search & Filter ====================

/**
 * Generic filter parameters
 */
export interface FilterParams {
  search?: string
  status?: Status | string
  dateFrom?: Timestamp
  dateTo?: Timestamp
  [key: string]: unknown
}

/**
 * Search parameters with pagination and sorting
 */
export interface SearchParams extends PaginationParams {
  query?: string
  filters?: FilterParams
}
