/**
 * Category Service
 * Handles service category data
 */

import { httpClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/categories`

// ==================== Types ====================

/**
 * Service Category
 */
export interface ServiceCategory {
  name: string
  description?: string
  iconUrl?: string
  color: string
  slug: string
}

/**
 * Popular category with counts
 */
export interface PopularCategory {
  name: string
  slug: string
  icon: string
  gradient: string
  providerCount: number
  description?: string
  color?: string
  displayOrder?: number
}

// ==================== Category Service Class ====================

class CategoryService {
  // Cache for categories to reduce API calls
  private categoriesCache: PopularCategory[] | null = null
  private popularCategoriesCache: PopularCategory[] | null = null
  private cacheTimestamp: number = 0
  private readonly CACHE_DURATION = 5 * 60 * 1000 // 5 minutes

  // ============================================
  // Categories
  // ============================================

  /**
   * Get all service categories with provider counts
   * GET /api/v1/categories?limit=25
   */
  async getCategories(limit = 25, useCache = true): Promise<PopularCategory[]> {
    try {
      // Check if cache is still valid
      const now = Date.now()
      const cacheIsValid = this.categoriesCache &&
                          useCache &&
                          (now - this.cacheTimestamp) < this.CACHE_DURATION

      if (cacheIsValid) {
        console.log('[CategoryService] Returning cached categories')
        return this.categoriesCache!
      }

      console.log('[CategoryService] Fetching categories from API')

      const response = await httpClient.get<ApiResponse<PopularCategory[]>>(
        `${API_BASE}?limit=${limit}`
      )

      console.log('[CategoryService] Categories retrieved:', response.data)

      // Handle wrapped response format
      const categories = response.data?.data || response.data
      const categoriesList = categories as PopularCategory[]

      // Cache the results
      this.categoriesCache = categoriesList
      this.cacheTimestamp = now

      return categoriesList
    } catch (error) {
      console.error('[CategoryService] Error fetching categories:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Get category by slug
   */
  async getCategoryBySlug(slug: string): Promise<PopularCategory | null> {
    const categories = await this.getCategories()
    return categories.find(c => c.slug === slug) || null
  }

  /**
   * Search categories by name
   */
  async searchCategories(query: string): Promise<PopularCategory[]> {
    const categories = await this.getCategories()
    const lowerQuery = query.toLowerCase()

    return categories.filter(category =>
      category.name.includes(query) ||
      category.slug.toLowerCase().includes(lowerQuery) ||
      category.description?.includes(query)
    )
  }

  /**
   * Get popular categories
   * GET /api/v1/categories/popular?limit=8
   *
   * Returns categories sorted by provider count (most popular first)
   */
  async getPopularCategories(limit = 8, useCache = true): Promise<PopularCategory[]> {
    try {
      // Check if cache is still valid
      const now = Date.now()
      const cacheIsValid = this.popularCategoriesCache &&
                          useCache &&
                          (now - this.cacheTimestamp) < this.CACHE_DURATION

      if (cacheIsValid) {
        console.log('[CategoryService] Returning cached popular categories')
        return this.popularCategoriesCache!.slice(0, limit)
      }

      console.log('[CategoryService] Fetching popular categories from API')

      const response = await httpClient.get<ApiResponse<PopularCategory[]>>(
        `${API_BASE}/popular?limit=${limit}`
      )

      console.log('[CategoryService] Popular categories retrieved:', response.data)

      // Handle wrapped response format
      const categories = response.data?.data || response.data
      const categoriesList = categories as PopularCategory[]

      // Cache the results
      this.popularCategoriesCache = categoriesList
      this.cacheTimestamp = now

      return categoriesList
    } catch (error) {
      console.error('[CategoryService] Error fetching popular categories:', error)
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
    this.categoriesCache = null
    this.popularCategoriesCache = null
    this.cacheTimestamp = 0
    console.log('[CategoryService] Cache cleared')
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
    return new Error('خطا در بارگذاری اطلاعات دسته‌بندی')
  }
}

// Export singleton instance
export const categoryService = new CategoryService()
export default categoryService
