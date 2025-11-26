import { serviceCategoryClient, userManagementClient } from '@/core/api/client/http-client'

export interface UploadImageResponse {
  imageUrl: string
  thumbnailUrl?: string
}

export interface UpdateProfileRequest {
  fullName?: string
  email?: string
  profileImageUrl?: string
}

export interface UpdateBusinessInfoRequest {
  businessName?: string
  description?: string
  logoUrl?: string
}

export interface UpdateLocationRequest {
  formattedAddress: string
  country: string
  provinceId?: number
  cityId?: number
  latitude: number
  longitude: number
}

export interface BusinessHoursRequest {
  dayOfWeek: number
  isOpen: boolean
  openTime?: {
    hours: number
    minutes: number
  } | null
  closeTime?: {
    hours: number
    minutes: number
  } | null
  breaks: {
    start: {
      hours: number
      minutes: number
    }
    end: {
      hours: number
      minutes: number
    }
  }[]
}

export interface SendVerificationCodeResponse {
  success: boolean
  message: string
  expiresAt: string
}

export interface VerifyPhoneCodeResponse {
  success: boolean
  message: string
  phoneNumber?: string
  verifiedAt?: string
}

class ProviderProfileService {
  private readonly basePath = '/v1/providers'

  /**
   * Upload profile image
   */
  async uploadProfileImage(file: File): Promise<UploadImageResponse> {
    const formData = new FormData()
    formData.append('image', file)

    const response = await serviceCategoryClient.post<UploadImageResponse>(
      `${this.basePath}/profile/image`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    )

    if (!response.data) {
      throw new Error('No response data received from server')
    }

    return response.data
  }

  /**
   * Upload business logo
   */
  async uploadBusinessLogo(file: File): Promise<UploadImageResponse> {
    const formData = new FormData()
    formData.append('image', file)

    const response = await serviceCategoryClient.post<UploadImageResponse>(
      `${this.basePath}/business/logo`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    )

    if (!response.data) {
      throw new Error('No response data received from server')
    }

    return response.data
  }

  /**
   * Update provider profile information
   */
  async updateProfile(data: UpdateProfileRequest): Promise<void> {
    await serviceCategoryClient.put(`${this.basePath}/profile`, data)
  }

  /**
   * Update business information
   */
  async updateBusinessInfo(data: UpdateBusinessInfoRequest): Promise<void> {
    await serviceCategoryClient.put(`${this.basePath}/business`, data)
  }

  /**
   * Update location information
   */
  async updateLocation(providerId: string, data: UpdateLocationRequest): Promise<void> {
    await serviceCategoryClient.put(`${this.basePath}/${providerId}/location`, data)
  }

  /**
   * Update business hours
   */
  async updateBusinessHours(providerId: string, hours: BusinessHoursRequest[]): Promise<void> {
    await serviceCategoryClient.put(`${this.basePath}/${providerId}/business-hours`, {
      businessHours: hours
    })
  }

  /**
   * Get current provider profile
   */
  async getProfile(): Promise<any> {
    const response = await serviceCategoryClient.get(`${this.basePath}/profile`)
    return response.data
  }

  /**
   * Send phone verification code
   */
  async sendPhoneVerificationCode(userId: string, phoneNumber: string): Promise<SendVerificationCodeResponse> {
    const response = await userManagementClient.post<SendVerificationCodeResponse>(
      `/v1/Users/${userId}/phone/send-verification`,
      { phoneNumber }
    )

    if (!response.data) {
      throw new Error('No response data received from server')
    }

    return response.data
  }

  /**
   * Verify phone number with code
   */
  async verifyPhoneCode(
    userId: string,
    phoneNumber: string,
    verificationCode: string
  ): Promise<VerifyPhoneCodeResponse> {
    const response = await userManagementClient.post<VerifyPhoneCodeResponse>(
      `/api/v1/Users/${userId}/phone/verify`,
      { phoneNumber, verificationCode }
    )

    if (!response.data) {
      throw new Error('No response data received from server')
    }

    return response.data
  }
}

export const providerProfileService = new ProviderProfileService()
