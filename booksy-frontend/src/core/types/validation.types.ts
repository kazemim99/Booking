/**
 * Validation Type Definitions
 * Types for validation rules, validators, and Persian-specific validation
 */

import type { FormValues } from './forms.types'

// ==================== Validation Result Types ====================

/**
 * Validation result
 */
export interface ValidationResult {
  valid: boolean
  error?: string
  errors?: string[]
}

/**
 * Async validation result
 */
export type AsyncValidationResult = Promise<ValidationResult>

/**
 * Field validation result
 */
export type FieldValidationResult = string | null | undefined

/**
 * Async field validation result
 */
export type AsyncFieldValidationResult = Promise<FieldValidationResult>

// ==================== Validator Functions ====================

/**
 * Synchronous validator function
 */
export type ValidatorFn<T = unknown> = (
  value: T,
  context?: ValidationContext
) => FieldValidationResult

/**
 * Asynchronous validator function
 */
export type AsyncValidatorFn<T = unknown> = (
  value: T,
  context?: ValidationContext
) => AsyncFieldValidationResult

/**
 * Combined validator function (sync or async)
 */
export type Validator<T = unknown> = ValidatorFn<T> | AsyncValidatorFn<T>

/**
 * Validation context
 */
export interface ValidationContext {
  formValues?: FormValues
  field?: string
  label?: string
  [key: string]: unknown
}

// ==================== Validation Schema ====================

/**
 * Field validation schema
 */
export interface FieldValidationSchema<T = unknown> {
  required?: boolean | string
  validators?: Array<Validator<T>>
  asyncValidators?: Array<AsyncValidatorFn<T>>
  validateOn?: 'change' | 'blur' | 'submit'
  debounce?: number // For async validators
}

/**
 * Form validation schema
 */
export type FormValidationSchema<T extends FormValues = FormValues> = {
  [K in keyof T]?: FieldValidationSchema<T[K]>
}

// ==================== Built-in Validators ====================

/**
 * Required field validator options
 */
export interface RequiredOptions {
  message?: string
  trim?: boolean
  allowEmptyString?: boolean
}

/**
 * String length validator options
 */
export interface LengthOptions {
  min?: number
  max?: number
  exact?: number
  message?: string
  trimBeforeCheck?: boolean
}

/**
 * Number range validator options
 */
export interface RangeOptions {
  min?: number
  max?: number
  inclusive?: boolean
  message?: string
}

/**
 * Pattern validator options
 */
export interface PatternOptions {
  pattern: RegExp
  message?: string
  flags?: string
}

/**
 * Email validator options
 */
export interface EmailOptions {
  message?: string
  allowDisplayName?: boolean
  requireTld?: boolean
  allowIpDomain?: boolean
}

/**
 * URL validator options
 */
export interface UrlOptions {
  message?: string
  protocols?: string[]
  requireProtocol?: boolean
  requireTld?: boolean
}

/**
 * Date validator options
 */
export interface DateOptions {
  message?: string
  min?: Date | string
  max?: Date | string
  format?: string
}

/**
 * File validator options
 */
export interface FileOptions {
  message?: string
  maxSize?: number // bytes
  minSize?: number // bytes
  allowedTypes?: string[]
  allowedExtensions?: string[]
}

/**
 * Custom validator options
 */
export interface CustomValidatorOptions<T = unknown> {
  message?: string
  validator: (value: T, context?: ValidationContext) => boolean | Promise<boolean>
}

// ==================== Persian-Specific Validators ====================

/**
 * Iranian National ID (کد ملی) validator options
 */
export interface NationalIdOptions {
  message?: string
  allowForeign?: boolean // Allow non-Iranian IDs
}

/**
 * Iranian mobile number validator options
 */
export interface MobileNumberOptions {
  message?: string
  requireCountryCode?: boolean // +98 or 0098
  allowLandline?: boolean
}

/**
 * Iranian postal code validator options
 */
export interface PostalCodeOptions {
  message?: string
  format?: 'with-dash' | 'without-dash' | 'both' // 1234567890 or 12345-67890
}

/**
 * Iranian bank card number validator options
 */
export interface BankCardOptions {
  message?: string
  checkLuhn?: boolean // Validate using Luhn algorithm
  allowedBanks?: string[] // Bank BIN prefixes
}

/**
 * Iranian IBAN validator options
 */
export interface IbanOptions {
  message?: string
  checkChecksum?: boolean
}

/**
 * Persian text validator options
 */
export interface PersianTextOptions {
  message?: string
  allowNumbers?: boolean
  allowEnglish?: boolean
  allowSpecialChars?: boolean
  minWords?: number
  maxWords?: number
}

/**
 * Persian name validator options
 */
export interface PersianNameOptions {
  message?: string
  allowEnglish?: boolean
  minLength?: number
  maxLength?: number
  requireLastName?: boolean
}

/**
 * Jalaali (Persian) date validator options
 */
export interface JalaaliDateOptions {
  message?: string
  min?: string // Format: YYYY/MM/DD
  max?: string
  format?: 'YYYY/MM/DD' | 'YYYY-MM-DD' | 'YY/MM/DD'
}

// ==================== Comparison Validators ====================

/**
 * Field comparison options
 */
export interface ComparisonOptions {
  message?: string
  compareWith: string // Field name to compare with
  operator?: 'eq' | 'ne' | 'gt' | 'gte' | 'lt' | 'lte'
}

/**
 * Confirmation field options (e.g., password confirmation)
 */
export interface ConfirmationOptions {
  message?: string
  matchField: string // Field name that must match
  caseSensitive?: boolean
}

// ==================== Conditional Validators ====================

/**
 * Conditional validation options
 */
export interface ConditionalOptions<T = unknown> {
  condition: (formValues: FormValues, context?: ValidationContext) => boolean
  validators: Array<Validator<T>>
  message?: string
}

/**
 * Dependent field validation options
 */
export interface DependentOptions<T = unknown> {
  dependsOn: string | string[] // Field name(s) this field depends on
  validators: Array<Validator<T>>
  message?: string
}

// ==================== Array Validators ====================

/**
 * Array length validator options
 */
export interface ArrayLengthOptions {
  message?: string
  min?: number
  max?: number
  exact?: number
}

/**
 * Array item validator options
 */
export interface ArrayItemOptions<T = unknown> {
  message?: string
  itemValidator: Validator<T>
  allowEmpty?: boolean
}

/**
 * Unique array items validator options
 */
export interface UniqueItemsOptions<T = unknown> {
  message?: string
  identifier?: keyof T | ((item: T) => unknown)
}

// ==================== Object Validators ====================

/**
 * Object shape validator options
 */
export interface ObjectShapeOptions {
  message?: string
  shape: Record<string, Validator>
  strict?: boolean // No additional properties allowed
}

/**
 * Object keys validator options
 */
export interface ObjectKeysOptions {
  message?: string
  required?: string[]
  optional?: string[]
  forbidden?: string[]
}

// ==================== Cross-Field Validation ====================

/**
 * Cross-field validation rule
 */
export interface CrossFieldRule {
  fields: string[]
  validator: (values: Record<string, unknown>, context?: ValidationContext) => string | null
  message?: string
}

/**
 * Cross-field validation schema
 */
export interface CrossFieldSchema {
  rules: CrossFieldRule[]
}

// ==================== Async Validators ====================

/**
 * Server-side validation options
 */
export interface ServerValidationOptions {
  message?: string
  endpoint: string
  method?: 'GET' | 'POST'
  debounce?: number
  cache?: boolean
  cacheTime?: number
}

/**
 * Unique value validation options (check against database)
 */
export interface UniqueValueOptions {
  message?: string
  endpoint: string
  field: string
  excludeId?: string | number
  debounce?: number
}

/**
 * Exists validation options (check if value exists in database)
 */
export interface ExistsOptions {
  message?: string
  endpoint: string
  field: string
  debounce?: number
}

// ==================== Validation Error Types ====================

/**
 * Validation error detail
 */
export interface ValidationErrorDetail {
  field: string
  value: unknown
  message: string
  rule?: string
  params?: Record<string, unknown>
}

/**
 * Validation errors collection
 */
export interface ValidationErrors {
  [field: string]: string | string[]
}

/**
 * Validation error with details
 */
export interface DetailedValidationError {
  field: string
  errors: ValidationErrorDetail[]
}

// ==================== Validation Messages ====================

/**
 * Default validation messages (Persian)
 */
export interface ValidationMessages {
  required: string
  email: string
  url: string
  min: string
  max: string
  minLength: string
  maxLength: string
  pattern: string
  numeric: string
  alpha: string
  alphanumeric: string
  nationalId: string
  mobileNumber: string
  postalCode: string
  bankCard: string
  iban: string
  persianText: string
  persianName: string
  jalaaliDate: string
  match: string
  unique: string
  exists: string
  [key: string]: string
}

/**
 * Custom validation messages
 */
export type CustomMessages = Partial<ValidationMessages>

// ==================== Validation Utilities ====================

/**
 * Validation state
 */
export interface ValidationState {
  isValidating: boolean
  validatedFields: Set<string>
  errors: ValidationErrors
  lastValidation?: Date
}

/**
 * Validation options
 */
export interface ValidationOptions {
  abortEarly?: boolean // Stop validation on first error
  stripUnknown?: boolean // Remove fields not in schema
  context?: ValidationContext
  messages?: CustomMessages
}

// ==================== Type Guards ====================

/**
 * Check if validator is async
 */
export function isAsyncValidator<T = unknown>(
  validator: Validator<T>
): validator is AsyncValidatorFn<T> {
  return validator.constructor.name === 'AsyncFunction'
}

/**
 * Check if validation result is valid
 */
export function isValidResult(result: ValidationResult): boolean {
  return result.valid && !result.error && (!result.errors || result.errors.length === 0)
}

// ==================== Persian Regex Patterns ====================

/**
 * Common Persian validation regex patterns
 */
export const PERSIAN_PATTERNS = {
  // Persian characters only
  persianOnly: /^[\u0600-\u06FF\s]+$/,

  // Persian characters with numbers
  persianWithNumbers: /^[\u0600-\u06FF0-9\s]+$/,

  // Persian characters with English
  persianWithEnglish: /^[\u0600-\u06FFa-zA-Z\s]+$/,

  // Persian characters with numbers and English
  persianMixed: /^[\u0600-\u06FFa-zA-Z0-9\s]+$/,

  // Iranian National ID (10 digits)
  nationalId: /^\d{10}$/,

  // Iranian mobile number (09XXXXXXXXX)
  mobileNumber: /^09\d{9}$/,

  // Iranian mobile with country code
  mobileWithCountryCode: /^(\+98|0098|98)?9\d{9}$/,

  // Iranian postal code (XXXXXXXXXX or XXXXX-XXXXX)
  postalCode: /^\d{10}$|^\d{5}-\d{5}$/,

  // Iranian bank card (16 digits)
  bankCard: /^\d{16}$/,

  // Iranian IBAN (IR + 24 digits)
  iban: /^IR\d{24}$/,

  // Jalaali date (YYYY/MM/DD)
  jalaaliDate: /^(13|14)\d{2}\/(0[1-9]|1[0-2])\/(0[1-9]|[12]\d|3[01])$/,

  // Persian name (no numbers or special chars)
  persianName: /^[\u0600-\u06FF\s]+$/,
} as const

// ==================== Validator Factory Types ====================

/**
 * Validator factory function
 */
export type ValidatorFactory<T = unknown, O = unknown> = (
  options?: O
) => Validator<T>

/**
 * Async validator factory function
 */
export type AsyncValidatorFactory<T = unknown, O = unknown> = (
  options?: O
) => AsyncValidatorFn<T>
