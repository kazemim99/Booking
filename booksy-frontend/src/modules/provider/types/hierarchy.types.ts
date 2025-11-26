// booksy-frontend/src/modules/provider/types/hierarchy.types.ts

/**
 * Provider Hierarchy Types
 * Defines all types for the provider hierarchy feature including
 * Organizations, Individuals, Invitations, and Join Requests
 */

// ============================================================================
// ENUMS
// ============================================================================

export enum HierarchyType {
  Organization = 'Organization',
  Individual = 'Individual',
}

export enum InvitationStatus {
  Pending = 'Pending',
  Accepted = 'Accepted',
  Rejected = 'Rejected',
  Expired = 'Expired',
  Cancelled = 'Cancelled',
}

export enum JoinRequestStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Cancelled = 'Cancelled',
}

export enum ProviderBusinessType {
  Salon = 'Salon',
  Barbershop = 'Barbershop',
  SpaWellness = 'SpaWellness',
  Clinic = 'Clinic',
  BeautySalon = 'BeautySalon',
  Other = 'Other',
}

// Alias for backward compatibility
export const ProviderHierarchyType = HierarchyType

// ============================================================================
// REQUEST TYPES
// ============================================================================

export interface RegisterOrganizationRequest {
  businessName: string
  businessDescription: string
  category: string
  phoneNumber: string
  email: string
  addressLine1: string
  addressLine2?: string
  city: string
  province: string
  postalCode: string
  latitude: number
  longitude: number
  ownerFirstName: string
  ownerLastName: string
  logoUrl?: string
  idempotencyKey?: string
}

export interface RegisterIndependentIndividualRequest {
  ownerId: string
  firstName: string
  lastName: string
  bio?: string
  email: string
  phone: string
  city: string
  state: string
  country: string
  photoUrl?: string
}

export interface SendInvitationRequest {
  organizationId: string
  inviteePhoneNumber: string
  inviteeName: string
  message?: string
  expiresInHours?: number // default 72
}

export interface AcceptInvitationRequest {
  invitationId: string
  profileData?: {
    firstName?: string
    lastName?: string
    bio?: string
    photoUrl?: string
  }
}

export interface CreateJoinRequestRequest {
  organizationId: string
  requesterId: string
  message?: string
}

export interface ConvertToOrganizationRequest {
  individualProviderId: string
  businessName: string
  description?: string
  businessType: ProviderBusinessType
  logoUrl?: string
}

export interface GetStaffMembersRequest {
  organizationId: string
  isActive?: boolean
  page?: number
  pageSize?: number
}

// ============================================================================
// RESPONSE TYPES
// ============================================================================

export interface ProviderInvitation {
  id: string
  organizationId: string
  organizationName: string
  organizationLogo?: string
  organizationType?: ProviderBusinessType
  inviteePhoneNumber: string
  inviteeName: string
  message?: string
  status: InvitationStatus
  sentAt: Date
  expiresAt: Date
  respondedAt?: Date
  acceptedByProviderId?: string
  createdBy?: string
  createdByName?: string
}

export interface JoinRequest {
  id: string
  organizationId: string
  organizationName: string
  organizationLogo?: string
  organizationType?: ProviderBusinessType
  requesterId: string
  requesterName: string
  requesterPhone?: string
  requesterEmail?: string
  message?: string
  status: JoinRequestStatus
  createdAt: Date
  respondedAt?: Date
  respondedBy?: string
  respondedByName?: string
  rejectionReason?: string
}

export interface StaffMember {
  id: string
  providerId: string
  organizationId: string
  firstName: string
  lastName: string
  fullName: string
  email?: string
  phoneNumber?: string
  photoUrl?: string
  role: string
  title?: string
  bio?: string
  specializations?: string[]
  isActive: boolean
  joinedAt: Date
  leftAt?: Date
}

export interface ProviderHierarchyDetails {
  provider: {
    id: string
    hierarchyType: HierarchyType
    // For Organizations
    businessName?: string
    businessType?: ProviderBusinessType
    description?: string
    logoUrl?: string
    // For Individuals
    firstName?: string
    lastName?: string
    fullName?: string
    bio?: string
    photoUrl?: string
    // Relationships
    parentOrganizationId?: string
    staffCount?: number
    activeStaffCount?: number
  }
  staff?: StaffMember[]
  parentOrganization?: {
    id: string
    businessName: string
    businessType?: ProviderBusinessType
    logoUrl?: string
    city?: string
    state?: string
  }
  organizationOwner?: {
    id: string
    firstName: string
    lastName: string
    fullName: string
  }
}

export interface OrganizationSummary {
  id: string
  businessName: string
  businessType: ProviderBusinessType
  description?: string
  logoUrl?: string
  city: string
  state: string
  country: string
  staffCount: number
  activeStaffCount: number
  allowsJoinRequests: boolean
  isVerified: boolean
  averageRating?: number
}

// ============================================================================
// API RESPONSE WRAPPERS
// ============================================================================

export interface HierarchyApiResponse<T> {
  success: boolean
  data?: T
  message?: string
  errorCode?: string
}

export interface PagedHierarchyResponse<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// ============================================================================
// STORE STATE TYPES
// ============================================================================

export interface HierarchyState {
  // Current provider hierarchy
  currentHierarchy: ProviderHierarchyDetails | null

  // Invitations
  sentInvitations: ProviderInvitation[]
  receivedInvitations: ProviderInvitation[]

  // Join Requests
  sentJoinRequests: JoinRequest[]
  receivedJoinRequests: JoinRequest[]

  // Staff
  staffMembers: StaffMember[]

  // Organizations for search
  availableOrganizations: OrganizationSummary[]

  // Loading states
  loading: {
    hierarchy: boolean
    invitations: boolean
    joinRequests: boolean
    staff: boolean
    organizations: boolean
  }

  // Error states
  errors: {
    hierarchy?: string
    invitations?: string
    joinRequests?: string
    staff?: string
    organizations?: string
  }
}

// ============================================================================
// UTILITY TYPES
// ============================================================================

export interface InvitationFilters {
  status?: InvitationStatus
  startDate?: Date
  endDate?: Date
}

export interface JoinRequestFilters {
  status?: JoinRequestStatus
  startDate?: Date
  endDate?: Date
}

export interface OrganizationSearchFilters {
  city?: string
  state?: string
  businessType?: ProviderBusinessType
  minStaffCount?: number
  verifiedOnly?: boolean
  searchTerm?: string
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

export function getJoinRequestStatusLabel(status: JoinRequestStatus): string {
  const labels: Record<JoinRequestStatus, string> = {
    [JoinRequestStatus.Pending]: 'در انتظار',
    [JoinRequestStatus.Approved]: 'تایید شده',
    [JoinRequestStatus.Rejected]: 'رد شده',
    [JoinRequestStatus.Cancelled]: 'لغو شده',
  }
  return labels[status] || status
}

export function canApproveJoinRequest(request: JoinRequest): boolean {
  return request.status === JoinRequestStatus.Pending
}
