/**
 * Error Interceptor
 *
 * Global error handler for API responses with Persian error messages
 * Shows toast notifications for all errors using the toast service
 */

import type { AxiosError } from 'axios'
import { toastService } from '@/core/services/toast.service'

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
    toastService.error('خطا در برقراری ارتباط با سرور. لطفاً اتصال اینترنت خود را بررسی کنید', 'خطای شبکه')
    return Promise.reject(error)
  }

  const { status, data } = error.response

  // Handle different error codes
  switch (status) {
    case 400:
      handleValidationError(data)
      break

    case 401:
      // Handled by auth interceptor - don't show duplicate toast
      break

    case 403:
      toastService.error('شما اجازه دسترسی به این بخش را ندارید', 'دسترسی ممنوع')
      break

    case 404:
      toastService.error('اطلاعات مورد نظر یافت نشد', 'یافت نشد')
      break

    case 409:
      toastService.error('این اطلاعات قبلاً ثبت شده است', 'تداخل در داده‌ها')
      break

    case 422:
      handleValidationError(data)
      break

    case 429:
      toastService.warning('تعداد درخواست‌های شما بیش از حد مجاز است. لطفاً کمی صبر کنید', 'محدودیت درخواست')
      break

    case 500:
    case 502:
    case 503:
    case 504:
      // Show server error with the backend message if available
      const serverMessage = data?.message || 'خطای سرور رخ داده است. لطفاً بعداً تلاش کنید'
      toastService.error(serverMessage, 'خطای سرور')
      break

    default:
      toastService.error(data?.message || 'خطای نامشخصی رخ داده است', 'خطا')
  }

  return Promise.reject(error)
}

function handleValidationError(data: ApiError) {
  if (data.errors) {
    // Show all validation errors as separate toasts
    const errorMessages = Object.entries(data.errors).flatMap(([field, messages]) => {
      return messages
    })

    // Show each validation error
    errorMessages.forEach((message) => {
      toastService.error(message, 'خطای اعتبارسنجی', 6000)
    })
    return
  }

  // If no structured errors, show the general error message
  toastService.error(data.message || 'داده‌های ورودی نامعتبر است', 'خطای اعتبارسنجی')
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
