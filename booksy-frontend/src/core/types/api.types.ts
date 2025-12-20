/**
 * API Type Definitions
 * Types for API requests, responses, and HTTP operations
 */

import type { AxiosRequestConfig, AxiosResponse, AxiosError } from 'axios'
import type {
  ID,
  PaginatedResponse,
  ListResponse,
  ValidationErrors,
  Timestamp,
} from './common.types'

// ==================== HTTP Methods ====================

/**
 * Supported HTTP methods
 */
export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'

/**
 * HTTP status codes
 */
export enum HttpStatus {
  OK = 200,
  Created = 201,
  Accepted = 202,
  NoContent = 204,
  BadRequest = 400,
  Unauthorized = 401,
  Forbidden = 403,
  NotFound = 404,
  Conflict = 409,
  UnprocessableEntity = 422,
  TooManyRequests = 429,
  InternalServerError = 500,
  BadGateway = 502,
  ServiceUnavailable = 503,
  GatewayTimeout = 504,
}

// ==================== Request Types ====================

/**
 * Base API request configuration
 */
export interface ApiRequestConfig extends AxiosRequestConfig {
  // Request metadata
  metadata?: {
    startTime: Date
    isRetry?: boolean
    retryCount?: number
  }

  // Cache configuration
  cache?: {
    enabled?: boolean
    ttl?: number // Time to live in milliseconds
  }

  // Retry configuration
  retry?: {
    enabled?: boolean
    maxRetries?: number
    retryDelay?: number
  }

  // Custom headers
  headers?: Record<string, string>
}

/**
 * Generic API request wrapper
 */
export interface ApiRequest<T = unknown> {
  method: HttpMethod
  url: string
  data?: T
  params?: Record<string, unknown>
  config?: ApiRequestConfig
}

/**
 * Paginated request parameters
 */
export interface PaginatedRequest {
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

/**
 * Search request with filters
 */
export interface SearchRequest extends PaginatedRequest {
  query?: string
  filters?: Record<string, unknown>
}

// ==================== Response Types ====================

/**
 * Base API response wrapper
 */
export interface ApiResponse<T = unknown> {
  data: T
  message?: string
  success: boolean
  timestamp?: Timestamp
  traceId?: string
}

/**
 * Response metadata
 */
export interface ResponseMeta {
  requestId?: string
  processingTime?: number
  apiVersion?: string
  timestamp?: Timestamp
}

/**
 * API response with metadata
 */
export interface ApiResponseWithMeta<T = unknown> extends ApiResponse<T> {
  meta?: ResponseMeta
}

/**
 * List response from API
 */
export interface ApiListResponse<T = unknown> extends ApiResponse<ListResponse<T>> {}

/**
 * Paginated response from API
 */
export interface ApiPaginatedResponse<T = unknown>
  extends ApiResponse<PaginatedResponse<T>> {}

/**
 * Empty response (204 No Content)
 */
export interface ApiEmptyResponse {
  success: true
  message?: string
}

// ==================== Error Types ====================

/**
 * API error details
 */
export interface ApiErrorDetail {
  field?: string
  message: string
  code?: string
  value?: unknown
}

/**
 * Comprehensive API error response
 */
export interface ApiError {
  message: string
  error?: string
  title?: string
  status: number
  statusText?: string
  type?: string
  traceId?: string
  timestamp?: Timestamp
  path?: string
  details?: ApiErrorDetail[]
  validationErrors?: ValidationErrors
}

/**
 * Validation error response (400, 422)
 */
export interface ValidationErrorResponse extends ApiError {
  validationErrors: ValidationErrors
  details: ApiErrorDetail[]
}

/**
 * Axios error wrapper with typed data
 */
export type ApiAxiosError<T = unknown> = AxiosError<ApiError, T>

// ==================== Authentication ====================

/**
 * Authentication token response
 */
export interface AuthTokenResponse {
  accessToken: string
  refreshToken: string
  tokenType: string
  expiresIn: number
  expiresAt?: Timestamp
}

/**
 * User authentication response
 */
export interface AuthResponse extends AuthTokenResponse {
  user: {
    id: ID
    email: string
    firstName: string
    lastName: string
    phoneNumber?: string
    role: string
    [key: string]: unknown
  }
}

/**
 * Refresh token request
 */
export interface RefreshTokenRequest {
  refreshToken: string
}

/**
 * Refresh token response
 */
export interface RefreshTokenResponse extends AuthTokenResponse {}

// ==================== Upload Types ====================

/**
 * File upload progress
 */
export interface UploadProgress {
  loaded: number
  total: number
  percentage: number
}

/**
 * File upload response
 */
export interface UploadResponse {
  id: ID
  fileName: string
  fileSize: number
  fileType: string
  url: string
  thumbnailUrl?: string
  uploadedAt: Timestamp
}

/**
 * Batch upload response
 */
export interface BatchUploadResponse {
  successful: UploadResponse[]
  failed: Array<{
    fileName: string
    error: string
  }>
  totalCount: number
  successCount: number
  failureCount: number
}

// ==================== Batch Operations ====================

/**
 * Batch operation request
 */
export interface BatchRequest<T = unknown> {
  items: T[]
  options?: {
    stopOnError?: boolean
    parallel?: boolean
  }
}

/**
 * Batch operation result
 */
export interface BatchResult<T = unknown> {
  successful: T[]
  failed: Array<{
    item: T
    error: string
    index: number
  }>
  totalCount: number
  successCount: number
  failureCount: number
}

/**
 * Batch delete request
 */
export interface BatchDeleteRequest {
  ids: ID[]
}

/**
 * Batch delete response
 */
export interface BatchDeleteResponse {
  deletedIds: ID[]
  failedIds: ID[]
  deletedCount: number
  failedCount: number
}

// ==================== Webhooks ====================

/**
 * Webhook event
 */
export interface WebhookEvent<T = unknown> {
  id: ID
  type: string
  timestamp: Timestamp
  data: T
  signature?: string
}

/**
 * Webhook response
 */
export interface WebhookResponse {
  received: boolean
  processedAt: Timestamp
}

// ==================== Query Builders ====================

/**
 * Query operator types
 */
export type QueryOperator =
  | 'eq' // Equal
  | 'ne' // Not equal
  | 'gt' // Greater than
  | 'gte' // Greater than or equal
  | 'lt' // Less than
  | 'lte' // Less than or equal
  | 'in' // In array
  | 'nin' // Not in array
  | 'contains' // String contains
  | 'startsWith' // String starts with
  | 'endsWith' // String ends with

/**
 * Query filter
 */
export interface QueryFilter {
  field: string
  operator: QueryOperator
  value: unknown
}

/**
 * Query builder for complex searches
 */
export interface QueryBuilder {
  filters?: QueryFilter[]
  sort?: Array<{ field: string; order: 'asc' | 'desc' }>
  fields?: string[] // Field selection
  include?: string[] // Related entities to include
  page?: number
  limit?: number
}

// ==================== Cache Control ====================

/**
 * Cache control directives
 */
export interface CacheControl {
  maxAge?: number // Seconds
  sMaxAge?: number // Shared cache max age
  public?: boolean
  private?: boolean
  noCache?: boolean
  noStore?: boolean
  mustRevalidate?: boolean
}

/**
 * Cache metadata
 */
export interface CacheMetadata {
  key: string
  cachedAt: Timestamp
  expiresAt: Timestamp
  ttl: number
  hitCount?: number
}

// ==================== Rate Limiting ====================

/**
 * Rate limit information
 */
export interface RateLimitInfo {
  limit: number // Maximum requests allowed
  remaining: number // Remaining requests
  reset: Timestamp // When the limit resets
  retryAfter?: number // Seconds to wait before retry
}

/**
 * Rate limit exceeded error
 */
export interface RateLimitError extends ApiError {
  rateLimitInfo: RateLimitInfo
}

// ==================== API Client Configuration ====================

/**
 * API client configuration
 */
export interface ApiClientConfig {
  baseURL: string
  timeout?: number
  headers?: Record<string, string>
  withCredentials?: boolean
  auth?: {
    username?: string
    password?: string
    token?: string
  }
  retry?: {
    maxRetries?: number
    retryDelay?: number
    retryableStatuses?: number[]
  }
  cache?: {
    enabled?: boolean
    defaultTtl?: number
  }
}

// ==================== Type Guards ====================

/**
 * Type guard for API error
 */
export function isApiError(error: unknown): error is ApiError {
  return (
    typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    'message' in error
  )
}

/**
 * Type guard for validation error response
 */
export function isValidationError(error: unknown): error is ValidationErrorResponse {
  return (
    isApiError(error) &&
    'validationErrors' in error &&
    (error.status === HttpStatus.BadRequest ||
      error.status === HttpStatus.UnprocessableEntity)
  )
}

/**
 * Type guard for Axios error
 */
export function isAxiosError<T = unknown>(error: unknown): error is ApiAxiosError<T> {
  return (
    typeof error === 'object' &&
    error !== null &&
    'isAxiosError' in error &&
    (error as { isAxiosError: boolean }).isAxiosError === true
  )
}

// ==================== Helper Types ====================

/**
 * Extract response data type from API response
 */
export type ExtractApiData<T> = T extends ApiResponse<infer U> ? U : never

/**
 * Extract list item type from paginated response
 */
export type ExtractPaginatedItem<T> = T extends ApiPaginatedResponse<infer U> ? U : never

/**
 * Extract list item type from list response
 */
export type ExtractListItem<T> = T extends ApiListResponse<infer U> ? U : never

/**
 * Unwrap Axios response
 */
export type UnwrapAxiosResponse<T> = T extends AxiosResponse<infer U> ? U : T

// ==================== Request/Response Interceptors ====================

/**
 * Request interceptor handler
 */
export type RequestInterceptor = (
  config: ApiRequestConfig
) => ApiRequestConfig | Promise<ApiRequestConfig>

/**
 * Response interceptor handler
 */
export type ResponseInterceptor<T = unknown> = (
  response: AxiosResponse<T>
) => AxiosResponse<T> | Promise<AxiosResponse<T>>

/**
 * Error interceptor handler
 */
export type ErrorInterceptor = (error: ApiAxiosError) => Promise<never>

// ==================== Exports ====================

/**
 * Re-export Axios types for convenience
 */
export type { AxiosRequestConfig, AxiosResponse, AxiosError }
