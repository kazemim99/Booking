import httpClient from '@/core/api/client/http-client'
import { apiEndpoints } from '@/core/api/config/api-config'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  VerifyEmailRequest,
  RefreshTokenRequest,
  RefreshTokenResponse,
} from '../types/auth-response.types'

export const authApi = {
  /**
   * Login user
   */
  async login(credentials: LoginRequest): Promise<ApiResponse<LoginResponse>> {
    return httpClient.post<LoginResponse>(apiEndpoints.auth.login, credentials)
  },

  /**
   * Register new user
   */
  async register(data: RegisterRequest): Promise<ApiResponse<RegisterResponse>> {
    return httpClient.post<RegisterResponse>(apiEndpoints.users.register, data)
  },

  /**
   * Logout user
   */
  async logout(): Promise<ApiResponse<void>> {
    return httpClient.post<void>(apiEndpoints.auth.logout)
  },

  /**
   * Refresh access token
   */
  async refreshToken(data: RefreshTokenRequest): Promise<ApiResponse<RefreshTokenResponse>> {
    return httpClient.post<RefreshTokenResponse>(apiEndpoints.auth.refresh, data)
  },

  /**
   * Request password reset
   */
  async forgotPassword(data: ForgotPasswordRequest): Promise<ApiResponse<void>> {
    return httpClient.post<void>(apiEndpoints.auth.forgotPassword, data)
  },

  /**
   * Reset password with token
   */
  async resetPassword(data: ResetPasswordRequest): Promise<ApiResponse<void>> {
    return httpClient.post<void>(apiEndpoints.auth.resetPassword, data)
  },

  /**
   * Verify email with token
   */
  async verifyEmail(data: VerifyEmailRequest): Promise<ApiResponse<void>> {
    return httpClient.post<void>(apiEndpoints.auth.verifyEmail, data)
  },

  /**
   * Resend verification email
   */
  async resendVerification(email: string): Promise<ApiResponse<void>> {
    return httpClient.post<void>(apiEndpoints.auth.resendVerification, { email })
  },
}

export default authApi
