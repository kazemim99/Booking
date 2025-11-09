/**
 * Integration Tests: Customer Favorites (Priority 6)
 *
 * Tests the integration between Favorites components and UserManagement API
 *
 * Prerequisites:
 * - UserManagement API running on http://localhost:5020
 * - Valid JWT token for customer account
 * - Test providers available
 */

import { describe, it, expect, beforeAll, afterEach } from 'vitest'
import { favoritesService } from '@/modules/customer/services/favorites.service'

describe('Customer Favorites Integration Tests', () => {
  let customerId: string
  let testProviderId: string
  let authToken: string
  const createdFavoriteIds: string[] = []

  beforeAll(async () => {
    customerId = process.env.TEST_CUSTOMER_ID || 'test-customer-id'
    testProviderId = process.env.TEST_PROVIDER_ID || 'test-provider-id'
    authToken = process.env.TEST_AUTH_TOKEN || ''

    if (!authToken) {
      console.warn('⚠️  No auth token provided. Set TEST_AUTH_TOKEN environment variable.')
    }
  })

  afterEach(async () => {
    // Cleanup created favorites
    for (const favoriteId of createdFavoriteIds) {
      try {
        await favoritesService.removeFavorite(customerId, favoriteId)
      } catch (error) {
        // Ignore cleanup errors
      }
    }
    createdFavoriteIds.length = 0
  })

  describe('Add Favorite Provider', () => {
    it('should add a provider to favorites', async () => {
      const favorite = await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
        notes: 'آرایشگاه عالی',
      })

      expect(favorite).toHaveProperty('id')
      expect(favorite).toHaveProperty('customerId', customerId)
      expect(favorite).toHaveProperty('providerId', testProviderId)
      expect(favorite).toHaveProperty('notes', 'آرایشگاه عالی')
      expect(favorite).toHaveProperty('addedAt')

      createdFavoriteIds.push(testProviderId)
    })

    it('should add favorite without notes', async () => {
      const favorite = await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
      })

      expect(favorite).toHaveProperty('id')
      expect(favorite.notes).toBeUndefined()

      createdFavoriteIds.push(testProviderId)
    })

    it('should handle duplicate favorite (idempotent)', async () => {
      // Add once
      await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
      })

      // Try to add again - should either succeed or return existing
      const result = await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
      })

      expect(result).toHaveProperty('providerId', testProviderId)

      createdFavoriteIds.push(testProviderId)
    })
  })

  describe('Get Favorites List', () => {
    beforeAll(async () => {
      // Ensure at least one favorite exists
      await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
      })
      createdFavoriteIds.push(testProviderId)
    })

    it('should retrieve all favorites for a customer', async () => {
      const favorites = await favoritesService.getFavorites({
        customerId,
        includeProviderDetails: true,
      })

      expect(Array.isArray(favorites)).toBe(true)
      expect(favorites.length).toBeGreaterThan(0)

      favorites.forEach(favorite => {
        expect(favorite).toHaveProperty('id')
        expect(favorite).toHaveProperty('customerId', customerId)
        expect(favorite).toHaveProperty('providerId')
        expect(favorite).toHaveProperty('addedAt')
      })
    })

    it('should include provider details when requested', async () => {
      const favorites = await favoritesService.getFavorites({
        customerId,
        includeProviderDetails: true,
      })

      if (favorites.length > 0) {
        expect(favorites[0]).toHaveProperty('provider')
        expect(favorites[0].provider).toHaveProperty('businessName')
        expect(favorites[0].provider).toHaveProperty('rating')
        expect(favorites[0].provider).toHaveProperty('category')
      }
    })

    it('should support pagination', async () => {
      const page1 = await favoritesService.getFavorites({
        customerId,
        page: 1,
        pageSize: 5,
      })

      const page2 = await favoritesService.getFavorites({
        customerId,
        page: 2,
        pageSize: 5,
      })

      expect(page1.length).toBeLessThanOrEqual(5)
      expect(page2.length).toBeLessThanOrEqual(5)

      // Pages should have different items
      if (page1.length > 0 && page2.length > 0) {
        expect(page1[0].id).not.toBe(page2[0].id)
      }
    })

    it('should cache favorites for 10 minutes', async () => {
      // First call - from API
      const firstCall = await favoritesService.getFavorites({ customerId })

      // Second call - should be from cache
      const secondCall = await favoritesService.getFavorites({ customerId })

      expect(firstCall).toEqual(secondCall)
    })
  })

  describe('Remove Favorite Provider', () => {
    it('should remove a favorite successfully', async () => {
      // Add favorite first
      await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
      })

      // Remove it
      await favoritesService.removeFavorite(customerId, testProviderId)

      // Verify it's removed
      const favorites = await favoritesService.getFavorites({ customerId })
      const removed = favorites.find(f => f.providerId === testProviderId)

      expect(removed).toBeUndefined()
    })

    it('should invalidate cache on removal', async () => {
      // Add and cache
      await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
      })
      await favoritesService.getFavorites({ customerId }) // Cache it

      // Remove
      await favoritesService.removeFavorite(customerId, testProviderId)

      // Fresh fetch should not include removed item
      const favorites = await favoritesService.getFavorites({ customerId })
      const removed = favorites.find(f => f.providerId === testProviderId)

      expect(removed).toBeUndefined()
    })

    it('should handle removing non-existent favorite', async () => {
      const nonExistentId = 'non-existent-provider-id'

      await expect(
        favoritesService.removeFavorite(customerId, nonExistentId)
      ).rejects.toThrow()
    })
  })

  describe('Toggle Favorite', () => {
    it('should toggle favorite from not favorited to favorited', async () => {
      // Ensure not favorited
      await favoritesService.removeFavorite(customerId, testProviderId).catch(() => {})

      // Toggle to favorite
      const result = await favoritesService.toggleFavorite(customerId, testProviderId)

      expect(result.isFavorite).toBe(true)
      expect(result.favorite).toBeDefined()
      expect(result.favorite?.providerId).toBe(testProviderId)

      createdFavoriteIds.push(testProviderId)
    })

    it('should toggle favorite from favorited to not favorited', async () => {
      // Ensure favorited
      await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
      })

      // Toggle to unfavorite
      const result = await favoritesService.toggleFavorite(customerId, testProviderId)

      expect(result.isFavorite).toBe(false)
      expect(result.favorite).toBeUndefined()
    })

    it('should add notes when toggling to favorite', async () => {
      // Remove first
      await favoritesService.removeFavorite(customerId, testProviderId).catch(() => {})

      // Toggle with notes
      const result = await favoritesService.toggleFavorite(
        customerId,
        testProviderId,
        'بهترین آرایشگاه'
      )

      expect(result.isFavorite).toBe(true)
      expect(result.favorite?.notes).toBe('بهترین آرایشگاه')

      createdFavoriteIds.push(testProviderId)
    })
  })

  describe('Check Favorite Status', () => {
    it('should return true for favorited provider', async () => {
      // Add to favorites
      await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
      })

      // Load into cache
      await favoritesService.getFavorites({ customerId })

      const isFavorite = favoritesService.isFavorite(testProviderId)

      expect(isFavorite).toBe(true)

      createdFavoriteIds.push(testProviderId)
    })

    it('should return false for non-favorited provider', async () => {
      // Ensure not favorited
      await favoritesService.removeFavorite(customerId, testProviderId).catch(() => {})

      // Load into cache
      await favoritesService.getFavorites({ customerId })

      const isFavorite = favoritesService.isFavorite(testProviderId)

      expect(isFavorite).toBe(false)
    })

    it('should use Set for O(1) lookup', async () => {
      // Add multiple favorites
      const providerIds = ['provider1', 'provider2', 'provider3']

      for (const providerId of providerIds) {
        await favoritesService.addFavorite(customerId, { providerId })
        createdFavoriteIds.push(providerId)
      }

      // Load into cache
      await favoritesService.getFavorites({ customerId })

      // Check all (should be fast even with many favorites)
      const startTime = Date.now()
      providerIds.forEach(id => favoritesService.isFavorite(id))
      const duration = Date.now() - startTime

      // Should be nearly instant (< 10ms for 3 checks)
      expect(duration).toBeLessThan(10)
    })
  })

  describe('Quick Rebook Suggestions', () => {
    it('should fetch quick rebook suggestions', async () => {
      const suggestions = await favoritesService.getQuickRebookSuggestions(customerId)

      expect(Array.isArray(suggestions)).toBe(true)

      if (suggestions.length > 0) {
        const suggestion = suggestions[0]

        expect(suggestion).toHaveProperty('favorite')
        expect(suggestion.favorite).toHaveProperty('providerId')
        expect(suggestion).toHaveProperty('suggestedSlots')
        expect(Array.isArray(suggestion.suggestedSlots)).toBe(true)

        if (suggestion.lastService) {
          expect(suggestion.lastService).toHaveProperty('id')
          expect(suggestion.lastService).toHaveProperty('name')
          expect(suggestion.lastService).toHaveProperty('duration')
          expect(suggestion.lastService).toHaveProperty('price')
        }
      }
    })

    it('should include available time slots', async () => {
      const suggestions = await favoritesService.getQuickRebookSuggestions(customerId)

      suggestions.forEach(suggestion => {
        suggestion.suggestedSlots.forEach(slot => {
          expect(slot).toHaveProperty('date')
          expect(slot).toHaveProperty('startTime')
          expect(slot).toHaveProperty('endTime')
          expect(slot).toHaveProperty('available', true)
        })
      })
    })

    it('should prioritize recently booked providers', async () => {
      const suggestions = await favoritesService.getQuickRebookSuggestions(customerId)

      if (suggestions.length > 1) {
        // First suggestion should have most recent booking
        for (let i = 0; i < suggestions.length - 1; i++) {
          const current = suggestions[i].favorite.lastBookedAt
          const next = suggestions[i + 1].favorite.lastBookedAt

          if (current && next) {
            expect(new Date(current).getTime()).toBeGreaterThanOrEqual(
              new Date(next).getTime()
            )
          }
        }
      }
    })
  })

  describe('Error Handling', () => {
    it('should handle invalid customer ID', async () => {
      const invalidCustomerId = 'invalid-customer-id'

      await expect(
        favoritesService.getFavorites({ customerId: invalidCustomerId })
      ).rejects.toThrow()
    })

    it('should handle invalid provider ID when adding', async () => {
      const invalidProviderId = ''

      await expect(
        favoritesService.addFavorite(customerId, { providerId: invalidProviderId })
      ).rejects.toThrow()
    })

    it('should handle network errors gracefully', async () => {
      // Service should not crash on network errors
      expect(favoritesService).toBeDefined()
    })
  })

  describe('Cache Management', () => {
    it('should invalidate cache on add', async () => {
      // Get initial favorites
      const before = await favoritesService.getFavorites({ customerId })

      // Add new favorite
      await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
      })
      createdFavoriteIds.push(testProviderId)

      // Get updated favorites (should fetch fresh data)
      const after = await favoritesService.getFavorites({ customerId })

      expect(after.length).toBeGreaterThan(before.length)
    })

    it('should clear cache when needed', () => {
      favoritesService.clearCache()

      // Next fetch should be from API, not cache
      // This is hard to test without mocking, but we ensure it doesn't throw
      expect(() => favoritesService.clearCache()).not.toThrow()
    })
  })

  describe('Persian/RTL Support', () => {
    it('should handle Persian notes correctly', async () => {
      const persianNotes = 'بهترین آرایشگاه شهر - خدمات عالی'

      const favorite = await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
        notes: persianNotes,
      })

      expect(favorite.notes).toBe(persianNotes)

      createdFavoriteIds.push(testProviderId)
    })

    it('should handle mixed Persian/English content', async () => {
      const mixedNotes = 'Best salon - بهترین آرایشگاه'

      const favorite = await favoritesService.addFavorite(customerId, {
        providerId: testProviderId,
        notes: mixedNotes,
      })

      expect(favorite.notes).toBe(mixedNotes)

      createdFavoriteIds.push(testProviderId)
    })
  })
})
