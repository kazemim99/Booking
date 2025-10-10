// src/modules/provider/stores/provider.store.ts

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { providerService } from '../services/provider.service'
import type {
  Provider,
  ProviderSummary,
  ProviderSearchFilters,
  RegisterProviderRequest,
  UpdateProviderRequest,
  DeactivateProviderRequest,
  ProviderStatistics,
  ProviderStatus,
} from '../types/provider.types'
import type { PagedResult } from '@/core/types/common.types'
import { useAuthStore } from '@/core/stores/modules/auth.store'

export const useProviderStore = defineStore('provider', () => {
  // ============================================
  // State
  // ============================================

  const providers = ref<ProviderSummary[]>([])
  const currentProvider = ref<Provider | null>(null)
  const featuredProviders = ref<ProviderSummary[]>([])

  // Search & Pagination
  const searchResults = ref<PagedResult<ProviderSummary> | null>(null)
  const currentFilters = ref<ProviderSearchFilters>({
    pageNumber: 1,
    pageSize: 12,
  })
  const currentPage = ref(1)
  const pageSize = ref(12)

  // Statistics
  const statistics = ref<ProviderStatistics | null>(null)

  // UI State
  const isLoading = ref(false)
  const isSearching = ref(false)
  const error = ref<string | null>(null)
  const viewMode = ref<'grid' | 'list' | 'map'>('grid')

  // ============================================
  // Getters (Computed)
  // ============================================

  const hasProviders = computed(() => providers.value.length > 0)

  const totalProviders = computed(() => searchResults.value?.totalCount ?? 0)

  const totalPages = computed(() => searchResults.value?.totalPages ?? 0)

  const hasPreviousPage = computed(() => searchResults.value?.hasPreviousPage ?? false)

  const hasNextPage = computed(() => searchResults.value?.hasNextPage ?? false)

  const isFirstPage = computed(() => currentPage.value === 1)

  const isLastPage = computed(() => currentPage.value === totalPages.value)

  const activeFiltersCount = computed(() => {
    return Object.values(currentFilters.value).filter(
      (v) => v !== undefined && v !== null && v !== '',
    ).length
  })

  const providersByStatus = computed(() => {
    return (status: ProviderStatus) => providers.value.filter((p) => p.status === status)
  })

  // ============================================
  // Actions - Search & Browse
  // ============================================

  /**
   * Search providers with filters and pagination
   */
  async function searchProviders(filters?: ProviderSearchFilters): Promise<void> {
    isSearching.value = true
    error.value = null

    try {
      const searchParams: ProviderSearchFilters = {
        ...filters,
        pageNumber:
          filters?.pageNumber !== undefined && filters?.pageNumber !== null
            ? filters.pageNumber
            : currentPage.value,
        pageSize:
          filters?.pageSize !== undefined && filters?.pageSize !== null
            ? filters.pageSize
            : pageSize.value,
      }

      searchResults.value = await providerService.searchProviders(searchParams)
      providers.value = searchResults.value.items
      currentFilters.value = filters || ({} as ProviderSearchFilters)
      currentPage.value = filters?.pageNumber ?? currentPage.value
      pageSize.value = filters?.pageSize ?? pageSize.value
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to search providers'
      } else {
        error.value = 'Failed to search providers'
      }
      console.error('Search providers error:', err)
      throw err
    } finally {
      isSearching.value = false
    }
  }

  /**
   * Load next page
   */
  async function loadNextPage(): Promise<void> {
    if (!hasNextPage.value) return
    await searchProviders({
      ...currentFilters.value,
      pageNumber: currentPage.value + 1,
      pageSize: pageSize.value,
    })
  }
  async function loadPreviousPage(): Promise<void> {
    if (!hasPreviousPage.value) return
    await searchProviders({
      ...currentFilters.value,
      pageNumber: currentPage.value - 1,
      pageSize: pageSize.value,
    })
  }
  async function goToPage(page: number): Promise<void> {
    if (page < 1 || page > totalPages.value) return
    await searchProviders({
      ...currentFilters.value,
      pageNumber: page,
      pageSize: pageSize.value,
    })
  }
  async function applyFilters(filters: ProviderSearchFilters): Promise<void> {
    await searchProviders({
      ...filters,
      pageNumber: 1,
      pageSize: pageSize.value,
    })
  }
  async function clearFilters(): Promise<void> {
    currentFilters.value = {} as ProviderSearchFilters
    await searchProviders({
      pageNumber: 1,
      pageSize: pageSize.value,
    })
  }

  async function loadFeaturedProviders(categoryFilter?: string, limit = 10): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      featuredProviders.value = await providerService.getFeaturedProviders(categoryFilter, limit)
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to load featured providers'
      } else {
        error.value = 'Failed to load featured providers'
      }
      console.error('Load featured providers error:', err)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Search by location
   */
  async function searchByLocation(
    latitude: number,
    longitude: number,
    radiusKm: number,
  ): Promise<void> {
    isSearching.value = true
    error.value = null

    try {
      const results = await providerService.getProvidersByLocation(latitude, longitude, radiusKm)
      providers.value = results
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to load featured location'
      } else {
        error.value = 'Failed to load featured location'
      }
      console.error('Load featured location error:', err)
    } finally {
      isLoading.value = false
    }
  }

  // ============================================
  // Actions - Provider Details
  // ============================================

  /**
   * Get provider by ID
   */
  async function getProviderById(
    id: string,
    includeServices = true,
    includeStaff = true,
  ): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      currentProvider.value = await providerService.getProviderById(
        id,
        includeServices,
        includeStaff,
      )
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to deactivate provider'
      } else {
        error.value = 'Failed to deactivate provider'
      }
      console.error('Deactivate provider error:', err)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Load provider statistics
   */
  async function loadStatistics(id: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      statistics.value = await providerService.getProviderStatistics(id)
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to load featured provider statistics'
      } else {
        error.value = 'Failed to load featured provider statistics'
      }
      console.error('Load featured provider statistics error:', err)
    } finally {
      isLoading.value = false
    }
  }

  // ============================================
  // Actions - Provider Management
  // ============================================

  async function registerProvider(data: RegisterProviderRequest): Promise<Provider | undefined> {

    isLoading.value = true
    error.value = null

    try {
      const newProvider = await providerService.registerProvider(data)
      currentProvider.value = newProvider

      // ✅ Store provider ID in localStorage for future retrieval
      if (newProvider?.id) {
        localStorage.setItem('provider_id', newProvider.id)
        console.log('[ProviderStore] Provider registered and ID stored:', newProvider.id)
      }

      return newProvider
    } catch (err: unknown) {
      // ✅ Add proper error handling
      if (err instanceof Error) {
        error.value = err.message || 'Failed to register provider'
      } else {
        error.value = 'Failed to register provider'
      }

      console.error('Register provider error:', err)
      // ✅ Return undefined to indicate failure
      return undefined
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Update provider
   */
  async function updateProvider(
    id: string,
    data: UpdateProviderRequest,
  ): Promise<Provider | undefined> {
    isLoading.value = true
    error.value = null

    try {
      const updated = await providerService.updateProvider(id, data)
      currentProvider.value = updated

      // Update in list if exists
      const index = providers.value.findIndex((p) => p.id === id)
      if (index !== -1) {
        providers.value[index] = {
          ...providers.value[index],
          businessName: updated.profile.businessName,
          description: updated.profile.description,
          logoUrl: updated.profile.logoUrl,
          city: updated.address.city,
          state: updated.address.state,
        }
      }

      return updated
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to update provider'
      } else {
        error.value = 'Failed to update provider'
      }
      console.error('Update provider error:', err)
      return undefined
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Load current logged-in provider's data
   */
  async function loadCurrentProvider(): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      // Get current user from auth store
      const authStore = useAuthStore()
      const currentUserId = authStore.user?.id
      const userEmail = authStore.user?.email

      console.log('[ProviderStore] Loading provider for user:', currentUserId, userEmail)

      if (!currentUserId) {
        throw new Error('No authenticated user found')
      }

      // Get provider ID from localStorage (stored during registration)
      let providerId = localStorage.getItem('provider_id')

      // Fallback: If no provider ID in localStorage, try to search for provider by email
      // This handles the case where provider was created externally (e.g., by UserFactory)
      if (!providerId && userEmail) {
        console.log('[ProviderStore] No provider ID in storage. Attempting to find provider by email:', userEmail)

        try {
          // Search for providers with the user's email
          const searchResults = await providerService.searchProviders({
            searchTerm: userEmail,
            pageNumber: 1,
            pageSize: 1
          })

          if (searchResults.items && searchResults.items.length > 0) {
            const foundProvider = searchResults.items[0]
            providerId = foundProvider.id
            // Store the found provider ID for future use
            localStorage.setItem('provider_id', providerId)
            console.log('[ProviderStore] Found provider via search:', providerId)
          } else {
            console.warn('[ProviderStore] No provider found via email search')
          }
        } catch (searchError) {
          console.error('[ProviderStore] Error searching for provider:', searchError)
        }
      }

      if (!providerId) {
        console.warn('[ProviderStore] No provider ID found. User may need to register as provider.')
        currentProvider.value = null
        error.value = null
        return
      }

      console.log('[ProviderStore] Fetching provider by ID:', providerId)

      // Get provider by ID
      const provider = await providerService.getProviderById(providerId, true, true)

      if (provider) {
        console.log('[ProviderStore] Provider loaded successfully:', provider.id)
        currentProvider.value = provider
        // Ensure provider ID is stored
        localStorage.setItem('provider_id', provider.id)
      } else {
        // Provider not found - might have been deleted
        console.warn('[ProviderStore] Provider not found with ID:', providerId)
        currentProvider.value = null
        // Clear the invalid provider ID from storage
        localStorage.removeItem('provider_id')
        error.value = null
      }
    } catch (err: unknown) {
      console.error('[ProviderStore] Load current provider error:', err)

      // Don't set error for network issues - just log it
      if (err instanceof Error) {
        console.error('[ProviderStore] Error details:', err.message)
      }

      // Set currentProvider to null but don't set error
      // This allows the onboarding flow to work even if there's a temporary network issue
      currentProvider.value = null
    } finally {
      isLoading.value = false
    }
  }
  /**
   * Activate provider
   */
  async function activateProvider(id: string, notes?: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      await providerService.activateProvider(id, { notes })

      // Refresh current provider if it's the same
      if (currentProvider.value?.id === id) {
        await getProviderById(id)
      }
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to active provider'
      } else {
        error.value = 'Failed to active provider'
      }
      console.error(' active provider:', err)
    } finally {
      isLoading.value = false
    }
  }
  /**
   * Deactivate provider
   */
  async function deactivateProvider(id: string, data: DeactivateProviderRequest): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      await providerService.deactivateProvider(id, data)

      // Refresh current provider if it's the same
      if (currentProvider.value?.id === id) {
        await getProviderById(id)
      }
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to deactivate provider'
      } else {
        error.value = 'Failed to deactivate provider'
      }
      console.error('Deactivate provider error:', err)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Suspend provider
   */
  async function suspendProvider(id: string, reason: string, notes?: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      await providerService.suspendProvider(id, reason, notes)

      if (currentProvider.value?.id === id) {
        await getProviderById(id)
      }
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to deactivate provider'
      } else {
        error.value = 'Failed to deactivate provider'
      }
      console.error('Deactivate provider error:', err)
    } finally {
      isLoading.value = false
    }
  }
  // ============================================
  // Actions - Admin
  // ============================================

  /**
   * Get providers by status (admin)
   */
  async function getProvidersByStatus(status: ProviderStatus, maxResults = 100): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      providers.value = await providerService.getProvidersByStatus(status, maxResults)
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to deactivate provider'
      } else {
        error.value = 'Failed to deactivate provider'
      }
      console.error('Deactivate provider error:', err)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Verify provider (admin)
   */
  async function verifyProvider(id: string, notes?: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      await providerService.verifyProvider(id, notes)

      if (currentProvider.value?.id === id) {
        await getProviderById(id)
      }
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to deactivate provider'
      } else {
        error.value = 'Failed to deactivate provider'
      }
      console.error('Deactivate provider error:', err)
    } finally {
      isLoading.value = false
    }
  }
  // ============================================
  // Actions - UI State
  // ============================================

  function setViewMode(mode: 'grid' | 'list' | 'map'): void {
    viewMode.value = mode
    localStorage.setItem('provider-view-mode', mode)
  }

  function clearCurrentProvider(): void {
    currentProvider.value = null
    statistics.value = null
  }

  function clearError(): void {
    error.value = null
  }

  function reset(): void {
    providers.value = []
    currentProvider.value = null
    featuredProviders.value = []
    searchResults.value = null
    currentFilters.value = {} as ProviderSearchFilters
    currentPage.value = 1
    statistics.value = null
    error.value = null
  }

  // Initialize view mode from localStorage
  const savedViewMode = localStorage.getItem('provider-view-mode') as 'grid' | 'list' | 'map'
  if (savedViewMode) {
    viewMode.value = savedViewMode
  }

  // ============================================
  // Return Store API
  // ============================================

  return {
    // State
    providers,
    currentProvider,
    featuredProviders,
    searchResults,
    currentFilters,
    currentPage,
    pageSize,
    statistics,
    isLoading,
    isSearching,
    error,
    viewMode,

    // Getters
    hasProviders,
    totalProviders,
    totalPages,
    hasPreviousPage,
    hasNextPage,
    isFirstPage,
    isLastPage,
    activeFiltersCount,
    providersByStatus,

    // Actions
    loadCurrentProvider,
    searchProviders,
    loadNextPage,
    loadPreviousPage,
    goToPage,
    applyFilters,
    clearFilters,
    loadFeaturedProviders,
    searchByLocation,
    getProviderById,
    loadStatistics,
    registerProvider,
    updateProvider,
    activateProvider,
    deactivateProvider,
    suspendProvider,
    getProvidersByStatus,
    verifyProvider,
    setViewMode,
    clearCurrentProvider,
    clearError,
    reset,
  }
})
