// src/modules/provider/stores/hierarchy.store.ts

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { hierarchyService } from '../services/hierarchy.service'
import type {
  ProviderHierarchyResponse,
  ProviderInvitation,
  ProviderJoinRequest,
  StaffMember,
  SendInvitationRequest,
  AcceptInvitationRequest,
  CreateJoinRequestRequest,
  ApproveJoinRequestRequest,
  RejectJoinRequestRequest,
  RemoveStaffMemberRequest,
  ConvertToOrganizationRequest,
  GetStaffMembersQuery,
  GetInvitationsQuery,
  GetJoinRequestsQuery,
  ProviderHierarchyType,
  InvitationStatus,
  JoinRequestStatus,
} from '../types/hierarchy.types'
import type { PagedResult } from '@/core/types/common.types'

export const useHierarchyStore = defineStore('hierarchy', () => {
  // ============================================
  // State
  // ============================================

  // Hierarchy
  const currentHierarchy = ref<ProviderHierarchyResponse | null>(null)
  const staffMembers = ref<PagedResult<StaffMember> | null>(null)

  // Invitations
  const pendingInvitations = ref<PagedResult<ProviderInvitation> | null>(null)
  const sentInvitations = ref<PagedResult<ProviderInvitation> | null>(null)

  // Join Requests
  const pendingJoinRequests = ref<PagedResult<ProviderJoinRequest> | null>(null)
  const myJoinRequests = ref<PagedResult<ProviderJoinRequest> | null>(null)

  // UI State
  const isLoading = ref(false)
  const isLoadingStaff = ref(false)
  const isLoadingInvitations = ref(false)
  const isLoadingJoinRequests = ref(false)
  const error = ref<string | null>(null)

  // ============================================
  // Getters (Computed)
  // ============================================

  const isOrganization = computed(() => {
    return currentHierarchy.value?.hierarchyType === ('Organization' as ProviderHierarchyType)
  })

  const isIndividual = computed(() => {
    return currentHierarchy.value?.hierarchyType === ('Individual' as ProviderHierarchyType)
  })

  const isIndependent = computed(() => {
    return currentHierarchy.value?.isIndependent ?? false
  })

  const canAddStaff = computed(() => {
    return isOrganization.value
  })

  const hasStaff = computed(() => {
    return (currentHierarchy.value?.staffCount ?? 0) > 0
  })

  const staffCount = computed(() => {
    return currentHierarchy.value?.staffCount ?? 0
  })

  const activeStaffCount = computed(() => {
    return staffMembers.value?.items?.filter((s) => s.isActive).length ?? 0
  })

  const hasPendingInvitations = computed(() => {
    return (pendingInvitations.value?.totalCount ?? 0) > 0
  })

  const hasPendingJoinRequests = computed(() => {
    return (pendingJoinRequests.value?.totalCount ?? 0) > 0
  })

  // ============================================
  // Actions - Hierarchy Queries
  // ============================================

  /**
   * Load provider hierarchy information
   */
  async function loadHierarchy(providerId: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      currentHierarchy.value = await hierarchyService.getProviderHierarchy(providerId)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load hierarchy'
      console.error('Error loading hierarchy:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Load staff members for an organization
   */
  async function loadStaffMembers(query: GetStaffMembersQuery): Promise<void> {
    isLoadingStaff.value = true
    error.value = null

    try {
      staffMembers.value = await hierarchyService.getStaffMembers(query)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load staff members'
      console.error('Error loading staff members:', err)
      throw err
    } finally {
      isLoadingStaff.value = false
    }
  }

  /**
   * Refresh staff members (use current query params)
   */
  async function refreshStaffMembers(organizationId: string): Promise<void> {
    await loadStaffMembers({ organizationId })
  }

  // ============================================
  // Actions - Invitations
  // ============================================

  /**
   * Send invitation to individual to join organization
   */
  async function sendInvitation(
    organizationId: string,
    request: SendInvitationRequest,
  ): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      await hierarchyService.sendInvitation(organizationId, request)
      // Reload invitations after sending
      await loadPendingInvitations({ organizationId })
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to send invitation'
      console.error('Error sending invitation:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Accept invitation to join organization
   */
  async function acceptInvitation(
    organizationId: string,
    invitationId: string,
    request: AcceptInvitationRequest,
  ): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      await hierarchyService.acceptInvitation(organizationId, invitationId, request)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to accept invitation'
      console.error('Error accepting invitation:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Load pending invitations for an organization
   */
  async function loadPendingInvitations(query: GetInvitationsQuery): Promise<void> {
    isLoadingInvitations.value = true
    error.value = null

    try {
      const result = await hierarchyService.getPendingInvitations({
        ...query,
        status: 'Pending' as InvitationStatus,
      })
      pendingInvitations.value = result
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load invitations'
      console.error('Error loading invitations:', err)
      throw err
    } finally {
      isLoadingInvitations.value = false
    }
  }

  // ============================================
  // Actions - Join Requests
  // ============================================

  /**
   * Create join request from individual to organization
   */
  async function createJoinRequest(
    organizationId: string,
    request: CreateJoinRequestRequest,
  ): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      await hierarchyService.createJoinRequest(organizationId, request)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to create join request'
      console.error('Error creating join request:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Approve join request
   */
  async function approveJoinRequest(
    organizationId: string,
    requestId: string,
    request: ApproveJoinRequestRequest,
  ): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      await hierarchyService.approveJoinRequest(organizationId, requestId, request)
      // Reload join requests and staff after approval
      await Promise.all([
        loadPendingJoinRequests({ organizationId }),
        refreshStaffMembers(organizationId),
      ])
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to approve join request'
      console.error('Error approving join request:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Reject join request
   */
  async function rejectJoinRequest(
    organizationId: string,
    requestId: string,
    request: RejectJoinRequestRequest,
  ): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      await hierarchyService.rejectJoinRequest(organizationId, requestId, request)
      // Reload join requests after rejection
      await loadPendingJoinRequests({ organizationId })
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to reject join request'
      console.error('Error rejecting join request:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Load pending join requests for an organization
   */
  async function loadPendingJoinRequests(query: GetJoinRequestsQuery): Promise<void> {
    isLoadingJoinRequests.value = true
    error.value = null

    try {
      const result = await hierarchyService.getJoinRequests({
        ...query,
        status: 'Pending' as JoinRequestStatus,
      })
      pendingJoinRequests.value = result
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load join requests'
      console.error('Error loading join requests:', err)
      throw err
    } finally {
      isLoadingJoinRequests.value = false
    }
  }

  /**
   * Load my join requests (for individual providers)
   */
  async function loadMyJoinRequests(requesterId: string): Promise<void> {
    isLoadingJoinRequests.value = true
    error.value = null

    try {
      const result = await hierarchyService.getJoinRequests({
        requesterId,
        organizationId: '', // Will be filtered on backend
      })
      myJoinRequests.value = result
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load my join requests'
      console.error('Error loading my join requests:', err)
      throw err
    } finally {
      isLoadingJoinRequests.value = false
    }
  }

  // ============================================
  // Actions - Staff Management
  // ============================================

  /**
   * Remove staff member from organization
   */
  async function removeStaffMember(
    organizationId: string,
    staffId: string,
    request: RemoveStaffMemberRequest,
  ): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      await hierarchyService.removeStaffMember(organizationId, staffId, request)
      // Reload staff after removal
      await refreshStaffMembers(organizationId)
      // Reload hierarchy to update staff count
      await loadHierarchy(organizationId)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to remove staff member'
      console.error('Error removing staff member:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  // ============================================
  // Actions - Conversion
  // ============================================

  /**
   * Convert individual provider to organization
   */
  async function convertToOrganization(
    providerId: string,
    request: ConvertToOrganizationRequest,
  ): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      currentHierarchy.value = await hierarchyService.convertToOrganization(providerId, request)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to convert to organization'
      console.error('Error converting to organization:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  // ============================================
  // Actions - State Management
  // ============================================

  /**
   * Clear all hierarchy data
   */
  function clearHierarchyData(): void {
    currentHierarchy.value = null
    staffMembers.value = null
    pendingInvitations.value = null
    sentInvitations.value = null
    pendingJoinRequests.value = null
    myJoinRequests.value = null
    error.value = null
  }

  /**
   * Clear error
   */
  function clearError(): void {
    error.value = null
  }

  // ============================================
  // Return Store API
  // ============================================

  return {
    // State
    currentHierarchy,
    staffMembers,
    pendingInvitations,
    sentInvitations,
    pendingJoinRequests,
    myJoinRequests,
    isLoading,
    isLoadingStaff,
    isLoadingInvitations,
    isLoadingJoinRequests,
    error,

    // Getters
    isOrganization,
    isIndividual,
    isIndependent,
    canAddStaff,
    hasStaff,
    staffCount,
    activeStaffCount,
    hasPendingInvitations,
    hasPendingJoinRequests,

    // Actions
    loadHierarchy,
    loadStaffMembers,
    refreshStaffMembers,
    sendInvitation,
    acceptInvitation,
    loadPendingInvitations,
    createJoinRequest,
    approveJoinRequest,
    rejectJoinRequest,
    loadPendingJoinRequests,
    loadMyJoinRequests,
    removeStaffMember,
    convertToOrganization,
    clearHierarchyData,
    clearError,
  }
})
