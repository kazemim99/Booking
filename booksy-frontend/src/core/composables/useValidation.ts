/**
 * Validation Composable
 * Provides validation utilities and Persian-specific validators
 */

import { ref, computed, type Ref } from 'vue'
import type {
  Validator,
  ValidationResult,
  NationalIdOptions,
  MobileNumberOptions,
  PostalCodeOptions,
  EmailOptions,
  PersianTextOptions,
} from '@/core/types/validation.types'
import { PERSIAN_PATTERNS } from '@/core/types/validation.types'

// ==================== Default Error Messages (Persian) ====================

const DEFAULT_MESSAGES = {
  required: 'این فیلد الزامی است',
  email: 'آدرس ایمیل معتبر نیست',
  min: 'مقدار باید حداقل {min} باشد',
  max: 'مقدار نباید بیشتر از {max} باشد',
  minLength: 'حداقل {min} کاراکتر مورد نیاز است',
  maxLength: 'حداکثر {max} کاراکتر مجاز است',
  pattern: 'فرمت ورودی صحیح نیست',
  nationalId: 'کد ملی معتبر نیست',
  mobileNumber: 'شماره موبایل معتبر نیست',
  postalCode: 'کد پستی معتبر نیست',
  persian: 'لطفاً از حروف فارسی استفاده کنید',
  match: 'مقادیر مطابقت ندارند',
} as const

// ==================== Composable ====================

export function useValidation() {
  const errors = ref<Map<string, string>>(new Map())
  const isValidating = ref(false)

  // ==================== Computed ====================

  const hasErrors = computed(() => errors.value.size > 0)
  const errorCount = computed(() => errors.value.size)

  // ==================== Core Functions ====================

  /**
   * Validate a value with multiple validators
   */
  async function validate(
    fieldName: string,
    value: unknown,
    validators: Validator[]
  ): Promise<ValidationResult> {
    isValidating.value = true

    try {
      for (const validator of validators) {
        const error = await validator(value)

        if (error) {
          errors.value.set(fieldName, error)
          return { valid: false, error }
        }
      }

      // Clear error if validation passed
      errors.value.delete(fieldName)
      return { valid: true }
    } finally {
      isValidating.value = false
    }
  }

  /**
   * Set error for a field
   */
  function setError(fieldName: string, message: string): void {
    errors.value.set(fieldName, message)
  }

  /**
   * Clear error for a field
   */
  function clearError(fieldName: string): void {
    errors.value.delete(fieldName)
  }

  /**
   * Clear all errors
   */
  function clearAllErrors(): void {
    errors.value.clear()
  }

  /**
   * Get error for a field
   */
  function getError(fieldName: string): string | undefined {
    return errors.value.get(fieldName)
  }

  // ==================== Built-in Validators ====================

  /**
   * Required validator
   */
  function required(message?: string): Validator {
    return (value: unknown) => {
      if (value === null || value === undefined || value === '') {
        return message || DEFAULT_MESSAGES.required
      }

      if (typeof value === 'string' && value.trim() === '') {
        return message || DEFAULT_MESSAGES.required
      }

      if (Array.isArray(value) && value.length === 0) {
        return message || DEFAULT_MESSAGES.required
      }

      return null
    }
  }

  /**
   * Email validator
   */
  function email(options: EmailOptions = {}): Validator {
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

    return (value: unknown) => {
      if (!value) return null

      if (typeof value !== 'string') {
        return options.message || DEFAULT_MESSAGES.email
      }

      if (!emailPattern.test(value)) {
        return options.message || DEFAULT_MESSAGES.email
      }

      return null
    }
  }

  /**
   * Min value validator
   */
  function min(minValue: number, message?: string): Validator {
    return (value: unknown) => {
      if (value === null || value === undefined) return null

      const numValue = Number(value)

      if (isNaN(numValue)) {
        return message || DEFAULT_MESSAGES.min.replace('{min}', String(minValue))
      }

      if (numValue < minValue) {
        return message || DEFAULT_MESSAGES.min.replace('{min}', String(minValue))
      }

      return null
    }
  }

  /**
   * Max value validator
   */
  function max(maxValue: number, message?: string): Validator {
    return (value: unknown) => {
      if (value === null || value === undefined) return null

      const numValue = Number(value)

      if (isNaN(numValue)) {
        return message || DEFAULT_MESSAGES.max.replace('{max}', String(maxValue))
      }

      if (numValue > maxValue) {
        return message || DEFAULT_MESSAGES.max.replace('{max}', String(maxValue))
      }

      return null
    }
  }

  /**
   * Min length validator
   */
  function minLength(length: number, message?: string): Validator {
    return (value: unknown) => {
      if (!value) return null

      const strValue = String(value)

      if (strValue.length < length) {
        return message || DEFAULT_MESSAGES.minLength.replace('{min}', String(length))
      }

      return null
    }
  }

  /**
   * Max length validator
   */
  function maxLength(length: number, message?: string): Validator {
    return (value: unknown) => {
      if (!value) return null

      const strValue = String(value)

      if (strValue.length > length) {
        return message || DEFAULT_MESSAGES.maxLength.replace('{max}', String(length))
      }

      return null
    }
  }

  /**
   * Pattern validator
   */
  function pattern(regex: RegExp, message?: string): Validator {
    return (value: unknown) => {
      if (!value) return null

      if (typeof value !== 'string') {
        return message || DEFAULT_MESSAGES.pattern
      }

      if (!regex.test(value)) {
        return message || DEFAULT_MESSAGES.pattern
      }

      return null
    }
  }

  // ==================== Persian-Specific Validators ====================

  /**
   * Iranian National ID validator
   */
  function nationalId(options: NationalIdOptions = {}): Validator {
    return (value: unknown) => {
      if (!value) return null

      const strValue = String(value)

      // Check format
      if (!PERSIAN_PATTERNS.nationalId.test(strValue)) {
        return options.message || DEFAULT_MESSAGES.nationalId
      }

      // Validate check digit using Luhn algorithm
      const digits = strValue.split('').map(Number)
      const checkDigit = digits.pop()!

      const sum = digits.reduce((acc, digit, index) => {
        return acc + digit * (10 - index)
      }, 0)

      const remainder = sum % 11
      const expectedCheck = remainder < 2 ? remainder : 11 - remainder

      if (checkDigit !== expectedCheck) {
        return options.message || DEFAULT_MESSAGES.nationalId
      }

      return null
    }
  }

  /**
   * Iranian mobile number validator
   */
  function mobileNumber(options: MobileNumberOptions = {}): Validator {
    return (value: unknown) => {
      if (!value) return null

      let strValue = String(value).trim()

      // Remove spaces and dashes
      strValue = strValue.replace(/[\s-]/g, '')

      // Check with country code
      if (options.requireCountryCode) {
        if (!PERSIAN_PATTERNS.mobileWithCountryCode.test(strValue)) {
          return options.message || DEFAULT_MESSAGES.mobileNumber
        }
      } else {
        // Check standard format
        if (!PERSIAN_PATTERNS.mobileNumber.test(strValue)) {
          return options.message || DEFAULT_MESSAGES.mobileNumber
        }
      }

      return null
    }
  }

  /**
   * Iranian postal code validator
   */
  function postalCode(options: PostalCodeOptions = {}): Validator {
    return (value: unknown) => {
      if (!value) return null

      const strValue = String(value).trim()

      if (!PERSIAN_PATTERNS.postalCode.test(strValue)) {
        return options.message || DEFAULT_MESSAGES.postalCode
      }

      return null
    }
  }

  /**
   * Persian text validator
   */
  function persianText(options: PersianTextOptions = {}): Validator {
    return (value: unknown) => {
      if (!value) return null

      const strValue = String(value)

      let pattern = PERSIAN_PATTERNS.persianOnly

      if (options.allowNumbers && options.allowEnglish) {
        pattern = PERSIAN_PATTERNS.persianMixed
      } else if (options.allowNumbers) {
        pattern = PERSIAN_PATTERNS.persianWithNumbers
      } else if (options.allowEnglish) {
        pattern = PERSIAN_PATTERNS.persianWithEnglish
      }

      if (!pattern.test(strValue)) {
        return options.message || DEFAULT_MESSAGES.persian
      }

      return null
    }
  }

  /**
   * Match validator (for password confirmation, etc.)
   */
  function match(otherValue: Ref<unknown>, message?: string): Validator {
    return (value: unknown) => {
      if (value !== otherValue.value) {
        return message || DEFAULT_MESSAGES.match
      }

      return null
    }
  }

  // ==================== Return ====================

  return {
    // State
    errors,
    isValidating,

    // Computed
    hasErrors,
    errorCount,

    // Core functions
    validate,
    setError,
    clearError,
    clearAllErrors,
    getError,

    // Built-in validators
    required,
    email,
    min,
    max,
    minLength,
    maxLength,
    pattern,

    // Persian validators
    nationalId,
    mobileNumber,
    postalCode,
    persianText,
    match,

    // Constants
    PERSIAN_PATTERNS,
    DEFAULT_MESSAGES,
  }
}
