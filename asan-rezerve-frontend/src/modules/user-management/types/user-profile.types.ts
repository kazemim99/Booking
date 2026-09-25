// src/modules/user-management/types/user-profile.types.ts

import { Address, PrivacySettings, UserPreferences } from "./user.types"

/**
 * User Profile Domain Types
 * Aligned with UserManagement Bounded Context
 */

// ============================================
// Core Profile Types
// ============================================

export interface UserProfile {
  id: string
  email: string | undefined
  firstName: string
  lastName: string
  phoneNumber?: string
  dateOfBirth?: string
  bio?: string
  avatarUrl?: string
  coverImageUrl?: string
  address?: Address
  preferences: UserPreferences
  privacySettings: PrivacySettings
  status: UserStatus
  userType: UserType
  createdAt: string
  updatedAt: string
  lastLoginAt?: string
  emailVerified: boolean
  phoneVerified: boolean
}

// export interface Address {
//   street: string
//   city: string
//   state: string
//   postalCode: string
//   country: string
//   latitude?: number
//   longitude?: number
// }

// export interface UserPreferences {
//   language: string
//   timezone: string
//   currency: string
//   dateFormat: string
//   timeFormat: '12h' | '24h'
//   notifications: NotificationPreferences
//   theme: 'light' | 'dark' | 'auto'
// }

export interface NotificationPreferences {
  email: boolean
  sms: boolean
  push: boolean
  marketing: boolean
  bookingReminders: boolean
  promotions: boolean
  newsletter: boolean
}

// export interface PrivacySettings {
//   profileVisibility: 'public' | 'private' | 'contacts'
//   showEmail: boolean
//   showPhone: boolean
//   showAddress: boolean
//   showBirthdate: boolean
//   allowSearchEngineIndexing: boolean
// }

export enum UserStatus {
  Active = 'Active',
  Inactive = 'Inactive',
  Suspended = 'Suspended',
  Draft = 'Draft',
}

export enum UserType {
  Client = 'Client',
  Provider = 'Provider',
  Admin = 'Admin',
}

// ============================================
// API Request/Response Types
// ============================================

export interface UpdateProfileRequest {
  firstName: string
  lastName: string
  phoneNumber?: string
  dateOfBirth?: string
  bio?: string
  address?: Address
}

export interface UpdatePreferencesRequest {
  language?: string
  timezone?: string
  currency?: string
  dateFormat?: string
  timeFormat?: '12h' | '24h'
  theme?: 'light' | 'dark' | 'auto'
  notifications?: NotificationPreferences
}

export interface UpdatePrivacySettingsRequest {
  profileVisibility?: 'public' | 'private' | 'contacts'
  showEmail?: boolean
  showPhone?: boolean
  showAddress?: boolean
  showBirthdate?: boolean
  allowSearchEngineIndexing?: boolean
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

export interface UploadAvatarRequest {
  file: File
}

export interface UserProfileResponse {
  success: boolean
  data: UserProfile
  message?: string
}

export interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
  errors?: Record<string, string[]>
}

// ============================================
// Form State Types
// ============================================

export interface ProfileFormState {
  firstName: string
  lastName: string
  phoneNumber: string
  dateOfBirth: string
  bio: string
  address: Address
}

export interface PreferencesFormState {
  language: string
  timezone: string
  currency: string
  dateFormat: string
  timeFormat: '12h' | '24h'
  theme: 'light' | 'dark' | 'auto'
  notifications: NotificationPreferences
}

export type PrivacyFormState = PrivacySettings

export interface PasswordFormState {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

// ============================================
// Validation Types
// ============================================

export interface ValidationErrors {
  [key: string]: string
}

export interface FormValidation {
  isValid: boolean
  errors: ValidationErrors
}

// ============================================
// UI State Types
// ============================================

export interface ProfileTab {
  id: string
  label: string
  icon: string
  component?: string
}

export interface ProfileStats {
  totalBookings: number
  completedBookings: number
  cancelledBookings: number
  totalSpent: number
  favoriteProviders: number
  reviewsGiven: number
}

// ============================================
// Utility Types
// ============================================

export type ProfileSection = 'personal' | 'preferences' | 'privacy' | 'security' | 'stats'

export interface AvatarUploadProgress {
  uploading: boolean
  progress: number
  error?: string
}
export type { PrivacySettings }

