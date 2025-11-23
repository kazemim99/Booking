// src/modules/provider/services/hierarchy.service.ts

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type {
  ProviderHierarchyResponse,
  InvitationResponse,
  JoinRequestResponse,
  RegisterOrganizationRequest,
  RegisterIndependentIndividualRequest,
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
  StaffMember,
  ProviderInvitation,
  ProviderJoinRequest,
} from '../types/hierarchy.types'
import type { PagedResult } from '@/core/types/common.types'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/providers`

/**
 * Provider Hierarchy Service
 * Handles all hierarchy-related API operations including organizations,
 * individuals, invitations, and join requests.
 */
class HierarchyService {
  // ============================================
  // Registration Endpoints
  // ============================================

  /**
   * Register a new organization provider (salon, clinic, etc.)
   */
  async registerOrganization(request: RegisterOrganizationRequest): Promise<ProviderHierarchyResponse> {
    try {
      const response = await serviceCategoryClient.post<ProviderHierarchyResponse>(
        `${API_BASE}/organizations`,
        request,
      )
      return response.data!
    } catch (error) {
      console.error('Error registering organization:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Register a new independent individual provider (freelancer, mobile professional)
   */
  async registerIndividual(
    request: RegisterIndependentIndividualRequest,
  ): Promise<ProviderHierarchyResponse> {
    try {
      const response = await serviceCategoryClient.post<ProviderHierarchyResponse>(
        `${API_BASE}/individuals`,
        request,
      )
      return response.data!
    } catch (error) {
      console.error('Error registering individual:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Hierarchy Query Endpoints
  // ============================================

  /**
   * Get provider hierarchy information (parent and staff)
   */
  async getProviderHierarchy(providerId: string): Promise<ProviderHierarchyResponse> {
    try {
      const response = await serviceCategoryClient.get<ProviderHierarchyResponse>(
        `${API_BASE}/${providerId}/hierarchy`,
      )
      return response.data!
    } catch (error) {
      console.error(`Error fetching hierarchy for provider ${providerId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Get all staff members of an organization
   */
  async getStaffMembers(query: GetStaffMembersQuery): Promise<PagedResult<StaffMember>> {
    try {
      const { organizationId, ...params } = query
      const response = await serviceCategoryClient.get<PagedResult<StaffMember>>(
        `${API_BASE}/${organizationId}/hierarchy/staff`,
        { params },
      )
      return response.data!
    } catch (error) {
      console.error(`Error fetching staff for organization ${query.organizationId}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Invitation Endpoints
  // ============================================

  /**
   * Send invitation to individual to join organization
   */
  async sendInvitation(
    organizationId: string,
    request: SendInvitationRequest,
  ): Promise<InvitationResponse> {
    try {
      const response = await serviceCategoryClient.post<InvitationResponse>(
        `${API_BASE}/${organizationId}/hierarchy/invitations`,
        request,
      )
      return response.data!
    } catch (error) {
      console.error(`Error sending invitation from organization ${organizationId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Accept invitation to join organization
   */
  async acceptInvitation(
    organizationId: string,
    invitationId: string,
    request: AcceptInvitationRequest,
  ): Promise<void> {
    try {
      await serviceCategoryClient.post(
        `${API_BASE}/${organizationId}/hierarchy/invitations/${invitationId}/accept`,
        request,
      )
    } catch (error) {
      console.error(`Error accepting invitation ${invitationId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Get pending invitations for an organization
   */
  async getPendingInvitations(query: GetInvitationsQuery): Promise<PagedResult<ProviderInvitation>> {
    try {
      const { organizationId, ...params } = query
      const response = await serviceCategoryClient.get<PagedResult<ProviderInvitation>>(
        `${API_BASE}/${organizationId}/hierarchy/invitations`,
        { params },
      )
      return response.data!
    } catch (error) {
      console.error(`Error fetching invitations for organization ${query.organizationId}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Join Request Endpoints
  // ============================================

  /**
   * Create join request from individual to organization
   */
  async createJoinRequest(
    organizationId: string,
    request: CreateJoinRequestRequest,
  ): Promise<JoinRequestResponse> {
    try {
      const response = await serviceCategoryClient.post<JoinRequestResponse>(
        `${API_BASE}/${organizationId}/hierarchy/join-requests`,
        request,
      )
      return response.data!
    } catch (error) {
      console.error(`Error creating join request to organization ${organizationId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Get join requests (for organization owner or individual requester)
   */
  async getJoinRequests(query: GetJoinRequestsQuery): Promise<PagedResult<ProviderJoinRequest>> {
    try {
      const organizationId = query.organizationId
      if (!organizationId) {
        throw new Error('Organization ID is required')
      }

      const { organizationId: _, ...params } = query
      const response = await serviceCategoryClient.get<PagedResult<ProviderJoinRequest>>(
        `${API_BASE}/${organizationId}/hierarchy/join-requests`,
        { params },
      )
      return response.data!
    } catch (error) {
      console.error('Error fetching join requests:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Approve join request
   */
  async approveJoinRequest(
    organizationId: string,
    requestId: string,
    request: ApproveJoinRequestRequest,
  ): Promise<void> {
    try {
      await serviceCategoryClient.post(
        `${API_BASE}/${organizationId}/hierarchy/join-requests/${requestId}/approve`,
        request,
      )
    } catch (error) {
      console.error(`Error approving join request ${requestId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Reject join request
   */
  async rejectJoinRequest(
    organizationId: string,
    requestId: string,
    request: RejectJoinRequestRequest,
  ): Promise<void> {
    try {
      await serviceCategoryClient.post(
        `${API_BASE}/${organizationId}/hierarchy/join-requests/${requestId}/reject`,
        request,
      )
    } catch (error) {
      console.error(`Error rejecting join request ${requestId}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Staff Management Endpoints
  // ============================================

  /**
   * Remove staff member from organization
   */
  async removeStaffMember(
    organizationId: string,
    staffId: string,
    request: RemoveStaffMemberRequest,
  ): Promise<void> {
    try {
      await serviceCategoryClient.delete(`${API_BASE}/${organizationId}/hierarchy/staff/${staffId}`, {
        data: request,
      })
    } catch (error) {
      console.error(`Error removing staff member ${staffId} from organization ${organizationId}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Conversion Endpoints
  // ============================================

  /**
   * Convert individual provider to organization
   */
  async convertToOrganization(
    providerId: string,
    request: ConvertToOrganizationRequest,
  ): Promise<ProviderHierarchyResponse> {
    try {
      const response = await serviceCategoryClient.post<ProviderHierarchyResponse>(
        `${API_BASE}/${providerId}/hierarchy/convert-to-organization`,
        request,
      )
      return response.data!
    } catch (error) {
      console.error(`Error converting provider ${providerId} to organization:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Utility Methods
  // ============================================

  /**
   * Handle API errors consistently
   */
  private handleError(error: unknown): Error {
    if (error instanceof Error) {
      return error
    }
    return new Error('An unknown error occurred')
  }
}

// Export singleton instance
export const hierarchyService = new HierarchyService()
export default hierarchyService
