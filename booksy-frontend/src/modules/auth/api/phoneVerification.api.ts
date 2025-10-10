import { userManagementClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  SendVerificationCodeRequest,
  SendVerificationCodeResponse,
  VerifyCodeRequest,
  VerifyCodeResponse,
} from '../types/phoneVerification.types'

export const phoneVerificationApi = {
  /**
   * Send verification code to phone number
   */
  async sendVerificationCode(
    request: SendVerificationCodeRequest
  ): Promise<ApiResponse<SendVerificationCodeResponse>> {
    return userManagementClient.post<SendVerificationCodeResponse>(
      '/v1/auth/send-verification-code',
      request
    )
  },

  /**
   * Verify OTP code
   */
  async verifyCode(request: VerifyCodeRequest): Promise<ApiResponse<VerifyCodeResponse>> {
    return userManagementClient.post<VerifyCodeResponse>('/v1/auth/verify-code', request)
  },
}

export default phoneVerificationApi
