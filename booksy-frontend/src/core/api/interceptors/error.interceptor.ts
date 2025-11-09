/**
 * Error Interceptor
 *
 * Global error handler for API responses with Persian error messages
 */

import type { AxiosError } from 'axios'

interface ApiError {
  message: string
  code: string
  errors?: Record<string, string[]>
}

/**
 * Global error handler for API responses
 */
export function errorInterceptor(error: AxiosError<ApiError>) {
  // Network error
  if (!error.response) {
    console.error('خطا در برقراری ارتباط با سرور')
    return Promise.reject(error)
  }

  const { status, data } = error.response

  // Handle different error codes
  switch (status) {
    case 400:
      handleValidationError(data)
      break

    case 401:
      // Handled by auth interceptor
      break

    case 403:
      console.error('شما اجازه دسترسی به این بخش را ندارید')
      break

    case 404:
      console.error('اطلاعات مورد نظر یافت نشد')
      break

    case 409:
      console.error('تداخل در داده‌ها. این اطلاعات قبلاً ثبت شده است')
      break

    case 422:
      handleValidationError(data)
      break

    case 429:
      console.error('تعداد درخواست‌های شما بیش از حد مجاز است. لطفاً کمی صبر کنید')
      break

    case 500:
    case 502:
    case 503:
      console.error('خطای سرور. لطفاً بعداً تلاش کنید')
      break

    default:
      console.error(data?.message || 'خطای نامشخص')
  }

  return Promise.reject(error)
}

function handleValidationError(data: ApiError) {
  if (data.errors) {
    // Show first validation error
    const firstError = Object.values(data.errors)[0]?.[0]
    if (firstError) {
      console.error(firstError)
      return
    }
  }

  console.error(data.message || 'داده‌های ورودی نامعتبر است')
}

/**
 * Persian error messages map
 */
export const PersianErrorMessages: Record<string, string> = {
  'Network Error': 'خطا در برقراری ارتباط با سرور',
  'Request failed': 'درخواست ناموفق بود',
  'Unauthorized': 'شما اجازه دسترسی ندارید',
  'Forbidden': 'دسترسی ممنوع',
  'Not Found': 'یافت نشد',
  'Validation Error': 'خطای اعتبارسنجی',
  'Server Error': 'خطای سرور',
  'Timeout': 'زمان درخواست به پایان رسید'
}
