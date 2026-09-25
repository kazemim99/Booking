// src/modules/provider/stores/staff.store.ts

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { staffService } from '../services/staff.service'
import type {
  Staff,
  CreateStaffRequest,
  UpdateStaffRequest,
  StaffFilters,
  StaffRole,
} from '../types/staff.types'
import { calculateTenure } from '../types/staff.types'

export const useStaffStore = defineStore('staff', () => {
  // ============================================
  // State
  // ============================================

  const staff = ref<Staff[]>([])
  const currentStaff = ref<Staff | null>(null)
  const isLoading = ref(false)
  const isCreating = ref(false)
  const isUpdating = ref(false)
  const error = ref<string | null>(null)

  // Filter state
  const filters = ref<StaffFilters>({
    role: '',
    isActive: 'all',
    searchTerm: '',
  })

  // ============================================
  // Computed
  // ============================================

  /**
   * Filtered staff based on current filters
   */
  const filteredStaff = computed(() => {
    let result = [...staff.value]

    // Filter by role
    if (filters.value.role) {
      result = result.filter((s) => s.role === filters.value.role)
    }

    // Filter by active status
    if (filters.value.isActive !== 'all') {
      result = result.filter((s) => s.isActive === filters.value.isActive)
    }

    // Filter by search term
    if (filters.value.searchTerm) {
      const term = filters.value.searchTerm.toLowerCase()
      result = result.filter(
        (s) =>
          s.fullName.toLowerCase().includes(term) ||
          s.email.toLowerCase().includes(term) ||
          s.phone?.toLowerCase().includes(term) ||
          s.role.toLowerCase().includes(term)
      )
    }

    return result
  })

  /**
   * Active staff members only
   */
  const activeStaff = computed(() => staff.value.filter((s) => s.isActive))

  /**
   * Inactive staff members only
   */
  const inactiveStaff = computed(() => staff.value.filter((s) => !s.isActive))

  /**
   * Staff grouped by role
   */
  const staffByRole = computed(() => {
    const groups: Record<string, Staff[]> = {}

    staff.value.forEach((s) => {
      if (!groups[s.role]) {
        groups[s.role] = []
      }
      groups[s.role].push(s)
    })

    return groups
  })

  /**
   * Check if store has any staff
   */
  const hasStaff = computed(() => staff.value.length > 0)

  /**
   * Check if filtered results are empty
   */
  const hasFilteredStaff = computed(() => filteredStaff.value.length > 0)

  /**
   * Total staff count
   */
  const totalStaff = computed(() => staff.value.length)

  /**
   * Active staff count
   */
  const activeStaffCount = computed(() => activeStaff.value.length)

  /**
   * Inactive staff count
   */
  const inactiveStaffCount = computed(() => inactiveStaff.value.length)

  /**
   * Average tenure in months
   */
  const averageTenure = computed(() => {
    if (staff.value.length === 0) return 0

    const totalMonths = staff.value.reduce((sum, s) => {
      return sum + calculateTenure(s.hiredAt, s.terminatedAt)
    }, 0)

    return Math.round(totalMonths / staff.value.length)
  })

  // ============================================
  // Actions - Fetch
  // ============================================

  /**
   * Load all staff for a provider
   */
  async function loadStaffByProvider(providerId: string, activeOnly = false): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      console.log('[StaffStore] Loading staff for provider:', providerId)
      const data = await staffService.getStaffByProvider(providerId, activeOnly)
      staff.value = data
      console.log('[StaffStore] Staff loaded:', data.length)
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to load staff'
      console.error('[StaffStore] Error loading staff:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Get staff member by ID from local cache
   */
  function getStaffById(id: string): Staff | null {
    const cached = staff.value.find((s) => s.id === id)
    if (cached) {
      currentStaff.value = cached
      return cached
    }
    return null
  }

  // ============================================
  // Actions - Create
  // ============================================

  /**
   * Create new staff member with optimistic update
   */
  async function createStaff(providerId: string, data: CreateStaffRequest): Promise<Staff> {
    isCreating.value = true
    error.value = null

    try {
      console.log('[StaffStore] Creating staff:', data)

      // Create via API
      const newStaff = await staffService.createStaff(providerId, data)

      // Add to store
      staff.value.push(newStaff)
      console.log('[StaffStore] Staff created and added to store')

      return newStaff
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to create staff'
      console.error('[StaffStore] Error creating staff:', err)
      throw err
    } finally {
      isCreating.value = false
    }
  }

  // ============================================
  // Actions - Update
  // ============================================

  /**
   * Update staff member with optimistic update
   */
  async function updateStaff(providerId: string, staffId: string, data: UpdateStaffRequest): Promise<Staff> {
    isUpdating.value = true
    error.value = null

    const originalIndex = staff.value.findIndex((s) => s.id === staffId)
    const original = originalIndex >= 0 ? { ...staff.value[originalIndex] } : null

    // Optimistically update UI
    if (originalIndex >= 0) {
      staff.value[originalIndex] = { ...staff.value[originalIndex], ...data }
      applyFilters()
    }

    try {
      console.log('[StaffStore] Updating staff:', staffId, data)

      // Update via API
      const updatedStaff = await staffService.updateStaff(providerId, staffId, data)

      // Replace with server response
      if (originalIndex >= 0) {
        staff.value[originalIndex] = updatedStaff
        applyFilters()
      }

      // Update current staff if it's the same one
      if (currentStaff.value?.id === staffId) {
        currentStaff.value = updatedStaff
      }

      console.log('[StaffStore] Staff updated successfully')
      return updatedStaff
    } catch (err: unknown) {
      // Rollback on failure
      if (original && originalIndex >= 0) {
        staff.value[originalIndex] = original
        applyFilters()
      }

      error.value = err instanceof Error ? err.message : 'Failed to update staff'
      console.error('[StaffStore] Error updating staff:', err)
      throw err
    } finally {
      isUpdating.value = false
    }
  }


  // ============================================
  // Actions - Delete
  // ============================================

  /**
   * Delete/Remove staff member with optimistic update
   */
  async function deleteStaff(providerId: string, staffId: string): Promise<void> {
    const originalIndex = staff.value.findIndex((s) => s.id === staffId)
    const original = originalIndex >= 0 ? { ...staff.value[originalIndex] } : null

    // Optimistically remove from UI
    if (originalIndex >= 0) {
      staff.value.splice(originalIndex, 1)
    }

    try {
      console.log('[StaffStore] Deleting staff:', staffId)

      // Delete via API
      await staffService.deleteStaff(providerId, staffId)
      console.log('[StaffStore] Staff deleted successfully')
    } catch (err: unknown) {
      // Rollback on failure
      if (original && originalIndex >= 0) {
        staff.value.splice(originalIndex, 0, original)
      }

      error.value = err instanceof Error ? err.message : 'Failed to delete staff'
      console.error('[StaffStore] Error deleting staff:', err)
      throw err
    }
  }

  // ============================================
  // Actions - Photo Upload
  // ============================================

  /**
   * Upload staff profile photo
   */
  async function uploadStaffPhoto(
    providerId: string,
    staffId: string,
    file: File,
    onUploadProgress?: (progressEvent: any) => void
  ): Promise<{ imageUrl: string; thumbnailUrl: string }> {
    isUpdating.value = true
    error.value = null

    try {
      console.log('[StaffStore] Uploading photo for staff:', staffId)

      // Upload via API
      const result = await staffService.uploadStaffPhoto(providerId, staffId, file, onUploadProgress)

      // Update local state with new photo URL
      const staffIndex = staff.value.findIndex((s) => s.id === staffId)
      if (staffIndex >= 0) {
        staff.value[staffIndex] = {
          ...staff.value[staffIndex],
          profilePhotoUrl: result.imageUrl
        }
      }

      // Update current staff if it's the same one
      if (currentStaff.value?.id === staffId) {
        currentStaff.value = {
          ...currentStaff.value,
          profilePhotoUrl: result.imageUrl
        }
      }

      console.log('[StaffStore] Photo uploaded successfully:', result.imageUrl)
      return result
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to upload photo'
      console.error('[StaffStore] Error uploading photo:', err)
      throw err
    } finally {
      isUpdating.value = false
    }
  }

  // ============================================
  // Actions - Filters
  // ============================================

  /**
   * Set role filter
   */
  function setRoleFilter(role: StaffRole | '') {
    filters.value.role = role
    applyFilters()
  }

  /**
   * Set active status filter
   */
  function setActiveFilter(isActive: boolean | 'all') {
    filters.value.isActive = isActive
    applyFilters()
  }

  /**
   * Set search term filter
   */
  function setSearchTerm(searchTerm: string) {
    filters.value.searchTerm = searchTerm
    applyFilters()
  }

  /**
   * Clear all filters
   */
  function clearFilters() {
    filters.value = {
      role: '',
      isActive: 'all',
      searchTerm: '',
    }
  }

  /**
   * Apply filters (triggers computed re-evaluation)
   */
  function applyFilters() {
    // This is just a trigger for reactivity
    // The actual filtering happens in the filteredStaff computed property
  }

  // ============================================
  // Actions - Utility
  // ============================================

  /**
   * Reset store state
   */
  function resetStore() {
    staff.value = []
    currentStaff.value = null
    isLoading.value = false
    isCreating.value = false
    isUpdating.value = false
    error.value = null
    clearFilters()
  }

  /**
   * Clear error
   */
  function clearError() {
    error.value = null
  }

  // ============================================
  // Return
  // ============================================

  return {
    // State
    staff,
    currentStaff,
    isLoading,
    isCreating,
    isUpdating,
    error,
    filters,

    // Computed
    filteredStaff,
    activeStaff,
    inactiveStaff,
    staffByRole,
    hasStaff,
    hasFilteredStaff,
    totalStaff,
    activeStaffCount,
    inactiveStaffCount,
    averageTenure,

    // Actions
    loadStaffByProvider,
    getStaffById,
    createStaff,
    updateStaff,
    deleteStaff,
    uploadStaffPhoto,
    setRoleFilter,
    setActiveFilter,
    setSearchTerm,
    clearFilters,
    resetStore,
    clearError,
  }
})
