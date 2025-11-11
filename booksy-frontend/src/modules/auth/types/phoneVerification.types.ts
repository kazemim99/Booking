// ========================================
// Phone Verification Request/Response Types
// Backend API: /api/v1/phone-verification (UserManagement)
// NOTE: Uses camelCase - transform interceptor converts to PascalCase for .NET backend
// ========================================

/**
 * Request phone number verification - sends OTP code
 * Maps to: RequestPhoneVerificationRequest (C#)
 */
export interface SendVerificationCodeRequest {
  phoneNumber: string // Phone number to verify (supports formats: 09xxx, +989xxx)
  method?: 'SMS' | 'Call' | 'WhatsApp' // Verification method (default: SMS)
  purpose?: 'Registration' | 'Login' | 'PasswordReset' | 'PhoneChange' // Purpose (default: Registration)
  userId?: string // Optional user ID if associated with existing user
}

/**
 * Response after requesting phone verification
 * Maps to: PhoneVerificationResponse (C#)
 */
export interface SendVerificationCodeResponse {
  verificationId: string // Unique verification identifier (Guid)
  phoneNumber: string // Masked phone number (e.g., *****1234)
  method: string // Verification method used
  expiresAt: string // ISO 8601 datetime when code expires
  maxAttempts: number // Maximum verification attempts allowed
  message: string // Message to display to user
}

/**
 * Verify phone number with OTP code
 * Maps to: VerifyPhoneRequest (C#)
 */
export interface VerifyCodeRequest {
  verificationId: string // Verification ID from SendVerificationCode response
  code: string // 6-digit OTP code
}

/**
 * Response after verifying phone number
 * Maps to: VerifyPhoneResponse (C#)
 */
export interface VerifyCodeResponse {
  success: boolean // Whether verification was successful
  message: string // Message describing the result
  phoneNumber?: string // Verified phone number (if successful)
  verifiedAt?: string // ISO 8601 datetime when verified (if successful)
  remainingAttempts?: number // Remaining attempts (if failed)
  blockedUntil?: string // ISO 8601 datetime when unblocked (if blocked)
}

/**
 * Resend OTP code for existing verification
 * Maps to: ResendOtpRequest (C#)
 */
export interface ResendOtpRequest {
  verificationId: string // Verification ID to resend OTP for
}

/**
 * Response after resending OTP
 * Maps to: ResendOtpResponse (C#)
 */
export interface ResendOtpResponse {
  success: boolean // Whether resend was successful
  message: string // Message describing the result
  phoneNumber: string // Masked phone number
  expiresAt?: string // When the new code expires
  remainingResendAttempts?: number // Remaining resend attempts
  canResendAfter?: string // When resend is allowed again (if too soon)
}

/**
 * Register/login user from verified phone number
 * Maps to: RegisterFromVerifiedPhoneRequest (C#)
 */
export interface RegisterFromVerifiedPhoneRequest {
  verificationId: string // Verification ID from successful phone verification
  userType: 'Provider' | 'Customer' // Type of user account to create
  firstName?: string // Optional first name
  lastName?: string // Optional last name
}

/**
 * Response from phone-based registration/login
 * Includes JWT authentication tokens
 * Maps to: RegisterFromVerifiedPhoneResponse (C#)
 */
export interface RegisterFromVerifiedPhoneResponse {
  userId: string // Created/existing user ID
  phoneNumber: string // User's phone number
  accessToken: string // JWT access token
  refreshToken: string // JWT refresh token
  expiresIn: number // Token expiration in seconds
  tokenType: string // "Bearer"
  message: string // Success message
}

export interface UserInfo {
  id: string
  phoneNumber: string
  phoneVerified: boolean
  userType: string
  roles: string[]
  status: string // UserStatus: Draft, Active, etc.
}

export interface PhoneVerificationState {
  phoneNumber: string
  countryCode: string
  maskedPhone: string
  expiresIn: number
  isLoading: boolean
  error: string | null
  step: 'phone' | 'otp' | 'success'
  remainingAttempts: number
}

export interface CountryOption {
  code: string
  name: string
  dialCode: string
  flag: string
}

// Common country codes for the picker
export const COUNTRY_OPTIONS: CountryOption[] = [
  { code: 'DE', name: 'Germany', dialCode: '+49', flag: '🇩🇪' },
  { code: 'US', name: 'United States', dialCode: '+1', flag: '🇺🇸' },
  { code: 'GB', name: 'United Kingdom', dialCode: '+44', flag: '🇬🇧' },
  { code: 'FR', name: 'France', dialCode: '+43', flag: '🇫🇷' },
  { code: 'IT', name: 'Italy', dialCode: '+39', flag: '🇮🇹' },
  { code: 'ES', name: 'Spain', dialCode: '+34', flag: '🇪🇸' },
  { code: 'NL', name: 'Netherlands', dialCode: '+31', flag: '🇳🇱' },
  { code: 'BE', name: 'Belgium', dialCode: '+32', flag: '🇧🇪' },
  { code: 'AT', name: 'Austria', dialCode: '+43', flag: '🇦🇹' },
  { code: 'CH', name: 'Switzerland', dialCode: '+41', flag: '🇨🇭' },
  { code: 'PL', name: 'Poland', dialCode: '+48', flag: '🇵🇱' },
  { code: 'TR', name: 'Turkey', dialCode: '+90', flag: '🇹🇷' },
]

// Phone number validation patterns
export const PHONE_PATTERNS: Record<string, RegExp> = {
  DE: /^(\+49|0)[1-9]\d{1,14}$/,
  US: /^(\+1|1)?[2-9]\d{9}$/,
  GB: /^(\+44|0)[1-9]\d{8,9}$/,
  // Add more as needed
}
