export interface SendVerificationCodeRequest {
  phoneNumber: string
  countryCode: string
}

export interface SendVerificationCodeResponse {
  success: boolean
  message: string
  maskedPhoneNumber: string
  expiresIn: number
  isNewUser?: boolean
}

export interface VerifyCodeRequest {
  phoneNumber: string
  code: string
  userType?: 'Provider' | 'Client' | 'Admin'
}

export interface VerifyCodeResponse {
  success: boolean
  accessToken?: string
  refreshToken?: string
  expiresIn?: number
  user?: UserInfo
  errorMessage?: string
  remainingAttempts?: number
  isNewUser?: boolean
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
