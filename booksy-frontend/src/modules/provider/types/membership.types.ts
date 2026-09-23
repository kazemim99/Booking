// src/modules/provider/types/membership.types.ts

/**
 * The membership model, as the API returns it.
 *
 * Replaces hierarchy.types, which described the retired provider-hierarchy model
 * (Organization vs Individual providers, parentOrganizationId, join requests,
 * convert-to-organization). None of those are concepts in this product: a salon is a
 * Provider, and a person who works there is a membership of it.
 */

/** One salon this person belongs to — `GET /v1/memberships/me`. */
export interface MyMembership {
  membershipId: string
  organizationId: string
  organizationName: string
  organizationLogo?: string | null
  roles: string[]
  status: string
  providesServices: boolean
  joinedAt?: string | null
  /** Derived client-side from `roles`; the API sends roles, not a flag, on this endpoint. */
  isOwner: boolean
}

/** One person on a salon's roster — `GET /v1/providers/{id}/hierarchy/members`. */
export interface OrgMember {
  membershipId: string
  personId?: string | null
  name: string
  phoneNumber?: string | null
  roles: string[]
  status: string
  isOwner: boolean
  providesServices: boolean
  joinedAt?: string | null
  bioOverride?: string | null
  photoUrl?: string | null
  /** No app account yet: the salon manages them, and they can claim the membership later. */
  isUnclaimed?: boolean
}

export interface ProviderInvitation {
  id: string
  organizationId: string
  organizationName?: string
  inviteePhoneNumber: string
  inviteeName?: string
  message?: string
  status: string
  sentAt?: Date
  expiresAt?: Date
  organizationLogo?: string | null
}

/** What an invitee is shown before accepting — deliberately minimal (masked phone). */
export interface InvitationSummary {
  invitationId: string
  organizationId: string
  organizationName: string
  /** The salon's own photo, absolute; null when it has none. */
  organizationLogo?: string | null
  maskedPhone: string
  inviteeName?: string
  status: string
  expiresAt?: Date
}
