/**
 * Platform Service
 * Handles platform-wide statistics and information
 */

import { httpClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/Platform`

// ==================== Types ====================

/**
 * Platform-wide statistics
 */
export interface PlatformStatistics {
  totalProviders: number
  totalCustomers: number
  totalBookings: number
  averageRating: number
  totalServices: number
  citiesWithProviders: number
  popularCategories: Record<string, number>
  lastUpdated: string
}

// ==================== Platform Service Class ====================

class PlatformService {
  // Cache for statistics (cache for 5 minutes)
  private statisticsCache: PlatformStatistics | null = null
  private cacheTimestamp: number = 0
  private readonly CACHE_DURATION = 5 * 60 * 1000 // 5 minutes

  // ============================================
  // Statistics
  // ============================================

  /**
   * Get platform-wide statistics
   * GET /api/v1/platform/statistics
   *
   * Example response:
   * {
   *   "totalProviders": 125,
   *   "totalCustomers": 6250,
   *   "totalBookings": 15625,
   *   "averageRating": 4.7,
   *   "totalServices": 450,
   *   "citiesWithProviders": 12,
   *   "popularCategories": {
   *     "آرایشگاه مو": 45,
   *     "ماساژ": 32
   *   },
   *   "lastUpdated": "2025-12-02T10:30:00Z"
   * }
   */
  async getStatistics(useCache = true): Promise<PlatformStatistics> {
    try {
      // Check if cache is still valid
      const now = Date.now()
      const cacheIsValid = this.statisticsCache &&
                          useCache &&
                          (now - this.cacheTimestamp) < this.CACHE_DURATION

      if (cacheIsValid) {
        console.log('[PlatformService] Returning cached statistics')
        return this.statisticsCache!
      }

      console.log('[PlatformService] Fetching statistics from API')

      const response = await httpClient.get<ApiResponse<PlatformStatistics>>(
        `${API_BASE}/statistics`
      )

      console.log('[PlatformService] Statistics retrieved:', response.data)

      // Handle wrapped response format
      const statistics = response.data?.data || response.data
      const stats = statistics as PlatformStatistics

      // Cache the results
      this.statisticsCache = stats
      this.cacheTimestamp = now

      return stats
    } catch (error) {
      console.error('[PlatformService] Error fetching statistics:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Utility Methods
  // ============================================

  /**
   * Clear cache
   */
  clearCache(): void {
    this.statisticsCache = null
    this.cacheTimestamp = 0
    console.log('[PlatformService] Cache cleared')
  }

  /**
   * Format large numbers for display (e.g., 10000 -> "10,000+")
   */
  formatStatNumber(num: number, includeSymbol = true): string {
    if (num >= 1000000) {
      const formatted = (num / 1000000).toFixed(1)
      return includeSymbol ? `${formatted}M+` : formatted
    } else if (num >= 1000) {
      const formatted = Math.floor(num / 1000)
      return includeSymbol ? `${formatted},000+` : `${formatted},000`
    }
    return includeSymbol ? `${num}+` : `${num}`
  }

  /**
   * Convert English numbers to Persian numbers
   */
  convertToPersianNumber(num: string | number): string {
    const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
    return num.toString()
      .split('')
      .map(digit => {
        if (digit >= '0' && digit <= '9') {
          return persianDigits[parseInt(digit)]
        }
        return digit
      })
      .join('')
  }

  /**
   * Format statistics for Persian display
   */
  formatForDisplay(stats: PlatformStatistics) {
    return {
      providers: this.convertToPersianNumber(this.formatStatNumber(stats.totalProviders)),
      customers: this.convertToPersianNumber(this.formatStatNumber(stats.totalCustomers)),
      rating: this.convertToPersianNumber(stats.averageRating.toFixed(1)),
      bookings: this.convertToPersianNumber(this.formatStatNumber(stats.totalBookings)),
      services: this.convertToPersianNumber(this.formatStatNumber(stats.totalServices)),
      cities: this.convertToPersianNumber(stats.citiesWithProviders),
    }
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
    return new Error('خطا در بارگذاری آمار پلتفرم')
  }
}

// Export singleton instance
export const platformService = new PlatformService()
export default platformService
