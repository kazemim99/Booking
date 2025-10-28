// src/modules/provider/stores/service.store.ts

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { serviceService } from '../services/service.service'
import type {
  Service,
  ServiceSummary,
  CreateServiceRequest,
  UpdateServiceRequest,
  ServiceFilters,
  ServiceStatus,
  ServiceCategory,
} from '../types/service.types'
import { useProviderStore } from './provider.store'

export const useServiceStore = defineStore('service', () => {
  // ============================================
  // State
  // ============================================

  const services = ref<Service[]>([])
  const currentService = ref<Service | null>(null)
  const filteredServices = ref<Service[]>([])

  // Filters
  const currentFilters = ref<ServiceFilters>({})
  const searchTerm = ref('')
  const selectedCategory = ref<ServiceCategory | undefined>(undefined)
  const selectedStatus = ref<ServiceStatus | undefined>(undefined)

  // UI State
  const isLoading = ref(false)
  const isSaving = ref(false)
  const error = ref<string | null>(null)
  const successMessage = ref<string | null>(null)

  // ============================================
  // Getters (Computed)
  // ============================================

  const hasServices = computed(() => services.value.length > 0)

  const activeServices = computed(() =>
    services.value.filter((s) => s.status === 'Active')
  )

  const draftServices = computed(() =>
    services.value.filter((s) => s.status === 'Draft')
  )

  const inactiveServices = computed(() =>
    services.value.filter((s) => s.status === 'Inactive')
  )

  const servicesByCategory = computed(() => {
    return (category: ServiceCategory) =>
      services.value.filter((s) => s.category === category)
  })

  const serviceById = computed(() => {
    return (id: string) => services.value.find((s) => s.id === id)
  })

  const hasFilteredServices = computed(() => filteredServices.value.length > 0)

  const hasActiveFilters = computed(() => {
    return (
      !!searchTerm.value ||
      !!selectedCategory.value ||
      !!selectedStatus.value ||
      !!currentFilters.value.minPrice ||
      !!currentFilters.value.maxPrice
    )
  })

  const totalServices = computed(() => services.value.length)

  // ============================================
  // Actions - CRUD Operations
  // ============================================

  /**
   * Load all services for a provider
   * Also refreshes the currentProvider in provider store to reflect updated service list
   */
  async function loadServices(providerId: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      services.value = await serviceService.getServicesByProvider(providerId)
      applyFilters()

      // Refresh currentProvider to update the provider's service list
      const providerStore = useProviderStore()
      if (providerStore.currentProvider?.id === providerId) {
        console.log('[ServiceStore] Refreshing currentProvider after loading services')
        await providerStore.loadCurrentProvider(true) // Force refresh
      }
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to load services'
      } else {
        error.value = 'Failed to load services'
      }
      console.error('Load services error:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Load a single service by ID
   */
  async function loadService(id: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      currentService.value = await serviceService.getServiceById(id)
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to load service'
      } else {
        error.value = 'Failed to load service'
      }
      console.error('Load service error:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Create a new service
   * Also refreshes the currentProvider to update the provider's service list
   */
  async function createService(data: CreateServiceRequest): Promise<Service> {
    isSaving.value = true
    error.value = null
    successMessage.value = null

    try {
      const newService = await serviceService.createService(data)

      console.log('[ServiceStore] Service created, received:', newService)

      if (!newService || !newService.id) {
        throw new Error('Invalid service data returned from server')
      }

      // Optimistically add to local state
      services.value.push(newService)
      applyFilters()

      // Refresh currentProvider to update the provider's service list
      const providerStore = useProviderStore()
      if (providerStore.currentProvider?.id === data.providerId) {
        console.log('[ServiceStore] Refreshing currentProvider after creating service')
        providerStore.loadCurrentProvider(true).catch(err => {
          console.error('[ServiceStore] Failed to refresh provider after service creation:', err)
        })
      }

      successMessage.value = 'Service created successfully'
      return newService
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to create service'
      } else {
        error.value = 'Failed to create service'
      }
      console.error('Create service error:', err)
      throw err
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Update an existing service (with optimistic update)
   */
  async function updateService(
    id: string,
    data: UpdateServiceRequest
  ): Promise<Service> {
    isSaving.value = true
    error.value = null
    successMessage.value = null

    // Store original for rollback
    const originalIndex = services.value.findIndex((s) => s.id === id)
    const original = originalIndex >= 0 ? { ...services.value[originalIndex] } : null

    try {
      // Optimistically update UI
      if (originalIndex >= 0) {
        services.value[originalIndex] = {
          ...services.value[originalIndex],
          ...data,
        }
        applyFilters()
      }

      // Make API call
      const updatedService = await serviceService.updateService(id, data)

      console.log('[ServiceStore] Service updated, received:', updatedService)

      if (!updatedService || !updatedService.id) {
        throw new Error('Invalid service data returned from server')
      }

      // Replace with server response
      if (originalIndex >= 0) {
        services.value[originalIndex] = updatedService
        applyFilters()
      }

      // Update current service if it's the one being edited
      if (currentService.value?.id === id) {
        currentService.value = updatedService
      }

      successMessage.value = 'Service updated successfully'
      return updatedService
    } catch (err: unknown) {
      // Rollback on failure
      if (original && originalIndex >= 0) {
        services.value[originalIndex] = original
        applyFilters()
      }

      if (err instanceof Error) {
        error.value = err.message || 'Failed to update service'
      } else {
        error.value = 'Failed to update service'
      }
      console.error('Update service error:', err)
      throw err
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Delete a service
   */
  async function deleteService(id: string): Promise<void> {
    isSaving.value = true
    error.value = null
    successMessage.value = null

    // Store original for rollback
    const originalIndex = services.value.findIndex((s) => s.id === id)
    const original = originalIndex >= 0 ? { ...services.value[originalIndex] } : null

    if (!original) {
      throw new Error('Service not found')
    }

    try {
      // Optimistically remove from UI
      if (originalIndex >= 0) {
        services.value.splice(originalIndex, 1)
        applyFilters()
      }

      // Make API call with providerId
      await serviceService.deleteService(id, original.providerId)

      // Clear current service if it's the one being deleted
      if (currentService.value?.id === id) {
        currentService.value = null
      }

      // Refresh currentProvider to update the provider's service list
      const providerStore = useProviderStore()
      const deletedService = original
      if (deletedService && providerStore.currentProvider?.id === deletedService.providerId) {
        console.log('[ServiceStore] Refreshing currentProvider after deleting service')
        providerStore.loadCurrentProvider(true).catch(err => {
          console.error('[ServiceStore] Failed to refresh provider after service deletion:', err)
        })
      }

      successMessage.value = 'Service deleted successfully'
    } catch (err: unknown) {
      // Rollback on failure
      if (original && originalIndex >= 0) {
        services.value.splice(originalIndex, 0, original)
        applyFilters()
      }

      if (err instanceof Error) {
        error.value = err.message || 'Failed to delete service'
      } else {
        error.value = 'Failed to delete service'
      }
      console.error('Delete service error:', err)
      throw err
    } finally {
      isSaving.value = false
    }
  }

  // ============================================
  // Actions - Status Management
  // ============================================

  /**
   * Activate a service
   */
  async function activateService(id: string): Promise<void> {
    try {
      const updated = await serviceService.activateService(id)
      const index = services.value.findIndex((s) => s.id === id)
      if (index >= 0) {
        services.value[index] = updated
        applyFilters()
      }
      successMessage.value = 'Service activated successfully'
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to activate service'
      } else {
        error.value = 'Failed to activate service'
      }
      throw err
    }
  }

  /**
   * Deactivate a service
   */
  async function deactivateService(id: string): Promise<void> {
    try {
      const updated = await serviceService.deactivateService(id)
      const index = services.value.findIndex((s) => s.id === id)
      if (index >= 0) {
        services.value[index] = updated
        applyFilters()
      }
      successMessage.value = 'Service deactivated successfully'
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to deactivate service'
      } else {
        error.value = 'Failed to deactivate service'
      }
      throw err
    }
  }

  /**
   * Archive a service
   */
  async function archiveService(id: string): Promise<void> {
    try {
      const updated = await serviceService.archiveService(id)
      const index = services.value.findIndex((s) => s.id === id)
      if (index >= 0) {
        services.value[index] = updated
        applyFilters()
      }
      successMessage.value = 'Service archived successfully'
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to archive service'
      } else {
        error.value = 'Failed to archive service'
      }
      throw err
    }
  }

  // ============================================
  // Actions - Filtering & Search
  // ============================================

  /**
   * Apply current filters to services
   */
  function applyFilters(): void {
    let filtered = [...services.value]

    // Search term
    if (searchTerm.value) {
      const term = searchTerm.value.toLowerCase()
      filtered = filtered.filter(
        (s) =>
          s.name.toLowerCase().includes(term) ||
          s.description.toLowerCase().includes(term) ||
          s.tags.some((tag) => tag.toLowerCase().includes(term))
      )
    }

    // Category filter
    if (selectedCategory.value) {
      filtered = filtered.filter((s) => s.category === selectedCategory.value)
    }

    // Status filter
    if (selectedStatus.value) {
      filtered = filtered.filter((s) => s.status === selectedStatus.value)
    }

    // Price range filter
    if (currentFilters.value.minPrice !== undefined) {
      filtered = filtered.filter((s) => s.basePrice >= currentFilters.value.minPrice!)
    }
    if (currentFilters.value.maxPrice !== undefined) {
      filtered = filtered.filter((s) => s.basePrice <= currentFilters.value.maxPrice!)
    }

    // Tags filter
    if (currentFilters.value.tags && currentFilters.value.tags.length > 0) {
      filtered = filtered.filter((s) =>
        currentFilters.value.tags!.some((tag) => s.tags.includes(tag))
      )
    }

    filteredServices.value = filtered
  }

  /**
   * Set search term and apply filters
   */
  function setSearchTerm(term: string): void {
    searchTerm.value = term
    applyFilters()
  }

  /**
   * Set category filter and apply
   */
  function setCategory(category: ServiceCategory | undefined): void {
    selectedCategory.value = category
    applyFilters()
  }

  /**
   * Set status filter and apply
   */
  function setStatus(status: ServiceStatus | undefined): void {
    selectedStatus.value = status
    applyFilters()
  }

  /**
   * Set multiple filters at once
   */
  function setFilters(filters: ServiceFilters): void {
    currentFilters.value = filters
    searchTerm.value = filters.searchTerm || ''
    selectedCategory.value = filters.category
    selectedStatus.value = filters.status
    applyFilters()
  }

  /**
   * Clear all filters
   */
  function clearFilters(): void {
    searchTerm.value = ''
    selectedCategory.value = undefined
    selectedStatus.value = undefined
    currentFilters.value = {}
    applyFilters()
  }

  // ============================================
  // Actions - Bulk Operations
  // ============================================

  /**
   * Bulk activate services
   */
  async function bulkActivate(serviceIds: string[]): Promise<void> {
    try {
      await serviceService.bulkActivateServices(serviceIds)
      // Reload services to get updated status
      const providerId = services.value[0]?.providerId
      if (providerId) {
        await loadServices(providerId)
      }
      successMessage.value = `${serviceIds.length} service(s) activated successfully`
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to activate services'
      } else {
        error.value = 'Failed to activate services'
      }
      throw err
    }
  }

  /**
   * Bulk deactivate services
   */
  async function bulkDeactivate(serviceIds: string[]): Promise<void> {
    try {
      await serviceService.bulkDeactivateServices(serviceIds)
      const providerId = services.value[0]?.providerId
      if (providerId) {
        await loadServices(providerId)
      }
      successMessage.value = `${serviceIds.length} service(s) deactivated successfully`
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to deactivate services'
      } else {
        error.value = 'Failed to deactivate services'
      }
      throw err
    }
  }

  /**
   * Bulk delete services
   */
  async function bulkDelete(serviceIds: string[]): Promise<void> {
    try {
      await serviceService.bulkDeleteServices(serviceIds)
      const providerId = services.value[0]?.providerId
      if (providerId) {
        await loadServices(providerId)
      }
      successMessage.value = `${serviceIds.length} service(s) deleted successfully`
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to delete services'
      } else {
        error.value = 'Failed to delete services'
      }
      throw err
    }
  }

  /**
   * Update service display order
   */
  async function updateServiceOrder(providerId: string, serviceIds: string[]): Promise<void> {
    try {
      await serviceService.updateServiceOrder(providerId, serviceIds)
      // Update local order
      const orderedServices = serviceIds
        .map((id) => services.value.find((s) => s.id === id))
        .filter((s): s is Service => s !== undefined)

      services.value = orderedServices
      applyFilters()
      successMessage.value = 'Service order updated successfully'
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to update service order'
      } else {
        error.value = 'Failed to update service order'
      }
      throw err
    }
  }

  // ============================================
  // Actions - Utility
  // ============================================

  /**
   * Clear error message
   */
  function clearError(): void {
    error.value = null
  }

  /**
   * Clear success message
   */
  function clearSuccess(): void {
    successMessage.value = null
  }

  /**
   * Clear current service
   */
  function clearCurrentService(): void {
    currentService.value = null
  }

  /**
   * Reset store to initial state
   */
  function $reset(): void {
    services.value = []
    currentService.value = null
    filteredServices.value = []
    currentFilters.value = {}
    searchTerm.value = ''
    selectedCategory.value = undefined
    selectedStatus.value = undefined
    isLoading.value = false
    isSaving.value = false
    error.value = null
    successMessage.value = null
  }

  return {
    // State
    services,
    currentService,
    filteredServices,
    currentFilters,
    searchTerm,
    selectedCategory,
    selectedStatus,
    isLoading,
    isSaving,
    error,
    successMessage,

    // Getters
    hasServices,
    hasFilteredServices,
    totalServices,
    activeServices,
    draftServices,
    inactiveServices,
    servicesByCategory,
    serviceById,
    hasActiveFilters,

    // Actions - CRUD
    loadServices,
    loadService,
    createService,
    updateService,
    deleteService,

    // Actions - Status
    activateService,
    deactivateService,
    archiveService,

    // Actions - Filtering
    applyFilters,
    setSearchTerm,
    setCategory,
    setStatus,
    setFilters,
    clearFilters,

    // Actions - Bulk
    bulkActivate,
    bulkDeactivate,
    bulkDelete,
    updateServiceOrder,

    // Actions - Utility
    clearError,
    clearSuccess,
    clearCurrentService,
    $reset,
  }
})
