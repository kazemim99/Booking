/**
 * Utility functions for parsing and formatting API validation errors
 */

export interface ValidationError {
  field: string
  messages: string[]
}

export interface ParsedError {
  title: string
  message?: string
  errors?: Record<string, string[]>
  validationErrors?: ValidationError[]
}

/**
 * Parse Axios error response to extract validation errors
 */
export function parseApiError(error: any): ParsedError {
  // Default error structure
  const result: ParsedError = {
    title: 'خطا در ذخیره اطلاعات',
  }

  if (!error.response) {
    // Network error
    result.message = 'خطا در ارتباط با سرور. لطفاً اتصال اینترنت خود را بررسی کنید.'
    return result
  }

  const { status, data } = error.response

  // Handle different error formats
  if (data) {
    // Format 1: { message: string }
    if (data.message) {
      result.message = data.message
    }

    // Format 2: { errors: { field: [messages] } } - ASP.NET validation format
    if (data.errors && typeof data.errors === 'object') {
      result.errors = data.errors
      result.title = 'خطاهای اعتبارسنجی'
    }

    // Format 3: { title: string, errors: {...} } - Problem Details format
    if (data.title) {
      result.title = data.title
    }

    // Format 4: { validationErrors: [{field, messages}] }
    if (data.validationErrors && Array.isArray(data.validationErrors)) {
      result.validationErrors = data.validationErrors
      result.title = 'خطاهای اعتبارسنجی'
    }

    // Format 5: Direct error array
    if (Array.isArray(data)) {
      result.message = data.join('، ')
    }
  }

  // HTTP status-based messages
  if (!result.message && !result.errors && !result.validationErrors) {
    switch (status) {
      case 400:
        result.message = 'درخواست نامعتبر است. لطفاً اطلاعات ورودی را بررسی کنید.'
        break
      case 401:
        result.message = 'احراز هویت انجام نشده است. لطفاً مجدد وارد شوید.'
        break
      case 403:
        result.message = 'شما دسترسی لازم برای انجام این عملیات را ندارید.'
        break
      case 404:
        result.message = 'منبع مورد نظر یافت نشد.'
        break
      case 409:
        result.message = 'تضاد در داده‌ها. این عملیات قبلاً انجام شده است.'
        break
      case 422:
        result.message = 'داده‌های ارسالی قابل پردازش نیستند.'
        result.title = 'خطای اعتبارسنجی'
        break
      case 500:
        result.message = 'خطای داخلی سرور. لطفاً بعداً مجدد تلاش کنید.'
        break
      case 503:
        result.message = 'سرویس در حال حاضر در دسترس نیست. لطفاً بعداً تلاش کنید.'
        break
      default:
        result.message = `خطای سرور (کد ${status})`
    }
  }

  return result
}

/**
 * Convert validation errors to a flat list of messages
 */
export function getErrorMessages(parsedError: ParsedError): string[] {
  const messages: string[] = []

  if (parsedError.message) {
    messages.push(parsedError.message)
  }

  if (parsedError.errors) {
    Object.entries(parsedError.errors).forEach(([field, fieldErrors]) => {
      if (Array.isArray(fieldErrors)) {
        fieldErrors.forEach(err => messages.push(`${formatFieldName(field)}: ${err}`))
      } else {
        messages.push(`${formatFieldName(field)}: ${fieldErrors}`)
      }
    })
  }

  if (parsedError.validationErrors) {
    parsedError.validationErrors.forEach(ve => {
      ve.messages.forEach(msg => messages.push(`${formatFieldName(ve.field)}: ${msg}`))
    })
  }

  return messages
}

/**
 * Format field name from camelCase to readable format
 */
function formatFieldName(field: string): string {
  // Handle common patterns
  const fieldMap: Record<string, string> = {
    businessName: 'نام کسب‌وکار',
    businessDescription: 'توضیحات',
    category: 'دسته‌بندی',
    phoneNumber: 'شماره تلفن',
    email: 'ایمیل',
    addressLine1: 'آدرس',
    city: 'شهر',
    province: 'استان',
    postalCode: 'کد پستی',
    latitude: 'عرض جغرافیایی',
    longitude: 'طول جغرافیایی',
    ownerFirstName: 'نام مالک',
    ownerLastName: 'نام خانوادگی مالک',
    services: 'خدمات',
    businessHours: 'ساعات کاری',
    dayOfWeek: 'روز هفته',
    openTime: 'ساعت شروع',
    closeTime: 'ساعت پایان',
    breaks: 'استراحت‌ها',
  }

  if (fieldMap[field]) {
    return fieldMap[field]
  }

  // Fallback: Convert camelCase to readable format
  return field
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (str) => str.toUpperCase())
    .trim()
}

/**
 * Check if error is a validation error
 */
export function isValidationError(error: any): boolean {
  return (
    error.response?.status === 400 ||
    error.response?.status === 422 ||
    !!error.response?.data?.errors ||
    !!error.response?.data?.validationErrors
  )
}
