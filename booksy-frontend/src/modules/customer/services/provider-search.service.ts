// Customer provider search service

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  ProviderSearchFilters,
  ProviderSearchResult,
  PagedSearchResult,
} from '../types/search.types'
import type { Provider } from '@/modules/provider/types/provider.types'

class ProviderSearchService {
  private readonly baseUrl = '/api/v1.0/providers'

  /**
   * Search providers with filters
   */
  async searchProviders(
    filters: ProviderSearchFilters = {},
  ): Promise<PagedSearchResult<ProviderSearchResult>> {
    try {
      const params = this.buildSearchParams(filters)

      const response = await serviceCategoryClient.get<PagedSearchResult<ProviderSearchResult>>(
        `${this.baseUrl}/search`,
        { params },
      )

      if (!response.success || !response.data) {
        throw new Error(response.message || 'خطا در جستجوی ارائه‌دهندگان')
      }

      return response.data
    } catch (error) {
      console.error('[ProviderSearchService] Search error:', error)
      throw error
    }
  }

  /**
   * Search providers by location
   */
  async searchByLocation(
    latitude: number,
    longitude: number,
    radiusKm: number = 5,
    filters?: Omit<ProviderSearchFilters, 'latitude' | 'longitude' | 'radiusKm'>,
  ): Promise<ProviderSearchResult[]> {
    try {
      const params = {
        latitude,
        longitude,
        radiusKm,
        ...this.buildSearchParams(filters || {}),
      }

      const response = await serviceCategoryClient.get<ProviderSearchResult[]>(
        `${this.baseUrl}/by-location`,
        { params },
      )

      if (!response.success || !response.data) {
        throw new Error(response.message || 'خطا در جستجوی ارائه‌دهندگان')
      }

      return response.data
    } catch (error) {
      console.error('[ProviderSearchService] Location search error:', error)
      throw error
    }
  }

  /**
   * Get provider details by ID
   */
  async getProviderById(id: string): Promise<Provider> {
    try {
      const response = await serviceCategoryClient.get<Provider>(`${this.baseUrl}/${id}`)

      if (!response.success || !response.data) {
        throw new Error(response.message || 'خطا در بارگذاری اطلاعات ارائه‌دهنده')
      }

      return response.data
    } catch (error) {
      console.error('[ProviderSearchService] Get provider error:', error)
      throw error
    }
  }

  /**
   * Get featured/popular providers
   */
  async getFeaturedProviders(limit: number = 10): Promise<ProviderSearchResult[]> {
    try {
      const response = await serviceCategoryClient.get<ProviderSearchResult[]>(
        `${this.baseUrl}/featured`,
        { params: { limit } },
      )

      if (!response.success || !response.data) {
        return []
      }

      return response.data
    } catch (error) {
      console.error('[ProviderSearchService] Get featured providers error:', error)
      return []
    }
  }

  /**
   * Get provider gallery images
   */
  async getProviderGallery(providerId: string): Promise<any[]> {
    try {
      const response = await serviceCategoryClient.get<any[]>(
        `${this.baseUrl}/${providerId}/gallery`,
      )

      if (!response.success || !response.data) {
        return []
      }

      return response.data
    } catch (error) {
      console.error('[ProviderSearchService] Get gallery error:', error)
      return []
    }
  }

  /**
   * Build search params from filters
   */
  private buildSearchParams(filters: ProviderSearchFilters): Record<string, any> {
    const params: Record<string, any> = {}

    // Text search
    if (filters.searchTerm) {
      params.searchTerm = filters.searchTerm
    }
    if (filters.query) {
      params.query = filters.query
    }

    // Category/Type
    if (filters.category) {
      params.category = Array.isArray(filters.category)
        ? filters.category.join(',')
        : filters.category
    }
    if (filters.providerTypes && filters.providerTypes.length > 0) {
      params.providerTypes = filters.providerTypes.join(',')
    }

    // Location
    if (filters.latitude !== undefined) {
      params.latitude = filters.latitude
    }
    if (filters.longitude !== undefined) {
      params.longitude = filters.longitude
    }
    if (filters.radiusKm !== undefined) {
      params.radiusKm = filters.radiusKm
    }
    if (filters.location) {
      params.latitude = filters.location.latitude
      params.longitude = filters.location.longitude
      params.radiusKm = filters.location.radiusKm
    }

    // Rating
    if (filters.minRating !== undefined) {
      params.minRating = filters.minRating
    }

    // Price range
    if (filters.priceRange) {
      params.minPrice = filters.priceRange.min
      params.maxPrice = filters.priceRange.max
    }

    // Availability
    if (filters.openNow !== undefined) {
      params.openNow = filters.openNow
    }
    if (filters.hasAvailableSlots !== undefined) {
      params.hasAvailableSlots = filters.hasAvailableSlots
    }

    // Sorting
    if (filters.sortBy) {
      params.sortBy = filters.sortBy
    }
    if (filters.sortOrder) {
      params.sortOrder = filters.sortOrder
    }

    // Pagination
    params.pageNumber = filters.pageNumber || 1
    params.pageSize = filters.pageSize || 20

    return params
  }
}

// Export singleton instance
export const providerSearchService = new ProviderSearchService()
export default providerSearchService
