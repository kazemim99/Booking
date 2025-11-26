// booksy-frontend/src/modules/provider/services/hierarchy.service.ts

/**
 * Provider Hierarchy Service
 * API client for all provider hierarchy operations including
 * Organizations, Individuals, Invitations, Join Requests, and Conversions
 */

import { serviceCategoryClient } from '@/core/api/client/http-client'
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
  HierarchyApiResponse,
  PagedHierarchyResponse,
  OrganizationSearchFilters,
} from '../types/hierarchy.types'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/providers`

class HierarchyService {
  // ============================================================================
  // ORGANIZATION & INDIVIDUAL REGISTRATION
  // ============================================================================

  /**
   * Get current user's draft provider (if exists)
   * Uses the /registration/progress endpoint for consistency
   */
  async getDraftProvider(): Promise<any> {
    try {
      const response = await serviceCategoryClient.get<any>('v1/registration/progress')

      // Extract draft data from progress response
      if (response.data?.hasDraft && response.data?.draftData) {
        const draft = response.data.draftData

        // Add registration step to the response for consistency
        return {
          ...draft,
          registrationStep: response.data.currentStep || draft.registrationStep
        }
      }

      return null
    } catch (error: any) {
      // Return null if no draft found (404)
      if (error.response?.status === 404) {
        return null
      }
      throw error
    }
  }

  /**
   * Register a new organization provider
   */
  async registerOrganization(
    request: RegisterOrganizationRequest
  ): Promise<HierarchyApiResponse<{ providerId: string; hierarchyType: string }>> {
    const response = await serviceCategoryClient.post<{ providerId: string; hierarchyType: string }>(
      `${API_BASE}/organizations`,
      request
    )
    return response as unknown as HierarchyApiResponse<{ providerId: string; hierarchyType: string }>
  }

  /**
   * Register a new independent individual provider
   */
  async registerIndividual(
    request: RegisterIndependentIndividualRequest
  ): Promise<HierarchyApiResponse<{ providerId: string; hierarchyType: string }>> {
    const response = await serviceCategoryClient.post<{ providerId: string; hierarchyType: string }>(
      `${API_BASE}/individuals`,
      request
    )
    return response as unknown as HierarchyApiResponse<{ providerId: string; hierarchyType: string }>
  }

  // ============================================================================
  // HIERARCHY QUERIES
  // ============================================================================

  /**
   * Get provider hierarchy details including staff and parent organization
   */
  async getProviderHierarchy(providerId: string): Promise<ProviderHierarchyDetails> {
    const response = await serviceCategoryClient.get<ProviderHierarchyDetails>(
      `${API_BASE}/${providerId}/hierarchy`
    )
    return response.data!
  }

  /**
   * Get staff members for an organization with filtering
   */
  async getStaffMembers(request: GetStaffMembersRequest): Promise<PagedHierarchyResponse<StaffMember>> {
    const params = new URLSearchParams()
    if (request.isActive !== undefined) params.append('isActive', String(request.isActive))
    if (request.page) params.append('page', String(request.page))
    if (request.pageSize) params.append('pageSize', String(request.pageSize))

    const response = await serviceCategoryClient.get<PagedHierarchyResponse<StaffMember>>(
      `${API_BASE}/${request.organizationId}/hierarchy/staff?${params.toString()}`
    )
    return response.data!
  }

  /**
   * Remove a staff member from an organization
   */
  async removeStaffMember(organizationId: string, staffId: string): Promise<HierarchyApiResponse<void>> {
    const response = await serviceCategoryClient.delete<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/staff/${staffId}`
    )
    return response.data!
  }

  // ============================================================================
  // INVITATION MANAGEMENT
  // ============================================================================

  /**
   * Send an invitation to an individual to join organization as staff
   */
  async sendInvitation(
    organizationId: string,
    request: SendInvitationRequest
  ): Promise<HierarchyApiResponse<ProviderInvitation>> {
    const response = await serviceCategoryClient.post<HierarchyApiResponse<ProviderInvitation>>(
      `${API_BASE}/${organizationId}/hierarchy/invitations`,
      request
    )
    return response.data!
  }

  /**
   * Get all invitations sent by an organization
   */
  async getSentInvitations(organizationId: string): Promise<ProviderInvitation[]> {
    const response = await serviceCategoryClient.get<ProviderInvitation[]>(
      `${API_BASE}/${organizationId}/hierarchy/invitations`
    )
    return response.data!
  }

  /**
   * Get all invitations received by an individual
   */
  async getReceivedInvitations(individualId: string): Promise<ProviderInvitation[]> {
    const response = await serviceCategoryClient.get<ProviderInvitation[]>(
      `${API_BASE}/${individualId}/hierarchy/invitations/received`
    )
    return response.data!
  }

  /**
   * Get a specific invitation by ID
   */
  async getInvitation(organizationId: string, invitationId: string): Promise<ProviderInvitation> {
    const response = await serviceCategoryClient.get<ProviderInvitation>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}`
    )
    return response.data!
  }

  /**
   * Accept an invitation to join an organization
   */
  async acceptInvitation(
    organizationId: string,
    invitationId: string,
    request: AcceptInvitationRequest
  ): Promise<HierarchyApiResponse<{ staffMemberId: string; organizationId: string }>> {
    const response = await serviceCategoryClient.post<HierarchyApiResponse<{ staffMemberId: string; organizationId: string }>>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}/accept`,
      request
    )
    return response.data!
  }

  /**
   * Reject an invitation
   */
  async rejectInvitation(
    organizationId: string,
    invitationId: string
  ): Promise<HierarchyApiResponse<void>> {
    const response = await serviceCategoryClient.post<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}/reject`
    )
    return response.data!
  }

  /**
   * Resend an expired or rejected invitation
   */
  async resendInvitation(
    organizationId: string,
    invitationId: string
  ): Promise<HierarchyApiResponse<ProviderInvitation>> {
    const response = await serviceCategoryClient.post<HierarchyApiResponse<ProviderInvitation>>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}/resend`
    )
    return response.data!
  }

  /**
   * Cancel a pending invitation
   */
  async cancelInvitation(
    organizationId: string,
    invitationId: string
  ): Promise<HierarchyApiResponse<void>> {
    const response = await serviceCategoryClient.delete<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}`
    )
    return response.data!
  }

  // ============================================================================
  // JOIN REQUEST MANAGEMENT
  // ============================================================================

  /**
   * Create a join request to an organization
   */
  async createJoinRequest(
    organizationId: string,
    request: CreateJoinRequestRequest
  ): Promise<HierarchyApiResponse<JoinRequest>> {
    const response = await serviceCategoryClient.post<HierarchyApiResponse<JoinRequest>>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests`,
      request
    )
    return response.data!
  }

  /**
   * Get all join requests sent by an individual
   */
  async getSentJoinRequests(individualId: string): Promise<JoinRequest[]> {
    const response = await serviceCategoryClient.get<JoinRequest[]>(
      `${API_BASE}/${individualId}/hierarchy/join-requests/sent`
    )
    return response.data!
  }

  /**
   * Get all join requests received by an organization
   */
  async getReceivedJoinRequests(organizationId: string): Promise<JoinRequest[]> {
    const response = await serviceCategoryClient.get<JoinRequest[]>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests`
    )
    return response.data!
  }

  /**
   * Approve a join request
   */
  async approveJoinRequest(
    organizationId: string,
    requestId: string
  ): Promise<HierarchyApiResponse<{ staffMemberId: string }>> {
    const response = await serviceCategoryClient.post<HierarchyApiResponse<{ staffMemberId: string }>>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests/${requestId}/approve`
    )
    return response.data!
  }

  /**
   * Reject a join request
   */
  async rejectJoinRequest(
    organizationId: string,
    requestId: string,
    reason?: string
  ): Promise<HierarchyApiResponse<void>> {
    const response = await serviceCategoryClient.post<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests/${requestId}/reject`,
      { reason }
    )
    return response.data!
  }

  /**
   * Cancel a pending join request
   */
  async cancelJoinRequest(
    organizationId: string,
    requestId: string
  ): Promise<HierarchyApiResponse<void>> {
    const response = await serviceCategoryClient.delete<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests/${requestId}`
    )
    return response.data!
  }

  // ============================================================================
  // CONVERSION
  // ============================================================================

  /**
   * Convert an independent individual to an organization
   */
  async convertToOrganization(
    individualId: string,
    request: ConvertToOrganizationRequest
  ): Promise<HierarchyApiResponse<{ newProviderId: string; hierarchyType: string }>> {
    const response = await serviceCategoryClient.post<HierarchyApiResponse<{ newProviderId: string; hierarchyType: string }>>(
      `${API_BASE}/${individualId}/hierarchy/convert-to-organization`,
      request
    )
    return response.data!
  }

  // ============================================================================
  // ORGANIZATION SEARCH
  // ============================================================================

  /**
   * Search for organizations that individuals can join
   */
  async searchOrganizations(
    filters: OrganizationSearchFilters
  ): Promise<PagedHierarchyResponse<OrganizationSummary>> {
    const params = new URLSearchParams()
    if (filters.city) params.append('city', filters.city)
    if (filters.state) params.append('state', filters.state)
    if (filters.businessType) params.append('type', filters.businessType)
    if (filters.minStaffCount) params.append('minStaffCount', String(filters.minStaffCount))
    if (filters.verifiedOnly) params.append('verifiedOnly', 'true')
    if (filters.searchTerm) params.append('search', filters.searchTerm)

    const response = await serviceCategoryClient.get<PagedHierarchyResponse<OrganizationSummary>>(
      `${API_BASE}/organizations/search?${params.toString()}`
    )
    return response.data!
  }

  /**
   * Get organization details by ID
   */
  async getOrganization(organizationId: string): Promise<OrganizationSummary> {
    const response = await serviceCategoryClient.get<OrganizationSummary>(
      `${API_BASE}/organizations/${organizationId}`
    )
    return response.data!
  }
}

// Export singleton instance
export const hierarchyService = new HierarchyService()
export default hierarchyService
