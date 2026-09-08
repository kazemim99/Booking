// src/modules/provider/stores/membership.store.ts

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { membershipService } from '../services/membership.service'
import type { MyMembership, OrgMember, ProviderInvitation } from '../types/membership.types'

/**
 * The single source of truth for "who is this person, at which salon".
 *
 * Replaces hierarchy.store, which answered the same questions through the retired
 * provider-hierarchy model: it asked whether the signed-in user's OWN Provider was an
 * "Individual" with a parentOrganizationId. Under the membership model a person who works
 * somewhere has no Provider at all — they have a membership. A person may own one salon
 * and hold memberships at others; both come from the same list.
 */
export const useMembershipStore = defineStore('membership', () => {
  // ============================================
  // State
  // ============================================

  /** Every salon this person belongs to, owned or worked at. */
  const myMemberships = ref<MyMembership[]>([])
  /** The roster of the salon currently being managed. */
  const members = ref<OrgMember[]>([])
  const sentInvitations = ref<ProviderInvitation[]>([])

  const loading = ref({ mine: false, members: false, invitations: false })
  const errors = ref<{ mine?: string; members?: string; invitations?: string }>({})

  // ============================================
  // Computed
  // ============================================

  const ownedMemberships = computed(() => myMemberships.value.filter((m) => m.isOwner))
  /**
   * Salons this person works at without running them. This is what "is a staff member"
   * means now — it is not a property of a Provider.
   */
  const staffMemberships = computed(() => myMemberships.value.filter((m) => !m.isOwner))
  const isStaffMember = computed(() => staffMemberships.value.length > 0)
  const isOwner = computed(() => ownedMemberships.value.length > 0)

  /** The salon whose staff workspace is being shown: the first non-owner membership. */
  const activeStaffMembership = computed(() => staffMemberships.value[0] ?? null)

  const activeMembers = computed(() => members.value.filter((m) => m.status === 'Active'))
  const staffCount = computed(() => members.value.length)
  const activeStaffCount = computed(() => activeMembers.value.length)
  const pendingInvitations = computed(() =>
    sentInvitations.value.filter((i) => i.status === 'Pending'),
  )

  // ============================================
  // Actions
  // ============================================

  async function loadMyMemberships(force = false) {
    if (myMemberships.value.length > 0 && !force) return myMemberships.value
    try {
      loading.value.mine = true
      errors.value.mine = undefined
      myMemberships.value = await membershipService.getMyMemberships()
      return myMemberships.value
    } catch (error: any) {
      errors.value.mine = error?.message ?? 'Failed to load memberships'
      throw error
    } finally {
      loading.value.mine = false
    }
  }

  async function loadMembers(organizationId: string) {
    try {
      loading.value.members = true
      errors.value.members = undefined
      members.value = await membershipService.getMembers(organizationId)
      return members.value
    } catch (error: any) {
      errors.value.members = error?.message ?? 'Failed to load members'
      throw error
    } finally {
      loading.value.members = false
    }
  }

  async function removeMember(membershipId: string, reason?: string) {
    await membershipService.terminate(membershipId, reason)
    members.value = members.value.filter((m) => m.membershipId !== membershipId)
  }

  async function loadSentInvitations(organizationId: string) {
    try {
      loading.value.invitations = true
      errors.value.invitations = undefined
      sentInvitations.value = await membershipService.getSentInvitations(organizationId)
      return sentInvitations.value
    } catch (error: any) {
      errors.value.invitations = error?.message ?? 'Failed to load invitations'
      throw error
    } finally {
      loading.value.invitations = false
    }
  }

  async function sendInvitation(organizationId: string, phoneNumber: string, inviteeName?: string) {
    const invitation = await membershipService.sendInvitation(organizationId, phoneNumber, inviteeName)
    if (invitation) sentInvitations.value.unshift(invitation)
    return invitation
  }

  async function resendInvitation(organizationId: string, invitation: ProviderInvitation) {
    return membershipService.sendInvitation(
      organizationId,
      invitation.inviteePhoneNumber,
      invitation.inviteeName,
    )
  }

  async function cancelInvitation(invitationId: string, reason?: string) {
    await membershipService.revokeInvitation(invitationId, reason)
    sentInvitations.value = sentInvitations.value.filter((i) => i.id !== invitationId)
  }

  function getInvitation(invitationId: string) {
    return membershipService.getInvitationSummary(invitationId)
  }

  function acceptInvitation(invitationId: string) {
    return membershipService.acceptInvitation(invitationId)
  }

  function registerAndAcceptInvitation(
    invitationId: string,
    payload: { firstName: string; lastName: string; email?: string; otpCode: string },
  ) {
    return membershipService.registerAndAccept(invitationId, payload)
  }

  function sendInvitationOtp(invitationId: string) {
    return membershipService.sendInvitationOtp(invitationId)
  }

  function reset() {
    myMemberships.value = []
    members.value = []
    sentInvitations.value = []
    errors.value = {}
  }

  return {
    myMemberships,
    members,
    sentInvitations,
    loading,
    errors,
    ownedMemberships,
    staffMemberships,
    isStaffMember,
    isOwner,
    activeStaffMembership,
    activeMembers,
    staffCount,
    activeStaffCount,
    pendingInvitations,
    loadMyMemberships,
    loadMembers,
    removeMember,
    loadSentInvitations,
    sendInvitation,
    resendInvitation,
    cancelInvitation,
    getInvitation,
    acceptInvitation,
    registerAndAcceptInvitation,
    sendInvitationOtp,
    reset,
  }
})
