/**
 * Customer Favorites Types
 *
 * Type definitions for favorite providers and quick rebooking.
 * Based on backend Favorites API.
 */

// ============================================================================
// CORE ENTITIES
// ============================================================================

/**
 * Favorite provider entity
 */
export interface FavoriteProvider {
  id: string
  customerId: string
  providerId: string
  provider?: ProviderSummary
  notes?: string
  addedAt: string
  lastBookedAt?: string
  totalBookings: number
}

/**
 * Provider summary for favorites
 */
export interface ProviderSummary {
  id: string
  businessName: string
  logoUrl?: string
  coverImageUrl?: string
  rating: number
  reviewCount: number
  category: string
  address: {
    city: string
    state: string
    formattedAddress: string
  }
  allowOnlineBooking: boolean
  offersMobileServices: boolean
}

/**
 * Quick rebook suggestion
 */
export interface QuickRebookSuggestion {
  providerId: string
  providerName: string
  serviceId: string
  serviceName: string
  lastBookedDate: string
  suggestedTimeSlots: TimeSlotSuggestion[]
}

/**
 * Time slot suggestion for rebooking
 */
export interface TimeSlotSuggestion {
  date: string // ISO date
  startTime: string // HH:mm
  endTime: string // HH:mm
  staffId?: string
  staffName?: string
  available: boolean
}

// ============================================================================
// REQUEST/RESPONSE TYPES
// ============================================================================

/**
 * Request to add favorite provider
 */
export interface AddFavoriteRequest {
  providerId: string
  notes?: string
}

/**
 * Request to remove favorite provider
 */
export interface RemoveFavoriteRequest {
  providerId: string
}

/**
 * Request to get favorites list
 */
export interface GetFavoritesRequest {
  customerId: string
  includeProviderDetails?: boolean
  page?: number
  pageSize?: number
}

/**
 * Request to get quick rebook suggestions
 */
export interface GetQuickRebookRequest {
  customerId: string
  providerId: string
  limit?: number
}

// ============================================================================
// VIEW MODELS
// ============================================================================

/**
 * Favorite card view for display
 */
export interface FavoriteCardView {
  id: string
  providerId: string
  businessName: string
  logoUrl?: string
  rating: number
  reviewCount: number
  category: string
  location: string
  lastBookedAt?: string
  totalBookings: number
  notes?: string
  allowOnlineBooking: boolean
}

/**
 * Quick rebook card view
 */
export interface QuickRebookCardView {
  providerId: string
  providerName: string
  serviceName: string
  lastBookedDate: string
  suggestedDate: string
  suggestedTime: string
  available: boolean
}

// ============================================================================
// STATISTICS TYPES
// ============================================================================

/**
 * Favorites statistics
 */
export interface FavoritesStatistics {
  totalFavorites: number
  recentlyAdded: number // Added in last 30 days
  mostBooked: FavoriteProvider | null
  totalBookingsFromFavorites: number
}

// ============================================================================
// FILTER/SEARCH TYPES
// ============================================================================

/**
 * Favorites filter options
 */
export interface FavoritesFilters {
  category?: string
  city?: string
  hasRecentBooking?: boolean // Booked in last 30 days
  searchTerm?: string
  sortBy?: 'name' | 'addedAt' | 'lastBookedAt' | 'totalBookings'
  sortOrder?: 'asc' | 'desc'
}

// ============================================================================
// TYPE GUARDS
// ============================================================================

export function isFavoriteProvider(obj: unknown): obj is FavoriteProvider {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    'id' in obj &&
    'customerId' in obj &&
    'providerId' in obj &&
    'addedAt' in obj
  )
}

export function isProviderSummary(obj: unknown): obj is ProviderSummary {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    'id' in obj &&
    'businessName' in obj
  )
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Format time ago
 */
export function formatTimeAgo(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) return 'امروز'
  if (diffDays === 1) return 'دیروز'
  if (diffDays < 7) return `${diffDays} روز پیش`
  if (diffDays < 30) return `${Math.floor(diffDays / 7)} هفته پیش`
  if (diffDays < 365) return `${Math.floor(diffDays / 30)} ماه پیش`
  return `${Math.floor(diffDays / 365)} سال پیش`
}

/**
 * Check if provider was recently booked
 */
export function isRecentlyBooked(lastBookedAt?: string): boolean {
  if (!lastBookedAt) return false

  const lastBooked = new Date(lastBookedAt)
  const now = new Date()
  const diffDays = Math.floor((now.getTime() - lastBooked.getTime()) / (1000 * 60 * 60 * 24))

  return diffDays <= 30
}

/**
 * Get provider category label in Persian
 */
export function getCategoryLabel(category: string): string {
  const categoryLabels: Record<string, string> = {
    'Beauty': 'زیبایی',
    'Hair': 'آرایشگاه',
    'Spa': 'اسپا',
    'Fitness': 'ورزش',
    'Wellness': 'سلامت',
    'Medical': 'پزشکی',
    'Automotive': 'خودرو',
    'Home': 'خانه',
    'Other': 'سایر',
  }

  return categoryLabels[category] || category
}

/**
 * Format rating display
 */
export function formatRating(rating: number): string {
  return rating.toFixed(1)
}

/**
 * Get star rating array for display
 */
export function getStarRating(rating: number): { full: number; half: boolean; empty: number } {
  const full = Math.floor(rating)
  const half = rating % 1 >= 0.5
  const empty = 5 - full - (half ? 1 : 0)

  return { full, half, empty }
}

/**
 * Convert FavoriteProvider to FavoriteCardView
 */
export function toFavoriteCardView(favorite: FavoriteProvider): FavoriteCardView {
  const provider = favorite.provider

  return {
    id: favorite.id,
    providerId: favorite.providerId,
    businessName: provider?.businessName || 'نامشخص',
    logoUrl: provider?.logoUrl,
    rating: provider?.rating || 0,
    reviewCount: provider?.reviewCount || 0,
    category: provider?.category || 'Other',
    location: provider?.address?.city || 'نامشخص',
    lastBookedAt: favorite.lastBookedAt,
    totalBookings: favorite.totalBookings,
    notes: favorite.notes,
    allowOnlineBooking: provider?.allowOnlineBooking || false,
  }
}

/**
 * Sort favorites by criteria
 */
export function sortFavorites(
  favorites: FavoriteProvider[],
  sortBy: 'name' | 'addedAt' | 'lastBookedAt' | 'totalBookings' = 'addedAt',
  sortOrder: 'asc' | 'desc' = 'desc'
): FavoriteProvider[] {
  const sorted = [...favorites].sort((a, b) => {
    let comparison = 0

    switch (sortBy) {
      case 'name':
        comparison = (a.provider?.businessName || '').localeCompare(
          b.provider?.businessName || ''
        )
        break

      case 'addedAt':
        comparison = new Date(a.addedAt).getTime() - new Date(b.addedAt).getTime()
        break

      case 'lastBookedAt':
        const aDate = a.lastBookedAt ? new Date(a.lastBookedAt).getTime() : 0
        const bDate = b.lastBookedAt ? new Date(b.lastBookedAt).getTime() : 0
        comparison = aDate - bDate
        break

      case 'totalBookings':
        comparison = a.totalBookings - b.totalBookings
        break
    }

    return sortOrder === 'asc' ? comparison : -comparison
  })

  return sorted
}

/**
 * Filter favorites by criteria
 */
export function filterFavorites(
  favorites: FavoriteProvider[],
  filters: FavoritesFilters
): FavoriteProvider[] {
  return favorites.filter((favorite) => {
    // Search term filter
    if (filters.searchTerm) {
      const searchLower = filters.searchTerm.toLowerCase()
      const matchesName = favorite.provider?.businessName?.toLowerCase().includes(searchLower)
      const matchesNotes = favorite.notes?.toLowerCase().includes(searchLower)

      if (!matchesName && !matchesNotes) {
        return false
      }
    }

    // Category filter
    if (filters.category && favorite.provider?.category !== filters.category) {
      return false
    }

    // City filter
    if (filters.city && favorite.provider?.address?.city !== filters.city) {
      return false
    }

    // Recent booking filter
    if (filters.hasRecentBooking && !isRecentlyBooked(favorite.lastBookedAt)) {
      return false
    }

    return true
  })
}
