/**
 * Availability Service
 * Handles availability checking and slot retrieval for bookings
 */

import { httpClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/availability`

// ==================== Types ====================

/**
 * Time slot with availability status
 */
export interface TimeSlot {
  startTime: string // ISO 8601 format
  endTime: string
  durationMinutes: number
  isAvailable: boolean
  availableStaffId?: string
  availableStaffName?: string
  // Legacy fields for backward compatibility
  available?: boolean
  staffMemberId?: string
  staffName?: string
  reason?: string // Why unavailable
}

/**
 * Available date (for calendar view)
 */
export interface AvailableDate {
  date: string // YYYY-MM-DD format
  hasAvailability: boolean
  totalSlots: number
  availableSlots: number
}

/**
 * Availability check result
 */
export interface AvailabilityCheckResult {
  available: boolean
  conflictingBookingId?: string
  reason?: string
  alternativeSlots?: TimeSlot[]
}

/**
 * Request for getting available slots
 */
export interface GetSlotsRequest {
  providerId: string
  serviceId: string
  date: string // YYYY-MM-DD format
  staffMemberId?: string
}

/**
 * Request for checking specific time availability
 */
export interface CheckAvailabilityRequest {
  providerId: string
  serviceId: string
  startTime: string // ISO 8601 format
  staffMemberId?: string
}

/**
 * Request for getting available dates
 */
export interface GetAvailableDatesRequest {
  providerId: string
  serviceId: string
  fromDate: string // YYYY-MM-DD format
  toDate: string // YYYY-MM-DD format
  staffMemberId?: string
}

/**
 * Get slots response
 */
export interface GetSlotsResponse {
  date: string
  providerId: string
  serviceId: string
  slots: TimeSlot[]
  validationMessages?: string[]
  timezone?: string
}

/**
 * Get available dates response
 */
export interface GetAvailableDatesResponse {
  dates: AvailableDate[]
  totalDays: number
  availableDays: number
}

/**
 * Qualified staff member
 */
export interface QualifiedStaffMember {
  id: string
  name: string
  photoUrl?: string
  rating?: number
  reviewCount?: number
  specialization?: string
}

/**
 * Get qualified staff response
 */
export interface GetQualifiedStaffResponse {
  providerId: string
  serviceId: string
  qualifiedStaff: QualifiedStaffMember[]
}

// ==================== Availability Service Class ====================

class AvailabilityService {
  // ============================================
  // Get Available Slots
  // ============================================

  /**
   * Get available time slots for a specific date
   * GET /api/v1/availability/slots?providerId={providerId}&serviceId={serviceId}&date=2025-11-15
   *
   * Example response:
   * {
   *   "date": "2025-11-15",
   *   "providerId": "provider-123",
   *   "serviceId": "service-456",
   *   "slots": [
   *     {
   *       "startTime": "2025-11-15T09:00:00Z",
   *       "endTime": "2025-11-15T10:00:00Z",
   *       "available": true,
   *       "staffMemberId": "staff-789"
   *     },
   *     {
   *       "startTime": "2025-11-15T10:00:00Z",
   *       "endTime": "2025-11-15T11:00:00Z",
   *       "available": false,
   *       "reason": "Already booked"
   *     }
   *   ],
   *   "timezone": "Asia/Tehran"
   * }
   */
  async getAvailableSlots(request: GetSlotsRequest): Promise<GetSlotsResponse> {
    try {
      console.log('[AvailabilityService] Getting available slots:', request)

      const params: Record<string, string> = {
        providerId: request.providerId,
        serviceId: request.serviceId,
        date: request.date,
      }

      if (request.staffMemberId) {
        params.staffMemberId = request.staffMemberId
      }

      const response = await httpClient.get<ApiResponse<GetSlotsResponse>>(
        `${API_BASE}/slots`,
        { params }
      )

      console.log('[AvailabilityService] Slots retrieved:', response.data)

      // Handle wrapped response format
      const data = response.data?.data || response.data
      return data as GetSlotsResponse
    } catch (error) {
      console.error('[AvailabilityService] Error getting slots:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Get available slots with Persian date support
   * Converts Jalaali date to Gregorian before API call
   */
  async getAvailableSlotsJalaali(
    providerId: string,
    serviceId: string,
    jalaaliDate: string, // YYYY/MM/DD format
    staffMemberId?: string
  ): Promise<GetSlotsResponse> {
    // TODO: Convert Jalaali to Gregorian using moment-jalaali or similar
    // For now, assuming date is already Gregorian
    return this.getAvailableSlots({
      providerId,
      serviceId,
      date: jalaaliDate.replace(/\//g, '-'),
      staffMemberId,
    })
  }

  // ============================================
  // Check Specific Time Availability
  // ============================================

  /**
   * Check if a specific time slot is available
   * GET /api/v1/availability/check?providerId={providerId}&serviceId={serviceId}&startTime=2025-11-15T10:00:00
   *
   * Example response:
   * {
   *   "available": true,
   *   "alternativeSlots": [
   *     {
   *       "startTime": "2025-11-15T11:00:00Z",
   *       "endTime": "2025-11-15T12:00:00Z",
   *       "available": true
   *     }
   *   ]
   * }
   */
  async checkAvailability(request: CheckAvailabilityRequest): Promise<AvailabilityCheckResult> {
    try {
      console.log('[AvailabilityService] Checking availability:', request)

      const params: Record<string, string> = {
        providerId: request.providerId,
        serviceId: request.serviceId,
        startTime: request.startTime,
      }

      if (request.staffMemberId) {
        params.staffMemberId = request.staffMemberId
      }

      const response = await httpClient.get<ApiResponse<AvailabilityCheckResult>>(
        `${API_BASE}/check`,
        { params }
      )

      console.log('[AvailabilityService] Availability check result:', response.data)

      // Handle wrapped response format
      const data = response.data?.data || response.data
      return data as AvailabilityCheckResult
    } catch (error) {
      console.error('[AvailabilityService] Error checking availability:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Get Qualified Staff
  // ============================================

  /**
   * Get qualified staff members for a service
   * GET /api/v1/services/provider/{providerId}/{serviceId}/qualified-staff
   *
   * Example response:
   * {
   *   "providerId": "provider-123",
   *   "serviceId": "service-456",
   *   "qualifiedStaff": [
   *     {
   *       "id": "staff-789",
   *       "name": "احمد محمدی",
   *       "photoUrl": "https://...",
   *       "rating": 4.8,
   *       "reviewCount": 120,
   *       "specialization": "متخصص پوست"
   *     }
   *   ]
   * }
   */
  async getQualifiedStaff(providerId: string, serviceId: string): Promise<GetQualifiedStaffResponse> {
    try {
      console.log('[AvailabilityService] Getting qualified staff:', { providerId, serviceId })

      const response = await httpClient.get<ApiResponse<GetQualifiedStaffResponse>>(
        `/v1/services/provider/${providerId}/${serviceId}/qualified-staff`
      )

      console.log('[AvailabilityService] Qualified staff retrieved:', response.data)

      // Handle wrapped response format
      const data = response.data?.data || response.data
      return data as GetQualifiedStaffResponse
    } catch (error) {
      console.error('[AvailabilityService] Error getting qualified staff:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Get Available Dates
  // ============================================

  /**
   * Get available dates within a date range (for calendar view)
   * GET /api/v1/availability/dates?providerId={providerId}&serviceId={serviceId}&fromDate=2025-11-10&toDate=2025-11-30
   *
   * Example response:
   * {
   *   "dates": [
   *     {
   *       "date": "2025-11-15",
   *       "hasAvailability": true,
   *       "totalSlots": 8,
   *       "availableSlots": 5
   *     },
   *     {
   *       "date": "2025-11-16",
   *       "hasAvailability": false,
   *       "totalSlots": 8,
   *       "availableSlots": 0
   *     }
   *   ],
   *   "totalDays": 20,
   *   "availableDays": 15
   * }
   */
  async getAvailableDates(request: GetAvailableDatesRequest): Promise<GetAvailableDatesResponse> {
    try {
      console.log('[AvailabilityService] Getting available dates:', request)

      const params: Record<string, string> = {
        providerId: request.providerId,
        serviceId: request.serviceId,
        fromDate: request.fromDate,
        toDate: request.toDate,
      }

      if (request.staffMemberId) {
        params.staffMemberId = request.staffMemberId
      }

      const response = await httpClient.get<ApiResponse<GetAvailableDatesResponse>>(
        `${API_BASE}/dates`,
        { params }
      )

      console.log('[AvailabilityService] Available dates retrieved:', response.data)

      // Handle wrapped response format
      const data = response.data?.data || response.data
      return data as GetAvailableDatesResponse
    } catch (error) {
      console.error('[AvailabilityService] Error getting available dates:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Utility Methods
  // ============================================

  /**
   * Get next available slot for a service
   */
  async getNextAvailableSlot(
    providerId: string,
    serviceId: string,
    staffMemberId?: string
  ): Promise<TimeSlot | null> {
    try {
      // Get today's date
      const today = new Date()
      const dateStr = today.toISOString().split('T')[0]

      // Get slots for today
      const response = await this.getAvailableSlots({
        providerId,
        serviceId,
        date: dateStr,
        staffMemberId,
      })

      // Find first available slot
      const availableSlot = response.slots.find(slot => slot.available)
      if (availableSlot) {
        return availableSlot
      }

      // If no slots today, try tomorrow
      const tomorrow = new Date(today)
      tomorrow.setDate(tomorrow.getDate() + 1)
      const tomorrowStr = tomorrow.toISOString().split('T')[0]

      const tomorrowResponse = await this.getAvailableSlots({
        providerId,
        serviceId,
        date: tomorrowStr,
        staffMemberId,
      })

      return tomorrowResponse.slots.find(slot => slot.available) || null
    } catch (error) {
      console.error('[AvailabilityService] Error getting next available slot:', error)
      return null
    }
  }

  /**
   * Get available slots for next N days
   */
  async getAvailableSlotsForDays(
    providerId: string,
    serviceId: string,
    days: number = 7,
    staffMemberId?: string
  ): Promise<Map<string, TimeSlot[]>> {
    const slots = new Map<string, TimeSlot[]>()

    try {
      const today = new Date()

      for (let i = 0; i < days; i++) {
        const date = new Date(today)
        date.setDate(date.getDate() + i)
        const dateStr = date.toISOString().split('T')[0]

        const response = await this.getAvailableSlots({
          providerId,
          serviceId,
          date: dateStr,
          staffMemberId,
        })

        slots.set(dateStr, response.slots.filter(slot => slot.available))
      }

      return slots
    } catch (error) {
      console.error('[AvailabilityService] Error getting slots for days:', error)
      return slots
    }
  }

  /**
   * Format time slot for display (Persian)
   */
  formatTimeSlot(slot: TimeSlot): string {
    const start = new Date(slot.startTime)
    const end = new Date(slot.endTime)

    const formatTime = (date: Date) => {
      const hours = String(date.getHours()).padStart(2, '0')
      const minutes = String(date.getMinutes()).padStart(2, '0')
      return `${hours}:${minutes}`
    }

    return `${formatTime(start)} - ${formatTime(end)}`
  }

  /**
   * Convert time to Persian numbers
   */
  toPersianTime(timeStr: string): string {
    const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
    return timeStr.replace(/\d/g, (digit) => persianDigits[parseInt(digit)])
  }

  /**
   * Group slots by time of day
   */
  groupSlotsByTimeOfDay(slots: TimeSlot[]): {
    morning: TimeSlot[]
    afternoon: TimeSlot[]
    evening: TimeSlot[]
  } {
    return slots.reduce(
      (acc, slot) => {
        const hour = new Date(slot.startTime).getHours()

        if (hour < 12) {
          acc.morning.push(slot)
        } else if (hour < 17) {
          acc.afternoon.push(slot)
        } else {
          acc.evening.push(slot)
        }

        return acc
      },
      { morning: [] as TimeSlot[], afternoon: [] as TimeSlot[], evening: [] as TimeSlot[] }
    )
  }

  // ============================================
  // Error Handling
  // ============================================

  /**
   * Centralized error handling
   */
  private handleError(error: unknown): Error {
    if (error instanceof Error) {
      return error
    }
    return new Error('خطا در بررسی زمان‌های خالی')
  }
}

// Export singleton instance
export const availabilityService = new AvailabilityService()
export default availabilityService
