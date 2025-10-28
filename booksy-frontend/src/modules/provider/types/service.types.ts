// Service Status
export enum ServiceStatus {
  Draft = 'Draft',
  Active = 'Active',
  Inactive = 'Inactive',
  Archived = 'Archived',
}

// Service Category
export enum ServiceCategory {
  HairCare = 'HairCare',
  Nails = 'Nails',
  Massage = 'Massage',
  Facial = 'Facial',
  Waxing = 'Waxing',
  Makeup = 'Makeup',
  Spa = 'Spa',
  Fitness = 'Fitness',
  Consulting = 'Consulting',
  Other = 'Other',
}

// Service Type
export enum ServiceType {
  Standard = 'Standard',
  Package = 'Package',
  Membership = 'Membership',
}

// ============================================
// Core Service Model
// ============================================

export interface Service {
  id: string
  providerId: string

  // Basic Information
  name: string
  description: string
  category: ServiceCategory
  type: ServiceType

  // Pricing
  basePrice: number
  currency: string

  // Duration (in minutes)
  duration: number
  preparationTime?: number
  bufferTime?: number

  // Status & Visibility
  status: ServiceStatus
  allowOnlineBooking: boolean
  availableAtLocation: boolean
  availableAsMobile: boolean

  // Booking Rules
  maxAdvanceBookingDays: number
  minAdvanceBookingHours: number
  requiresDeposit: boolean
  depositPercentage?: number

  // Media
  imageUrl?: string
  images?: string[]

  // Staff
  qualifiedStaffIds: string[]

  // Metadata
  tags: string[]
  displayOrder: number

  // Timestamps
  createdAt: string
  lastModifiedAt?: string
  activatedAt?: string
}

// ============================================
// Service Summary (for lists)
// ============================================

export interface ServiceSummary {
  id: string
  providerId: string
  name: string
  description: string
  category: ServiceCategory
  basePrice: number
  currency: string
  duration: number
  status: ServiceStatus
  imageUrl?: string
  tags: string[]
  displayOrder: number
}

// ============================================
// Service Options & Add-ons
// ============================================

export interface ServiceOption {
  id: string
  serviceId: string
  name: string
  description?: string
  priceModifier: number
  durationModifier: number
  isRequired: boolean
  displayOrder: number
}

export interface PriceTier {
  id: string
  serviceId: string
  name: string
  description?: string
  price: number
  conditions?: string
}

// ============================================
// Request/Command Models
// ============================================

// Backend-compatible request that matches AddProviderServiceCommand
export interface CreateServiceRequest {
  providerId: string // Used for routing, not in body
  serviceName: string // Changed from 'name' to match backend
  description?: string
  durationHours: number // Split duration into hours
  durationMinutes: number // and minutes to match backend
  price: number // Changed from 'basePrice' to match backend
  currency?: string
  category?: string
  isMobileService?: boolean
}

// Extended request for frontend form (includes all optional fields)
export interface CreateServiceRequestExtended extends CreateServiceRequest {
  type?: ServiceType
  preparationTime?: number
  bufferTime?: number
  allowOnlineBooking?: boolean
  availableAtLocation?: boolean
  maxAdvanceBookingDays?: number
  minAdvanceBookingHours?: number
  requiresDeposit?: boolean
  depositPercentage?: number
  imageUrl?: string
  tags?: string[]
}

// Backend-compatible update request that matches UpdateProviderServiceRequest
export interface UpdateServiceRequest {
  providerId: string // Used for routing, not in body
  serviceName?: string // Changed from 'name' to match backend
  description?: string
  durationHours?: number // Split duration into hours
  durationMinutes?: number // and minutes to match backend
  price?: number // Changed from 'basePrice' to match backend
  currency?: string
  category?: string
  isMobileService?: boolean
}

// Extended update request for frontend (includes all optional fields)
export interface UpdateServiceRequestExtended extends Omit<UpdateServiceRequest, 'providerId'> {
  type?: ServiceType
  preparationTime?: number
  bufferTime?: number
  allowOnlineBooking?: boolean
  availableAtLocation?: boolean
  maxAdvanceBookingDays?: number
  minAdvanceBookingHours?: number
  requiresDeposit?: boolean
  depositPercentage?: number
  imageUrl?: string
  tags?: string[]
  qualifiedStaffIds?: string[]
  displayOrder?: number
}

export interface ServiceFilters {
  searchTerm?: string
  category?: ServiceCategory
  status?: ServiceStatus
  minPrice?: number
  maxPrice?: number
  tags?: string[]
}

// ============================================
// Form Models
// ============================================

export interface ServiceFormData {
  name: string
  description: string
  category: ServiceCategory
  type: ServiceType
  basePrice: number | string
  currency: string
  duration: number | string
  preparationTime: number | string
  bufferTime: number | string
  allowOnlineBooking: boolean
  availableAtLocation: boolean
  availableAsMobile: boolean
  maxAdvanceBookingDays: number | string
  minAdvanceBookingHours: number | string
  requiresDeposit: boolean
  depositPercentage: number | string
  imageUrl: string
  tags: string
  qualifiedStaffIds: string[]
}

// ============================================
// Validation Errors
// ============================================

export interface ServiceValidationErrors {
  name?: string
  description?: string
  category?: string
  basePrice?: string
  duration?: string
  preparationTime?: string
  bufferTime?: string
  maxAdvanceBookingDays?: string
  minAdvanceBookingHours?: string
  depositPercentage?: string
  imageUrl?: string
}

// ============================================
// Category Labels (for UI)
// ============================================

export const SERVICE_CATEGORY_LABELS: Record<ServiceCategory, string> = {
  [ServiceCategory.HairCare]: 'Hair Care',
  [ServiceCategory.Nails]: 'Nails',
  [ServiceCategory.Massage]: 'Massage',
  [ServiceCategory.Facial]: 'Facial',
  [ServiceCategory.Waxing]: 'Waxing',
  [ServiceCategory.Makeup]: 'Makeup',
  [ServiceCategory.Spa]: 'Spa',
  [ServiceCategory.Fitness]: 'Fitness',
  [ServiceCategory.Consulting]: 'Consulting',
  [ServiceCategory.Other]: 'Other',
}

export const SERVICE_STATUS_LABELS: Record<ServiceStatus, string> = {
  [ServiceStatus.Draft]: 'Draft',
  [ServiceStatus.Active]: 'Active',
  [ServiceStatus.Inactive]: 'Inactive',
  [ServiceStatus.Archived]: 'Archived',
}

// ============================================
// Helper Functions
// ============================================

/**
 * Convert form data to backend-compatible CreateServiceRequest
 * Converts total duration minutes to hours + minutes format
 */
export function toCreateServiceRequest(
  providerId: string,
  formData: Partial<ServiceFormData> | { name: string; description: string; duration: number; basePrice: number; category: ServiceCategory; currency?: string }
): CreateServiceRequest {
  // Handle duration - convert total minutes to hours + minutes
  const totalMinutes = typeof formData.duration === 'string'
    ? parseInt(formData.duration, 10)
    : (formData.duration || 0)

  const durationHours = Math.floor(totalMinutes / 60)
  const durationMinutes = totalMinutes % 60

  // Map frontend property names to backend property names
  return {
    providerId,
    serviceName: formData.name || '', // name → serviceName
    description: formData.description,
    durationHours,
    durationMinutes,
    price: typeof formData.basePrice === 'string' // basePrice → price
      ? parseFloat(formData.basePrice)
      : (formData.basePrice || 0),
    currency: formData.currency || 'USD',
    category: formData.category,
    isMobileService: (formData as ServiceFormData).availableAsMobile || false,
  }
}
