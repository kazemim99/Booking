// src/core/types/auth.types.ts

export interface User {
  id: string
  email: string
  fullName: string
  firstName?: string
  lastName?: string
  phoneNumber?: string
  avatarUrl?: string | null
  roles?: string[]
  permissions?: string[]
  status: 'Active' | 'Inactive' | 'Suspended' | 'Pending'
  createdAt: string
  updatedAt: string
  lastLoginAt?: string
  emailVerified?: boolean
  phoneVerified?: boolean
}

export interface AuthResponse {
  token: string
  refreshToken: string
  user: User
  expiresIn?: number
}

export interface LoginCredentials {
  email: string
  password: string
  rememberMe?: boolean
}

export interface RegisterData {
  email: string
  password: string
  confirmPassword: string
  firstName: string
  lastName: string
  phoneNumber?: string
  userType: 'Customer' | 'Provider'
  acceptTerms: boolean
}

export interface PasswordResetRequest {
  email: string
}

export interface PasswordReset {
  token: string
  password: string
  confirmPassword: string
}

export interface ChangePassword {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface VerifyEmailRequest {
  token: string
}

export interface ResendVerificationRequest {
  email: string
}

// Auth State
export interface AuthState {
  token: string | null
  refreshToken: string | null
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
  validationErrors: ValidationErrors
}

export interface ValidationErrors {
  [key: string]: string[]
}

// JWT Payload
export interface JwtPayload {
  sub: string // Subject (user ID)
  email: string
  roles: string[]
  iat: number // Issued at
  exp: number // Expiration time
  nbf?: number // Not before
}

// Role & Permission types
export type UserRole = 'Admin' | 'Provider' | 'Customer' | 'Support'

export type Permission =
  | 'read:profile'
  | 'update:profile'
  | 'delete:profile'
  | 'read:users'
  | 'create:users'
  | 'update:users'
  | 'delete:users'
  | 'read:bookings'
  | 'create:bookings'
  | 'update:bookings'
  | 'delete:bookings'
  | 'read:services'
  | 'create:services'
  | 'update:services'
  | 'delete:services'
  | 'manage:settings'

export interface RolePermissions {
  role: UserRole
  permissions: Permission[]
}
