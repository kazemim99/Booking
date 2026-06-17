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
  userId: string
  businessName: string
  description?: string
  address?: string
  phoneNumber: string
  email: string
  status: 'Pending' | 'Approved' | 'Rejected' | 'Suspended'
  rating?: number
  totalBookings?: number
  createdAt: string
  approvedAt?: string
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
