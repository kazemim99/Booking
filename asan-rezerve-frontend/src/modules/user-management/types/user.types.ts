import { NotificationPreferences } from "./user-profile.types"

export enum UserStatus {
  Draft = 'Draft',
  Active = 'Active',
  Inactive = 'Inactive',
  Suspended = 'Suspended',
  Deactivated = 'Deactivated',
}

export enum UserType {
  Client = 'Client',
  Provider = 'Provider',
  Admin = 'Admin',
}

export enum UserRole {
  Customer = 'Customer',
  ServiceProvider = 'ServiceProvider',
  SystemAdministrator = 'SystemAdministrator',
  BusinessOwner = 'BusinessOwner',
  Staff = 'Staff',
}

export interface User {
  lastModifiedAt: string
  privacySettings?: PrivacySettings | undefined
  id: string
  email: string | undefined
  fullName?: string | undefined
  firstName?: string
  lastName?: string
  phoneNumber?: string
  avatarUrl?: string | undefined
  permissions?: string[]
  userType: UserType
  roles: string[]
  status: UserStatus
  createdAt: string
  updatedAt?: string | undefined
  lastLoginAt?: string
  emailVerified?: boolean
  phoneVerified?: boolean
  profile: UserProfile
  preferences: UserPreferences
  metadata: UserMetadata
  activatedAt?: string | undefined
}


export interface UserProfile {
  coverImageUrl?: string
  firstName: string
  lastName: string
  phoneNumber?: string
  dateOfBirth?: string
  avatarUrl?: string
  bio?: string
  address?: Address | undefined
}

export interface Address {
  formattedAddress?: string
  street?: string
  city: string
  state: string
  postalCode: string
  country: string
  latitude?: number | undefined
  longitude?: number | undefined
}



export interface UserPreferences {
  theme: string | 'light' | 'dark' | 'auto',
  notifications: NotificationPreferences
  language: string
  timezone: string
  currency: string
  dateFormat: string,
  timeFormat: string,
  notificationSettings: NotificationSettings,
  privacySettings: PrivacySettings | undefined
}

export interface NotificationSettings {
  emailNotifications: boolean
  smsNotifications: boolean
  pushNotifications: boolean
  appointmentReminders: boolean
  promotionalEmails: boolean
}

export interface PrivacySettings {
  profileVisibility: 'public' | 'private' | 'contacts'
  showEmail: boolean
  showPhone: boolean
  showAddress: boolean
  showBirthdate: boolean
  allowSearchEngineIndexing: boolean
}

export interface UserMetadata {
  totalBookings: number
  completedBookings: number
  cancelledBookings: number
  noShows: number
  favoriteProviders: string[]
  lastActivityAt: string
}

// Authentication Types
export interface LoginCredentials {
  email: string
  password: string
}

export interface AuthToken {
  accessToken: string
  refreshToken: string
  expiresIn: number
  tokenType: string
}

export interface AuthResponse {
  user: User
  tokens: AuthToken
}

// Search & Filter
export interface UserSearchFilters {
  searchTerm?: string
  userType?: UserType
  status?: UserStatus
  role?: UserRole
  city?: string
  state?: string
  registeredAfter?: string
  registeredBefore?: string
  pageNumber?: number
  pageSize?: number
}
