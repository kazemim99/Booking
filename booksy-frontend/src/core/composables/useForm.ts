import { reactive, computed, ref, toRaw } from 'vue'

export interface FormField {
  value: unknown
  error: string | null
  touched: boolean
  dirty: boolean
}

export type FormValidations = Record<string, (value: unknown) => string | null>

export interface ValidationErrors {
  [field: string]: string[]
}

interface ErrorWithMessage {
  message?: string
}

export function useForm<T extends Record<string, unknown>>(
  initialValues: T,
  validations?: FormValidations,
) {
  const formData = reactive<T>({ ...initialValues })

  const fields = reactive<Record<string, FormField>>(
    Object.keys(initialValues).reduce(
      (acc, key) => {
        acc[key] = {
          value: initialValues[key],
          error: null,
          touched: false,
          dirty: false,
        }
        return acc
      },
      {} as Record<string, FormField>,
    ),
  )

  const isSubmitting = ref(false)
  const submitError = ref<string | null>(null)
  const serverErrors = ref<ValidationErrors>({})

  const isValid = computed(() => {
    return Object.values(fields).every((field) => !field.error)
  })

  const isDirty = computed(() => {
    return Object.values(fields).some((field) => field.dirty)
  })

  const hasErrors = computed(() => {
    return Object.keys(serverErrors.value).length > 0 || !isValid.value
  })

  function validateField(fieldName: string): void {
    const field = fields[fieldName]
    if (!field) return

    // Clear server error when user starts typing
    if (serverErrors.value[fieldName]) {
      delete serverErrors.value[fieldName]
    }

    // Run client-side validation
    if (validations?.[fieldName]) {
      field.error = validations[fieldName](formData[fieldName])
    }
  }

  function validateAll(): boolean {
    if (!validations) return true

    Object.keys(formData).forEach((key) => {
      validateField(key)
    })

    return isValid.value
  }

  function setFieldValue(fieldName: string, value: unknown): void {
    if (fieldName in formData) {
      ;(formData as Record<string, unknown>)[fieldName] = value
      fields[fieldName].value = value
      fields[fieldName].dirty = true
      validateField(fieldName)
    }
  }

  function setFieldTouched(fieldName: string, touched = true): void {
    if (fields[fieldName]) {
      fields[fieldName].touched = touched
      if (touched) {
        validateField(fieldName)
      }
    }
  }

  function setFieldError(fieldName: string, error: string | null): void {
    if (fields[fieldName]) {
      fields[fieldName].error = error
    }
  }

  // ✅ FIXED: Case-insensitive error matching for server errors
  function setServerErrors(errors: ValidationErrors): void {
    // Create a normalized version with case-insensitive field matching
    const normalizedErrors: ValidationErrors = {}

    Object.keys(errors).forEach((errorKey) => {
      // Try to find matching field name (case-insensitive)
      const matchingFieldKey = Object.keys(formData).find(
        (fieldKey) => fieldKey.toLowerCase() === errorKey.toLowerCase(),
      )

      // Use the matched field key (or lowercase version if no match)
      const targetKey = matchingFieldKey || errorKey.toLowerCase()
      normalizedErrors[targetKey] = errors[errorKey]

      // Set error on the individual field if it exists
      if (fields[targetKey]) {
        // Use the first error message for the field
        fields[targetKey].error = errors[errorKey][0] || null
      }
    })

    serverErrors.value = normalizedErrors
  }

  function clearServerErrors(): void {
    serverErrors.value = {}
  }

  function clearAllErrors(): void {
    Object.keys(fields).forEach((key) => {
      fields[key].error = null
    })
    serverErrors.value = {}
    submitError.value = null
  }

  function getFieldValue(fieldName: string): unknown {
    return formData[fieldName]
  }

  function getFieldError(fieldName: string): string | null {
    // Return client-side validation error or server error
    return fields[fieldName]?.error || null
  }

  function getServerFieldErrors(fieldName: string): string[] {
    return serverErrors.value[fieldName] || []
  }

  function getAllServerErrors(): string[] {
    return Object.values(serverErrors.value).flat()
  }

  function isFieldTouched(fieldName: string): boolean {
    return fields[fieldName]?.touched || false
  }

  function isFieldDirty(fieldName: string): boolean {
    return fields[fieldName]?.dirty || false
  }

  function reset(): void {
    Object.keys(formData).forEach((key) => {
      ;(formData as Record<string, unknown>)[key] = initialValues[key]
      fields[key] = {
        value: initialValues[key],
        error: null,
        touched: false,
        dirty: false,
      }
    })
    isSubmitting.value = false
    submitError.value = null
    serverErrors.value = {}
  }

  async function handleSubmit(onSubmit: (values: T) => Promise<void> | void): Promise<void> {
    // Clear previous errors
    clearServerErrors()
    submitError.value = null

    // Validate all fields
    if (!validateAll()) {
      return
    }

    try {
      isSubmitting.value = true

      // Convert reactive object to plain object
      const plainData = toRaw(formData) as T

      // Create a clean copy of the data
      const submitData = Object.keys(plainData).reduce((acc, key) => {
        acc[key as keyof T] = plainData[key as keyof T]
        return acc
      }, {} as T)

      await onSubmit(submitData)
    } catch (error: unknown) {
      const errorWithMessage = error as ErrorWithMessage & {
        errors?: ValidationErrors
        statusCode?: number
      }

      // Handle validation errors (422 or 400 with errors object)
      if (errorWithMessage.errors) {
        setServerErrors(errorWithMessage.errors)
        submitError.value = 'Please fix the validation errors below'
      } else {
        submitError.value = errorWithMessage.message || 'An error occurred'
      }

      throw error
    } finally {
      isSubmitting.value = false
    }
  }

  return {
    formData,
    fields,
    isValid,
    isDirty,
    hasErrors,
    isSubmitting,
    submitError,
    serverErrors,
    setFieldValue,
    setFieldTouched,
    setFieldError,
    setServerErrors,
    clearServerErrors,
    clearAllErrors,
    getFieldValue,
    getFieldError,
    getServerFieldErrors,
    getAllServerErrors,
    isFieldTouched,
    isFieldDirty,
    validateField,
    validateAll,
    handleSubmit,
    reset,
  }
}
