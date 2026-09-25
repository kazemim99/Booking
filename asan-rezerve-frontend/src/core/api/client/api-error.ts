export interface ValidationErrors {
  [field: string]: string[]
}

interface ErrorData {
  message?: string
  error?: string
  title?: string
  errors?: ValidationErrors
  [key: string]: unknown
}

export class ApiError extends Error {
  public readonly statusCode: number
  public readonly data: unknown
  public readonly errors?: ValidationErrors

  constructor(message: string, statusCode: number, data?: unknown) {
    super(message)
    this.name = 'ApiError'
    this.statusCode = statusCode
    this.data = data

    // Extract validation errors from various possible formats
    if (data && typeof data === 'object') {
      const errorData = data as ErrorData

      // Try to extract errors from different possible locations
      if (errorData.errors && typeof errorData.errors === 'object') {
        this.errors = this.normalizeErrors(errorData.errors)
      }
    }

    // Maintains proper stack trace for where error was thrown
    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, ApiError)
    }
  }

  /**
   * Normalize errors to our standard format
   * Handles both array format and string format from different APIs
   */
  private normalizeErrors(errors: unknown): ValidationErrors {
    if (!errors || typeof errors !== 'object') {
      return {}
    }

    const normalized: ValidationErrors = {}

    Object.entries(errors).forEach(([key, value]) => {
      // Handle different error formats:
      // 1. Array of strings: { "Email": ["Email is required", "Email is invalid"] }
      // 2. Single string: { "Email": "Email is required" }
      // 3. Nested object: { "Email": { "required": "Email is required" } }

      if (Array.isArray(value)) {
        // Already in array format
        normalized[key] = value.filter((v): v is string => typeof v === 'string')
      } else if (typeof value === 'string') {
        // Convert single string to array
        normalized[key] = [value]
      } else if (typeof value === 'object' && value !== null) {
        // Flatten nested object values
        normalized[key] = Object.values(value).filter((v): v is string => typeof v === 'string')
      }
    })

    return normalized
  }

  public isClientError(): boolean {
    return this.statusCode >= 400 && this.statusCode < 500
  }

  public isServerError(): boolean {
    return this.statusCode >= 500
  }

  public isNetworkError(): boolean {
    return this.statusCode === 0
  }

  public isValidationError(): boolean {
    return this.statusCode === 422 || (this.statusCode === 400 && !!this.errors)
  }

  public getValidationErrors(): string[] {
    if (!this.errors) return []
    return Object.values(this.errors).flat()
  }

  public getFieldErrors(field: string): string[] {
    if (!this.errors || !this.errors[field]) return []
    return this.errors[field]
  }

  public hasFieldError(field: string): boolean {
    return !!(this.errors && this.errors[field] && this.errors[field].length > 0)
  }

  public getAllFieldErrors(): Record<string, string> {
    if (!this.errors) return {}

    const fieldErrors: Record<string, string> = {}
    Object.entries(this.errors).forEach(([field, messages]) => {
      fieldErrors[field] = messages[0] || '' // Take first error message
    })

    return fieldErrors
  }

  public toJSON(): Record<string, unknown> {
    return {
      name: this.name,
      message: this.message,
      statusCode: this.statusCode,
      data: this.data,
      errors: this.errors,
    }
  }
}
