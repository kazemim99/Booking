/**
 * Microservices Configuration
 * Each microservice has its own base URL following DDD principles
 *
 * Architecture:
 * - ServiceCategory API (port 5010): Handles providers, services, bookings, schedules
 * - UserManagement API (port 5020): Handles authentication, users, profiles, customers
 */
export const microservices = {
  serviceCategory: {
    baseURL: import.meta.env.VITE_SERVICE_CATEGORY_API_URL || 'http://localhost:5010/api',
    timeout: 30000,
    withCredentials: true,
  },
  userManagement: {
    baseURL: import.meta.env.VITE_USER_MANAGEMENT_API_URL || 'http://localhost:5020/api',
    timeout: 30000,
    withCredentials: true,
  },
} as const

/**
 * @deprecated Legacy configuration - use microservices.serviceCategory or microservices.userManagement directly
 */
export const apiConfig = microservices.serviceCategory

export const apiEndpoints = {
  // Auth endpoints
  auth: {
    login: 'v1/auth/login',
    logout: 'v1/auth/logout',
    refresh: 'v1/auth/refresh',
    forgotPassword: 'v1/auth/forgot-password',
    resetPassword: 'v1/auth/reset-password',
    verifyEmail: 'v1/auth/verify-email',
    resendVerification: 'v1/auth/resend-verification',
  },

  // User endpoints
  users: {
    register: 'v1/users',
    profile: 'v1/users/profile',
    updateProfile: 'v1/users/profile',
    changePassword: 'v1/users/change-password',
    list: 'v1/users',
    byId: (id: string) => `v1/users/${id}`,
    search: 'v1/users/search',
  },

  // Booking endpoints
  bookings: {
    list: 'v1/bookings',
    create: 'v1/bookings',
    byId: (id: string) => `v1/bookings/${id}`,
    cancel: (id: string) => `v1/bookings/${id}/cancel`,
    reschedule: (id: string) => `v1/bookings/${id}/reschedule`,
    upcoming: 'v1/bookings/upcoming',
    history: 'v1/bookings/history',
  },

  // Appointment endpoints
  appointments: {
    list: '/appointments',
    byId: (id: string) => `/appointments/${id}`,
    availability: '/appointments/availability',
    slots: '/appointments/slots',
  },

  // Service endpoints
  services: {
    list: '/services',
    byId: (id: string) => `/services/${id}`,
    search: '/services/search',
    categories: '/services/categories',
  },

  // Schedule endpoints
  schedules: {
    list: '/schedules',
    byId: (id: string) => `/schedules/${id}`,
    workingHours: (id: string) => `/schedules/${id}/working-hours`,
    exceptions: (id: string) => `/schedules/${id}/exceptions`,
  },

  // Notification endpoints
  notifications: {
    list: '/notifications',
    unread: '/notifications/unread',
    markRead: (id: string) => `/notifications/${id}/read`,
    markAllRead: '/notifications/read-all',
  },

  // Review endpoints
  reviews: {
    list: '/reviews',
    create: '/reviews',
    byId: (id: string) => `/reviews/${id}`,
    byService: (serviceId: string) => `/reviews/service/${serviceId}`,
  },
}
