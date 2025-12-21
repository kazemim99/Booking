/**
 * Provider Registration API Service
 * Handles complete provider registration submission
 */

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type { ProviderRegistrationData } from '../types/registration.types'

export interface RegisterProviderFullRequest {
  ownerId: string
  categoryId: string
  businessInfo: {
    businessName: string
    ownerFirstName: string
    ownerLastName: string
    phoneNumber: string
  }
  address: {
    street: string
    city: string
    postalCode: string
  }
  location?: {
    latitude: number
    longitude: number
    formattedAddress?: string
  }
  businessHours: Record<number, DayHoursRequest | null>
  services: ServiceRequest[]
  assistanceOptions: string[]
  teamMembers: TeamMemberRequest[]
}

interface DayHoursRequest {
  dayOfWeek: number
  isOpen: boolean
  openTime: TimeSlotRequest | null
  closeTime: TimeSlotRequest | null
  breaks: BreakTimeRequest[]
}

interface TimeSlotRequest {
  hours: number
  minutes: number
}

interface BreakTimeRequest {
  start: TimeSlotRequest
  end: TimeSlotRequest
}

interface ServiceRequest {
  name: string
  durationHours: number
  durationMinutes: number
  price: number
  priceType: 'fixed' | 'variable'
}

interface TeamMemberRequest {
  name: string
  email: string
  phoneNumber: string
  countryCode: string
  position: string
  isOwner: boolean
}

export interface RegisterProviderFullResponse {
  providerId: string
  businessName: string
  status: string
  registeredAt: string
  servicesCount: number
  staffCount: number
  message: string
  // Authentication tokens (returned on registration)
  accessToken?: string
  refreshToken?: string
  expiresIn?: number
}

// Progressive Registration DTOs
export interface CreateProviderDraftRequest {
  businessName: string
  businessDescription: string
  category: string
  phoneNumber: string
  email: string
  ownerFirstName: string
  ownerLastName: string
  logoUrl?: string
  addressLine1: string
  addressLine2?: string
  city: string
  province: string
  postalCode: string
  latitude: number
  longitude: number
}

export interface CreateProviderDraftResponse {
  providerId: string
  registrationStep: number
  message: string
  accessToken?: string
  refreshToken?: string
  expiresIn?: number
}

export interface GetDraftProviderResponse {
  providerId: string | null
  registrationStep: number | null
  hasDraft: boolean
  draftData: {
    providerId: string
    businessName: string
    businessDescription: string
    category: string
    phoneNumber: string
    email: string
    addressLine1: string
    addressLine2: string | null
    city: string
    province: string
    postalCode: string
    latitude: number
    longitude: number
    registrationStep: number
  } | null
}

export interface RegistrationProgressResponse {
  hasDraft: boolean
  currentStep: number | null
  providerId?: string
  draftData: {
    providerId: string
    registrationStep: number
    status: string
    businessInfo: {
      businessName: string
      businessDescription: string
      category: string
      phoneNumber: string
      email: string
      ownerFirstName?: string
      ownerLastName?: string
      logoUrl?: string
    }
    location: {
      addressLine1: string
      addressLine2: string | null
      city: string
      province: string
      postalCode: string
      latitude: number
      longitude: number
    }
    services: Array<{
      id: string
      name: string
      durationHours: number
      durationMinutes: number
      price: number
      priceType: string
    }>
    staff: Array<{
      id: string
      name: string
      email: string
      phoneNumber: string
      position: string
    }>
    businessHours: Array<{
      dayOfWeek: number
      isOpen: boolean
      openTimeHours: number | null
      openTimeMinutes: number | null
      closeTimeHours: number | null
      closeTimeMinutes: number | null
      breaks: Array<{
        startTimeHours: number
        startTimeMinutes: number
        endTimeHours: number
        endTimeMinutes: number
        label: string | null
      }>
    }>
    galleryImages: Array<{
      imageUrl: string
      thumbnailUrl: string | null
      displayOrder: number
    }>
  } | null
}

export interface CompleteRegistrationRequest {
  providerId: string
}

export interface CompleteRegistrationResponse {
  providerId: string
  status: string
  message: string
  accessToken: string | null
  refreshToken: string | null
}

class ProviderRegistrationService {
  /**
   * Map frontend category ID to backend ProviderType enum
   */
  private mapCategoryToProviderType(categoryId: string): string {
    const mapping: Record<string, string> = {
      'nail_salon': 'Salon',
      'hair_salon': 'Salon',
      'brows_lashes': 'Salon',
      'braids_locs': 'Salon',
      'massage': 'Spa',
      'barbershop': 'Salon',
      'aesthetic_medicine': 'Medical',
      'dental_orthodontics': 'Clinic',
      'hair_removal': 'Salon',
      'health_fitness': 'GymFitness',
      'home_services': 'HomeServices',
    }

    return mapping[categoryId] || 'Salon' // Default to Salon if not found
  }

  /**
   * Map backend ProviderType enum to frontend category ID
   */
  private mapProviderTypeToCategory(providerType: string): string {
    const reverseMapping: Record<string, string> = {
      'Salon': 'hair_salon',
      'Spa': 'massage',
      'Medical': 'aesthetic_medicine',
      'Clinic': 'dental_orthodontics',
      'GymFitness': 'health_fitness',
      'HomeServices': 'home_services',
    }

    return reverseMapping[providerType] || 'hair_salon' // Default to hair_salon if not found
  }

  /**
   * Create a draft provider (Step 3 - After business info, category, and location)
   */
  async createProviderDraft(
    request: CreateProviderDraftRequest,
  ): Promise<CreateProviderDraftResponse> {
    // Map category ID to ProviderType enum
    const mappedRequest = {
      ...request,
      category: this.mapCategoryToProviderType(request.category),
    }

    const response = await serviceCategoryClient.post<CreateProviderDraftResponse>(
      'v1/providers/draft',
      mappedRequest,
    )
    return response.data!
  }

  /**
   * Get the current user's draft provider
   * @deprecated Use getRegistrationProgress() instead - this endpoint may be removed
   * @see getRegistrationProgress
   */
  async getDraftProvider(): Promise<GetDraftProviderResponse> {
    const response = await serviceCategoryClient.get<GetDraftProviderResponse>(
      'v1/providers/draft',
    )

    // Map ProviderType back to frontend category ID
    if (response.data && response.data.draftData) {
      response.data.draftData.category = this.mapProviderTypeToCategory(
        response.data.draftData.category,
      )
    }

    return response.data!
  }

  /**
   * Step 3: Save location and create provider draft
   */
  async saveStep3Location(request: CreateProviderDraftRequest): Promise<CreateProviderDraftResponse> {
    const mappedRequest = {
      ...request,
      category: this.mapCategoryToProviderType(request.category),
    }

    const response = await serviceCategoryClient.post<CreateProviderDraftResponse>(
      'v1/registration/step-3/location',
      mappedRequest,
    )
    return response.data!
  }

  /**
   * Step 4: Save services to provider draft
   */
  async saveStep4Services(providerId: string, services: ServiceRequest[]): Promise<{
    providerId: string
    registrationStep: number
    servicesCount: number
    message: string
  }> {
    const response = await serviceCategoryClient.post<{
      providerId: string
      registrationStep: number
      servicesCount: number
      message: string
    }>(
      'v1/registration/step-4/services',
      {
        providerId,
        services: services.map((s: ServiceRequest) => ({
          name: s.name,
          durationHours: s.durationHours,
          durationMinutes: s.durationMinutes,
          price: s.price,
          priceType: s.priceType,
        })),
      },
    )
    return response.data!
  }

  /**
   * Step 5: Save staff members to provider draft
   */
  async saveStep5Staff(providerId: string, staffMembers: TeamMemberRequest[]): Promise<{
    providerId: string
    registrationStep: number
    staffCount: number
    message: string
  }> {
    const response = await serviceCategoryClient.post<{
      providerId: string
      registrationStep: number
      staffCount: number
      message: string
    }>(
      'v1/registration/step-5/staff',
      {
        providerId,
        staffMembers: staffMembers.map(m => ({
          name: m.name,
          email: m.email,
          phoneNumber: m.phoneNumber,
          position: m.position,
        })),
      },
    )
    return response.data!
  }

  /**
   * Step 6: Save working hours to provider draft
   */
  async saveStep6WorkingHours(providerId: string, businessHours: DayHoursRequest[]): Promise<{
    providerId: string
    registrationStep: number
    openDaysCount: number
    message: string
  }> {
    const response = await serviceCategoryClient.post<{
      providerId: string
      registrationStep: number
      openDaysCount: number
      message: string
    }>(
      'v1/registration/step-6/working-hours',
      {
        providerId,
        businessHours: businessHours.map(bh => ({
          dayOfWeek: bh.dayOfWeek,
          isOpen: bh.isOpen,
          openTime: bh.openTime,
          closeTime: bh.closeTime,
          breaks: bh.breaks,
        })),
      },
    )
    return response.data!
  }

  /**
   * Step 7: Upload gallery images and mark step as complete
   * This endpoint uploads files directly and auto-updates registration step to 7
   */
  async saveStep7Gallery(files: File[]): Promise<{
    providerId: string
    registrationStep: number
    imagesCount: number
    message: string
  }> {
    const formData = new FormData()

    files.forEach((file) => {
      formData.append('files', file)
    })

    const response = await serviceCategoryClient.post<{
      providerId: string
      registrationStep: number
      imagesCount: number
      message: string
    }>(
      'v1/registration/step-7/gallery',
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      }
    )
    return response.data!
  }

  /**
   * Step 8: Save optional feedback
   */
  async saveStep8Feedback(providerId: string, feedbackText?: string): Promise<{
    providerId: string
    registrationStep: number
    message: string
  }> {
    const response = await serviceCategoryClient.post<{
      providerId: string
      registrationStep: number
      message: string
    }>(
      'v1/registration/step-8/feedback',
      {
        providerId,
        feedbackText: feedbackText || null,
      },
    )
    return response.data!
  }

  /**
   * Step 9: Complete provider registration (Final step)
   */
  async saveStep9Complete(providerId: string): Promise<CompleteRegistrationResponse> {
    const response = await serviceCategoryClient.post<CompleteRegistrationResponse>(
      'v1/registration/step-9/complete',
      { providerId },
    )
    return response.data!
  }

  /**
   * Get current registration progress
   */
  async getRegistrationProgress(): Promise<RegistrationProgressResponse> {
    const response = await serviceCategoryClient.get<RegistrationProgressResponse>(
      'v1/registration/progress',
    )

    // Map ProviderType back to frontend category ID
    if (response.data && response.data.draftData) {
      response.data.draftData.businessInfo.category = this.mapProviderTypeToCategory(
        response.data.draftData.businessInfo.category,
      )
    }

    return response.data!
  }

  /**
   * Upload business logo during registration (before draft is created)
   * This uploads the image file and returns a URL that can be used in the draft
   */
  async uploadBusinessLogo(file: File): Promise<string> {
    const formData = new FormData()
    formData.append('image', file)

    const response = await serviceCategoryClient.post<{ imageUrl: string }>(
      'v1/registration/upload-logo',
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      }
    )
    return response.data!.imageUrl
  }

  /**
   * Complete provider registration (LEGACY - use saveStep9Complete instead)
   * @deprecated
   */
  async completeRegistration(
    providerId: string,
  ): Promise<CompleteRegistrationResponse> {
    const response = await serviceCategoryClient.post<CompleteRegistrationResponse>(
      `v1/providers/${providerId}/complete`,
      { providerId },
    )
    return response.data!
  }

  /**
   * Submit complete provider registration (LEGACY - will be deprecated)
   */
  async registerProviderFull(
    registrationData: ProviderRegistrationData,
  ): Promise<RegisterProviderFullResponse> {
    // Transform frontend data to backend DTO format
    const request: RegisterProviderFullRequest = this.mapToRequest(registrationData)

    const response = await serviceCategoryClient.post<RegisterProviderFullResponse>(
      'v1/providers/register-full',
      request,
    )

    return response.data!
  }

  /**
   * Map frontend registration data to backend request format
   */
  private mapToRequest(data: ProviderRegistrationData): RegisterProviderFullRequest {
    // Map business hours (convert array to dictionary)
    const businessHours: Record<number, DayHoursRequest | null> = {}
    data.businessHours.forEach((dayHours) => {
      if (!dayHours.isOpen) {
        businessHours[dayHours.dayOfWeek] = null
        return
      }

      businessHours[dayHours.dayOfWeek] = {
        dayOfWeek: dayHours.dayOfWeek,
        isOpen: dayHours.isOpen,
        openTime: dayHours.openTime
          ? {
            hours: dayHours.openTime.hours,
            minutes: dayHours.openTime.minutes,
          }
          : null,
        closeTime: dayHours.closeTime
          ? {
            hours: dayHours.closeTime.hours,
            minutes: dayHours.closeTime.minutes,
          }
          : null,
        breaks: dayHours.breaks.map((br) => ({
          start: {
            hours: br.start.hours,
            minutes: br.start.minutes,
          },
          end: {
            hours: br.end.hours,
            minutes: br.end.minutes,
          },
        })),
      }
    })

    // Map services
    const services: ServiceRequest[] = data.services.map((service) => ({
      name: service.name,
      durationHours: service.durationHours,
      durationMinutes: service.durationMinutes,
      price: service.price,
      priceType: service.priceType,
    }))

    // Map team members
    const teamMembers: TeamMemberRequest[] = data.teamMembers.map((member) => ({
      name: member.name,
      email: member.email,
      phoneNumber: member.phoneNumber,
      countryCode: member.countryCode,
      position: member.position,
      isOwner: member.isOwner,
    }))

    // Build complete request
    return {
      ownerId: data.userId,
      categoryId: data.categoryId || '',
      businessInfo: {
        businessName: data.businessInfo.businessName || '',
        ownerFirstName: data.businessInfo.ownerFirstName || '',
        ownerLastName: data.businessInfo.ownerLastName || '',
        phoneNumber: data.businessInfo.phoneNumber || '',
      },
      address: {
        street: data.address.addressLine1 || '',
        city: data.address.city || '',
        postalCode: data.address.zipCode || '',
      },
      location:
        data.location.latitude && data.location.longitude
          ? {
            latitude: data.location.latitude,
            longitude: data.location.longitude,
            formattedAddress: data.location.formattedAddress,
          }
          : undefined,
      businessHours,
      services,
      assistanceOptions: data.assistanceOptions,
      teamMembers,
    }
  }
}

export const providerRegistrationService = new ProviderRegistrationService()
