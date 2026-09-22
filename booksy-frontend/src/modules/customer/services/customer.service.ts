/**
 * Customer Service
 * API client for customer profile, bookings, reviews, and preferences
 */

import { reviewsApi } from '@/modules/reviews/api/reviews.api'
import type { MyReview } from '@/modules/reviews/types/reviews.types'
import { userManagementClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  CustomerProfile,
  UpdateCustomerProfileRequest,
  UpcomingBooking,
  BookingHistoryEntry,
  BookingHistoryResult,
  CustomerReview,
  UpdateReviewRequest,
  NotificationPreferences,
  UpdatePreferencesRequest,
} from '../types/customer.types'

const API_VERSION = 'v1'
const CUSTOMERS_BASE = `/${API_VERSION}/Customers`

// ============================================================================
// CUSTOMER SERVICE CLASS
// ============================================================================

class CustomerService {
  // ========================================
  // Profile Operations
  // ========================================

  /**
   * Get customer profile
   * GET /api/v1/customers/{customerId}/profile
   */
  async getProfile(customerId: string): Promise<CustomerProfile> {
    try {
      console.log('[CustomerService] Fetching profile:', customerId)

      const response = await userManagementClient.get<ApiResponse<CustomerProfile>>(
        `${CUSTOMERS_BASE}/${customerId}/profile`
      )

      const profile = response.data?.data || response.data

      if (!profile) {
        throw new Error('Profile not found')
      }

      console.log('[CustomerService] Profile retrieved:', profile)
      return profile as CustomerProfile
    } catch (error) {
      console.error('[CustomerService] Error fetching profile:', error)
      throw this.handleError(error, 'خطا در دریافت اطلاعات کاربر')
    }
  }

  /**
   * Update customer profile
   * PUT /api/v1/customers/{customerId}
   */
  async updateProfile(
    customerId: string,
    request: UpdateCustomerProfileRequest
  ): Promise<CustomerProfile> {
    try {
      console.log('[CustomerService] Updating profile:', customerId, request)

      const response = await userManagementClient.put<ApiResponse<CustomerProfile>>(
        `${CUSTOMERS_BASE}/${customerId}`,
        request
      )

      const profile = response.data?.data || response.data

      if (!profile) {
        throw new Error('Failed to update profile')
      }

      console.log('[CustomerService] Profile updated:', profile)
      return profile as CustomerProfile
    } catch (error) {
      console.error('[CustomerService] Error updating profile:', error)
      throw this.handleError(error, 'خطا در بهروزرسانی پروفایل')
    }
  }

  // ========================================
  // Bookings Operations
  // ========================================

  /**
   * Get upcoming bookings
   * GET /api/v1/customers/{customerId}/bookings/upcoming?limit=5
   */
  async getUpcomingBookings(customerId: string, limit: number = 5): Promise<UpcomingBooking[]> {
    try {
      console.log('[CustomerService] Fetching upcoming bookings:', customerId, limit)

      const response = await userManagementClient.get<ApiResponse<UpcomingBooking[]>>(
        `${CUSTOMERS_BASE}/${customerId}/bookings/upcoming`,
        {
          params: { limit },
        }
      )

      const bookings = this.extractArray<UpcomingBooking>(response.data)

      console.log('[CustomerService] Upcoming bookings retrieved:', bookings.length)
      return bookings
    } catch (error) {
      console.error('[CustomerService] Error fetching upcoming bookings:', error)
      throw this.handleError(error, 'خطا در دریافت نوبت‌های آینده')
    }
  }

  /**
   * Get booking history
   * GET /api/v1/customers/{customerId}/bookings/history?page=1&size=20
   */
  async getBookingHistory(
    customerId: string,
    page: number = 1,
    size: number = 20
  ): Promise<BookingHistoryResult> {
    try {
      console.log('[CustomerService] Fetching booking history:', customerId, page, size)

      const response = await userManagementClient.get<ApiResponse<BookingHistoryResult>>(
        `${CUSTOMERS_BASE}/${customerId}/bookings/history`,
        {
          params: { page, size },
        }
      )

      const result = response.data?.data || response.data

      // Handle both paginated and non-paginated responses
      if (result && 'items' in result) {
        console.log('[CustomerService] Booking history retrieved:', result.items.length)
        return result as BookingHistoryResult
      }

      // If array was returned directly, wrap it
      const items = this.extractArray<BookingHistoryEntry>(response.data)
      const paginatedResult: BookingHistoryResult = {
        items,
        page,
        pageSize: size,
        totalPages: 1,
        totalCount: items.length,
      }

      console.log('[CustomerService] Booking history retrieved:', items.length)
      return paginatedResult
    } catch (error) {
      console.error('[CustomerService] Error fetching booking history:', error)
      throw this.handleError(error, 'خطا در دریافت تاریخچه نوبت‌ها')
    }
  }

  // ========================================
  // Reviews Operations
  // ========================================

  /**
   * The customer's own reviews, every moderation state.
   * GET /api/v1/reviews/me (ServiceCatalog). This used to call /api/v1/customers/{id}/reviews on UserManagement —
   * a route that never existed, so "My reviews" could only fail. The caller is identified by their token, so the
   * customer id is no longer needed; the parameter stays so the store's call sites do not change.
   */
  async getReviews(_customerId: string): Promise<CustomerReview[]> {
    try {
      return (await reviewsApi.mine()).map(toCustomerReview)
    } catch (error) {
      console.error('[CustomerService] Error fetching reviews:', error)
      throw this.handleError(error, 'خطا در دریافت نظرات')
    }
  }

  /**
   * Edit one of the customer's reviews — PUT /api/v1/reviews/{id}. The edited review returns to moderation, so the
   * review is re-read and handed back as it now stands (awaiting approval), not as the form last showed it.
   */
  async updateReview(
    _customerId: string,
    reviewId: string,
    request: UpdateReviewRequest
  ): Promise<CustomerReview> {
    try {
      await reviewsApi.edit(reviewId, {
        rating: request.rating,
        comment: request.text,
        dimensions: request.dimensions ?? {},
      })
      const updated = (await reviewsApi.mine()).find((r) => r.reviewId === reviewId)
      if (!updated) throw new Error('Failed to update review')
      return toCustomerReview(updated)
    } catch (error) {
      console.error('[CustomerService] Error updating review:', error)
      throw this.handleError(error, 'خطا در بهروزرسانی نظر')
    }
  }

  // ========================================
  // Preferences Operations
  // ========================================

  /**
   * Get notification preferences
   * GET /api/v1/customers/{customerId}/preferences
   */
  async getPreferences(customerId: string): Promise<NotificationPreferences> {
    try {
      console.log('[CustomerService] Fetching preferences:', customerId)

      const response = await userManagementClient.get<ApiResponse<NotificationPreferences>>(
        `${CUSTOMERS_BASE}/${customerId}/preferences`
      )

      const preferences = response.data?.data || response.data

      if (!preferences) {
        throw new Error('Preferences not found')
      }

      console.log('[CustomerService] Preferences retrieved:', preferences)
      return preferences as NotificationPreferences
    } catch (error) {
      console.error('[CustomerService] Error fetching preferences:', error)
      throw this.handleError(error, 'خطا در دریافت تنظیمات')
    }
  }

  /**
   * Update notification preferences
   * PATCH /api/v1/customers/{customerId}/preferences
   */
  async updatePreferences(
    customerId: string,
    request: UpdatePreferencesRequest
  ): Promise<NotificationPreferences> {
    try {
      console.log('[CustomerService] Updating preferences:', customerId, request)

      const response = await userManagementClient.patch<ApiResponse<NotificationPreferences>>(
        `${CUSTOMERS_BASE}/${customerId}/preferences`,
        request
      )

      const preferences = response.data?.data || response.data

      if (!preferences) {
        throw new Error('Failed to update preferences')
      }

      console.log('[CustomerService] Preferences updated:', preferences)
      return preferences as NotificationPreferences
    } catch (error) {
      console.error('[CustomerService] Error updating preferences:', error)
      throw this.handleError(error, 'خطا در بهروزرسانی تنظیمات')
    }
  }

  // ========================================
  // Helper Methods
  // ========================================

  /**
   * Extract array from response data (handles various response formats)
   */
  private extractArray<T>(data: any): T[] {
    if (Array.isArray(data)) {
      return data
    }

    if (data?.data) {
      if (Array.isArray(data.data)) {
        return data.data
      }
      if (data.data.items && Array.isArray(data.data.items)) {
        return data.data.items
      }
    }

    return []
  }

  /**
   * Centralized error handling
   */
  private handleError(error: unknown, defaultMessage: string): Error {
    if (error instanceof Error) {
      return error
    }

    // Handle API errors
    if (typeof error === 'object' && error !== null) {
      const apiError = error as any
      if (apiError.response?.data?.message) {
        return new Error(apiError.response.data.message)
      }
      if (apiError.message) {
        return new Error(apiError.message)
      }
    }

    return new Error(defaultMessage)
  }
}

// Export singleton instance
/** The reviews API's shape, in the shape the customer profile already renders. */
function toCustomerReview(r: MyReview): CustomerReview {
  return {
    id: r.reviewId,
    providerId: r.providerId,
    providerName: r.providerName ?? '',
    providerLogoUrl: r.providerLogoUrl ?? undefined,
    serviceId: '',
    serviceName: r.serviceName ?? '',
    rating: r.rating,
    text: r.comment ?? undefined,
    createdAt: r.createdAt,
    updatedAt: r.editedAt ?? undefined,
    canEdit: r.canEdit,
    moderationStatus: r.moderationStatus,
    moderationReason: r.moderationReason,
    dimensions: r.dimensions,
  }
}

export const customerService = new CustomerService()
export default customerService
