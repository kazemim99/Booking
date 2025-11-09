import { userManagementClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  SendVerificationCodeRequest,
  SendVerificationCodeResponse,
  VerifyCodeRequest,
  VerifyCodeResponse,
  ResendOtpRequest,
  ResendOtpResponse,
} from '../types/phoneVerification.types'

/**
 * Phone Verification API
 * Base URL: /api/v1/phone-verification
 * Backend: UserManagement API (port 5020)
 */
export const phoneVerificationApi = {
  /**
   * Request phone number verification - sends OTP code
   * POST /api/v1/phone-verification/request
   */
  async sendVerificationCode(
    request: SendVerificationCodeRequest
  ): Promise<ApiResponse<SendVerificationCodeResponse>> {
    return userManagementClient.post<SendVerificationCodeResponse>(
      '/v1/phone-verification/request',
      request
    )
  },

  /**
   * Verify phone number with OTP code
   * POST /api/v1/phone-verification/verify
   */
  async verifyCode(request: VerifyCodeRequest): Promise<ApiResponse<VerifyCodeResponse>> {
    return userManagementClient.post<VerifyCodeResponse>('/v1/phone-verification/verify', request)
  },

  /**
   * Resend OTP code for existing verification
   * POST /api/v1/phone-verification/resend
   */
  async resendOtp(request: ResendOtpRequest): Promise<ApiResponse<ResendOtpResponse>> {
    return userManagementClient.post<ResendOtpResponse>('/v1/phone-verification/resend', request)
  },
}

export default phoneVerificationApi
