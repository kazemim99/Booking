/**
 * Customer Store
 * Centralized Pinia store for customer profile, bookings, favorites, reviews, and preferences
 */

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { customerService } from '../services/customer.service'
import { favoritesService } from '../services/favorites.service'
import { cache, CacheKeys, CacheTTL } from '@/core/utils/cache'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import type {
  CustomerProfile,
  UpdateCustomerProfileRequest,
  UpcomingBooking,
  BookingHistoryEntry,
  CustomerReview,
  UpdateReviewRequest,
  NotificationPreferences,
  UpdatePreferencesRequest,
  CustomerModalType,
  LoadingState,
  ErrorState,
} from '../types/customer.types'
import type { FavoriteProvider } from '../types/favorites.types'

export const useCustomerStore = defineStore('customer', () => {
  // ========================================
  // State
  // ========================================

  const profile = ref<CustomerProfile | null>(null)
  const upcomingBookings = ref<UpcomingBooking[]>([])
  const bookingHistory = ref<BookingHistoryEntry[]>([])
  const favorites = ref<FavoriteProvider[]>([])
  const reviews = ref<CustomerReview[]>([])
  const preferences = ref<NotificationPreferences | null>(null)

  const activeModal = ref<CustomerModalType>(null)

  const loading = ref<LoadingState>({
    profile: false,
    upcomingBookings: false,
    bookingHistory: false,
    favorites: false,
    reviews: false,
    preferences: false,
  })

  const errors = ref<ErrorState>({
    profile: null,
    upcomingBookings: null,
    bookingHistory: null,
    favorites: null,
    reviews: null,
    preferences: null,
  })

  // Pagination for booking history
  const bookingHistoryPage = ref(1)
  const bookingHistoryTotalPages = ref(1)
  const bookingHistoryHasMore = computed(
    () => bookingHistoryPage.value < bookingHistoryTotalPages.value,
  )

  // ========================================
  // Getters
  // ========================================

  const userInitial = computed(() => {
    if (!profile.value?.firstName) return 'ک'
    const firstName = profile.value.firstName
    return firstName.charAt(0)
  })

  const userColor = computed(() => {
    if (!profile.value?.id) return '#667eea'
    const colors = ['#667eea', '#764ba2', '#f093fb', '#4facfe', '#fa709a', '#fee140']
    const index = profile.value.id.charCodeAt(0) % colors.length
    return colors[index]
  })

  const upcomingBookingsCount = computed(() => upcomingBookings.value.length)
  const favoritesCount = computed(() => favorites.value.length)
  const reviewsCount = computed(() => reviews.value.length)

  // ========================================
  // Profile Actions
  // ========================================

  async function fetchProfile(customerId: string, forceRefresh = false): Promise<void> {

    if (!customerId) {
      console.warn('[CustomerStore] No customer ID provided')
      return
    }

    // Check cache first (5 min TTL)
    const cacheKey = CacheKeys.customerProfile(customerId)
    if (!forceRefresh) {
      const cached = cache.get<CustomerProfile>(cacheKey)
      if (cached) {
        console.log('[CustomerStore] Profile loaded from cache')
        profile.value = cached
        return
      }
    }

    loading.value.profile = true
    errors.value.profile = null

    try {
      profile.value = await customerService.getProfile(customerId)
      // Cache the result (5 minutes)
      cache.set(cacheKey, profile.value, CacheTTL.MEDIUM)
      console.log('[CustomerStore] Profile fetched and cached')
    } catch (error) {
      const message = error instanceof Error ? error.message : 'خطا در دریافت اطلاعات کاربر'
      errors.value.profile = message
      console.error('[CustomerStore] Error fetching profile:', error)
      throw error
    } finally {
      loading.value.profile = false
    }
  }

  async function updateProfile(
    customerId: string,
    request: UpdateCustomerProfileRequest,
  ): Promise<void> {
    if (!customerId) {
      throw new Error('No customer ID provided')
    }

    loading.value.profile = true
    errors.value.profile = null

    try {
      profile.value = await customerService.updateProfile(customerId, request)

      // Update auth store user data to reflect the new name in the UI
      const authStore = useAuthStore()
      if (authStore.currentUser) {
        authStore.currentUser.firstName = request.firstName
        authStore.currentUser.lastName = request.lastName
        authStore.currentUser.fullName = `${request.firstName} ${request.lastName}`
        // Update localStorage to persist the changes
        localStorage.setItem('user', JSON.stringify(authStore.currentUser))
        console.log('[CustomerStore] Auth store user data updated with new name')
      }

      // Invalidate profile cache after update
      cache.invalidate(CacheKeys.customerProfile(customerId))
      console.log('[CustomerStore] Profile updated and cache invalidated')
    } catch (error) {
      const message = error instanceof Error ? error.message : 'خطا در بهروزرسانی پروفایل'
      errors.value.profile = message
      console.error('[CustomerStore] Error updating profile:', error)
      throw error
    } finally {
      loading.value.profile = false
    }
  }

  // ========================================
  // Bookings Actions
  // ========================================

  async function fetchUpcomingBookings(
    customerId: string,
    limit: number = 5,
    forceRefresh = false,
  ): Promise<void> {
    if (!customerId) {
      console.warn('[CustomerStore] No customer ID provided')
      return
    }

    // Check cache first (2 min TTL for bookings)
    const cacheKey = CacheKeys.upcomingBookings(customerId)
    if (!forceRefresh) {
      const cached = cache.get<UpcomingBooking[]>(cacheKey)
      if (cached) {
        console.log('[CustomerStore] Upcoming bookings loaded from cache')
        upcomingBookings.value = cached
        return
      }
    }

    loading.value.upcomingBookings = true
    errors.value.upcomingBookings = null

    try {
      upcomingBookings.value = await customerService.getUpcomingBookings(customerId, limit)
      // Cache the result (2 minutes - bookings change frequently)
      cache.set(cacheKey, upcomingBookings.value, CacheTTL.SHORT)
      console.log(
        '[CustomerStore] Upcoming bookings fetched and cached:',
        upcomingBookings.value.length,
      )
    } catch (error) {
      const message = error instanceof Error ? error.message : 'خطا در دریافت نوبت‌های آینده'
      errors.value.upcomingBookings = message
      console.error('[CustomerStore] Error fetching upcoming bookings:', error)
    } finally {
      loading.value.upcomingBookings = false
    }
  }

  async function fetchBookingHistory(
    customerId: string,
    page: number = 1,
    size: number = 20,
  ): Promise<void> {
    if (!customerId) {
      console.warn('[CustomerStore] No customer ID provided')
      return
    }

    loading.value.bookingHistory = true
    errors.value.bookingHistory = null

    try {
      const result = await customerService.getBookingHistory(customerId, page, size)

      if (page === 1) {
        bookingHistory.value = result.items
      } else {
        bookingHistory.value.push(...result.items)
      }

      bookingHistoryPage.value = result.page
      bookingHistoryTotalPages.value = result.totalPages

      console.log('[CustomerStore] Booking history fetched:', result.items.length)
    } catch (error) {
      const message = error instanceof Error ? error.message : 'خطا در دریافت تاریخچه نوبت‌ها'
      errors.value.bookingHistory = message
      console.error('[CustomerStore] Error fetching booking history:', error)
    } finally {
      loading.value.bookingHistory = false
    }
  }

  async function loadMoreBookingHistory(customerId: string): Promise<void> {
    if (!bookingHistoryHasMore.value || loading.value.bookingHistory) {
      return
    }

    await fetchBookingHistory(customerId, bookingHistoryPage.value + 1)
  }

  // ========================================
  // Favorites Actions
  // ========================================

  async function fetchFavorites(customerId: string, forceRefresh = false): Promise<void> {
    if (!customerId) {
      console.warn('[CustomerStore] No customer ID provided')
      return
    }

    // Check cache first (5 min TTL)
    const cacheKey = CacheKeys.favorites(customerId)
    if (!forceRefresh) {
      const cached = cache.get<FavoriteProvider[]>(cacheKey)
      if (cached) {
        console.log('[CustomerStore] Favorites loaded from cache')
        favorites.value = cached
        return
      }
    }

    loading.value.favorites = true
    errors.value.favorites = null

    try {
      favorites.value = await favoritesService.getFavorites({
        customerId,
        includeProviderDetails: true,
      })
      // Cache the result (5 minutes)
      cache.set(cacheKey, favorites.value, CacheTTL.MEDIUM)
      console.log('[CustomerStore] Favorites fetched and cached:', favorites.value.length)
    } catch (error) {
      const message = error instanceof Error ? error.message : 'خطا در دریافت علاقه‌مندی‌ها'
      errors.value.favorites = message
      console.error('[CustomerStore] Error fetching favorites:', error)
    } finally {
      loading.value.favorites = false
    }
  }

  async function addFavorite(
    customerId: string,
    providerId: string,
    notes?: string,
  ): Promise<void> {
    if (!customerId) {
      throw new Error('No customer ID provided')
    }

    loading.value.favorites = true

    try {
      const favorite = await favoritesService.addFavorite(customerId, { providerId, notes })
      favorites.value.push(favorite)
      // Invalidate favorites cache after adding
      cache.invalidate(CacheKeys.favorites(customerId))
      console.log('[CustomerStore] Favorite added and cache invalidated:', providerId)
    } catch (error) {
      console.error('[CustomerStore] Error adding favorite:', error)
      throw error
    } finally {
      loading.value.favorites = false
    }
  }

  async function removeFavorite(customerId: string, providerId: string): Promise<void> {
    if (!customerId) {
      throw new Error('No customer ID provided')
    }

    loading.value.favorites = true

    try {
      await favoritesService.removeFavorite(customerId, providerId)
      favorites.value = favorites.value.filter((f) => f.providerId !== providerId)
      // Invalidate favorites cache after removing
      cache.invalidate(CacheKeys.favorites(customerId))
      console.log('[CustomerStore] Favorite removed and cache invalidated:', providerId)
    } catch (error) {
      console.error('[CustomerStore] Error removing favorite:', error)
      throw error
    } finally {
      loading.value.favorites = false
    }
  }

  // ========================================
  // Reviews Actions
  // ========================================

  async function fetchReviews(customerId: string): Promise<void> {
    if (!customerId) {
      console.warn('[CustomerStore] No customer ID provided')
      return
    }

    loading.value.reviews = true
    errors.value.reviews = null

    try {
      reviews.value = await customerService.getReviews(customerId)
      console.log('[CustomerStore] Reviews fetched:', reviews.value.length)
    } catch (error) {
      const message = error instanceof Error ? error.message : 'خطا در دریافت نظرات'
      errors.value.reviews = message
      console.error('[CustomerStore] Error fetching reviews:', error)
    } finally {
      loading.value.reviews = false
    }
  }

  async function updateReview(
    customerId: string,
    reviewId: string,
    request: UpdateReviewRequest,
  ): Promise<void> {
    if (!customerId) {
      throw new Error('No customer ID provided')
    }

    loading.value.reviews = true

    try {
      const updatedReview = await customerService.updateReview(customerId, reviewId, request)
      const index = reviews.value.findIndex((r) => r.id === reviewId)

      if (index !== -1) {
        reviews.value[index] = updatedReview
      }

      console.log('[CustomerStore] Review updated:', reviewId)
    } catch (error) {
      console.error('[CustomerStore] Error updating review:', error)
      throw error
    } finally {
      loading.value.reviews = false
    }
  }

  // ========================================
  // Preferences Actions
  // ========================================

  async function fetchPreferences(customerId: string): Promise<void> {
    if (!customerId) {
      console.warn('[CustomerStore] No customer ID provided')
      return
    }

    loading.value.preferences = true
    errors.value.preferences = null

    try {
      preferences.value = await customerService.getPreferences(customerId)
      console.log('[CustomerStore] Preferences fetched')
    } catch (error) {
      const message = error instanceof Error ? error.message : 'خطا در دریافت تنظیمات'
      errors.value.preferences = message
      console.error('[CustomerStore] Error fetching preferences:', error)
    } finally {
      loading.value.preferences = false
    }
  }

  async function updatePreferences(
    customerId: string,
    request: UpdatePreferencesRequest,
  ): Promise<void> {
    if (!customerId) {
      throw new Error('No customer ID provided')
    }

    loading.value.preferences = true
    errors.value.preferences = null

    try {
      preferences.value = await customerService.updatePreferences(customerId, request)
      console.log('[CustomerStore] Preferences updated')
    } catch (error) {
      const message = error instanceof Error ? error.message : 'خطا در بهروزرسانی تنظیمات'
      errors.value.preferences = message
      console.error('[CustomerStore] Error updating preferences:', error)
      throw error
    } finally {
      loading.value.preferences = false
    }
  }

  // ========================================
  // UI Actions
  // ========================================

  function openModal(modal: Exclude<CustomerModalType, null>): void {
    activeModal.value = modal
    console.log('[CustomerStore] Modal opened:', modal)
  }

  function closeModal(): void {
    activeModal.value = null
    console.log('[CustomerStore] Modal closed')
  }

  // ========================================
  // Utility Actions
  // ========================================

  function clearErrors(): void {
    errors.value = {
      profile: null,
      upcomingBookings: null,
      bookingHistory: null,
      favorites: null,
      reviews: null,
      preferences: null,
    }
  }

  function resetStore(): void {
    profile.value = null
    upcomingBookings.value = []
    bookingHistory.value = []
    favorites.value = []
    reviews.value = []
    preferences.value = null
    activeModal.value = null
    bookingHistoryPage.value = 1
    bookingHistoryTotalPages.value = 1
    clearErrors()
    console.log('[CustomerStore] Store reset')
  }

  // ========================================
  // Return Store
  // ========================================

  return {
    // State
    profile,
    upcomingBookings,
    bookingHistory,
    favorites,
    reviews,
    preferences,
    activeModal,
    loading,
    errors,

    // Getters
    userInitial,
    userColor,
    upcomingBookingsCount,
    favoritesCount,
    reviewsCount,
    bookingHistoryHasMore,

    // Actions
    fetchProfile,
    updateProfile,
    fetchUpcomingBookings,
    fetchBookingHistory,
    loadMoreBookingHistory,
    fetchFavorites,
    addFavorite,
    removeFavorite,
    fetchReviews,
    updateReview,
    fetchPreferences,
    updatePreferences,
    openModal,
    closeModal,
    clearErrors,
    resetStore,
  }
})
