/**
 * Form Type Definitions
 * Types for form fields, validation, and state management
 */

// Re-export ValidationError for backwards compatibility
export type { ValidationError } from './common.types'

// ==================== Field Types ====================

/**
 * Input field types
 */
export type InputType =
  | 'text'
  | 'email'
  | 'password'
  | 'number'
  | 'tel'
  | 'url'
  | 'search'
  | 'date'
  | 'time'
  | 'datetime-local'
  | 'month'
  | 'week'
  | 'color'
  | 'file'
  | 'hidden'
  | 'range'
  | 'textarea'
  | 'select'
  | 'checkbox'
  | 'radio'
  | 'switch'

/**
 * Field size variants
 */
export type FieldSize = 'sm' | 'md' | 'lg'

/**
 * Field variant/style
 */
export type FieldVariant = 'default' | 'outlined' | 'filled' | 'underlined'

// ==================== Form Field Configuration ====================

/**
 * Base form field properties
 */
export interface BaseFieldProps<T = unknown> {
  name: string
  label?: string
  placeholder?: string
  helperText?: string
  required?: boolean
  disabled?: boolean
  readonly?: boolean
  size?: FieldSize
  variant?: FieldVariant
  defaultValue?: T
  value?: T
  error?: string | boolean
  icon?: string
  prefix?: string
  suffix?: string
  className?: string
  dir?: 'ltr' | 'rtl' | 'auto'
}

/**
 * Input field properties
 */
export interface InputFieldProps extends BaseFieldProps<string | number> {
  type?: Extract<
    InputType,
    | 'text'
    | 'email'
    | 'password'
    | 'number'
    | 'tel'
    | 'url'
    | 'search'
    | 'date'
    | 'time'
    | 'datetime-local'
  >
  maxLength?: number
  minLength?: number
  min?: number | string
  max?: number | string
  step?: number | string
  pattern?: string
  autocomplete?: string
  inputMode?: 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url'
}

/**
 * Textarea field properties
 */
export interface TextareaFieldProps extends BaseFieldProps<string> {
  rows?: number
  cols?: number
  maxLength?: number
  minLength?: number
  resize?: boolean
  autoGrow?: boolean
}

/**
 * Select field option
 */
export interface SelectOption<T = string | number> {
  label: string
  value: T
  disabled?: boolean
  description?: string
  icon?: string
}

/**
 * Select field properties
 */
export interface SelectFieldProps<T = string | number> extends BaseFieldProps<T> {
  options: SelectOption<T>[]
  multiple?: boolean
  searchable?: boolean
  clearable?: boolean
  loading?: boolean
  onCreate?: (value: string) => void
  emptyMessage?: string
}

/**
 * Checkbox field properties
 */
export interface CheckboxFieldProps extends BaseFieldProps<boolean> {
  checked?: boolean
  indeterminate?: boolean
  labelPosition?: 'left' | 'right'
}

/**
 * Radio group field properties
 */
export interface RadioGroupFieldProps<T = string | number> extends BaseFieldProps<T> {
  options: SelectOption<T>[]
  orientation?: 'horizontal' | 'vertical'
}

/**
 * Switch field properties
 */
export interface SwitchFieldProps extends BaseFieldProps<boolean> {
  checked?: boolean
  labelPosition?: 'left' | 'right'
  onLabel?: string
  offLabel?: string
}

/**
 * File upload field properties
 */
export interface FileFieldProps extends BaseFieldProps<File | File[]> {
  accept?: string
  multiple?: boolean
  maxSize?: number // In bytes
  maxFiles?: number
  preview?: boolean
  dropzone?: boolean
}

/**
 * Date picker field properties
 */
export interface DateFieldProps extends BaseFieldProps<Date | string> {
  format?: 'gregorian' | 'jalaali' // Persian calendar support
  minDate?: Date | string
  maxDate?: Date | string
  disabledDates?: Array<Date | string>
  enableTime?: boolean
  timeFormat?: '12' | '24'
  locale?: 'fa' | 'en'
}

/**
 * Time picker field properties
 */
export interface TimeFieldProps extends BaseFieldProps<string> {
  format?: '12' | '24'
  step?: number // Minutes step
  minTime?: string
  maxTime?: string
}

// ==================== Form Field State ====================

/**
 * Field state with validation
 */
export interface FieldState<T = unknown> {
  value: T
  error: string | null
  touched: boolean
  dirty: boolean
  validating: boolean
}

/**
 * Field metadata
 */
export interface FieldMeta {
  name: string
  label?: string
  required: boolean
  disabled: boolean
  readonly: boolean
}

// ==================== Form State ====================

/**
 * Form values type
 */
export type FormValues = Record<string, unknown>

/**
 * Form errors type
 */
export type FormErrors<T extends FormValues = FormValues> = {
  [K in keyof T]?: string
}

/**
 * Form touched fields
 */
export type FormTouched<T extends FormValues = FormValues> = {
  [K in keyof T]?: boolean
}

/**
 * Form dirty fields
 */
export type FormDirty<T extends FormValues = FormValues> = {
  [K in keyof T]?: boolean
}

/**
 * Complete form state
 */
export interface FormState<T extends FormValues = FormValues> {
  values: T
  errors: FormErrors<T>
  touched: FormTouched<T>
  dirty: FormDirty<T>
  isSubmitting: boolean
  isValidating: boolean
  isValid: boolean
  submitCount: number
  initialValues: T
}

// ==================== Form Configuration ====================

/**
 * Form submission handler
 */
export type FormSubmitHandler<T extends FormValues = FormValues> = (
  values: T,
  formState: FormState<T>
) => void | Promise<void>

/**
 * Form validation handler
 */
export type FormValidateHandler<T extends FormValues = FormValues> = (
  values: T
) => FormErrors<T> | Promise<FormErrors<T>>

/**
 * Field validation handler
 */
export type FieldValidateHandler<T = unknown> = (
  value: T,
  formValues: FormValues
) => string | null | Promise<string | null>

/**
 * Form change handler
 */
export type FormChangeHandler<T extends FormValues = FormValues> = (
  values: T,
  changedField: keyof T
) => void

/**
 * Form configuration options
 */
export interface FormConfig<T extends FormValues = FormValues> {
  initialValues: T
  validationSchema?: unknown // For use with Yup, Zod, etc.
  validate?: FormValidateHandler<T>
  onSubmit: FormSubmitHandler<T>
  onChange?: FormChangeHandler<T>
  validateOnChange?: boolean
  validateOnBlur?: boolean
  validateOnMount?: boolean
  enableReinitialize?: boolean
}

// ==================== Form Actions ====================

/**
 * Form action creators
 */
export interface FormActions<T extends FormValues = FormValues> {
  setFieldValue: (field: keyof T, value: unknown) => void
  setFieldError: (field: keyof T, error: string | null) => void
  setFieldTouched: (field: keyof T, touched: boolean) => void
  setValues: (values: Partial<T>) => void
  setErrors: (errors: FormErrors<T>) => void
  setTouched: (touched: FormTouched<T>) => void
  resetForm: (values?: T) => void
  submitForm: () => Promise<void>
  validateForm: () => Promise<FormErrors<T>>
  validateField: (field: keyof T) => Promise<string | null>
}

// ==================== Form Context ====================

/**
 * Form context value (state + actions)
 */
export interface FormContext<T extends FormValues = FormValues>
  extends FormState<T>,
    FormActions<T> {
  registerField: (name: keyof T, meta: FieldMeta) => void
  unregisterField: (name: keyof T) => void
}

// ==================== Form Validation ====================

/**
 * Validation rule function
 */
export type ValidationRuleFn<T = unknown> = (
  value: T,
  formValues?: FormValues
) => string | null

/**
 * Validation rule definition
 */
export interface ValidationRule<T = unknown> {
  rule: ValidationRuleFn<T>
  message: string
  params?: Record<string, unknown>
}

/**
 * Field validation rules
 */
export interface FieldValidationRules<T = unknown> {
  required?: boolean | string
  min?: number | { value: number; message: string }
  max?: number | { value: number; message: string }
  minLength?: number | { value: number; message: string }
  maxLength?: number | { value: number; message: string }
  pattern?: RegExp | { value: RegExp; message: string }
  email?: boolean | string
  url?: boolean | string
  custom?: ValidationRuleFn<T> | Array<ValidationRuleFn<T>>
}

/**
 * Schema-based validation (for Yup, Zod, etc.)
 */
export interface ValidationSchema {
  validate: (values: FormValues) => Promise<FormErrors> | FormErrors
  validateField?: (field: string, value: unknown) => Promise<string | null> | string | null
}

// ==================== Form Field Registration ====================

/**
 * Field registration result
 */
export interface FieldRegistration<T = unknown> {
  name: string
  value: T
  onChange: (value: T) => void
  onBlur: () => void
  error: string | null
  touched: boolean
  dirty: boolean
  disabled: boolean
  readonly: boolean
  required: boolean
}

/**
 * Register field options
 */
export interface RegisterFieldOptions {
  required?: boolean
  disabled?: boolean
  readonly?: boolean
  validate?: FieldValidateHandler
  defaultValue?: unknown
}

// ==================== Form Helpers ====================

/**
 * Helper to get nested field value
 */
export type GetFieldValue<T, K> = K extends keyof T
  ? T[K]
  : K extends `${infer P}.${infer R}`
  ? P extends keyof T
    ? GetFieldValue<T[P], R>
    : never
  : never

/**
 * Helper to set nested field value
 */
export type SetFieldValue<T, K, V> = K extends keyof T
  ? { [P in keyof T]: P extends K ? V : T[P] }
  : T

/**
 * Form field path (supports nested paths like "user.address.city")
 */
export type FieldPath<T> = T extends object
  ? {
      [K in keyof T]: K extends string
        ? T[K] extends object
          ? `${K}` | `${K}.${FieldPath<T[K]>}`
          : `${K}`
        : never
    }[keyof T]
  : never

// ==================== Form Events ====================

/**
 * Field change event
 */
export interface FieldChangeEvent<T = unknown> {
  field: string
  value: T
  previousValue: T
}

/**
 * Field blur event
 */
export interface FieldBlurEvent {
  field: string
}

/**
 * Form submit event
 */
export interface FormSubmitEvent<T extends FormValues = FormValues> {
  values: T
  errors: FormErrors<T>
  isValid: boolean
}

/**
 * Form reset event
 */
export interface FormResetEvent<T extends FormValues = FormValues> {
  values: T
}
