/**
 * User Type Enumeration
 * Re-export from main enums file for modular access
 */

export { UserType } from '../enums.types'

// Additional user type specific helpers
export const USER_TYPE_LABELS: Record<string, string> = {
  individual: 'فردی',
  business: 'تجاری',
  professional: 'حرفه‌ای',
}

export const USER_TYPE_DESCRIPTIONS: Record<string, string> = {
  individual: 'کاربر عادی',
  business: 'حساب تجاری',
  professional: 'ارائه‌دهنده خدمات حرفه‌ای',
}
