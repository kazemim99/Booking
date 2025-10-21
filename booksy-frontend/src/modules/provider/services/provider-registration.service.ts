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
}

class ProviderRegistrationService {
  /**
   * Submit complete provider registration
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
