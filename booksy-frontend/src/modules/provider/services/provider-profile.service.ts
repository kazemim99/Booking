import { serviceCategoryClient } from '@/core/api/client/http-client'

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
}

export const providerProfileService = new ProviderProfileService()
