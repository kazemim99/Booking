import type { ProviderStatus } from '../constants/provider-status'

export interface User {
  id: string
  email: string
  phoneNumber?: string
  firstName?: string
  lastName?: string
  role: 'Admin' | 'Provider' | 'Client'
  isActive: boolean
  createdAt: string
  lastLoginAt?: string
}

export interface Provider {
  id: string
  businessName: string
  description?: string
  type?: string
  status: ProviderStatus
  city?: string
  state?: string
  country?: string
  logoUrl?: string
  profileImageUrl?: string
  allowOnlineBooking?: boolean
  offersMobileServices?: boolean
  isVerified?: boolean
  averageRating?: number
  totalReviews?: number
  serviceCount?: number
  registeredAt?: string
  lastActiveAt?: string
}

/**
 * `GET /Providers/{id}` returns a richer, nested shape than the list endpoints —
 * contact details and address are objects, not flat fields.
 */
export interface ProviderDetails extends Provider {
  ownerId?: string
  contactInfo?: {
    email?: string
    primaryPhone?: string
    secondaryPhone?: string
    website?: string
  }
  address?: {
    formattedAddress?: string
    city?: string
    state?: string
    postalCode?: string
    country?: string
    latitude?: number
    longitude?: number
  }
  yearsInBusiness?: number
  hierarchyType?: string
  isIndependent?: boolean
  tags?: string[]
}

export interface Service {
  id: string
  providerId: string
  categoryId: string
  name: string
  description?: string
  duration: number
  price: number
  currency: string
  isActive: boolean
  createdAt: string
}

export interface Category {
  id: string
  name: string
  description?: string
  iconUrl?: string
  parentCategoryId?: string
  isActive: boolean
  serviceCount?: number
}

export interface Booking {
  id: string
  clientId: string
  providerId: string
  serviceId: string
  bookingDate: string
  status: 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed'
  totalAmount: number
  currency: string
  createdAt: string
}

export interface Payment {
  id: string
  bookingId: string
  amount: number
  currency: string
  status: 'Pending' | 'Completed' | 'Failed' | 'Refunded'
  paymentMethod: string
  transactionId?: string
  createdAt: string
}

export interface SystemLog {
  id: string
  level: 'Info' | 'Warning' | 'Error' | 'Critical'
  message: string
  source: string
  timestamp: string
  details?: Record<string, unknown>
}

export interface DashboardStats {
  totalUsers: number
  totalProviders: number
  totalBookings: number
  totalRevenue: number
  activeUsers: number
  pendingProviders: number
  todayBookings: number
  monthlyGrowth: number
}

export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface ApiError {
  message: string
  code?: string
  details?: Record<string, unknown>
}
