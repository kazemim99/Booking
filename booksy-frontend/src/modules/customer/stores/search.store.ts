// Customer search store - manages provider and service search state

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { providerSearchService } from '../services/provider-search.service'
import type {
  ProviderSearchFilters,
  ProviderSearchResult,
  PagedSearchResult,
} from '../types/search.types'

export const useSearchStore = defineStore('customer-search', () => {
  // ============================================
  // State
  // ============================================

  // Search results
  const searchResults = ref<PagedSearchResult<ProviderSearchResult> | null>(null)
  const providers = ref<ProviderSearchResult[]>([])
  const featuredProviders = ref<ProviderSearchResult[]>([])

  // Current search filters
  const currentFilters = ref<ProviderSearchFilters>({
    pageNumber: 1,
    pageSize: 20,
  })

  // UI state
  const isLoading = ref(false)
  const isSearching = ref(false)
  const error = ref<string | null>(null)

  // View mode
  const viewMode = ref<'grid' | 'list' | 'map'>('grid')

  // Recent searches (stored in localStorage)
  const recentSearches = ref<string[]>([])

  // ============================================
  // Getters (Computed)
  // ============================================

  const hasResults = computed(() => providers.value.length > 0)

  const totalResults = computed(() => searchResults.value?.totalCount ?? 0)

  const currentPage = computed(() => searchResults.value?.pageNumber ?? 1)

  const totalPages = computed(() => searchResults.value?.totalPages ?? 0)

  const hasNextPage = computed(() => searchResults.value?.hasNextPage ?? false)

  const hasPreviousPage = computed(() => searchResults.value?.hasPreviousPage ?? false)

  const activeFiltersCount = computed(() => {
    let count = 0
    const filters = currentFilters.value

    if (filters.searchTerm) count++
    if (filters.category) count++
    if (filters.location) count++
    if (filters.minRating) count++
    if (filters.priceRange) count++
    if (filters.openNow) count++

    return count
  })

  const isFiltered = computed(() => activeFiltersCount.value > 0)

  // ============================================
  // Actions - Search
  // ============================================

  /**
   * Search providers with filters
   */
  async function searchProviders(filters?: Partial<ProviderSearchFilters>): Promise<void> {
    isSearching.value = true
    error.value = null

    try {
      // Merge with current filters
      const searchFilters: ProviderSearchFilters = {
        ...currentFilters.value,
        ...filters,
      }

      // Perform search
      const result = await providerSearchService.searchProviders(searchFilters)

      // Update state
      searchResults.value = result
      providers.value = result.items
      currentFilters.value = searchFilters

      // Save search term to recent searches
      if (searchFilters.searchTerm) {
        addRecentSearch(searchFilters.searchTerm)
      }
    } catch (err: any) {
      error.value = err.message || 'خطا در جستجوی ارائه‌دهندگان'
      console.error('[SearchStore] Search error:', err)
      providers.value = []
    } finally {
      isSearching.value = false
    }
  }

  /**
   * Search by location (GPS coordinates)
   */
  async function searchByLocation(
    latitude: number,
    longitude: number,
    radiusKm: number = 5,
  ): Promise<void> {
    isSearching.value = true
    error.value = null

    try {
      const results = await providerSearchService.searchByLocation(latitude, longitude, radiusKm, {
        ...currentFilters.value,
      })

      providers.value = results

      // Update filters to reflect location search
      currentFilters.value = {
        ...currentFilters.value,
        latitude,
        longitude,
        radiusKm,
      }
    } catch (err: any) {
      error.value = err.message || 'خطا در جستجوی ارائه‌دهندگان'
      console.error('[SearchStore] Location search error:', err)
      providers.value = []
    } finally {
      isSearching.value = false
    }
  }

  /**
   * Load featured/popular providers
   */
  async function loadFeaturedProviders(limit: number = 10): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      const results = await providerSearchService.getFeaturedProviders(limit)
      featuredProviders.value = results
    } catch (err: any) {
      error.value = err.message || 'خطا در بارگذاری ارائه‌دهندگان محبوب'
      console.error('[SearchStore] Load featured error:', err)
    } finally {
      isLoading.value = false
    }
  }

  // ============================================
  // Actions - Pagination
  // ============================================

  /**
   * Go to next page
   */
  async function nextPage(): Promise<void> {
    if (!hasNextPage.value) return

    await searchProviders({
      ...currentFilters.value,
      pageNumber: currentPage.value + 1,
    })
  }

  /**
   * Go to previous page
   */
  async function previousPage(): Promise<void> {
    if (!hasPreviousPage.value) return

    await searchProviders({
      ...currentFilters.value,
      pageNumber: currentPage.value - 1,
    })
  }

  /**
   * Go to specific page
   */
  async function goToPage(page: number): Promise<void> {
    if (page < 1 || page > totalPages.value) return

    await searchProviders({
      ...currentFilters.value,
      pageNumber: page,
    })
  }

  // ============================================
  // Actions - Filters
  // ============================================

  /**
   * Apply filters and search
   */
  async function applyFilters(filters: Partial<ProviderSearchFilters>): Promise<void> {
    await searchProviders({
      ...filters,
      pageNumber: 1, // Reset to first page when applying filters
    })
  }

  /**
   * Update a single filter
   */
  async function updateFilter(
    key: keyof ProviderSearchFilters,
    value: any,
  ): Promise<void> {
    await applyFilters({
      ...currentFilters.value,
      [key]: value,
    })
  }

  /**
   * Clear all filters and search
   */
  async function clearFilters(): Promise<void> {
    currentFilters.value = {
      pageNumber: 1,
      pageSize: 20,
    }

    await searchProviders()
  }

  /**
   * Clear specific filter
   */
  async function removeFilter(key: keyof ProviderSearchFilters): Promise<void> {
    const newFilters = { ...currentFilters.value }
    delete newFilters[key]

    await applyFilters(newFilters)
  }

  // ============================================
  // Actions - View Mode
  // ============================================

  /**
   * Change view mode (grid/list/map)
   */
  function setViewMode(mode: 'grid' | 'list' | 'map'): void {
    viewMode.value = mode

    // Save to localStorage
    localStorage.setItem('provider-view-mode', mode)
  }

  // ============================================
  // Actions - Recent Searches
  // ============================================

  /**
   * Add search term to recent searches
   */
  function addRecentSearch(searchTerm: string): void {
    const term = searchTerm.trim()
    if (!term) return

    // Remove if already exists
    const filtered = recentSearches.value.filter((t) => t !== term)

    // Add to beginning
    recentSearches.value = [term, ...filtered].slice(0, 10) // Keep max 10

    // Save to localStorage
    localStorage.setItem('recent-searches', JSON.stringify(recentSearches.value))
  }

  /**
   * Load recent searches from localStorage
   */
  function loadRecentSearches(): void {
    const stored = localStorage.getItem('recent-searches')
    if (stored) {
      try {
        recentSearches.value = JSON.parse(stored)
      } catch (err) {
        console.error('[SearchStore] Error loading recent searches:', err)
        recentSearches.value = []
      }
    }
  }

  /**
   * Clear recent searches
   */
  function clearRecentSearches(): void {
    recentSearches.value = []
    localStorage.removeItem('recent-searches')
  }

  // ============================================
  // Actions - Reset
  // ============================================

  /**
   * Reset store to initial state
   */
  function reset(): void {
    searchResults.value = null
    providers.value = []
    currentFilters.value = {
      pageNumber: 1,
      pageSize: 20,
    }
    error.value = null
    isLoading.value = false
    isSearching.value = false
  }

  // ============================================
  // Initialization
  // ============================================

  // Load saved view mode
  const savedViewMode = localStorage.getItem('provider-view-mode')
  if (savedViewMode === 'grid' || savedViewMode === 'list' || savedViewMode === 'map') {
    viewMode.value = savedViewMode
  }

  // Load recent searches
  loadRecentSearches()

  // ============================================
  // Return Public API
  // ============================================

  return {
    // State
    searchResults,
    providers,
    featuredProviders,
    currentFilters,
    isLoading,
    isSearching,
    error,
    viewMode,
    recentSearches,

    // Getters
    hasResults,
    totalResults,
    currentPage,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    activeFiltersCount,
    isFiltered,

    // Actions
    searchProviders,
    searchByLocation,
    loadFeaturedProviders,
    nextPage,
    previousPage,
    goToPage,
    applyFilters,
    updateFilter,
    clearFilters,
    removeFilter,
    setViewMode,
    addRecentSearch,
    loadRecentSearches,
    clearRecentSearches,
    reset,
  }
})
