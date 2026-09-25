// src/modules/provider/services/staff.service.ts

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type {
  Staff,
  CreateStaffRequest,
  UpdateStaffRequest,
} from '../types/staff.types'
import {
  isActiveStatus,
  primaryRole,
  splitName,
  type OrganizationMemberDto,
} from './membership.mapper'

const API_VERSION = 'v1'
const PROVIDERS_BASE = `/${API_VERSION}/providers`
const MEMBERSHIPS_BASE = `/${API_VERSION}/memberships`

/**
 * Staff API client, backed by the MEMBERSHIP model.
 *
 * A staff member is an OrganizationMembership of the salon — never a second Provider.
 * The public method signatures and the `Staff` shape are unchanged so the store and the
 * views above this layer did not have to move at the same time; what changed is which
 * backend model answers them. `staffId` throughout is a MembershipId.
 *
 * Field-level consequences of the model (see membership.mapper.ts):
 *  - `email` is no longer returned or written — it belongs to the person's own account.
 *  - a name can only be written for a member who has no app account; for one who does,
 *    the backend rejects it, because their name is theirs, not the salon's.
 */
class StaffService {
  // ============================================
  // Staff CRUD Operations
  // ============================================

  private toStaff(member: OrganizationMemberDto, providerId: string): Staff {
    const { firstName, lastName } = splitName(member.name)
    return {
      id: member.membershipId,
      providerId,
      firstName,
      lastName,
      fullName: member.name,
      phoneNumber: member.phoneNumber ?? undefined,
      phone: member.phoneNumber ?? undefined,
      email: undefined,
      role: primaryRole(member),
      isActive: isActiveStatus(member.status),
      hiredAt: member.joinedAt ?? undefined,
      biography: member.bioOverride ?? undefined,
      profilePhotoUrl: member.photoUrl ?? undefined,
    }
  }

  /**
   * Get all members of a provider
   * Backend: GET /api/v1/providers/{id}/hierarchy/members
   *
   * Replaces GET /providers/{id}/staff, which read the legacy sub-provider rows and so
   * could not see anyone added through the invitation/membership flow the mobile app uses.
   */
  async getStaffByProvider(providerId: string, activeOnly = false): Promise<Staff[]> {
    try {
      const response = await serviceCategoryClient.get<{ members?: OrganizationMemberDto[] }>(
        `${PROVIDERS_BASE}/${providerId}/hierarchy/members`
      )

      const members = response.data?.members ?? []
      const mapped = members.map((m) => this.toStaff(m, providerId))
      return activeOnly ? mapped.filter((s) => s.isActive) : mapped
    } catch (error) {
      console.error(`[StaffService] Error fetching members for provider ${providerId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Add a member to the salon
   * Backend: POST /api/v1/providers/{id}/staff
   *
   * The route name is legacy but the handler is membership-native: a phone that identifies
   * an existing person links to them, otherwise an unclaimed membership is created carrying
   * the display name, bookable immediately and claimable later by invitation.
   */
  async createStaff(providerId: string, data: CreateStaffRequest): Promise<Staff> {
    try {
      const response = await serviceCategoryClient.post<any>(
        `${PROVIDERS_BASE}/${providerId}/staff`,
        data
      )

      const created = response.data ?? {}
      const membershipId = created.membershipId ?? created.id
      const fullName = created.displayName ?? `${data.firstName} ${data.lastName}`.trim()
      const { firstName, lastName } = splitName(fullName)

      return {
        id: membershipId,
        providerId,
        firstName,
        lastName,
        fullName,
        phoneNumber: data.phoneNumber,
        phone: data.phoneNumber,
        role: data.role ?? 'ServiceProvider',
        isActive: true,
        biography: data.biography,
        profilePhotoUrl: data.profilePhotoUrl,
      }
    } catch (error) {
      console.error(`[StaffService] Error creating member:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Update a member's salon-scoped details
   * Backend: PATCH /api/v1/memberships/{membershipId}
   *
   * Only what the salon owns is sent: display name (rejected by the backend for a member
   * who has their own account), per-salon bio, and photo. Email/phone are person-level and
   * are deliberately not written from here.
   */
  async updateStaff(providerId: string, staffId: string, data: UpdateStaffRequest): Promise<Staff> {
    try {
      const displayName = `${data.firstName ?? ''} ${data.lastName ?? ''}`.trim()

      const response = await serviceCategoryClient.patch<any>(
        `${MEMBERSHIPS_BASE}/${staffId}`,
        {
          displayName: displayName.length > 0 ? displayName : undefined,
          bioOverride: data.biography,
          photoUrl: data.profilePhotoUrl,
        }
      )

      const result = response.data ?? {}
      const fullName = result.displayName ?? displayName
      const { firstName, lastName } = splitName(fullName)

      return {
        id: result.membershipId ?? staffId,
        providerId,
        firstName,
        lastName,
        fullName,
        phoneNumber: data.phoneNumber,
        phone: data.phoneNumber,
        role: data.role ?? (result.roles?.[0] ?? 'ServiceProvider'),
        isActive: true,
        biography: result.bioOverride ?? data.biography,
        profilePhotoUrl: result.photoUrl ?? data.profilePhotoUrl,
      }
    } catch (error) {
      console.error(`[StaffService] Error updating member ${staffId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Remove a member from the salon
   * Backend: POST /api/v1/memberships/{membershipId}/terminate
   *
   * Termination is a lifecycle transition, not a delete: the membership keeps its history
   * (and any bookings attributed to it), and the person keeps their account and can join
   * another salon — or this one again later.
   */
  async deleteStaff(providerId: string, staffId: string): Promise<void> {
    try {
      await serviceCategoryClient.post(`${MEMBERSHIPS_BASE}/${staffId}/terminate`, {
        reason: 'Removed by organization',
      })
    } catch (error) {
      console.error(`[StaffService] Error removing member ${staffId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Upload a member's photo
   * Backend: POST /api/v1/providers/{id}/staff/{membershipId}/photo
   *
   * Stored on the membership's staff profile — an unclaimed member has no personal avatar
   * to fall back on, and for a claimed one it acts as a per-salon override.
   */
  async uploadStaffPhoto(
    providerId: string,
    staffId: string,
    file: File,
    onUploadProgress?: (progressEvent: any) => void
  ): Promise<{ imageUrl: string; thumbnailUrl: string }> {
    try {
      const formData = new FormData()
      formData.append('file', file)

      const response = await serviceCategoryClient.post<{ imageUrl: string; thumbnailUrl: string }>(
        `${PROVIDERS_BASE}/${providerId}/staff/${staffId}/photo`,
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data'
          },
          onUploadProgress
        }
      )

      return response.data!
    } catch (error) {
      console.error(`[StaffService] Error uploading member photo:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Error Handling
  // ============================================

  /**
   * Centralized error handling with support for validation errors
   */
  private handleError(error: unknown): Error {
    // If already an Error with validation info, return as-is
    if (error instanceof Error) {
      // Check if this is already a validation error from HTTP client
      if ((error as any).isValidationError && (error as any).validationErrors) {
        return error
      }
      return error
    }

    return new Error('An unknown error occurred')
  }
}

// Export singleton instance
export const staffService = new StaffService()
export default staffService
