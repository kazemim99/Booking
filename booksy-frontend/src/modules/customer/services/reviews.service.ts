/**
 * Reviews Service
 * Handles review-related operations for customers
 */

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/Reviews`

// ==================== Request/Response Types ====================

/**
 * Create review request
 */
export interface CreateReviewRequest {
  bookingId: string
  rating: number
  comment?: string
}

/**
 * Update review request
 */
export interface UpdateReviewRequest {
  rating: number
  text?: string
}

/**
 * Review response
 */
export interface ReviewResponse {
  id: string
  bookingId: string
  customerId: string
  providerId: string
  rating: number
  comment?: string
  createdAt: string
  updatedAt?: string
  isVerified: boolean
  canEdit: boolean
  editDeadline?: string
}

// ==================== Reviews Service Class ====================

class ReviewsService {
  /**
   * Create a review for a completed booking
   * POST /api/v1/reviews/bookings/{bookingId}
   */
  async createReview(bookingId: string, data: { rating: number; comment?: string }): Promise<ReviewResponse> {
    try {
      console.log(`[ReviewsService] Creating review for booking: ${bookingId}`, data)

      const response = await serviceCategoryClient.post<ApiResponse<ReviewResponse>>(
        `${API_BASE}/bookings/${bookingId}`,
        data
      )

      console.log('[ReviewsService] Review created:', response.data)

      const review = response.data?.data || response.data
      return review as ReviewResponse
    } catch (error) {
      console.error(`[ReviewsService] Error creating review:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Update an existing review (within 7-day edit window)
   * PUT /api/v1/reviews/{reviewId}
   *
   * Note: This endpoint may not exist in the backend yet.
   * If it doesn't exist, this will fail with 404.
   */
  async updateReview(reviewId: string, data: UpdateReviewRequest): Promise<ReviewResponse> {
    try {
      console.log(`[ReviewsService] Updating review: ${reviewId}`, data)

      const response = await serviceCategoryClient.put<ApiResponse<ReviewResponse>>(
        `${API_BASE}/${reviewId}`,
        data
      )

      console.log('[ReviewsService] Review updated:', response.data)

      const review = response.data?.data || response.data
      return review as ReviewResponse
    } catch (error) {
      console.error(`[ReviewsService] Error updating review:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Get reviews for a specific provider
   * GET /api/v1/reviews/providers/{providerId}
   */
  async getProviderReviews(
    providerId: string,
    pageNumber = 1,
    pageSize = 20,
    verifiedOnly = false
  ): Promise<{
    reviews: ReviewResponse[]
    statistics: {
      totalReviews: number
      averageRating: number
      ratingDistribution: Record<number, number>
    }
    pagination: {
      pageNumber: number
      pageSize: number
      totalPages: number
      totalItems: number
    }
  }> {
    try {
      console.log(`[ReviewsService] Fetching reviews for provider: ${providerId}`)

      const params = {
        pageNumber,
        pageSize,
        verifiedOnly,
        sortBy: 'date',
        sortDescending: true
      }

      const response = await serviceCategoryClient.get<ApiResponse<any>>(
        `${API_BASE}/providers/${providerId}`,
        { params }
      )

      console.log('[ReviewsService] Reviews fetched:', response.data)

      const data = response.data?.data || response.data
      return data
    } catch (error) {
      console.error(`[ReviewsService] Error fetching provider reviews:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Mark a review as helpful
   * PUT /api/v1/reviews/{reviewId}/helpful
   */
  async markReviewHelpful(reviewId: string): Promise<void> {
    try {
      console.log(`[ReviewsService] Marking review as helpful: ${reviewId}`)

      await serviceCategoryClient.put(`${API_BASE}/${reviewId}/helpful`)

      console.log('[ReviewsService] Review marked as helpful')
    } catch (error) {
      console.error(`[ReviewsService] Error marking review as helpful:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Check if a review can be edited (within 7-day window)
   */
  canEditReview(createdAt: string): boolean {
    const reviewDate = new Date(createdAt)
    const now = new Date()
    const daysSinceReview = Math.floor((now.getTime() - reviewDate.getTime()) / (1000 * 60 * 60 * 24))

    return daysSinceReview <= 7
  }

  /**
   * Calculate edit deadline (7 days from creation)
   */
  getEditDeadline(createdAt: string): Date {
    const reviewDate = new Date(createdAt)
    const deadline = new Date(reviewDate)
    deadline.setDate(deadline.getDate() + 7)

    return deadline
  }

  /**
   * Centralized error handling
   */
  private handleError(error: unknown): Error {
    if (error instanceof Error) {
      return error
    }
    return new Error('An unexpected error occurred')
  }
}

// Export singleton instance
export const reviewsService = new ReviewsService()
export default reviewsService
