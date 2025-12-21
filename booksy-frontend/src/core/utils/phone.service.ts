/**
 * Phone number formatting and validation utilities
 * Centralized phone operations for consistent handling across components
 */

/**
 * Format phone number for display
 * @param phoneNumber - Raw phone number
 * @returns Formatted phone number
 */
export function formatPhone(phoneNumber?: string): string {
  if (!phoneNumber) return ''

  // Remove all non-digit characters
  const cleaned = phoneNumber.replace(/\D/g, '')

  // Handle Iranian phone numbers (11 digits starting with 0)
  if (cleaned.length === 11 && cleaned.startsWith('0')) {
    return cleaned
  }

  // Handle international format (+98)
  if (cleaned.length === 12 && cleaned.startsWith('98')) {
    return `0${cleaned.substring(2)}`
  }

  // Return cleaned if valid length, otherwise original
  return cleaned.length >= 10 ? cleaned : phoneNumber
}

/**
 * Format phone number with dashes or spaces
 * @param phoneNumber - Raw phone number
 * @param format - Format type: 'dashes' (999-999-9999) or 'spaces' (999 999 9999)
 * @returns Formatted phone number
 */
export function formatPhoneDisplay(phoneNumber?: string, format: 'dashes' | 'spaces' = 'spaces'): string {
  if (!phoneNumber) return ''

  const cleaned = phoneNumber.replace(/\D/g, '')

  if (cleaned.length === 11) {
    // Iranian format: 0912 123 4567 or 0912-123-4567
    const part1 = cleaned.substring(0, 4)
    const part2 = cleaned.substring(4, 7)
    const part3 = cleaned.substring(7)
    const separator = format === 'dashes' ? '-' : ' '
    return `${part1}${separator}${part2}${separator}${part3}`
  }

  if (cleaned.length === 10) {
    // US format: 912 123 4567 or 912-123-4567
    const part1 = cleaned.substring(0, 3)
    const part2 = cleaned.substring(3, 6)
    const part3 = cleaned.substring(6)
    const separator = format === 'dashes' ? '-' : ' '
    return `${part1}${separator}${part2}${separator}${part3}`
  }

  return cleaned
}

/**
 * Validate Iranian mobile phone number
 * @param phoneNumber - Phone number to validate
 * @returns True if valid Iranian mobile number
 */
export function isValidIranianMobile(phoneNumber: string): boolean {
  if (!phoneNumber || typeof phoneNumber !== 'string') return false

  const cleaned = phoneNumber.replace(/\D/g, '')

  // Iranian mobile numbers: 11 digits starting with 09
  if (cleaned.length === 11 && cleaned.startsWith('09')) {
    return true
  }

  // International format: 12 digits starting with 989
  if (cleaned.length === 12 && cleaned.startsWith('989')) {
    return true
  }

  // Old format: 11 digits starting with 0 but not 09
  if (cleaned.length === 11 && cleaned.startsWith('0') && !cleaned.startsWith('09')) {
    return true
  }

  return false
}

/**
 * Validate Iranian landline phone number
 * @param phoneNumber - Phone number to validate
 * @returns True if valid Iranian landline number
 */
export function isValidIranianLandline(phoneNumber: string): boolean {
  if (!phoneNumber || typeof phoneNumber !== 'string') return false

  const cleaned = phoneNumber.replace(/\D/g, '')

  // Landline: 10-11 digits starting with 0, not starting with 09
  if (cleaned.length >= 10 && cleaned.length <= 11 && cleaned.startsWith('0') && !cleaned.startsWith('09')) {
    return true
  }

  return false
}

/**
 * Validate any phone number format (Iranian mobile, landline, or international)
 * @param phoneNumber - Phone number to validate
 * @returns True if valid phone number
 */
export function isValidPhoneNumber(phoneNumber: string): boolean {
  if (!phoneNumber || typeof phoneNumber !== 'string') return false

  const cleaned = phoneNumber.replace(/\D/g, '')

  // Minimum 10 digits, maximum 15 digits (international standard)
  return cleaned.length >= 10 && cleaned.length <= 15
}

/**
 * Convert phone number to international format
 * @param phoneNumber - Phone number to convert
 * @returns Phone number in international format (with + prefix)
 */
export function toInternationalFormat(phoneNumber?: string): string {
  if (!phoneNumber) return ''

  const cleaned = phoneNumber.replace(/\D/g, '')

  // If starts with 0 (Iranian format), convert to +98
  if (cleaned.startsWith('0')) {
    return `+98${cleaned.substring(1)}`
  }

  // If already starts with 98, just add +
  if (cleaned.startsWith('98')) {
    return `+${cleaned}`
  }

  // For other numbers, try to add +
  return `+${cleaned}`
}

/**
 * Convert international phone number to local format
 * @param phoneNumber - International phone number
 * @returns Phone number in local format (e.g., for Iran: starts with 0)
 */
export function toLocalFormat(phoneNumber?: string): string {
  if (!phoneNumber) return ''

  const cleaned = phoneNumber.replace(/\D/g, '')

  // If starts with 98 (Iran country code), convert to 0
  if (cleaned.startsWith('98')) {
    return `0${cleaned.substring(2)}`
  }

  // If starts with + and 98, remove + and convert
  if (phoneNumber.startsWith('+98')) {
    return `0${cleaned.substring(2)}`
  }

  return cleaned
}

/**
 * Get phone number country code
 * @param phoneNumber - Phone number
 * @returns Country code (e.g., "+98" for Iran, "+1" for US)
 */
export function getPhoneCountryCode(phoneNumber?: string): string {
  if (!phoneNumber) return ''

  if (phoneNumber.includes('+98')) return '+98'
  if (phoneNumber.includes('0098')) return '+98'
  if (phoneNumber.startsWith('09')) return '+98'

  const cleaned = phoneNumber.replace(/\D/g, '')

  // Guess based on length and start
  if (cleaned.startsWith('98')) return '+98'
  if (cleaned.startsWith('1')) return '+1'
  if (cleaned.length === 11 && cleaned.startsWith('0')) return '+98'

  return ''
}

/**
 * Normalize phone number to standard format
 * @param phoneNumber - Phone number to normalize
 * @returns Normalized phone number
 */
export function normalizePhoneNumber(phoneNumber?: string): string {
  if (!phoneNumber) return ''

  // Remove all non-digit characters except +
  let cleaned = phoneNumber.replace(/[^\d+]/g, '')

  // Remove multiple + signs
  cleaned = cleaned.replace(/\+{2,}/g, '+')

  // If + is not at start, move it to start
  if (cleaned.includes('+') && !cleaned.startsWith('+')) {
    cleaned = '+' + cleaned.replace(/\+/g, '')
  }

  return cleaned
}

/**
 * Mask phone number for display (e.g., for security)
 * @param phoneNumber - Phone number to mask
 * @param showChars - Number of characters to show at the end (default: 4)
 * @returns Masked phone number (e.g., "**** **** 4567")
 */
export function maskPhoneNumber(phoneNumber?: string, showChars: number = 4): string {
  if (!phoneNumber) return ''

  const cleaned = phoneNumber.replace(/\D/g, '')

  if (cleaned.length <= showChars) {
    return cleaned
  }

  const maskLength = cleaned.length - showChars
  return '*'.repeat(maskLength) + cleaned.substring(maskLength)
}

/**
 * Get phone number carrier type (mobile vs landline)
 * @param phoneNumber - Iranian phone number
 * @returns 'mobile', 'landline', or 'unknown'
 */
export function getIranianPhoneType(phoneNumber: string): 'mobile' | 'landline' | 'unknown' {
  if (!phoneNumber) return 'unknown'

  const cleaned = phoneNumber.replace(/\D/g, '')

  if (isValidIranianMobile(cleaned)) {
    return 'mobile'
  }

  if (isValidIranianLandline(cleaned)) {
    return 'landline'
  }

  return 'unknown'
}
