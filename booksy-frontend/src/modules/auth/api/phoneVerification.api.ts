import { userManagementClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  SendVerificationCodeRequest,
  SendVerificationCodeResponse,
  CompleteCustomerAuthenticationRequest,
  CompleteCustomerAuthenticationResponse,
  CompleteProviderAuthenticationRequest,
  CompleteProviderAuthenticationResponse,
  ResendOtpRequest,
  ResendOtpResponse,
} from '../types/phoneVerification.types'

/**
 * Phone Verification & Authentication API
 * Base URL: /api/v1/auth
 * Backend: UserManagement API (port 5020)
 *
 * New unified flow:
 * 1. sendVerificationCode() - Send OTP to phone
 * 2. completeCustomerAuthentication() or completeProviderAuthentication() - Verify OTP + Login/Register
 */
export const phoneVerificationApi = {
  /**
   * Send OTP verification code to phone number
   * POST /api/v1/auth/send-verification-code
   */
  async sendVerificationCode(
    request: SendVerificationCodeRequest
  ): Promise<ApiResponse<SendVerificationCodeResponse>> {
    return userManagementClient.post<SendVerificationCodeResponse>(
      '/v1/auth/send-verification-code',
      {
        phoneNumber: request.phoneNumber,
        countryCode: request.countryCode || '+98',
      }
    )
  },

  /**
   * Complete customer authentication (verify code + login/register)
   * POST /api/v1/auth/customer/complete-authentication
   *
   * This endpoint:
   * - Verifies the OTP code
   * - Creates User if doesn't exist
   * - Creates Customer aggregate if doesn't exist
   * - Returns JWT tokens
   */
  async completeCustomerAuthentication(
    request: CompleteCustomerAuthenticationRequest
  ): Promise<ApiResponse<CompleteCustomerAuthenticationResponse>> {
    return userManagementClient.post<CompleteCustomerAuthenticationResponse>(
      '/v1/auth/customer/complete-authentication',
      request
    )
  },

  /**
   * Complete provider authentication (verify code + login/register)
   * POST /api/v1/auth/provider/complete-authentication
   *
   * This endpoint:
   * - Verifies the OTP code
   * - Creates User if doesn't exist
   * - Queries Provider aggregate from ServiceCatalog
   * - Returns JWT tokens with provider claims
   */
  async completeProviderAuthentication(
    request: CompleteProviderAuthenticationRequest
  ): Promise<ApiResponse<CompleteProviderAuthenticationResponse>> {
    return userManagementClient.post<CompleteProviderAuthenticationResponse>(
      '/v1/auth/provider/complete-authentication',
      request
    )
  },

  /**
   * Resend OTP code for existing verification
   * POST /api/v1/phoneverification/resend
   *
   * Note: This still uses the old endpoint as it's still available
   */
  async resendOtp(request: ResendOtpRequest): Promise<ApiResponse<ResendOtpResponse>> {
    return userManagementClient.post<ResendOtpResponse>('/v1/phoneverification/resend', request)
  },

  // ==================== DEPRECATED METHODS ====================
  // These are kept for backward compatibility but should not be used in new code

  /**
   * @deprecated Use completeCustomerAuthentication() or completeProviderAuthentication() instead
   */
  async verifyCode(phoneNumber: string, code: string): Promise<ApiResponse<any>> {
    console.warn(
      'phoneVerificationApi.verifyCode() is deprecated. Use completeCustomerAuthentication() or completeProviderAuthentication() instead.'
    )
    throw new Error(
      'This endpoint has been removed. Please use completeCustomerAuthentication() or completeProviderAuthentication()'
    )
  },

  /**
   * @deprecated Use completeCustomerAuthentication() or completeProviderAuthentication() instead
   */
  async registerFromVerifiedPhone(request: any): Promise<ApiResponse<any>> {
    console.warn(
      'phoneVerificationApi.registerFromVerifiedPhone() is deprecated. Use completeCustomerAuthentication() or completeProviderAuthentication() instead.'
    )
    throw new Error(
      'This endpoint has been removed. Please use completeCustomerAuthentication() or completeProviderAuthentication()'
    )
  },
}

export default phoneVerificationApi
