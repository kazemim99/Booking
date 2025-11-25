// booksy-frontend/src/modules/provider/services/hierarchy.service.ts

/**
 * Provider Hierarchy Service
 * API client for all provider hierarchy operations including
 * Organizations, Individuals, Invitations, Join Requests, and Conversions
 */

import apiClient from '@/core/services/api-client.service'
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

const API_BASE = '/v1/providers'

class HierarchyService {
  // ============================================================================
  // ORGANIZATION & INDIVIDUAL REGISTRATION
  // ============================================================================

  /**
   * Register a new organization provider
   */
  async registerOrganization(
    request: RegisterOrganizationRequest
  ): Promise<HierarchyApiResponse<{ providerId: string; hierarchyType: string }>> {
    const response = await apiClient.post<HierarchyApiResponse<{ providerId: string; hierarchyType: string }>>(
      `${API_BASE}/organizations`,
      request
    )
    return response.data
  }

  /**
   * Register a new independent individual provider
   */
  async registerIndividual(
    request: RegisterIndependentIndividualRequest
  ): Promise<HierarchyApiResponse<{ providerId: string; hierarchyType: string }>> {
    const response = await apiClient.post<HierarchyApiResponse<{ providerId: string; hierarchyType: string }>>(
      `${API_BASE}/individuals`,
      request
    )
    return response.data
  }

  // ============================================================================
  // HIERARCHY QUERIES
  // ============================================================================

  /**
   * Get provider hierarchy details including staff and parent organization
   */
  async getProviderHierarchy(providerId: string): Promise<ProviderHierarchyDetails> {
    const response = await apiClient.get<ProviderHierarchyDetails>(
      `${API_BASE}/${providerId}/hierarchy`
    )
    return response.data
  }

  /**
   * Get staff members for an organization with filtering
   */
  async getStaffMembers(request: GetStaffMembersRequest): Promise<PagedHierarchyResponse<StaffMember>> {
    const params = new URLSearchParams()
    if (request.isActive !== undefined) params.append('isActive', String(request.isActive))
    if (request.page) params.append('page', String(request.page))
    if (request.pageSize) params.append('pageSize', String(request.pageSize))

    const response = await apiClient.get<PagedHierarchyResponse<StaffMember>>(
      `${API_BASE}/${request.organizationId}/hierarchy/staff?${params.toString()}`
    )
    return response.data
  }

  /**
   * Remove a staff member from an organization
   */
  async removeStaffMember(organizationId: string, staffId: string): Promise<HierarchyApiResponse<void>> {
    const response = await apiClient.delete<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/staff/${staffId}`
    )
    return response.data
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
    const response = await apiClient.post<HierarchyApiResponse<ProviderInvitation>>(
      `${API_BASE}/${organizationId}/hierarchy/invitations`,
      request
    )
    return response.data
  }

  /**
   * Get all invitations sent by an organization
   */
  async getSentInvitations(organizationId: string): Promise<ProviderInvitation[]> {
    const response = await apiClient.get<ProviderInvitation[]>(
      `${API_BASE}/${organizationId}/hierarchy/invitations`
    )
    return response.data
  }

  /**
   * Get all invitations received by an individual
   */
  async getReceivedInvitations(individualId: string): Promise<ProviderInvitation[]> {
    const response = await apiClient.get<ProviderInvitation[]>(
      `${API_BASE}/${individualId}/hierarchy/invitations/received`
    )
    return response.data
  }

  /**
   * Get a specific invitation by ID
   */
  async getInvitation(organizationId: string, invitationId: string): Promise<ProviderInvitation> {
    const response = await apiClient.get<ProviderInvitation>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}`
    )
    return response.data
  }

  /**
   * Accept an invitation to join an organization
   */
  async acceptInvitation(
    organizationId: string,
    invitationId: string,
    request: AcceptInvitationRequest
  ): Promise<HierarchyApiResponse<{ staffMemberId: string; organizationId: string }>> {
    const response = await apiClient.post<HierarchyApiResponse<{ staffMemberId: string; organizationId: string }>>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}/accept`,
      request
    )
    return response.data
  }

  /**
   * Reject an invitation
   */
  async rejectInvitation(
    organizationId: string,
    invitationId: string
  ): Promise<HierarchyApiResponse<void>> {
    const response = await apiClient.post<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}/reject`
    )
    return response.data
  }

  /**
   * Resend an expired or rejected invitation
   */
  async resendInvitation(
    organizationId: string,
    invitationId: string
  ): Promise<HierarchyApiResponse<ProviderInvitation>> {
    const response = await apiClient.post<HierarchyApiResponse<ProviderInvitation>>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}/resend`
    )
    return response.data
  }

  /**
   * Cancel a pending invitation
   */
  async cancelInvitation(
    organizationId: string,
    invitationId: string
  ): Promise<HierarchyApiResponse<void>> {
    const response = await apiClient.delete<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}`
    )
    return response.data
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
    const response = await apiClient.post<HierarchyApiResponse<JoinRequest>>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests`,
      request
    )
    return response.data
  }

  /**
   * Get all join requests sent by an individual
   */
  async getSentJoinRequests(individualId: string): Promise<JoinRequest[]> {
    const response = await apiClient.get<JoinRequest[]>(
      `${API_BASE}/${individualId}/hierarchy/join-requests/sent`
    )
    return response.data
  }

  /**
   * Get all join requests received by an organization
   */
  async getReceivedJoinRequests(organizationId: string): Promise<JoinRequest[]> {
    const response = await apiClient.get<JoinRequest[]>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests`
    )
    return response.data
  }

  /**
   * Approve a join request
   */
  async approveJoinRequest(
    organizationId: string,
    requestId: string
  ): Promise<HierarchyApiResponse<{ staffMemberId: string }>> {
    const response = await apiClient.post<HierarchyApiResponse<{ staffMemberId: string }>>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests/${requestId}/approve`
    )
    return response.data
  }

  /**
   * Reject a join request
   */
  async rejectJoinRequest(
    organizationId: string,
    requestId: string,
    reason?: string
  ): Promise<HierarchyApiResponse<void>> {
    const response = await apiClient.post<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests/${requestId}/reject`,
      { reason }
    )
    return response.data
  }

  /**
   * Cancel a pending join request
   */
  async cancelJoinRequest(
    organizationId: string,
    requestId: string
  ): Promise<HierarchyApiResponse<void>> {
    const response = await apiClient.delete<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests/${requestId}`
    )
    return response.data
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
    const response = await apiClient.post<HierarchyApiResponse<{ newProviderId: string; hierarchyType: string }>>(
      `${API_BASE}/${individualId}/hierarchy/convert-to-organization`,
      request
    )
    return response.data
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

    const response = await apiClient.get<PagedHierarchyResponse<OrganizationSummary>>(
      `${API_BASE}/organizations/search?${params.toString()}`
    )
    return response.data
  }

  /**
   * Get organization details by ID
   */
  async getOrganization(organizationId: string): Promise<OrganizationSummary> {
    const response = await apiClient.get<OrganizationSummary>(
      `${API_BASE}/organizations/${organizationId}`
    )
    return response.data
  }
}

// Export singleton instance
export const hierarchyService = new HierarchyService()
export default hierarchyService
