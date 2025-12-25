/**
 * Entity Type Definitions
 * Domain entity interfaces for the Booksy application
 */

import type {
  ID,
  Timestamp,
  AuditFields,
  Address,
  Coordinates,
  Money,
  Price,
} from './common.types'
import type {
  UserRole,
  UserStatus,
  Gender,
  ProviderCategory,
  ProviderType,
  ProviderStatus,
  StaffRole,
  StaffStatus,
  ServiceCategory,
  ServiceStatus,
  PricingType,
  BookingStatus,
  PaymentStatus,
  PaymentMethod,
  PaymentProvider,
  WeekDay,
  NotificationType,
  NotificationChannel,
  NotificationStatus,
  ReviewStatus,
  Language,
  Currency,
  MediaType,
} from './enums.types'

// ==================== Base Entity ====================

/**
 * Base entity with ID and audit fields
 */
export interface BaseEntity extends AuditFields {
  id: ID
}

// ==================== User Entities ====================

/**
 * User entity
 */
export interface User extends BaseEntity {
  email: string
  phoneNumber: string
  firstName: string
  lastName: string
  displayName?: string
  avatar?: string
  role: UserRole
  status: UserStatus
  gender?: Gender
  dateOfBirth?: Timestamp
  nationalId?: string
  emailVerified: boolean
  phoneVerified: boolean
  locale: Language
  timezone?: string
  lastLoginAt?: Timestamp
}

/**
 * Customer entity (extends User)
 */
export interface Customer extends User {
  role: UserRole.Customer
  address?: Address
  preferences?: CustomerPreferences
  loyaltyPoints?: number
  totalBookings?: number
  totalSpent?: number
}

/**
 * Customer preferences
 */
export interface CustomerPreferences {
  currency: Currency
  language: Language
  dateFormat: 'gregorian' | 'jalaali'
  numberFormat: 'western' | 'persian'
  notifications: NotificationPreferences
  privacy: PrivacySettings
}

/**
 * Notification preferences
 */
export interface NotificationPreferences {
  email: boolean
  sms: boolean
  push: boolean
  channels: NotificationChannel[]
  quietHoursStart?: string // HH:mm format
  quietHoursEnd?: string
  bookingReminders: boolean
  promotionalOffers: boolean
  newsletter: boolean
}

/**
 * Privacy settings
 */
export interface PrivacySettings {
  showProfile: boolean
  showReviews: boolean
  showBookingHistory: boolean
  allowDataSharing: boolean
}

// ==================== Provider Entities ====================

/**
 * Provider/Business entity
 */
export interface Provider extends BaseEntity {
  name: string
  nameEn?: string
  slug: string
  primaryCategory: ProviderCategory  // NEW: Primary business category
  /** @deprecated Use primaryCategory instead */
  type?: ProviderType  // DEPRECATED: Kept for backward compatibility
  status: ProviderStatus
  ownerId: ID
  description?: string
  shortDescription?: string
  logo?: string
  coverImage?: string
  phoneNumber: string
  email: string
  website?: string
  address: Address
  coordinates?: Coordinates
  rating?: number
  reviewCount?: number
  bookingCount?: number
  verified: boolean
  verifiedAt?: Timestamp
  featured: boolean
  settings?: ProviderSettings
  socialMedia?: SocialMediaLinks
}

/**
 * Provider settings
 */
export interface ProviderSettings {
  autoAcceptBookings: boolean
  requireDeposit: boolean
  depositPercentage?: number
  cancellationPolicy: CancellationPolicy
  bookingLeadTime: number // Minutes before booking can be made
  maxBookingAdvance: number // Days in advance
  bufferTime: number // Minutes between bookings
  allowWalkIns: boolean
  enableOnlinePayment: boolean
  currency: Currency
  timezone: string
  locale: Language
}

/**
 * Cancellation policy
 */
export interface CancellationPolicy {
  allowCancellation: boolean
  freeBeforeHours: number // Hours before appointment
  refundPercentage: number
  penaltyAmount?: Money
}

/**
 * Social media links
 */
export interface SocialMediaLinks {
  instagram?: string
  telegram?: string
  whatsapp?: string
  facebook?: string
  twitter?: string
  linkedin?: string
}

// ==================== Staff Entities ====================

/**
 * Staff member entity
 */
export interface Staff extends BaseEntity {
  providerId: ID
  userId?: ID
  firstName: string
  lastName: string
  displayName?: string
  avatar?: string
  role: StaffRole
  status: StaffStatus
  phoneNumber: string
  email?: string
  bio?: string
  specialties?: string[]
  rating?: number
  reviewCount?: number
  experienceYears?: number
  certifications?: Certification[]
  workingHours?: WorkingHours[]
  commissionRate?: number
}

/**
 * Staff certification
 */
export interface Certification {
  id: ID
  name: string
  issuer: string
  issueDate: Timestamp
  expiryDate?: Timestamp
  certificateUrl?: string
  verified: boolean
}

/**
 * Working hours for a specific day
 */
export interface WorkingHours {
  dayOfWeek: WeekDay
  isWorkingDay: boolean
  timeSlots: TimeSlot[]
  breaks?: TimeSlot[]
}

/**
 * Time slot
 */
export interface TimeSlot {
  startTime: string // HH:mm format
  endTime: string
}

// ==================== Service Entities ====================

/**
 * Service entity
 */
export interface Service extends BaseEntity {
  providerId: ID
  name: string
  nameEn?: string
  slug: string
  category: ServiceCategory
  status: ServiceStatus
  description?: string
  shortDescription?: string
  image?: string
  duration: number // Minutes
  price: Price
  pricingType: PricingType
  minPrice?: Money
  maxPrice?: Money
  staffIds?: ID[]
  requiresDeposit: boolean
  depositAmount?: Money
  bookingLeadTime?: number
  bufferTime?: number
  maxAdvanceBooking?: number
  features?: string[]
  tags?: string[]
  viewCount?: number
  bookingCount?: number
  rating?: number
  reviewCount?: number
}

/**
 * Service option/add-on
 */
export interface ServiceOption extends BaseEntity {
  serviceId: ID
  name: string
  nameEn?: string
  description?: string
  price: Money
  duration?: number // Additional minutes
  required: boolean
  multiple: boolean
  maxQuantity?: number
  displayOrder: number
}

// ==================== Booking Entities ====================

/**
 * Booking/Appointment entity
 */
export interface Booking extends BaseEntity {
  customerId: ID
  providerId: ID
  serviceId: ID
  staffId?: ID
  status: BookingStatus
  startTime: Timestamp
  endTime: Timestamp
  duration: number // Minutes
  totalAmount: Money
  depositAmount?: Money
  paidAmount: Money
  customerNotes?: string
  staffNotes?: string
  cancellationReason?: string
  cancelledBy?: ID
  cancelledAt?: Timestamp
  confirmedAt?: Timestamp
  completedAt?: Timestamp
  noShowAt?: Timestamp
  reminderSentAt?: Timestamp
  source: string
  referenceNumber: string
  metadata?: Record<string, unknown>
}

/**
 * Booking detail with related entities
 */
export interface BookingDetail extends Booking {
  customer: Customer
  provider: Provider
  service: Service
  staff?: Staff
  payments?: Payment[]
  selectedOptions?: ServiceOption[]
}

// ==================== Payment Entities ====================

/**
 * Payment entity
 */
export interface Payment extends BaseEntity {
  bookingId: ID
  customerId: ID
  providerId: ID
  amount: Money
  status: PaymentStatus
  method: PaymentMethod
  provider: PaymentProvider
  transactionId?: string
  referenceNumber?: string
  description?: string
  paidAt?: Timestamp
  authorizedAt?: Timestamp
  capturedAt?: Timestamp
  refundedAt?: Timestamp
  refundedAmount?: Money
  failureReason?: string
  metadata?: PaymentMetadata
}

/**
 * Payment metadata (provider-specific data)
 */
export interface PaymentMetadata {
  // ZarinPal specific
  authority?: string
  refId?: string

  // Card payment
  cardNumber?: string // Last 4 digits
  cardBank?: string

  // General
  ipAddress?: string
  userAgent?: string
  [key: string]: unknown
}

/**
 * Payout entity (provider earnings)
 */
export interface Payout extends BaseEntity {
  providerId: ID
  amount: Money
  status: PaymentStatus
  periodStart: Timestamp
  periodEnd: Timestamp
  commission: Money
  netAmount: Money
  bankAccount?: string
  bankName?: string
  accountHolder?: string
  processedAt?: Timestamp
  paidAt?: Timestamp
  failureReason?: string
  notes?: string
}

// ==================== Review Entities ====================

/**
 * Review entity
 */
export interface Review extends BaseEntity {
  bookingId: ID
  customerId: ID
  providerId: ID
  serviceId: ID
  staffId?: ID
  rating: number // 1-5
  title?: string
  comment?: string
  pros?: string[]
  cons?: string[]
  status: ReviewStatus
  helpful: number
  notHelpful: number
  images?: string[]
  response?: ReviewResponse
  publishedAt?: Timestamp
  flaggedAt?: Timestamp
  flagReason?: string
}

/**
 * Provider response to review
 */
export interface ReviewResponse {
  respondedBy: ID
  respondedAt: Timestamp
  message: string
}

// ==================== Notification Entities ====================

/**
 * Notification entity
 */
export interface Notification extends BaseEntity {
  userId: ID
  type: NotificationType
  channel: NotificationChannel
  status: NotificationStatus
  title: string
  message: string
  data?: Record<string, unknown>
  actionUrl?: string
  readAt?: Timestamp
  sentAt?: Timestamp
  deliveredAt?: Timestamp
  failureReason?: string
  expiresAt?: Timestamp
}

/**
 * Notification template
 */
export interface NotificationTemplate extends BaseEntity {
  type: NotificationType
  channel: NotificationChannel
  subject?: string
  template: string
  variables: string[]
  locale: Language
  active: boolean
}

// ==================== Media Entities ====================

/**
 * Media/File entity
 */
export interface Media extends BaseEntity {
  entityType: string
  entityId: ID
  type: MediaType
  fileName: string
  originalName: string
  mimeType: string
  size: number
  url: string
  thumbnailUrl?: string
  width?: number
  height?: number
  duration?: number
  alt?: string
  title?: string
  description?: string
  displayOrder?: number
  metadata?: Record<string, unknown>
}

/**
 * Gallery image
 */
export interface GalleryImage extends Media {
  category: string
  tags?: string[]
  featured: boolean
  beforeImage?: string
  afterImage?: string
}

// ==================== Business Hours Entities ====================

/**
 * Business hours for a provider
 */
export interface BusinessHours extends BaseEntity {
  providerId: ID
  dayOfWeek: WeekDay
  isOpen: boolean
  openTime?: string // HH:mm
  closeTime?: string
  breaks?: TimeSlot[]
  specialNote?: string
}

/**
 * Special hours (holidays, closures)
 */
export interface SpecialHours extends BaseEntity {
  providerId: ID
  date: Timestamp
  isClosed: boolean
  openTime?: string
  closeTime?: string
  reason?: string
  note?: string
}

// ==================== Location Entities ====================

/**
 * Province entity
 */
export interface Province {
  id: ID
  name: string
  nameEn?: string
  code?: string
  coordinates?: Coordinates
}

/**
 * City entity
 */
export interface City {
  id: ID
  provinceId: ID
  name: string
  nameEn?: string
  code?: string
  coordinates?: Coordinates
}

// ==================== Analytics Entities ====================

/**
 * Provider analytics
 */
export interface ProviderAnalytics {
  providerId: ID
  period: string
  totalBookings: number
  completedBookings: number
  cancelledBookings: number
  noShowBookings: number
  totalRevenue: Money
  averageBookingValue: Money
  newCustomers: number
  returningCustomers: number
  averageRating: number
  totalReviews: number
  viewCount: number
  clickCount: number
  conversionRate: number
}

/**
 * Service analytics
 */
export interface ServiceAnalytics {
  serviceId: ID
  period: string
  bookingCount: number
  revenue: Money
  averageRating: number
  reviewCount: number
  popularTimes: Record<string, number>
  popularDays: Record<WeekDay, number>
}

// ==================== Search & Discovery ====================

/**
 * Search result item
 */
export interface SearchResult {
  id: ID
  type: 'provider' | 'service' | 'staff'
  title: string
  subtitle?: string
  description?: string
  image?: string
  rating?: number
  reviewCount?: number
  price?: Money
  distance?: number
  relevanceScore: number
  url: string
}

/**
 * Search filters
 */
export interface SearchFilters {
  query?: string
  category?: ServiceCategory
  providerCategory?: ProviderCategory  // NEW: Filter by provider category
  /** @deprecated Use providerCategory instead */
  providerType?: ProviderType  // DEPRECATED
  location?: {
    city?: string
    province?: string
    coordinates?: Coordinates
    radius?: number // kilometers
  }
  priceRange?: {
    min?: number
    max?: number
  }
  rating?: number
  availability?: {
    date?: Timestamp
    time?: string
  }
  features?: string[]
  verified?: boolean
  featured?: boolean
  sortBy?: 'relevance' | 'rating' | 'price' | 'distance' | 'popular'
  sortOrder?: 'asc' | 'desc'
}

// ==================== Type Exports ====================

/**
 * Union type of all entities
 */
export type Entity =
  | User
  | Customer
  | Provider
  | Staff
  | Service
  | ServiceOption
  | Booking
  | Payment
  | Payout
  | Review
  | Notification
  | Media
  | BusinessHours
  | SpecialHours

/**
 * Entity type name
 */
export type EntityTypeName =
  | 'user'
  | 'customer'
  | 'provider'
  | 'staff'
  | 'service'
  | 'service_option'
  | 'booking'
  | 'payment'
  | 'payout'
  | 'review'
  | 'notification'
  | 'media'
  | 'business_hours'
  | 'special_hours'
