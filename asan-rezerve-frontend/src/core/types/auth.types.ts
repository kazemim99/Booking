// src/core/types/auth.types.ts

import { User, UserType } from "@/modules/user-management/types/user.types"
import type { ValidationErrors } from './common.types'

// Re-export commonly duplicated types from api.types.ts and enums.types.ts
export type { AuthResponse, RefreshTokenRequest } from './api.types'
export type { UserRole } from './enums.types'
export { ValidationErrors }

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
  userType: UserType
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


// JWT Payload
export interface JwtPayload {
  sub: string // Subject (user ID)
  email: string
  roles: string[]
  iat: number // Issued at
  exp: number // Expiration time
  nbf?: number // Not before
}

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
