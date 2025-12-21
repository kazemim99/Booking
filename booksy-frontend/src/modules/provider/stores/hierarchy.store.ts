// booksy-frontend/src/modules/provider/stores/hierarchy.store.ts

/**
 * Provider Hierarchy Store
 * Manages state for provider hierarchy including invitations,
 * join requests, staff members, and organization relationships
 */

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { hierarchyService } from '../services/hierarchy.service'
import type {
  RegisterOrganizationRequest,
  RegisterIndependentIndividualRequest,
  SendInvitationRequest,
  AcceptInvitationRequest,
  CreateJoinRequestRequest,
  ConvertToOrganizationRequest,
  GetStaffMembersRequest,
  ProviderInvitation,
  JoinRequest,
  StaffMember,
  ProviderHierarchyDetails,
  OrganizationSummary,
  OrganizationSearchFilters,
  InvitationStatus,
  JoinRequestStatus,
} from '../types/hierarchy.types'

export const useHierarchyStore = defineStore('hierarchy', () => {
  // ============================================================================
  // STATE
  // ============================================================================

  // Current provider hierarchy
  const currentHierarchy = ref<ProviderHierarchyDetails | null>(null)

  // Invitations
  const sentInvitations = ref<ProviderInvitation[]>([])
  const receivedInvitations = ref<ProviderInvitation[]>([])

  // Join Requests
  const sentJoinRequests = ref<JoinRequest[]>([])
  const receivedJoinRequests = ref<JoinRequest[]>([])

  // Staff
  const staffMembers = ref<StaffMember[]>([])

  // Organizations for search
  const availableOrganizations = ref<OrganizationSummary[]>([])

  // Loading states
  const loading = ref({
    hierarchy: false,
    invitations: false,
    joinRequests: false,
    staff: false,
    organizations: false,
  })

  // Error states
  const errors = ref<{
    hierarchy?: string
    invitations?: string
    joinRequests?: string
    staff?: string
    organizations?: string
  }>({})

  // ============================================================================
  // COMPUTED
  // ============================================================================

  const isOrganization = computed(() => {
    return currentHierarchy.value?.provider.hierarchyType === 'Organization'
  })

  const isIndividual = computed(() => {
    return currentHierarchy.value?.provider.hierarchyType === 'Individual'
  })

  const hasParentOrganization = computed(() => {
    return !!currentHierarchy.value?.parentOrganization
  })

  const activeStaffCount = computed(() => {
    return staffMembers.value?.filter(s => s.isActive).length || 0
  })

  const pendingInvitations = computed(() => {
    return sentInvitations.value?.filter(inv => inv.status === 'Pending' as InvitationStatus) || []
  })

  const pendingJoinRequests = computed(() => {
    return receivedJoinRequests.value?.filter(req => req.status === 'Pending' as JoinRequestStatus) || []
  })

  // ============================================================================
  // ACTIONS - REGISTRATION
  // ============================================================================

  async function registerOrganization(request: RegisterOrganizationRequest) {
    try {
      loading.value.hierarchy = true
      errors.value.hierarchy = undefined

      const result = await hierarchyService.registerOrganization(request)

      if (result.success && result.data) {
        // Reload hierarchy to get updated data
        await loadProviderHierarchy(result.data.providerId)
        return result.data
      } else {
        throw new Error(result.message || 'Failed to register organization')
      }
    } catch (error: any) {
      errors.value.hierarchy = error.message || 'Failed to register organization'
      throw error
    } finally {
      loading.value.hierarchy = false
    }
  }

  async function registerIndividual(request: RegisterIndependentIndividualRequest) {
    try {
      loading.value.hierarchy = true
      errors.value.hierarchy = undefined

      const result = await hierarchyService.registerIndividual(request)

      if (result.success && result.data) {
        await loadProviderHierarchy(result.data.providerId)
        return result.data
      } else {
        throw new Error(result.message || 'Failed to register individual')
      }
    } catch (error: any) {
      errors.value.hierarchy = error.message || 'Failed to register individual'
      throw error
    } finally {
      loading.value.hierarchy = false
    }
  }

  // ============================================================================
  // ACTIONS - HIERARCHY QUERIES
  // ============================================================================

  async function loadProviderHierarchy(providerId: string) {
    try {
      loading.value.hierarchy = true
      errors.value.hierarchy = undefined

      currentHierarchy.value = await hierarchyService.getProviderHierarchy(providerId)
    } catch (error: any) {
      errors.value.hierarchy = error.message || 'Failed to load provider hierarchy'
      throw error
    } finally {
      loading.value.hierarchy = false
    }
  }

  async function loadStaffMembers(request: GetStaffMembersRequest) {
    try {
      loading.value.staff = true
      errors.value.staff = undefined

      const result = await hierarchyService.getStaffMembers(request)

      console.log('loadStaffMembers result:', result)

      // Ensure items is an array
      if (result && Array.isArray(result.items)) {
        staffMembers.value = result.items
      } else {
        staffMembers.value = []
      }

      return result
    } catch (error: any) {
      errors.value.staff = error.message || 'Failed to load staff members'
      staffMembers.value = [] // Reset to empty array on error
      throw error
    } finally {
      loading.value.staff = false
    }
  }

  async function removeStaffMember(organizationId: string, staffId: string) {
    try {
      loading.value.staff = true
      errors.value.staff = undefined

      await hierarchyService.removeStaffMember(organizationId, staffId)

      // Remove from local state
      staffMembers.value = staffMembers.value.filter(s => s.id !== staffId)

      // Reload hierarchy to update counts
      await loadProviderHierarchy(organizationId)
    } catch (error: any) {
      errors.value.staff = error.message || 'Failed to remove staff member'
      throw error
    } finally {
      loading.value.staff = false
    }
  }

  // ============================================================================
  // ACTIONS - INVITATIONS
  // ============================================================================

  async function sendInvitation(organizationId: string, request: SendInvitationRequest) {
    try {
      loading.value.invitations = true
      errors.value.invitations = undefined

      const result = await hierarchyService.sendInvitation(organizationId, request)

      console.log('sendInvitation result:', result)

      if (result.success && result.data) {
        // Add to sent invitations at the beginning of the list
        sentInvitations.value.unshift(result.data)
        return result.data
      } else {
        throw new Error(result.message || 'Failed to send invitation')
      }
    } catch (error: any) {
      console.error('Error in sendInvitation:', error)
      errors.value.invitations = error.message || 'Failed to send invitation'
      throw error
    } finally {
      loading.value.invitations = false
    }
  }

  async function loadSentInvitations(organizationId: string) {
    try {
      loading.value.invitations = true
      errors.value.invitations = undefined

      sentInvitations.value = await hierarchyService.getSentInvitations(organizationId)
    } catch (error: any) {
      errors.value.invitations = error.message || 'Failed to load sent invitations'
      throw error
    } finally {
      loading.value.invitations = false
    }
  }

  async function loadReceivedInvitations(individualId: string) {
    try {
      loading.value.invitations = true
      errors.value.invitations = undefined

      receivedInvitations.value = await hierarchyService.getReceivedInvitations(individualId)
    } catch (error: any) {
      errors.value.invitations = error.message || 'Failed to load received invitations'
      throw error
    } finally {
      loading.value.invitations = false
    }
  }

  async function getInvitation(organizationId: string, invitationId: string): Promise<ProviderInvitation> {
    try {
      loading.value.invitations = true
      errors.value.invitations = undefined

      return await hierarchyService.getInvitation(organizationId, invitationId)
    } catch (error: any) {
      errors.value.invitations = error.message || 'Failed to load invitation'
      throw error
    } finally {
      loading.value.invitations = false
    }
  }

  async function acceptInvitation(
    organizationId: string,
    invitationId: string,
    request: AcceptInvitationRequest
  ) {
    try {
      loading.value.invitations = true
      errors.value.invitations = undefined

      const result = await hierarchyService.acceptInvitation(organizationId, invitationId, request)

      if (result.success && result.data) {
        // Update invitation status in local state
        const invitation = receivedInvitations.value.find(inv => inv.id === invitationId)
        if (invitation) {
          invitation.status = 'Accepted' as InvitationStatus
          invitation.respondedAt = new Date()
        }

        // Reload hierarchy to show new organization relationship
        if (result.data.organizationId) {
          await loadProviderHierarchy(result.data.organizationId)
        }

        return result.data
      } else {
        throw new Error(result.message || 'Failed to accept invitation')
      }
    } catch (error: any) {
      errors.value.invitations = error.message || 'Failed to accept invitation'
      throw error
    } finally {
      loading.value.invitations = false
    }
  }

  async function rejectInvitation(organizationId: string, invitationId: string) {
    try {
      loading.value.invitations = true
      errors.value.invitations = undefined

      await hierarchyService.rejectInvitation(organizationId, invitationId)

      // Update invitation status in local state
      const invitation = receivedInvitations.value.find(inv => inv.id === invitationId)
      if (invitation) {
        invitation.status = 'Rejected' as InvitationStatus
        invitation.respondedAt = new Date()
      }
    } catch (error: any) {
      errors.value.invitations = error.message || 'Failed to reject invitation'
      throw error
    } finally {
      loading.value.invitations = false
    }
  }

  async function resendInvitation(organizationId: string, invitationId: string) {
    try {
      loading.value.invitations = true
      errors.value.invitations = undefined

      // Get the invitation to pass to resendInvitation
      const invitation = sentInvitations.value.find(inv => inv.id === invitationId)
      if (!invitation) {
        throw new Error('Invitation not found')
      }

      const result = await hierarchyService.resendInvitation(organizationId, invitation)

      if (result.success && result.data) {
        // Update invitation in local state
        const index = sentInvitations.value.findIndex(inv => inv.id === invitationId)
        if (index !== -1) {
          sentInvitations.value[index] = result.data
        }
        return result.data
      } else {
        throw new Error(result.message || 'Failed to resend invitation')
      }
    } catch (error: any) {
      errors.value.invitations = error.message || 'Failed to resend invitation'
      throw error
    } finally {
      loading.value.invitations = false
    }
  }

  async function cancelInvitation(organizationId: string, invitationId: string) {
    try {
      loading.value.invitations = true
      errors.value.invitations = undefined

      await hierarchyService.cancelInvitation(organizationId, invitationId)

      // Remove from local state
      sentInvitations.value = sentInvitations.value.filter(inv => inv.id !== invitationId)
    } catch (error: any) {
      errors.value.invitations = error.message || 'Failed to cancel invitation'
      throw error
    } finally {
      loading.value.invitations = false
    }
  }

  // ============================================================================
  // ACTIONS - JOIN REQUESTS
  // ============================================================================

  async function createJoinRequest(organizationId: string, request: CreateJoinRequestRequest) {
    try {
      loading.value.joinRequests = true
      errors.value.joinRequests = undefined

      const result = await hierarchyService.createJoinRequest(organizationId, request)

      if (result.success && result.data) {
        // Add to sent join requests
        sentJoinRequests.value.unshift(result.data)
        return result.data
      } else {
        throw new Error(result.message || 'Failed to create join request')
      }
    } catch (error: any) {
      errors.value.joinRequests = error.message || 'Failed to create join request'
      throw error
    } finally {
      loading.value.joinRequests = false
    }
  }

  async function loadSentJoinRequests(individualId: string) {
    try {
      loading.value.joinRequests = true
      errors.value.joinRequests = undefined

      sentJoinRequests.value = await hierarchyService.getSentJoinRequests(individualId)
    } catch (error: any) {
      errors.value.joinRequests = error.message || 'Failed to load sent join requests'
      throw error
    } finally {
      loading.value.joinRequests = false
    }
  }

  async function loadReceivedJoinRequests(organizationId: string) {
    try {
      loading.value.joinRequests = true
      errors.value.joinRequests = undefined

      const result = await hierarchyService.getReceivedJoinRequests(organizationId)

      console.log('loadReceivedJoinRequests result:', result)

      // Ensure result is an array
      if (Array.isArray(result)) {
        receivedJoinRequests.value = result
      } else {
        receivedJoinRequests.value = []
      }
    } catch (error: any) {
      errors.value.joinRequests = error.message || 'Failed to load received join requests'
      receivedJoinRequests.value = [] // Reset to empty array on error
      throw error
    } finally {
      loading.value.joinRequests = false
    }
  }

  async function approveJoinRequest(organizationId: string, requestId: string) {
    try {
      loading.value.joinRequests = true
      errors.value.joinRequests = undefined

      const result = await hierarchyService.approveJoinRequest(organizationId, requestId)

      if (result.success && result.data) {
        // Update request status in local state
        const request = receivedJoinRequests.value.find(req => req.id === requestId)
        if (request) {
          request.status = 'Approved' as JoinRequestStatus
          request.respondedAt = new Date()
        }

        // Reload staff members to include new member
        await loadStaffMembers({ organizationId, isActive: true })

        // Reload hierarchy to update counts
        await loadProviderHierarchy(organizationId)

        return result.data
      } else {
        throw new Error(result.message || 'Failed to approve join request')
      }
    } catch (error: any) {
      errors.value.joinRequests = error.message || 'Failed to approve join request'
      throw error
    } finally {
      loading.value.joinRequests = false
    }
  }

  async function rejectJoinRequest(organizationId: string, requestId: string, reason?: string) {
    try {
      loading.value.joinRequests = true
      errors.value.joinRequests = undefined

      await hierarchyService.rejectJoinRequest(organizationId, requestId, reason)

      // Update request status in local state
      const request = receivedJoinRequests.value.find(req => req.id === requestId)
      if (request) {
        request.status = 'Rejected' as JoinRequestStatus
        request.respondedAt = new Date()
        request.rejectionReason = reason
      }
    } catch (error: any) {
      errors.value.joinRequests = error.message || 'Failed to reject join request'
      throw error
    } finally {
      loading.value.joinRequests = false
    }
  }

  async function cancelJoinRequest(organizationId: string, requestId: string) {
    try {
      loading.value.joinRequests = true
      errors.value.joinRequests = undefined

      await hierarchyService.cancelJoinRequest(organizationId, requestId)

      // Remove from local state
      sentJoinRequests.value = sentJoinRequests.value.filter(req => req.id !== requestId)
    } catch (error: any) {
      errors.value.joinRequests = error.message || 'Failed to cancel join request'
      throw error
    } finally {
      loading.value.joinRequests = false
    }
  }

  // ============================================================================
  // ACTIONS - CONVERSION
  // ============================================================================

  async function convertToOrganization(individualId: string, request: ConvertToOrganizationRequest) {
    try {
      loading.value.hierarchy = true
      errors.value.hierarchy = undefined

      const result = await hierarchyService.convertToOrganization(individualId, request)

      if (result.success && result.data) {
        // Reload hierarchy to show new organization type
        await loadProviderHierarchy(result.data.newProviderId)
        return result.data
      } else {
        throw new Error(result.message || 'Failed to convert to organization')
      }
    } catch (error: any) {
      errors.value.hierarchy = error.message || 'Failed to convert to organization'
      throw error
    } finally {
      loading.value.hierarchy = false
    }
  }

  // ============================================================================
  // ACTIONS - ORGANIZATION SEARCH
  // ============================================================================

  async function searchOrganizations(filters: OrganizationSearchFilters) {
    try {
      loading.value.organizations = true
      errors.value.organizations = undefined

      const result = await hierarchyService.searchOrganizations(filters)
      availableOrganizations.value = result.items
      return result
    } catch (error: any) {
      errors.value.organizations = error.message || 'Failed to search organizations'
      throw error
    } finally {
      loading.value.organizations = false
    }
  }

  // ============================================================================
  // UTILITIES
  // ============================================================================

  function clearErrors() {
    errors.value = {}
  }

  function reset() {
    currentHierarchy.value = null
    sentInvitations.value = []
    receivedInvitations.value = []
    sentJoinRequests.value = []
    receivedJoinRequests.value = []
    staffMembers.value = []
    availableOrganizations.value = []
    errors.value = {}
  }

  // ============================================================================
  // RETURN
  // ============================================================================

  return {
    // State
    currentHierarchy,
    sentInvitations,
    receivedInvitations,
    sentJoinRequests,
    receivedJoinRequests,
    staffMembers,
    availableOrganizations,
    loading,
    errors,

    // Computed
    isOrganization,
    isIndividual,
    hasParentOrganization,
    activeStaffCount,
    pendingInvitations,
    pendingJoinRequests,

    // Registration actions
    registerOrganization,
    registerIndividual,

    // Hierarchy query actions
    loadProviderHierarchy,
    loadStaffMembers,
    removeStaffMember,

    // Invitation actions
    sendInvitation,
    loadSentInvitations,
    loadReceivedInvitations,
    getInvitation,
    acceptInvitation,
    rejectInvitation,
    resendInvitation,
    cancelInvitation,

    // Join request actions
    createJoinRequest,
    loadSentJoinRequests,
    loadReceivedJoinRequests,
    approveJoinRequest,
    rejectJoinRequest,
    cancelJoinRequest,

    // Conversion actions
    convertToOrganization,

    // Organization search actions
    searchOrganizations,

    // Utilities
    clearErrors,
    reset,
  }
})
