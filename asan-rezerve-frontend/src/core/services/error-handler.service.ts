/**
 * Error Handler Service
 * Provides utilities to handle API errors and convert them to user-friendly messages
 */

import { toastService } from './toast.service'
import type { AxiosError } from 'axios'

export interface ApiErrorResponse {
  success: false
  statusCode: number
  message: string
  error: {
    code: string
    message: string
    errors?: Record<string, string[]>
  }
  metadata?: {
    requestId: string
    timestamp: string
    path: string
    method: string
  }
}

export interface FieldError {
  field: string
  messages: string[]
}

export interface ErrorHandlerResult {
  isValidationError: boolean
  isServerError: boolean
  generalMessage: string
  fieldErrors: FieldError[]
  errorCode?: string
}

class ErrorHandlerService {
  /**
   * Handle API error and return structured error information
   */
  handleError(error: unknown): ErrorHandlerResult {
    // Handle Axios errors
    if (this.isAxiosError(error)) {
      const axiosError = error as AxiosError<ApiErrorResponse>

      // No response (network error)
      if (!axiosError.response) {
        return {
          isValidationError: false,
          isServerError: true,
          generalMessage: 'Network error. Please check your internet connection.',
          fieldErrors: [],
          errorCode: 'NETWORK_ERROR'
        }
      }

      const { status, data } = axiosError.response

      // 4xx Validation errors
      if (status >= 400 && status < 500) {
        return this.handleClientError(data, status)
      }

      // 5xx Server errors
      if (status >= 500) {
        return this.handleServerError(data, status)
      }
    }

    // Generic error
    return {
      isValidationError: false,
      isServerError: false,
      generalMessage: this.extractErrorMessage(error),
      fieldErrors: [],
      errorCode: 'UNKNOWN_ERROR'
    }
  }

  /**
   * Handle client errors (4xx)
   */
  private handleClientError(data: ApiErrorResponse, status: number): ErrorHandlerResult {
    const fieldErrors: FieldError[] = []

    // Extract field-level validation errors
    if (data.error?.errors) {
      Object.entries(data.error.errors).forEach(([field, messages]) => {
        fieldErrors.push({
          field: this.normalizeFieldName(field),
          messages: messages || []
        })
      })
    }

    return {
      isValidationError: status === 400 && fieldErrors.length > 0,
      isServerError: false,
      generalMessage: data.error?.message || data.message || 'Validation failed',
      fieldErrors,
      errorCode: data.error?.code || 'VALIDATION_ERROR'
    }
  }

  /**
   * Handle server errors (5xx)
   */
  private handleServerError(data: ApiErrorResponse, _status: number): ErrorHandlerResult {
    const errorMessages: string[] = []

    // Collect all error messages from the response
    if (data.error?.errors) {
      Object.values(data.error.errors).forEach(messages => {
        errorMessages.push(...messages)
      })
    }

    // If no specific errors, use general message
    if (errorMessages.length === 0) {
      errorMessages.push(
        data.error?.message ||
        data.message ||
        'An unexpected server error occurred. Please try again later.'
      )
    }

    return {
      isValidationError: false,
      isServerError: true,
      generalMessage: 'Server error occurred',
      fieldErrors: [],
      errorCode: data.error?.code || 'SERVER_ERROR'
    }
  }

  /**
   * Display errors to user (toast for 5xx, inline for 4xx)
   */
  displayError(result: ErrorHandlerResult): void {
    if (result.isServerError) {
      // Show 5xx errors as toasts
      toastService.error(result.generalMessage, 'Server Error')
    } else if (!result.isValidationError) {
      // Show non-validation errors as toasts
      toastService.error(result.generalMessage, 'Error')
    }
    // Note: Validation errors (4xx) should be displayed inline in forms
  }

  /**
   * Display multiple server errors as toasts
   */
  displayServerErrors(messages: string[]): void {
    toastService.errorBatch(messages, 'Server Error')
  }

  /**
   * Get field error message for inline display
   */
  getFieldError(fieldErrors: FieldError[], fieldName: string): string | null {
    const error = fieldErrors.find(e => this.matchField(e.field, fieldName))
    return error && error.messages.length > 0 ? error.messages[0] : null
  }

  /**
   * Check if field has error
   */
  hasFieldError(fieldErrors: FieldError[], fieldName: string): boolean {
    return fieldErrors.some(e => this.matchField(e.field, fieldName))
  }

  /**
   * Normalize field names (convert from backend format to frontend format)
   * E.g., "businessInfo.businessName" -> "businessName"
   * E.g., "services[0].price" -> "services.0.price"
   */
  private normalizeFieldName(field: string): string {
    return field
      .replace(/\[(\d+)\]/g, '.$1') // Convert array notation
      .toLowerCase()
  }

  /**
   * Match field names (case-insensitive, handles nested fields)
   */
  private matchField(errorField: string, fieldName: string): boolean {
    const normalizedError = this.normalizeFieldName(errorField)
    const normalizedField = fieldName.toLowerCase()

    // Exact match
    if (normalizedError === normalizedField) {
      return true
    }

    // Check if error field ends with the field name (handles nested fields)
    if (normalizedError.endsWith(`.${normalizedField}`)) {
      return true
    }

    return false
  }

  /**
   * Extract error message from unknown error type
   */
  private extractErrorMessage(error: unknown): string {
    if (error instanceof Error) {
      return error.message
    }
    if (typeof error === 'string') {
      return error
    }
    return 'An unexpected error occurred'
  }

  /**
   * Type guard for Axios errors
   */
  private isAxiosError(error: unknown): error is AxiosError {
    return (error as AxiosError).isAxiosError === true
  }
}

export const errorHandlerService = new ErrorHandlerService()
