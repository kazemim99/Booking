// Customer-facing search and browse types

import type { ProviderType, ProviderStatus } from '@/modules/provider/types/provider.types'

// ============================================
// Search Filters
// ============================================

export interface ProviderSearchFilters {
  // Text search
  searchTerm?: string
  query?: string

  // Category
  category?: ProviderType | ProviderType[]
  providerTypes?: ProviderType[]

  // Location-based
  location?: LocationFilter
  latitude?: number
  longitude?: number
  radiusKm?: number

  // Rating
  minRating?: number

  // Price
  priceRange?: PriceRangeFilter

  // Availability
  openNow?: boolean
  hasAvailableSlots?: boolean

  // Sorting
  sortBy?: 'distance' | 'rating' | 'popular' | 'newest'
  sortOrder?: 'asc' | 'desc'

  // Pagination
  pageNumber?: number
  pageSize?: number
}

export interface LocationFilter {
  latitude: number
  longitude: number
  radiusKm: number
  address?: string
}

export interface PriceRangeFilter {
  min: number
  max: number
}

// ============================================
// Search Results
// ============================================

export interface ProviderSearchResult {
  id: string
  businessName: string
  description: string
  type: ProviderType
  status: ProviderStatus

  // Images
  logoUrl?: string
  coverImageUrl?: string

  // Rating
  averageRating: number
  totalReviews: number

  // Location
  address: {
    formattedAddress: string
    city: string
    province: string
  }
  coordinates?: {
    latitude: number
    longitude: number
  }
  distanceKm?: number // Only present if location search

  // Availability
  isOpen: boolean
  currentStatus: string // "Open", "Closed", "Opens at 9:00 AM"
  nextAvailableSlot?: {
    startTime: string
    endTime: string
  }

  // Quick info
  allowOnlineBooking: boolean
  offersMobileServices: boolean
  tags: string[]

  // Pricing (optional)
  priceRange?: {
    min: number
    max: number
    currency: string
  }
}

export interface ServiceSearchResult {
  id: string
  name: string
  description: string
  category: string

  // Pricing
  basePrice: number
  currency: string

  // Duration
  durationMinutes: number

  // Provider info
  provider: {
    id: string
    businessName: string
    logoUrl?: string
    averageRating: number
  }

  // Availability
  allowOnlineBooking: boolean
  isAvailable: boolean
}

// ============================================
// Paged Results
// ============================================

export interface PagedSearchResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// ============================================
// Service Filters
// ============================================

export interface ServiceSearchFilters {
  searchTerm?: string
  category?: string
  minPrice?: number
  maxPrice?: number
  maxDuration?: number // in minutes
  providerId?: string
  pageNumber?: number
  pageSize?: number
}

// ============================================
// View Models (for UI display)
// ============================================

export interface ProviderCardViewModel {
  id: string
  businessName: string
  type: string // Display string
  logoUrl?: string
  rating: number
  reviewCount: number
  address: string
  distance?: string // "2.5 کیلومتر"
  status: string // "باز" or "بسته" or "ساعت ۹ باز می‌شود"
  isOpen: boolean
  nextSlot?: string // "امروز ۱۴:۰۰"
  tags: string[]
  priceRange?: string // "۱۰۰,۰۰۰ - ۵۰۰,۰۰۰ تومان"
  allowOnlineBooking: boolean
}

export interface ServiceCardViewModel {
  id: string
  name: string
  description: string
  price: string // Formatted: "۱۵۰,۰۰۰ تومان"
  duration: string // Formatted: "۹۰ دقیقه"
  providerName: string
  providerId: string
  providerLogo?: string
  rating: number
  isAvailable: boolean
}
