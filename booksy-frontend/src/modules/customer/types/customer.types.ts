/**
 * Customer Profile Types
 * Type definitions for customer profile, bookings, reviews, and preferences
 */

// ============================================================================
// CUSTOMER PROFILE
// ============================================================================

export interface CustomerProfile {
  id: string
  firstName: string,
  lastName: string,
  phoneNumber: string
  email?: string
  createdAt: string
}

export interface UpdateCustomerProfileRequest {
  firstName: string
  lastName: string
  email?: string
}

// ============================================================================
// BOOKINGS
// ============================================================================

export interface UpcomingBooking {
  id: string
  providerId: string
  providerName: string
  providerLogoUrl?: string
  serviceName: string
  startTime: string
  endTime: string
  status: BookingStatus
  totalPrice?: number
}

export interface BookingHistoryEntry {
  id: string
  providerId: string
  providerName: string
  providerLogoUrl?: string
  serviceName: string
  startTime: string
  endTime: string
  status: BookingStatus
  totalPrice?: number
  createdAt: string
}

export interface BookingHistoryResult {
  items: BookingHistoryEntry[]
  page: number
  pageSize: number
  totalPages: number
  totalCount: number
}

export type BookingStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow'

// ============================================================================
// REVIEWS
// ============================================================================

export interface CustomerReview {
  id: string
  providerId: string
  providerName: string
  providerLogoUrl?: string
  serviceId: string
  serviceName: string
  rating: number
  text?: string
  createdAt: string
  updatedAt?: string
  canEdit: boolean // true if < 7 days old
}

export interface UpdateReviewRequest {
  rating: number
  text?: string
}

// ============================================================================
// NOTIFICATION PREFERENCES
// ============================================================================

export interface NotificationPreferences {
  smsEnabled: boolean
  emailEnabled: boolean
  reminderTiming: ReminderTiming
}

export type ReminderTiming = '1h' | '24h' | '3d'

export interface UpdatePreferencesRequest {
  smsEnabled: boolean
  emailEnabled: boolean
  reminderTiming: ReminderTiming
}

// ============================================================================
// MODAL STATE
// ============================================================================

export type CustomerModalType =
  | 'profile'
  | 'bookings'
  | 'favorites'
  | 'reviews'
  | 'settings'
  | null

// ============================================================================
// VIEW MODELS (for display)
// ============================================================================

export interface BookingCardView {
  id: string
  providerName: string
  providerLogoUrl?: string
  serviceName: string
  date: string // Jalali formatted
  time: string // Persian formatted
  status: BookingStatus
  statusLabel: string // Persian
  price?: string // Persian formatted
  isUpcoming: boolean
}

export interface ReviewCardView {
  id: string
  providerName: string
  providerLogoUrl?: string
  serviceName: string
  rating: number
  text?: string
  date: string // Jalali formatted
  canEdit: boolean
}

// ============================================================================
// HELPER TYPES
// ============================================================================

export interface LoadingState {
  profile: boolean
  upcomingBookings: boolean
  bookingHistory: boolean
  favorites: boolean
  reviews: boolean
  preferences: boolean
}

export interface ErrorState {
  profile: string | null
  upcomingBookings: string | null
  bookingHistory: string | null
  favorites: string | null
  reviews: string | null
  preferences: string | null
}

// ============================================================================
// TYPE GUARDS
// ============================================================================

export function isCustomerProfile(obj: unknown): obj is CustomerProfile {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    'id' in obj &&
    'fullName' in obj &&
    'phoneNumber' in obj
  )
}

export function isBookingHistoryEntry(obj: unknown): obj is BookingHistoryEntry {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    'id' in obj &&
    'providerId' in obj &&
    'serviceName' in obj &&
    'startTime' in obj &&
    'status' in obj
  )
}

export function isCustomerReview(obj: unknown): obj is CustomerReview {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    'id' in obj &&
    'providerId' in obj &&
    'rating' in obj &&
    'createdAt' in obj
  )
}

// ============================================================================
// CONSTANTS
// ============================================================================

export const BOOKING_STATUS_LABELS: Record<BookingStatus, string> = {
  Pending: 'در انتظار تایید',
  Confirmed: 'تایید شده',
  Completed: 'انجام شده',
  Cancelled: 'لغو شده',
  NoShow: 'عدم حضور',
}

export const REMINDER_TIMING_LABELS: Record<ReminderTiming, string> = {
  '1h': '۱ ساعت قبل',
  '24h': '۱ روز قبل',
  '3d': '۳ روز قبل',
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Check if a booking is upcoming (start time is in the future)
 */
export function isUpcoming(startTime: string): boolean {
  const bookingTime = new Date(startTime)
  const now = new Date()
  return bookingTime > now
}

/**
 * Check if a review can be edited (created less than 7 days ago)
 */
export function canEditReview(createdAt: string, updatedAt?: string): boolean {
  const reviewDate = new Date(updatedAt || createdAt)
  const now = new Date()
  const diffDays = Math.floor((now.getTime() - reviewDate.getTime()) / (1000 * 60 * 60 * 24))
  return diffDays < 7
}

/**
 * Get booking status label in Persian
 */
export function getBookingStatusLabel(status: BookingStatus): string {
  return BOOKING_STATUS_LABELS[status] || status
}

/**
 * Get reminder timing label in Persian
 */
export function getReminderTimingLabel(timing: ReminderTiming): string {
  return REMINDER_TIMING_LABELS[timing] || timing
}

/**
 * Format price in Persian
 */
export function formatPrice(price: number): string {
  return new Intl.NumberFormat('fa-IR').format(price) + ' تومان'
}

/**
 * Convert rating to star array for display
 */
export function getStarRating(rating: number): { full: number; half: boolean; empty: number } {
  const full = Math.floor(rating)
  const half = rating % 1 >= 0.5
  const empty = 5 - full - (half ? 1 : 0)
  return { full, half, empty }
}
