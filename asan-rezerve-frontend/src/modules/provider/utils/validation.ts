// src/modules/provider/utils/validation.ts

/**
 * Provider Form Validation Utilities
 * Reusable validation functions for provider-related forms
 */

export interface ValidationResult {
  isValid: boolean
  errors: Record<string, string>
}

export interface ValidationRule {
  required?: boolean
  minLength?: number
  maxLength?: number
  pattern?: RegExp
  min?: number
  max?: number
  custom?: (value: unknown) => boolean
  message?: string
}

export interface ValidationSchema {
  [field: string]: ValidationRule | ValidationRule[]
}

// ============================================
// Core Validation Functions
// ============================================

/**
 * Validate a single field against rules
 */
export function validateField(
  value: unknown,
  rules: ValidationRule | ValidationRule[],
  fieldName: string = 'Field',
): string | null {
  const ruleArray = Array.isArray(rules) ? rules : [rules]

  for (const rule of ruleArray) {
    // Required check
    if (rule.required && !value) {
      return rule.message || `${fieldName} is required`
    }

    // Skip other checks if value is empty and not required
    if (!value && !rule.required) {
      continue
    }

    // String validations
    if (typeof value === 'string') {
      // Min length
      if (rule.minLength !== undefined && value.length < rule.minLength) {
        return rule.message || `${fieldName} must be at least ${rule.minLength} characters`
      }

      // Max length
      if (rule.maxLength !== undefined && value.length > rule.maxLength) {
        return rule.message || `${fieldName} must not exceed ${rule.maxLength} characters`
      }

      // Pattern
      if (rule.pattern && !rule.pattern.test(value)) {
        return rule.message || `${fieldName} format is invalid`
      }
    }

    // Number validations
    if (typeof value === 'number') {
      // Min value
      if (rule.min !== undefined && value < rule.min) {
        return rule.message || `${fieldName} must be at least ${rule.min}`
      }

      // Max value
      if (rule.max !== undefined && value > rule.max) {
        return rule.message || `${fieldName} must not exceed ${rule.max}`
      }
    }

    // Custom validation
    if (rule.custom && !rule.custom(value)) {
      return rule.message || `${fieldName} is invalid`
    }
  }

  return null
}

/**
 * Validate an entire object against a schema
 */
export function validateSchema(
  data: Record<string, unknown>,
  schema: ValidationSchema,
): ValidationResult {
  const errors: Record<string, string> = {}

  for (const [field, rules] of Object.entries(schema)) {
    const error = validateField(data[field], rules, field)
    if (error) {
      errors[field] = error
    }
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors,
  }
}

// ============================================
// Specific Field Validators
// ============================================

/**
 * Validate email address
 */
export function validateEmail(email: string): string | null {
  if (!email) {
    return 'Email is required'
  }

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  if (!emailRegex.test(email)) {
    return 'Invalid email format'
  }

  if (email.length > 100) {
    return 'Email must not exceed 100 characters'
  }

  return null
}

/**
 * Validate phone number
 */
export function validatePhone(phone: string): string | null {
  if (!phone) {
    return 'Phone number is required'
  }

  // Remove all non-digit characters for validation
  const cleaned = phone.replace(/\D/g, '')

  if (cleaned.length < 10) {
    return 'Phone number must be at least 10 digits'
  }

  if (cleaned.length > 15) {
    return 'Phone number must not exceed 15 digits'
  }

  return null
}

/**
 * Validate URL
 */
export function validateUrl(url: string, required: boolean = false): string | null {
  if (!url) {
    return required ? 'URL is required' : null
  }

  try {
    new URL(url)
    return null
  } catch {
    return 'Invalid URL format'
  }
}

/**
 * Validate postal code
 */
export function validatePostalCode(postalCode: string, country: string = 'USA'): string | null {
  if (!postalCode) {
    return 'Postal code is required'
  }

  // US ZIP code format
  if (country === 'USA') {
    const zipRegex = /^\d{5}(-\d{4})?$/
    if (!zipRegex.test(postalCode)) {
      return 'Invalid US ZIP code format (e.g., 12345 or 12345-6789)'
    }
  }

  // Canadian postal code
  if (country === 'Canada') {
    const canadaRegex = /^[A-Za-z]\d[A-Za-z] ?\d[A-Za-z]\d$/
    if (!canadaRegex.test(postalCode)) {
      return 'Invalid Canadian postal code format (e.g., K1A 0B1)'
    }
  }

  // UK postcode
  if (country === 'UK') {
    const ukRegex = /^[A-Z]{1,2}\d{1,2} ?\d[A-Z]{2}$/i
    if (!ukRegex.test(postalCode)) {
      return 'Invalid UK postcode format'
    }
  }

  return null
}

/**
 * Validate business name
 */
export function validateBusinessName(name: string): string | null {
  if (!name || !name.trim()) {
    return 'Business name is required'
  }

  const trimmed = name.trim()

  if (trimmed.length < 2) {
    return 'Business name must be at least 2 characters'
  }

  if (trimmed.length > 100) {
    return 'Business name must not exceed 100 characters'
  }

  // Check for valid characters (letters, numbers, spaces, and common punctuation)
  const nameRegex = /^[a-zA-Z0-9\s\-&'.,()]+$/
  if (!nameRegex.test(trimmed)) {
    return 'Business name contains invalid characters'
  }

  return null
}

/**
 * Validate description
 */
export function validateDescription(
  description: string,
  minLength: number = 50,
  maxLength: number = 500,
): string | null {
  if (!description || !description.trim()) {
    return 'Description is required'
  }

  const trimmed = description.trim()

  if (trimmed.length < minLength) {
    return `Description must be at least ${minLength} characters`
  }

  if (trimmed.length > maxLength) {
    return `Description must not exceed ${maxLength} characters`
  }

  return null
}

/**
 * Validate tags array
 */
export function validateTags(tags: string[], maxTags: number = 10): string | null {
  if (!tags || tags.length === 0) {
    return null // Tags are usually optional
  }

  if (tags.length > maxTags) {
    return `Maximum ${maxTags} tags allowed`
  }

  for (const tag of tags) {
    if (tag.length > 30) {
      return 'Each tag must not exceed 30 characters'
    }

    const tagRegex = /^[a-zA-Z0-9\s\-]+$/
    if (!tagRegex.test(tag)) {
      return `Tag "${tag}" contains invalid characters`
    }
  }

  return null
}

// ============================================
// Provider-Specific Validation Schemas
// ============================================

/**
 * Provider Registration Validation Schema
 */
export const providerRegistrationSchema: ValidationSchema = {
  businessName: {
    required: true,
    minLength: 2,
    maxLength: 100,
    message: 'Business name must be between 2 and 100 characters',
  },
  type: {
    required: true,
    message: 'Provider type is required',
  },
  description: {
    required: true,
    minLength: 50,
    maxLength: 500,
    message: 'Description must be between 50 and 500 characters',
  },
  email: {
    required: true,
    pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    message: 'Invalid email format',
  },
  primaryPhone: {
    required: true,
    message: 'Primary phone is required',
  },
  addressLine1: {
    required: true,
    minLength: 5,
    maxLength: 200,
    message: 'Street address is required',
  },
  city: {
    required: true,
    minLength: 2,
    maxLength: 100,
    message: 'City is required',
  },
  state: {
    required: true,
    minLength: 2,
    maxLength: 50,
    message: 'State/Province is required',
  },
  postalCode: {
    required: true,
    message: 'Postal code is required',
  },
  country: {
    required: true,
    message: 'Country is required',
  },
}

/**
 * Provider Profile Update Schema
 */
export const providerUpdateSchema: ValidationSchema = {
  businessName: {
    minLength: 2,
    maxLength: 100,
    message: 'Business name must be between 2 and 100 characters',
  },
  description: {
    minLength: 50,
    maxLength: 500,
    message: 'Description must be between 50 and 500 characters',
  },
  email: {
    pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    message: 'Invalid email format',
  },
}

// ============================================
// Validation Helper Functions
// ============================================

/**
 * Check if field has error
 */
export function hasError(errors: Record<string, string>, field: string): boolean {
  return !!errors[field]
}

/**
 * Get error message for field
 */
export function getError(errors: Record<string, string>, field: string): string | undefined {
  return errors[field]
}

/**
 * Clear error for field
 */
export function clearError(errors: Record<string, string>, field: string): void {
  delete errors[field]
}

/**
 * Clear all errors
 */
export function clearAllErrors(errors: Record<string, string>): void {
  Object.keys(errors).forEach((key) => delete errors[key])
}

/**
 * Count total errors
 */
export function countErrors(errors: Record<string, string>): number {
  return Object.keys(errors).length
}

/**
 * Check if form has unknown errors
 */
export function hasunknownErrors(errors: Record<string, string>): boolean {
  return Object.keys(errors).length > 0
}

/**
 * Format error messages for display
 */
export function formatErrors(errors: Record<string, string>): string[] {
  return Object.entries(errors).map(([field, message]) => {
    const fieldName = field.replace(/([A-Z])/g, ' $1').trim()
    return `${fieldName}: ${message}`
  })
}

// ============================================
// Async Validators (for unique checks, etc.)
// ============================================

/**
 * Create debounced validator
 */
export function createDebouncedValidator(
  validator: (value: unknown) => Promise<string | null>,
  delay: number = 500,
): (value: unknown) => Promise<string | null> {
  let timeoutId: ReturnType<typeof setTimeout>

  return (value: unknown) => {
    return new Promise((resolve) => {
      clearTimeout(timeoutId)
      timeoutId = setTimeout(async () => {
        const error = await validator(value)
        resolve(error)
      }, delay)
    })
  }
}

/**
 * Example: Validate business name uniqueness (would call API)
 */
export async function validateBusinessNameUnique(
  businessName: string,
  excludeId?: string,
): Promise<string | null> {
  // This would make an API call to check uniqueness
  // For now, just return null (valid)
  console.log(businessName)
  console.log(excludeId)
  // Example implementation:
  // try {
  //   const response = await providerService.checkBusinessNameAvailable(businessName, excludeId)
  //   return response.isAvailable ? null : 'Business name is already taken'
  // } catch (error) {
  //   return 'Unable to verify business name'
  // }

  return null
}

/**
 * Example: Validate email uniqueness
 */
export async function validateEmailUnique(
  email: string,
  excludeId?: string,
): Promise<string | null> {
  console.log(email)
  console.log(excludeId)

  // This would make an API call to check uniqueness
  return null
}

// ============================================
// Validation Composables for Vue
// ============================================

/**
 * Create a reactive validation state
 */
export function useValidation() {
  const errors: Record<string, string> = {}
  const touched: Record<string, boolean> = {}

  const validate = (field: string, value: unknown, rules: ValidationRule | ValidationRule[]) => {
    const error = validateField(value, rules, field)
    if (error) {
      errors[field] = error
    } else {
      delete errors[field]
    }
    return !error
  }

  const validateAll = (data: Record<string, unknown>, schema: ValidationSchema) => {
    const result = validateSchema(data, schema)
    Object.assign(errors, result.errors)
    return result.isValid
  }

  const touch = (field: string) => {
    touched[field] = true
  }

  const reset = () => {
    clearAllErrors(errors)
    Object.keys(touched).forEach((key) => delete touched[key])
  }

  const shouldShowError = (field: string): boolean => {
    return touched[field] && !!errors[field]
  }

  return {
    errors,
    touched,
    validate,
    validateAll,
    touch,
    reset,
    shouldShowError,
    hasErrors: () => hasunknownErrors(errors),
    getError: (field: string) => getError(errors, field),
    clearError: (field: string) => clearError(errors, field),
  }
}

// ============================================
// Export Everything
// ============================================

export default {
  validateField,
  validateSchema,
  validateEmail,
  validatePhone,
  validateUrl,
  validatePostalCode,
  validateBusinessName,
  validateDescription,
  validateTags,
  providerRegistrationSchema,
  providerUpdateSchema,
  hasError,
  getError,
  clearError,
  clearAllErrors,
  countErrors,
  hasunknownErrors,
  formatErrors,
  createDebouncedValidator,
  validateBusinessNameUnique,
  validateEmailUnique,
  useValidation,
}
