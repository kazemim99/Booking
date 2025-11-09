/**
 * Favorites Service
 * Handles customer favorite providers and quick rebooking
 */

import { httpClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  FavoriteProvider,
  AddFavoriteRequest,
  RemoveFavoriteRequest,
  GetFavoritesRequest,
  QuickRebookSuggestion,
} from '../types/favorites.types'

const API_VERSION = 'v1'
const FAVORITES_BASE = `/${API_VERSION}/customers`

// ==================== Favorites Cache ====================

interface CacheEntry<T> {
  data: T
  timestamp: number
  expiresAt: number
}

class FavoritesCache<T> {
  private cache = new Map<string, CacheEntry<T>>()
  private ttl: number

  constructor(ttlMinutes: number = 10) {
    this.ttl = ttlMinutes * 60 * 1000
  }

  set(key: string, data: T): void {
    const now = Date.now()
    this.cache.set(key, {
      data,
      timestamp: now,
      expiresAt: now + this.ttl,
    })
  }

  get(key: string): T | null {
    const entry = this.cache.get(key)
    if (!entry) return null

    if (Date.now() > entry.expiresAt) {
      this.cache.delete(key)
      return null
    }

    return entry.data
  }

  invalidate(key: string): void {
    this.cache.delete(key)
  }

  clear(): void {
    this.cache.clear()
  }
}

// ==================== Favorites Service Class ====================

class FavoritesService {
  // Cache for favorites list (10 minute TTL)
  private favoritesCache = new FavoritesCache<FavoriteProvider[]>(10)

  // Set of favorite provider IDs for quick checks
  private favoriteIdsSet = new Set<string>()

  // ============================================
  // Favorites CRUD Operations
  // ============================================

  /**
   * Get customer's favorite providers
   * GET /api/v1/customers/{customerId}/favorites
   */
  async getFavorites(request: GetFavoritesRequest): Promise<FavoriteProvider[]> {
    try {
      console.log('[FavoritesService] Fetching favorites:', request)

      // Check cache first
      const cacheKey = `favorites_${request.customerId}`
      const cached = this.favoritesCache.get(cacheKey)
      if (cached) {
        console.log('[FavoritesService] Returning cached favorites')
        this.updateFavoriteIdsSet(cached)
        return cached
      }

      const response = await httpClient.get<ApiResponse<FavoriteProvider[]>>(
        `${FAVORITES_BASE}/${request.customerId}/favorites`,
        {
          params: {
            includeProviderDetails: request.includeProviderDetails ?? true,
            pageNumber: request.page,
            pageSize: request.pageSize,
          },
        }
      )

      console.log('[FavoritesService] Favorites retrieved:', response.data)

      // Handle wrapped response
      let favorites: FavoriteProvider[]

      if (response.data?.data) {
        // Check if it's a paged result
        if (Array.isArray(response.data.data)) {
          favorites = response.data.data
        } else if ((response.data.data as any).items) {
          favorites = (response.data.data as any).items
        } else {
          favorites = []
        }
      } else if (Array.isArray(response.data)) {
        favorites = response.data
      } else {
        favorites = []
      }

      // Cache the results
      this.favoritesCache.set(cacheKey, favorites)
      this.updateFavoriteIdsSet(favorites)

      return favorites
    } catch (error) {
      console.error('[FavoritesService] Error fetching favorites:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Add provider to favorites
   * POST /api/v1/customers/{customerId}/favorites
   */
  async addFavorite(customerId: string, request: AddFavoriteRequest): Promise<FavoriteProvider> {
    try {
      console.log('[FavoritesService] Adding favorite:', request)

      const response = await httpClient.post<ApiResponse<FavoriteProvider>>(
        `${FAVORITES_BASE}/${customerId}/favorites`,
        request
      )

      console.log('[FavoritesService] Favorite added:', response.data)

      // Handle wrapped response
      const favorite = response.data?.data || response.data

      if (!favorite) {
        throw new Error('Failed to add favorite')
      }

      // Invalidate cache
      this.invalidateCache(customerId)

      // Add to favorites set
      this.favoriteIdsSet.add(request.providerId)

      return favorite as FavoriteProvider
    } catch (error) {
      console.error('[FavoritesService] Error adding favorite:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Remove provider from favorites
   * DELETE /api/v1/customers/{customerId}/favorites/{providerId}
   */
  async removeFavorite(customerId: string, providerId: string): Promise<void> {
    try {
      console.log(`[FavoritesService] Removing favorite: ${providerId}`)

      await httpClient.delete(
        `${FAVORITES_BASE}/${customerId}/favorites/${providerId}`
      )

      console.log('[FavoritesService] Favorite removed successfully')

      // Invalidate cache
      this.invalidateCache(customerId)

      // Remove from favorites set
      this.favoriteIdsSet.delete(providerId)
    } catch (error) {
      console.error(`[FavoritesService] Error removing favorite ${providerId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Toggle favorite status
   * Convenience method to add/remove favorite
   */
  async toggleFavorite(
    customerId: string,
    providerId: string,
    notes?: string
  ): Promise<{ isFavorite: boolean; favorite?: FavoriteProvider }> {
    const isFavorite = this.isFavorite(providerId)

    if (isFavorite) {
      await this.removeFavorite(customerId, providerId)
      return { isFavorite: false }
    } else {
      const favorite = await this.addFavorite(customerId, { providerId, notes })
      return { isFavorite: true, favorite }
    }
  }

  // ============================================
  // Quick Check Operations
  // ============================================

  /**
   * Check if provider is in favorites (from cache)
   * Fast check without API call
   */
  isFavorite(providerId: string): boolean {
    return this.favoriteIdsSet.has(providerId)
  }

  /**
   * Get favorite by provider ID (from cache)
   */
  getFavoriteByProviderId(providerId: string, customerId: string): FavoriteProvider | null {
    const cacheKey = `favorites_${customerId}`
    const cached = this.favoritesCache.get(cacheKey)

    if (!cached) return null

    return cached.find((f) => f.providerId === providerId) || null
  }

  // ============================================
  // Quick Rebook Operations
  // ============================================

  /**
   * Get quick rebook suggestions for a favorite provider
   * GET /api/v1/customers/{customerId}/favorites/{providerId}/quick-rebook
   */
  async getQuickRebookSuggestions(
    customerId: string,
    providerId: string,
    limit: number = 3
  ): Promise<QuickRebookSuggestion[]> {
    try {
      console.log(`[FavoritesService] Fetching quick rebook suggestions for ${providerId}`)

      const response = await httpClient.get<ApiResponse<QuickRebookSuggestion[]>>(
        `${FAVORITES_BASE}/${customerId}/favorites/${providerId}/quick-rebook`,
        {
          params: { limit },
        }
      )

      const data = response.data?.data || response.data

      return Array.isArray(data) ? data : []
    } catch (error) {
      console.error('[FavoritesService] Error fetching quick rebook suggestions:', error)
      // Return empty array instead of throwing - quick rebook is optional
      return []
    }
  }

  // ============================================
  // Cache Management
  // ============================================

  /**
   * Invalidate favorites cache for a customer
   */
  invalidateCache(customerId: string): void {
    const cacheKey = `favorites_${customerId}`
    this.favoritesCache.invalidate(cacheKey)
    console.log(`[FavoritesService] Cache invalidated for customer ${customerId}`)
  }

  /**
   * Clear all caches
   */
  clearCache(): void {
    this.favoritesCache.clear()
    this.favoriteIdsSet.clear()
    console.log('[FavoritesService] All caches cleared')
  }

  /**
   * Update the favorites IDs set from favorites list
   */
  private updateFavoriteIdsSet(favorites: FavoriteProvider[]): void {
    this.favoriteIdsSet.clear()
    favorites.forEach((f) => this.favoriteIdsSet.add(f.providerId))
  }

  // ============================================
  // Helper Methods
  // ============================================================================

  /**
   * Centralized error handling
   */
  private handleError(error: unknown): Error {
    if (error instanceof Error) {
      return error
    }
    return new Error('خطا در عملیات علاقه‌مندی‌ها')
  }
}

// Export singleton instance
export const favoritesService = new FavoritesService()
export default favoritesService
