/**
 * Booking Service
 * Handles all booking-related API operations
 * Based on Booksy API Collection v1
 */

import { httpClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type { BookingStatus } from '@/core/types/enums.types'
import type {
  Appointment,
  BookingRequest,
  CancellationRequest,
  RescheduleRequest,
} from '../types/booking.types'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/bookings`

// ==================== Request/Response Types ====================

/**
 * Booking list filters (matches Postman collection)
 */
export interface BookingListFilters {
  status?: BookingStatus
  customerId?: string
  providerId?: string
  pageNumber?: number
  pageSize?: number
  startDate?: string
  endDate?: string
}

/**
 * Create booking request (matches Postman collection payload)
 */
export interface CreateBookingRequest {
  customerId: string
  providerId: string
  serviceId: string
  startTime: string // ISO 8601 format
  endTime: string
  notes?: string
  depositAmount?: number
  totalAmount: number
}

/**
 * Confirm booking request
 */
export interface ConfirmBookingRequest {
  notes?: string
  confirmedAt?: string
}

/**
 * Complete booking request
 */
export interface CompleteBookingRequest {
  completedAt?: string
  notes?: string
  actualDuration?: number
}

/**
 * Cancel booking request (matches Postman collection)
 */
export interface CancelBookingRequest {
  reason: string
  notes?: string
}

/**
 * Assign staff request
 */
export interface AssignStaffRequest {
  staffMemberId: string
  reason?: string
}

/**
 * Add notes request
 */
export interface AddNotesRequest {
  notes: string
  isInternal?: boolean
}

/**
 * Mark as no-show request
 */
export interface MarkNoShowRequest {
  reason?: string
  chargeNoShowFee?: boolean
}

/**
 * Paginated booking response
 */
export interface PaginatedBookingsResponse {
  items: Appointment[]
  totalItems: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// ==================== Booking Service Class ====================

class BookingService {
  // ============================================
  // Booking List & Retrieval
  // ============================================

  /**
   * Get list of bookings with filters
   * GET /api/v1/bookings
   *
   * Query parameters:
   * - status: Filter by booking status
   * - customerId: Filter by customer
   * - providerId: Filter by provider
   * - pageNumber: Page number (default: 1)
   * - pageSize: Items per page (default: 10)
   */
  async getBookings(filters: BookingListFilters = {}): Promise<PaginatedBookingsResponse> {
    try {
      console.log('[BookingService] Fetching bookings with filters:', filters)

      const params = {
        pageNumber: filters.pageNumber || 1,
        pageSize: filters.pageSize || 10,
        ...(filters.status && { status: filters.status }),
        ...(filters.customerId && { customerId: filters.customerId }),
        ...(filters.providerId && { providerId: filters.providerId }),
        ...(filters.startDate && { startDate: filters.startDate }),
        ...(filters.endDate && { endDate: filters.endDate }),
      }

      const response = await httpClient.get<ApiResponse<PaginatedBookingsResponse>>(
        API_BASE,
        { params }
      )

      console.log('[BookingService] Bookings retrieved:', response.data)

      // Handle wrapped response format
      const data = response.data?.data || response.data
      return data as PaginatedBookingsResponse
    } catch (error) {
      console.error('[BookingService] Error fetching bookings:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Get bookings for current customer
   * Convenience method that filters by authenticated customer
   */
  async getMyBookings(
    status?: BookingStatus,
    pageNumber = 1,
    pageSize = 10
  ): Promise<PaginatedBookingsResponse> {
    // The API will automatically filter by authenticated user
    return this.getBookings({ status, pageNumber, pageSize })
  }

  /**
   * Get bookings for a specific provider
   * Used by provider dashboard to view their bookings
   */
  async getProviderBookings(
    providerId: string,
    status?: BookingStatus,
    pageNumber = 1,
    pageSize = 10
  ): Promise<PaginatedBookingsResponse> {
    return this.getBookings({ providerId, status, pageNumber, pageSize })
  }

  /**
   * Get single booking by ID
   * GET /api/v1/bookings/{id}
   */
  async getBookingById(id: string): Promise<Appointment> {
    try {
      console.log(`[BookingService] Fetching booking: ${id}`)

      const response = await httpClient.get<ApiResponse<Appointment>>(
        `${API_BASE}/${id}`
      )

      console.log('[BookingService] Booking retrieved:', response.data)

      // Handle wrapped response format
      const data = response.data?.data || response.data
      return data as Appointment
    } catch (error) {
      console.error(`[BookingService] Error fetching booking ${id}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Booking Creation
  // ============================================

  /**
   * Create a new booking
   * POST /api/v1/bookings
   *
   * Request body (from Postman collection):
   * {
   *   "customerId": "user-123",
   *   "providerId": "provider-456",
   *   "serviceId": "service-789",
   *   "startTime": "2025-01-15T10:00:00Z",
   *   "endTime": "2025-01-15T11:00:00Z",
   *   "notes": "لطفا در زمان مقرر حاضر باشید",
   *   "depositAmount": 500000,
   *   "totalAmount": 2000000
   * }
   */
  async createBooking(data: CreateBookingRequest): Promise<Appointment> {
    try {
      console.log('[BookingService] Creating booking:', data)

      const response = await httpClient.post<ApiResponse<Appointment>>(
        API_BASE,
        data
      )

      console.log('[BookingService] Booking created:', response.data)

      // Handle wrapped response format
      const booking = response.data?.data || response.data
      return booking as Appointment
    } catch (error) {
      console.error('[BookingService] Error creating booking:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Create booking from BookingRequest (alternative interface)
   * Maps the BookingRequest type to CreateBookingRequest
   */
  async createBookingFromRequest(request: BookingRequest): Promise<Appointment> {
    const createRequest: CreateBookingRequest = {
      customerId: '', // Will be set from auth context
      providerId: request.providerId,
      serviceId: request.serviceId,
      startTime: request.scheduledStartTime,
      endTime: '', // Will be calculated based on service duration
      notes: request.bookingNotes,
      totalAmount: 0, // Will be calculated
    }

    return this.createBooking(createRequest)
  }

  // ============================================
  // Booking Status Management
  // ============================================

  /**
   * Confirm a booking
   * POST /api/v1/bookings/{id}/confirm
   *
   * Request body (from Postman collection):
   * {
   *   "notes": "رزرو تایید شد"
   * }
   */
  async confirmBooking(id: string, data?: ConfirmBookingRequest): Promise<Appointment> {
    try {
      console.log(`[BookingService] Confirming booking: ${id}`, data)

      const response = await httpClient.post<ApiResponse<Appointment>>(
        `${API_BASE}/${id}/confirm`,
        data || {}
      )

      console.log('[BookingService] Booking confirmed:', response.data)

      // Handle wrapped response format
      const booking = response.data?.data || response.data
      return booking as Appointment
    } catch (error) {
      console.error(`[BookingService] Error confirming booking ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Complete a booking (mark as done)
   * POST /api/v1/bookings/{id}/complete
   *
   * Request body:
   * {
   *   "completedAt": "2025-01-15T11:00:00Z",
   *   "notes": "خدمات با موفقیت انجام شد"
   * }
   */
  async completeBooking(id: string, data?: CompleteBookingRequest): Promise<Appointment> {
    try {
      console.log(`[BookingService] Completing booking: ${id}`, data)

      const response = await httpClient.post<ApiResponse<Appointment>>(
        `${API_BASE}/${id}/complete`,
        data || {}
      )

      console.log('[BookingService] Booking completed:', response.data)

      // Handle wrapped response format
      const booking = response.data?.data || response.data
      return booking as Appointment
    } catch (error) {
      console.error(`[BookingService] Error completing booking ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Cancel a booking
   * POST /api/v1/bookings/{id}/cancel
   *
   * Request body (from Postman collection):
   * {
   *   "reason": "درخواست مشتری",
   *   "notes": "مشتری نیاز به تغییر زمان دارد"
   * }
   */
  async cancelBooking(id: string, data: CancelBookingRequest): Promise<Appointment> {
    try {
      console.log(`[BookingService] Cancelling booking: ${id}`, data)

      const response = await httpClient.post<ApiResponse<Appointment>>(
        `${API_BASE}/${id}/cancel`,
        data
      )

      console.log('[BookingService] Booking cancelled:', response.data)

      // Handle wrapped response format
      const booking = response.data?.data || response.data
      return booking as Appointment
    } catch (error) {
      console.error(`[BookingService] Error cancelling booking ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Cancel booking from CancellationRequest (alternative interface)
   */
  async cancelBookingFromRequest(request: CancellationRequest): Promise<Appointment> {
    return this.cancelBooking(request.appointmentId, {
      reason: request.reason,
      notes: request.notes,
    })
  }

  // ============================================
  // Booking Modifications
  // ============================================

  /**
   * Reschedule a booking
   * POST /api/v1/bookings/{id}/reschedule
   *
   * Request body:
   * {
   *   "newStartTime": "2025-01-16T14:00:00Z",
   *   "reason": "درخواست مشتری"
   * }
   */
  async rescheduleBooking(request: RescheduleRequest): Promise<Appointment> {
    try {
      console.log('[BookingService] Rescheduling booking:', request)

      const response = await httpClient.post<ApiResponse<Appointment>>(
        `${API_BASE}/${request.appointmentId}/reschedule`,
        {
          newStartTime: request.newStartTime,
          reason: request.reason,
        }
      )

      console.log('[BookingService] Booking rescheduled:', response.data)

      const booking = response.data?.data || response.data
      return booking as Appointment
    } catch (error) {
      console.error('[BookingService] Error rescheduling booking:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Assign or reassign staff to a booking
   * PUT /api/v1/bookings/{id}/assign-staff/{staffId}
   *
   * Request body:
   * {
   *   "reason": "کارمند قبلی در دسترس نیست"
   * }
   */
  async assignStaff(
    bookingId: string,
    staffMemberId: string,
    reason?: string
  ): Promise<Appointment> {
    try {
      console.log(`[BookingService] Assigning staff ${staffMemberId} to booking ${bookingId}`)

      const response = await httpClient.put<ApiResponse<Appointment>>(
        `${API_BASE}/${bookingId}/assign-staff/${staffMemberId}`,
        { reason }
      )

      console.log('[BookingService] Staff assigned:', response.data)

      const booking = response.data?.data || response.data
      return booking as Appointment
    } catch (error) {
      console.error(`[BookingService] Error assigning staff to booking ${bookingId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Add notes to a booking
   * POST /api/v1/bookings/{id}/notes
   *
   * Request body:
   * {
   *   "notes": "مشتری ۱۵ دقیقه دیرتر رسید",
   *   "isInternal": true
   * }
   */
  async addNotes(
    bookingId: string,
    notes: string,
    isInternal: boolean = true
  ): Promise<Appointment> {
    try {
      console.log(`[BookingService] Adding notes to booking ${bookingId}`)

      const response = await httpClient.post<ApiResponse<Appointment>>(
        `${API_BASE}/${bookingId}/notes`,
        { notes, isInternal }
      )

      console.log('[BookingService] Notes added:', response.data)

      const booking = response.data?.data || response.data
      return booking as Appointment
    } catch (error) {
      console.error(`[BookingService] Error adding notes to booking ${bookingId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Mark a booking as no-show
   * POST /api/v1/bookings/{id}/no-show
   *
   * Request body:
   * {
   *   "reason": "مشتری حاضر نشد",
   *   "chargeNoShowFee": true
   * }
   */
  async markNoShow(
    bookingId: string,
    reason?: string,
    chargeNoShowFee: boolean = false
  ): Promise<Appointment> {
    try {
      console.log(`[BookingService] Marking booking ${bookingId} as no-show`)

      const response = await httpClient.post<ApiResponse<Appointment>>(
        `${API_BASE}/${bookingId}/no-show`,
        { reason, chargeNoShowFee }
      )

      console.log('[BookingService] Booking marked as no-show:', response.data)

      const booking = response.data?.data || response.data
      return booking as Appointment
    } catch (error) {
      console.error(`[BookingService] Error marking booking ${bookingId} as no-show:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Utility Methods
  // ============================================

  /**
   * Get upcoming bookings for customer
   */
  async getUpcomingBookings(pageSize = 10): Promise<Appointment[]> {
    const response = await this.getMyBookings(undefined, 1, pageSize)

    // Filter for upcoming bookings (status: Confirmed or Pending)
    return response.items.filter(booking =>
      ['Confirmed', 'Pending'].includes(booking.status) &&
      new Date(booking.scheduledStartTime) > new Date()
    )
  }

  /**
   * Get past bookings for customer
   */
  async getPastBookings(pageSize = 10): Promise<Appointment[]> {
    const response = await this.getMyBookings(undefined, 1, pageSize)

    // Filter for past bookings
    return response.items.filter(booking =>
      new Date(booking.scheduledStartTime) < new Date()
    )
  }

  /**
   * Get booking statistics for provider
   */
  async getProviderBookingStats(providerId: string): Promise<{
    total: number
    pending: number
    confirmed: number
    completed: number
    cancelled: number
  }> {
    try {
      // Get all bookings for provider
      const response = await this.getProviderBookings(providerId, undefined, 1, 1000)

      const stats = {
        total: response.totalItems,
        pending: 0,
        confirmed: 0,
        completed: 0,
        cancelled: 0,
      }

      response.items.forEach(booking => {
        switch (booking.status) {
          case 'Pending':
            stats.pending++
            break
          case 'Confirmed':
            stats.confirmed++
            break
          case 'Completed':
            stats.completed++
            break
          case 'Cancelled':
            stats.cancelled++
            break
        }
      })

      return stats
    } catch (error) {
      console.error('[BookingService] Error fetching provider booking stats:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Error Handling
  // ============================================

  /**
   * Centralized error handling with support for validation errors
   */
  private handleError(error: unknown): Error {
    // If already an Error with validation info, return as-is
    if (error instanceof Error) {
      // Check if this is already a validation error from HTTP client
      if ((error as any).isValidationError && (error as any).validationErrors) {
        return error
      }
      return error
    }

    return new Error('An unexpected error occurred')
  }
}

// Export singleton instance
export const bookingService = new BookingService()
export default bookingService
