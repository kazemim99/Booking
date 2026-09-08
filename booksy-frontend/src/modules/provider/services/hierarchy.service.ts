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
  HierarchyType,
} from '../types/hierarchy.types'
import {
  isActiveStatus,
  primaryRole,
  splitName,
  type OrganizationMemberDto,
} from './membership.mapper'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/Providers`

/**
 * Convert relative URL to absolute URL using the API base URL
 */
function toAbsoluteUrl(url: string | undefined): string | undefined {

  if (!url) return url
  if (url.startsWith('http://') || url.startsWith('https://')) return url
  if (url.startsWith('/')) {
    // Get the API base URL from environment or use default
    const apiBaseUrl = import.meta.env.VITE_SERVICE_CATALOG_API_URL || '/api'
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
      const response = await serviceCategoryClient.get<any>('v1/Registration/progress')

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
   * Get the organization's members (the salon's roster).
   * Backend: GET /api/v1/Providers/{id}/hierarchy/members
   *
   * Reads MEMBERSHIPS. It previously read `/hierarchy/staff`, which returns the legacy
   * sub-provider rows — so anyone added through the invitation/membership flow (i.e.
   * everyone added from the mobile app) was invisible here, including in the customer-facing
   * staff picker this feeds. `StaffMember.id` is now a MembershipId, which is what
   * bookings are attributed to and what the update/terminate endpoints expect.
   *
   * Paging is client-side: the members endpoint returns a salon's full roster in one
   * response (rosters are small, and the old endpoint's page/pageSize were not honoured
   * server-side either).
   */
  async getStaffMembers(request: GetStaffMembersRequest): Promise<PagedHierarchyResponse<StaffMember>> {
    const response = await serviceCategoryClient.get<{ members?: OrganizationMemberDto[] }>(
      `${API_BASE}/${request.organizationId}/hierarchy/members`
    )

    const mapMember = (member: OrganizationMemberDto): StaffMember => {
      const { firstName, lastName } = splitName(member.name)
      return {
        id: member.membershipId,
        // A member is not a Provider any more; the only provider in play is the salon.
        providerId: request.organizationId,
        organizationId: request.organizationId,
        firstName,
        lastName,
        fullName: member.name,
        email: undefined,
        phoneNumber: member.phoneNumber ?? undefined,
        photoUrl: member.photoUrl ?? undefined,
        role: primaryRole(member),
        bio: member.bioOverride ?? undefined,
        specializations: [],
        isActive: isActiveStatus(member.status),
        joinedAt: member.joinedAt ? new Date(member.joinedAt) : new Date(),
      }
    }

    let items = (response.data?.members ?? []).map(mapMember)

    if (request.isActive !== undefined) {
      items = items.filter((s) => s.isActive === request.isActive)
    }

    const totalCount = items.length
    const pageSize = request.pageSize || totalCount || 10
    const page = request.page || 1

    if (request.page && request.pageSize) {
      items = items.slice((page - 1) * pageSize, page * pageSize)
    }

    return {
      items,
      totalCount,
      page,
      pageSize,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize),
    }
  }

  /**
   * Remove a member from an organization.
   * Backend: POST /api/v1/memberships/{membershipId}/terminate
   *
   * Termination is a lifecycle transition, not a delete — the membership keeps its history
   * and the person keeps their account. Replaces DELETE /hierarchy/staff/{id}, which
   * un-parented a sub-provider row.
   */
  async removeStaffMember(
    organizationId: string,
    staffId: string,
    reason: string = 'Removed by organization'
  ): Promise<HierarchyApiResponse<void>> {
    await serviceCategoryClient.post(`/${API_VERSION}/memberships/${staffId}/terminate`, { reason })
    return { success: true }
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
   * Accept an invitation to join a salon (person who already has an account).
   * Backend: POST /api/v1/memberships/invitations/{invitationId}/accept
   *
   * Working somewhere does not make you a Provider. The old route
   * (/providers/{myProviderId}/hierarchy/invitations/{id}/accept) required the invitee to
   * ALREADY be an Individual provider and then re-parented that provider row under the
   * salon; someone who was only a customer, or had no provider of their own, could not
   * accept at all. The membership endpoint takes only the invitation and the authenticated
   * person, and creates a membership.
   *
   * `organizationId` is retained in the signature for call-site compatibility and is no
   * longer sent — the invitation already knows which salon it belongs to.
   */
  async acceptInvitation(
    organizationId: string,
    invitationId: string,
    request: AcceptInvitationRequest
  ): Promise<HierarchyApiResponse<{ staffMemberId: string; organizationId: string }>> {
    const response = await serviceCategoryClient.post<any>(
      `/${API_VERSION}/memberships/invitations/${invitationId}/accept`
    )

    const result = response.data ?? {}
    return {
      success: true,
      data: {
        staffMemberId: result.membershipId ?? '',
        organizationId: result.organizationId ?? organizationId,
      },
    }
  }

  /**
   * Accept an invitation as someone who has no account yet.
   * Backend: POST /api/v1/memberships/invitations/{invitationId}/register-and-accept
   *
   * Verifies the OTP against the phone the invitation was sent to, resolves that phone to
   * a person (REUSING an existing account if one already exists on it, never creating a
   * second), and creates the membership.
   *
   * Replaces .../hierarchy/invitations/{id}/accept-with-registration, whose handler ran a
   * saga that created a brand-new User AND a brand-new Individual Provider per invitee --
   * with manual compensating deletes when a step failed -- and cloned the salon's services,
   * hours and gallery onto that shadow provider. That is the synthetic-provider model this
   * migration exists to remove: an employee is a membership of the salon, so there is
   * nothing to clone and no second provider to create.
   *
   * Consequences for callers: no tokens are returned (the new member signs in with their
   * own phone via the normal OTP flow), and the cloning statistics are always zero because
   * nothing is cloned any more.
   */
  async acceptInvitationWithRegistration(
    request: AcceptInvitationWithRegistrationRequest
  ): Promise<AcceptInvitationWithRegistrationResponse> {
    const response = await serviceCategoryClient.post<any>(
      `/${API_VERSION}/memberships/invitations/${request.invitationId}/register-and-accept`,
      {
        firstName: request.firstName,
        lastName: request.lastName,
        email: request.email,
        otpCode: request.otpCode,
      }
    )

    const result = response.data ?? {}
    return {
      userId: result.personId ?? '',
      // The membership, not a provider — there is no second provider any more. Kept under
      // the existing field name so callers that only pass it along keep compiling.
      providerId: result.membershipId ?? '',
      // The new member signs in with their own phone through the normal OTP flow; this
      // endpoint deliberately does not mint a session.
      accessToken: '',
      refreshToken: '',
      // Nothing is cloned any more: a member works from the salon's own services and hours.
      clonedServicesCount: 0,
      clonedWorkingHoursCount: 0,
      clonedGalleryCount: 0,
    }
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
