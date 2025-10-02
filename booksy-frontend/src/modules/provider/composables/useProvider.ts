// src/modules/provider/composables/useProvider.ts

import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '../stores/provider.store'
import type {
  ProviderSummary,
  ProviderSearchFilters,
  ProviderType,
  ProviderStatus,
} from '../types/provider.types'

/**
 * Composable for provider-related functionality
 * Provides convenient access to provider store and common operations
 */
export function useProvider() {
  const router = useRouter()
  const providerStore = useProviderStore()

  // ============================================
  // State (from store)
  // ============================================

  const providers = computed(() => providerStore.providers)
  const currentProvider = computed(() => providerStore.currentProvider)
  const isLoading = computed(() => providerStore.isLoading)
  const isSearching = computed(() => providerStore.isSearching)
  const error = computed(() => providerStore.error)
  const searchResults = computed(() => providerStore.searchResults)
  const statistics = computed(() => providerStore.statistics)

  // ============================================
  // Navigation Helpers
  // ============================================

  const navigateToProvider = (providerId: string) => {
    router.push({ name: 'ProviderDetails', params: { id: providerId } })
  }

  const navigateToBooking = (providerId: string, serviceId?: string) => {
    router.push({
      name: 'NewBooking',
      query: {
        providerId,
        ...(serviceId && { serviceId }),
      },
    })
  }

  const navigateToProviderList = () => {
    router.push({ name: 'ProviderList' })
  }

  const navigateToProviderSearch = () => {
    router.push({ name: 'ProviderSearch' })
  }

  // ============================================
  // Search & Filter Operations
  // ============================================

  const searchProviders = async (filters: ProviderSearchFilters) => {
    return providerStore.searchProviders(filters)
  }

  const searchByLocation = async (latitude: number, longitude: number, radiusKm: number = 10) => {
    return providerStore.searchByLocation(latitude, longitude, radiusKm)
  }

  const applyFilters = async (filters: ProviderSearchFilters) => {
    return providerStore.applyFilters(filters)
  }

  const clearFilters = async () => {
    return providerStore.clearFilters()
  }

  // ============================================
  // Provider Data Operations
  // ============================================

  const getProviderById = async (
    providerId: string,
    includeServices = true,
    includeStaff = true,
  ) => {
    return providerStore.getProviderById(providerId, includeServices, includeStaff)
  }

  const loadFeaturedProviders = async (categoryFilter?: string, limit = 10) => {
    return providerStore.loadFeaturedProviders(categoryFilter, limit)
  }

  const loadStatistics = async (providerId: string) => {
    return providerStore.loadStatistics(providerId)
  }

  // ============================================
  // Pagination
  // ============================================

  const nextPage = async () => {
    return providerStore.loadNextPage()
  }

  const previousPage = async () => {
    return providerStore.loadPreviousPage()
  }

  const goToPage = async (page: number) => {
    return providerStore.goToPage(page)
  }

  // ============================================
  // Utility Functions
  // ============================================

  const getProviderStatusColor = (status: ProviderStatus): string => {
    const colors: Record<ProviderStatus, string> = {
      Active: '#10b981',
      Pending: '#f59e0b',
      Inactive: '#6b7280',
      Suspended: '#ef4444',
      Deactivated: '#9ca3af',
    }
    return colors[status] || '#6b7280'
  }

  const getProviderTypeIcon = (type: ProviderType): string => {
    const icons: Record<ProviderType, string> = {
      Individual: '👤',
      Salon: '💇',
      Clinic: '🏥',
      Spa: '🧖',
      Studio: '🎨',
      Professional: '💼',
    }
    return icons[type] || '📍'
  }

  const formatProviderLocation = (provider: ProviderSummary): string => {
    return `${provider.city}, ${provider.state}`
  }

  const isProviderBookable = (provider: ProviderSummary): boolean => {
    return provider.status === 'Active' && provider.allowOnlineBooking
  }

  const getProviderInitials = (businessName: string): string => {
    return businessName
      .split(' ')
      .map((word) => word[0])
      .join('')
      .toUpperCase()
      .slice(0, 2)
  }

  // ============================================
  // Geolocation Helper
  // ============================================

  const searchNearby = async (radiusKm: number = 10): Promise<void> => {
    return new Promise((resolve, reject) => {
      if (!navigator.geolocation) {
        reject(new Error('Geolocation is not supported by your browser'))
        return
      }

      navigator.geolocation.getCurrentPosition(
        async (position) => {
          try {
            await searchByLocation(position.coords.latitude, position.coords.longitude, radiusKm)
            resolve()
          } catch (error) {
            reject(error)
          }
        },
        (error) => {
          console.error(error)
          reject(new Error('Unable to retrieve your location'))
        },
      )
    })
  }

  // ============================================
  // State Management
  // ============================================

  const clearCurrentProvider = () => {
    providerStore.clearCurrentProvider()
  }

  const clearError = () => {
    providerStore.clearError()
  }

  const reset = () => {
    providerStore.reset()
  }

  // ============================================
  // Computed Helpers
  // ============================================

  const hasProviders = computed(() => providerStore.hasProviders)
  const totalProviders = computed(() => providerStore.totalProviders)
  const totalPages = computed(() => providerStore.totalPages)
  const currentPage = computed(() => providerStore.currentPage)
  const hasPreviousPage = computed(() => providerStore.hasPreviousPage)
  const hasNextPage = computed(() => providerStore.hasNextPage)
  const isFirstPage = computed(() => providerStore.isFirstPage)
  const isLastPage = computed(() => providerStore.isLastPage)
  const activeFiltersCount = computed(() => providerStore.activeFiltersCount)

  return {
    // State
    providers,
    currentProvider,
    isLoading,
    isSearching,
    error,
    searchResults,
    statistics,

    // Computed
    hasProviders,
    totalProviders,
    totalPages,
    currentPage,
    hasPreviousPage,
    hasNextPage,
    isFirstPage,
    isLastPage,
    activeFiltersCount,

    // Navigation
    navigateToProvider,
    navigateToBooking,
    navigateToProviderList,
    navigateToProviderSearch,

    // Search & Filter
    searchProviders,
    searchByLocation,
    searchNearby,
    applyFilters,
    clearFilters,

    // Data Operations
    getProviderById,
    loadFeaturedProviders,
    loadStatistics,

    // Pagination
    nextPage,
    previousPage,
    goToPage,

    // Utilities
    getProviderStatusColor,
    getProviderTypeIcon,
    formatProviderLocation,
    isProviderBookable,
    getProviderInitials,

    // State Management
    clearCurrentProvider,
    clearError,
    reset,
  }
}

/**
 * Composable for provider formatting utilities
 */
export function useProviderFormatters() {
  const formatPrice = (amount: number, currency: string = 'USD'): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(amount)
  }

  const formatDuration = (minutes: number): string => {
    if (minutes < 60) {
      return `${minutes} min`
    }
    const hours = Math.floor(minutes / 60)
    const mins = minutes % 60
    return mins > 0 ? `${hours}h ${mins}min` : `${hours}h`
  }

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString)
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    }).format(date)
  }

  const formatRelativeTime = (dateString: string): string => {
    const date = new Date(dateString)
    const now = new Date()
    const diffTime = Math.abs(now.getTime() - date.getTime())
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))

    if (diffDays < 1) return 'Today'
    if (diffDays === 1) return 'Yesterday'
    if (diffDays < 7) return `${diffDays} days ago`
    if (diffDays < 30) {
      const weeks = Math.floor(diffDays / 7)
      return `${weeks} week${weeks > 1 ? 's' : ''} ago`
    }
    if (diffDays < 365) {
      const months = Math.floor(diffDays / 30)
      return `${months} month${months > 1 ? 's' : ''} ago`
    }
    const years = Math.floor(diffDays / 365)
    return `${years} year${years > 1 ? 's' : ''} ago`
  }

  const formatPhoneNumber = (phone: string): string => {
    // Basic US phone number formatting
    const cleaned = phone.replace(/\D/g, '')
    const match = cleaned.match(/^(\d{3})(\d{3})(\d{4})$/)
    if (match) {
      return `(${match[1]}) ${match[2]}-${match[3]}`
    }
    return phone
  }

  const truncateText = (text: string, maxLength: number): string => {
    if (text.length <= maxLength) return text
    return text.slice(0, maxLength).trim() + '...'
  }

  const formatAddress = (address: {
    addressLine1: string
    addressLine2?: string
    city: string
    state: string
    postalCode: string
    country: string
  }): string => {
    const parts = [
      address.addressLine1,
      address.addressLine2,
      `${address.city}, ${address.state} ${address.postalCode}`,
      address.country,
    ].filter(Boolean)

    return parts.join('\n')
  }

  const formatCompactAddress = (city: string, state: string, country?: string): string => {
    if (country && country !== 'USA') {
      return `${city}, ${state}, ${country}`
    }
    return `${city}, ${state}`
  }

  return {
    formatPrice,
    formatDuration,
    formatDate,
    formatRelativeTime,
    formatPhoneNumber,
    truncateText,
    formatAddress,
    formatCompactAddress,
  }
}
