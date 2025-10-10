export interface LoginRequest {
  email: string
  password: string
  rememberMe?: boolean
}

export interface LoginResponse {
  user: {
    id: string
    email: string
    roles: string[]
    permissions?: string[]
    status: string
    createdAt: string
    lastModifiedAt?: string
    profile: {
      firstName: string
      lastName: string
      avatarUrl?: string
    }
  }
  tokens: {
    accessToken: string
    refreshToken: string
  }
}

export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  phoneNumber?: string
  userType: string
}

export interface RegisterResponse {
  user: {
    id: string
    email: string
    roles: string[]
    permissions?: string[]
    status: string
    createdAt: string
    lastModifiedAt?: string
    profile: {
      firstName: string
      lastName: string
      avatarUrl?: string
    }
  }
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface RefreshTokenResponse {
  tokens: {
    accessToken: string
    refreshToken: string
  }
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  password: string
  confirmPassword: string
}

export interface VerifyEmailRequest {
  token: string
}
