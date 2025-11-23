/**
 * Provider Hierarchy Types
 *
 * Type definitions for provider hierarchy system including organizations,
 * individuals, invitations, and join requests.
 * Based on backend ProviderHierarchy entities and DTOs.
 */

// ============================================================================
// ENUMS
// ============================================================================

/**
 * Provider hierarchy type distinguishing organizations from individuals
 */
export enum ProviderHierarchyType {
  Organization = 'Organization',
  Individual = 'Individual',
}

/**
 * Status of a provider invitation
 */
export enum InvitationStatus {
  Pending = 'Pending',
  Accepted = 'Accepted',
  Rejected = 'Rejected',
  Expired = 'Expired',
}

/**
 * Status of a join request from individual to organization
 */
export enum JoinRequestStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
}

// ============================================================================
// LABEL MAPPINGS
// ============================================================================

export const HIERARCHY_TYPE_LABELS: Record<ProviderHierarchyType, string> = {
  [ProviderHierarchyType.Organization]: 'Organization',
  [ProviderHierarchyType.Individual]: 'Individual',
}

export const INVITATION_STATUS_LABELS: Record<InvitationStatus, string> = {
  [InvitationStatus.Pending]: 'Pending',
  [InvitationStatus.Accepted]: 'Accepted',
  [InvitationStatus.Rejected]: 'Rejected',
  [InvitationStatus.Expired]: 'Expired',
}

export const JOIN_REQUEST_STATUS_LABELS: Record<JoinRequestStatus, string> = {
  [JoinRequestStatus.Pending]: 'Pending',
  [JoinRequestStatus.Approved]: 'Approved',
  [JoinRequestStatus.Rejected]: 'Rejected',
}

// ============================================================================
// CORE ENTITIES
// ============================================================================

/**
 * Organization Provider (Salons/Clinics)
 * Represents physical business location with brand identity
 */
export interface OrganizationProvider {
  id: string
  ownerId: string
  hierarchyType: ProviderHierarchyType.Organization
  isIndependent: false
  parentProviderId: null

  // Business profile
  businessName: string
  description: string
  logoUrl?: string
  coverImageUrl?: string

  // Staff members
  staffMembers?: IndividualProvider[]
  staffCount?: number

  // Status
  status: string
  allowsStaffBookings: boolean

  // Timestamps
  createdAt: string
  lastModifiedAt?: string
}

/**
 * Individual Provider (Professionals)
 * Can be independent (solo freelancer) or linked to Organization
 */
export interface IndividualProvider {
  id: string
  ownerId: string
  hierarchyType: ProviderHierarchyType.Individual
  isIndependent: boolean
  parentProviderId: string | null

  // Personal profile
  firstName?: string
  lastName?: string
  fullName?: string
  bio?: string
  avatarUrl?: string

  // Parent organization (if linked)
  parentOrganization?: {
    id: string
    businessName: string
    logoUrl?: string
  }

  // Professional info
  specializations?: string[]
  services?: string[]

  // Status
  status: string
  canAcceptDirectBookings: boolean

  // Timestamps
  createdAt: string
  lastModifiedAt?: string
}

/**
 * Provider Invitation
 * Organization invites individual to join as staff member
 */
export interface ProviderInvitation {
  id: string
  organizationId: string
  organizationName?: string
  inviteePhoneNumber: string
  inviteeName?: string
  message?: string
  status: InvitationStatus
  createdAt: string
  expiresAt: string
  acceptedAt?: string
  rejectedAt?: string
  respondedBy?: string
}

/**
 * Provider Join Request
 * Individual requests to join an existing organization
 */
export interface ProviderJoinRequest {
  id: string
  organizationId: string
  organizationName?: string
  organizationLogoUrl?: string
  requesterId: string
  requesterName?: string
  requesterAvatarUrl?: string
  message?: string
  status: JoinRequestStatus
  createdAt: string
  reviewedAt?: string
  reviewedBy?: string
  rejectionReason?: string
}

/**
 * Staff Member (Individual Provider working at Organization)
 */
export interface StaffMember {
  id: string
  providerId: string
  firstName: string
  lastName: string
  fullName: string
  email?: string
  phone?: string
  title?: string
  bio?: string
  photoUrl?: string
  isActive: boolean
  specializations: string[]
  servicesCount?: number
  joinedAt?: string
}

// ============================================================================
// PROVIDER HIERARCHY DATA
// ============================================================================

/**
 * Complete provider hierarchy information
 */
export interface ProviderHierarchy {
  provider: OrganizationProvider | IndividualProvider
  parent?: OrganizationProvider
  children?: IndividualProvider[]
  isOrganization: boolean
  isIndividual: boolean
  isIndependent: boolean
  canAddStaff: boolean
}

// ============================================================================
// REQUEST TYPES (Commands)
// ============================================================================

/**
 * Register new organization provider
 */
export interface RegisterOrganizationRequest {
  ownerId: string
  businessName: string
  description: string
  type: string // Business type (Salon, Clinic, etc.)
  email: string
  phone: string
  addressLine1: string
  addressLine2?: string
  city: string
  state: string
  postalCode: string
  country: string
  provinceId?: number
  cityId?: number
  logoUrl?: string
  coverImageUrl?: string
  latitude?: number
  longitude?: number
  allowOnlineBooking?: boolean
  offersMobileServices?: boolean
  tags?: string[]
}

/**
 * Register new independent individual provider
 */
export interface RegisterIndependentIndividualRequest {
  ownerId: string
  firstName: string
  lastName: string
  bio?: string
  email: string
  phone: string
  avatarUrl?: string
  specializations?: string[]
  offersMobileServices?: boolean
  // Service area
  city?: string
  state?: string
  country?: string
  serviceRadius?: number
  latitude?: number
  longitude?: number
}

/**
 * Send invitation to individual to join organization
 */
export interface SendInvitationRequest {
  organizationId: string
  inviteePhoneNumber: string
  inviteeName?: string
  message?: string
}

/**
 * Accept invitation to join organization
 */
export interface AcceptInvitationRequest {
  invitationId: string
  individualProviderId?: string // If already registered
  // If not registered, complete profile
  firstName?: string
  lastName?: string
  email?: string
  bio?: string
  avatarUrl?: string
}

/**
 * Create join request from individual to organization
 */
export interface CreateJoinRequestRequest {
  organizationId: string
  requesterId: string
  message?: string
}

/**
 * Approve join request
 */
export interface ApproveJoinRequestRequest {
  joinRequestId: string
  approvedBy: string
}

/**
 * Reject join request
 */
export interface RejectJoinRequestRequest {
  joinRequestId: string
  rejectedBy: string
  rejectionReason?: string
}

/**
 * Remove staff member from organization
 */
export interface RemoveStaffMemberRequest {
  organizationId: string
  staffMemberId: string
  reason: string
  notes?: string
}

/**
 * Convert individual provider to organization
 */
export interface ConvertToOrganizationRequest {
  individualProviderId: string
  businessName: string
  description: string
  businessType: string
  logoUrl?: string
  coverImageUrl?: string
}

// ============================================================================
// RESPONSE TYPES
// ============================================================================

/**
 * Provider hierarchy response from API
 */
export interface ProviderHierarchyResponse {
  id: string
  hierarchyType: ProviderHierarchyType
  isIndependent: boolean
  parentProviderId: string | null

  // Parent organization info (if applicable)
  parentOrganization?: {
    id: string
    businessName: string
    logoUrl?: string
  }

  // Staff members (if organization)
  staffMembers?: StaffMember[]
  staffCount?: number
}

/**
 * Invitation response from API
 */
export interface InvitationResponse {
  id: string
  organizationId: string
  organizationName: string
  inviteePhoneNumber: string
  inviteeName?: string
  message?: string
  status: InvitationStatus
  createdAt: string
  expiresAt: string
}

/**
 * Join request response from API
 */
export interface JoinRequestResponse {
  id: string
  organizationId: string
  organizationName: string
  requesterId: string
  requesterName: string
  message?: string
  status: JoinRequestStatus
  createdAt: string
  reviewedAt?: string
}

// ============================================================================
// QUERY TYPES
// ============================================================================

/**
 * Query parameters for listing staff members
 */
export interface GetStaffMembersQuery {
  organizationId: string
  isActive?: boolean
  searchTerm?: string
  page?: number
  pageSize?: number
}

/**
 * Query parameters for listing invitations
 */
export interface GetInvitationsQuery {
  organizationId: string
  status?: InvitationStatus
  page?: number
  pageSize?: number
}

/**
 * Query parameters for listing join requests
 */
export interface GetJoinRequestsQuery {
  organizationId?: string
  requesterId?: string
  status?: JoinRequestStatus
  page?: number
  pageSize?: number
}

// ============================================================================
// VIEW MODELS (UI Specific)
// ============================================================================

/**
 * Provider type selection for registration
 */
export interface ProviderTypeOption {
  type: ProviderHierarchyType
  label: string
  description: string
  icon: string
  benefits: string[]
  recommended: boolean
}

/**
 * Staff member card view
 */
export interface StaffMemberCardView {
  id: string
  fullName: string
  email?: string
  phone?: string
  title?: string
  photoUrl?: string
  isActive: boolean
  specializations: string[]
  servicesCount: number
  joinedAt: string
}

/**
 * Invitation card view
 */
export interface InvitationCardView {
  id: string
  inviteeName: string
  phoneNumber: string
  status: InvitationStatus
  statusLabel: string
  createdAt: string
  expiresAt: string
  isExpired: boolean
  canResend: boolean
}

/**
 * Join request card view
 */
export interface JoinRequestCardView {
  id: string
  requesterName: string
  requesterPhoto?: string
  message?: string
  status: JoinRequestStatus
  statusLabel: string
  createdAt: string
  canApprove: boolean
  canReject: boolean
}

// ============================================================================
// UTILITY TYPES
// ============================================================================

/**
 * Provider with hierarchy information
 */
export type ProviderWithHierarchy = {
  hierarchyType: ProviderHierarchyType
  isIndependent: boolean
  parentProviderId: string | null
  parent?: OrganizationProvider
  staffMembers?: IndividualProvider[]
  staffCount?: number
}

/**
 * Status badge variant based on invitation/request status
 */
export type HierarchyStatusBadgeVariant = 'success' | 'warning' | 'danger' | 'info' | 'default'

// ============================================================================
// TYPE GUARDS
// ============================================================================

export function isOrganizationProvider(
  provider: OrganizationProvider | IndividualProvider
): provider is OrganizationProvider {
  return provider.hierarchyType === ProviderHierarchyType.Organization
}

export function isIndividualProvider(
  provider: OrganizationProvider | IndividualProvider
): provider is IndividualProvider {
  return provider.hierarchyType === ProviderHierarchyType.Individual
}

export function isInvitationPending(invitation: ProviderInvitation): boolean {
  return invitation.status === InvitationStatus.Pending && new Date(invitation.expiresAt) > new Date()
}

export function isInvitationExpired(invitation: ProviderInvitation): boolean {
  return invitation.status === InvitationStatus.Pending && new Date(invitation.expiresAt) <= new Date()
}

export function canApproveJoinRequest(request: ProviderJoinRequest): boolean {
  return request.status === JoinRequestStatus.Pending
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Get hierarchy type label
 */
export function getHierarchyTypeLabel(type: ProviderHierarchyType): string {
  return HIERARCHY_TYPE_LABELS[type] || type
}

/**
 * Get invitation status label
 */
export function getInvitationStatusLabel(status: InvitationStatus): string {
  return INVITATION_STATUS_LABELS[status] || status
}

/**
 * Get join request status label
 */
export function getJoinRequestStatusLabel(status: JoinRequestStatus): string {
  return JOIN_REQUEST_STATUS_LABELS[status] || status
}

/**
 * Get status badge variant for invitation
 */
export function getInvitationStatusVariant(status: InvitationStatus): HierarchyStatusBadgeVariant {
  switch (status) {
    case InvitationStatus.Accepted:
      return 'success'
    case InvitationStatus.Pending:
      return 'warning'
    case InvitationStatus.Rejected:
      return 'danger'
    case InvitationStatus.Expired:
      return 'default'
    default:
      return 'info'
  }
}

/**
 * Get status badge variant for join request
 */
export function getJoinRequestStatusVariant(status: JoinRequestStatus): HierarchyStatusBadgeVariant {
  switch (status) {
    case JoinRequestStatus.Approved:
      return 'success'
    case JoinRequestStatus.Pending:
      return 'warning'
    case JoinRequestStatus.Rejected:
      return 'danger'
    default:
      return 'info'
  }
}

/**
 * Format provider hierarchy type for display
 */
export function formatHierarchyType(type: ProviderHierarchyType, isIndependent?: boolean): string {
  if (type === ProviderHierarchyType.Individual && isIndependent) {
    return 'Independent Professional'
  }
  if (type === ProviderHierarchyType.Individual && !isIndependent) {
    return 'Staff Member'
  }
  return 'Business Organization'
}

/**
 * Check if provider can add staff
 */
export function canProviderAddStaff(provider: { hierarchyType: ProviderHierarchyType }): boolean {
  return provider.hierarchyType === ProviderHierarchyType.Organization
}

/**
 * Check if provider can accept direct bookings
 */
export function canAcceptDirectBookings(
  hierarchyType: ProviderHierarchyType,
  isIndependent: boolean,
  hasStaff?: boolean
): boolean {
  // Independent individuals always accept direct bookings
  if (hierarchyType === ProviderHierarchyType.Individual && isIndependent) {
    return true
  }

  // Organizations without staff accept direct bookings
  if (hierarchyType === ProviderHierarchyType.Organization && !hasStaff) {
    return true
  }

  // Staff members (linked individuals) accept direct bookings
  if (hierarchyType === ProviderHierarchyType.Individual && !isIndependent) {
    return true
  }

  // Organizations with staff don't accept direct bookings (customer selects staff)
  return false
}
