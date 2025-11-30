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
  AcceptInvitationWithRegistrationRequest,
  AcceptInvitationWithRegistrationResponse,
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

/**
 * Convert relative URL to absolute URL using the API base URL
 */
function toAbsoluteUrl(url: string | undefined): string | undefined {
  if (!url) return url
  if (url.startsWith('http://') || url.startsWith('https://')) return url
  if (url.startsWith('/')) {
    // Get the API base URL from environment or use default
    const apiBaseUrl = import.meta.env.VITE_SERVICE_CATEGORY_API_URL || 'http://localhost:5010/api'
    // Remove /api suffix to get just the domain
    const baseUrl = apiBaseUrl.replace(/\/api\/?$/, '')
    return `${baseUrl}${url}`
  }
  return url
}

/**
 * Map backend invitation response to frontend ProviderInvitation type
 * Backend uses different field names than frontend
 */
function mapInvitationResponse(backendInvitation: any): ProviderInvitation {
  return {
    id: backendInvitation.invitationId || backendInvitation.id,
    organizationId: backendInvitation.organizationId,
    organizationName: backendInvitation.organizationName || '',
    organizationLogo: toAbsoluteUrl(backendInvitation.organizationLogo),
    organizationType: backendInvitation.organizationType,
    inviteePhoneNumber: backendInvitation.phoneNumber || backendInvitation.inviteePhoneNumber,
    inviteeName: backendInvitation.inviteeName || '',
    message: backendInvitation.message,
    status: backendInvitation.status,
    sentAt: new Date(backendInvitation.createdAt || backendInvitation.sentAt),
    expiresAt: new Date(backendInvitation.expiresAt),
    respondedAt: backendInvitation.respondedAt ? new Date(backendInvitation.respondedAt) : undefined,
    acceptedByProviderId: backendInvitation.acceptedByProviderId,
    createdBy: backendInvitation.createdBy,
    createdByName: backendInvitation.createdByName,
  }
}

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
    const response = await serviceCategoryClient.get<any>(
      `${API_BASE}/${providerId}/hierarchy`
    )

    // Map the flat API response to the expected nested structure
    const data = response.data
    return {
      provider: {
        id: data.providerId,
        hierarchyType: data.hierarchyType as HierarchyType,
        businessName: data.businessName,
        businessType: data.businessType,
        description: data.description,
        logoUrl: data.logoUrl,
        firstName: data.firstName,
        lastName: data.lastName,
        fullName: data.fullName,
        bio: data.bio,
        photoUrl: data.photoUrl,
        parentOrganizationId: data.parentOrganizationId,
        staffCount: data.totalStaffCount,
        activeStaffCount: data.activeStaffCount,
      },
      staff: data.staffMembers || [],
      parentOrganization: data.parentOrganization ? {
        id: data.parentOrganization.id,
        businessName: data.parentOrganization.businessName,
        businessType: data.parentOrganization.businessType,
        logoUrl: data.parentOrganization.logoUrl,
        city: data.parentOrganization.city,
        state: data.parentOrganization.state,
      } : undefined,
      organizationOwner: data.organizationOwner,
    }
  }

  /**
   * Get staff members for an organization with filtering
   */
  async getStaffMembers(request: GetStaffMembersRequest): Promise<PagedHierarchyResponse<StaffMember>> {
    const params = new URLSearchParams()
    if (request.isActive !== undefined) params.append('isActive', String(request.isActive))
    if (request.page) params.append('page', String(request.page))
    if (request.pageSize) params.append('pageSize', String(request.pageSize))

    const response = await serviceCategoryClient.get<any>(
      `${API_BASE}/${request.organizationId}/hierarchy/staff?${params.toString()}`
    )

    console.log('getStaffMembers raw response:', response.data)

    // Map staff member to ensure fullName is present
    const mapStaffMember = (staff: any): StaffMember => ({
      ...staff,
      fullName: staff.fullName || `${staff.firstName || ''} ${staff.lastName || ''}`.trim(),
      specializations: staff.specializations || [],
    })

    // Backend returns { organizationId, staffMembers: [...] }
    // Extract the staffMembers array and wrap in PagedHierarchyResponse format
    if (response.data && response.data.staffMembers && Array.isArray(response.data.staffMembers)) {
      return {
        items: response.data.staffMembers.map(mapStaffMember),
        totalCount: response.data.staffMembers.length,
        page: request.page || 1,
        pageSize: request.pageSize || response.data.staffMembers.length,
        hasNextPage: false,
      }
    }

    // Fallback: if response.data already has the expected format
    if (response.data && response.data.items) {
      return {
        ...response.data,
        items: response.data.items.map(mapStaffMember),
      } as PagedHierarchyResponse<StaffMember>
    }

    // No staff members found
    return {
      items: [],
      totalCount: 0,
      page: request.page || 1,
      pageSize: request.pageSize || 10,
      hasNextPage: false,
    }
  }

  /**
   * Remove a staff member from an organization
   */
  async removeStaffMember(
    organizationId: string,
    staffId: string,
    reason: string = 'Removed by organization'
  ): Promise<HierarchyApiResponse<void>> {
    const response = await serviceCategoryClient.delete<HierarchyApiResponse<void>>(
      `${API_BASE}/${organizationId}/hierarchy/staff/${staffId}`,
      {
        data: { reason }
      }
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
    const response = await serviceCategoryClient.post<any>(
      `${API_BASE}/${organizationId}/hierarchy/invitations`,
      request
    )

    console.log('sendInvitation raw response:', response.data)

    // Check if response is already wrapped in HierarchyApiResponse format
    if (response.data && typeof response.data === 'object' && 'success' in response.data) {
      return {
        success: response.data.success,
        data: response.data.data ? mapInvitationResponse(response.data.data) : undefined,
        message: response.data.message,
        errorCode: response.data.errorCode,
      }
    }

    // Check if response has the invitation data directly (201 Created response)
    if (response.data && typeof response.data === 'object') {
      // If it has invitationId, it's the invitation object itself
      if ('invitationId' in response.data || 'id' in response.data) {
        return {
          success: true,
          data: mapInvitationResponse(response.data),
        }
      }
    }

    // Otherwise, wrap the response data
    return {
      success: true,
      data: mapInvitationResponse(response.data),
    }
  }

  /**
   * Get all invitations sent by an organization
   */
  async getSentInvitations(organizationId: string): Promise<ProviderInvitation[]> {
    const response = await serviceCategoryClient.get<any>(
      `${API_BASE}/${organizationId}/hierarchy/invitations`
    )

    console.log('getSentInvitations raw response:', response.data)

    // Backend returns { data: { organizationId, invitations: [...] } }
    // Extract the invitations array
    if (response.data && response.data.invitations && Array.isArray(response.data.invitations)) {
      return response.data.invitations.map((inv: any) => ({
        ...mapInvitationResponse(inv),
        organizationId: inv.organizationId || organizationId, // Ensure organizationId is set
      }))
    }

    // Fallback: if response.data is already an array
    if (Array.isArray(response.data)) {
      return response.data.map((inv: any) => ({
        ...mapInvitationResponse(inv),
        organizationId: inv.organizationId || organizationId, // Ensure organizationId is set
      }))
    }

    // No invitations found
    return []
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
    const response = await serviceCategoryClient.get<any>(
      `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}`
    )

    // Map the backend response to frontend format
    return mapInvitationResponse(response.data!)
  }

  /**
   * Accept an invitation to join an organization (existing registered user)
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
   * Accept invitation with quick registration (for unregistered users)
   * This endpoint:
   * - Verifies OTP code
   * - Creates user account
   * - Creates individual provider profile
   * - Clones services, hours, and gallery from organization
   * - Accepts the invitation
   * - Returns authentication tokens and cloning statistics
   */
  async acceptInvitationWithRegistration(
    request: AcceptInvitationWithRegistrationRequest
  ): Promise<AcceptInvitationWithRegistrationResponse> {
    const response = await serviceCategoryClient.post<AcceptInvitationWithRegistrationResponse>(
      `${API_BASE}/${request.organizationId}/hierarchy/invitations/${request.invitationId}/accept-with-registration`,
      request
    )
    // Backend returns the data directly, not wrapped
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
   * Reuses the existing send invitation endpoint with the same phone number
   */
  async resendInvitation(
    organizationId: string,
    invitation: ProviderInvitation
  ): Promise<HierarchyApiResponse<ProviderInvitation>> {
    // Reuse the send invitation endpoint with the same details
    const request: SendInvitationRequest = {
      organizationId: organizationId,
      inviteePhoneNumber: invitation.inviteePhoneNumber,
      inviteeName: invitation.inviteeName || '',
      firstName: invitation.inviteeName?.split(' ')[0] || '',
      lastName: invitation.inviteeName?.split(' ').slice(1).join(' ') || '',
      email: undefined,
      message: invitation.message,
    }

    return await this.sendInvitation(organizationId, request)
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
    const response = await serviceCategoryClient.get<any>(
      `${API_BASE}/${organizationId}/hierarchy/join-requests`
    )

    console.log('getReceivedJoinRequests raw response:', response.data)

    // Backend returns { organizationId, joinRequests: [...] }
    // Extract the joinRequests array
    if (response.data && response.data.joinRequests && Array.isArray(response.data.joinRequests)) {
      return response.data.joinRequests as JoinRequest[]
    }

    // Fallback: if it uses 'requests' field name
    if (response.data && response.data.requests && Array.isArray(response.data.requests)) {
      return response.data.requests as JoinRequest[]
    }

    // Fallback: if response.data is already an array
    if (Array.isArray(response.data)) {
      return response.data as JoinRequest[]
    }

    // No requests found
    return []
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
