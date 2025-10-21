export enum ProviderStatus {
  Drafted = 'Drafted',
  PendingVerification = 'PendingVerification',
  Verified = 'Verified',
  Active = 'Active',
  Inactive = 'Inactive',
  Suspended = 'Suspended',
  Archived = 'Archived',
}

export enum ProviderType {
  Individual = 'Individual',
  Salon = 'Salon',
  Clinic = 'Clinic',
  Spa = 'Spa',
  Studio = 'Studio',
  Professional = 'Professional',
}

// ============================================
// Core Domain Models
// ============================================

export interface Provider {
  id: string
  ownerId: string

  // Profile
  profile: BusinessProfile

  // Status & Type
  status: ProviderStatus
  type: ProviderType

  // Contact Information
  contactInfo: ContactInfo

  // Address
  address: BusinessAddress

  // Settings
  requiresApproval: boolean
  allowOnlineBooking: boolean
  offersMobileServices: boolean

  // Collections
  tags: string[]
  services?: ServiceSummary[]
  staff?: StaffMember[]
  businessHours?: BusinessHours[]

  // Timestamps
  registeredAt: string
  activatedAt?: string
  verifiedAt?: string
  lastActiveAt?: string
  createdAt: string
  lastModifiedAt?: string
}

export interface BusinessProfile {
  businessName: string
  description: string
  logoUrl?: string
  coverImageUrl?: string
  websiteUrl?: string
  socialMediaLinks?: SocialMediaLinks
}

export interface ContactInfo {
  email: string
  primaryPhone: string
  secondaryPhone?: string
  faxNumber?: string
}

export interface BusinessAddress {
  addressLine1: string
  addressLine2?: string
  city: string
  state: string
  postalCode: string
  country: string
  latitude?: number
  longitude?: number
}

export interface SocialMediaLinks {
  facebook?: string
  instagram?: string
  twitter?: string
  linkedin?: string
}

export interface BusinessHours {
  id: string
  dayOfWeek: DayOfWeek
  openTime: string
  closeTime: string
  isOpen: boolean
  breaks?: TimeBreak[]
}

export interface TimeBreak {
  startTime: string
  endTime: string
}

export enum DayOfWeek {
  Monday = 0,
  Tuesday = 1,
  Wednesday = 2,
  Thursday = 3,
  Friday = 4,
  Saturday = 5,
  Sunday = 6,
}

// ============================================
// Summary & List Models
// ============================================

export interface ProviderSummary {
  id: string
  businessName: string
  description: string
  type: ProviderType
  status: ProviderStatus
  logoUrl?: string
  city: string
  state: string
  country: string
  allowOnlineBooking: boolean
  offersMobileServices: boolean
  tags: string[]
  registeredAt: string
  lastActiveAt?: string
}

export interface ServiceSummary {
  id: string
  providerId: string
  name: string
  description: string
  category: string
  type: string
  basePrice: number
  currency: string
  duration: number
  status: string
  imageUrl?: string
  tags: string[]
}

export interface StaffMember {
  id: string
  providerId: string
  firstName: string
  lastName: string
  email: string
  phone: string
  title?: string
  bio?: string
  photoUrl?: string
  isActive: boolean
  specializations: string[]
}

// ============================================
// Search & Filter Models
// ============================================

export interface PaginationParams {
  pageNumber: number
  pageSize: number
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

export interface ProviderSearchFilters extends PaginationParams {
  searchTerm?: string
  city?: string
  state?: string
  country?: string
  type?: ProviderType
  status?: ProviderStatus
  offersMobileServices?: boolean
  allowOnlineBooking?: boolean
  tags?: string[]
  latitude?: number
  longitude?: number
  radiusKm?: number
  minRating?: number
}

// ============================================
// Command Models (Requests)
// ============================================

export interface RegisterProviderRequest {
  ownerId: string
  businessName: string
  description: string
  type: ProviderType
  email: string
  primaryPhone: string
  secondaryPhone?: string
  websiteUrl?: string
  addressLine1: string
  addressLine2?: string
  logoUrl?: string
  coverImageUrl?: string
  city: string
  state: string
  postalCode: string
  country: string
  requiresApproval: boolean
  allowOnlineBooking: boolean
  offersMobileServices: boolean
  latitude: number
  longitude: number
  tags?: string[]
}

export interface UpdateProviderRequest {
  businessName?: string
  description?: string
  logoUrl?: string
  coverImageUrl?: string
  websiteUrl?: string
  email?: string
  primaryPhone?: string
  secondaryPhone?: string
  addressLine1?: string
  addressLine2?: string
  city?: string
  state?: string
  postalCode?: string
  country?: string
  allowOnlineBooking?: boolean
  offersMobileServices?: boolean
  tags?: string[]
  businessHours?: BusinessHours[]
  services?: ServiceSummary[]
}

export interface ActivateProviderRequest {
  notes?: string
}

export interface DeactivateProviderRequest {
  reason: string
  notes?: string
}

// ============================================
// Statistics & Analytics
// ============================================

export interface ProviderStatistics {
  providerId: string
  businessName: string
  totalServices: number
  activeServices: number
  totalStaff: number
  activeStaff: number
  totalBookings: number
  completedBookings: number
  cancelledBookings: number
  totalRevenue: number
  averageRating?: number
  totalReviews: number
  registeredAt: string
}

// ============================================
// View Models (UI Specific)
// ============================================

export interface ProviderCardData {
  id: string
  businessName: string
  description: string
  type: string
  logoUrl?: string
  city: string
  state: string
  rating?: number
  reviewCount?: number
  tags: string[]
  allowOnlineBooking: boolean
  offersMobileServices: boolean
  distance?: number
}

export interface ProviderListViewMode {
  mode: 'grid' | 'list' | 'map'
}

// ============================================
// API Response Models
// ============================================

export interface ProviderResponse {
  id: string
  ownerId: string
  businessName: string
  description: string
  status: string
  type: string
  logoUrl?: string
  coverImageUrl?: string
  email: string
  primaryPhone: string
  secondaryPhone?: string
  websiteUrl?: string
  addressLine1: string
  addressLine2?: string
  city: string
  state: string
  postalCode: string
  country: string
  latitude?: number
  longitude?: number
  allowOnlineBooking: boolean
  offersMobileServices: boolean
  requiresApproval: boolean
  tags: string[]
  registeredAt: string
  activatedAt?: string
  verifiedAt?: string
  lastActiveAt?: string
  services?: ServiceSummary[]
  staff?: StaffMember[]
  businessHours?: BusinessHours[]
}

// ============================================
// Utility Types
// ============================================

export type ProviderFormData = Omit<RegisterProviderRequest, 'tags'> & {
  tags: string
}

export type ProviderStatusBadgeVariant = 'success' | 'warning' | 'danger' | 'info' | 'default'

export interface ProviderValidationError {
  field: string
  message: string
}
