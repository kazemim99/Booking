/**
 * Backend API Configuration
 *
 * The backend is a single modular-monolith host (Booksy.Host) serving every
 * bounded context under one origin. Both keys below resolve to the same host;
 * they are kept separate only so callers can stay context-aware. In all
 * environments the VITE_* vars are set to the relative '/api' path (proxied to
 * the host), which is also the safe default if a var is missing.
 */
export const microservices = {
  serviceCategory: {
    baseURL: import.meta.env.VITE_SERVICE_CATALOG_API_URL || '/api',
    timeout: 30000,
    withCredentials: true,
  },
  userManagement: {
    baseURL: import.meta.env.VITE_USER_MANAGEMENT_API_URL || '/api',
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
    login: 'v1/Auth/login',
    logout: 'v1/Auth/logout',
    refresh: 'v1/Auth/refresh',
    forgotPassword: 'v1/Auth/forgot-password',
    resetPassword: 'v1/Auth/reset-password',
    verifyEmail: 'v1/Auth/verify-email',
    resendVerification: 'v1/Auth/resend-verification',
  },

  // User endpoints
  users: {
    register: 'v1/Users',
    profile: 'v1/Users/profile',
    updateProfile: 'v1/Users/profile',
    changePassword: 'v1/Users/change-password',
    list: 'v1/Users',
    byId: (id: string) => `v1/Users/${id}`,
    search: 'v1/Users/search',
  },

  // Booking endpoints
  bookings: {
    list: 'v1/Bookings',
    create: 'v1/Bookings',
    byId: (id: string) => `v1/Bookings/${id}`,
    cancel: (id: string) => `v1/Bookings/${id}/cancel`,
    reschedule: (id: string) => `v1/Bookings/${id}/reschedule`,
    upcoming: 'v1/Bookings/upcoming',
    history: 'v1/Bookings/history',
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
