// src/modules/provider/services/membership.mapper.ts

/**
 * Shared mapping between the backend membership model and the two legacy staff shapes
 * the Vue app is built on (`Staff` and `StaffMember`).
 *
 * WHY THIS EXISTS
 * ---------------
 * Staff used to be modelled backend-side as a whole second Provider ("Individual") glued
 * to the salon by ParentProviderId — including, for a while, synthetic accounts with no
 * real identity. That is gone: a person who works at a salon is an OrganizationMembership
 * of it, and never a Provider. See openspec/changes/refactor-identity-and-membership.
 *
 * The Vue components were written against the old shapes, so rather than rewrite ~3,400
 * lines of views at once, the SERVICES were repointed at the membership endpoints and map
 * here. The identifier a component holds (`Staff.id` / `StaffMember.id`) is now a
 * MembershipId, which is exactly what the update/terminate/photo endpoints expect and what
 * bookings are attributed to.
 *
 * Two fields have no membership equivalent, on purpose:
 *  - `email`  — belongs to the Person in UserManagement, not to the salon.
 *  - `specializations` — never modelled; the old read returned null for it too.
 */

/** `OrganizationMemberDto` as returned by GET /v1/providers/{id}/hierarchy/members. */
export interface OrganizationMemberDto {
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
  isUnclaimed?: boolean
}

/** Split a single display name into the first/last pair the legacy shapes expect. */
export function splitName(name: string | null | undefined): { firstName: string; lastName: string } {
  const trimmed = (name ?? '').trim()
  if (!trimmed) return { firstName: '', lastName: '' }
  const index = trimmed.indexOf(' ')
  return index === -1
    ? { firstName: trimmed, lastName: '' }
    : { firstName: trimmed.slice(0, index), lastName: trimmed.slice(index + 1) }
}

/**
 * The single role a legacy consumer displays. A membership carries a SET of roles
 * (an owner who also cuts hair holds both Owner and StaffProvider), so pick the most
 * significant one for display and keep the full set available separately.
 */
export function primaryRole(member: OrganizationMemberDto): string {
  if (member.isOwner) return 'Owner'
  if (member.roles?.includes('Manager')) return 'Manager'
  if (member.roles?.includes('Receptionist')) return 'Receptionist'
  if (member.roles?.includes('StaffProvider')) return 'ServiceProvider'
  return member.roles?.[0] ?? 'Staff'
}

/**
 * Active means "currently part of this salon" — Invited and Suspended members are listed
 * but are not active. Terminated members are not returned by the backend at all.
 */
export function isActiveStatus(status: string | null | undefined): boolean {
  return (status ?? '').toLowerCase() === 'active'
}
