// src/modules/provider/services/membership.service.ts

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type {
  InvitationSummary,
  MyMembership,
  OrgMember,
  ProviderInvitation,
} from '../types/membership.types'

const API_VERSION = 'v1'
const MEMBERSHIPS = `/${API_VERSION}/memberships`
const PROVIDERS = `/${API_VERSION}/providers`

/**
 * API client for the membership model.
 *
 * Replaces hierarchy.service, which spoke to the retired provider-hierarchy surface
 * (sub-provider staff reads, join requests, convert-to-organization, and an accept flow
 * that created a second Provider for the invitee). Everything here is membership-native.
 */
class MembershipService {
  // ---------------------------------------------------------------- me

  /** Every salon the signed-in person belongs to, owned or worked at. */
  async getMyMemberships(): Promise<MyMembership[]> {
    const response = await serviceCategoryClient.get<{ memberships?: any[] }>(`${MEMBERSHIPS}/me`)
    return (response.data?.memberships ?? []).map((m) => ({
      membershipId: m.membershipId,
      organizationId: m.organizationId,
      organizationName: m.organizationName ?? '',
      organizationLogo: m.organizationLogo ?? null,
      roles: m.roles ?? [],
      status: m.status ?? '',
      providesServices: !!m.providesServices,
      joinedAt: m.joinedAt ?? null,
      // This endpoint returns the role set rather than a flag; ownership is simply
      // "holds the Owner role at that salon".
      isOwner: (m.roles ?? []).includes('Owner'),
    }))
  }

  // ------------------------------------------------------------- roster

  async getMembers(organizationId: string): Promise<OrgMember[]> {
    const response = await serviceCategoryClient.get<{ members?: OrgMember[] }>(
      `${PROVIDERS}/${organizationId}/hierarchy/members`,
    )
    return response.data?.members ?? []
  }

  /**
   * Remove someone from a salon. Termination is a lifecycle transition, not a delete: the
   * membership keeps its history and any bookings attributed to it, and the person keeps
   * their account.
   */
  async terminate(membershipId: string, reason = 'Removed by organization'): Promise<void> {
    await serviceCategoryClient.post(`${MEMBERSHIPS}/${membershipId}/terminate`, { reason })
  }

  async updateMember(
    membershipId: string,
    payload: {
      displayName?: string
      bioOverride?: string
      providesServices?: boolean
      photoUrl?: string
    },
  ): Promise<void> {
    await serviceCategoryClient.patch(`${MEMBERSHIPS}/${membershipId}`, payload)
  }

  // -------------------------------------------------------- invitations

  async getSentInvitations(organizationId: string): Promise<ProviderInvitation[]> {
    const response = await serviceCategoryClient.get<any>(
      `${PROVIDERS}/${organizationId}/hierarchy/invitations`,
    )
    const list = response.data?.invitations ?? []
    return list.map((i: any) => ({
      id: i.invitationId ?? i.id,
      organizationId: i.organizationId ?? organizationId,
      organizationName: i.organizationName,
      inviteePhoneNumber: i.phoneNumber ?? i.inviteePhoneNumber ?? '',
      inviteeName: i.inviteeName,
      message: i.message,
      status: i.status,
      sentAt: i.createdAt ? new Date(i.createdAt) : undefined,
      expiresAt: i.expiresAt ? new Date(i.expiresAt) : undefined,
    }))
  }

  async sendInvitation(
    organizationId: string,
    inviteePhoneNumber: string,
    inviteeName?: string,
    message?: string,
  ): Promise<ProviderInvitation | null> {
    const response = await serviceCategoryClient.post<any>(
      `${PROVIDERS}/${organizationId}/hierarchy/invitations`,
      { inviteePhoneNumber, inviteeName, message },
    )
    const data = response.data
    if (!data) return null
    return {
      id: data.invitationId ?? data.id,
      organizationId: data.organizationId ?? organizationId,
      inviteePhoneNumber: data.phoneNumber ?? inviteePhoneNumber,
      inviteeName: data.inviteeName ?? inviteeName,
      status: data.status ?? 'Pending',
      sentAt: data.createdAt ? new Date(data.createdAt) : new Date(),
      expiresAt: data.expiresAt ? new Date(data.expiresAt) : undefined,
    }
  }

  /** Withdraw a pending invitation. Closes it (audited); never deletes it. */
  async revokeInvitation(invitationId: string, reason?: string): Promise<void> {
    await serviceCategoryClient.post(`${MEMBERSHIPS}/invitations/${invitationId}/revoke`, { reason })
  }

  /** Public: what an invitee is shown before accepting. Phone is masked by the API. */
  async getInvitationSummary(invitationId: string): Promise<InvitationSummary> {
    const response = await serviceCategoryClient.get<any>(
      `${MEMBERSHIPS}/invitations/${invitationId}`,
    )
    const data = response.data ?? {}
    return {
      invitationId: data.invitationId ?? invitationId,
      organizationId: data.organizationId,
      organizationName: data.organizationName ?? '',
      maskedPhone: data.maskedPhone ?? '',
      inviteeName: data.inviteeName,
      status: data.status ?? '',
      expiresAt: data.expiresAt ? new Date(data.expiresAt) : undefined,
    }
  }

  /** Accept as someone who already has an account. Creates a membership, never a Provider. */
  async acceptInvitation(invitationId: string): Promise<{ membershipId: string; organizationId: string }> {
    const response = await serviceCategoryClient.post<any>(
      `${MEMBERSHIPS}/invitations/${invitationId}/accept`,
    )
    return {
      membershipId: response.data?.membershipId ?? '',
      organizationId: response.data?.organizationId ?? '',
    }
  }

  /** Send the OTP to the phone the invitation was issued to (never one supplied by the client). */
  async sendInvitationOtp(invitationId: string): Promise<{ maskedPhone: string }> {
    const response = await serviceCategoryClient.post<any>(
      `${MEMBERSHIPS}/invitations/${invitationId}/send-otp`,
    )
    return { maskedPhone: response.data?.maskedPhone ?? '' }
  }

  /**
   * Accept as someone with no account yet. Verifies the OTP against the invited phone and
   * REUSES an existing account on that phone rather than creating a second one.
   */
  async registerAndAccept(
    invitationId: string,
    payload: { firstName: string; lastName: string; email?: string; otpCode: string },
  ): Promise<{ membershipId: string; organizationId: string; personId: string; isNewAccount: boolean }> {
    const response = await serviceCategoryClient.post<any>(
      `${MEMBERSHIPS}/invitations/${invitationId}/register-and-accept`,
      payload,
    )
    const data = response.data ?? {}
    return {
      membershipId: data.membershipId ?? '',
      organizationId: data.organizationId ?? '',
      personId: data.personId ?? '',
      isNewAccount: !!data.isNewAccount,
    }
  }
}

export const membershipService = new MembershipService()
export default membershipService
